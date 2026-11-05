namespace dns;

/// <summary>Validates DNSSEC messages and signs caller-supplied zones or record sets.</summary>
public class DnsDnssecService : IDnsDnssecService
{
    /// <summary>Creates a stateless DNSSEC service.</summary>
    public DnsDnssecService()
    {
    }

    /// <summary>Validates the DNSSEC data and chain of trust in one message.</summary>
    /// <param name="message">The DNS message to validate.</param>
    /// <param name="policy">The trust anchors, algorithms, times, and work limits.</param>
    /// <returns>The security state, proofs, and diagnostics.</returns>
    public DnsSecurityResult ValidateDnssec(
        DnsMessage message,
        DnsSecurityPolicy policy)
    {
        return DnssecMessageValidationOperations.ValidateMessage(
            message,
            policy);
    }

    /// <summary>Signs caller-supplied record sets or a complete zone without retaining private keys.</summary>
    /// <param name="request">The data, caller-owned keys, times, and denial policy.</param>
    /// <returns>The generated signatures, denial records, and diagnostics.</returns>
    public DnsSecurityResult SignDnssec(DnsSignatureRequest request)
    {
        return DnssecSigningOperations.SignDnssec(request);
    }
}
