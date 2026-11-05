namespace asn1;

/// <summary>
/// Contains the status of a canonicality check.
/// </summary>
public class AsnCanonicalityResult
{
    /// <summary>
    /// Contains the canonicality status.
    /// </summary>
    public AsnCanonicalityStatus Status;

    /// <summary>
    /// Indicates whether EncodingRules identifies the checked canonical rules.
    /// </summary>
    public bool HasEncodingRules;

    /// <summary>
    /// Contains the canonical encoding rules checked by this result.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Indicates whether ViolationOffset identifies a canonicality violation.
    /// </summary>
    public bool HasViolationOffset;

    /// <summary>
    /// Contains the zero-based violation offset when HasViolationOffset is true.
    /// </summary>
    public int ViolationOffset;

    /// <summary>
    /// Indicates whether ViolationOffset is measured in bits rather than bytes.
    /// </summary>
    public bool ViolationOffsetIsBit;

    /// <summary>
    /// Indicates whether canonical re-encoding can repair a noncanonical value.
    /// </summary>
    public bool Repairable;

    /// <summary>
    /// Contains the reflected value path associated with the result.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Contains canonicality diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
