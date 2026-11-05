namespace sip;

/// <summary>Describes one proxy processing operation.</summary>
public class SipProxyRequest
{
    /// <summary>The received request.</summary>
    public SipMessage? Message;
    /// <summary>The transport envelope.</summary>
    public SipTransportMessage? TransportMessage;
    /// <summary>The initial proxy targets.</summary>
    public SipProxyTarget[] Targets = new SipProxyTarget[0];
    /// <summary>The proxy options.</summary>
    public SipProxyOptions Options = new SipProxyOptions();
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>A previously established authentication result.</summary>
    public SipAuthenticationResult? Authentication;
}
