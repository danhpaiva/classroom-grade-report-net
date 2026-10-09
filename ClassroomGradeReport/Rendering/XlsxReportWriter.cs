using ClassroomGradeReport.Reporting;
using ClosedXML.Excel;

namespace ClassroomGradeReport.Rendering;

/// <summary>Excel (.xlsx) export: real numeric cells, formatted header, frozen panes.</summary>
public static class XlsxReportWriter
{
    public static void Write(GradeReport report, Stream output)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Notas");
        var col = 1;

        Header(ws, 1, col++, "Aluno");
        Header(ws, 1, col++, "E-mail");
        foreach (var c in report.Columns)
            Header(ws, 1, col++, $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})");
        var totalCol = col;
        Header(ws, 1, col++, "Total");
        Header(ws, 1, col++, "Total possível");
        Header(ws, 1, col, "Percentual");
        var lastCol = col;

        for (var r = 0; r < report.Rows.Count; r++)
        {
            var row = report.Rows[r];
            var x = r + 2;
            Text(ws, x, 1, row.Student.FullName);
            Text(ws, x, 2, row.Student.Email ?? "");

            for (var i = 0; i < row.Cells.Count; i++)
            {
                var cell = ws.Cell(x, 3 + i);
                if (row.Cells[i].Grade is { } g) cell.SetValue(g);
                else cell.SetValue("-");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            ws.Cell(x, totalCol).SetValue(row.Total);
            ws.Cell(x, totalCol + 1).SetValue(row.TotalPossible);
            if (row.Percentage is { } p)
            {
                ws.Cell(x, totalCol + 2).SetValue(p / 100.0);
                ws.Cell(x, totalCol + 2).Style.NumberFormat.Format = "0.0%";
            }
            else
            {
                ws.Cell(x, totalCol + 2).SetValue("-");
            }
            ws.Cell(x, totalCol + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        ws.Range(2, 3, Math.Max(2, report.Rows.Count + 1), lastCol - 1).Style.NumberFormat.Format = "0.##";
        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(2);
        ws.Columns().AdjustToContents(1, Math.Max(1, report.Rows.Count + 1), 8, 40);

        // Second sheet: how the numbers were computed.
        var info = wb.AddWorksheet("Critério");
        Text(info, 1, 1, "Turma");
        Text(info, 1, 2, report.CourseName);
        Text(info, 2, 1, "Critério de nota");
        Text(info, 2, 2, ReportFormatting.Criteria(report.Options));
        for (var i = 0; i < report.Warnings.Count; i++)
        {
            Text(info, 3 + i, 1, "Aviso");
            Text(info, 3 + i, 2, report.Warnings[i]);
        }
        info.Column(1).Style.Font.Bold = true;
        info.Columns().AdjustToContents();

        wb.SaveAs(output);
    }

    public static async Task WriteAsync(GradeReport report, string path, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await using var ms = new MemoryStream();
        Write(report, ms);
        await File.WriteAllBytesAsync(path, ms.ToArray(), ct);
    }

    private static void Header(IXLWorksheet ws, int row, int col, string text)
    {
        var c = ws.Cell(row, col);
        c.SetValue(text);
        c.Style.Font.Bold = true;
        c.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
        c.Style.Alignment.WrapText = true;
        c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    // Names/titles come from the API: always store them as plain text, never as formulas.
    private static void Text(IXLWorksheet ws, int row, int col, string text) =>
        ws.Cell(row, col).SetValue(text);
}
