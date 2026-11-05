namespace sip;

/// <summary>Holds transfer validation, dialog correlation, and authorization results.</summary>
public class SipTransferResult
{
    /// <summary>Indicates that header semantics and required dialog correlation are valid.</summary>
    public bool Valid;
    /// <summary>Indicates that an application authorization policy was evaluated.</summary>
    public bool AuthorizationEvaluated;
    /// <summary>Indicates that application policy authorized the operation.</summary>
    public bool Authorized;
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
    /// <summary>Indicates that Replaces was restricted to an early dialog.</summary>
    public bool EarlyOnly;
    /// <summary>The recommended SIP failure status, or zero when no failure is selected.</summary>
    public int StatusCode;
    /// <summary>The validation or authorization failure reason.</summary>
    public string? ErrorMessage;
    /// <summary>The application authorization decision, when evaluated.</summary>
    public SipAuthorizationDecision? Authorization;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
