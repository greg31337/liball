namespace sip;

/// <summary>Holds one queued INVITE usage event.</summary>
public class SipInviteEvent
{
    /// <summary>The INVITE usage state.</summary>
    public SipInviteSessionState State;
    /// <summary>The complete INVITE usage snapshot after the event.</summary>
    public SipInviteSnapshot? Snapshot;
    /// <summary>The dialog snapshot.</summary>
    public SipDialogSnapshot? Dialog;
    /// <summary>The associated SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The offer/answer state.</summary>
    public SipOfferAnswer? OfferAnswer;
    /// <summary>The event reason.</summary>
    public string? Reason;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The protocol reasons carried by the associated message.</summary>
    public SipExtensionHeaderValue[] Reasons = new SipExtensionHeaderValue[0];
    /// <summary>The currently accepted identity of the remote endpoint.</summary>
    public SipNameAddress? ConnectedIdentity;
    /// <summary>The local endpoint UUID for the communication session.</summary>
    public string? LocalSessionId;
    /// <summary>The most recently learned remote endpoint UUID.</summary>
    public string? RemoteSessionId;
    /// <summary>The capabilities advertised by the remote endpoint.</summary>
    public SipCapabilitySet? RemoteCapabilities;
    /// <summary>The feature capability values advertised by the remote endpoint.</summary>
    public SipExtensionHeaderValue[] RemoteFeatureCapabilities =
        new SipExtensionHeaderValue[0];
    /// <summary>The terminal transport failure description.</summary>
    public string? ErrorMessage;
}
