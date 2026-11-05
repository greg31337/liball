using System;

namespace email;
/// <summary>
/// Controls how address parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailAddressParserOptions : IEmailAddressParserOptionsContract
{
    EmailAddressParseMode mode;
    int maximumAddresses;
    int maximumInputBytes;
    bool allowInternationalized;
    /// <summary>
    /// Creates a new address parser options object.
    /// </summary>
    public EmailAddressParserOptions()
    {
        mode = EmailAddressParseMode.Tolerant;
        maximumAddresses = EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes > int.MaxValue ? int.MaxValue : (int)EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
        allowInternationalized = true;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailAddressParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailAddressParseMode value)
    {
        if (value < EmailAddressParseMode.Strict || value > EmailAddressParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the maximum addresses.
    /// </summary>
    /// <returns>The current maximum addresses.</returns>
    public int GetMaximumAddresses()
    {
        return maximumAddresses;
    }

    /// <summary>
    /// Sets the maximum addresses used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAddresses(int value)
    {
        maximumAddresses = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    public int GetMaximumInputBytes()
    {
        return maximumInputBytes;
    }

    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumInputBytes(int value)
    {
        maximumInputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailAddressParserOptions Clone()
    {
        EmailAddressParserOptions copy = new EmailAddressParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumAddresses(maximumAddresses);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetAllowInternationalized(allowInternationalized);
        return copy;
    }
}
