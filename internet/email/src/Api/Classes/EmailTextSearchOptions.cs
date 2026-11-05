using System;

namespace email;
/// <summary>
/// Controls how text search operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailTextSearchOptions : IEmailTextSearchOptionsContract
{
    /// <summary>
    /// The default maximum query characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumQueryCharacters = 1024;
    /// <summary>
    /// The default maximum total characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumTotalCharacters = 10000000;
    /// <summary>
    /// The default maximum matches used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumMatches = 1000;
    /// <summary>
    /// The default maximum context characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumContextCharacters = 120;
    int maximumDepth;
    int maximumEntityCount;
    int maximumQueryCharacters;
    int maximumTotalCharacters;
    int maximumMatches;
    int maximumContextCharacters;
    bool caseSensitive;
    bool includeTextAttachments;
    bool includeHtml;
    EmailTextDecodeOptions textOptions;
    EmailFlowedTextOptions flowedOptions;
    EmailHtmlTextOptions htmlOptions;
    /// <summary>
    /// Creates a new text search options object.
    /// </summary>
    public EmailTextSearchOptions()
    {
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        maximumQueryCharacters = DefaultMaximumQueryCharacters;
        maximumTotalCharacters = DefaultMaximumTotalCharacters;
        maximumMatches = DefaultMaximumMatches;
        maximumContextCharacters = DefaultMaximumContextCharacters;
        caseSensitive = false;
        includeTextAttachments = false;
        includeHtml = true;
        textOptions = new EmailTextDecodeOptions();
        flowedOptions = new EmailFlowedTextOptions();
        htmlOptions = new EmailHtmlTextOptions();
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDepth(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value");
        maximumDepth = value;
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEntityCount(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumEntityCount = value;
    }

    /// <summary>
    /// Gets the maximum query characters.
    /// </summary>
    /// <returns>The current maximum query characters.</returns>
    public int GetMaximumQueryCharacters()
    {
        return maximumQueryCharacters;
    }

    /// <summary>
    /// Sets the maximum query characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumQueryCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumQueryCharacters = value;
    }

    /// <summary>
    /// Gets the maximum total characters.
    /// </summary>
    /// <returns>The current maximum total characters.</returns>
    public int GetMaximumTotalCharacters()
    {
        return maximumTotalCharacters;
    }

    /// <summary>
    /// Sets the maximum total characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumTotalCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumTotalCharacters = value;
    }

    /// <summary>
    /// Gets the maximum matches.
    /// </summary>
    /// <returns>The current maximum matches.</returns>
    public int GetMaximumMatches()
    {
        return maximumMatches;
    }

    /// <summary>
    /// Sets the maximum matches used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumMatches(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumMatches = value;
    }

    /// <summary>
    /// Gets the maximum context characters.
    /// </summary>
    /// <returns>The current maximum context characters.</returns>
    public int GetMaximumContextCharacters()
    {
        return maximumContextCharacters;
    }

    /// <summary>
    /// Sets the maximum context characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumContextCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumContextCharacters = value;
    }

    /// <summary>
    /// Gets the case sensitive.
    /// </summary>
    /// <returns><c>true</c> when the case sensitive setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetCaseSensitive()
    {
        return caseSensitive;
    }

    /// <summary>
    /// Sets the case sensitive used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case sensitive behavior.</param>
    public void SetCaseSensitive(bool value)
    {
        caseSensitive = value;
    }

    /// <summary>
    /// Gets the include text attachments.
    /// </summary>
    /// <returns><c>true</c> when the include text attachments setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeTextAttachments()
    {
        return includeTextAttachments;
    }

    /// <summary>
    /// Sets the include text attachments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include text attachments behavior.</param>
    public void SetIncludeTextAttachments(bool value)
    {
        includeTextAttachments = value;
    }

    /// <summary>
    /// Gets the include HTML.
    /// </summary>
    /// <returns><c>true</c> when the include HTML setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeHtml()
    {
        return includeHtml;
    }

    /// <summary>
    /// Sets the include HTML used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include HTML behavior.</param>
    public void SetIncludeHtml(bool value)
    {
        includeHtml = value;
    }

    /// <summary>
    /// Gets the text options.
    /// </summary>
    /// <returns>The current text options.</returns>
    public EmailTextDecodeOptions GetTextOptions()
    {
        return textOptions.Clone();
    }

    /// <summary>
    /// Sets the text options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetTextOptions(EmailTextDecodeOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        textOptions = value.Clone();
    }

    /// <summary>
    /// Gets the flowed options.
    /// </summary>
    /// <returns>The current flowed options.</returns>
    public EmailFlowedTextOptions GetFlowedOptions()
    {
        return flowedOptions.Clone();
    }

    /// <summary>
    /// Sets the flowed options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetFlowedOptions(EmailFlowedTextOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        flowedOptions = value.Clone();
    }

    /// <summary>
    /// Gets the HTML options.
    /// </summary>
    /// <returns>The current HTML options.</returns>
    public EmailHtmlTextOptions GetHtmlOptions()
    {
        return htmlOptions.Clone();
    }

    /// <summary>
    /// Sets the HTML options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHtmlOptions(EmailHtmlTextOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        htmlOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTextSearchOptions Clone()
    {
        EmailTextSearchOptions copy = new EmailTextSearchOptions();
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetMaximumQueryCharacters(maximumQueryCharacters);
        copy.SetMaximumTotalCharacters(maximumTotalCharacters);
        copy.SetMaximumMatches(maximumMatches);
        copy.SetMaximumContextCharacters(maximumContextCharacters);
        copy.SetCaseSensitive(caseSensitive);
        copy.SetIncludeTextAttachments(includeTextAttachments);
        copy.SetIncludeHtml(includeHtml);
        copy.SetTextOptions(textOptions);
        copy.SetFlowedOptions(flowedOptions);
        copy.SetHtmlOptions(htmlOptions);
        return copy;
    }
}
