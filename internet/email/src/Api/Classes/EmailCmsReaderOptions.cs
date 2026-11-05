using System;

namespace email;
/// <summary>
/// Controls how CMS reader operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCmsReaderOptions : IEmailCmsReaderOptionsContract
{
    /// <summary>
    /// The default read buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultReadBufferBytes = 8192;
    EmailCryptographicLimits limits;
    bool allowBer;
    int readBufferBytes;
    /// <summary>
    /// Creates a new CMS reader options object.
    /// </summary>
    public EmailCmsReaderOptions()
    {
        limits = new EmailCryptographicLimits();
        allowBer = true;
        readBufferBytes = DefaultReadBufferBytes;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the allow ber.
    /// </summary>
    /// <returns><c>true</c> when the allow ber setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowBer()
    {
        return allowBer;
    }

    /// <summary>
    /// Sets the allow ber used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow ber behavior.</param>
    public void SetAllowBer(bool value)
    {
        allowBer = value;
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCmsReaderOptions Clone()
    {
        EmailCmsReaderOptions copy = new EmailCmsReaderOptions();
        copy.SetLimits(limits);
        copy.SetAllowBer(allowBer);
        copy.SetReadBufferBytes(readBufferBytes);
        return copy;
    }
}
