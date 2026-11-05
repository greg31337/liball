using System;

namespace email;
/// <summary>
/// Controls how composer operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailComposerOptions : IEmailComposerOptionsContract
{
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAddressCount = 1000;
    /// <summary>
    /// The default maximum custom field count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumCustomFieldCount = 100;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderValueBytes = 65536;
    /// <summary>
    /// The default maximum alternative count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAlternativeCount = 32;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum body bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumBodyBytes = 104857600;
    bool allowSmtpUtf8;
    bool requireFrom;
    bool requireDate;
    bool requireMessageId;
    bool requireRecipient;
    int maximumAddressCount;
    int maximumCustomFieldCount;
    int maximumHeaderValueBytes;
    int maximumAlternativeCount;
    int maximumAttachmentCount;
    long maximumBodyBytes;
    /// <summary>
    /// Creates a new composer options object.
    /// </summary>
    public EmailComposerOptions()
    {
        allowSmtpUtf8 = false;
        requireFrom = true;
        requireDate = true;
        requireMessageId = false;
        requireRecipient = true;
        maximumAddressCount = DefaultMaximumAddressCount;
        maximumCustomFieldCount = DefaultMaximumCustomFieldCount;
        maximumHeaderValueBytes = DefaultMaximumHeaderValueBytes;
        maximumAlternativeCount = DefaultMaximumAlternativeCount;
        maximumAttachmentCount = DefaultMaximumAttachmentCount;
        maximumBodyBytes = DefaultMaximumBodyBytes;
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
    /// Gets the require from.
    /// </summary>
    /// <returns><c>true</c> when the require from setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireFrom()
    {
        return requireFrom;
    }

    /// <summary>
    /// Sets the require from used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require from behavior.</param>
    public void SetRequireFrom(bool value)
    {
        requireFrom = value;
    }

    /// <summary>
    /// Gets the require date.
    /// </summary>
    /// <returns><c>true</c> when the require date setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireDate()
    {
        return requireDate;
    }

    /// <summary>
    /// Sets the require date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require date behavior.</param>
    public void SetRequireDate(bool value)
    {
        requireDate = value;
    }

    /// <summary>
    /// Gets the require message ID.
    /// </summary>
    /// <returns><c>true</c> when the require message ID setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireMessageId()
    {
        return requireMessageId;
    }

    /// <summary>
    /// Sets the require message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require message ID behavior.</param>
    public void SetRequireMessageId(bool value)
    {
        requireMessageId = value;
    }

    /// <summary>
    /// Gets the require recipient.
    /// </summary>
    /// <returns><c>true</c> when the require recipient setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireRecipient()
    {
        return requireRecipient;
    }

    /// <summary>
    /// Sets the require recipient used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require recipient behavior.</param>
    public void SetRequireRecipient(bool value)
    {
        requireRecipient = value;
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
    /// Gets the maximum custom field count.
    /// </summary>
    /// <returns>The current maximum custom field count.</returns>
    public int GetMaximumCustomFieldCount()
    {
        return maximumCustomFieldCount;
    }

    /// <summary>
    /// Sets the maximum custom field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCustomFieldCount(int value)
    {
        maximumCustomFieldCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Gets the maximum alternative count.
    /// </summary>
    /// <returns>The current maximum alternative count.</returns>
    public int GetMaximumAlternativeCount()
    {
        return maximumAlternativeCount;
    }

    /// <summary>
    /// Sets the maximum alternative count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAlternativeCount(int value)
    {
        maximumAlternativeCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum attachment count.
    /// </summary>
    /// <returns>The current maximum attachment count.</returns>
    public int GetMaximumAttachmentCount()
    {
        return maximumAttachmentCount;
    }

    /// <summary>
    /// Sets the maximum attachment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAttachmentCount(int value)
    {
        maximumAttachmentCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum body bytes.
    /// </summary>
    /// <returns>The current maximum body bytes.</returns>
    public long GetMaximumBodyBytes()
    {
        return maximumBodyBytes;
    }

    /// <summary>
    /// Sets the maximum body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumBodyBytes(long value)
    {
        maximumBodyBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailComposerOptions Clone()
    {
        EmailComposerOptions copy = new EmailComposerOptions();
        copy.SetAllowSmtpUtf8(allowSmtpUtf8);
        copy.SetRequireFrom(requireFrom);
        copy.SetRequireDate(requireDate);
        copy.SetRequireMessageId(requireMessageId);
        copy.SetRequireRecipient(requireRecipient);
        copy.SetMaximumAddressCount(maximumAddressCount);
        copy.SetMaximumCustomFieldCount(maximumCustomFieldCount);
        copy.SetMaximumHeaderValueBytes(maximumHeaderValueBytes);
        copy.SetMaximumAlternativeCount(maximumAlternativeCount);
        copy.SetMaximumAttachmentCount(maximumAttachmentCount);
        copy.SetMaximumBodyBytes(maximumBodyBytes);
        return copy;
    }
}
