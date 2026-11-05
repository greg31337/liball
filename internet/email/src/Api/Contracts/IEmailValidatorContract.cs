using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailValidator"/>.
/// </summary>
public interface IEmailValidatorContract
{
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailValidationResult ValidateWithMessage(EmailMessage message);
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailValidationResult ValidateWithMessageAndOptions(EmailMessage message, EmailValidationOptions options);
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailValidationResult ValidateWithEntity(MimeEntity entity);
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailValidationResult ValidateWithEntityAndOptions(MimeEntity entity, EmailValidationOptions options);
}
