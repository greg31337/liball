using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Validates header and reports policy violations.
/// </summary>
public class EmailHeaderValidator : IEmailHeaderValidatorContract
{
    internal static EmailHeaderValidationResult ValidateImplementationWithHeaders(EmailHeaders headers)
    {
        return ValidateImplementationWithHeadersAndOptions(headers, new EmailHeaderValidationOptions());
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHeaderValidationResult ValidateWithHeaders(EmailHeaders headers)
    {
        return EmailHeaderValidator.ValidateImplementationWithHeaders(headers);
    }

    internal static EmailHeaderValidationResult ValidateImplementationWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderValidationOptions options)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailHeaderValidationWorker worker = new EmailHeaderValidationWorker(headers, options.Clone());
        return worker.Validate();
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHeaderValidationResult ValidateWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderValidationOptions options)
    {
        return EmailHeaderValidator.ValidateImplementationWithHeadersAndOptions(headers, options);
    }
}
