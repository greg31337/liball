namespace sip;

/// <summary>Describes an application-controlled mapping between independent session legs.</summary>
public class SipBackToBackMapping
{
    /// <summary>The leg on which the source message was received.</summary>
    public SipBackToBackLeg SourceLeg;
    /// <summary>The received source-leg message.</summary>
    public SipMessage? SourceMessage;
    /// <summary>The application-created message for the opposite leg.</summary>
    public SipMessage? MappedMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
