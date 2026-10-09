using ClassroomGradeReport.Api;
using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

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
    if (options.AllCourses && options.CsvPath is { } p && p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        throw new UserFacingException("Com --all-courses, --csv deve ser uma pasta (um CSV por turma), não um arquivo .csv.");

    IReadOnlyList<Course> selected = options.AllCourses ? courses
        : [options.Course is { } query ? FindCourse(courses, query) : ConsoleReportRenderer.PickCourse(courses, console)!];

    var builder = services.GetRequiredService<GradeReportBuilder>();
    var reportOptions = new ReportOptions(options.IncludeDrafts, options.MissingAsZero);

    foreach (var course in selected)
    {
        var data = await console.Status().StartAsync($"Carregando {Markup.Escape(course.Name)}...", _ => gateway.LoadCourseDataAsync(course, cts.Token));
        var report = builder.Build(data, reportOptions);

        ConsoleReportRenderer.Render(report, console);

        if (options.CsvPath is { } csv)
        {
            var target = options.AllCourses ? Path.Combine(csv, CsvReportWriter.FileNameFor(course.Name, course.Id)) : csv;
            await CsvReportWriter.WriteAsync(report, target, cts.Token);
            console.MarkupLine($"[green]CSV gerado:[/] {Markup.Escape(Path.GetFullPath(target))}");
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
