using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationFormatter"/>.
/// </summary>
public interface IEmailAuthenticationFormatterContract
{
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    EmailAuthenticationFormatResult FormatWithValue(EmailAuthenticationResults value);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAuthenticationFormatResult FormatWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options);
    /// <summary>
    /// Formats the received spf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    EmailAuthenticationFormatResult FormatReceivedSpfWithValue(EmailReceivedSpf value);
    /// <summary>
    /// Formats the received spf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAuthenticationFormatResult FormatReceivedSpfWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options);
}
