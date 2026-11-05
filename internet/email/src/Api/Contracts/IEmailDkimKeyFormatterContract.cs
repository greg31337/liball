namespace email;
/// <summary>
/// Defines operations for formatting DKIM DNS key records.
/// </summary>
public interface IEmailDkimKeyFormatterContract
{
    /// <summary>
    /// Formats a deterministic canonical DKIM DNS key record.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatWithValue(EmailDkimKey value);
    /// <summary>
    /// Formats a deterministic canonical DKIM DNS key record with options.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options);
    /// <summary>
    /// Preserves the original safe DNS key record when available.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatPreservingWithValue(EmailDkimKey value);
    /// <summary>
    /// Preserves the original safe DNS key record when available, subject to formatting limits.
    /// </summary>
    /// <param name = "value">The DKIM key to format.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatPreservingWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options);
}
