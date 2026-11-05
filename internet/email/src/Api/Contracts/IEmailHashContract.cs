using System;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHash"/>.
/// </summary>
public interface IEmailHashContract
{
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeExactWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes);
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailHashResult ComputeExactWithContentAndAlgorithmAndMaximumBytesAndBufferBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes);
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeExactWithContentAndAlgorithm(ReadOnlySpan<byte> content, EmailHashAlgorithm algorithm);
    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes);
    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithmAndMaximumBytesAndBufferBytes(Stream decodedContent, EmailHashAlgorithm algorithm, long maximumBytes, int bufferBytes);
    /// <summary>
    /// Computes the decoded.
    /// </summary>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeDecodedWithDecodedContentAndAlgorithm(ReadOnlySpan<byte> decodedContent, EmailHashAlgorithm algorithm);
}
