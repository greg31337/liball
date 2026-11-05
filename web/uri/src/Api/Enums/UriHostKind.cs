namespace uri;

/// <summary>
/// Identifies the grammar used by an authority host.
/// </summary>
public enum UriHostKind
{
    /// <summary>No authority host is present.</summary>
    Absent = 0,

    /// <summary>The host is a registered name, including a present empty host.</summary>
    RegisteredName = 1,

    /// <summary>The host is a strict four-octet IPv4 address.</summary>
    Ipv4 = 2,

    /// <summary>The host is an IPv6 address enclosed by an IP-literal.</summary>
    Ipv6 = 3,

    /// <summary>The host is an IPvFuture address enclosed by an IP-literal.</summary>
    IpvFuture = 4
}
