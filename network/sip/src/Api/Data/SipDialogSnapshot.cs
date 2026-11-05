namespace sip;

/// <summary>Holds an immutable-style snapshot of dialog state.</summary>
public class SipDialogSnapshot
{
    /// <summary>The dialog identifier.</summary>
    public SipDialogId? Id;
    /// <summary>The local dialog role.</summary>
    public SipDialogRole Role;
    /// <summary>The dialog state.</summary>
    public SipDialogState State;
    /// <summary>The local URI.</summary>
    public SipUri? LocalUri;
    /// <summary>The remote URI.</summary>
    public SipUri? RemoteUri;
    /// <summary>The local target URI.</summary>
    public SipUri? LocalTarget;
    /// <summary>The remote target URI.</summary>
    public SipUri? RemoteTarget;
    /// <summary>The immutable route set.</summary>
    public SipRouteValue[] RouteSet = new SipRouteValue[0];
    /// <summary>The latest local CSeq, or negative one when absent.</summary>
    public int LocalCSeq = -1;
    /// <summary>The latest remote CSeq, or negative one when absent.</summary>
    public int RemoteCSeq = -1;
    /// <summary>Indicates a secure dialog.</summary>
    public bool Secure;
    /// <summary>The attached usage identifiers.</summary>
    public string[] UsageIds = new string[0];
    /// <summary>The associated live transaction identifiers.</summary>
    public string[] TransactionIds = new string[0];
    /// <summary>The associated authentication context identifier.</summary>
    public string? AuthenticationContextId;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
