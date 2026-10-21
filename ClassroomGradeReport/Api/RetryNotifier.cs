namespace ClassroomGradeReport.Api;

/// <summary>
/// Receives the "retrying in Ns" notices of <see cref="ApiRetry"/>. By default they go to stderr; while a
/// live display (progress bar) is on screen the caller captures them to print later, so nothing garbles it.
/// </summary>
public sealed class RetryNotifier
{
    private volatile Action<string>? _handler;

    /// <summary>Sends every notice to <paramref name="handler"/> until the returned scope is disposed.</summary>
    public IDisposable Capture(Action<string> handler)
    {
        _handler = handler;
        return new Scope(this);
    }

    /// <summary>Reports a retry notice to the active handler, or to stderr when none is set.</summary>
    public void Notify(string message) => (_handler ?? Console.Error.WriteLine)(message);

    private sealed class Scope(RetryNotifier owner) : IDisposable
    {
        public void Dispose() => owner._handler = null;
    }
}
