using System.Net;
using System.Text;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Self-contained HTML export (inline CSS, no scripts, no external requests).</summary>
public static class HtmlReportWriter
{
    private static string E(string? text) => WebUtility.HtmlEncode(text ?? "");

    public static string Render(GradeReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"pt-BR\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        // The report only needs inline styles: block everything else defensively.
        sb.AppendLine("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'\">");
        sb.AppendLine($"<title>Notas - {E(report.CourseName)}</title>");
        sb.AppendLine($"<style>{Css}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{E(report.CourseName)}</h1>");
        sb.AppendLine($"<p class=\"meta\">Critério de nota: {E(ReportFormatting.Criteria(report.Options))}</p>");
        foreach (var w in report.Warnings)
            sb.AppendLine($"<p class=\"warn\">Aviso: {E(w)}</p>");

        sb.AppendLine("<div class=\"wrap\"><table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th class=\"name\">Aluno</th>");
        foreach (var c in report.Columns)
            sb.AppendLine($"<th class=\"num\">{E(c.Title)}<br><small>(/{E(ReportFormatting.Number(c.MaxPoints))})</small></th>");
        sb.AppendLine("<th class=\"num total\">Total</th><th class=\"num total\">Possível</th><th class=\"num total\">%</th>");
        sb.AppendLine("</tr></thead>");

        sb.AppendLine("<tbody>");
        foreach (var row in report.Rows)
        {
            sb.Append("<tr>");
            sb.Append($"<th class=\"name\" scope=\"row\">{E(row.Student.FullName)}");
            if (!string.IsNullOrWhiteSpace(row.Student.Email)) sb.Append($"<br><small>{E(row.Student.Email)}</small>");
            sb.Append("</th>");
            foreach (var cell in row.Cells)
                sb.Append(cell.Grade is null
                    ? "<td class=\"num missing\">-</td>"
                    : $"<td class=\"num\">{E(ReportFormatting.Cell(cell))}</td>");
            sb.Append($"<td class=\"num total\">{E(ReportFormatting.Number(row.Total))}</td>");
            sb.Append($"<td class=\"num total\">{E(ReportFormatting.Number(row.TotalPossible))}</td>");
            sb.Append($"<td class=\"num total {PercentClass(row.Percentage)}\">{E(ReportFormatting.Percent(row.Percentage))}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table></div>");
        sb.AppendLine($"<p class=\"meta\">{report.Rows.Count} aluno(s), {report.Columns.Count} atividade(s).</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static async Task WriteAsync(GradeReport report, string path, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, Render(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);
    }

    private static string PercentClass(double? pct) => pct switch
    {
        null => "",
        >= 70 => "good",
        >= 50 => "mid",
        _ => "low",
    };

    private const string Css = """
        :root{color-scheme:light dark;--bg:#fff;--fg:#1f2328;--muted:#656d76;--line:#d0d7de;--head:#eef2f7;--good:#1a7f37;--mid:#9a6700;--low:#cf222e}
        @media (prefers-color-scheme:dark){:root{--bg:#0d1117;--fg:#e6edf3;--muted:#8d96a0;--line:#30363d;--head:#161b22;--good:#3fb950;--mid:#d29922;--low:#f85149}}
        body{font:15px/1.45 system-ui,-apple-system,"Segoe UI",sans-serif;background:var(--bg);color:var(--fg);margin:0;padding:24px}
        h1{margin:0 0 4px;font-size:1.5rem}
        .meta{color:var(--muted);margin:4px 0 16px}.warn{color:var(--mid);margin:4px 0}
        .wrap{overflow:auto;border:1px solid var(--line);border-radius:8px}
        table{border-collapse:collapse;width:100%}
        th,td{padding:8px 12px;border-bottom:1px solid var(--line);white-space:nowrap}
        thead th{position:sticky;top:0;background:var(--head);text-align:right;vertical-align:bottom}
        tbody tr:hover{background:var(--head)}
        .name{text-align:left;font-weight:600}tbody th.name{position:sticky;left:0;background:var(--bg)}
        .num{text-align:right;font-variant-numeric:tabular-nums}
        small{font-weight:400;color:var(--muted)}
        .missing{color:var(--muted)}.total{font-weight:600;border-left:1px solid var(--line)}
        .good{color:var(--good)}.mid{color:var(--mid)}.low{color:var(--low)}
        @media print{body{padding:0}.wrap{border:0;overflow:visible}thead th{position:static}}
        """;
}
