namespace dns;

/// <summary>Resolves DNS names iteratively from configured root hints.</summary>
public interface IDnsRecursiveResolver
{
    /// <summary>Loads and caches root hints from a caller-provided root zone.</summary>
    /// <param name="zone">The root NS and name-server address records.</param>
    /// <param name="source">The caller-provided source description.</param>
    /// <returns>The normalized validated root hints.</returns>
    DnsRootHints LoadRootHints(DnsZone zone, string source);

    /// <summary>Validates and normalizes caller-provided root hints.</summary>
    /// <param name="rootHints">The root hints to validate.</param>
    /// <returns>An independent validated copy.</returns>
    DnsRootHints ValidateRootHints(DnsRootHints rootHints);

    /// <summary>Selects the closest known cached delegation for a name.</summary>
    /// <param name="name">The absolute name that will be resolved.</param>
    /// <param name="rootHints">The validated fallback root delegation.</param>
    /// <returns>The closest usable cached delegation or the root fallback.</returns>
    DnsDelegation SelectClosestDelegation(
        DnsName name,
        DnsRootHints rootHints);

    /// <summary>Performs one bounded iterative resolution.</summary>
    /// <param name="request">The requested name, type, and class.</param>
    /// <param name="rootHints">The initial root name-server delegation.</param>
    /// <param name="options">The recursion, hardening, and protocol limits.</param>
    /// <returns>The answer, followed delegations, and validation metadata.</returns>
    DnsResolveResult ResolveIteratively(DnsResolveRequest request, DnsRootHints rootHints, DnsRecursiveOptions options);
}
