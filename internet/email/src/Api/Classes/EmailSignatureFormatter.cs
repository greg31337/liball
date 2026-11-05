using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Formats signature for email output.
/// </summary>
public class EmailSignatureFormatter : IEmailSignatureFormatterContract
{
    internal static EmailSignatureFormatResult FormatImplementationWithValueUsingEmailDkimSignature(EmailDkimSignature value)
    {
        return FormatImplementationWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(value, new EmailSignatureFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueUsingEmailDkimSignature(EmailDkimSignature value)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueUsingEmailDkimSignature(value);
    }

    internal static EmailSignatureFormatResult FormatImplementationWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        Validate(value, options);
        EmailSignatureFormatterWorker worker = new EmailSignatureFormatterWorker(options.Clone());
        worker.FormatTags(value.GetTags());
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(value, options);
    }

    internal static EmailSignatureFormatResult FormatImplementationWithValueUsingEmailArcSeal(EmailArcSeal value)
    {
        return FormatImplementationWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(value, new EmailSignatureFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueUsingEmailArcSeal(EmailArcSeal value)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueUsingEmailArcSeal(value);
    }

    internal static EmailSignatureFormatResult FormatImplementationWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(EmailArcSeal value, EmailSignatureFormatOptions options)
    {
        Validate(value, options);
        EmailSignatureFormatterWorker worker = new EmailSignatureFormatterWorker(options.Clone());
        worker.FormatTags(value.GetTags());
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(EmailArcSeal value, EmailSignatureFormatOptions options)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(value, options);
    }

    internal static EmailSignatureFormatResult FormatImplementationWithValueUsingEmailArcAuthenticationResults(EmailArcAuthenticationResults value)
    {
        return FormatImplementationWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(value, new EmailSignatureFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueUsingEmailArcAuthenticationResults(EmailArcAuthenticationResults value)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueUsingEmailArcAuthenticationResults(value);
    }

    internal static EmailSignatureFormatResult FormatImplementationWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options)
    {
        Validate(value, options);
        EmailSignatureFormatterWorker worker = new EmailSignatureFormatterWorker(options.Clone());
        worker.FormatArcAuthenticationResults(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options)
    {
        return EmailSignatureFormatter.FormatImplementationWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(value, options);
    }

    static void Validate(object value, EmailSignatureFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }
}
