using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTransferEncoding"/>.
/// </summary>
public interface IEmailTransferEncodingContract
{
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTransferValidationResult ValidateWithInputAndKind(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind);
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTransferValidationResult ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes);
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The transfer validation result produced by the operation.</returns>
    EmailTransferValidationResult CopyWithInputAndOutputAndKindAndMaximumBytes(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes);
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <param name = "cancellation">The cancellation used by the operation.</param>
    /// <param name = "progress">The progress used by the operation.</param>
    /// <returns>The transfer validation result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailTransferValidationResult CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes, int bufferBytes, IEmailCancellation? cancellation, IEmailProgress? progress);
    /// <summary>
    /// Validates the supplied kind against the selected policy.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void ValidateKind(EmailTransferEncodingKind kind);
    /// <summary>
    /// Validates the supplied identity kind against the selected policy.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void ValidateIdentityKind(EmailTransferEncodingKind kind);
    /// <summary>
    /// Determines whether the current value or state is identity kind.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns><c>true</c> when the value or state is identity kind; otherwise, <c>false</c>.</returns>
    bool IsIdentityKind(EmailTransferEncodingKind kind);
}
