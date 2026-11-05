using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirReaderOptions"/>.
/// </summary>
public interface IEmailMaildirReaderOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailArchiveLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailArchiveLimits value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMaildirReaderOptions Clone();
}
