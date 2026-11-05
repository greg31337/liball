namespace fake;

/// <summary>
/// Selects an IP address category.
/// </summary>
public enum NetworkAddressKind
{
    /// <summary>
    /// Uses an address block reserved for documentation for the selected IP version.
    /// </summary>
    Documentation = 0,

    /// <summary>
    /// Uses an IPv4 private-use or IPv6 unique-local address block.
    /// </summary>
    Private = 1,

    /// <summary>
    /// Uses a loopback address or block.
    /// </summary>
    Loopback = 2,

    /// <summary>
    /// Uses a link-local address block.
    /// </summary>
    LinkLocal = 3,

    /// <summary>
    /// Uses a multicast address block.
    /// </summary>
    Multicast = 4,

    /// <summary>
    /// Uses a public-unicast-shaped block and makes no allocation or reachability claim.
    /// </summary>
    Public = 5
}
