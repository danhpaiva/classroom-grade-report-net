using ClassroomGradeReport.Domain;

namespace ClassroomGradeReport.Api;

public interface IClassroomGateway
{
    Task<IReadOnlyList<Course>> ListTeacherCoursesAsync(bool activeOnly, CancellationToken ct);
    Task<ClassroomData> LoadCourseDataAsync(Course course, CancellationToken ct);
}
