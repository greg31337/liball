namespace sip;

/// <summary>Holds one queued proxy event.</summary>
public class SipProxyEvent
{
    /// <summary>The proxy event kind.</summary>
    public SipProxyEventKind Kind;
    /// <summary>The proxy request.</summary>
    public SipProxyRequest? Request;
    /// <summary>The current proxy result.</summary>
    public SipProxyResult? Result;
    /// <summary>The affected branch target.</summary>
    public SipProxyTarget? Target;
    /// <summary>The associated SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The event reason.</summary>
    public string? Reason;
}
