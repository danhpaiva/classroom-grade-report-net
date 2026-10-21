using ClassroomGradeReport.Api;
using ClassroomGradeReport.Auth;
using ClassroomGradeReport.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ClassroomGradeReport.Cli;

/// <summary>Application entry flow: parse the arguments, handle --help/--logout, otherwise run the report.</summary>
public static class App
{
    /// <summary>Runs the app and returns the process exit code (0 on success).</summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken ct)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            Console.WriteLine(CliOptions.HelpText);
            return 0;
        }

        if (options.Logout)
        {
            Logout(options);
            return 0;
        }

        await using var services = BuildServices(options);
        await services.GetRequiredService<ReportRunner>().RunAsync(options, ct);
        return 0;
    }

    /// <summary>Deletes the saved OAuth token so the next run asks for authorization again.</summary>
    private static void Logout(CliOptions options)
    {
        new GoogleAuthenticator(AuthPaths.ResolveCredentials(options.CredentialsPath), AuthPaths.TokenDir).DeleteToken();
        Console.WriteLine("Token apagado. Na próxima execução você poderá autorizar com outra conta.");
    }

    /// <summary>Wires the authenticator, API gateway, report builder, console and runner.</summary>
    private static ServiceProvider BuildServices(CliOptions options) =>
        new ServiceCollection()
            .AddSingleton<IAuthenticator>(_ => new GoogleAuthenticator(
                AuthPaths.ResolveCredentials(options.CredentialsPath), AuthPaths.TokenDir))
            .AddSingleton<IClassroomGateway, ClassroomGateway>()
            .AddSingleton<GradeReportBuilder>()
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton<ReportRunner>()
            .BuildServiceProvider();
}
