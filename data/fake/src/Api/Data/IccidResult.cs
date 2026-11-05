namespace fake;

/// <summary>Contains an ICCID-shaped value, its safely parseable components, and independent validity metadata.</summary>
/// <remarks>Component parsing and checksum validation do not establish issuer allocation, card existence, ownership, or usability.</remarks>
public class IccidResult
{
    /// <summary>The complete ICCID-shaped value.</summary>
    public string Value = string.Empty;

    /// <summary>Every digit before the trailing check digit, or an empty string when structure is invalid.</summary>
    public string Payload = string.Empty;

    /// <summary>The telecommunications major industry identifier <c>89</c>, or an empty string when structure is invalid.</summary>
    public string MajorIndustryIdentifier = string.Empty;

    /// <summary>The payload after the major industry identifier, whose issuer and account boundary requires external allocation data.</summary>
    public string IssuerAndAccountIdentifier = string.Empty;

    /// <summary>The trailing Luhn check digit, or an empty string when structure is invalid.</summary>
    public string CheckDigit = string.Empty;

    /// <summary>Whether length, characters, and the telecommunications prefix are structurally valid.</summary>
    public ValidityMode StructuralValidity;

    /// <summary>Whether the trailing Luhn check digit is valid.</summary>
    public ValidityMode ChecksumValidity;

    /// <summary>Whether the library generated the value synthetically.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the result claims an external assignment, always <c>false</c>.</summary>
    public bool RepresentsAssignment;

    /// <summary>The library's allocation knowledge, always <see cref="AllocationStatus.Unknown"/>.</summary>
    public AllocationStatus AllocationStatus;

    /// <summary>The generalized generation and validation metadata for this value.</summary>
    public GenerationMetadata Metadata;

    internal IccidResult()
    {
        StructuralValidity = ValidityMode.Unknown;
        ChecksumValidity = ValidityMode.Unknown;
        AllocationStatus = AllocationStatus.Unknown;
        Metadata = new GenerationMetadata();
    }
}
