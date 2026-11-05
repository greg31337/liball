using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimKeyParserOptions"/>.
/// </summary>
public interface IEmailDkimKeyParserOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailStructuredValueParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailStructuredValueParseMode value);
    /// <summary>
    /// Gets the maximum tags.
    /// </summary>
    /// <returns>The current maximum tags.</returns>
    int GetMaximumTags();
    /// <summary>
    /// Sets the maximum tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumTags(int value);
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
    /// Gets the maximum decoded key bytes.
    /// </summary>
    /// <returns>The current maximum decoded key bytes.</returns>
    long GetMaximumDecodedKeyBytes();
    /// <summary>
    /// Sets the maximum decoded key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumDecodedKeyBytes(long value);
    /// <summary>
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    int GetMinimumRsaKeyBits();
    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMinimumRsaKeyBits(int value);
    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    int GetMaximumRsaKeyBits();
    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumRsaKeyBits(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDkimKeyParserOptions Clone();
}
