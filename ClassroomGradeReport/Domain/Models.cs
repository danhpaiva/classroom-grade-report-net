namespace ClassroomGradeReport.Domain;

/// <summary>A Classroom course. <paramref name="State"/> is the API course state (e.g. ACTIVE).</summary>
public record Course(string Id, string Name, string? Section, string State);

/// <summary>An enrolled student.</summary>
public record Student(string UserId, string FullName, string? Email);

/// <summary>A course work item. <paramref name="MaxPoints"/> is null when the assignment is ungraded.</summary>
public record Assignment(string Id, string Title, double? MaxPoints, string State);

/// <summary>A student's submission. Grades are null when absent (which is NOT the same as zero).</summary>
public record Submission(string CourseWorkId, string UserId, double? AssignedGrade, double? DraftGrade);

/// <summary>Everything needed to build the report of one course.</summary>
public record ClassroomData(
    Course Course,
    IReadOnlyList<Student> Students,
    IReadOnlyList<Assignment> Assignments,
    IReadOnlyList<Submission> Submissions);
