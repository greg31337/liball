namespace avif;

/// <summary>
/// Provides a thread-safe one-way cancellation signal for bounded decode operations.
/// </summary>
public class AvifCancellation : IAvifCancellation
{
    internal object Gate;
    internal bool Requested;

    /// <summary>Creates a cancellation signal in the non-cancelled state.</summary>
    public AvifCancellation()
    {
        Gate = new object();
        Requested = false;
    }

    /// <summary>Requests cancellation of operations observing this signal.</summary>
    public void Cancel()
    {
        lock (Gate)
        {
            Requested = true;
        }
    }

    /// <summary>Returns whether cancellation has been requested.</summary>
    /// <returns><c>true</c> after cancellation has been requested; otherwise <c>false</c>.</returns>
    public bool IsCancellationRequested()
    {
        lock (Gate)
        {
            return Requested;
        }
    }
}
