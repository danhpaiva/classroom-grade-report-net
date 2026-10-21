using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Tests;

public class ExportAndSelectorTests
{
    private static readonly Course Math = new("123", "Matemática", null, "ACTIVE");

    private static GradeReport Report(string? examTitle = null) =>
        new("Matemática", new ReportOptions(), [], [], [], examTitle);

    private static Export CsvExport(string location) =>
        new("CSV", "--csv", ".csv", location, (_, _, _) => Task.CompletedTask);

    [Fact]
    public void AllCourses_GroupsByCourse_WithGeralAndProvasFolders()
    {
        var export = CsvExport("out");
        var taken = new HashSet<string>();

        var general = export.TargetFor(Report(), Math, allCourses: true, taken);
        var exam = export.TargetFor(Report("Prova - A1"), Math, allCourses: true, taken);

        Assert.Equal(Path.Combine("out", "Matemática-123", "Geral", "Matemática-123.csv"), general);
        Assert.Equal(Path.Combine("out", "Matemática-123", "Provas", "Prova - A1.csv"), exam);
    }

    [Fact]
    public void SingleCourse_UsesTheGivenFileNameAsFolder()
    {
        var export = CsvExport(Path.Combine("dir", "notas.csv"));
        var taken = new HashSet<string>();

        var general = export.TargetFor(Report(), Math, allCourses: false, taken);
        var exam = export.TargetFor(Report("PROVA A2"), Math, allCourses: false, taken);

        var root = Path.Combine(Path.GetFullPath("dir"), "notas");
        Assert.Equal(Path.Combine(root, "Geral", "notas.csv"), general);
        Assert.Equal(Path.Combine(root, "Provas", "PROVA A2.csv"), exam);
    }

    [Fact]
    public void ExamsWithTheSameTitle_DoNotOverwriteEachOther()
    {
        var export = CsvExport("out");
        var taken = new HashSet<string>();

        var first = export.TargetFor(Report("Prova"), Math, true, taken);
        var second = export.TargetFor(Report("PROVA"), Math, true, taken);

        Assert.NotEqual(first, second);
        Assert.EndsWith("PROVA (2).csv", second);
    }

    [Fact]
    public void ExamTitle_WithInvalidFileNameCharacters_IsSanitized()
    {
        var path = CsvExport("out").TargetFor(Report("Prova: A1/B2"), Math, true, new HashSet<string>());
        Assert.Equal("Prova_ A1_B2.csv", Path.GetFileName(path));
    }

    [Fact]
    public void EnsureFolders_RejectsFilePaths()
    {
        var ex = Assert.Throws<UserFacingException>(() => Export.EnsureFolders([CsvExport("notas.csv")]));
        Assert.Contains("--csv", ex.Message);
        Export.EnsureFolders([CsvExport("pasta")]); // does not throw
    }

    [Fact]
    public void FromOptions_CreatesOneExportPerRequestedFormat()
    {
        var exports = Export.FromOptions(CliOptions.Parse(["--csv", "a", "--html", "b"]));
        Assert.Equal(["--csv", "--html"], exports.Select(e => e.Option));
    }

    [Fact]
    public void Find_PrefersExactId_ThenUniqueNameMatch()
    {
        Course[] courses = [Math, new("9", "Física", null, "ACTIVE")];
        Assert.Equal("123", CourseSelector.Find(courses, "123").Id);
        Assert.Equal("9", CourseSelector.Find(courses, "fís").Id);
    }

    [Fact]
    public void Find_FailsWhenNoneOrManyMatch()
    {
        Course[] courses = [Math, new("2", "Matemática B", null, "ACTIVE")];
        Assert.Throws<UserFacingException>(() => CourseSelector.Find(courses, "inexistente"));
        var ex = Assert.Throws<UserFacingException>(() => CourseSelector.Find(courses, "matem"));
        Assert.Contains("Use o ID", ex.Message);
    }

    [Theory]
    [InlineData("--all-courses", "--course", "x")]
    [InlineData("--exams-only", "--no-exams")]
    public void Validate_RejectsContradictoryOptions(params string[] args) =>
        Assert.Throws<UserFacingException>(() => CliOptions.Parse(args).Validate());
}
