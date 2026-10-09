using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Domain;
using Google.Apis.Classroom.v1;
using Google.Apis.Services;
using Course = ClassroomGradeReport.Domain.Course;
using Student = ClassroomGradeReport.Domain.Student;

namespace ClassroomGradeReport.Api;

/// <summary>Thin wrapper over Google.Apis.Classroom.v1. All list calls follow nextPageToken.</summary>
public sealed class ClassroomGateway(IAuthenticator authenticator) : IClassroomGateway
{
    private ClassroomService? _service;

    private async Task<ClassroomService> GetServiceAsync(CancellationToken ct) =>
        _service ??= new ClassroomService(new BaseClientService.Initializer
        {
            HttpClientInitializer = await authenticator.AuthorizeAsync(ct),
            ApplicationName = "ClassroomGradeReport",
        });

    public async Task<IReadOnlyList<Course>> ListTeacherCoursesAsync(bool activeOnly, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        var result = new List<Course>();
        string? token = null;
        do
        {
            var req = svc.Courses.List();
            req.TeacherId = "me";
            req.PageSize = 100;
            req.PageToken = token;
            if (activeOnly) req.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var page = await ApiRetry.ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            result.AddRange((page.Courses ?? []).Select(c => new Course(c.Id, c.Name ?? "(sem nome)", c.Section, c.CourseState ?? "")));
            token = page.NextPageToken;
        } while (!string.IsNullOrEmpty(token));
        return result;
    }

    public async Task<ClassroomData> LoadCourseDataAsync(Course course, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        var students = await LoadStudentsAsync(svc, course.Id, ct);
        var work = await LoadCourseWorkAsync(svc, course.Id, ct);
        var subs = await LoadSubmissionsAsync(svc, course.Id, ct);
        return new ClassroomData(course, students, work, subs);
    }

    private static async Task<List<Student>> LoadStudentsAsync(ClassroomService svc, string courseId, CancellationToken ct)
    {
        var list = new List<Student>();
        string? token = null;
        do
        {
            var req = svc.Courses.Students.List(courseId);
            req.PageSize = 100;
            req.PageToken = token;
            var page = await ApiRetry.ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            list.AddRange((page.Students ?? []).Select(s =>
                new Student(s.UserId, s.Profile?.Name?.FullName ?? s.UserId, s.Profile?.EmailAddress)));
            token = page.NextPageToken;
        } while (!string.IsNullOrEmpty(token));
        return list;
    }

    private static async Task<List<Assignment>> LoadCourseWorkAsync(ClassroomService svc, string courseId, CancellationToken ct)
    {
        var list = new List<Assignment>();
        string? token = null;
        do
        {
            var req = svc.Courses.CourseWork.List(courseId);
            req.PageSize = 100;
            req.PageToken = token;
            var page = await ApiRetry.ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            list.AddRange((page.CourseWork ?? []).Select(w =>
                new Assignment(w.Id, w.Title ?? "(sem título)", w.MaxPoints, w.State ?? "")));
            token = page.NextPageToken;
        } while (!string.IsNullOrEmpty(token));
        return list;
    }

    private static async Task<List<Submission>> LoadSubmissionsAsync(ClassroomService svc, string courseId, CancellationToken ct)
    {
        var list = new List<Submission>();
        string? token = null;
        do
        {
            var req = svc.Courses.CourseWork.StudentSubmissions.List(courseId, "-");
            req.PageSize = 100;
            req.PageToken = token;
            var page = await ApiRetry.ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            list.AddRange((page.StudentSubmissions ?? []).Select(s =>
                new Submission(s.CourseWorkId, s.UserId, s.AssignedGrade, s.DraftGrade)));
            token = page.NextPageToken;
        } while (!string.IsNullOrEmpty(token));
        return list;
    }
}
