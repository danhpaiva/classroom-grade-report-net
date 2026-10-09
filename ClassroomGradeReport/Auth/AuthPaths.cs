namespace ClassroomGradeReport.Auth;

public static class AuthPaths
{
    public const string CredentialsEnvVar = "CLASSROOM_CREDENTIALS_PATH";

    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassroomGradeReport");

    public static string TokenDir => Path.Combine(AppDataDir, "token");

    /// <summary>Precedence: --credentials, then env var, then the default location.</summary>
    public static string ResolveCredentials(string? cliPath)
    {
        if (!string.IsNullOrWhiteSpace(cliPath)) return cliPath;
        var env = Environment.GetEnvironmentVariable(CredentialsEnvVar);
        return string.IsNullOrWhiteSpace(env) ? Path.Combine(AppDataDir, "credentials.json") : env;
    }
}
