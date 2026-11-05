using System;

namespace email;
/// <summary>
/// Controls how text decode operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailTextDecodeOptions : IEmailTextDecodeOptionsContract
{
    string? fallbackCharset;
    long maximumDecodedBytes;
    EmailTextNewlinePolicy newlinePolicy;
    EmailTextUnicodeNormalization unicodeNormalization;
    bool removeControlCharacters;
    bool removeBidiControls;
    EmailCharsetDecodeOptions charsetOptions;
    /// <summary>
    /// Creates a new text decode options object.
    /// </summary>
    public EmailTextDecodeOptions()
    {
        fallbackCharset = "us-ascii";
        maximumDecodedBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
        newlinePolicy = EmailTextNewlinePolicy.Preserve;
        unicodeNormalization = EmailTextUnicodeNormalization.None;
        removeControlCharacters = false;
        removeBidiControls = false;
        charsetOptions = new EmailCharsetDecodeOptions();
    }

    /// <summary>
    /// Determines whether the fallback charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fallback charset is available; otherwise, <c>false</c>.</returns>
    public bool HasFallbackCharset()
    {
        return fallbackCharset != null;
    }

    /// <summary>
    /// Gets the fallback charset.
    /// </summary>
    /// <returns>The current fallback charset.</returns>
    public string? GetFallbackCharset()
    {
        return fallbackCharset;
    }

    /// <summary>
    /// Sets the fallback charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetFallbackCharset(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (value.Length == 0)
            throw new ArgumentException("A fallback charset cannot be empty.", "value");
        fallbackCharset = value;
    }

    /// <summary>
    /// Clears the stored fallback charset value or values.
    /// </summary>
    public void ClearFallbackCharset()
    {
        fallbackCharset = null;
    }

    /// <summary>
    /// Gets the maximum decoded bytes.
    /// </summary>
    /// <returns>The current maximum decoded bytes.</returns>
    public long GetMaximumDecodedBytes()
    {
        return maximumDecodedBytes;
    }

    /// <summary>
    /// Sets the maximum decoded bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDecodedBytes(long value)
    {
        if (value < 1 || value > int.MaxValue)
            throw new ArgumentOutOfRangeException("value");
        maximumDecodedBytes = value;
    }

    /// <summary>
    /// Gets the newline policy.
    /// </summary>
    /// <returns>The current newline policy.</returns>
    public EmailTextNewlinePolicy GetNewlinePolicy()
    {
        return newlinePolicy;
    }

    /// <summary>
    /// Sets the newline policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetNewlinePolicy(EmailTextNewlinePolicy value)
    {
        if (value < EmailTextNewlinePolicy.Preserve || value > EmailTextNewlinePolicy.NormalizeToCrlf)
            throw new ArgumentOutOfRangeException("value");
        newlinePolicy = value;
    }

    /// <summary>
    /// Gets the unicode normalization.
    /// </summary>
    /// <returns>The current unicode normalization.</returns>
    public EmailTextUnicodeNormalization GetUnicodeNormalization()
    {
        return unicodeNormalization;
    }

    /// <summary>
    /// Sets the unicode normalization used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetUnicodeNormalization(EmailTextUnicodeNormalization value)
    {
        if (value < EmailTextUnicodeNormalization.None || value > EmailTextUnicodeNormalization.Nfkc)
            throw new ArgumentOutOfRangeException("value");
        unicodeNormalization = value;
    }

    /// <summary>
    /// Gets the remove control characters.
    /// </summary>
    /// <returns><c>true</c> when the remove control characters setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRemoveControlCharacters()
    {
        return removeControlCharacters;
    }

    /// <summary>
    /// Sets the remove control characters used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove control characters behavior.</param>
    public void SetRemoveControlCharacters(bool value)
    {
        removeControlCharacters = value;
    }

    /// <summary>
    /// Gets the remove bidi controls.
    /// </summary>
    /// <returns><c>true</c> when the remove bidi controls setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRemoveBidiControls()
    {
        return removeBidiControls;
    }

    /// <summary>
    /// Sets the remove bidi controls used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the remove bidi controls behavior.</param>
    public void SetRemoveBidiControls(bool value)
    {
        removeBidiControls = value;
    }

    /// <summary>
    /// Gets the charset options.
    /// </summary>
    /// <returns>The current charset options.</returns>
    public EmailCharsetDecodeOptions GetCharsetOptions()
    {
        return charsetOptions.Clone();
    }

    /// <summary>
    /// Sets the charset options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCharsetOptions(EmailCharsetDecodeOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        charsetOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTextDecodeOptions Clone()
    {
        EmailTextDecodeOptions copy = new EmailTextDecodeOptions();
        if (fallbackCharset == null)
            copy.ClearFallbackCharset();
        else
            copy.SetFallbackCharset(fallbackCharset);
        copy.SetMaximumDecodedBytes(maximumDecodedBytes);
        copy.SetNewlinePolicy(newlinePolicy);
        copy.SetUnicodeNormalization(unicodeNormalization);
        copy.SetRemoveControlCharacters(removeControlCharacters);
        copy.SetRemoveBidiControls(removeBidiControls);
        copy.SetCharsetOptions(charsetOptions);
        return copy;
    }
}
