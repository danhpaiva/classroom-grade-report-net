using System.Text;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Plain-text export: a monospace-aligned table preceded by the course and grading criteria.</summary>
public static class TxtReportWriter
{
    /// <summary>Renders the report as aligned plain text.</summary>
    public static string Render(GradeReport report)
    {
        var header = Header(report);
        var rows = report.Rows.Select(DataRow).ToList();
        var widths = ColumnWidths(header, rows);

        var sb = new StringBuilder();
        AppendPreamble(sb, report);
        AppendRow(sb, header, widths);
        sb.AppendLine(string.Join("-+-", widths.Select(w => new string('-', w))));
        foreach (var r in rows) AppendRow(sb, r, widths);

        sb.AppendLine();
        sb.AppendLine($"{report.Rows.Count} aluno(s), {report.Columns.Count} atividade(s).");
        if (report.Stats is { } stats)
            foreach (var line in ReportFormatting.StatsLines(stats)) sb.AppendLine(line);
        return sb.ToString();
    }

    /// <summary>Renders the report and saves it to <paramref name="path"/> (UTF-8 with BOM, folders created as needed).</summary>
    public static Task WriteAsync(GradeReport report, string path, CancellationToken ct) =>
        ReportFile.WriteTextAsync(path, Render(report), utf8Bom: true, ct);

    private static List<string> Header(GradeReport report) =>
        ["Aluno",
            .. report.Columns.Select(c => $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})"),
            "Total", "Possível", "%"];

    private static List<string> DataRow(StudentRow r) =>
        [r.Student.FullName,
            .. r.Cells.Select(ReportFormatting.Cell),
            ReportFormatting.Number(r.Total),
            ReportFormatting.Number(r.TotalPossible),
            ReportFormatting.Percent(r.Percentage)];

    /// <summary>Width of each column: the longest text between the header and every row.</summary>
    private static int[] ColumnWidths(List<string> header, List<List<string>> rows) =>
        header.Select((h, i) => Math.Max(h.Length, rows.Select(r => r[i].Length).DefaultIfEmpty(0).Max())).ToArray();

    private static void AppendPreamble(StringBuilder sb, GradeReport report)
    {
        sb.AppendLine($"Turma: {report.CourseName}");
        sb.AppendLine($"Critério de nota: {ReportFormatting.Criteria(report.Options)}");
        foreach (var w in report.Warnings) sb.AppendLine($"Aviso: {w}");
        sb.AppendLine();
    }

    // First column (names) is left-aligned; everything else (numbers) is right-aligned.
    private static void AppendRow(StringBuilder sb, IReadOnlyList<string> cells, int[] widths) =>
        sb.AppendLine(string.Join(" | ", cells.Select((c, i) => i == 0 ? c.PadRight(widths[i]) : c.PadLeft(widths[i]))).TrimEnd());
}
