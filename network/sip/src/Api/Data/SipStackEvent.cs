namespace sip;

/// <summary>Holds one event from the unified SIP endpoint queue.</summary>
public class SipStackEvent
{
    /// <summary>The stack event kind.</summary>
    public SipStackEventKind Kind;
    /// <summary>The endpoint lifecycle state after the event.</summary>
    public SipEndpointState State;
    /// <summary>The associated SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The associated transport event.</summary>
    public SipTransportEvent? TransportEvent;
    /// <summary>The associated transaction event.</summary>
    public SipTransactionEvent? TransactionEvent;
    /// <summary>The associated dialog event.</summary>
    public SipDialogEvent? DialogEvent;
    /// <summary>The associated registration event.</summary>
    public SipRegistrationEvent? RegistrationEvent;
    /// <summary>The associated proxy event.</summary>
    public SipProxyEvent? ProxyEvent;
    /// <summary>The associated INVITE event.</summary>
    public SipInviteEvent? InviteEvent;
    /// <summary>The associated application extension dispatch result.</summary>
    public SipExtensionResult? ExtensionResult;
    /// <summary>The associated transport send result.</summary>
    public SipSendResult? SendResult;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
