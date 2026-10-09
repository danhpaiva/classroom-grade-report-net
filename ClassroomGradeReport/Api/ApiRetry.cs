using System.Net;
using ClassroomGradeReport.Errors;
using Google;

namespace ClassroomGradeReport.Api;

/// <summary>Retries 429/5xx with exponential backoff and translates Google errors to pt-BR messages.</summary>
public static class ApiRetry
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> call, CancellationToken ct, int maxAttempts = 5, TimeSpan? baseDelay = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await call();
            }
            catch (GoogleApiException ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var wait = delay * Math.Pow(2, attempt - 1) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));
                Console.Error.WriteLine($"API indisponível ou cota excedida ({(int)ex.HttpStatusCode}). Nova tentativa em {wait.TotalSeconds:F0}s...");
                await Task.Delay(wait, ct);
            }
            catch (GoogleApiException ex)
            {
                throw Translate(ex);
            }
        }
    }

    private static bool IsTransient(GoogleApiException ex) =>
        ex.HttpStatusCode == HttpStatusCode.TooManyRequests || (int)ex.HttpStatusCode >= 500;

    public static Exception Translate(GoogleApiException ex)
    {
        var text = ex.Error?.Message ?? ex.Message;
        return ex.HttpStatusCode switch
        {
            HttpStatusCode.Forbidden when text.Contains("has not been used", StringComparison.OrdinalIgnoreCase)
                || text.Contains("disabled", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SERVICE_DISABLED", StringComparison.OrdinalIgnoreCase) =>
                new UserFacingException("A Google Classroom API não está habilitada no projeto do Google Cloud. Ative-a em APIs e serviços > Biblioteca (veja o README).", ex),
            HttpStatusCode.Forbidden =>
                new UserFacingException($"Acesso negado pela API (403): {text}", ex),
            HttpStatusCode.NotFound =>
                new UserFacingException("Turma não encontrada ou sem permissão de acesso (404).", ex),
            HttpStatusCode.TooManyRequests =>
                new UserFacingException("Cota da API excedida (429) mesmo após várias tentativas. Tente novamente mais tarde.", ex),
            _ => new UserFacingException($"Erro na API do Google ({(int)ex.HttpStatusCode}): {text}", ex),
        };
    }
}
