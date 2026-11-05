using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBase64"/>.
/// </summary>
public interface IEmailBase64Contract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailBase64DecodeResult Decode(ReadOnlySpan<byte> input, EmailBase64DecodeMode mode, long maximumDecodedBytes);
    /// <summary>
    /// Gets the maximum decoded size.
    /// </summary>
    /// <param name = "encodedByteCount">The number of items or bytes to process.</param>
    /// <returns>The current maximum decoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetMaximumDecodedSize(long encodedByteCount);
    /// <summary>
    /// Gets the decoded size from quantum count.
    /// </summary>
    /// <param name = "quantumCount">The number of items or bytes to process.</param>
    /// <param name = "paddingCount">The number of items or bytes to process.</param>
    /// <returns>The current decoded size from quantum count.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetDecodedSizeFromQuantumCount(long quantumCount, int paddingCount);
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
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    byte[] EncodeWithInputAndLineLength(ReadOnlySpan<byte> input, int lineLength);
    /// <summary>
    /// Gets the encoded size.
    /// </summary>
    /// <param name = "inputByteCount">The number of items or bytes to process.</param>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <returns>The current encoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetEncodedSize(long inputByteCount, int lineLength);
    /// <summary>
    /// Validates the supplied line length against the selected policy.
    /// </summary>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void ValidateLineLength(int lineLength);
}
