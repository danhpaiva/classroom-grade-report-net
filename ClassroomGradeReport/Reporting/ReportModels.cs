using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Reporting;

/// <summary>How grades are computed: whether drafts count and whether missing grades are zero.</summary>
public record ReportOptions(bool IncludeDrafts = false, bool MissingAsZero = false);

/// <summary>
/// How exams are told apart from regular assignments: any title containing <see cref="Keyword"/>
/// (case- and accent-insensitive) is an exam. Each exam gets its own report and is never summed with others.
/// </summary>
public record ExamSplit(string Keyword = "PROVA", bool IncludeGeneral = true, bool IncludeExams = true)
{
    /// <summary>True when <paramref name="title"/> contains the keyword.</summary>
    public bool IsExam(string title) => TitleMatcher.Contains(title, Keyword);
}

/// <summary>A report column: one assignment and its maximum points.</summary>
public record ReportColumn(string AssignmentId, string Title, double MaxPoints);

/// <summary>One cell of the report. <see cref="Grade"/> is null when the student has no grade.</summary>
public record GradeCell(double? Grade, double Weight);

/// <summary>A student's line: cells (one per column), totals and percentage (null when nothing was graded).</summary>
public record StudentRow(
    Student Student,
    IReadOnlyList<GradeCell> Cells,
    double Total,
    double TotalPossible,
    double? Percentage);

/// <summary>Class statistics of one exam, over the students who have a grade.</summary>
/// <param name="Graded">Students with a grade.</param>
/// <param name="Total">Students in the class.</param>
/// <param name="Average">Mean grade of the graded students.</param>
/// <param name="Highest">Highest grade.</param>
/// <param name="Lowest">Lowest grade.</param>
/// <param name="BelowAverage">Graded students whose grade is below <paramref name="Average"/>.</param>
public record ExamStats(int Graded, int Total, double Average, double Highest, double Lowest, int BelowAverage);

/// <summary>A finished report. <c>ExamTitle</c> is set for a per-exam report, null for the general one.</summary>
public record GradeReport(
    string CourseName,
    ReportOptions Options,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<StudentRow> Rows,
    IReadOnlyList<string> Warnings,
    string? ExamTitle = null,
    ExamStats? Stats = null)
{
    public bool IsExam => ExamTitle is not null;
}
