using ClassroomGradeReport.Errors;

namespace ClassroomGradeReport.Cli;

public record CliOptions
{
    public string? Course { get; init; }
    public string? CsvPath { get; init; }
    public string? XlsxPath { get; init; }
    public string? TxtPath { get; init; }
    public string? HtmlPath { get; init; }
    public string? CredentialsPath { get; init; }
    public bool IncludeDrafts { get; init; }
    public bool MissingAsZero { get; init; }
    public bool IncludeInactiveCourses { get; init; }
    public bool AllCourses { get; init; }
    public bool Logout { get; init; }
    public bool ShowHelp { get; init; }

    public static string HelpText => """
        ClassroomGradeReport - relatório de notas do Google Classroom (somente leitura)

        Uso:
          dotnet run -- [opções]

        Opções:
          --course <id|nome>       Turma a usar (ID ou parte do nome); pula o menu de seleção
          --csv <caminho>          Exporta o relatório em CSV (separador ';', UTF-8 com BOM);
                                   com --all-courses, é uma pasta e gera um CSV por turma
          --xlsx <caminho>         Exporta o relatório em Excel (.xlsx); pode ser usado junto com --csv;
                                   com --all-courses, é uma pasta e gera um .xlsx por turma
          --txt <caminho>          Exporta o relatório em texto simples (.txt, tabela alinhada, UTF-8 com BOM);
                                   pode ser combinado com --csv/--xlsx; com --all-courses, é uma pasta
          --html <caminho>         Exporta o relatório em página HTML autocontida (abre em qualquer navegador);
                                   combina com os demais formatos; com --all-courses, é uma pasta
          --all-courses            Gera o relatório de todas as turmas (sem menu)
          --include-drafts         Usa a nota rascunho (draftGrade) quando não houver nota devolvida (assignedGrade)
          --missing-as-zero        Trata atividade sem nota como 0 (por padrão ela é ignorada e exibida como "-")
          --include-inactive       Inclui turmas que não estão ativas (arquivadas, etc.)
          --credentials <caminho>  Caminho do credentials.json (ou variável CLASSROOM_CREDENTIALS_PATH;
                                   padrão: %APPDATA%\ClassroomGradeReport\credentials.json)
          --logout                 Apaga o token salvo (sair da conta); na próxima execução a autorização é refeita
          -h, --help               Mostra esta ajuda
        """;

    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var o = new CliOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string Value()
            {
                if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new UserFacingException($"A opção {arg} requer um valor.");
                return args[++i];
            }

            o = arg switch
            {
                "--course" => o with { Course = Value() },
                "--csv" => o with { CsvPath = Value() },
                "--xlsx" => o with { XlsxPath = Value() },
                "--txt" => o with { TxtPath = Value() },
                "--html" => o with { HtmlPath = Value() },
                "--credentials" => o with { CredentialsPath = Value() },
                "--include-drafts" => o with { IncludeDrafts = true },
                "--missing-as-zero" => o with { MissingAsZero = true },
                "--include-inactive" => o with { IncludeInactiveCourses = true },
                "--all-courses" => o with { AllCourses = true },
                "--logout" => o with { Logout = true },
                "-h" or "--help" => o with { ShowHelp = true },
                _ => throw new UserFacingException($"Opção desconhecida: {arg}. Use --help para ver as opções disponíveis.")
            };
        }
        return o;
    }
}
