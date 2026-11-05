namespace sip;

/// <summary>Describes an INFO package operation.</summary>
public class SipInfoRequest
{
    /// <summary>The containing dialog.</summary>
    public SipDialogId? DialogId;
    /// <summary>The INFO package token.</summary>
    public string? Package;
    /// <summary>The INFO body bytes.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The INFO body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The INFO body limits and legacy usage policy.</summary>
    public SipInfoOptions Options = new SipInfoOptions();
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
