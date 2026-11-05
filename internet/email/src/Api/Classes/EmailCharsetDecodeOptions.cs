using System;

namespace email;
/// <summary>
/// Controls how charset decode operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCharsetDecodeOptions : IEmailCharsetDecodeOptionsContract
{
    EmailCharsetFallbackPolicy fallbackPolicy;
    EmailCharsetBomPolicy bomPolicy;
    string replacement;
    int maximumCharacters;
    int maximumCharsetNameLength;
    /// <summary>
    /// Creates a new charset decode options object.
    /// </summary>
    public EmailCharsetDecodeOptions()
    {
        fallbackPolicy = EmailCharsetFallbackPolicy.Replacement;
        bomPolicy = EmailCharsetBomPolicy.Detect;
        replacement = "\uFFFD";
        maximumCharacters = EmailDecodingLimitsConstants.DefaultMaxTextCharacters;
        maximumCharsetNameLength = EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength;
    }

    /// <summary>
    /// Gets the fallback policy.
    /// </summary>
    /// <returns>The current fallback policy.</returns>
    public EmailCharsetFallbackPolicy GetFallbackPolicy()
    {
        return fallbackPolicy;
    }

    /// <summary>
    /// Sets the fallback policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFallbackPolicy(EmailCharsetFallbackPolicy value)
    {
        if (value < EmailCharsetFallbackPolicy.Strict || value > EmailCharsetFallbackPolicy.Replacement)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        fallbackPolicy = value;
    }

    /// <summary>
    /// Gets the BOM policy.
    /// </summary>
    /// <returns>The current BOM policy.</returns>
    public EmailCharsetBomPolicy GetBomPolicy()
    {
        return bomPolicy;
    }

    /// <summary>
    /// Sets the BOM policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetBomPolicy(EmailCharsetBomPolicy value)
    {
        if (value < EmailCharsetBomPolicy.Ignore || value > EmailCharsetBomPolicy.RequireMatch)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        bomPolicy = value;
    }

    /// <summary>
    /// Gets the replacement.
    /// </summary>
    /// <returns>The current replacement.</returns>
    public string GetReplacement()
    {
        return replacement;
    }

    /// <summary>
    /// Sets the replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetReplacement(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Charset replacement text cannot be empty.", "value");
        }

        replacement = value;
    }

    /// <summary>
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    public int GetMaximumCharacters()
    {
        return maximumCharacters;
    }

    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumCharacters(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumCharacters = value;
    }

    /// <summary>
    /// Gets the maximum charset name length.
    /// </summary>
    /// <returns>The current maximum charset name length.</returns>
    public int GetMaximumCharsetNameLength()
    {
        return maximumCharsetNameLength;
    }

    /// <summary>
    /// Sets the maximum charset name length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumCharsetNameLength(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumCharsetNameLength = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCharsetDecodeOptions Clone()
    {
        EmailCharsetDecodeOptions copy = new EmailCharsetDecodeOptions();
        copy.SetFallbackPolicy(fallbackPolicy);
        copy.SetBomPolicy(bomPolicy);
        copy.SetReplacement(replacement);
        copy.SetMaximumCharacters(maximumCharacters);
        copy.SetMaximumCharsetNameLength(maximumCharsetNameLength);
        return copy;
    }
}
