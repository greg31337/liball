using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlTextOptions"/>.
/// </summary>
public interface IEmailHtmlTextOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailHtmlLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailHtmlLimits value);
    /// <summary>
    /// Gets the include link targets.
    /// </summary>
    /// <returns><c>true</c> when the include link targets setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeLinkTargets();
    /// <summary>
    /// Sets the include link targets used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include link targets behavior.</param>
    void SetIncludeLinkTargets(bool value);
    /// <summary>
    /// Gets the include image alternative text.
    /// </summary>
    /// <returns><c>true</c> when the include image alternative text setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeImageAlternativeText();
    /// <summary>
    /// Sets the include image alternative text used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include image alternative text behavior.</param>
    void SetIncludeImageAlternativeText(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHtmlTextOptions Clone();
}
