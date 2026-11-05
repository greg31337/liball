using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderWriteOptions"/>.
/// </summary>
public interface IEmailHeaderWriteOptionsContract
{
    /// <summary>
    /// Gets the preferred line length.
    /// </summary>
    /// <returns>The current preferred line length.</returns>
    int GetPreferredLineLength();
    /// <summary>
    /// Sets the preferred line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetPreferredLineLength(int value);
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
    /// Gets the include separator.
    /// </summary>
    /// <returns><c>true</c> when the include separator setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeSeparator();
    /// <summary>
    /// Sets the include separator used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include separator behavior.</param>
    void SetIncludeSeparator(bool value);
    /// <summary>
    /// Gets the check cardinality.
    /// </summary>
    /// <returns><c>true</c> when the check cardinality setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetCheckCardinality();
    /// <summary>
    /// Sets the check cardinality used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the check cardinality behavior.</param>
    void SetCheckCardinality(bool value);
    /// <summary>
    /// Gets the canonicalize field names.
    /// </summary>
    /// <returns><c>true</c> when the canonicalize field names setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetCanonicalizeFieldNames();
    /// <summary>
    /// Sets the canonicalize field names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the canonicalize field names behavior.</param>
    void SetCanonicalizeFieldNames(bool value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeaderWriteOptions Clone();
}
