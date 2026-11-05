using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxReaderOptions"/>.
/// </summary>
public interface IEmailMboxReaderOptionsContract
{
    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    EmailMboxFormat GetFormat();
    /// <summary>
    /// Sets the format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFormat(EmailMboxFormat value);
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
    /// Gets the read buffer bytes.
    /// </summary>
    /// <returns>The current read buffer bytes.</returns>
    int GetReadBufferBytes();
    /// <summary>
    /// Sets the read buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetReadBufferBytes(int value);
    /// <summary>
    /// Gets the leave open.
    /// </summary>
    /// <returns><c>true</c> when the leave open setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetLeaveOpen();
    /// <summary>
    /// Sets the leave open used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the leave open behavior.</param>
    void SetLeaveOpen(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMboxReaderOptions Clone();
}
