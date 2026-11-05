using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailGenerator"/>.
/// </summary>
public interface IEmailGeneratorContract
{
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The generation result produced by the operation.</returns>
    EmailGenerationResult GenerateWithMessage(EmailMessage message);
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The generation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailGenerationResult GenerateWithMessageAndOptions(EmailMessage message, EmailGenerationOptions options);
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The generation result produced by the operation.</returns>
    EmailGenerationResult GenerateWithEntity(MimeEntity entity);
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The generation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailGenerationResult GenerateWithEntityAndOptions(MimeEntity entity, EmailGenerationOptions options);
}
