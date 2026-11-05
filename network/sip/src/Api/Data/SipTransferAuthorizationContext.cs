namespace sip;

/// <summary>Holds validated transfer information for application authorization.</summary>
public class SipTransferAuthorizationContext
{
    /// <summary>The validated incoming request.</summary>
    public SipMessage? Message;
    /// <summary>The inferred or selected call-control operation.</summary>
    public SipTransferOperation Operation;
    /// <summary>The resource named by Refer-To, when present.</summary>
    public SipNameAddress? ReferTo;
    /// <summary>The identity asserted by Referred-By, when present.</summary>
    public SipNameAddress? ReferredBy;
    /// <summary>The dialog selected by Replaces or Join, when present.</summary>
    public SipDialogSnapshot? ReferencedDialog;
    /// <summary>The dialog selected by Target-Dialog, when it matches.</summary>
    public SipDialogSnapshot? TargetDialog;
    /// <summary>The authenticated initiator identity supplied by the application.</summary>
    public string? AuthenticatedIdentity;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
