using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailResentOptions"/>.
/// </summary>
public interface IEmailResentOptionsContract
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailResentOptions Clone();
}
