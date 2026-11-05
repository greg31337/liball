namespace sip;

/// <summary>Holds the result of adding capability or caller-preference headers.</summary>
public class SipCapabilityMessageResult
{
    /// <summary>Indicates that all requested headers were added.</summary>
    public bool Success;
    /// <summary>The detached message containing the added headers.</summary>
    public SipMessage? Message;
    /// <summary>The failure description, or null on success.</summary>
    public string? ErrorMessage;
}
