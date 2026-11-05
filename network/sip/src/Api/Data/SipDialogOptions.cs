namespace sip;

/// <summary>Holds dialog policy and resource limits.</summary>
public class SipDialogOptions
{
    /// <summary>The local role used to orient dialog identifiers and state.</summary>
    public SipDialogRole Role = SipDialogRole.Uac;
    /// <summary>The maximum live dialog count.</summary>
    public int MaxDialogCount = SipDefaults.MaxDialogCount;
    /// <summary>The maximum usages attached to one dialog.</summary>
    public int MaxUsageCount = 32;
    /// <summary>The maximum live transactions associated with one dialog.</summary>
    public int MaxTransactionCount = 64;
    /// <summary>The maximum queued dialog event and snapshot count.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
    /// <summary>Requires strict remote CSeq ordering.</summary>
    public bool StrictRemoteCSeq = true;
    /// <summary>Prevents SIPS downgrade.</summary>
    public bool PreventSecureDowngrade = true;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
