using System.Globalization;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

public static class ReportFormatting
{
    public static readonly CultureInfo Culture = new("pt-BR");

    public static string Number(double value) => value.ToString("0.##", Culture);

    public static string Cell(GradeCell cell) => cell.Grade is { } g ? Number(g) : "-";

    public static string Percent(double? pct) => pct is { } p ? p.ToString("0.0", Culture) + "%" : "-";

    public static string Criteria(ReportOptions o) =>
        (o.IncludeDrafts ? "assignedGrade, com draftGrade quando ausente" : "somente assignedGrade (nota devolvida)")
        + (o.MissingAsZero ? "; sem nota = 0" : "; sem nota ('-') não é somada");

    /// <summary>Safe, unique file name for a course: "{name}-{id}{extension}" (extension includes the dot).</summary>
    public static string FileNameFor(string courseName, string courseId, string extension)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(courseName.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (name.Length > 80) name = name[..80].TrimEnd();
        return $"{(name.Length == 0 ? "turma" : name)}-{courseId}{extension}";
    }
}
