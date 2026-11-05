using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailQuotedPrintable"/>.
/// </summary>
public interface IEmailQuotedPrintableContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailQuotedPrintableDecodeResult Decode(ReadOnlySpan<byte> input, EmailQuotedPrintableDecodeMode mode, long maximumDecodedBytes);
    /// <summary>
    /// Gets the maximum decoded size.
    /// </summary>
    /// <param name = "encodedByteCount">The number of items or bytes to process.</param>
    /// <returns>The current maximum decoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetMaximumDecodedSize(long encodedByteCount);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] EncodeWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "newlinePolicy">The newline policy used by the operation.</param>
    /// <param name = "protectFromLines">Whether to enable the protect from lines behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] EncodeWithInputAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <param name = "newlinePolicy">The newline policy used by the operation.</param>
    /// <param name = "protectFromLines">Whether to enable the protect from lines behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, int lineLength, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines);
    /// <summary>
    /// Validates the supplied line length against the selected policy.
    /// </summary>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void ValidateLineLength(int lineLength);
}
