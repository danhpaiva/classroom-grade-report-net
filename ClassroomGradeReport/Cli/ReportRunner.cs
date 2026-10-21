using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using ClassroomGradeReport.Api;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using Spectre.Console;

namespace ClassroomGradeReport.Cli;

/// <summary>Runs the report flow: pick courses, load their data, build the reports, print and export them.</summary>
public sealed class ReportRunner(
    IClassroomGateway gateway, GradeReportBuilder builder, IAnsiConsole console, RetryNotifier retryNotifier)
{
    /// <summary>Courses loaded at the same time with --all-courses (bounded to respect the API quota).</summary>
    public const int MaxParallelCourses = 3;

    /// <summary>Runs the whole flow for the given options.</summary>
    /// <exception cref="UserFacingException">Invalid options, no courses, or an API/authorization problem.</exception>
    public async Task RunAsync(CliOptions options, CancellationToken ct)
    {
        options.Validate();
        var exports = Export.FromOptions(options);
        if (options.AllCourses) Export.EnsureFolders(exports);

        var courses = await ListCoursesAsync(options, ct);
        var selected = CourseSelector.Select(options, courses, console);

        foreach (var data in await LoadAllAsync(selected, ct))
            await ProcessCourseAsync(data, options, exports, ct);
    }

    /// <summary>Lists the teacher's courses, failing with a helpful message when there are none.</summary>
    private async Task<IReadOnlyList<Course>> ListCoursesAsync(CliOptions options, CancellationToken ct)
    {
        var courses = await gateway.ListTeacherCoursesAsync(activeOnly: !options.IncludeInactiveCourses, ct);
        if (courses.Count > 0) return courses;

        throw new UserFacingException("Nenhuma turma encontrada em que você seja professor(a)." +
            (options.IncludeInactiveCourses ? "" : " Use --include-inactive para incluir turmas não ativas."));
    }

    /// <summary>
    /// Loads every course (up to <see cref="MaxParallelCourses"/> at a time) under a progress display, in input order.
    /// The first real failure cancels the loads still running and is the one reported, so no load is left orphaned.
    /// Retry notices are held back while the display is live and printed afterwards.
    /// </summary>
    private async Task<IReadOnlyList<ClassroomData>> LoadAllAsync(IReadOnlyList<Course> courses, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var gate = new SemaphoreSlim(MaxParallelCourses);
        var firstError = new StrongBox<Exception?>();
        var notices = new ConcurrentQueue<string>();

        try
        {
            using (retryNotifier.Capture(notices.Enqueue))
            {
                return await console.Progress()
                    .AutoClear(true)
                    .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        var loads = courses
                            .Select(c => LoadOneAsync(c, ctx.AddTask(Markup.Escape(c.Name), autoStart: false), gate, cts, firstError))
                            .ToList();
                        return await Task.WhenAll(loads);
                    });
            }
        }
        catch when (firstError.Value is { } error)
        {
            // Tasks cancelled as a consequence of the failure throw OperationCanceledException; report the real cause.
            ExceptionDispatchInfo.Capture(error).Throw();
            throw;
        }
        finally
        {
            foreach (var notice in notices.Distinct())
                console.MarkupLineInterpolated($"[yellow]{notice}[/]");
        }
    }

    /// <summary>Loads one course once a slot is free in <paramref name="gate"/>, advancing its progress bar.</summary>
    private async Task<ClassroomData> LoadOneAsync(
        Course course, ProgressTask bar, SemaphoreSlim gate, CancellationTokenSource cts, StrongBox<Exception?> firstError)
    {
        try
        {
            await gate.WaitAsync(cts.Token);
            try
            {
                bar.IsIndeterminate = true;
                bar.StartTask();
                var data = await gateway.LoadCourseDataAsync(course, cts.Token);
                bar.IsIndeterminate = false;
                bar.Increment(bar.MaxValue);
                return data;
            }
            finally { gate.Release(); }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.CompareExchange(ref firstError.Value, ex, null);
            await cts.CancelAsync();
            throw;
        }
    }

    /// <summary>Builds the general and exam reports of one course, prints them and writes every requested export.</summary>
    private async Task ProcessCourseAsync(
        ClassroomData data, CliOptions options, IReadOnlyList<Export> exports, CancellationToken ct)
    {
        var reports = builder.BuildAll(data, options.ToReportOptions(), options.ToExamSplit());
        if (reports.Count == 0)
            console.MarkupLineInterpolated($"[yellow]{data.Course.Name}: nenhuma atividade com \"{options.ExamKeyword}\" no título.[/]");

        var takenExamPaths = new HashSet<string>();
        foreach (var report in reports)
        {
            ConsoleReportRenderer.Render(report, console);
            foreach (var export in exports)
                await WriteExportAsync(export, report, data.Course, options.AllCourses, takenExamPaths, ct);
            console.WriteLine();
        }
    }

    private async Task WriteExportAsync(
        Export export, GradeReport report, Course course, bool allCourses, ISet<string> takenExamPaths, CancellationToken ct)
    {
        var target = export.TargetFor(report, course, allCourses, takenExamPaths);
        await export.Write(report, target, ct);
        console.MarkupLine($"[green]{export.Label} gerado:[/] {Markup.Escape(Path.GetFullPath(target))}");
    }
}
