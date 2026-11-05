namespace sip;

/// <summary>Resolves SIP and SIPS server targets using RFC 3263 DNS processing.</summary>
public class SipDnsResolver : ISipDnsResolver
{
    SipDnsResolutionEngine engine;

    /// <summary>Initializes a SIP DNS resolver.</summary>
    public SipDnsResolver()
    {
        engine = new SipDnsResolutionEngine(new SipCryptographicRandomSource(),
            new SipSystemMonotonicClock());
    }

    /// <summary>Resolves one SIP target.</summary>
    /// <param name="request">The resolution request.</param>
    /// <param name="options">The resolver options.</param>
    /// <returns>The ordered resolution result.</returns>
    public SipResolutionResult ResolveTarget(SipResolutionRequest request, SipDnsOptions options)
    {
        return engine.Resolve(request, options);
    }

    /// <summary>Reports a failed candidate so later results can prefer available alternatives.</summary>
    /// <param name="candidate">The candidate that failed.</param>
    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
        engine.ReportFailure(candidate);
    }

    /// <summary>Flushes cached resolver state.</summary>
    public void FlushResolverCache()
    {
        engine.Flush();
    }

    internal void ConfigureResolutionSources(ISipRandomSource? random,
        ISipMonotonicClock? clock)
    {
        if (random == null || clock == null)
        {
            throw new System.ArgumentNullException("random");
        }
        engine = new SipDnsResolutionEngine(random, clock);
    }
}
