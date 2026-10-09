using System.Text;
using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Tests;

public class CsvAndCliTests
{
    private static GradeReport Sample(string name = "Ana", string title = "Prova") =>
        new GradeReportBuilder().Build(new ClassroomData(
            new Course("c", "T", null, "ACTIVE"),
            [new Student("u", name, "ana@x.com")],
            [new Assignment("a", title, 10, "PUBLISHED"), new Assignment("b", "Trab", 5, "PUBLISHED")],
            [new Submission("a", "u", 7.5, null)]), new ReportOptions());

    [Fact]
    public void Csv_UsesSemicolonAndDecimalComma()
    {
        var lines = CsvReportWriter.Render(Sample()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Aluno;E-mail;Prova (/10);Trab (/5);Total;Total possível;Percentual", lines[0]);
        Assert.Equal("Ana;ana@x.com;7,5;-;7,5;10;75,0%", lines[1]);
    }

    [Fact]
    public void Csv_EscapesSeparatorsAndFormulas()
    {
        var csv = CsvReportWriter.Render(Sample(name: "=CMD(1)", title: "A;B"));
        Assert.Contains("\"A;B (/10)\"", csv);
        Assert.Contains("'=CMD(1);", csv);
    }

    [Fact]
    public async Task Csv_File_HasUtf8Bom()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        try
        {
            await CsvReportWriter.WriteAsync(Sample(), path, CancellationToken.None);
            Assert.Equal(Encoding.UTF8.GetPreamble(), File.ReadAllBytes(path).Take(3).ToArray());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Cli_ParsesAllOptions()
    {
        var o = CliOptions.Parse(["--course", "Mat", "--csv", "a.csv", "--include-drafts", "--missing-as-zero", "--include-inactive", "--credentials", "c.json"]);
        Assert.Equal("Mat", o.Course);
        Assert.Equal("a.csv", o.CsvPath);
        Assert.Equal("c.json", o.CredentialsPath);
        Assert.True(o.IncludeDrafts && o.MissingAsZero && o.IncludeInactiveCourses);
    }

    [Fact]
    public void Cli_ParsesAllCourses() => Assert.True(CliOptions.Parse(["--all-courses"]).AllCourses);

    [Fact]
    public void Csv_FileName_IsSanitizedAndIncludesId()
    {
        Assert.Equal("Mat_ 9_A-123.csv", CsvReportWriter.FileNameFor("Mat/ 9:A", "123").Replace(":", "_"));
        Assert.Equal("turma-5.csv", CsvReportWriter.FileNameFor("  ", "5"));
    }

    [Theory]
    [InlineData("--nope")]
    [InlineData("--csv")]
    public void Cli_RejectsBadInput(string arg) =>
        Assert.Throws<UserFacingException>(() => CliOptions.Parse([arg]));
}
