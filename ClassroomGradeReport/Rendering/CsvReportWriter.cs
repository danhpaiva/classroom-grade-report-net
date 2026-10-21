using System.Text;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Excel pt-BR friendly CSV: ';' separator, decimal comma, UTF-8 with BOM.</summary>
public static class CsvReportWriter
{
    /// <summary>Renders the report as CSV text: one header line plus one line per student (CRLF line endings).</summary>
    public static string Render(GradeReport report)
    {
        var sb = new StringBuilder();
        AppendLine(sb, Header(report));
        foreach (var row in report.Rows)
            AppendLine(sb, DataRow(row));
        return sb.ToString();
    }

    /// <summary>Renders the report and saves it to <paramref name="path"/> (UTF-8 with BOM, folders created as needed).</summary>
    public static Task WriteAsync(GradeReport report, string path, CancellationToken ct) =>
        ReportFile.WriteTextAsync(path, Render(report), utf8Bom: true, ct);

    private static IEnumerable<string> Header(GradeReport report) =>
        ["Aluno", "E-mail",
            .. report.Columns.Select(c => $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})"),
            "Total", "Total possível", "Percentual"];

    private static IEnumerable<string> DataRow(StudentRow row) =>
        [row.Student.FullName, row.Student.Email ?? "",
            .. row.Cells.Select(ReportFormatting.Cell),
            ReportFormatting.Number(row.Total), ReportFormatting.Number(row.TotalPossible),
            ReportFormatting.Percent(row.Percentage)];

    private static void AppendLine(StringBuilder sb, IEnumerable<string> fields) =>
        sb.Append(string.Join(';', fields.Select(Escape))).Append("\r\n");

    /// <summary>Neutralizes formula injection, then quotes the field when it contains a separator, quote or line break.</summary>
    private static string Escape(string field)
    {
        if (LooksLikeFormula(field)) field = "'" + field;
        return field.AsSpan().IndexOfAny(";\"\r\n") >= 0 ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
    }

    /// <summary>
    /// True when a spreadsheet could interpret the text as a formula: it starts with '=', '+', '@',
    /// or with '-' not followed by a digit (so negative numbers stay numbers).
    /// </summary>
    private static bool LooksLikeFormula(string field)
    {
        if (field.Length == 0) return false;
        if (field[0] is '=' or '+' or '@') return true;
        return field[0] == '-' && field.Length > 1 && !char.IsDigit(field[1]);
    }
}
