namespace dns;

/// <summary>Resolves DNS names through configured recursive servers.</summary>
public interface IDnsResolver
{
    /// <summary>Parses system-style resolver configuration text.</summary>
    /// <param name="source">The complete resolver configuration text.</param>
    /// <returns>The parsed server, search, timeout, and option values.</returns>
    DnsResolverConfiguration ParseResolverConfiguration(string source);

    /// <summary>Validates and normalizes explicit resolver configuration.</summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>The normalized bounded configuration.</returns>
    DnsResolverConfiguration ValidateResolverConfiguration(DnsResolverConfiguration configuration);

    /// <summary>Resolves one DNS name, type, and class.</summary>
    /// <param name="request">The requested name and resolver policy.</param>
    /// <returns>The answer, alias chain, cache state, and diagnostics.</returns>
    DnsResolveResult Resolve(DnsResolveRequest request);
}
