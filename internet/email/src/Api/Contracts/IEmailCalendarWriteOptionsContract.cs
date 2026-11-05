using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarWriteOptions"/>.
/// </summary>
public interface IEmailCalendarWriteOptionsContract
{
    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    int GetMaximumOutputBytes();
    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumOutputBytes(int value);
    /// <summary>
    /// Gets the fold octets.
    /// </summary>
    /// <returns>The current fold octets.</returns>
    int GetFoldOctets();
    /// <summary>
    /// Sets the fold octets used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFoldOctets(int value);
    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    string GetCharset();
    /// <summary>
    /// Sets the charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCharset(string value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCalendarWriteOptions Clone();
}
