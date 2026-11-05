namespace iso20022;

/// <summary>Identifies the exception-less outcome of an XML message signing operation.</summary>
public enum MessageSigningStatus
{
    /// <summary>The signing outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The message was signed successfully.</summary>
    Signed = 1,
    /// <summary>The signing request or source message is invalid.</summary>
    InvalidRequest = 2,
    /// <summary>No usable signing key was supplied.</summary>
    KeyUnavailable = 3,
    /// <summary>A requested algorithm is not supported.</summary>
    UnsupportedAlgorithm = 4,
    /// <summary>A requested algorithm is rejected by secure-default policy.</summary>
    UnsafeAlgorithm = 5,
    /// <summary>The deterministic signature insertion location was not found.</summary>
    InsertionLocationNotFound = 6,
    /// <summary>Post-signing verification did not accept the generated signature.</summary>
    VerificationFailed = 7,
    /// <summary>A caller-selected resource limit stopped signing.</summary>
    ResourceLimitReached = 8
}
