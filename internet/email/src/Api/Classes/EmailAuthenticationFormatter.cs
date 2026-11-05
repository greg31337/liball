using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Formats authentication for email output.
/// </summary>
public class EmailAuthenticationFormatter : IEmailAuthenticationFormatterContract
{
    internal static EmailAuthenticationFormatResult FormatImplementationWithValue(EmailAuthenticationResults value)
    {
        return FormatImplementationWithValueAndOptions(value, new EmailAuthenticationFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult FormatWithValue(EmailAuthenticationResults value)
    {
        return EmailAuthenticationFormatter.FormatImplementationWithValue(value);
    }

    internal static EmailAuthenticationFormatResult FormatImplementationWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailAuthenticationFormatterWorker worker = new EmailAuthenticationFormatterWorker(options.Clone());
        worker.FormatAuthenticationResults(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAuthenticationFormatResult FormatWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options)
    {
        return EmailAuthenticationFormatter.FormatImplementationWithValueAndOptions(value, options);
    }

    internal static EmailAuthenticationFormatResult FormatReceivedSpfImplementationWithValue(EmailReceivedSpf value)
    {
        return FormatReceivedSpfImplementationWithValueAndOptions(value, new EmailAuthenticationFormatOptions());
    }

    /// <summary>
    /// Formats the received spf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult FormatReceivedSpfWithValue(EmailReceivedSpf value)
    {
        return EmailAuthenticationFormatter.FormatReceivedSpfImplementationWithValue(value);
    }

    internal static EmailAuthenticationFormatResult FormatReceivedSpfImplementationWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailAuthenticationFormatterWorker worker = new EmailAuthenticationFormatterWorker(options.Clone());
        worker.FormatReceivedSpf(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the received spf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAuthenticationFormatResult FormatReceivedSpfWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options)
    {
        return EmailAuthenticationFormatter.FormatReceivedSpfImplementationWithValueAndOptions(value, options);
    }
}
