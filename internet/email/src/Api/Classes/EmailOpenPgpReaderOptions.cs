using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Controls how openPGP reader operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailOpenPgpReaderOptions : IEmailOpenPgpReaderOptionsContract
{
    EmailCryptographicLimits limits;
    bool allowLegacyPacketHeaders;
    int maximumArmorLineBytes;
    int maximumArmorHeaderCount;
    /// <summary>
    /// Creates a new openPGP reader options object.
    /// </summary>
    public EmailOpenPgpReaderOptions()
    {
        limits = new EmailCryptographicLimits();
        allowLegacyPacketHeaders = true;
        maximumArmorLineBytes = 4096;
        maximumArmorHeaderCount = 64;
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
    /// Gets the allow legacy packet headers.
    /// </summary>
    /// <returns><c>true</c> when the allow legacy packet headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowLegacyPacketHeaders()
    {
        return allowLegacyPacketHeaders;
    }

    /// <summary>
    /// Sets the allow legacy packet headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow legacy packet headers behavior.</param>
    public void SetAllowLegacyPacketHeaders(bool value)
    {
        allowLegacyPacketHeaders = value;
    }

    /// <summary>
    /// Gets the maximum armor line bytes.
    /// </summary>
    /// <returns>The current maximum armor line bytes.</returns>
    public int GetMaximumArmorLineBytes()
    {
        return maximumArmorLineBytes;
    }

    /// <summary>
    /// Sets the maximum armor line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumArmorLineBytes(int value)
    {
        maximumArmorLineBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum armor header count.
    /// </summary>
    /// <returns>The current maximum armor header count.</returns>
    public int GetMaximumArmorHeaderCount()
    {
        return maximumArmorHeaderCount;
    }

    /// <summary>
    /// Sets the maximum armor header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumArmorHeaderCount(int value)
    {
        maximumArmorHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailOpenPgpReaderOptions Clone()
    {
        EmailOpenPgpReaderOptions copy = new EmailOpenPgpReaderOptions();
        copy.SetLimits(limits);
        copy.SetAllowLegacyPacketHeaders(allowLegacyPacketHeaders);
        copy.SetMaximumArmorLineBytes(maximumArmorLineBytes);
        copy.SetMaximumArmorHeaderCount(maximumArmorHeaderCount);
        return copy;
    }
}
