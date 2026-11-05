namespace argon2;

/// <summary>
/// Describes the outcome of verifying an Argon2 hash.
/// </summary>
public enum Argon2VerificationStatus
{
    /// <summary>
    /// The supplied value matches the expected hash.
    /// </summary>
    Match = 0,

    /// <summary>
    /// The supplied value does not match the expected hash.
    /// </summary>
    Mismatch = 1,

    /// <summary>
    /// The encoded hash is malformed or unsupported.
    /// </summary>
    InvalidHash = 2,

    /// <summary>
    /// The encoded hash exceeds the configured verification policy.
    /// </summary>
    RejectedByPolicy = 3
}
