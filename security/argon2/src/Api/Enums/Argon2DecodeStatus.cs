namespace argon2;

/// <summary>
/// Describes the outcome of decoding an Argon2 encoded hash.
/// </summary>
public enum Argon2DecodeStatus
{
    /// <summary>
    /// The encoded hash was decoded successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The encoded hash does not have the required PHC structure.
    /// </summary>
    InvalidFormat = 1,

    /// <summary>
    /// A binary component is not valid canonical PHC base64.
    /// </summary>
    InvalidBase64 = 2,

    /// <summary>
    /// One or more decoded parameters are outside the supported bounds.
    /// </summary>
    InvalidParameters = 3,

    /// <summary>
    /// The encoded hash identifies an unsupported Argon2 variant.
    /// </summary>
    UnsupportedVariant = 4,

    /// <summary>
    /// The encoded hash identifies an unsupported Argon2 version.
    /// </summary>
    UnsupportedVersion = 5
}
