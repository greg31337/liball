namespace sip;

/// <summary>Resolves SIP and ENUM targets and manages resolver cache state.</summary>
public interface ISipDnsResolver
{
    /// <summary>Resolves one SIP target.</summary>
    /// <param name="request">The resolution request.</param>
    /// <param name="options">The resolver options.</param>
    /// <returns>The ordered resolution result.</returns>
    SipResolutionResult ResolveTarget(SipResolutionRequest request, SipDnsOptions options);
    /// <summary>Reports a failed candidate so later results can prefer available alternatives.</summary>
    /// <param name="candidate">The candidate that failed.</param>
    void ReportCandidateFailure(SipServerCandidate candidate);
    /// <summary>Flushes cached resolver state.</summary>
    void FlushResolverCache();
}
