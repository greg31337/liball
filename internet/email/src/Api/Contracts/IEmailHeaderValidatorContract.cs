using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderValidator"/>.
/// </summary>
public interface IEmailHeaderValidatorContract
{
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHeaderValidationResult ValidateWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHeaderValidationResult ValidateWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderValidationOptions options);
}
