namespace dns;

/// <summary>Validates and atomically applies dynamic updates to a zone store.</summary>
public class DnsUpdateService : IDnsUpdateService
{
    internal IDnsZoneStore ZoneStore;
    internal DnsAuthorizationRateState AuthorizationRateState;

    /// <summary>Creates a dynamic-update service over one zone store.</summary>
    /// <param name="zoneStore">The hosted zones changed by accepted updates.</param>
    public DnsUpdateService(IDnsZoneStore zoneStore)
    {
        if (zoneStore == null)
        {
            throw new System.ArgumentNullException("zoneStore");
        }

        this.ZoneStore = zoneStore;
        this.AuthorizationRateState = new DnsAuthorizationRateState();
    }

    /// <summary>Applies one authenticated and authorized update request.</summary>
    /// <param name="request">The zone, prerequisites, mutations, and security policy.</param>
    /// <returns>The response code, committed transaction, and diagnostics.</returns>
    public DnsUpdateResult ApplyUpdate(DnsUpdateRequest request)
    {
        return UpdateApplicationOperations.ApplyUpdate(
            request,
            this.ZoneStore,
            this.AuthorizationRateState);
    }
}
