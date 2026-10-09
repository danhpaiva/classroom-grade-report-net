using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Tests;

public class TxtReportWriterTests
{
    private static GradeReport Sample() =>
        new GradeReportBuilder().Build(new ClassroomData(
            new Course("c", "Turma A", null, "ACTIVE"),
            [new Student("1", "Ana", null), new Student("2", "Bruno Lima", null)],
            [new Assignment("a", "Prova", 10, "PUBLISHED"), new Assignment("b", "Trab", 5, "PUBLISHED")],
            [new Submission("a", "1", 7.5, null), new Submission("a", "2", 9, null), new Submission("b", "2", 4, null)]),
            new ReportOptions());

    [Fact]
    public void Txt_HasHeaderCriteriaAndAlignedRows()
    {
        var lines = TxtReportWriter.Render(Sample()).Split(["\r\n", "\n"], StringSplitOptions.None);
        Assert.Equal("Turma: Turma A", lines[0]);
        Assert.StartsWith("Critério de nota:", lines[1]);

        var table = lines.Where(l => l.Contains(" | ")).ToList();
        Assert.Equal(3, table.Count); // header + 2 students
        Assert.Single(table.Select(l => l.Length).Distinct()); // aligned (same width)
        Assert.Contains("Ana        |         7,5 | ", table[1]);
        Assert.Contains("-", table[1]); // missing grade shown as dash
    }

    [Fact]
    public async Task Txt_File_HasUtf8Bom()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            await TxtReportWriter.WriteAsync(Sample(), path, CancellationToken.None);
            Assert.Equal(System.Text.Encoding.UTF8.GetPreamble(), File.ReadAllBytes(path).Take(3).ToArray());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Cli_ParsesTxt() => Assert.Equal("a.txt", ClassroomGradeReport.Cli.CliOptions.Parse(["--txt", "a.txt"]).TxtPath);
}
