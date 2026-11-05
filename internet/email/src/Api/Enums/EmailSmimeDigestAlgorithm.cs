using System;

namespace email;
/// <summary>
/// Specifies the supported S/MIME digest values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeDigestAlgorithm
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the sha 1 choice or state.
    /// </summary>
    Sha1 = 1,
    /// <summary>
    /// Represents the sha 256 choice or state.
    /// </summary>
    Sha256 = 2,
    /// <summary>
    /// Represents the sha 384 choice or state.
    /// </summary>
    Sha384 = 3,
    /// <summary>
    /// Represents the sha 512 choice or state.
    /// </summary>
    Sha512 = 4
}
