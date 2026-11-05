using System;

namespace email;
/// <summary>
/// Controls how message classification operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMessageClassificationOptions : IEmailMessageClassificationOptionsContract
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderValueBytes = 65536;
    /// <summary>
    /// The default maximum subject characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSubjectCharacters = 4096;
    int maximumHeaderCount;
    int maximumHeaderValueBytes;
    int maximumSubjectCharacters;
    int maximumDepth;
    int maximumEntityCount;
    EmailReportParseOptions reportParseOptions;
    /// <summary>
    /// Creates a new message classification options object.
    /// </summary>
    public EmailMessageClassificationOptions()
    {
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumHeaderValueBytes = DefaultMaximumHeaderValueBytes;
        maximumSubjectCharacters = DefaultMaximumSubjectCharacters;
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        reportParseOptions = new EmailReportParseOptions();
    }

    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    public int GetMaximumHeaderCount()
    {
        return maximumHeaderCount;
    }

    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderCount(int value)
    {
        maximumHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum header value bytes.
    /// </summary>
    /// <returns>The current maximum header value bytes.</returns>
    public int GetMaximumHeaderValueBytes()
    {
        return maximumHeaderValueBytes;
    }

    /// <summary>
    /// Sets the maximum header value bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderValueBytes(int value)
    {
        maximumHeaderValueBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum subject characters.
    /// </summary>
    /// <returns>The current maximum subject characters.</returns>
    public int GetMaximumSubjectCharacters()
    {
        return maximumSubjectCharacters;
    }

    /// <summary>
    /// Sets the maximum subject characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSubjectCharacters(int value)
    {
        maximumSubjectCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDepth(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value");
        maximumDepth = value;
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumEntityCount(int value)
    {
        maximumEntityCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the report parse options.
    /// </summary>
    /// <returns>The current report parse options.</returns>
    public EmailReportParseOptions GetReportParseOptions()
    {
        return reportParseOptions.Clone();
    }

    /// <summary>
    /// Sets the report parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetReportParseOptions(EmailReportParseOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        reportParseOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailMessageClassificationOptions Clone()
    {
        EmailMessageClassificationOptions copy = new EmailMessageClassificationOptions();
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumHeaderValueBytes(maximumHeaderValueBytes);
        copy.SetMaximumSubjectCharacters(maximumSubjectCharacters);
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetReportParseOptions(reportParseOptions);
        return copy;
    }
}
