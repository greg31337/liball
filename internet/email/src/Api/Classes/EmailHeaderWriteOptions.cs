using System;

namespace email;
/// <summary>
/// Controls how header write operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHeaderWriteOptions : IEmailHeaderWriteOptionsContract
{
    int preferredLineLength;
    int maximumLineLength;
    int maximumOutputBytes;
    int maximumEncodedWords;
    bool allowInternationalized;
    bool includeSeparator;
    bool checkCardinality;
    bool canonicalizeFieldNames;
    EmailEncodedWordEncodingChoice encodedWordEncoding;
    /// <summary>
    /// Creates a new header write options object.
    /// </summary>
    public EmailHeaderWriteOptions()
    {
        preferredLineLength = 78;
        maximumLineLength = 998;
        maximumOutputBytes = (int)EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
        maximumEncodedWords = EmailEncodedWordConstants.DefaultMaximumEncodedWords;
        allowInternationalized = false;
        includeSeparator = true;
        checkCardinality = true;
        canonicalizeFieldNames = true;
        encodedWordEncoding = EmailEncodedWordEncodingChoice.Automatic;
    }

    /// <summary>
    /// Gets the preferred line length.
    /// </summary>
    /// <returns>The current preferred line length.</returns>
    public int GetPreferredLineLength()
    {
        return preferredLineLength;
    }

    /// <summary>
    /// Sets the preferred line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetPreferredLineLength(int value)
    {
        if (value < 20 || value > maximumLineLength)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        preferredLineLength = value;
    }

    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    public int GetMaximumLineLength()
    {
        return maximumLineLength;
    }

    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLineLength(int value)
    {
        if (value < preferredLineLength || value > 998)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
    }

    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    public int GetMaximumOutputBytes()
    {
        return maximumOutputBytes;
    }

    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumOutputBytes(int value)
    {
        maximumOutputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum encoded words.
    /// </summary>
    /// <returns>The current maximum encoded words.</returns>
    public int GetMaximumEncodedWords()
    {
        return maximumEncodedWords;
    }

    /// <summary>
    /// Sets the maximum encoded words used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumEncodedWords(int value)
    {
        maximumEncodedWords = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the allow internationalized.
    /// </summary>
    /// <returns><c>true</c> when the allow internationalized setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowInternationalized()
    {
        return allowInternationalized;
    }

    /// <summary>
    /// Sets the allow internationalized used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow internationalized behavior.</param>
    public void SetAllowInternationalized(bool value)
    {
        allowInternationalized = value;
    }

    /// <summary>
    /// Gets the include separator.
    /// </summary>
    /// <returns><c>true</c> when the include separator setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeSeparator()
    {
        return includeSeparator;
    }

    /// <summary>
    /// Sets the include separator used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include separator behavior.</param>
    public void SetIncludeSeparator(bool value)
    {
        includeSeparator = value;
    }

    /// <summary>
    /// Gets the check cardinality.
    /// </summary>
    /// <returns><c>true</c> when the check cardinality setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetCheckCardinality()
    {
        return checkCardinality;
    }

    /// <summary>
    /// Sets the check cardinality used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the check cardinality behavior.</param>
    public void SetCheckCardinality(bool value)
    {
        checkCardinality = value;
    }

    /// <summary>
    /// Gets the canonicalize field names.
    /// </summary>
    /// <returns><c>true</c> when the canonicalize field names setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetCanonicalizeFieldNames()
    {
        return canonicalizeFieldNames;
    }

    /// <summary>
    /// Sets the canonicalize field names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the canonicalize field names behavior.</param>
    public void SetCanonicalizeFieldNames(bool value)
    {
        canonicalizeFieldNames = value;
    }

    /// <summary>
    /// Gets the encoded word encoding.
    /// </summary>
    /// <returns>The current encoded word encoding.</returns>
    public EmailEncodedWordEncodingChoice GetEncodedWordEncoding()
    {
        return encodedWordEncoding;
    }

    /// <summary>
    /// Sets the encoded word encoding used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetEncodedWordEncoding(EmailEncodedWordEncodingChoice value)
    {
        if (value < EmailEncodedWordEncodingChoice.Automatic || value > EmailEncodedWordEncodingChoice.QuotedPrintable)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        encodedWordEncoding = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHeaderWriteOptions Clone()
    {
        EmailHeaderWriteOptions copy = new EmailHeaderWriteOptions();
        copy.SetPreferredLineLength(preferredLineLength);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        copy.SetMaximumEncodedWords(maximumEncodedWords);
        copy.SetAllowInternationalized(allowInternationalized);
        copy.SetIncludeSeparator(includeSeparator);
        copy.SetCheckCardinality(checkCardinality);
        copy.SetCanonicalizeFieldNames(canonicalizeFieldNames);
        copy.SetEncodedWordEncoding(encodedWordEncoding);
        return copy;
    }
}
