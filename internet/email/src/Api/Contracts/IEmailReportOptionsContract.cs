using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportOptions"/>.
/// </summary>
public interface IEmailReportOptionsContract
{
    /// <summary>
    /// Gets the original content.
    /// </summary>
    /// <returns>The current original content.</returns>
    EmailReportOriginalContent GetOriginalContent();
    /// <summary>
    /// Sets the original content used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetOriginalContent(EmailReportOriginalContent value);
    /// <summary>
    /// Gets the include blind recipient headers.
    /// </summary>
    /// <returns><c>true</c> when the include blind recipient headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeBlindRecipientHeaders();
    /// <summary>
    /// Sets the include blind recipient headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include blind recipient headers behavior.</param>
    void SetIncludeBlindRecipientHeaders(bool value);
    /// <summary>
    /// Gets the maximum human readable characters.
    /// </summary>
    /// <returns>The current maximum human readable characters.</returns>
    int GetMaximumHumanReadableCharacters();
    /// <summary>
    /// Sets the maximum human readable characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHumanReadableCharacters(int value);
    /// <summary>
    /// Gets the maximum report bytes.
    /// </summary>
    /// <returns>The current maximum report bytes.</returns>
    int GetMaximumReportBytes();
    /// <summary>
    /// Sets the maximum report bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumReportBytes(int value);
    /// <summary>
    /// Gets the maximum original header bytes.
    /// </summary>
    /// <returns>The current maximum original header bytes.</returns>
    int GetMaximumOriginalHeaderBytes();
    /// <summary>
    /// Sets the maximum original header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumOriginalHeaderBytes(int value);
    /// <summary>
    /// Gets the composer options.
    /// </summary>
    /// <returns>The current composer options.</returns>
    EmailComposerOptions GetComposerOptions();
    /// <summary>
    /// Sets the composer options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetComposerOptions(EmailComposerOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailReportOptions Clone();
}
