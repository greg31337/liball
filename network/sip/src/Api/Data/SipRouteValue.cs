namespace sip;

/// <summary>
/// Holds one Route or Record-Route header field value.
/// </summary>
public class SipRouteValue
{
    /// <summary>The route name-address.</summary>
    public SipNameAddress? Address;

    /// <summary>Indicates that the route URI contains the loose-routing parameter.</summary>
    public bool LooseRouting;
}
