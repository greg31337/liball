namespace dns;

/// <summary>Constructs authoritative DNS answers from hosted zone data.</summary>
public interface IDnsAuthoritativeService
{
    /// <summary>Builds one authoritative answer under bounded policy.</summary>
    /// <param name="request">The DNS query, client context, and answer policy.</param>
    /// <returns>The response, answer kind, selected record sets, and diagnostics.</returns>
    DnsAuthoritativeResult AnswerAuthoritatively(DnsAuthoritativeRequest request);
}
