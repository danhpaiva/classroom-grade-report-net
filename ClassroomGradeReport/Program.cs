using ClassroomGradeReport.Cli;
using ClassroomGradeReport.Errors;
using Spectre.Console;

// Exit codes: 0 = success, 1 = error shown to the user, 130 = cancelled with Ctrl+C.
Console.OutputEncoding = System.Text.Encoding.UTF8;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return await App.RunAsync(args, cts.Token);
}
catch (UserFacingException ex)
{
    AnsiConsole.MarkupLineInterpolated($"[red]Erro:[/] {ex.Message}");
    return 1;
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[yellow]Operação cancelada.[/]");
    return 130;
}
