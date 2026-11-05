using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailComposerOptions"/>.
/// </summary>
public interface IEmailComposerOptionsContract
{
    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSmtpUtf8();
    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    void SetAllowSmtpUtf8(bool value);
    /// <summary>
    /// Gets the require from.
    /// </summary>
    /// <returns><c>true</c> when the require from setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireFrom();
    /// <summary>
    /// Sets the require from used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require from behavior.</param>
    void SetRequireFrom(bool value);
    /// <summary>
    /// Gets the require date.
    /// </summary>
    /// <returns><c>true</c> when the require date setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireDate();
    /// <summary>
    /// Sets the require date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require date behavior.</param>
    void SetRequireDate(bool value);
    /// <summary>
    /// Gets the require message ID.
    /// </summary>
    /// <returns><c>true</c> when the require message ID setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireMessageId();
    /// <summary>
    /// Sets the require message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require message ID behavior.</param>
    void SetRequireMessageId(bool value);
    /// <summary>
    /// Gets the require recipient.
    /// </summary>
    /// <returns><c>true</c> when the require recipient setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireRecipient();
    /// <summary>
    /// Sets the require recipient used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require recipient behavior.</param>
    void SetRequireRecipient(bool value);
    /// <summary>
    /// Gets the maximum address count.
    /// </summary>
    /// <returns>The current maximum address count.</returns>
    int GetMaximumAddressCount();
    /// <summary>
    /// Sets the maximum address count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAddressCount(int value);
    /// <summary>
    /// Gets the maximum custom field count.
    /// </summary>
    /// <returns>The current maximum custom field count.</returns>
    int GetMaximumCustomFieldCount();
    /// <summary>
    /// Sets the maximum custom field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCustomFieldCount(int value);
    /// <summary>
    /// Gets the maximum header value bytes.
    /// </summary>
    /// <returns>The current maximum header value bytes.</returns>
    int GetMaximumHeaderValueBytes();
    /// <summary>
    /// Sets the maximum header value bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderValueBytes(int value);
    /// <summary>
    /// Gets the maximum alternative count.
    /// </summary>
    /// <returns>The current maximum alternative count.</returns>
    int GetMaximumAlternativeCount();
    /// <summary>
    /// Sets the maximum alternative count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAlternativeCount(int value);
    /// <summary>
    /// Gets the maximum attachment count.
    /// </summary>
    /// <returns>The current maximum attachment count.</returns>
    int GetMaximumAttachmentCount();
    /// <summary>
    /// Sets the maximum attachment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAttachmentCount(int value);
    /// <summary>
    /// Gets the maximum body bytes.
    /// </summary>
    /// <returns>The current maximum body bytes.</returns>
    long GetMaximumBodyBytes();
    /// <summary>
    /// Sets the maximum body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumBodyBytes(long value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailComposerOptions Clone();
}
