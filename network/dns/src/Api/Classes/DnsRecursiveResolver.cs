namespace dns;

/// <summary>Resolves DNS names iteratively from validated root hints.</summary>
public class DnsRecursiveResolver : IDnsRecursiveResolver
{
    internal IDnsCache Cache = new MemoryDnsCache();
    internal IDnsQueryClient QueryClient = new DnsQueryClient();
    internal IDnsIterativeEntropy Entropy = new DnsIterativeEntropy();
    internal IDnsIterativeClock Clock = new DnsIterativeClock();
    internal System.Collections.Generic.List<DnsServerState> ServerStates =
        new System.Collections.Generic.List<DnsServerState>();
    internal System.Collections.Generic.List<DnsIterativeReachabilityState>
        ReachabilityStates =
            new System.Collections.Generic.List<DnsIterativeReachabilityState>();
    internal System.Collections.Generic.List<DnsAggressiveDenialEntry>
        DenialEntries =
            new System.Collections.Generic.List<DnsAggressiveDenialEntry>();

    /// <summary>Creates an iterative resolver with an empty delegation cache.</summary>
    public DnsRecursiveResolver()
    {
    }

    /// <summary>Loads and caches root hints from a caller-provided root zone.</summary>
    /// <param name="zone">The root NS and name-server address records.</param>
    /// <param name="source">The caller-provided source description.</param>
    /// <returns>The normalized validated root hints.</returns>
    public DnsRootHints LoadRootHints(DnsZone zone, string source)
    {
        DnsRootHints result = RootHintOperations.LoadRootHints(zone, source);
        RootHintOperations.SeedCache(result, this.Cache);
        return result;
    }

    /// <summary>Validates and normalizes caller-provided root hints.</summary>
    /// <param name="rootHints">The root hints to validate.</param>
    /// <returns>An independent validated copy.</returns>
    public DnsRootHints ValidateRootHints(DnsRootHints rootHints)
    {
        return RootHintOperations.ValidateRootHints(rootHints);
    }

    /// <summary>Selects the closest known cached delegation for a name.</summary>
    /// <param name="name">The absolute name that will be resolved.</param>
    /// <param name="rootHints">The validated fallback root delegation.</param>
    /// <returns>The closest usable cached delegation or the root fallback.</returns>
    public DnsDelegation SelectClosestDelegation(
        DnsName name,
        DnsRootHints rootHints)
    {
        DnsRootHints validated = RootHintOperations.ValidateRootHints(
            rootHints);
        return DelegationSelectionOperations.SelectClosestDelegation(
            name,
            validated,
            this.Cache);
    }

    /// <summary>Performs one bounded iterative resolution.</summary>
    /// <param name="request">The requested name, type, and class.</param>
    /// <param name="rootHints">The initial root name-server delegation.</param>
    /// <param name="options">The recursion, hardening, and protocol limits.</param>
    /// <returns>The query outcome, selected authorities, and referral metadata.</returns>
    public DnsResolveResult ResolveIteratively(
        DnsResolveRequest request,
        DnsRootHints rootHints,
        DnsRecursiveOptions options)
    {
        return IterativeResolverOperations.ResolveIteratively(
            request,
            rootHints,
            options,
            this);
    }
}
