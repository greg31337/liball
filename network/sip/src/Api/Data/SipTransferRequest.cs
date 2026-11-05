namespace sip;

/// <summary>Describes a request to validate, correlate, and authorize call control.</summary>
public class SipTransferRequest
{
    /// <summary>The incoming REFER, INVITE, or other dialog-forming request.</summary>
    public SipMessage? Message;
    /// <summary>The dialog snapshots available for correlation.</summary>
    public SipDialogSnapshot[] Dialogs = new SipDialogSnapshot[0];
    /// <summary>The expected operation, or none to infer it from the request.</summary>
    public SipTransferOperation Operation;
    /// <summary>Requires a valid Referred-By header field.</summary>
    public bool RequireReferredBy;
    /// <summary>The authenticated initiator identity supplied to application policy.</summary>
    public string? AuthenticatedIdentity;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
