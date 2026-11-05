namespace fake;

/// <summary>Contains an IMSI-shaped value, its parsed fields, and independent validity metadata.</summary>
/// <remarks>Field parsing validates only the caller-supplied MNC boundary and does not establish network or subscriber allocation.</remarks>
public class ImsiResult
{
    /// <summary>The complete IMSI-shaped value.</summary>
    public string Value = string.Empty;

    /// <summary>The three-digit mobile country code, or an empty string when structure is invalid.</summary>
    public string MobileCountryCode = string.Empty;

    /// <summary>The two- or three-digit mobile network code, or an empty string when structure is invalid.</summary>
    public string MobileNetworkCode = string.Empty;

    /// <summary>The remaining mobile subscription identification number, or an empty string when structure is invalid.</summary>
    public string MobileSubscriptionIdentificationNumber = string.Empty;

    /// <summary>The MNC length used to determine component boundaries.</summary>
    public int MobileNetworkCodeLength;

    /// <summary>Whether length, characters, and component boundaries are structurally valid.</summary>
    public ValidityMode StructuralValidity;

    /// <summary>Checksum validity, always <see cref="ValidityMode.NotApplicable"/> because IMSIs have no check digit.</summary>
    public ValidityMode ChecksumValidity;

    /// <summary>Whether the library generated the value synthetically.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the result claims an external assignment, always <c>false</c>.</summary>
    public bool RepresentsAssignment;

    /// <summary>The library's allocation knowledge, always <see cref="AllocationStatus.Unknown"/>.</summary>
    public AllocationStatus AllocationStatus;

    /// <summary>The generalized generation and validation metadata for this value.</summary>
    public GenerationMetadata Metadata;

    internal ImsiResult()
    {
        StructuralValidity = ValidityMode.Unknown;
        ChecksumValidity = ValidityMode.NotApplicable;
        AllocationStatus = AllocationStatus.Unknown;
        Metadata = new GenerationMetadata();
    }
}
