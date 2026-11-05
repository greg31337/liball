using System;

namespace email;
/// <summary>
/// Controls how report operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailReportOptions : IEmailReportOptionsContract
{
    /// <summary>
    /// The default maximum human readable characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHumanReadableCharacters = 1000000;
    /// <summary>
    /// The default maximum report bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumReportBytes = 10485760;
    /// <summary>
    /// The default maximum original header bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumOriginalHeaderBytes = 1048576;
    EmailReportOriginalContent originalContent;
    bool includeBlindRecipientHeaders;
    int maximumHumanReadableCharacters;
    int maximumReportBytes;
    int maximumOriginalHeaderBytes;
    EmailComposerOptions composerOptions;
    /// <summary>
    /// Creates a new report options object.
    /// </summary>
    public EmailReportOptions()
    {
        originalContent = EmailReportOriginalContent.Headers;
        includeBlindRecipientHeaders = false;
        maximumHumanReadableCharacters = DefaultMaximumHumanReadableCharacters;
        maximumReportBytes = DefaultMaximumReportBytes;
        maximumOriginalHeaderBytes = DefaultMaximumOriginalHeaderBytes;
        composerOptions = new EmailComposerOptions();
    }

    /// <summary>
    /// Gets the original content.
    /// </summary>
    /// <returns>The current original content.</returns>
    public EmailReportOriginalContent GetOriginalContent()
    {
        return originalContent;
    }

    /// <summary>
    /// Sets the original content used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetOriginalContent(EmailReportOriginalContent value)
    {
        if (value < EmailReportOriginalContent.None || value > EmailReportOriginalContent.FullMessage)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        originalContent = value;
    }

    /// <summary>
    /// Gets the include blind recipient headers.
    /// </summary>
    /// <returns><c>true</c> when the include blind recipient headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeBlindRecipientHeaders()
    {
        return includeBlindRecipientHeaders;
    }

    /// <summary>
    /// Sets the include blind recipient headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include blind recipient headers behavior.</param>
    public void SetIncludeBlindRecipientHeaders(bool value)
    {
        includeBlindRecipientHeaders = value;
    }

    /// <summary>
    /// Gets the maximum human readable characters.
    /// </summary>
    /// <returns>The current maximum human readable characters.</returns>
    public int GetMaximumHumanReadableCharacters()
    {
        return maximumHumanReadableCharacters;
    }

    /// <summary>
    /// Sets the maximum human readable characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHumanReadableCharacters(int value)
    {
        maximumHumanReadableCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum report bytes.
    /// </summary>
    /// <returns>The current maximum report bytes.</returns>
    public int GetMaximumReportBytes()
    {
        return maximumReportBytes;
    }

    /// <summary>
    /// Sets the maximum report bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumReportBytes(int value)
    {
        maximumReportBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum original header bytes.
    /// </summary>
    /// <returns>The current maximum original header bytes.</returns>
    public int GetMaximumOriginalHeaderBytes()
    {
        return maximumOriginalHeaderBytes;
    }

    /// <summary>
    /// Sets the maximum original header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumOriginalHeaderBytes(int value)
    {
        maximumOriginalHeaderBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the composer options.
    /// </summary>
    /// <returns>The current composer options.</returns>
    public EmailComposerOptions GetComposerOptions()
    {
        return composerOptions.Clone();
    }

    /// <summary>
    /// Sets the composer options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetComposerOptions(EmailComposerOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        composerOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailReportOptions Clone()
    {
        EmailReportOptions copy = new EmailReportOptions();
        copy.SetOriginalContent(originalContent);
        copy.SetIncludeBlindRecipientHeaders(includeBlindRecipientHeaders);
        copy.SetMaximumHumanReadableCharacters(maximumHumanReadableCharacters);
        copy.SetMaximumReportBytes(maximumReportBytes);
        copy.SetMaximumOriginalHeaderBytes(maximumOriginalHeaderBytes);
        copy.SetComposerOptions(composerOptions);
        return copy;
    }
}
