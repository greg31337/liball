using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextSearchOptions"/>.
/// </summary>
public interface IEmailTextSearchOptionsContract
{
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDepth(int value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the maximum query characters.
    /// </summary>
    /// <returns>The current maximum query characters.</returns>
    int GetMaximumQueryCharacters();
    /// <summary>
    /// Sets the maximum query characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumQueryCharacters(int value);
    /// <summary>
    /// Gets the maximum total characters.
    /// </summary>
    /// <returns>The current maximum total characters.</returns>
    int GetMaximumTotalCharacters();
    /// <summary>
    /// Sets the maximum total characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumTotalCharacters(int value);
    /// <summary>
    /// Gets the maximum matches.
    /// </summary>
    /// <returns>The current maximum matches.</returns>
    int GetMaximumMatches();
    /// <summary>
    /// Sets the maximum matches used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumMatches(int value);
    /// <summary>
    /// Gets the maximum context characters.
    /// </summary>
    /// <returns>The current maximum context characters.</returns>
    int GetMaximumContextCharacters();
    /// <summary>
    /// Sets the maximum context characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumContextCharacters(int value);
    /// <summary>
    /// Gets the case sensitive.
    /// </summary>
    /// <returns><c>true</c> when the case sensitive setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetCaseSensitive();
    /// <summary>
    /// Sets the case sensitive used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case sensitive behavior.</param>
    void SetCaseSensitive(bool value);
    /// <summary>
    /// Gets the include text attachments.
    /// </summary>
    /// <returns><c>true</c> when the include text attachments setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeTextAttachments();
    /// <summary>
    /// Sets the include text attachments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include text attachments behavior.</param>
    void SetIncludeTextAttachments(bool value);
    /// <summary>
    /// Gets the include HTML.
    /// </summary>
    /// <returns><c>true</c> when the include HTML setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeHtml();
    /// <summary>
    /// Sets the include HTML used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include HTML behavior.</param>
    void SetIncludeHtml(bool value);
    /// <summary>
    /// Gets the text options.
    /// </summary>
    /// <returns>The current text options.</returns>
    EmailTextDecodeOptions GetTextOptions();
    /// <summary>
    /// Sets the text options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetTextOptions(EmailTextDecodeOptions value);
    /// <summary>
    /// Gets the flowed options.
    /// </summary>
    /// <returns>The current flowed options.</returns>
    EmailFlowedTextOptions GetFlowedOptions();
    /// <summary>
    /// Sets the flowed options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetFlowedOptions(EmailFlowedTextOptions value);
    /// <summary>
    /// Gets the HTML options.
    /// </summary>
    /// <returns>The current HTML options.</returns>
    EmailHtmlTextOptions GetHtmlOptions();
    /// <summary>
    /// Sets the HTML options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHtmlOptions(EmailHtmlTextOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTextSearchOptions Clone();
}
