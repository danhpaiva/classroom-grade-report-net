using System.Text;

namespace ClassroomGradeReport.Rendering;

/// <summary>File output shared by every report writer.</summary>
internal static class ReportFile
{
    /// <summary>Creates the parent folder of <paramref name="path"/> when it does not exist yet.</summary>
    public static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    /// <summary>Writes <paramref name="content"/> as UTF-8 (optionally with BOM), creating the folder first.</summary>
    public static async Task WriteTextAsync(string path, string content, bool utf8Bom, CancellationToken ct)
    {
        EnsureDirectory(path);
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: utf8Bom), ct);
    }

    /// <summary>Writes <paramref name="bytes"/>, creating the folder first.</summary>
    public static async Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct)
    {
        EnsureDirectory(path);
        await File.WriteAllBytesAsync(path, bytes, ct);
    }
}
