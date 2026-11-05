using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDateFormatter"/>.
/// </summary>
public interface IEmailDateFormatterContract
{
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The date format result produced by the operation.</returns>
    EmailDateFormatResult FormatWithValue(EmailDateTime? value);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The date format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDateFormatResult FormatWithValueAndOptions(EmailDateTime? value, EmailDateFormatOptions options);
}
