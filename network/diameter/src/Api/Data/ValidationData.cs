using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Describes one command or AVP validation failure.
/// </summary>
public class DiameterValidationIssue
{
    /// <summary>The issue severity.</summary>
    public DiameterIssueSeverity Severity;

    /// <summary>The stable machine-readable issue code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable issue description.</summary>
    public string Message = string.Empty;

    /// <summary>The Diameter Result-Code corresponding to the failure.</summary>
    public uint ResultCode;

    /// <summary>The AVP code associated with the issue, or zero when none applies.</summary>
    public uint AvpCode;

    /// <summary>The AVP Vendor-ID associated with the issue.</summary>
    public uint AvpVendorId;

    /// <summary>The enclosing Grouped AVP code, or zero for a message-level issue.</summary>
    public uint ParentAvpCode;

    /// <summary>The enclosing Grouped AVP Vendor-ID.</summary>
    public uint ParentAvpVendorId;

    /// <summary>The zero-based AVP position, or <c>-1</c> when no AVP was received.</summary>
    public int AvpIndex = -1;

    /// <summary>The Grouped nesting depth of the issue.</summary>
    public int Depth;

    /// <summary>The required minimum occurrence count.</summary>
    public int ExpectedMinimum;

    /// <summary>The allowed maximum occurrence count, or <c>-1</c> when unlimited.</summary>
    public int ExpectedMaximum = -1;

    /// <summary>The observed occurrence count or enumerated value.</summary>
    public int ActualCount;

    /// <summary>The required payload length, or <c>-1</c> when variable.</summary>
    public int ExpectedLength = -1;

    /// <summary>The observed payload length, or <c>-1</c> when unavailable.</summary>
    public int ActualLength = -1;

    /// <summary>The required AVP position, or <c>-1</c> when unconstrained.</summary>
    public int ExpectedPosition = -1;

    /// <summary>The required Diameter message flags.</summary>
    public DiameterMessageFlags ExpectedMessageFlags;

    /// <summary>The observed Diameter message flags.</summary>
    public DiameterMessageFlags ActualMessageFlags;

    /// <summary>The required or prohibited Diameter AVP flags.</summary>
    public DiameterAvpFlags ExpectedAvpFlags;

    /// <summary>The observed Diameter AVP flags.</summary>
    public DiameterAvpFlags ActualAvpFlags;

    /// <summary>The received AVP associated with the issue, when available.</summary>
    public DiameterAvp? OffendingAvp;

    /// <summary>The complete Failed-AVP wrapper appropriate for this issue.</summary>
    public DiameterAvp? FailedAvp;
}

/// <summary>
/// Contains the information required to construct a validation error answer.
/// </summary>
public class DiameterErrorAnswerRequest
{
    /// <summary>The request that failed validation.</summary>
    public DiameterMessage? Request;

    /// <summary>The first validation issue to report.</summary>
    public DiameterValidationIssue? Issue;

    /// <summary>The Result-Code to place in the error answer.</summary>
    public uint ResultCode;

    /// <summary>The Experimental-Result used instead of Result-Code, or <c>null</c>.</summary>
    public DiameterExperimentalResult? ExperimentalResult;

    /// <summary>The diagnostic Error-Message text.</summary>
    public string ErrorMessage = string.Empty;

    /// <summary>The host that set the failure when it differs from Origin-Host, or <c>null</c>.</summary>
    public DiameterIdentity? ErrorReportingHost;

    /// <summary>The Failed-AVP wrapper, or <c>null</c> when the error is not AVP-specific.</summary>
    public DiameterAvp? FailedAvp;
}

/// <summary>
/// Contains the complete outcome of validating a Diameter message or Grouped AVP.
/// </summary>
public class DiameterValidationResult
{
    /// <summary>The overall validation status.</summary>
    public DiameterValidationStatus Status;

    /// <summary>The validation issues in deterministic processing order.</summary>
    public List<DiameterValidationIssue> Issues = new List<DiameterValidationIssue>();

    /// <summary>The error-answer input derived from the first issue.</summary>
    public DiameterErrorAnswerRequest? ErrorAnswer;
}
