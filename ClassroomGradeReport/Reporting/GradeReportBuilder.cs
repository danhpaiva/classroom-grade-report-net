using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Reporting;

/// <summary>Pure report calculation: no I/O, no API types.</summary>
public sealed class GradeReportBuilder(IGradingPolicy? policy = null)
{
    private static readonly HashSet<string> IgnoredStates = new(StringComparer.OrdinalIgnoreCase) { "DRAFT", "DELETED" };
    private readonly IGradingPolicy _policy = policy ?? new UnweightedGradingPolicy();

    /// <summary>Single report with every valid assignment (no exam split).</summary>
    public GradeReport Build(ClassroomData data, ReportOptions options)
    {
        var warnings = new List<string>();
        return BuildReport(data, data.Course.Name, ValidAssignments(data, warnings), options, warnings, null);
    }

    /// <summary>
    /// One general report (non-exam assignments) plus one report per exam, so exams are never summed
    /// with other activities nor with each other. Warnings are attached to the first report returned.
    /// </summary>
    public IReadOnlyList<GradeReport> BuildAll(ClassroomData data, ReportOptions options, ExamSplit split)
    {
        var warnings = new List<string>();
        var valid = ValidAssignments(data, warnings);
        var exams = valid.Where(a => split.IsExam(a.Title)).ToList();
        var general = valid.Except(exams).ToList();

        var reports = new List<GradeReport>();
        if (split.IncludeGeneral && (general.Count > 0 || exams.Count == 0 || !split.IncludeExams))
            reports.Add(BuildReport(data, data.Course.Name, general, options, [], null));
        if (split.IncludeExams)
            reports.AddRange(exams.Select(e =>
                BuildReport(data, $"{data.Course.Name} — {e.Title}", [e], options, [], e.Title)));

        if (reports.Count > 0 && warnings.Count > 0)
            reports[0] = reports[0] with { Warnings = warnings };
        return reports;
    }

    /// <summary>Drops DRAFT/DELETED assignments silently and ungraded ones with a warning.</summary>
    private static List<Assignment> ValidAssignments(ClassroomData data, List<string> warnings)
    {
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
        return assignments;
    }

    /// <summary>Builds the rows (one per student) for <paramref name="assignments"/> under the given report title.</summary>
    private GradeReport BuildReport(
        ClassroomData data, string title, List<Assignment> assignments, ReportOptions options,
        List<string> warnings, string? examTitle)
    {
        var columns = assignments.Select(a => new ReportColumn(a.Id, a.Title, a.MaxPoints!.Value)).ToList();

        // Index submissions; submissions from users no longer enrolled are dropped by the student loop.
        var submissions = data.Submissions
            .GroupBy(s => (s.UserId, s.CourseWorkId))
            .ToDictionary(g => g.Key, g => g.First());

        var rows = data.Students
            .OrderBy(s => s.FullName, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), ignoreCase: true))
            .Select(student => BuildRow(student, assignments, submissions, options))
            .ToList();

        return new GradeReport(title, options, columns, rows, warnings, examTitle);
    }

    /// <summary>Computes one student's cells and totals; missing grades are not summed unless MissingAsZero is set.</summary>
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
