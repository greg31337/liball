namespace sdp;

/// <summary>
/// Identifies the interpreted form of an SDP connection address.
/// </summary>
public enum SdpAddressKind
{
    /// <summary>The address form is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The address is a host name or externally interpreted name.</summary>
    HostName = 1,

    /// <summary>The address is an IPv4 unicast address.</summary>
    Ipv4 = 2,

    /// <summary>The address is an IPv6 unicast address.</summary>
    Ipv6 = 3,

    /// <summary>The address is an IPv4 multicast address.</summary>
    Ipv4Multicast = 4,

    /// <summary>The address is an IPv6 multicast address.</summary>
    Ipv6Multicast = 5
}
