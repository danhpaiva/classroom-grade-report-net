using System.Globalization;
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
    private static readonly CompareInfo Compare = new CultureInfo("pt-BR").CompareInfo;

    /// <summary>True when <paramref name="title"/> contains the keyword.</summary>
    public bool IsExam(string title) =>
        Keyword.Length > 0 &&
        Compare.IndexOf(title, Keyword, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
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

/// <summary>A finished report. <c>ExamTitle</c> is set for a per-exam report, null for the general one.</summary>
public record GradeReport(
    string CourseName,
    ReportOptions Options,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<StudentRow> Rows,
    IReadOnlyList<string> Warnings,
    string? ExamTitle = null)
{
    public bool IsExam => ExamTitle is not null;
}
