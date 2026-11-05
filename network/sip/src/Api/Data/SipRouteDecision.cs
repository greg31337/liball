namespace sip;

/// <summary>Holds an application routing-policy decision.</summary>
public class SipRouteDecision
{
    /// <summary>The selected proxy mode.</summary>
    public SipProxyMode Mode;
    /// <summary>The ordered routing targets.</summary>
    public SipProxyTarget[] Targets = new SipProxyTarget[0];
    /// <summary>Indicates that a redirect response should be generated.</summary>
    public bool Redirect;
    /// <summary>The redirect response status code.</summary>
    public int RedirectStatusCode = SipStatusCodes.MovedTemporarily;
    /// <summary>The rejection status code, or zero when accepted.</summary>
    public int RejectStatusCode;
    /// <summary>The decision reason.</summary>
    public string? Reason;
    /// <summary>Requests Record-Route insertion.</summary>
    public bool RecordRoute;
}
