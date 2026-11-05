namespace dns;

/// <summary>Produces authorized and authenticated full or incremental zone transfers.</summary>
public class DnsTransferService : IDnsTransferService
{
    internal IDnsZoneStore ZoneStore;
    internal DnsAuthorizationRateState AuthorizationRateState;

    /// <summary>Creates a zone-transfer service over one zone store.</summary>
    /// <param name="zoneStore">The hosted zones served to authorized clients.</param>
    public DnsTransferService(IDnsZoneStore zoneStore)
    {
        if (zoneStore == null)
        {
            throw new System.ArgumentNullException("zoneStore");
        }

        this.ZoneStore = zoneStore;
        this.AuthorizationRateState = new DnsAuthorizationRateState();
    }

    /// <summary>Serves one authorized, bounded AXFR or IXFR record stream.</summary>
    /// <param name="request">The zone, client, limits, and optional TSIG request.</param>
    /// <returns>The ordered SOA-bounded transfer messages and diagnostics.</returns>
    public DnsTransferResult ServeTransfer(DnsTransferRequest request)
    {
        return TransferServerOperations.ServeTransfer(
            request,
            this.ZoneStore,
            this.AuthorizationRateState);
    }
}
