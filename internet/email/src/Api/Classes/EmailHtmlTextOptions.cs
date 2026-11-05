using System;

namespace email;
/// <summary>
/// Controls how HTML text operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHtmlTextOptions : IEmailHtmlTextOptionsContract
{
    EmailHtmlLimits limits;
    bool includeLinkTargets;
    bool includeImageAlternativeText;
    /// <summary>
    /// Creates a new HTML text options object.
    /// </summary>
    public EmailHtmlTextOptions()
    {
        limits = new EmailHtmlLimits();
        includeLinkTargets = true;
        includeImageAlternativeText = true;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailHtmlLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailHtmlLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the include link targets.
    /// </summary>
    /// <returns><c>true</c> when the include link targets setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeLinkTargets()
    {
        return includeLinkTargets;
    }

    /// <summary>
    /// Sets the include link targets used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include link targets behavior.</param>
    public void SetIncludeLinkTargets(bool value)
    {
        includeLinkTargets = value;
    }

    /// <summary>
    /// Gets the include image alternative text.
    /// </summary>
    /// <returns><c>true</c> when the include image alternative text setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeImageAlternativeText()
    {
        return includeImageAlternativeText;
    }

    /// <summary>
    /// Sets the include image alternative text used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include image alternative text behavior.</param>
    public void SetIncludeImageAlternativeText(bool value)
    {
        includeImageAlternativeText = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHtmlTextOptions Clone()
    {
        EmailHtmlTextOptions copy = new EmailHtmlTextOptions();
        copy.SetLimits(limits);
        copy.SetIncludeLinkTargets(includeLinkTargets);
        copy.SetIncludeImageAlternativeText(includeImageAlternativeText);
        return copy;
    }
}
