using System;

namespace email;
/// <summary>
/// Controls how mbox writer operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMboxWriterOptions : IEmailMboxWriterOptionsContract
{
    EmailMboxFormat format;
    EmailArchiveLimits limits;
    EmailWriteOptions writeOptions;
    string? envelopeSender;
    DateTimeOffset envelopeTimestamp;
    bool autoFlush;
    bool leaveOpen;
    /// <summary>
    /// Creates a new mbox writer options object.
    /// </summary>
    public EmailMboxWriterOptions()
    {
        format = EmailMboxFormat.Mboxrd;
        limits = new EmailArchiveLimits();
        writeOptions = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        envelopeSender = null;
        envelopeTimestamp = DateTimeOffset.UnixEpoch;
        autoFlush = false;
        leaveOpen = true;
    }

    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    public EmailMboxFormat GetFormat()
    {
        return format;
    }

    /// <summary>
    /// Sets the format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFormat(EmailMboxFormat value)
    {
        if (value < EmailMboxFormat.Mboxo || value > EmailMboxFormat.Mboxrd)
            throw new ArgumentOutOfRangeException("value");
        format = value;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailArchiveLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailArchiveLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the write options.
    /// </summary>
    /// <returns>The current write options.</returns>
    public EmailWriteOptions GetWriteOptions()
    {
        return writeOptions.Clone();
    }

    /// <summary>
    /// Sets the write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetWriteOptions(EmailWriteOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        writeOptions = value.Clone();
    }

    /// <summary>
    /// Determines whether the envelope sender is present or available.
    /// </summary>
    /// <returns><c>true</c> when the envelope sender is available; otherwise, <c>false</c>.</returns>
    public bool HasEnvelopeSender()
    {
        return envelopeSender != null;
    }

    /// <summary>
    /// Gets the envelope sender.
    /// </summary>
    /// <returns>The current envelope sender.</returns>
    public string? GetEnvelopeSender()
    {
        return envelopeSender;
    }

    /// <summary>
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetEnvelopeSender(string value)
    {
        new EmailMboxWriterOperations().ValidateEnvelopeSender(value, "value");
        envelopeSender = value;
    }

    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    public void ClearEnvelopeSender()
    {
        envelopeSender = null;
    }

    /// <summary>
    /// Gets the envelope timestamp.
    /// </summary>
    /// <returns>The current envelope timestamp.</returns>
    public DateTimeOffset GetEnvelopeTimestamp()
    {
        return envelopeTimestamp;
    }

    /// <summary>
    /// Sets the envelope timestamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetEnvelopeTimestamp(DateTimeOffset value)
    {
        envelopeTimestamp = value;
    }

    /// <summary>
    /// Gets the auto flush.
    /// </summary>
    /// <returns><c>true</c> when the auto flush setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAutoFlush()
    {
        return autoFlush;
    }

    /// <summary>
    /// Sets the auto flush used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the auto flush behavior.</param>
    public void SetAutoFlush(bool value)
    {
        autoFlush = value;
    }

    /// <summary>
    /// Gets the leave open.
    /// </summary>
    /// <returns><c>true</c> when the leave open setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetLeaveOpen()
    {
        return leaveOpen;
    }

    /// <summary>
    /// Sets the leave open used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the leave open behavior.</param>
    public void SetLeaveOpen(bool value)
    {
        leaveOpen = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailMboxWriterOptions Clone()
    {
        EmailMboxWriterOptions copy = new EmailMboxWriterOptions();
        copy.SetFormat(format);
        copy.SetLimits(limits);
        copy.SetWriteOptions(writeOptions);
        if (envelopeSender != null)
            copy.SetEnvelopeSender(envelopeSender);
        copy.SetEnvelopeTimestamp(envelopeTimestamp);
        copy.SetAutoFlush(autoFlush);
        copy.SetLeaveOpen(leaveOpen);
        return copy;
    }
}
