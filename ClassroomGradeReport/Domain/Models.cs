namespace ClassroomGradeReport.Domain;

public record Course(string Id, string Name, string? Section, string State);

public record Student(string UserId, string FullName, string? Email);

public record Assignment(string Id, string Title, double? MaxPoints, string State);

/// <summary>A student's submission. Grades are null when absent (which is NOT the same as zero).</summary>
public record Submission(string CourseWorkId, string UserId, double? AssignedGrade, double? DraftGrade);

public record ClassroomData(
    Course Course,
    IReadOnlyList<Student> Students,
    IReadOnlyList<Assignment> Assignments,
    IReadOnlyList<Submission> Submissions);
