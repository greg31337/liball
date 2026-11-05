namespace dns;

/// <summary>Sends and processes authenticated DNS zone notifications.</summary>
public class DnsNotifyService : IDnsNotifyService
{
    internal IDnsZoneStore ZoneStore;
    internal IDnsQueryClient QueryClient;
    internal IDnsTransferClient TransferClient;
    internal DnsAuthorizationRateState AuthorizationRateState;

    /// <summary>Creates a notification service over zone, query, and transfer operations.</summary>
    /// <param name="zoneStore">The secondary zones used for serial comparison.</param>
    /// <param name="queryClient">The client used for NOTIFY and SOA exchanges.</param>
    /// <param name="transferClient">The client used to trigger required zone transfers.</param>
    public DnsNotifyService(
        IDnsZoneStore zoneStore,
        IDnsQueryClient queryClient,
        IDnsTransferClient transferClient)
    {
        if (zoneStore == null || queryClient == null ||
            transferClient == null)
        {
            throw new System.ArgumentNullException("service");
        }

        this.ZoneStore = zoneStore;
        this.QueryClient = queryClient;
        this.TransferClient = transferClient;
        this.AuthorizationRateState = new DnsAuthorizationRateState();
    }

    /// <summary>Sends one bounded NOTIFY operation to a secondary server.</summary>
    /// <param name="request">The zone, target, serial, retry policy, and authentication.</param>
    /// <returns>The acknowledgement, transport attempts, and diagnostics.</returns>
    public DnsNotifyResult SendNotify(DnsNotifyRequest request)
    {
        return NotifyOperations.SendNotify(
            request,
            this.QueryClient,
            this.AuthorizationRateState);
    }

    /// <summary>Processes one received NOTIFY operation from a primary server.</summary>
    /// <param name="request">The zone, primary, serial, authorization, and authentication.</param>
    /// <returns>The acknowledgement and transfer-trigger decision.</returns>
    public DnsNotifyResult ProcessNotify(DnsNotifyRequest request)
    {
        return NotifyOperations.ProcessNotify(
            request,
            this.ZoneStore,
            this.QueryClient,
            this.TransferClient,
            this.AuthorizationRateState);
    }
}
