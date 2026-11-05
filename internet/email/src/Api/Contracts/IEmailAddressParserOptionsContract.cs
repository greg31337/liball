using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressParserOptions"/>.
/// </summary>
public interface IEmailAddressParserOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailAddressParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailAddressParseMode value);
    /// <summary>
    /// Gets the maximum addresses.
    /// </summary>
    /// <returns>The current maximum addresses.</returns>
    int GetMaximumAddresses();
    /// <summary>
    /// Sets the maximum addresses used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAddresses(int value);
    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    int GetMaximumInputBytes();
    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumInputBytes(int value);
    /// <summary>
    /// Gets the allow internationalized.
    /// </summary>
    /// <returns><c>true</c> when the allow internationalized setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowInternationalized();
    /// <summary>
    /// Sets the allow internationalized used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow internationalized behavior.</param>
    void SetAllowInternationalized(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailAddressParserOptions Clone();
}
