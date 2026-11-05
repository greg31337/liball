namespace whois;

/// <summary>
/// Contains status metadata and bytes from a WHOIS exchange.
/// </summary>
public class WhoisRawResponse
{
    /// <summary>
    /// Contains the request metadata for this exchange.
    /// </summary>
    public WhoisRequest Request = new WhoisRequest();

    /// <summary>
    /// Contains the policy-approved address used by the exchange.
    /// </summary>
    public System.Net.IPAddress? ApprovedAddress = null;

    /// <summary>
    /// Contains the bytes retained from the server response.
    /// </summary>
    public byte[] Body = System.Array.Empty<byte>();

    /// <summary>
    /// Contains the wall-clock time at which the exchange began.
    /// </summary>
    public System.DateTimeOffset StartedAt = default(System.DateTimeOffset);

    /// <summary>
    /// Contains the elapsed exchange duration.
    /// </summary>
    public System.TimeSpan Duration = System.TimeSpan.Zero;

    /// <summary>
    /// Describes how the exchange completed.
    /// </summary>
    public WhoisExchangeStatus Status = WhoisExchangeStatus.Complete;

    /// <summary>
    /// Identifies the operation stage associated with the status.
    /// </summary>
    public WhoisOperationStage Stage = WhoisOperationStage.None;

    /// <summary>
    /// Indicates whether the response body was limited to a retained prefix.
    /// </summary>
    public bool IsTruncated = false;

    /// <summary>
    /// Contains the underlying exchange error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
