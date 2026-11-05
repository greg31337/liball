namespace whois;

/// <summary>
/// Specifies the preferred address family order for a WHOIS connection.
/// </summary>
public enum WhoisAddressPreference
{
    /// <summary>
    /// Preserves the address order supplied by the resolver.
    /// </summary>
    System = 0,

    /// <summary>
    /// Tries IPv4 addresses before IPv6 addresses.
    /// </summary>
    IPv4First = 1,

    /// <summary>
    /// Tries IPv6 addresses before IPv4 addresses.
    /// </summary>
    IPv6First = 2
}
