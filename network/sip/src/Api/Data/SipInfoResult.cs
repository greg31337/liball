namespace sip;

/// <summary>Holds the outcome of processing one INFO request.</summary>
public class SipInfoResult
{
    /// <summary>The high-level INFO request.</summary>
    public SipInfoRequest? Request;
    /// <summary>The generated or received INFO message.</summary>
    public SipMessage? Message;
    /// <summary>The associated dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The case-sensitive package name, or null for legacy INFO.</summary>
    public string? Package;
    /// <summary>Indicates a legacy INFO usage.</summary>
    public bool Legacy;
    /// <summary>Indicates that the INFO package was accepted.</summary>
    public bool Accepted;
    /// <summary>The response status code.</summary>
    public int StatusCode;
    /// <summary>The generated SIP response.</summary>
    public SipMessage? Response;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
