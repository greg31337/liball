namespace whois;

/// <summary>
/// Describes caller input for an automatic WHOIS lookup.
/// </summary>
public class WhoisLookupRequest
{
    /// <summary>
    /// Contains the caller-supplied query text.
    /// </summary>
    public string Text = string.Empty;

    /// <summary>
    /// Specifies how the query text is classified.
    /// </summary>
    public WhoisQueryKind Kind = WhoisQueryKind.Automatic;

    /// <summary>
    /// Contains options applied to the lookup.
    /// </summary>
    public WhoisClientOptions Options = new WhoisClientOptions();

    /// <summary>
    /// Contains a caller-selected initial endpoint, or <c>null</c> for automatic selection.
    /// </summary>
    public WhoisEndpoint? EndpointOverride = null;

    /// <summary>
    /// Contains the caller cancellation token observed by the lookup.
    /// </summary>
    public System.Threading.CancellationToken CancellationToken = default(System.Threading.CancellationToken);
}
