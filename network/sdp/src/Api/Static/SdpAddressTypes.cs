namespace sdp;

/// <summary>
/// Defines registered SDP address-type tokens.
/// </summary>
public static class SdpAddressTypes
{
    /// <summary>The Internet Protocol version 4 address token.</summary>
    public const string Ip4 = "IP4";

    /// <summary>The Internet Protocol version 6 address token.</summary>
    public const string Ip6 = "IP6";

    /// <summary>The ATM network service access point address token.</summary>
    public const string NetworkServiceAccessPoint = "NSAP";

    /// <summary>The ATM gateway identifier address token.</summary>
    public const string GatewayIdentifier = "GWID";

    /// <summary>The E.164 telephone-number address token.</summary>
    public const string E164 = "E164";
}
