using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFingerprint"/>.
/// </summary>
public interface IEmailFingerprintContract
{
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeExactWithMessage(EmailMessage message);
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeExactWithMessageAndAlgorithm(EmailMessage message, EmailHashAlgorithm algorithm);
    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailHashResult ComputeExactWithMessageAndAlgorithmAndMaximumBytes(EmailMessage message, EmailHashAlgorithm algorithm, long maximumBytes);
    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeSemanticWithMessage(EmailMessage message);
    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHashResult ComputeSemanticWithMessageAndOptions(EmailMessage message, EmailCanonicalComparisonOptions options);
    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeSemanticWithEntity(MimeEntity entity);
    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHashResult ComputeSemanticWithEntityAndOptions(MimeEntity entity, EmailCanonicalComparisonOptions options);
}
