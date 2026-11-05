namespace whois;

/// <summary>
/// Describes one exact RFC 3912 exchange before query framing.
/// </summary>
public class WhoisRequest
{
    /// <summary>
    /// Contains the endpoint selected for the exchange.
    /// </summary>
    public WhoisEndpoint Endpoint = new WhoisEndpoint();

    /// <summary>
    /// Identifies how the endpoint was selected.
    /// </summary>
    public WhoisEndpointSource Source = WhoisEndpointSource.Direct;

    /// <summary>
    /// Contains the exact query text before the terminating CRLF is appended.
    /// </summary>
    public string QueryText = string.Empty;

    /// <summary>
    /// Contains request encoding and response decoding behavior.
    /// </summary>
    public WhoisEncodingOptions Encoding = new WhoisEncodingOptions();

    /// <summary>
    /// Contains stage and overall timeout limits.
    /// </summary>
    public WhoisTimeoutOptions Timeouts = new WhoisTimeoutOptions();

    /// <summary>
    /// Contains request and response resource limits.
    /// </summary>
    public WhoisLimits Limits = new WhoisLimits();

    /// <summary>
    /// Contains the caller cancellation token observed by the exchange.
    /// </summary>
    public System.Threading.CancellationToken CancellationToken = default(System.Threading.CancellationToken);
}
