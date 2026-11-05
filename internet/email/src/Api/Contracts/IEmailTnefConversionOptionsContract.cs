using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefConversionOptions"/>.
/// </summary>
public interface IEmailTnefConversionOptionsContract
{
    /// <summary>
    /// Gets the reader options.
    /// </summary>
    /// <returns>The current reader options.</returns>
    EmailTnefReaderOptions GetReaderOptions();
    /// <summary>
    /// Sets the reader options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetReaderOptions(EmailTnefReaderOptions value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the include rich text body.
    /// </summary>
    /// <returns><c>true</c> when the include rich text body setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeRichTextBody();
    /// <summary>
    /// Sets the include rich text body used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include rich text body behavior.</param>
    void SetIncludeRichTextBody(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTnefConversionOptions Clone();
}
