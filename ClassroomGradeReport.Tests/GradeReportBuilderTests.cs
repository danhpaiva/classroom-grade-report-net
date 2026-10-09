using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Tests;

public class GradeReportBuilderTests
{
    private static readonly Course Course = new("c1", "Turma A", null, "ACTIVE");
    private static Student S(string id, string name) => new(id, name, null);
    private static Assignment A(string id, double? max, string state = "PUBLISHED") => new(id, $"Ativ {id}", max, state);
    private static Submission Sub(string cw, string user, double? assigned, double? draft = null) => new(cw, user, assigned, draft);

    private static GradeReport Build(
        IEnumerable<Student> students, IEnumerable<Assignment> assignments, IEnumerable<Submission> subs,
        ReportOptions? options = null) =>
        new GradeReportBuilder().Build(
            new ClassroomData(Course, students.ToList(), assignments.ToList(), subs.ToList()),
            options ?? new ReportOptions());

    [Fact]
    public void MissingGrade_IsNotZero_AndIsNotSummed()
    {
        var r = Build([S("u1", "Ana")], [A("a1", 10), A("a2", 10)], [Sub("a1", "u1", 8)]);
        var row = r.Rows.Single();
        Assert.Equal(8, row.Cells[0].Grade);
        Assert.Null(row.Cells[1].Grade);
        Assert.Equal(8, row.Total);
        Assert.Equal(10, row.TotalPossible);
        Assert.Equal(80, row.Percentage);
    }

    [Fact]
    public void ExplicitZero_IsCounted()
    {
        var row = Build([S("u1", "Ana")], [A("a1", 10), A("a2", 10)],
            [Sub("a1", "u1", 0), Sub("a2", "u1", 10)]).Rows.Single();
        Assert.Equal(0, row.Cells[0].Grade);
        Assert.Equal(20, row.TotalPossible);
        Assert.Equal(50, row.Percentage);
    }

    [Fact]
    public void MissingAsZero_CountsMissingAndNoSubmissionAsZero()
    {
        var row = Build([S("u1", "Ana")], [A("a1", 10), A("a2", 10)], [Sub("a1", "u1", 10)],
            new ReportOptions(MissingAsZero: true)).Rows.Single();
        Assert.Equal(0, row.Cells[1].Grade);
        Assert.Equal(20, row.TotalPossible);
        Assert.Equal(50, row.Percentage);
    }

    [Fact]
    public void DraftGrade_IgnoredByDefault()
    {
        var row = Build([S("u1", "Ana")], [A("a1", 10)], [Sub("a1", "u1", null, 7)]).Rows.Single();
        Assert.Null(row.Cells[0].Grade);
        Assert.Equal(0, row.TotalPossible);
        Assert.Null(row.Percentage);
    }

    [Fact]
    public void DraftGrade_UsedWhenIncluded_ButAssignedWins()
    {
        var r = Build([S("u1", "Ana")], [A("a1", 10), A("a2", 10)],
            [Sub("a1", "u1", null, 7), Sub("a2", "u1", 9, 3)], new ReportOptions(IncludeDrafts: true));
        var row = r.Rows.Single();
        Assert.Equal(7, row.Cells[0].Grade);
        Assert.Equal(9, row.Cells[1].Grade);
        Assert.Equal(16, row.Total);
    }

    [Fact]
    public void Assignments_WithoutMaxPoints_DraftOrDeleted_AreIgnored()
    {
        var r = Build([S("u1", "Ana")],
            [A("a1", 10), A("a2", null), A("a3", 0), A("a4", 10, "DRAFT"), A("a5", 10, "DELETED")],
            [Sub("a1", "u1", 5), Sub("a2", "u1", 5), Sub("a4", "u1", 5)]);
        Assert.Equal(["a1"], r.Columns.Select(c => c.AssignmentId));
        Assert.Equal(2, r.Warnings.Count); // a2 and a3 only; DRAFT/DELETED are silent
        Assert.Equal(5, r.Rows.Single().Total);
    }

    [Fact]
    public void Submissions_FromRemovedStudents_AreIgnored()
    {
        var r = Build([S("u1", "Ana")], [A("a1", 10)], [Sub("a1", "u1", 10), Sub("a1", "gone", 3)]);
        Assert.Single(r.Rows);
        Assert.Equal("u1", r.Rows[0].Student.UserId);
    }

    [Fact]
    public void Students_AreSortedAlphabetically_IgnoringCaseAndAccents()
    {
        var r = Build([S("1", "Zeca"), S("2", "ana"), S("3", "Álvaro"), S("4", "Bruno")], [A("a1", 10)], []);
        Assert.Equal(["Álvaro", "ana", "Bruno", "Zeca"], r.Rows.Select(x => x.Student.FullName));
    }

    [Fact]
    public void Percentage_IsNull_WhenNothingPossible()
    {
        var row = Build([S("u1", "Ana")], [], []).Rows.Single();
        Assert.Null(row.Percentage);
    }

    [Fact]
    public void Policy_Weights_ScaleGradeAndPossible()
    {
        var builder = new GradeReportBuilder(new DoublePolicy());
        var row = builder.Build(
            new ClassroomData(Course, [S("u1", "Ana")], [A("a1", 10)], [Sub("a1", "u1", 5)]),
            new ReportOptions()).Rows.Single();
        Assert.Equal(10, row.Total);
        Assert.Equal(20, row.TotalPossible);
        Assert.Equal(50, row.Percentage);
    }

    private sealed class DoublePolicy : IGradingPolicy
    {
        public double WeightFor(Assignment assignment) => 2.0;
    }
}
