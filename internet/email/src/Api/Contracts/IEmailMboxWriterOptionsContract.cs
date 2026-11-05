using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxWriterOptions"/>.
/// </summary>
public interface IEmailMboxWriterOptionsContract
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
    /// Gets the write options.
    /// </summary>
    /// <returns>The current write options.</returns>
    EmailWriteOptions GetWriteOptions();
    /// <summary>
    /// Sets the write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetWriteOptions(EmailWriteOptions value);
    /// <summary>
    /// Determines whether the envelope sender is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope sender is available; otherwise, <c>false</c>.</returns>
    bool HasEnvelopeSender();
    /// <summary>
    /// Gets the envelope sender.
    /// </summary>
    /// <returns>The current envelope sender.</returns>
    string? GetEnvelopeSender();
    /// <summary>
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetEnvelopeSender(string value);
    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    void ClearEnvelopeSender();
    /// <summary>
    /// Gets the envelope timestamp.
    /// </summary>
    /// <returns>The current envelope timestamp.</returns>
    DateTimeOffset GetEnvelopeTimestamp();
    /// <summary>
    /// Sets the envelope timestamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetEnvelopeTimestamp(DateTimeOffset value);
    /// <summary>
    /// Gets the auto flush.
    /// </summary>
    /// <returns><c>true</c> when the auto flush setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAutoFlush();
    /// <summary>
    /// Sets the auto flush used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the auto flush behavior.</param>
    void SetAutoFlush(bool value);
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
    EmailMboxWriterOptions Clone();
}
