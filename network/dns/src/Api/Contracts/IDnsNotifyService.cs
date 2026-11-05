namespace dns;

/// <summary>Sends and processes DNS zone-change notifications.</summary>
public interface IDnsNotifyService
{
    /// <summary>Sends one bounded NOTIFY operation to a secondary server.</summary>
    /// <param name="request">The zone, target, serial, retry policy, and authentication.</param>
    /// <returns>The acknowledgement, transport attempts, and diagnostics.</returns>
    DnsNotifyResult SendNotify(DnsNotifyRequest request);

    /// <summary>Processes one received NOTIFY operation from a primary server.</summary>
    /// <param name="request">The zone, primary, serial, authorization, and authentication.</param>
    /// <returns>The acknowledgement and transfer-trigger decision.</returns>
    DnsNotifyResult ProcessNotify(DnsNotifyRequest request);
}
