namespace sip;

/// <summary>Describes one SIP server resolution request.</summary>
public class SipResolutionRequest
{
    /// <summary>The URI being resolved.</summary>
    public SipUri? Uri;
    /// <summary>The domain to resolve when no URI is supplied.</summary>
    public string? Domain;
    /// <summary>An explicitly requested transport.</summary>
    public SipTransportKind Transport;
    /// <summary>Requires secure candidates when true.</summary>
    public bool Secure;
    /// <summary>Requests ENUM processing when true.</summary>
    public bool UseEnum;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
