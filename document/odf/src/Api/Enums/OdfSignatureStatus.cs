namespace odf;

/// <summary>
/// Identifies the result of signature inspection or verification.
/// </summary>
public enum OdfSignatureStatus
{
    /// <summary>
    /// The signature status is not known.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// No applicable signature is present.
    /// </summary>
    NotSigned = 1,
    /// <summary>
    /// A signature is present but was not verified.
    /// </summary>
    NotChecked = 2,
    /// <summary>
    /// The signature and referenced digests are valid.
    /// </summary>
    Valid = 3,
    /// <summary>
    /// Signature verification failed.
    /// </summary>
    Invalid = 4,
    /// <summary>
    /// Verification requires an unsupported algorithm.
    /// </summary>
    UnsupportedAlgorithm = 5,
    /// <summary>
    /// A referenced package resource is missing.
    /// </summary>
    MissingReference = 6,
    /// <summary>
    /// A referenced resource has a digest mismatch.
    /// </summary>
    DigestMismatch = 7,
    /// <summary>
    /// The signing certificate is invalid for the requested policy.
    /// </summary>
    CertificateInvalid = 8,
    /// <summary>
    /// Verification could not be completed because of an error.
    /// </summary>
    Error = 9
}
