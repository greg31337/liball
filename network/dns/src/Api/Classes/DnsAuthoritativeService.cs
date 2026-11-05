namespace dns;

/// <summary>Constructs authoritative DNS answers from a hosted zone store.</summary>
public class DnsAuthoritativeService : IDnsAuthoritativeService
{
    internal IDnsZoneStore ZoneStore;
    internal byte[] CookieSecret;
    internal DnsAuthorizationRateState AuthorizationRateState;

    /// <summary>Creates an authoritative service over one zone store.</summary>
    /// <param name="zoneStore">The hosted zones used to answer requests.</param>
    public DnsAuthoritativeService(IDnsZoneStore zoneStore)
    {
        if (zoneStore == null)
        {
            throw new System.ArgumentNullException("zoneStore");
        }

        this.ZoneStore = zoneStore;
        this.CookieSecret =
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        this.AuthorizationRateState = new DnsAuthorizationRateState();
    }

    /// <summary>Builds one authoritative answer under bounded policy.</summary>
    /// <param name="request">The DNS query, client context, and answer policy.</param>
    /// <returns>The response, answer kind, selected record sets, and diagnostics.</returns>
    public DnsAuthoritativeResult AnswerAuthoritatively(
        DnsAuthoritativeRequest request)
    {
        return AuthoritativeRequestOperations.AnswerRequest(
            request,
            this.ZoneStore,
            this.CookieSecret,
            this.AuthorizationRateState);
    }
}
