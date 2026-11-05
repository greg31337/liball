namespace sip;

/// <summary>Holds the result of identity metadata inspection or message transformation.</summary>
public class SipIdentityResult
{
    /// <summary>Indicates that the identity operation succeeded.</summary>
    public bool Success;
    /// <summary>The recommended SIP failure status, or 200 after successful inspection.</summary>
    public int StatusCode;
    /// <summary>The detached identity metadata.</summary>
    public SipIdentityMetadata? Metadata;
    /// <summary>The detached transformed message, when an operation produced one.</summary>
    public SipMessage? Message;
    /// <summary>The failure description, or null on success.</summary>
    public string? ErrorMessage;
}
