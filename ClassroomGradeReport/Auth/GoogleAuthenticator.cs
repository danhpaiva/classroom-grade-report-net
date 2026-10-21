using ClassroomGradeReport.Errors;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Classroom.v1;
using Google.Apis.Util.Store;

namespace ClassroomGradeReport.Auth;

/// <summary>Obtains the Google credential used to call the API.</summary>
public interface IAuthenticator
{
    /// <summary>Authorizes the user, reusing the saved token when there is one.</summary>
    Task<UserCredential> AuthorizeAsync(CancellationToken ct);
}

/// <summary>OAuth 2.0 (Desktop app) with read-only scopes. The token is cached under %APPDATA%.</summary>
public sealed class GoogleAuthenticator(string credentialsPath, string tokenDir) : IAuthenticator
{
    // Read-only scopes only: this app must never be able to write anything.
    public static readonly string[] Scopes =
    [
        ClassroomService.Scope.ClassroomCoursesReadonly,
        ClassroomService.Scope.ClassroomRostersReadonly,
        ClassroomService.Scope.ClassroomCourseworkStudentsReadonly,
    ];

    /// <summary>
    /// Reads credentials.json and authorizes. An expired/revoked token is deleted and the flow is repeated once.
    /// </summary>
    /// <exception cref="UserFacingException">Missing/invalid credentials.json or authorization refused.</exception>
    public async Task<UserCredential> AuthorizeAsync(CancellationToken ct)
    {
        if (!File.Exists(credentialsPath))
            throw new UserFacingException(
                $"Arquivo credentials.json não encontrado em: {credentialsPath}\n" +
                "Veja o README para criar a credencial OAuth (Desktop app) no Google Cloud, " +
                $"ou informe o caminho com --credentials / variável {AuthPaths.CredentialsEnvVar}.");

        GoogleClientSecrets secrets;
        try
        {
            await using var stream = File.OpenRead(credentialsPath);
            secrets = await GoogleClientSecrets.FromStreamAsync(stream, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new UserFacingException(
                "Não foi possível ler o credentials.json. Confirme que é uma credencial OAuth do tipo \"Desktop app\".", ex);
        }

        try
        {
            return await AuthorizeFreshAsync(secrets, ct);
        }
        catch (TokenResponseException ex) when (ex.Error?.Error == "invalid_grant")
        {
            // Expired/revoked refresh token: drop it and run the flow again once.
            Console.Error.WriteLine("Token expirado ou revogado. Apagando o token salvo e refazendo a autorização...");
            DeleteToken();
            return await AuthorizeFreshAsync(secrets, ct);
        }
    }

    /// <summary>Removes the cached token so the next call triggers a new authorization.</summary>
    public void DeleteToken()
    {
        if (Directory.Exists(tokenDir)) Directory.Delete(tokenDir, recursive: true);
    }

    /// <summary>
    /// Authorizes and makes sure the access token is usable right now. A cached token is only refreshed lazily on the
    /// first API call, so an expired/revoked refresh token would otherwise surface far from here; refreshing eagerly
    /// lets <see cref="AuthorizeAsync"/> catch <c>invalid_grant</c> and re-run the flow.
    /// </summary>
    private async Task<UserCredential> AuthorizeFreshAsync(GoogleClientSecrets secrets, CancellationToken ct)
    {
        var credential = await AuthorizeCoreAsync(secrets, ct);
        await credential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        return credential;
    }

    /// <summary>Runs the OAuth flow against the cached token store.</summary>
    private async Task<UserCredential> AuthorizeCoreAsync(GoogleClientSecrets secrets, CancellationToken ct)
    {
        try
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets.Secrets, Scopes, "user", ct, new FileDataStore(tokenDir, fullPath: true), new PrintingCodeReceiver());
        }
        catch (TokenResponseException ex) when (ex.Error?.Error == "access_denied")
        {
            throw new UserFacingException("Autorização recusada. O acesso ao Google Classroom é necessário para gerar o relatório.", ex);
        }
    }
}
