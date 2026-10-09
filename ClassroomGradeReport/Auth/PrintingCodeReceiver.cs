using Google.Apis.Auth.OAuth2;

namespace ClassroomGradeReport.Auth;

/// <summary>
/// Local-loopback receiver that prints the authorization URL instead of opening the default browser,
/// so the user can open it in the browser/profile of their choice. The URL carries no tokens.
/// </summary>
public sealed class PrintingCodeReceiver : LocalServerCodeReceiver
{
    protected override bool OpenBrowser(string url)
    {
        Console.WriteLine();
        Console.WriteLine("Abra o link abaixo no navegador (e na conta) de sua preferência para autorizar o acesso:");
        Console.WriteLine();
        Console.WriteLine(url);
        Console.WriteLine();
        Console.WriteLine("Aguardando a autorização... (o navegador será redirecionado para localhost ao concluir)");
        return true;
    }
}
