namespace fake;

/// <summary>
/// Captures generation settings and validation classifications for one generated or inspected value.
/// </summary>
public class GenerationMetadata
{
    /// <summary>The textual value or stable case name associated with this metadata.</summary>
    public string Value = string.Empty;

    /// <summary>The generated value without coercing its runtime type, or null when the value itself is null.</summary>
    public object? GeneratedValue;

    /// <summary>A stable provider-defined name for the value contract.</summary>
    public string ValueKind = string.Empty;

    /// <summary>The provider that owns the generation or inspection operation.</summary>
    public string Provider = string.Empty;

    /// <summary>The identifier contract used for classification when the value is an identifier.</summary>
    public IdentifierKind Kind;

    /// <summary>The captured requested locale.</summary>
    public string Locale = string.Empty;

    /// <summary>The locale whose authoritative rule supplied the identifier contract.</summary>
    public string SourceLocale = string.Empty;

    /// <summary>Whether locale fallback selected the identifier rule.</summary>
    public bool UsedLocaleFallback;

    /// <summary>The stable identifier of the applied locale rule.</summary>
    public string Rule = string.Empty;

    /// <summary>A concise description of the accepted identifier format.</summary>
    public string Format = string.Empty;

    /// <summary>The source review description attached to the locale rule.</summary>
    public string Provenance = string.Empty;

    /// <summary>Whether the metadata claims external assignment, always <c>false</c>.</summary>
    public bool RepresentsAssignment;

    /// <summary>The captured generation mode.</summary>
    public GenerationMode GenerationMode;

    /// <summary>The captured safety mode.</summary>
    public SafetyMode SafetyMode;

    /// <summary>The captured locale fallback mode.</summary>
    public StrictnessMode StrictnessMode;

    /// <summary>The captured Unicode normalization mode.</summary>
    public NormalizationMode NormalizationMode;

    /// <summary>Whether the associated value has valid structure.</summary>
    public ValidityMode StructuralValidity;

    /// <summary>Whether an applicable checksum is valid.</summary>
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

    /// <summary>The complete detailed validation result.</summary>
    public ValidationResult Validation;

    /// <summary>The requested or selected edge-case name, or an empty string when no edge was requested.</summary>
    public string RequestedEdgeCase = string.Empty;

    /// <summary>The rule deliberately violated by an invalid or adversarial fixture, or an empty string otherwise.</summary>
    public string ViolatedRule = string.Empty;

    internal GenerationMetadata()
    {
        Value = string.Empty;
        GeneratedValue = null;
        ValueKind = string.Empty;
        Provider = string.Empty;
        Kind = IdentifierKind.Uuid;
        Locale = string.Empty;
        SourceLocale = string.Empty;
        UsedLocaleFallback = false;
        Rule = string.Empty;
        Format = string.Empty;
        Provenance = string.Empty;
        RepresentsAssignment = false;
        GenerationMode = GenerationMode.Typical;
        SafetyMode = SafetyMode.Reserved;
        StrictnessMode = StrictnessMode.AllowFallback;
        NormalizationMode = NormalizationMode.None;
        StructuralValidity = ValidityMode.Unknown;
        ChecksumValidity = ValidityMode.Unknown;
        ConstraintValidity = ValidityMode.Unknown;
        CorrelationValidity = ValidityMode.NotApplicable;
        FictionalReserved = false;
        SafeReserved = false;
        AllocationStatus = AllocationStatus.Unknown;
        Validation = new ValidationResult();
        RequestedEdgeCase = string.Empty;
        ViolatedRule = string.Empty;
    }
}
