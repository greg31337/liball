namespace sip;

/// <summary>Describes one RFC 8840 Trickle ICE INFO package body.</summary>
public class SipIceTrickleRequest
{
    /// <summary>The dialog receiving the INFO request.</summary>
    public SipDialogId? DialogId;
    /// <summary>The ICE generation and candidates to encode.</summary>
    public SipIceDescription? Description;
    /// <summary>The application correlation identifier.</summary>
    public string? CorrelationId;
}
