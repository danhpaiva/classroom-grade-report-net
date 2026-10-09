using ClassroomGradeReport.Api;
using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

const int MaxParallelCourses = 3;

Console.OutputEncoding = System.Text.Encoding.UTF8;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    var options = CliOptions.Parse(args);
    if (options.ShowHelp)
    {
        Console.WriteLine(CliOptions.HelpText);
        return 0;
    }

    if (options.Logout)
    {
        new GoogleAuthenticator(AuthPaths.ResolveCredentials(options.CredentialsPath), AuthPaths.TokenDir).DeleteToken();
        Console.WriteLine("Token apagado. Na próxima execução você poderá autorizar com outra conta.");
        return 0;
    }

    var services = new ServiceCollection()
        .AddSingleton<IAuthenticator>(_ => new GoogleAuthenticator(
            AuthPaths.ResolveCredentials(options.CredentialsPath), AuthPaths.TokenDir))
        .AddSingleton<IClassroomGateway, ClassroomGateway>()
        .AddSingleton<GradeReportBuilder>()
        .AddSingleton(AnsiConsole.Console)
        .BuildServiceProvider();

    var gateway = services.GetRequiredService<IClassroomGateway>();
    var console = services.GetRequiredService<IAnsiConsole>();

    var courses = await gateway.ListTeacherCoursesAsync(activeOnly: !options.IncludeInactiveCourses, cts.Token);
    if (courses.Count == 0)
        throw new UserFacingException("Nenhuma turma encontrada em que você seja professor(a)." +
            (options.IncludeInactiveCourses ? "" : " Use --include-inactive para incluir turmas não ativas."));

    if (options.AllCourses && options.Course is not null)
        throw new UserFacingException("Use --course ou --all-courses, não os dois.");
    var exports = new List<Export>();
    if (options.CsvPath is { } csvPath) exports.Add(new("CSV", "--csv", ".csv", csvPath, CsvReportWriter.WriteAsync));
    if (options.XlsxPath is { } xlsxPath) exports.Add(new("Excel", "--xlsx", ".xlsx", xlsxPath, XlsxReportWriter.WriteAsync));
    if (options.TxtPath is { } txtPath) exports.Add(new("TXT", "--txt", ".txt", txtPath, TxtReportWriter.WriteAsync));
    if (options.AllCourses)
        foreach (var e in exports.Where(e => e.Path.EndsWith(e.Extension, StringComparison.OrdinalIgnoreCase)))
            throw new UserFacingException($"Com --all-courses, {e.Option} deve ser uma pasta (um arquivo por turma), não um arquivo {e.Extension}.");

    IReadOnlyList<Course> selected = options.AllCourses ? courses
        : [options.Course is { } query ? FindCourse(courses, query) : ConsoleReportRenderer.PickCourse(courses, console)!];

    var builder = services.GetRequiredService<GradeReportBuilder>();
    var reportOptions = new ReportOptions(options.IncludeDrafts, options.MissingAsZero);

    // Load courses concurrently (bounded, to respect API quota); render/write in the original order.
    using var gate = new SemaphoreSlim(MaxParallelCourses);
    var loads = selected.Select(async course =>
    {
        await gate.WaitAsync(cts.Token);
        try { return await gateway.LoadCourseDataAsync(course, cts.Token); }
        finally { gate.Release(); }
    }).ToList();

    for (var i = 0; i < selected.Count; i++)
    {
        var course = selected[i];
        var data = loads[i].IsCompleted
            ? await loads[i]
            : await console.Status().StartAsync($"Carregando {Markup.Escape(course.Name)}...", _ => loads[i]);
        var report = builder.Build(data, reportOptions);

        ConsoleReportRenderer.Render(report, console);

        foreach (var export in exports)
        {
            var target = options.AllCourses
                ? Path.Combine(export.Path, ReportFormatting.FileNameFor(course.Name, course.Id, export.Extension))
                : export.Path;
            await export.Write(report, target, cts.Token);
            console.MarkupLine($"[green]{export.Label} gerado:[/] {Markup.Escape(Path.GetFullPath(target))}");
        }
        console.WriteLine();
    }
    return 0;
}
catch (UserFacingException ex)
{
    AnsiConsole.MarkupLineInterpolated($"[red]Erro:[/] {ex.Message}");
    return 1;
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[yellow]Operação cancelada.[/]");
    return 130;
}

static Course FindCourse(IReadOnlyList<Course> courses, string query)
{
    var byId = courses.FirstOrDefault(c => c.Id == query);
    if (byId is not null) return byId;

    var matches = courses.Where(c => c.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
    return matches.Count switch
    {
        1 => matches[0],
        0 => throw new UserFacingException($"Nenhuma turma corresponde a \"{query}\"."),
        _ => throw new UserFacingException(
            $"Mais de uma turma corresponde a \"{query}\": {string.Join("; ", matches.Select(m => $"{m.Name} [{m.Id}]"))}. Use o ID."),
    };
}

/// <summary>One requested export format: where to write it and how.</summary>
record Export(string Label, string Option, string Extension, string Path, Func<GradeReport, string, CancellationToken, Task> Write);
