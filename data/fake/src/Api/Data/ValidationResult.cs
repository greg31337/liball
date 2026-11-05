namespace fake;

/// <summary>
/// Reports independent validity, safety, allocation, and correlation dimensions.
/// </summary>
public class ValidationResult
{
    /// <summary>The identifier contract used for validation when the value is an identifier.</summary>
    public IdentifierKind Kind;

    /// <summary>True when every applicable validity dimension passed.</summary>
    public bool Valid;

    /// <summary>Whether length, characters, separators, and required prefixes are structurally valid.</summary>
    public ValidityMode StructuralValidity;

    /// <summary>Whether an applicable check digit is valid, not applicable, or unavailable due to malformed syntax.</summary>
    public ValidityMode ChecksumValidity;

    /// <summary>Whether kind-specific constraints such as UUID version or ULID range are valid.</summary>
    public ValidityMode ConstraintValidity;

    /// <summary>Whether fields in a generated result satisfy their documented cross-field relationships.</summary>
    public ValidityMode CorrelationValidity;

    /// <summary>True only for generic project-defined fictional fixture namespaces.</summary>
    public bool FictionalReserved;

    /// <summary>Whether the value is confined to an authoritative or project-defined safe fixture range.</summary>
    public bool SafeReserved;

    /// <summary>The library's allocation or fictional-reservation knowledge.</summary>
    public AllocationStatus AllocationStatus;

    /// <summary>A concise explanation of the first failing or unavailable dimension.</summary>
    public string Detail = string.Empty;

    /// <summary>The requested or selected edge-case name, or an empty string when no edge was requested.</summary>
    public string RequestedEdgeCase = string.Empty;

    /// <summary>The rule deliberately violated by an invalid or adversarial fixture, or an empty string otherwise.</summary>
    public string ViolatedRule = string.Empty;

    internal ValidationResult()
    {
        Kind = IdentifierKind.Uuid;
        Valid = false;
        StructuralValidity = ValidityMode.Unknown;
        ChecksumValidity = ValidityMode.Unknown;
        ConstraintValidity = ValidityMode.Unknown;
        CorrelationValidity = ValidityMode.NotApplicable;
        FictionalReserved = false;
        SafeReserved = false;
        AllocationStatus = AllocationStatus.Unknown;
        Detail = string.Empty;
        RequestedEdgeCase = string.Empty;
        ViolatedRule = string.Empty;
    }
}
