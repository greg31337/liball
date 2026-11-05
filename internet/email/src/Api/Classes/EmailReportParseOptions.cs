using System;

namespace email;
/// <summary>
/// Controls how report parse operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailReportParseOptions : IEmailReportParseOptionsContract
{
    /// <summary>
    /// The default maximum report bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumReportBytes = 10485760;
    /// <summary>
    /// The default maximum part count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumPartCount = 100;
    /// <summary>
    /// The default maximum field block count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumFieldBlockCount = 1000;
    /// <summary>
    /// The default maximum field count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumFieldCount = 10000;
    /// <summary>
    /// The default maximum fields per block used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumFieldsPerBlock = 1000;
    /// <summary>
    /// The default maximum field block bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumFieldBlockBytes = 262144;
    /// <summary>
    /// The default maximum line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineBytes = 65536;
    int maximumReportBytes;
    int maximumPartCount;
    int maximumFieldBlockCount;
    int maximumFieldCount;
    int maximumFieldsPerBlock;
    int maximumFieldBlockBytes;
    int maximumLineBytes;
    EmailStructuredValueParseMode mode;
    /// <summary>
    /// Creates a new report parse options object.
    /// </summary>
    public EmailReportParseOptions()
    {
        maximumReportBytes = DefaultMaximumReportBytes;
        maximumPartCount = DefaultMaximumPartCount;
        maximumFieldBlockCount = DefaultMaximumFieldBlockCount;
        maximumFieldCount = DefaultMaximumFieldCount;
        maximumFieldsPerBlock = DefaultMaximumFieldsPerBlock;
        maximumFieldBlockBytes = DefaultMaximumFieldBlockBytes;
        maximumLineBytes = DefaultMaximumLineBytes;
        mode = EmailStructuredValueParseMode.Tolerant;
    }

    /// <summary>
    /// Gets the maximum report bytes.
    /// </summary>
    /// <returns>The current maximum report bytes.</returns>
    public int GetMaximumReportBytes()
    {
        return maximumReportBytes;
    }

    /// <summary>
    /// Sets the maximum report bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumReportBytes(int value)
    {
        maximumReportBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum part count.
    /// </summary>
    /// <returns>The current maximum part count.</returns>
    public int GetMaximumPartCount()
    {
        return maximumPartCount;
    }

    /// <summary>
    /// Sets the maximum part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPartCount(int value)
    {
        maximumPartCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum field block count.
    /// </summary>
    /// <returns>The current maximum field block count.</returns>
    public int GetMaximumFieldBlockCount()
    {
        return maximumFieldBlockCount;
    }

    /// <summary>
    /// Sets the maximum field block count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFieldBlockCount(int value)
    {
        maximumFieldBlockCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum field count.
    /// </summary>
    /// <returns>The current maximum field count.</returns>
    public int GetMaximumFieldCount()
    {
        return maximumFieldCount;
    }

    /// <summary>
    /// Sets the maximum field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFieldCount(int value)
    {
        maximumFieldCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum fields per block.
    /// </summary>
    /// <returns>The current maximum fields per block.</returns>
    public int GetMaximumFieldsPerBlock()
    {
        return maximumFieldsPerBlock;
    }

    /// <summary>
    /// Sets the maximum fields per block used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFieldsPerBlock(int value)
    {
        maximumFieldsPerBlock = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum field block bytes.
    /// </summary>
    /// <returns>The current maximum field block bytes.</returns>
    public int GetMaximumFieldBlockBytes()
    {
        return maximumFieldBlockBytes;
    }

    /// <summary>
    /// Sets the maximum field block bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFieldBlockBytes(int value)
    {
        maximumFieldBlockBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum line bytes.
    /// </summary>
    /// <returns>The current maximum line bytes.</returns>
    public int GetMaximumLineBytes()
    {
        return maximumLineBytes;
    }

    /// <summary>
    /// Sets the maximum line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumLineBytes(int value)
    {
        maximumLineBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailStructuredValueParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailStructuredValueParseMode value)
    {
        if (value < EmailStructuredValueParseMode.Strict || value > EmailStructuredValueParseMode.Tolerant)
            throw new ArgumentOutOfRangeException("value");
        mode = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailReportParseOptions Clone()
    {
        EmailReportParseOptions copy = new EmailReportParseOptions();
        copy.SetMaximumReportBytes(maximumReportBytes);
        copy.SetMaximumPartCount(maximumPartCount);
        copy.SetMaximumFieldBlockCount(maximumFieldBlockCount);
        copy.SetMaximumFieldCount(maximumFieldCount);
        copy.SetMaximumFieldsPerBlock(maximumFieldsPerBlock);
        copy.SetMaximumFieldBlockBytes(maximumFieldBlockBytes);
        copy.SetMaximumLineBytes(maximumLineBytes);
        copy.SetMode(mode);
        return copy;
    }
}
