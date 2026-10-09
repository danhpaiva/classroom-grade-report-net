using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using ClosedXML.Excel;

namespace ClassroomGradeReport.Tests;

public class XlsxReportWriterTests
{
    private static XLWorkbook RoundTrip(string name = "Ana")
    {
        var report = new GradeReportBuilder().Build(new ClassroomData(
            new Course("c", "Turma", null, "ACTIVE"),
            [new Student("u", name, "ana@x.com")],
            [new Assignment("a", "Prova", 10, "PUBLISHED"), new Assignment("b", "Trab", 5, "PUBLISHED")],
            [new Submission("a", "u", 7.5, null)]), new ReportOptions());
        var ms = new MemoryStream();
        XlsxReportWriter.Write(report, ms);
        ms.Position = 0;
        return new XLWorkbook(ms);
    }

    [Fact]
    public void Xlsx_HasNumericCellsAndMissingDash()
    {
        using var wb = RoundTrip();
        var ws = wb.Worksheet("Notas");
        Assert.Equal("Prova (/10)", ws.Cell(1, 3).GetString());
        Assert.Equal(XLDataType.Number, ws.Cell(2, 3).DataType);
        Assert.Equal(7.5, ws.Cell(2, 3).GetDouble());
        Assert.Equal("-", ws.Cell(2, 4).GetString());
        Assert.Equal(7.5, ws.Cell(2, 5).GetDouble());     // Total
        Assert.Equal(10, ws.Cell(2, 6).GetDouble());      // Total possível
        Assert.Equal(0.75, ws.Cell(2, 7).GetDouble(), 6); // Percentual as a fraction, formatted 0.0%
    }

    [Fact]
    public void Xlsx_StoresHostileTextAsPlainText_NotFormula()
    {
        using var wb = RoundTrip("=CMD(1)");
        var cell = wb.Worksheet("Notas").Cell(2, 1);
        Assert.False(cell.HasFormula);
        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.Equal("=CMD(1)", cell.GetString());
    }

    [Fact]
    public void Xlsx_HasCriteriaSheet()
    {
        using var wb = RoundTrip();
        Assert.Equal("Turma", wb.Worksheet("Critério").Cell(1, 2).GetString());
    }
}
