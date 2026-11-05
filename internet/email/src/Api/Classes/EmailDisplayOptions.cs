using System;

namespace email;
/// <summary>
/// Controls how display operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDisplayOptions : IEmailDisplayOptionsContract
{
    EmailBodyPreference bodyPreference;
    int maximumDepth;
    int maximumEntityCount;
    EmailTextDecodeOptions textOptions;
    EmailFlowedTextOptions flowedOptions;
    EmailHtmlTextOptions htmlTextOptions;
    EmailHtmlSanitizeOptions htmlSanitizeOptions;
    EmailTextAnalysisOptions analysisOptions;
    EmailRelatedIndexOptions relatedIndexOptions;
    /// <summary>
    /// Creates a new display options object.
    /// </summary>
    public EmailDisplayOptions()
    {
        bodyPreference = EmailBodyPreference.Html;
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        textOptions = new EmailTextDecodeOptions();
        flowedOptions = new EmailFlowedTextOptions();
        htmlTextOptions = new EmailHtmlTextOptions();
        htmlSanitizeOptions = new EmailHtmlSanitizeOptions();
        analysisOptions = new EmailTextAnalysisOptions();
        relatedIndexOptions = new EmailRelatedIndexOptions();
    }

    /// <summary>
    /// Gets the body preference.
    /// </summary>
    /// <returns>The current body preference.</returns>
    public EmailBodyPreference GetBodyPreference()
    {
        return bodyPreference;
    }

    /// <summary>
    /// Sets the body preference used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetBodyPreference(EmailBodyPreference value)
    {
        if (value < EmailBodyPreference.Html || value > EmailBodyPreference.PlainText)
            throw new ArgumentOutOfRangeException("value");
        bodyPreference = value;
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
    /// Gets the HTML text options.
    /// </summary>
    /// <returns>The current HTML text options.</returns>
    public EmailHtmlTextOptions GetHtmlTextOptions()
    {
        return htmlTextOptions.Clone();
    }

    /// <summary>
    /// Sets the HTML text options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHtmlTextOptions(EmailHtmlTextOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        htmlTextOptions = value.Clone();
    }

    /// <summary>
    /// Gets the HTML sanitize options.
    /// </summary>
    /// <returns>The current HTML sanitize options.</returns>
    public EmailHtmlSanitizeOptions GetHtmlSanitizeOptions()
    {
        return htmlSanitizeOptions.Clone();
    }

    /// <summary>
    /// Sets the HTML sanitize options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHtmlSanitizeOptions(EmailHtmlSanitizeOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        htmlSanitizeOptions = value.Clone();
    }

    /// <summary>
    /// Gets the analysis options.
    /// </summary>
    /// <returns>The current analysis options.</returns>
    public EmailTextAnalysisOptions GetAnalysisOptions()
    {
        return analysisOptions.Clone();
    }

    /// <summary>
    /// Sets the analysis options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAnalysisOptions(EmailTextAnalysisOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        analysisOptions = value.Clone();
    }

    /// <summary>
    /// Gets the related index options.
    /// </summary>
    /// <returns>The current related index options.</returns>
    public EmailRelatedIndexOptions GetRelatedIndexOptions()
    {
        return relatedIndexOptions.Clone();
    }

    /// <summary>
    /// Sets the related index options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetRelatedIndexOptions(EmailRelatedIndexOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        relatedIndexOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDisplayOptions Clone()
    {
        EmailDisplayOptions copy = new EmailDisplayOptions();
        copy.SetBodyPreference(bodyPreference);
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetTextOptions(textOptions);
        copy.SetFlowedOptions(flowedOptions);
        copy.SetHtmlTextOptions(htmlTextOptions);
        copy.SetHtmlSanitizeOptions(htmlSanitizeOptions);
        copy.SetAnalysisOptions(analysisOptions);
        copy.SetRelatedIndexOptions(relatedIndexOptions);
        return copy;
    }
}
