using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Validates email data and reports policy violations.
/// </summary>
public class EmailValidator : IEmailValidatorContract
{
    internal static EmailValidationResult ValidateImplementationWithMessage(EmailMessage message)
    {
        return ValidateImplementationWithMessageAndOptions(message, new EmailValidationOptions());
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailValidationResult ValidateWithMessage(EmailMessage message)
    {
        return EmailValidator.ValidateImplementationWithMessage(message);
    }

    internal static EmailValidationResult ValidateImplementationWithMessageAndOptions(EmailMessage message, EmailValidationOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return new EmailValidationWorker(options.Clone()).Validate(message);
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailValidationResult ValidateWithMessageAndOptions(EmailMessage message, EmailValidationOptions options)
    {
        return EmailValidator.ValidateImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailValidationResult ValidateImplementationWithEntity(MimeEntity entity)
    {
        return ValidateImplementationWithEntityAndOptions(entity, new EmailValidationOptions());
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailValidationResult ValidateWithEntity(MimeEntity entity)
    {
        return EmailValidator.ValidateImplementationWithEntity(entity);
    }

    internal static EmailValidationResult ValidateImplementationWithEntityAndOptions(MimeEntity entity, EmailValidationOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return new EmailValidationWorker(options.Clone()).ValidateMimeEntity(entity);
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailValidationResult ValidateWithEntityAndOptions(MimeEntity entity, EmailValidationOptions options)
    {
        return EmailValidator.ValidateImplementationWithEntityAndOptions(entity, options);
    }
}
