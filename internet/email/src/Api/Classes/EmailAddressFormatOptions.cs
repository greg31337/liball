using System;

namespace email;
/// <summary>
/// Controls how address format operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailAddressFormatOptions : IEmailAddressFormatOptionsContract
{
    /// <summary>
    /// The default maximum line length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineLength = 76;
    /// <summary>
    /// The default maximum encoded words used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumEncodedWords = EmailEncodedWordConstants.DefaultMaximumEncodedWords;
    EmailAddressDisplayNameFormat displayNameFormat;
    EmailAddressDomainFormat domainFormat;
    EmailEncodedWordEncodingChoice encodedWordEncoding;
    bool allowInternationalized;
    int firstLineColumn;
    int maximumLineLength;
    int maximumEncodedWords;
    int maximumOutputBytes;
    /// <summary>
    /// Creates a new address format options object.
    /// </summary>
    public EmailAddressFormatOptions()
    {
        displayNameFormat = EmailAddressDisplayNameFormat.EncodedWord;
        domainFormat = EmailAddressDomainFormat.Ascii;
        encodedWordEncoding = EmailEncodedWordEncodingChoice.Automatic;
        allowInternationalized = false;
        firstLineColumn = 0;
        maximumLineLength = DefaultMaximumLineLength;
        maximumEncodedWords = DefaultMaximumEncodedWords;
        maximumOutputBytes = (int)EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    }

    /// <summary>
    /// Gets the display name format.
    /// </summary>
    /// <returns>The current display name format.</returns>
    public EmailAddressDisplayNameFormat GetDisplayNameFormat()
    {
        return displayNameFormat;
    }

    /// <summary>
    /// Sets the display name format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetDisplayNameFormat(EmailAddressDisplayNameFormat value)
    {
        if (value < EmailAddressDisplayNameFormat.EncodedWord || value > EmailAddressDisplayNameFormat.Unicode)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        displayNameFormat = value;
    }

    /// <summary>
    /// Gets the domain format.
    /// </summary>
    /// <returns>The current domain format.</returns>
    public EmailAddressDomainFormat GetDomainFormat()
    {
        return domainFormat;
    }

    /// <summary>
    /// Sets the domain format used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetDomainFormat(EmailAddressDomainFormat value)
    {
        if (value < EmailAddressDomainFormat.Ascii || value > EmailAddressDomainFormat.Unicode)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        domainFormat = value;
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
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    public int GetFirstLineColumn()
    {
        return firstLineColumn;
    }

    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFirstLineColumn(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        firstLineColumn = value;
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
        if (value < 20 || value > DefaultMaximumLineLength)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailAddressFormatOptions Clone()
    {
        EmailAddressFormatOptions copy = new EmailAddressFormatOptions();
        copy.SetDisplayNameFormat(displayNameFormat);
        copy.SetDomainFormat(domainFormat);
        copy.SetEncodedWordEncoding(encodedWordEncoding);
        copy.SetAllowInternationalized(allowInternationalized);
        copy.SetFirstLineColumn(firstLineColumn);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumEncodedWords(maximumEncodedWords);
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        return copy;
    }
}
