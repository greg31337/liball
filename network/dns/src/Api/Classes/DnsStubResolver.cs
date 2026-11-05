namespace dns;

/// <summary>Resolves DNS names through configured recursive servers.</summary>
public class DnsStubResolver : IDnsResolver
{
    internal IDnsQueryClient QueryClient = new DnsQueryClient();
    internal IDnsCache Cache = new MemoryDnsCache();

    /// <summary>Creates a DNS stub resolver.</summary>
    public DnsStubResolver()
    {
    }

    /// <summary>Parses system-style resolver configuration text.</summary>
    /// <param name="source">The complete resolver configuration text.</param>
    /// <returns>The parsed server, search, timeout, and option values.</returns>
    public DnsResolverConfiguration ParseResolverConfiguration(string source)
    {
        return ResolverConfigurationOperations.ParseConfiguration(source);
    }

    /// <summary>Validates and normalizes explicit resolver configuration.</summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>The normalized bounded configuration.</returns>
    public DnsResolverConfiguration ValidateResolverConfiguration(
        DnsResolverConfiguration configuration)
    {
        return ResolverConfigurationOperations.ValidateConfiguration(
            configuration);
    }

    /// <summary>Resolves one DNS name, type, and class.</summary>
    /// <param name="request">The requested name and resolver policy.</param>
    /// <returns>The answer, alias chain, cache state, and diagnostics.</returns>
    public DnsResolveResult Resolve(DnsResolveRequest request)
    {
        return StubResolverOperations.Resolve(request, this);
    }
}
