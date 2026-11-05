namespace whois;

/// <summary>
/// Contains the status-bearing result of an automatic WHOIS lookup.
/// </summary>
public class WhoisLookupResult
{
    /// <summary>
    /// Contains the classified and normalized query.
    /// </summary>
    public WhoisQuery Query = new WhoisQuery();

    /// <summary>
    /// Contains every completed lookup hop in traversal order.
    /// </summary>
    public WhoisLookupHop[] Hops = System.Array.Empty<WhoisLookupHop>();

    /// <summary>
    /// Indicates whether lookup processing completed successfully.
    /// </summary>
    public bool IsComplete = false;

    /// <summary>
    /// Identifies the stage at which an incomplete lookup failed.
    /// </summary>
    public WhoisOperationStage FailedStage = WhoisOperationStage.None;

    /// <summary>
    /// Contains the lookup error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
