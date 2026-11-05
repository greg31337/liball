namespace whois;

/// <summary>
/// Reports a failed resolution, connection, write, or read operation with partial response context.
/// </summary>
public class WhoisTransportException : System.Exception
{
    /// <summary>
    /// Identifies the transport stage that failed.
    /// </summary>
    public WhoisOperationStage Stage;

    /// <summary>
    /// Contains the advertised endpoint used by the failed exchange.
    /// </summary>
    public WhoisEndpoint Endpoint;

    /// <summary>
    /// Contains the policy-approved address, or <c>null</c> when failure occurred earlier.
    /// </summary>
    public System.Net.IPAddress? ApprovedAddress;

    /// <summary>
    /// Contains the status and partial raw response retained before failure.
    /// </summary>
    public WhoisRawResponse PartialResponse;

    /// <summary>
    /// Initializes a transport exception with endpoint and partial-response context.
    /// </summary>
    /// <param name="message">The transport failure message.</param>
    /// <param name="stage">The transport operation stage that failed.</param>
    /// <param name="endpoint">The advertised endpoint.</param>
    /// <param name="approvedAddress">The approved address, or <c>null</c>.</param>
    /// <param name="partialResponse">The status-bearing partial raw response.</param>
    /// <param name="innerException">The underlying transport exception, or <c>null</c>.</param>
    public WhoisTransportException(string message, WhoisOperationStage stage, WhoisEndpoint endpoint, System.Net.IPAddress? approvedAddress, WhoisRawResponse partialResponse, System.Exception? innerException) : base(message, innerException)
    {
        Stage = stage;
        Endpoint = endpoint;
        ApprovedAddress = approvedAddress;
        PartialResponse = partialResponse;
    }
}
