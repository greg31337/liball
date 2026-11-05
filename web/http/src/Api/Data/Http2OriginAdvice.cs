namespace http;

/// <summary>
/// Represents Origin Set advice without deciding transport authority or connection selection.
/// </summary>
/// <remarks>
/// A permissive outcome is only an input to the caller's certificate, DNS, TLS, and
/// connection-pool policies. It does not establish authority or select a connection.
/// </remarks>
public class Http2OriginAdvice
{
    /// <summary>The operation status.</summary>
    public HttpOperationStatus Status;

    /// <summary>The Origin Set outcome.</summary>
    public Http2OriginOutcome Outcome;

    /// <summary>The origin supplied to the operation.</summary>
    public string Origin = "";

    /// <summary>Indicates that an ORIGIN frame initialized the connection Origin Set.</summary>
    public bool OriginSetInitialized;

    /// <summary>Indicates that the origin is present in the initialized Origin Set.</summary>
    public bool OriginInSet;

    /// <summary>
    /// Indicates that the Origin Set permits the caller to consider the connection authoritative.
    /// </summary>
    public bool OriginSetPermitsAuthority;

    /// <summary>
    /// Indicates that the Origin Set permits the caller to consider connection coalescing.
    /// </summary>
    public bool OriginSetPermitsCoalescing;

    /// <summary>
    /// Indicates that external authority validation remains necessary before use.
    /// </summary>
    public bool RequiresExternalAuthorityValidation;

    /// <summary>
    /// Indicates that external connection selection remains necessary before use.
    /// </summary>
    public bool RequiresExternalConnectionSelection;

    /// <summary>A stable error identifier, or <c>null</c> when advice was produced.</summary>
    public string? ErrorCodeName;

    /// <summary>A caller-readable error explanation, or <c>null</c> when advice was produced.</summary>
    public string? ErrorMessage;
}
