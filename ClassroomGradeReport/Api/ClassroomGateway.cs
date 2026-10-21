using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Domain;
using Google.Apis.Classroom.v1;
using Google.Apis.Services;
using Course = ClassroomGradeReport.Domain.Course;
using Student = ClassroomGradeReport.Domain.Student;

namespace ClassroomGradeReport.Api;

/// <summary>Thin wrapper over Google.Apis.Classroom.v1. All list calls follow nextPageToken.</summary>
public sealed class ClassroomGateway(IAuthenticator authenticator, RetryNotifier retryNotifier) : IClassroomGateway
{
    // Lazy<Task> guarantees the OAuth flow runs exactly once even when called concurrently.
    private readonly Lazy<Task<ClassroomService>> _service = new(async () =>
        new ClassroomService(new BaseClientService.Initializer
        {
            HttpClientInitializer = await authenticator.AuthorizeAsync(CancellationToken.None),
            ApplicationName = "ClassroomGradeReport",
        }));

    private const int PageSize = 100;

    // Partial responses: ask the API only for the fields the report reads (smaller payloads, faster pages).
    private const string CourseFields = "courses(id,name,section,courseState),nextPageToken";
    private const string StudentFields = "students(userId,profile(name(fullName),emailAddress)),nextPageToken";
    private const string CourseWorkFields = "courseWork(id,title,maxPoints,state),nextPageToken";
    private const string SubmissionFields = "studentSubmissions(courseWorkId,userId,assignedGrade,draftGrade),nextPageToken";

    /// <summary>Runs an API call with retry/backoff, reporting retries through the notifier.</summary>
    private Task<T> ExecuteAsync<T>(Func<Task<T>> call, CancellationToken ct) =>
        ApiRetry.ExecuteAsync(call, ct, onRetry: retryNotifier.Notify);

    private Task<ClassroomService> GetServiceAsync(CancellationToken ct) => _service.Value.WaitAsync(ct);

    /// <summary>Lists the courses where the signed-in user is a teacher; <paramref name="activeOnly"/> keeps only ACTIVE ones.</summary>
    public async Task<IReadOnlyList<Course>> ListTeacherCoursesAsync(bool activeOnly, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        return await FetchAllPagesAsync(async token =>
        {
            var req = svc.Courses.List();
            req.TeacherId = "me";
            req.Fields = CourseFields;
            req.PageSize = PageSize;
            req.PageToken = token;
            if (activeOnly) req.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var page = await ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            return (page.Courses?.Select(c => new Course(c.Id, c.Name ?? "(sem nome)", c.Section, c.CourseState ?? "")), page.NextPageToken);
        });
    }

    /// <summary>Loads students, assignments and submissions of a course.</summary>
    public async Task<ClassroomData> LoadCourseDataAsync(Course course, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        // The three lists are independent: fetch them concurrently (pages within a list stay sequential).
        var students = LoadStudentsAsync(svc, course.Id, ct);
        var work = LoadCourseWorkAsync(svc, course.Id, ct);
        var subs = LoadSubmissionsAsync(svc, course.Id, ct);
        await Task.WhenAll(students, work, subs);
        return new ClassroomData(course, await students, await work, await subs);
    }

    /// <summary>
    /// Follows nextPageToken until the API reports no more pages. <paramref name="fetchPage"/> receives the current
    /// page token (null for the first page) and returns that page's items plus the next token.
    /// </summary>
    private static async Task<List<T>> FetchAllPagesAsync<T>(Func<string?, Task<(IEnumerable<T>? Items, string? NextToken)>> fetchPage)
    {
        var all = new List<T>();
        string? token = null;
        do
        {
            var (items, next) = await fetchPage(token);
            all.AddRange(items ?? []);
            token = next;
        } while (!string.IsNullOrEmpty(token));
        return all;
    }

    private Task<List<Student>> LoadStudentsAsync(ClassroomService svc, string courseId, CancellationToken ct) =>
        FetchAllPagesAsync(async token =>
        {
            var req = svc.Courses.Students.List(courseId);
            req.Fields = StudentFields;
            req.PageSize = PageSize;
            req.PageToken = token;
            var page = await ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            return (page.Students?.Select(s => new Student(s.UserId, s.Profile?.Name?.FullName ?? s.UserId, s.Profile?.EmailAddress)),
                page.NextPageToken);
        });

    private Task<List<Assignment>> LoadCourseWorkAsync(ClassroomService svc, string courseId, CancellationToken ct) =>
        FetchAllPagesAsync(async token =>
        {
            var req = svc.Courses.CourseWork.List(courseId);
            req.Fields = CourseWorkFields;
            req.PageSize = PageSize;
            req.PageToken = token;
            var page = await ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            return (page.CourseWork?.Select(w => new Assignment(w.Id, w.Title ?? "(sem título)", w.MaxPoints, w.State ?? "")),
                page.NextPageToken);
        });

    private Task<List<Submission>> LoadSubmissionsAsync(ClassroomService svc, string courseId, CancellationToken ct) =>
        FetchAllPagesAsync(async token =>
        {
            var req = svc.Courses.CourseWork.StudentSubmissions.List(courseId, "-");
            req.Fields = SubmissionFields;
            req.PageSize = PageSize;
            req.PageToken = token;
            var page = await ExecuteAsync(() => req.ExecuteAsync(ct), ct);
            return (page.StudentSubmissions?.Select(s => new Submission(s.CourseWorkId, s.UserId, s.AssignedGrade, s.DraftGrade)),
                page.NextPageToken);
        });
}
