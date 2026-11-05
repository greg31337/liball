namespace iso20022;

/// <summary>Identifies the exception-less outcome of message checksum calculation or verification.</summary>
public enum ChecksumStatus
{
    /// <summary>The checksum outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>A checksum was calculated without an expected value.</summary>
    Calculated = 1,
    /// <summary>The calculated checksum matches the expected value.</summary>
    Matched = 2,
    /// <summary>The calculated checksum does not match the expected value.</summary>
    Mismatched = 3,
    /// <summary>Duplicate semantic keys prevent an unambiguous checksum.</summary>
    DuplicateKey = 4,
    /// <summary>The supplied message representation is invalid or unavailable.</summary>
    InvalidInput = 5,
    /// <summary>The requested hash algorithm is not supported or permitted.</summary>
    UnsupportedAlgorithm = 6,
    /// <summary>The requested checksum equality mode is not supported for the supplied representation.</summary>
    UnsupportedMode = 7,
    /// <summary>A caller-selected resource limit stopped checksum processing.</summary>
    ResourceLimitReached = 8
}
