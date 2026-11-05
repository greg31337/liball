namespace iso20022;

/// <summary>Identifies the aggregate or individual XML signature validation outcome.</summary>
public enum SignatureValidationStatus
{
    /// <summary>The signature validation outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>No XML signature was present.</summary>
    NotPresent = 1,
    /// <summary>The XML signature and all permitted references are valid.</summary>
    Valid = 2,
    /// <summary>The XML signature value is invalid.</summary>
    Invalid = 3,
    /// <summary>The XML signature markup is malformed.</summary>
    Malformed = 4,
    /// <summary>A required cryptographic algorithm is not supported.</summary>
    UnsupportedAlgorithm = 5,
    /// <summary>A cryptographic algorithm is rejected by caller or secure-default policy.</summary>
    UnsafeAlgorithm = 6,
    /// <summary>At least one signature reference is invalid.</summary>
    ReferenceInvalid = 7,
    /// <summary>The signing certificate did not satisfy caller trust policy.</summary>
    CertificateUntrusted = 8,
    /// <summary>A caller-selected resource limit stopped signature validation.</summary>
    ResourceLimitReached = 9
}
