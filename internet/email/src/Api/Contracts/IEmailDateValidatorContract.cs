using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDateValidator"/>.
/// </summary>
public interface IEmailDateValidatorContract
{
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDateValidationResult Validate(EmailDateTime value);
}
