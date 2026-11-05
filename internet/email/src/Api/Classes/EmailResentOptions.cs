using System;

namespace email;
/// <summary>
/// Controls how resent operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailResentOptions : IEmailResentOptionsContract
{
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAddressCount = 1000;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderValueBytes = 65536;
    bool allowSmtpUtf8;
    int maximumAddressCount;
    int maximumHeaderValueBytes;
    /// <summary>
    /// Creates a new resent options object.
    /// </summary>
    public EmailResentOptions()
    {
        allowSmtpUtf8 = false;
        maximumAddressCount = DefaultMaximumAddressCount;
        maximumHeaderValueBytes = DefaultMaximumHeaderValueBytes;
    }

    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSmtpUtf8()
    {
        return allowSmtpUtf8;
    }

    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    public void SetAllowSmtpUtf8(bool value)
    {
        allowSmtpUtf8 = value;
    }

    /// <summary>
    /// Gets the maximum address count.
    /// </summary>
    /// <returns>The current maximum address count.</returns>
    public int GetMaximumAddressCount()
    {
        return maximumAddressCount;
    }

    /// <summary>
    /// Sets the maximum address count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAddressCount(int value)
    {
        maximumAddressCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum header value bytes.
    /// </summary>
    /// <returns>The current maximum header value bytes.</returns>
    public int GetMaximumHeaderValueBytes()
    {
        return maximumHeaderValueBytes;
    }

    /// <summary>
    /// Sets the maximum header value bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderValueBytes(int value)
    {
        maximumHeaderValueBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailResentOptions Clone()
    {
        EmailResentOptions copy = new EmailResentOptions();
        copy.SetAllowSmtpUtf8(allowSmtpUtf8);
        copy.SetMaximumAddressCount(maximumAddressCount);
        copy.SetMaximumHeaderValueBytes(maximumHeaderValueBytes);
        return copy;
    }
}
