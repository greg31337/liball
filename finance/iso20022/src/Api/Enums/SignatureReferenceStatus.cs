namespace iso20022;

/// <summary>Identifies the validation outcome of one XML signature reference.</summary>
public enum SignatureReferenceStatus
{
    /// <summary>The reference outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The reference target and digest are valid.</summary>
    Valid = 1,
    /// <summary>The reference target is missing.</summary>
    MissingTarget = 2,
    /// <summary>The reference target is ambiguous or duplicated.</summary>
    AmbiguousTarget = 3,
    /// <summary>The calculated digest does not match the signed digest.</summary>
    DigestMismatch = 4,
    /// <summary>The digest or transform algorithm is not supported.</summary>
    UnsupportedAlgorithm = 5,
    /// <summary>The digest or transform algorithm is rejected by policy.</summary>
    UnsafeAlgorithm = 6,
    /// <summary>The reference escapes the permitted message containment boundary.</summary>
    ExternalReferenceRejected = 7,
    /// <summary>The reference markup is malformed.</summary>
    Malformed = 8
}
