using ClassroomGradeReport.Domain;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Rendering;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Cli;

/// <summary>One requested export format: where to write it and how.</summary>
/// <param name="Label">Name shown to the user, e.g. "CSV".</param>
/// <param name="Option">Command-line option that requested it, e.g. "--csv".</param>
/// <param name="Extension">File extension including the dot, e.g. ".csv".</param>
/// <param name="Location">Path given on the command line: a file for one course, a folder with --all-courses.</param>
/// <param name="Write">Writer that saves a report to a file.</param>
public record Export(string Label, string Option, string Extension, string Location, Func<GradeReport, string, CancellationToken, Task> Write)
{
    /// <summary>Builds one <see cref="Export"/> for each format requested on the command line.</summary>
    public static IReadOnlyList<Export> FromOptions(CliOptions options)
    {
        List<Export> exports = [];
        Add(exports, options.CsvPath, "CSV", "--csv", ".csv", CsvReportWriter.WriteAsync);
        Add(exports, options.XlsxPath, "Excel", "--xlsx", ".xlsx", XlsxReportWriter.WriteAsync);
        Add(exports, options.TxtPath, "TXT", "--txt", ".txt", TxtReportWriter.WriteAsync);
        Add(exports, options.HtmlPath, "HTML", "--html", ".html", HtmlReportWriter.WriteAsync);
        return exports;
    }

    /// <summary>With --all-courses every export path must be a folder, not a single file.</summary>
    /// <exception cref="UserFacingException">An export path looks like a file.</exception>
    public static void EnsureFolders(IEnumerable<Export> exports)
    {
        foreach (var e in exports.Where(e => e.Location.EndsWith(e.Extension, StringComparison.OrdinalIgnoreCase)))
            throw new UserFacingException($"Com --all-courses, {e.Option} deve ser uma pasta (um arquivo por turma), não um arquivo {e.Extension}.");
    }

    /// <summary>
    /// Destination file of <paramref name="report"/>. With --all-courses:
    /// "{base}/{course}/Geral/{course}.{ext}" and "{base}/{course}/Provas/{exam}.{ext}".
    /// With one course, given "{dir}/notas.csv": "{dir}/notas/Geral/notas.csv" and "{dir}/notas/Provas/{exam}.csv".
    /// </summary>
    /// <param name="takenExamPaths">Exam paths already used for this course, to avoid overwriting equal titles.</param>
    public string TargetFor(GradeReport report, Course course, bool allCourses, ISet<string> takenExamPaths)
    {
        var courseFile = ReportFormatting.FileNameFor(course.Name, course.Id, Extension);
        var courseDir = allCourses
            ? Path.Combine(Location, Path.GetFileNameWithoutExtension(courseFile))
            : Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(Location))!,
                Path.GetFileNameWithoutExtension(Location));

        if (report.ExamTitle is not null)
            return ReportFormatting.ExamPath(courseDir, report.ExamTitle, Extension, takenExamPaths);

        var generalFile = allCourses ? courseFile : Path.GetFileName(Location);
        return Path.Combine(courseDir, ReportFormatting.GeneralFolder, generalFile);
    }

    private static void Add(
        List<Export> exports, string? path, string label, string option, string extension,
        Func<GradeReport, string, CancellationToken, Task> write)
    {
        if (path is not null) exports.Add(new Export(label, option, extension, path, write));
    }
}
