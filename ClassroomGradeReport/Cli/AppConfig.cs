using System.Text.Json;
using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Errors;
using ClassroomGradeReport.Reporting;

namespace ClassroomGradeReport.Cli;

/// <summary>
/// Optional JSON file with default values for the command-line options, plus grade weights by title keyword.
/// Anything given on the command line overrides the file. Example:
/// <code>
/// { "examKeyword": "Prova", "csv": "relatorios", "weights": [ { "keyword": "Trabalho", "weight": 2 } ] }
/// </code>
/// </summary>
public sealed record AppConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string? ExamKeyword { get; init; }
    public bool? IncludeDrafts { get; init; }
    public bool? MissingAsZero { get; init; }
    public bool? IncludeInactive { get; init; }
    public string? Credentials { get; init; }
    public string? Csv { get; init; }
    public string? Xlsx { get; init; }
    public string? Txt { get; init; }
    public string? Html { get; init; }
    public List<WeightRule>? Weights { get; init; }

    /// <summary>Default location: %APPDATA%\ClassroomGradeReport\config.json.</summary>
    public static string DefaultPath => Path.Combine(AuthPaths.AppDataDir, "config.json");

    /// <summary>
    /// Loads the config from <paramref name="explicitPath"/> (must exist) or from <see cref="DefaultPath"/>
    /// (optional: an empty config is used when absent).
    /// </summary>
    /// <exception cref="UserFacingException">File missing (explicit path), unreadable, malformed or with invalid weights.</exception>
    public static AppConfig Load(string? explicitPath)
    {
        var path = explicitPath ?? DefaultPath;
        if (!File.Exists(path))
        {
            if (explicitPath is null) return new AppConfig();
            throw new UserFacingException($"Arquivo de configuração não encontrado: {path}");
        }

        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), JsonOptions) ?? new AppConfig();
            config.Validate(path);
            return config;
        }
        catch (JsonException ex)
        {
            throw new UserFacingException($"Configuração inválida em {path}: {ex.Message}", ex);
        }
    }

    /// <summary>The command-line options this config implies, used as the starting point before the arguments are applied.</summary>
    public CliOptions ToDefaults()
    {
        var defaults = new CliOptions
        {
            CsvPath = Csv, XlsxPath = Xlsx, TxtPath = Txt, HtmlPath = Html, CredentialsPath = Credentials,
            IncludeDrafts = IncludeDrafts ?? false,
            MissingAsZero = MissingAsZero ?? false,
            IncludeInactiveCourses = IncludeInactive ?? false,
        };
        return ExamKeyword is { Length: > 0 } keyword ? defaults with { ExamKeyword = keyword } : defaults;
    }

    /// <summary>Grade weights from the file; weight 1 for everything when none are configured.</summary>
    public IGradingPolicy ToPolicy() =>
        Weights is { Count: > 0 } ? new KeywordWeightPolicy(Weights) : new UnweightedGradingPolicy();

    private void Validate(string path)
    {
        foreach (var rule in Weights ?? [])
        {
            if (string.IsNullOrWhiteSpace(rule.Keyword) || rule.Weight <= 0)
                throw new UserFacingException(
                    $"Configuração inválida em {path}: cada peso precisa de \"keyword\" e de \"weight\" maior que zero.");
        }
    }
}
