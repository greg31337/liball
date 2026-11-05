namespace dns;

/// <summary>Validates DNSSEC data and signs caller-supplied zones or record sets.</summary>
public interface IDnsDnssecService
{
    /// <summary>Validates the DNSSEC data and chain of trust in one message.</summary>
    /// <param name="message">The DNS message to validate.</param>
    /// <param name="policy">The trust anchors, algorithms, times, and work limits.</param>
    /// <returns>The security state, proofs, and diagnostics.</returns>
    DnsSecurityResult ValidateDnssec(DnsMessage message, DnsSecurityPolicy policy);

    /// <summary>Signs caller-supplied record sets or a complete zone.</summary>
    /// <param name="request">The data, keys, times, and denial policy.</param>
    /// <returns>The generated security records and diagnostics.</returns>
    DnsSecurityResult SignDnssec(DnsSignatureRequest request);
}
