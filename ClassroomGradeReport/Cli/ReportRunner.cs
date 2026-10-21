using ClassroomGradeReport.Api;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using Spectre.Console;

namespace ClassroomGradeReport.Cli;

/// <summary>Runs the report flow: pick courses, load their data, build the reports, print and export them.</summary>
public sealed class ReportRunner(IClassroomGateway gateway, GradeReportBuilder builder, IAnsiConsole console)
{
    /// <summary>Courses loaded at the same time with --all-courses (bounded to respect the API quota).</summary>
    private const int MaxParallelCourses = 3;

    /// <summary>Runs the whole flow for the given options.</summary>
    /// <exception cref="UserFacingException">Invalid options, no courses, or an API/authorization problem.</exception>
    public async Task RunAsync(CliOptions options, CancellationToken ct)
    {
        options.Validate();
        var exports = Export.FromOptions(options);
        if (options.AllCourses) Export.EnsureFolders(exports);

        var courses = await ListCoursesAsync(options, ct);
        var selected = CourseSelector.Select(options, courses, console);

        // Load courses concurrently; print and write in the original order.
        using var gate = new SemaphoreSlim(MaxParallelCourses);
        var loads = selected.Select(course => LoadAsync(course, gate, ct)).ToList();

        for (var i = 0; i < selected.Count; i++)
        {
            var data = await AwaitLoadAsync(selected[i], loads[i]);
            await ProcessCourseAsync(data, options, exports, ct);
        }
    }

    /// <summary>Lists the teacher's courses, failing with a helpful message when there are none.</summary>
    private async Task<IReadOnlyList<Course>> ListCoursesAsync(CliOptions options, CancellationToken ct)
    {
        var courses = await gateway.ListTeacherCoursesAsync(activeOnly: !options.IncludeInactiveCourses, ct);
        if (courses.Count > 0) return courses;

        throw new UserFacingException("Nenhuma turma encontrada em que você seja professor(a)." +
            (options.IncludeInactiveCourses ? "" : " Use --include-inactive para incluir turmas não ativas."));
    }

    /// <summary>Loads one course once a slot is free in <paramref name="gate"/>.</summary>
    private async Task<ClassroomData> LoadAsync(Course course, SemaphoreSlim gate, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try { return await gateway.LoadCourseDataAsync(course, ct); }
        finally { gate.Release(); }
    }

    /// <summary>Waits for a course to load, showing a spinner only when it is not ready yet.</summary>
    private async Task<ClassroomData> AwaitLoadAsync(Course course, Task<ClassroomData> load) =>
        load.IsCompleted
            ? await load
            : await console.Status().StartAsync($"Carregando {Markup.Escape(course.Name)}...", _ => load);

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
