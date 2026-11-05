namespace diameter;

/// <summary>Discovers configured or DNS-derived Diameter peers.</summary>
public interface IDiameterPeerDiscovery
{
    /// <summary>Discovers peers for a realm, application, and transport.</summary>
    /// <param name="request">The peer discovery requirements.</param>
    /// <param name="timeoutMilliseconds">The positive total discovery timeout.</param>
    /// <returns>The configured or dynamically admitted discovery outcome.</returns>
    DiameterDiscoveryResult DiscoverDiameterPeers(
        DiameterDiscoveryRequest? request,
        int timeoutMilliseconds);

    /// <summary>Invalidates cached DNS and dynamic peers for a realm.</summary>
    /// <param name="realm">The realm whose discovery state is invalidated.</param>
    void InvalidateDiameterDiscoveryCache(DiameterIdentity realm);
}

/// <summary>Resolves DNS records used by Diameter peer discovery.</summary>
public interface IDnsResolver
{
    /// <summary>Resolves Naming Authority Pointer records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    DiameterDnsQueryResult ResolveDiameterNaptrRecords(
        string name,
        int timeoutMilliseconds);

    /// <summary>Resolves Service Locator records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    DiameterDnsQueryResult ResolveDiameterSrvRecords(
        string name,
        int timeoutMilliseconds);

    /// <summary>Resolves IPv4 address records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    DiameterDnsQueryResult ResolveDiameterIpv4Records(
        string name,
        int timeoutMilliseconds);

    /// <summary>Resolves IPv6 address records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    DiameterDnsQueryResult ResolveDiameterIpv6Records(
        string name,
        int timeoutMilliseconds);
}
