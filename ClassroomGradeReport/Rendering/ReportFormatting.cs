using System.Globalization;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Rendering;

/// <summary>Number, grade and file-name formatting shared by every output format (pt-BR).</summary>
public static class ReportFormatting
{
    /// <summary>Culture used for every number shown or exported (decimal comma).</summary>
    public static readonly CultureInfo Culture = new("pt-BR");

    /// <summary>Formats a number with up to two decimals, e.g. "7,5".</summary>
    public static string Number(double value) => value.ToString("0.##", Culture);

    /// <summary>Formats a grade cell; "-" when the student has no grade.</summary>
    public static string Cell(GradeCell cell) => cell.Grade is { } g ? Number(g) : "-";

    /// <summary>Formats a percentage with one decimal, e.g. "82,5%"; "-" when absent.</summary>
    public static string Percent(double? pct) => pct is { } p ? p.ToString("0.0", Culture) + "%" : "-";

    /// <summary>One-line description of how grades were computed, for report headers.</summary>
    public static string Criteria(ReportOptions o) =>
        (o.IncludeDrafts ? "assignedGrade, com draftGrade quando ausente" : "somente assignedGrade (nota devolvida)")
        + (o.MissingAsZero ? "; sem nota = 0" : "; sem nota ('-') não é somada");

    /// <summary>Human-readable lines describing the class statistics of an exam.</summary>
    public static IEnumerable<string> StatsLines(ExamStats s) =>
    [
        $"Avaliados: {s.Graded} de {s.Total} aluno(s)",
        $"Média da turma: {Number(s.Average)}",
        $"Maior nota: {Number(s.Highest)}",
        $"Menor nota: {Number(s.Lowest)}",
        $"Abaixo da média: {s.BelowAverage} aluno(s)",
    ];

    /// <summary>Safe, unique file name for a course: "{name}-{id}{extension}" (extension includes the dot).</summary>
    public static string FileNameFor(string courseName, string courseId, string extension) =>
        $"{SafeName(courseName, "turma")}-{courseId}{extension}";

    /// <summary>Folder that holds the general (non-exam) report of a course.</summary>
    public const string GeneralFolder = "Geral";
    /// <summary>Folder that holds one report per exam of a course.</summary>
    public const string ExamsFolder = "Provas";

    /// <summary>
    /// Path of an exam report: "{courseDir}/Provas/{exam title}{extension}".
    /// <paramref name="taken"/> tracks used paths so exams with the same title don't overwrite each other.
    /// </summary>
    public static string ExamPath(string courseDir, string examTitle, string extension, ISet<string> taken)
    {
        var dir = Path.Combine(courseDir, ExamsFolder);
        var name = SafeName(examTitle, "prova");
        var path = Path.Combine(dir, name + extension);
        for (var n = 2; !taken.Add(path.ToLowerInvariant()); n++)
            path = Path.Combine(dir, $"{name} ({n}){extension}");
        return path;
    }

    /// <summary>Replaces invalid file-name characters, trims and shortens to 80 chars; <paramref name="fallback"/> when empty.</summary>
    public static string SafeName(string text, string fallback)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(text.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (name.Length > 80) name = name[..80].TrimEnd();
        return name.Length == 0 ? fallback : name;
    }
}
