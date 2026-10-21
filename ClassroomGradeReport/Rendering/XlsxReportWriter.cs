using ClassroomGradeReport.Reporting;
using ClosedXML.Excel;

namespace ClassroomGradeReport.Rendering;

/// <summary>Excel (.xlsx) export: real numeric cells, formatted header, frozen panes.</summary>
public static class XlsxReportWriter
{
    private const int NameCol = 1;
    private const int EmailCol = 2;
    private const int FirstGradeCol = 3;

    /// <summary>Builds the workbook (a "Notas" sheet and a "Critério" sheet) and saves it to <paramref name="output"/>.</summary>
    public static void Write(GradeReport report, Stream output)
    {
        using var wb = new XLWorkbook();
        WriteGradesSheet(wb.AddWorksheet("Notas"), report);
        WriteCriteriaSheet(wb.AddWorksheet("Critério"), report);
        wb.SaveAs(output);
    }

    /// <summary>Builds the workbook and saves it to <paramref name="path"/> (folders created as needed).</summary>
    public static async Task WriteAsync(GradeReport report, string path, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        Write(report, ms);
        await ReportFile.WriteBytesAsync(path, ms.ToArray(), ct);
    }

    /// <summary>Header, one line per student, number formats, frozen panes and column widths.</summary>
    private static void WriteGradesSheet(IXLWorksheet ws, GradeReport report)
    {
        var totalCol = FirstGradeCol + report.Columns.Count;
        var lastCol = totalCol + 2;
        WriteHeader(ws, report, totalCol);

        for (var r = 0; r < report.Rows.Count; r++)
            WriteStudentRow(ws, report.Rows[r], excelRow: r + 2, totalCol);

        ws.Range(2, FirstGradeCol, Math.Max(2, report.Rows.Count + 1), lastCol - 1).Style.NumberFormat.Format = "0.##";
        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(EmailCol);
        ws.Columns().AdjustToContents(1, Math.Max(1, report.Rows.Count + 1), 8, 40);
    }

    private static void WriteHeader(IXLWorksheet ws, GradeReport report, int totalCol)
    {
        Header(ws, 1, NameCol, "Aluno");
        Header(ws, 1, EmailCol, "E-mail");
        for (var i = 0; i < report.Columns.Count; i++)
        {
            var c = report.Columns[i];
            Header(ws, 1, FirstGradeCol + i, $"{c.Title} (/{ReportFormatting.Number(c.MaxPoints)})");
        }
        Header(ws, 1, totalCol, "Total");
        Header(ws, 1, totalCol + 1, "Total possível");
        Header(ws, 1, totalCol + 2, "Percentual");
    }

    private static void WriteStudentRow(IXLWorksheet ws, StudentRow row, int excelRow, int totalCol)
    {
        Text(ws, excelRow, NameCol, row.Student.FullName);
        Text(ws, excelRow, EmailCol, row.Student.Email ?? "");

        for (var i = 0; i < row.Cells.Count; i++)
            WriteGrade(ws.Cell(excelRow, FirstGradeCol + i), row.Cells[i].Grade);

        ws.Cell(excelRow, totalCol).SetValue(row.Total);
        ws.Cell(excelRow, totalCol + 1).SetValue(row.TotalPossible);
        WritePercentage(ws.Cell(excelRow, totalCol + 2), row.Percentage);
    }

    /// <summary>A grade is a numeric cell; a missing grade is "-" (never 0).</summary>
    private static void WriteGrade(IXLCell cell, double? grade)
    {
        if (grade is { } g) cell.SetValue(g);
        else cell.SetValue("-");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    /// <summary>The percentage is stored as a fraction formatted "0.0%"; absent means "-".</summary>
    private static void WritePercentage(IXLCell cell, double? percentage)
    {
        if (percentage is { } p)
        {
            cell.SetValue(p / 100.0);
            cell.Style.NumberFormat.Format = "0.0%";
        }
        else
        {
            cell.SetValue("-");
        }
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    /// <summary>Second sheet: how the numbers were computed (course, criteria, warnings).</summary>
    private static void WriteCriteriaSheet(IXLWorksheet info, GradeReport report)
    {
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
