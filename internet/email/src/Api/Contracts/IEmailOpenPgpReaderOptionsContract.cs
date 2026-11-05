using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpReaderOptions"/>.
/// </summary>
public interface IEmailOpenPgpReaderOptionsContract
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
    /// Gets the allow legacy packet headers.
    /// </summary>
    /// <returns><c>true</c> when the allow legacy packet headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowLegacyPacketHeaders();
    /// <summary>
    /// Sets the allow legacy packet headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow legacy packet headers behavior.</param>
    void SetAllowLegacyPacketHeaders(bool value);
    /// <summary>
    /// Gets the maximum armor line bytes.
    /// </summary>
    /// <returns>The current maximum armor line bytes.</returns>
    int GetMaximumArmorLineBytes();
    /// <summary>
    /// Sets the maximum armor line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumArmorLineBytes(int value);
    /// <summary>
    /// Gets the maximum armor header count.
    /// </summary>
    /// <returns>The current maximum armor header count.</returns>
    int GetMaximumArmorHeaderCount();
    /// <summary>
    /// Sets the maximum armor header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumArmorHeaderCount(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailOpenPgpReaderOptions Clone();
}
