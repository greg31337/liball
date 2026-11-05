namespace sip;

/// <summary>Holds one ordered proxy branch target.</summary>
public class SipProxyTarget
{
    /// <summary>The target URI.</summary>
    public SipUri? Uri;
    /// <summary>The resolved server candidate.</summary>
    public SipServerCandidate? Candidate;
    /// <summary>The target quality in thousandths.</summary>
    public int QualityThousandths = 1000;
    /// <summary>The redirect Contact expiry in seconds, or negative one when absent.</summary>
    public int ExpiresSeconds = -1;
    /// <summary>The generated branch identifier.</summary>
    public string? Branch;
    /// <summary>The preferred outbound flow.</summary>
    public SipFlow? Flow;
    /// <summary>The number of redirects followed to reach this target.</summary>
    public int RedirectDepth;
    /// <summary>The Path or Service-Route set used to reach the target.</summary>
    public SipRouteValue[] RouteSet = new SipRouteValue[0];
}
