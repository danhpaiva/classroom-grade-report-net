using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Reporting;

public record ReportOptions(bool IncludeDrafts = false, bool MissingAsZero = false);

public record ReportColumn(string AssignmentId, string Title, double MaxPoints);

/// <summary>One cell of the report. <see cref="Grade"/> is null when the student has no grade.</summary>
public record GradeCell(double? Grade, double Weight);

public record StudentRow(
    Student Student,
    IReadOnlyList<GradeCell> Cells,
    double Total,
    double TotalPossible,
    double? Percentage);

public record GradeReport(
    string CourseName,
    ReportOptions Options,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<StudentRow> Rows,
    IReadOnlyList<string> Warnings);
