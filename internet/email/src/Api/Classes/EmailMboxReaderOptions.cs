using System;

namespace email;
/// <summary>
/// Controls how mbox reader operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMboxReaderOptions : IEmailMboxReaderOptionsContract
{
    /// <summary>
    /// The default read buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultReadBufferBytes = 8192;
    EmailMboxFormat format;
    EmailArchiveLimits limits;
    EmailParseOptions parseOptions;
    int readBufferBytes;
    bool leaveOpen;
    /// <summary>
    /// Creates a new mbox reader options object.
    /// </summary>
    public EmailMboxReaderOptions()
    {
        format = EmailMboxFormat.Mboxrd;
        limits = new EmailArchiveLimits();
        parseOptions = new EmailParseOptionsOperations().CreatePreserving();
        readBufferBytes = DefaultReadBufferBytes;
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
    /// Gets the parse options.
    /// </summary>
    /// <returns>The current parse options.</returns>
    public EmailParseOptions GetParseOptions()
    {
        return parseOptions.Clone();
    }

    /// <summary>
    /// Sets the parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetParseOptions(EmailParseOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        parseOptions = value.Clone();
    }

    /// <summary>
    /// Gets the read buffer bytes.
    /// </summary>
    /// <returns>The current read buffer bytes.</returns>
    public int GetReadBufferBytes()
    {
        return readBufferBytes;
    }

    /// <summary>
    /// Sets the read buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetReadBufferBytes(int value)
    {
        readBufferBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailMboxReaderOptions Clone()
    {
        EmailMboxReaderOptions copy = new EmailMboxReaderOptions();
        copy.SetFormat(format);
        copy.SetLimits(limits);
        copy.SetParseOptions(parseOptions);
        copy.SetReadBufferBytes(readBufferBytes);
        copy.SetLeaveOpen(leaveOpen);
        return copy;
    }
}
