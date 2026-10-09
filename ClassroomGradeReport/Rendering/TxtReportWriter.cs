using System.Text;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Plain-text export: a monospace-aligned table preceded by the course and grading criteria.</summary>
public static class TxtReportWriter
{
    public static string Render(GradeReport report)
    {
        var header = new List<string> { "Aluno" };
        header.AddRange(report.Columns.Select(c => $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})"));
        header.AddRange(["Total", "Possível", "%"]);

        var rows = report.Rows.Select(r => new List<string>([
            r.Student.FullName,
            .. r.Cells.Select(ReportFormatting.Cell),
            ReportFormatting.Number(r.Total),
            ReportFormatting.Number(r.TotalPossible),
            ReportFormatting.Percent(r.Percentage),
        ])).ToList();

        var widths = header.Select((h, i) => Math.Max(h.Length, rows.Select(r => r[i].Length).DefaultIfEmpty(0).Max())).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine($"Turma: {report.CourseName}");
        sb.AppendLine($"Critério de nota: {ReportFormatting.Criteria(report.Options)}");
        foreach (var w in report.Warnings) sb.AppendLine($"Aviso: {w}");
        sb.AppendLine();

        AppendRow(sb, header, widths);
        sb.AppendLine(string.Join("-+-", widths.Select(w => new string('-', w))));
        foreach (var r in rows) AppendRow(sb, r, widths);

        sb.AppendLine();
        sb.AppendLine($"{report.Rows.Count} aluno(s), {report.Columns.Count} atividade(s).");
        return sb.ToString();
    }

    public static async Task WriteAsync(GradeReport report, string path, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, Render(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);
    }

    // First column (names) is left-aligned; everything else (numbers) is right-aligned.
    private static void AppendRow(StringBuilder sb, IReadOnlyList<string> cells, int[] widths) =>
        sb.AppendLine(string.Join(" | ", cells.Select((c, i) => i == 0 ? c.PadRight(widths[i]) : c.PadLeft(widths[i]))).TrimEnd());
}
