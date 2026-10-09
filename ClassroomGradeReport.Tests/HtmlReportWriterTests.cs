using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Tests;

public class HtmlReportWriterTests
{
    private static GradeReport Sample(string name = "Ana", string title = "Prova") =>
        new GradeReportBuilder().Build(new ClassroomData(
            new Course("c", "Turma <A>", null, "ACTIVE"),
            [new Student("1", name, "ana@x.com")],
            [new Assignment("a", title, 10, "PUBLISHED"), new Assignment("b", "Trab", 5, "PUBLISHED")],
            [new Submission("a", "1", 7.5, null)]), new ReportOptions());

    [Fact]
    public void Html_ContainsTableValuesAndMissingMarker()
    {
        var html = HtmlReportWriter.Render(Sample());
        Assert.Contains("<html lang=\"pt-BR\">", html);
        Assert.Contains("<td class=\"num\">7,5</td>", html);
        Assert.Contains("<td class=\"num missing\">-</td>", html);
        Assert.Contains("75,0%", html);
    }

    [Fact]
    public void Html_EncodesUserControlledText()
    {
        var html = HtmlReportWriter.Render(Sample(name: "<script>alert(1)</script>", title: "<img src=x onerror=1>"));
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("Turma &lt;A&gt;", html);
    }

    [Fact]
    public void Html_HasRestrictiveContentSecurityPolicy() =>
        Assert.Contains("default-src 'none'", HtmlReportWriter.Render(Sample()));

    [Fact]
    public void Cli_ParsesHtml() => Assert.Equal("a.html", CliOptions.Parse(["--html", "a.html"]).HtmlPath);
}
