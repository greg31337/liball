using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextDecodeOptions"/>.
/// </summary>
public interface IEmailTextDecodeOptionsContract
{
    /// <summary>
    /// Determines whether the fallback charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fallback charset is available; otherwise, <c>false</c>.</returns>
    bool HasFallbackCharset();
    /// <summary>
    /// Gets the fallback charset.
    /// </summary>
    /// <returns>The current fallback charset.</returns>
    string? GetFallbackCharset();
    /// <summary>
    /// Sets the fallback charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetFallbackCharset(string value);
    /// <summary>
    /// Clears the stored fallback charset value or values.
    /// </summary>
    void ClearFallbackCharset();
    /// <summary>
    /// Gets the maximum decoded bytes.
    /// </summary>
    /// <returns>The current maximum decoded bytes.</returns>
    long GetMaximumDecodedBytes();
    /// <summary>
    /// Sets the maximum decoded bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDecodedBytes(long value);
    /// <summary>
    /// Gets the newline policy.
    /// </summary>
    /// <returns>The current newline policy.</returns>
    EmailTextNewlinePolicy GetNewlinePolicy();
    /// <summary>
    /// Sets the newline policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetNewlinePolicy(EmailTextNewlinePolicy value);
    /// <summary>
    /// Gets the unicode normalization.
    /// </summary>
    /// <returns>The current unicode normalization.</returns>
    EmailTextUnicodeNormalization GetUnicodeNormalization();
    /// <summary>
    /// Sets the unicode normalization used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetUnicodeNormalization(EmailTextUnicodeNormalization value);
    /// <summary>
    /// Gets the remove control characters.
    /// </summary>
    /// <returns><c>true</c> when the remove control characters setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRemoveControlCharacters();
    /// <summary>
    /// Sets the remove control characters used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove control characters behavior.</param>
    void SetRemoveControlCharacters(bool value);
    /// <summary>
    /// Gets the remove bidi controls.
    /// </summary>
    /// <returns><c>true</c> when the remove bidi controls setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRemoveBidiControls();
    /// <summary>
    /// Sets the remove bidi controls used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove bidi controls behavior.</param>
    void SetRemoveBidiControls(bool value);
    /// <summary>
    /// Gets the charset options.
    /// </summary>
    /// <returns>The current charset options.</returns>
    EmailCharsetDecodeOptions GetCharsetOptions();
    /// <summary>
    /// Sets the charset options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCharsetOptions(EmailCharsetDecodeOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTextDecodeOptions Clone();
}
