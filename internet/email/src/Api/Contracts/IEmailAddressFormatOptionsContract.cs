using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressFormatOptions"/>.
/// </summary>
public interface IEmailAddressFormatOptionsContract
{
    /// <summary>
    /// Gets the display name format.
    /// </summary>
    /// <returns>The current display name format.</returns>
    EmailAddressDisplayNameFormat GetDisplayNameFormat();
    /// <summary>
    /// Sets the display name format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetDisplayNameFormat(EmailAddressDisplayNameFormat value);
    /// <summary>
    /// Gets the domain format.
    /// </summary>
    /// <returns>The current domain format.</returns>
    EmailAddressDomainFormat GetDomainFormat();
    /// <summary>
    /// Sets the domain format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetDomainFormat(EmailAddressDomainFormat value);
    /// <summary>
    /// Gets the encoded word encoding.
    /// </summary>
    /// <returns>The current encoded word encoding.</returns>
    EmailEncodedWordEncodingChoice GetEncodedWordEncoding();
    /// <summary>
    /// Sets the encoded word encoding used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetEncodedWordEncoding(EmailEncodedWordEncodingChoice value);
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
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    int GetFirstLineColumn();
    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFirstLineColumn(int value);
    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    int GetMaximumLineLength();
    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLineLength(int value);
    /// <summary>
    /// Gets the maximum encoded words.
    /// </summary>
    /// <returns>The current maximum encoded words.</returns>
    int GetMaximumEncodedWords();
    /// <summary>
    /// Sets the maximum encoded words used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumEncodedWords(int value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailAddressFormatOptions Clone();
}
