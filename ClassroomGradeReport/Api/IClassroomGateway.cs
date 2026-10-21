using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Api;

/// <summary>Read-only access to Google Classroom data.</summary>
public interface IClassroomGateway
{
    /// <summary>Courses where the signed-in user is a teacher; <paramref name="activeOnly"/> keeps only ACTIVE ones.</summary>
    Task<IReadOnlyList<Course>> ListTeacherCoursesAsync(bool activeOnly, CancellationToken ct);
    /// <summary>Students, assignments and submissions of a course.</summary>
    Task<ClassroomData> LoadCourseDataAsync(Course course, CancellationToken ct);
}
