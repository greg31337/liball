using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCmsReaderOptions"/>.
/// </summary>
public interface IEmailCmsReaderOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailCryptographicLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailCryptographicLimits value);
    /// <summary>
    /// Gets the allow ber.
    /// </summary>
    /// <returns><c>true</c> when the allow ber setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowBer();
    /// <summary>
    /// Sets the allow ber used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow ber behavior.</param>
    void SetAllowBer(bool value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCmsReaderOptions Clone();
}
