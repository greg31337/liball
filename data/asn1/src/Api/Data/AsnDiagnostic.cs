namespace asn1;

/// <summary>
/// Describes a compiler or runtime diagnostic and its source relationships.
/// </summary>
public class AsnDiagnostic
{
    /// <summary>
    /// Contains the diagnostic severity.
    /// </summary>
    public AsnDiagnosticSeverity Severity;

    /// <summary>
    /// Contains the diagnostic category.
    /// </summary>
    public AsnDiagnosticCategory Category;

    /// <summary>
    /// Contains the stable diagnostic code.
    /// </summary>
    public string Code = "";

    /// <summary>
    /// Contains the diagnostic message.
    /// </summary>
    public string Message = "";

    /// <summary>
    /// Contains the stable violated-constraint identity for runtime diagnostics.
    /// </summary>
    public string ConstraintId = "";

    /// <summary>
    /// Contains the violated constraint category for runtime diagnostics.
    /// </summary>
    public AsnConstraintKind ConstraintKind;

    /// <summary>
    /// Contains the path to the runtime value that caused the diagnostic.
    /// </summary>
    public string ValuePath = "";

    /// <summary>
    /// Contains the stable executable plan-step identity associated with the diagnostic.
    /// </summary>
    public string PlanStepId = "";

    /// <summary>
    /// Indicates whether BitOffset identifies a location in encoded binary input.
    /// </summary>
    public bool HasBitOffset;

    /// <summary>
    /// Contains the zero-based encoded-input bit offset when HasBitOffset is true.
    /// </summary>
    public int BitOffset;

    /// <summary>
    /// Contains the primary source span when the diagnostic has one.
    /// </summary>
    public AsnSourceSpan? Span;

    /// <summary>
    /// Contains source locations related to the primary diagnostic.
    /// </summary>
    public AsnDiagnosticRelatedLocation[] RelatedLocations = [];
}
