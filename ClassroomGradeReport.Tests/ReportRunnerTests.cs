using ClassroomGradeReport.Api;
using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Reporting;
using Spectre.Console;

namespace ClassroomGradeReport.Tests;

public class ReportRunnerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cgr-" + Guid.NewGuid().ToString("N"));
    private readonly StringWriter _output = new();
    private readonly IAnsiConsole _console;

    public ReportRunnerTests()
    {
        _console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_output) });
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static Course C(string id, string name) => new(id, name, null, "ACTIVE");

    private static ClassroomData DataFor(Course course) => new(
        course,
        [new Student("u1", "Ana", null), new Student("u2", "Bia", null)],
        [new Assignment("a1", "Atividade", 10, "PUBLISHED"), new Assignment("p1", "Prova A1", 10, "PUBLISHED")],
        [new Submission("a1", "u1", 8, null), new Submission("p1", "u1", 4, null), new Submission("p1", "u2", 10, null)]);

    private ReportRunner Runner(FakeGateway gateway) =>
        new(gateway, new GradeReportBuilder(), _console, new RetryNotifier());

    private static CliOptions Options(params string[] args) => CliOptions.Parse(args);

    [Fact]
    public async Task OneCourse_WritesGeneralAndExamFiles_InSeparateFolders()
    {
        var gateway = new FakeGateway([C("1", "Turma A")]);
        var csv = Path.Combine(_dir, "notas.csv");

        await Runner(gateway).RunAsync(Options("--course", "Turma", "--csv", csv), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_dir, "notas", "Geral", "notas.csv")));
        Assert.True(File.Exists(Path.Combine(_dir, "notas", "Provas", "Prova A1.csv")));
        var general = await File.ReadAllTextAsync(Path.Combine(_dir, "notas", "Geral", "notas.csv"));
        Assert.DoesNotContain("Prova A1", general);
    }

    [Fact]
    public async Task AllCourses_WritesOneFolderPerCourse()
    {
        var gateway = new FakeGateway([C("1", "Turma A"), C("2", "Turma B")]);

        await Runner(gateway).RunAsync(Options("--all-courses", "--txt", _dir), CancellationToken.None);

        foreach (var name in new[] { "Turma A-1", "Turma B-2" })
        {
            Assert.True(File.Exists(Path.Combine(_dir, name, "Geral", name + ".txt")));
            Assert.True(File.Exists(Path.Combine(_dir, name, "Provas", "Prova A1.txt")));
        }
    }

    [Fact]
    public async Task ExamsOnly_SkipsGeneralReport()
    {
        var gateway = new FakeGateway([C("1", "Turma A")]);

        await Runner(gateway).RunAsync(Options("--course", "1", "--exams-only", "--csv", Path.Combine(_dir, "n.csv")), CancellationToken.None);

        Assert.False(Directory.Exists(Path.Combine(_dir, "n", "Geral")));
        Assert.True(File.Exists(Path.Combine(_dir, "n", "Provas", "Prova A1.csv")));
    }

    [Fact]
    public async Task NoCourses_FailsWithHint()
    {
        var ex = await Assert.ThrowsAsync<UserFacingException>(() =>
            Runner(new FakeGateway([])).RunAsync(Options("--all-courses"), CancellationToken.None));
        Assert.Contains("--include-inactive", ex.Message);
    }

    [Fact]
    public async Task FolderOption_PointingToFile_IsRejectedBeforeCallingTheApi()
    {
        var gateway = new FakeGateway([C("1", "Turma A")]);

        await Assert.ThrowsAsync<UserFacingException>(() =>
            Runner(gateway).RunAsync(Options("--all-courses", "--csv", "x.csv"), CancellationToken.None));

        Assert.Equal(0, gateway.ListCalls);
    }

    [Fact]
    public async Task FirstFailure_CancelsOtherLoads_AndIsTheErrorReported()
    {
        var slowStarted = new TaskCompletionSource();
        var gateway = new FakeGateway([C("1", "Falha"), C("2", "Lenta")])
        {
            Loader = async (course, ct) =>
            {
                if (course.Id == "1")
                {
                    await slowStarted.Task; // fail only once the slow load is really running
                    throw new UserFacingException("boom");
                }
                slowStarted.SetResult();
                await Task.Delay(TimeSpan.FromSeconds(30), ct); // only ends when cancelled
                return DataFor(course);
            },
        };

        var ex = await Assert.ThrowsAsync<UserFacingException>(() =>
            Runner(gateway).RunAsync(Options("--all-courses"), CancellationToken.None));

        Assert.Equal("boom", ex.Message);
        Assert.True(gateway.Cancelled.Contains("2"), "the slow load should have been cancelled, not left running");
    }

    [Fact]
    public async Task CallerCancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        var gateway = new FakeGateway([C("1", "Turma")])
        {
            Loader = async (_, ct) =>
            {
                await cts.CancelAsync();
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return null!;
            },
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Runner(gateway).RunAsync(Options("--all-courses"), cts.Token));
    }

    [Fact]
    public async Task RetryNotices_AreHeldBackDuringLoad_AndPrintedAfterwards()
    {
        var notifier = new RetryNotifier();
        var gateway = new FakeGateway([C("1", "Turma")])
        {
            Loader = (course, _) =>
            {
                notifier.Notify("API indisponível (503). Nova tentativa em 1s...");
                return Task.FromResult(DataFor(course));
            },
        };
        var runner = new ReportRunner(gateway, new GradeReportBuilder(), _console, notifier);

        await runner.RunAsync(Options("--all-courses"), CancellationToken.None);

        Assert.Contains("Nova tentativa em 1s", _output.ToString());
    }

    private sealed class FakeGateway(IReadOnlyList<Course> courses) : IClassroomGateway
    {
        private readonly object _lock = new();
        public int ListCalls { get; private set; }
        public HashSet<string> Cancelled { get; } = [];
        public Func<Course, CancellationToken, Task<ClassroomData>>? Loader { get; init; }

        public Task<IReadOnlyList<Course>> ListTeacherCoursesAsync(bool activeOnly, CancellationToken ct)
        {
            ListCalls++;
            return Task.FromResult(courses);
        }

        public async Task<ClassroomData> LoadCourseDataAsync(Course course, CancellationToken ct)
        {
            try
            {
                return Loader is null ? DataFor(course) : await Loader(course, ct);
            }
            catch (OperationCanceledException)
            {
                lock (_lock) Cancelled.Add(course.Id);
                throw;
            }
        }
    }
}
