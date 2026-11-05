namespace email;
/// <summary>
/// Represents a cancellation.
/// </summary>
public class EmailCancellation : IEmailCancellation, IEmailCancellationContract
{
    object synchronization;
    bool cancellationRequested;
    /// <summary>
    /// Creates a new cancellation.
    /// </summary>
    public EmailCancellation()
    {
        synchronization = new object ();
        cancellationRequested = false;
    }

    /// <summary>
    /// Determines whether the instance can cel.
    /// </summary>
    public void Cancel()
    {
        lock (synchronization)
        {
            cancellationRequested = true;
        }
    }

    /// <summary>
    /// Determines whether the current value or state is cancellation requested.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cancellation requested; otherwise, <c>false</c>.</returns>
    public bool IsCancellationRequested()
    {
        lock (synchronization)
        {
            return cancellationRequested;
        }
    }
}
