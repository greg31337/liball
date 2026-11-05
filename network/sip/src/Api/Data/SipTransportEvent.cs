namespace sip;

/// <summary>Holds one queued transport event.</summary>
public class SipTransportEvent
{
    /// <summary>The event kind.</summary>
    public SipTransportEventKind Kind;
    /// <summary>The transport kind.</summary>
    public SipTransportKind Transport;
    /// <summary>The message envelope.</summary>
    public SipTransportMessage? Message;
    /// <summary>The connection metadata.</summary>
    public SipConnectionInfo? Connection;
    /// <summary>The flow metadata.</summary>
    public SipFlow? Flow;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
