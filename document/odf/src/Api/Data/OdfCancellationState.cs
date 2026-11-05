namespace odf;

/// <summary>
/// Carries explicit cancellation state for synchronous SDK operations.
/// </summary>
public class OdfCancellationState
{
    /// <summary>
    /// Whether cancellation has been requested.
    /// </summary>
    public bool IsCancellationRequested;
    /// <summary>
    /// A caller-supplied reason for cancellation, or an empty string.
    /// </summary>
    public string Reason = "";
}
