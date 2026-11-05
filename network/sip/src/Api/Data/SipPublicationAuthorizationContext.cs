namespace sip;

/// <summary>Holds validated publication information for application authorization.</summary>
public class SipPublicationAuthorizationContext
{
    /// <summary>The incoming PUBLISH message when available.</summary>
    public SipMessage? Message;
    /// <summary>The normalized publication request.</summary>
    public SipPublicationRequest? Request;
    /// <summary>The matched stored publication for a conditional operation.</summary>
    public SipPublication? CurrentPublication;
    /// <summary>Indicates an initial publication.</summary>
    public bool Initial;
    /// <summary>Indicates an event-state modification.</summary>
    public bool Modify;
    /// <summary>Indicates an expiry refresh without a body.</summary>
    public bool Refresh;
    /// <summary>Indicates immediate publication removal.</summary>
    public bool Remove;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
