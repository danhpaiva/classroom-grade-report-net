using ClassroomGradeReport.Api;
using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;
using Google;

namespace ClassroomGradeReport.Tests;

public class ConfigAndStatsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cgr-" + Guid.NewGuid().ToString("N"));

    public ConfigAndStatsTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteConfig(string json)
    {
        var path = Path.Combine(_dir, "config.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Config_ProvidesDefaults_AndCommandLineOverrides()
    {
        var config = AppConfig.Load(WriteConfig("""
            // comments and trailing commas are allowed
            { "examKeyword": "Bimestral", "csv": "from-config", "includeDrafts": true, }
            """));

        var defaults = CliOptions.Parse(["--all-courses"], config.ToDefaults());
        Assert.Equal("Bimestral", defaults.ExamKeyword);
        Assert.Equal("from-config", defaults.CsvPath);
        Assert.True(defaults.IncludeDrafts);

        var overridden = CliOptions.Parse(["--csv", "from-cli", "--exam-keyword", "Prova"], config.ToDefaults());
        Assert.Equal("from-cli", overridden.CsvPath);
        Assert.Equal("Prova", overridden.ExamKeyword);
    }

    [Fact]
    public void Config_Missing_IsFineByDefault_ButAnErrorWhenExplicit()
    {
        Assert.Equal("PROVA", new AppConfig().ToDefaults().ExamKeyword);
        Assert.Throws<UserFacingException>(() => AppConfig.Load(Path.Combine(_dir, "nao-existe.json")));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "weights": [ { "keyword": "Trabalho", "weight": 0 } ] }""")]
    [InlineData("""{ "weights": [ { "keyword": "", "weight": 2 } ] }""")]
    public void Config_Invalid_ProducesFriendlyError(string json) =>
        Assert.Throws<UserFacingException>(() => AppConfig.Load(WriteConfig(json)));

    [Fact]
    public void Weights_ScaleMatchingAssignments_FirstRuleWins()
    {
        var policy = AppConfig.Load(WriteConfig("""
            { "weights": [ { "keyword": "trabalho final", "weight": 3 }, { "keyword": "Trabalho", "weight": 2 } ] }
            """)).ToPolicy();

        Assert.Equal(3, policy.WeightFor(new Assignment("1", "TRABALHO FINAL", 10, "PUBLISHED")));
        Assert.Equal(2, policy.WeightFor(new Assignment("2", "Trabalho 1", 10, "PUBLISHED")));
        Assert.Equal(1, policy.WeightFor(new Assignment("3", "Atividade", 10, "PUBLISHED")));
        Assert.IsType<UnweightedGradingPolicy>(new AppConfig().ToPolicy());
    }

    private static GradeReport ExamWith(params double?[] grades)
    {
        var students = grades.Select((_, i) => new Student($"u{i}", $"Aluno {i}", null)).ToList();
        var subs = grades.Select((g, i) => new Submission("p1", $"u{i}", g, null)).ToList();
        return new GradeReportBuilder().BuildAll(
            new ClassroomData(new Course("c", "T", null, "ACTIVE"), students, [new Assignment("p1", "Prova", 10, "PUBLISHED")], subs),
            new ReportOptions(), new ExamSplit(IncludeGeneral: false)).Single();
    }

    [Fact]
    public void ExamStats_AreComputedOverGradedStudents()
    {
        var stats = ExamWith(10, 4, 7, null).Stats!;

        Assert.Equal(3, stats.Graded);
        Assert.Equal(4, stats.Total);
        Assert.Equal(7, stats.Average);
        Assert.Equal(10, stats.Highest);
        Assert.Equal(4, stats.Lowest);
        Assert.Equal(1, stats.BelowAverage); // only the 4; the 7 equals the average
    }

    [Fact]
    public void ExamStats_AreAbsent_WhenNobodyHasAGrade()
    {
        Assert.Null(ExamWith(null, null).Stats);
    }

    [Fact]
    public void ExamStats_AppearInTxtAndHtml_ButGeneralReportsHaveNone()
    {
        var exam = ExamWith(10, 4);
        Assert.Contains("Média da turma: 7", TxtReportWriter.Render(exam));
        Assert.Contains("Maior nota: 10", HtmlReportWriter.Render(exam));

        var general = new GradeReportBuilder().Build(
            new ClassroomData(new Course("c", "T", null, "ACTIVE"), [], [new Assignment("a", "Atividade", 10, "PUBLISHED")], []),
            new ReportOptions());
        Assert.Null(general.Stats);
    }

    [Fact]
    public void RetryNotifier_CapturesNotices_ThenFallsBackWhenDisposed()
    {
        var notifier = new RetryNotifier();
        List<string> seen = [];

        using (notifier.Capture(seen.Add)) notifier.Notify("um");

        Assert.Equal(["um"], seen);
        var stderr = Console.Error;
        var sink = new StringWriter();
        Console.SetError(sink);
        try { notifier.Notify("dois"); }
        finally { Console.SetError(stderr); }
        Assert.Contains("dois", sink.ToString());
        Assert.Equal(["um"], seen);
    }

    [Fact]
    public async Task ApiRetry_ExpiredAuthorization_BecomesAFriendlyError()
    {
        var expired = new Google.Apis.Auth.OAuth2.Responses.TokenResponseException(
            new Google.Apis.Auth.OAuth2.Responses.TokenErrorResponse { Error = "invalid_grant" });

        var ex = await Assert.ThrowsAsync<UserFacingException>(() =>
            ApiRetry.ExecuteAsync<int>(() => throw expired, CancellationToken.None));

        Assert.Contains("--logout", ex.Message);
    }

    [Fact]
    public async Task ApiRetry_AnnouncesEachRetry_ThroughTheCallback()
    {
        var attempts = 0;
        List<string> notices = [];

        var result = await ApiRetry.ExecuteAsync(
            () => ++attempts < 3
                ? throw new GoogleApiException("svc", "down") { HttpStatusCode = System.Net.HttpStatusCode.ServiceUnavailable }
                : Task.FromResult(42),
            CancellationToken.None, baseDelay: TimeSpan.Zero, onRetry: notices.Add);

        Assert.Equal(42, result);
        Assert.Equal(2, notices.Count);
        Assert.All(notices, n => Assert.Contains("503", n));
    }
}
