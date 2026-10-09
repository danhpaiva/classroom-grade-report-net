using System.Text;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Excel pt-BR friendly CSV: ';' separator, decimal comma, UTF-8 with BOM.</summary>
public static class CsvReportWriter
{
    public static string Render(GradeReport report)
    {
        var sb = new StringBuilder();
        AppendLine(sb, ["Aluno", "E-mail",
            .. report.Columns.Select(c => $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})"),
            "Total", "Total possível", "Percentual"]);

        foreach (var row in report.Rows)
        {
            AppendLine(sb, [row.Student.FullName, row.Student.Email ?? "",
                .. row.Cells.Select(ReportFormatting.Cell),
                ReportFormatting.Number(row.Total), ReportFormatting.Number(row.TotalPossible),
                ReportFormatting.Percent(row.Percentage)]);
        }
        return sb.ToString();
    }

    public static async Task WriteAsync(GradeReport report, string path, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, Render(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);
    }

    private static void AppendLine(StringBuilder sb, IEnumerable<string> fields) =>
        sb.Append(string.Join(';', fields.Select(Escape))).Append("\r\n");

    private static string Escape(string field)
    {
        // Neutralize spreadsheet formula injection from user-controlled text (names, titles).
        if (field.Length > 0 && field[0] is '=' or '+' or '@' || field.StartsWith('-') && field.Length > 1 && !char.IsDigit(field[1]))
            field = "'" + field;
        return field.AsSpan().IndexOfAny(";\"\r\n") >= 0 ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
    }
}
