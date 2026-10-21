using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Reporting;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ClassroomGradeReport.Rendering;

/// <summary>Terminal output: the report table and the course selection menu.</summary>
public static class ConsoleReportRenderer
{
    /// <summary>Prints the course, grading criteria, warnings, the grades table and a summary line.</summary>
    public static void Render(GradeReport report, IAnsiConsole console)
    {
        console.MarkupLine($"[bold]Turma:[/] {Markup.Escape(report.CourseName)}");
        console.MarkupLine($"[grey]Critério de nota: {Markup.Escape(ReportFormatting.Criteria(report.Options))}[/]");
        foreach (var w in report.Warnings)
            console.MarkupLine($"[yellow]Aviso:[/] {Markup.Escape(w)}");

        console.Write(BuildTable(report));
        console.MarkupLine($"[grey]{report.Rows.Count} aluno(s), {report.Columns.Count} atividade(s).[/]");
    }

    /// <summary>Shows an arrow-key menu and returns the chosen course.</summary>
    public static Course? PickCourse(IReadOnlyList<Course> courses, IAnsiConsole console) =>
        console.Prompt(new SelectionPrompt<Course>()
            .Title("Escolha a [green]turma[/]:")
            .UseConverter(c => Markup.Escape(string.IsNullOrWhiteSpace(c.Section) ? c.Name : $"{c.Name} ({c.Section})"))
            .AddChoices(courses));

    private static Table BuildTable(GradeReport report)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Aluno");
        foreach (var c in report.Columns)
            table.AddColumn(new TableColumn($"{Markup.Escape(c.Title)}\n[grey](/{ReportFormatting.Number(c.MaxPoints)})[/]").RightAligned());
        table.AddColumn(new TableColumn("Total").RightAligned());
        table.AddColumn(new TableColumn("Possível").RightAligned());
        table.AddColumn(new TableColumn("%").RightAligned());

        foreach (var row in report.Rows)
            table.AddRow(TableRow(row));
        return table;
    }

    private static IRenderable[] TableRow(StudentRow row) =>
    [
        new Text(row.Student.FullName),
        .. row.Cells.Select(c => new Text(ReportFormatting.Cell(c))),
        new Text(ReportFormatting.Number(row.Total)),
        new Text(ReportFormatting.Number(row.TotalPossible)),
        new Text(ReportFormatting.Percent(row.Percentage)),
    ];
}
