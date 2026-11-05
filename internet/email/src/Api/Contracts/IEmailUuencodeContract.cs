using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailUuencode"/>.
/// </summary>
public interface IEmailUuencodeContract
{
    /// <summary>
    /// Detects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    EmailUuencodeDetectionResult Detect(ReadOnlySpan<byte> input);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailUuencodeDecodeResult Decode(ReadOnlySpan<byte> input, long maximumDecodedBytes);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] Encode(ReadOnlySpan<byte> input, int fileMode, string fileName);
    /// <summary>
    /// Parses the supplied header data.
    /// </summary>
    /// <param name = "line">The line used by the operation.</param>
    /// <param name = "position">The position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailUuencodeDetectionResult ParseHeader(ReadOnlySpan<byte> line, EmailSourcePosition position);
    /// <summary>
    /// Validates the supplied metadata against the selected policy.
    /// </summary>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void ValidateMetadata(int fileMode, string fileName);
}
