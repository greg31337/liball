using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Specifies the supported S/MIME decompression values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeDecompressionStatus
{
    /// <summary>
    /// Represents the success choice or state.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Represents the malformed choice or state.
    /// </summary>
    Malformed = 1,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 2,
    /// <summary>
    /// Represents the decompression failed choice or state.
    /// </summary>
    DecompressionFailed = 3
}
