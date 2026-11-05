namespace email;
/// <summary>
/// Supplies cancellation state to a synchronous email operation.
/// </summary>
public interface IEmailCancellation
{
    /// <summary>
    /// Determines whether cancellation has been requested.
    /// </summary>
    /// <returns><c>true</c> when cancellation has been requested; otherwise, <c>false</c>.</returns>
    bool IsCancellationRequested();
}
