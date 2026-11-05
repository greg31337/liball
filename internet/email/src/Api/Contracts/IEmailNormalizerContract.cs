using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailNormalizer"/>.
/// </summary>
public interface IEmailNormalizerContract
{
    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    EmailNormalizationResult NormalizeWithMessage(EmailMessage message);
    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailNormalizationResult NormalizeWithMessageAndOptions(EmailMessage message, EmailNormalizationOptions options);
    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    EmailNormalizationResult NormalizeWithEntity(MimeEntity entity);
    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailNormalizationResult NormalizeWithEntityAndOptions(MimeEntity entity, EmailNormalizationOptions options);
}
