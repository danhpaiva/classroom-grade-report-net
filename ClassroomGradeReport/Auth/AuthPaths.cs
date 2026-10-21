namespace ClassroomGradeReport.Auth;

/// <summary>Where the app keeps its credentials and cached OAuth token.</summary>
public static class AuthPaths
{
    /// <summary>Environment variable that can point to credentials.json.</summary>
    public const string CredentialsEnvVar = "CLASSROOM_CREDENTIALS_PATH";

    /// <summary>Per-user app folder under %APPDATA%.</summary>
    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassroomGradeReport");

    /// <summary>Folder where the OAuth token is cached.</summary>
    public static string TokenDir => Path.Combine(AppDataDir, "token");

    /// <summary>Precedence: --credentials, then env var, then the default location.</summary>
    public static string ResolveCredentials(string? cliPath)
    {
        if (!string.IsNullOrWhiteSpace(cliPath)) return cliPath;
        var env = Environment.GetEnvironmentVariable(CredentialsEnvVar);
        return string.IsNullOrWhiteSpace(env) ? Path.Combine(AppDataDir, "credentials.json") : env;
    }
}
