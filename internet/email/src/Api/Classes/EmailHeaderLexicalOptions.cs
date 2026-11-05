using System;

namespace email;
/// <summary>
/// Controls how header lexical operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHeaderLexicalOptions : IEmailHeaderLexicalOptionsContract
{
    /// <summary>
    /// The default maximum tokens used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumTokens = 4096;
    /// <summary>
    /// The default maximum token bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumTokenBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
    /// <summary>
    /// The default maximum comment depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumCommentDepth = EmailParsingLimitsConstants.DefaultMaxNestingDepth;
    EmailHeaderLexicalMode mode;
    int maximumTokens;
    int maximumTokenBytes;
    int maximumCommentDepth;
    bool allowInternationalized;
    /// <summary>
    /// Creates a new header lexical options object.
    /// </summary>
    public EmailHeaderLexicalOptions()
    {
        mode = EmailHeaderLexicalMode.Tolerant;
        maximumTokens = DefaultMaximumTokens;
        maximumTokenBytes = DefaultMaximumTokenBytes;
        maximumCommentDepth = DefaultMaximumCommentDepth;
        allowInternationalized = false;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailHeaderLexicalMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailHeaderLexicalMode value)
    {
        if (value < EmailHeaderLexicalMode.Strict || value > EmailHeaderLexicalMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the maximum tokens.
    /// </summary>
    /// <returns>The current maximum tokens.</returns>
    public int GetMaximumTokens()
    {
        return maximumTokens;
    }

    /// <summary>
    /// Sets the maximum tokens used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumTokens(int value)
    {
        maximumTokens = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum token bytes.
    /// </summary>
    /// <returns>The current maximum token bytes.</returns>
    public int GetMaximumTokenBytes()
    {
        return maximumTokenBytes;
    }

    /// <summary>
    /// Sets the maximum token bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumTokenBytes(int value)
    {
        maximumTokenBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum comment depth.
    /// </summary>
    /// <returns>The current maximum comment depth.</returns>
    public int GetMaximumCommentDepth()
    {
        return maximumCommentDepth;
    }

    /// <summary>
    /// Sets the maximum comment depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCommentDepth(int value)
    {
        maximumCommentDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailHeaderLexicalOptions Clone()
    {
        EmailHeaderLexicalOptions copy = new EmailHeaderLexicalOptions();
        copy.SetMode(mode);
        copy.SetMaximumTokens(maximumTokens);
        copy.SetMaximumTokenBytes(maximumTokenBytes);
        copy.SetMaximumCommentDepth(maximumCommentDepth);
        copy.SetAllowInternationalized(allowInternationalized);
        return copy;
    }
}
