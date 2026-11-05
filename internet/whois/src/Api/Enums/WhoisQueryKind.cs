namespace whois;

/// <summary>
/// Identifies the kind of resource represented by a WHOIS query.
/// </summary>
public enum WhoisQueryKind
{
    /// <summary>
    /// Requests automatic classification of the caller text.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// A domain name query.
    /// </summary>
    Domain = 1,

    /// <summary>
    /// An IPv4 address query.
    /// </summary>
    IPv4Address = 2,

    /// <summary>
    /// An IPv6 address query.
    /// </summary>
    IPv6Address = 3,

    /// <summary>
    /// An IPv4 network prefix query.
    /// </summary>
    IPv4Prefix = 4,

    /// <summary>
    /// An IPv6 network prefix query.
    /// </summary>
    IPv6Prefix = 5,

    /// <summary>
    /// An autonomous system number query.
    /// </summary>
    AutonomousSystem = 6,

    /// <summary>
    /// An IPv4 or IPv6 reverse-DNS name query.
    /// </summary>
    ReverseDomain = 7,

    /// <summary>
    /// A caller-defined query that is not automatically routed.
    /// </summary>
    FreeForm = 8
}
