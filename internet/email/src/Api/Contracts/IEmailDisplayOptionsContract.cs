using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDisplayOptions"/>.
/// </summary>
public interface IEmailDisplayOptionsContract
{
    /// <summary>
    /// Gets the body preference.
    /// </summary>
    /// <returns>The current body preference.</returns>
    EmailBodyPreference GetBodyPreference();
    /// <summary>
    /// Sets the body preference used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetBodyPreference(EmailBodyPreference value);
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
    /// Gets the HTML text options.
    /// </summary>
    /// <returns>The current HTML text options.</returns>
    EmailHtmlTextOptions GetHtmlTextOptions();
    /// <summary>
    /// Sets the HTML text options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHtmlTextOptions(EmailHtmlTextOptions value);
    /// <summary>
    /// Gets the HTML sanitize options.
    /// </summary>
    /// <returns>The current HTML sanitize options.</returns>
    EmailHtmlSanitizeOptions GetHtmlSanitizeOptions();
    /// <summary>
    /// Sets the HTML sanitize options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHtmlSanitizeOptions(EmailHtmlSanitizeOptions value);
    /// <summary>
    /// Gets the analysis options.
    /// </summary>
    /// <returns>The current analysis options.</returns>
    EmailTextAnalysisOptions GetAnalysisOptions();
    /// <summary>
    /// Sets the analysis options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAnalysisOptions(EmailTextAnalysisOptions value);
    /// <summary>
    /// Gets the related index options.
    /// </summary>
    /// <returns>The current related index options.</returns>
    EmailRelatedIndexOptions GetRelatedIndexOptions();
    /// <summary>
    /// Sets the related index options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetRelatedIndexOptions(EmailRelatedIndexOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDisplayOptions Clone();
}
