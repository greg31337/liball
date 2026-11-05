using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFileLoadOptions"/>.
/// </summary>
public interface IEmailFileLoadOptionsContract
{
    /// <summary>
    /// Gets the parse options.
    /// </summary>
    /// <returns>The current parse options.</returns>
    EmailParseOptions GetParseOptions();
    /// <summary>
    /// Sets the parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetParseOptions(EmailParseOptions value);
    /// <summary>
    /// Gets the maximum path characters.
    /// </summary>
    /// <returns>The current maximum path characters.</returns>
    int GetMaximumPathCharacters();
    /// <summary>
    /// Sets the maximum path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPathCharacters(int value);
    /// <summary>
    /// Gets the require eml extension.
    /// </summary>
    /// <returns><c>true</c> when the require eml extension setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireEmlExtension();
    /// <summary>
    /// Sets the require eml extension used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require eml extension behavior.</param>
    void SetRequireEmlExtension(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailFileLoadOptions Clone();
}
