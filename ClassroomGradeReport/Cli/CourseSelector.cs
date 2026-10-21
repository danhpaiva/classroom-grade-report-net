using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using Spectre.Console;

namespace ClassroomGradeReport.Cli;

/// <summary>Decides which courses a run reports on.</summary>
public static class CourseSelector
{
    /// <summary>All courses with --all-courses; otherwise the one given by --course, or picked from a menu.</summary>
    public static IReadOnlyList<Course> Select(CliOptions options, IReadOnlyList<Course> courses, IAnsiConsole console)
    {
        if (options.AllCourses) return courses;
        var course = options.Course is { } query ? Find(courses, query) : ConsoleReportRenderer.PickCourse(courses, console)!;
        return [course];
    }

    /// <summary>Finds a course by exact ID or by part of its name (case-insensitive).</summary>
    /// <exception cref="UserFacingException">No course matches, or more than one does.</exception>
    public static Course Find(IReadOnlyList<Course> courses, string query)
    {
        var byId = courses.FirstOrDefault(c => c.Id == query);
        if (byId is not null) return byId;

        var matches = courses.Where(c => c.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new UserFacingException($"Nenhuma turma corresponde a \"{query}\"."),
            _ => throw new UserFacingException(
                $"Mais de uma turma corresponde a \"{query}\": {string.Join("; ", matches.Select(m => $"{m.Name} [{m.Id}]"))}. Use o ID."),
        };
    }
}
