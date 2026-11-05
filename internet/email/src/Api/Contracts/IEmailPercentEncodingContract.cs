using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPercentEncoding"/>.
/// </summary>
public interface IEmailPercentEncodingContract
{
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] Encode(ReadOnlySpan<byte> input);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailPercentDecodeResult DecodeWithInputAndModeAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailPercentDecodeResult DecodeWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes, EmailSourcePosition? initialPosition);
}
