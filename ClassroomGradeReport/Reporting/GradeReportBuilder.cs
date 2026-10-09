using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Reporting;

/// <summary>Pure report calculation: no I/O, no API types.</summary>
public sealed class GradeReportBuilder(IGradingPolicy? policy = null)
{
    private static readonly HashSet<string> IgnoredStates = new(StringComparer.OrdinalIgnoreCase) { "DRAFT", "DELETED" };
    private readonly IGradingPolicy _policy = policy ?? new UnweightedGradingPolicy();

    public GradeReport Build(ClassroomData data, ReportOptions options)
    {
        var warnings = new List<string>();
        var assignments = new List<Assignment>();

        foreach (var a in data.Assignments)
        {
            if (IgnoredStates.Contains(a.State)) continue;
            if (a.MaxPoints is not > 0)
            {
                warnings.Add($"Atividade \"{a.Title}\" ignorada: sem pontuação máxima (maxPoints).");
                continue;
            }
            assignments.Add(a);
        }

        var columns = assignments.Select(a => new ReportColumn(a.Id, a.Title, a.MaxPoints!.Value)).ToList();

        // Index submissions; submissions from users no longer enrolled are dropped by the student loop.
        var submissions = data.Submissions
            .GroupBy(s => (s.UserId, s.CourseWorkId))
            .ToDictionary(g => g.Key, g => g.First());

        var rows = data.Students
            .OrderBy(s => s.FullName, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), ignoreCase: true))
            .Select(student => BuildRow(student, assignments, submissions, options))
            .ToList();

        return new GradeReport(data.Course.Name, options, columns, rows, warnings);
    }

    private StudentRow BuildRow(
        Student student,
        List<Assignment> assignments,
        Dictionary<(string, string), Submission> submissions,
        ReportOptions options)
    {
        var cells = new List<GradeCell>(assignments.Count);
        double total = 0, possible = 0;

        foreach (var a in assignments)
        {
            var weight = _policy.WeightFor(a);
            submissions.TryGetValue((student.UserId, a.Id), out var sub);
            var grade = sub is null ? null : sub.AssignedGrade ?? (options.IncludeDrafts ? sub.DraftGrade : null);

            if (grade is null && options.MissingAsZero) grade = 0;
            cells.Add(new GradeCell(grade * weight, weight));

            if (grade is not null)
            {
                total += grade.Value * weight;
                possible += a.MaxPoints!.Value * weight;
            }
        }

        double? pct = possible > 0 ? total / possible * 100.0 : null;
        return new StudentRow(student, cells, total, possible, pct);
    }
}
