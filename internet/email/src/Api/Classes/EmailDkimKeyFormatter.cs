using System;

namespace email;
/// <summary>
/// Formats DKIM keys as safe DNS key records.
/// </summary>
public class EmailDkimKeyFormatter : IEmailDkimKeyFormatterContract
{
    /// <summary>
    /// Formats a deterministic canonical DKIM DNS key record.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatWithValue(EmailDkimKey value)
    {
        return FormatWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Formats a deterministic canonical DKIM DNS key record with options.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailDkimKeyFormatterWorker worker = new EmailDkimKeyFormatterWorker(options.Clone());
        worker.FormatCanonical(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Preserves the original safe DNS key record when available.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatPreservingWithValue(EmailDkimKey value)
    {
        return FormatPreservingWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Preserves the original safe DNS key record when available, subject to formatting limits.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatPreservingWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailDkimKeyFormatterWorker worker = new EmailDkimKeyFormatterWorker(options.Clone());
        worker.FormatPreserving(value);
        return worker.CreateResult();
    }

    static void Validate(EmailDkimKey value, EmailValueFormatOptions options)
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
