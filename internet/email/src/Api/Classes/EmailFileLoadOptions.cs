using System;

namespace email;
/// <summary>
/// Controls how file load operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailFileLoadOptions : IEmailFileLoadOptionsContract
{
    EmailParseOptions parseOptions;
    int maximumPathCharacters;
    bool requireEmlExtension;
    /// <summary>
    /// Creates a new file load options object.
    /// </summary>
    public EmailFileLoadOptions()
    {
        parseOptions = new EmailParseOptionsOperations().CreatePreserving();
        maximumPathCharacters = EmailArchiveLimitsConstants.DefaultMaxPathCharacters;
        requireEmlExtension = true;
    }

    /// <summary>
    /// Gets the parse options.
    /// </summary>
    /// <returns>The current parse options.</returns>
    public EmailParseOptions GetParseOptions()
    {
        return parseOptions.Clone();
    }

    /// <summary>
    /// Sets the parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetParseOptions(EmailParseOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        parseOptions = value.Clone();
    }

    /// <summary>
    /// Gets the maximum path characters.
    /// </summary>
    /// <returns>The current maximum path characters.</returns>
    public int GetMaximumPathCharacters()
    {
        return maximumPathCharacters;
    }

    /// <summary>
    /// Sets the maximum path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPathCharacters(int value)
    {
        maximumPathCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the require eml extension.
    /// </summary>
    /// <returns><c>true</c> when the require eml extension setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireEmlExtension()
    {
        return requireEmlExtension;
    }

    /// <summary>
    /// Sets the require eml extension used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require eml extension behavior.</param>
    public void SetRequireEmlExtension(bool value)
    {
        requireEmlExtension = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailFileLoadOptions Clone()
    {
        EmailFileLoadOptions copy = new EmailFileLoadOptions();
        copy.SetParseOptions(parseOptions);
        copy.SetMaximumPathCharacters(maximumPathCharacters);
        copy.SetRequireEmlExtension(requireEmlExtension);
        return copy;
    }
}
