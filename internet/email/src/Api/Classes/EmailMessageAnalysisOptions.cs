using System;

namespace email;
/// <summary>
/// Controls how message analysis operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMessageAnalysisOptions : IEmailMessageAnalysisOptionsContract
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAddressCount = 10000;
    /// <summary>
    /// The default maximum structured header bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumStructuredHeaderBytes = 1048576;
    /// <summary>
    /// The default maximum decoded content bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumDecodedContentBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
    int maximumDepth;
    int maximumEntityCount;
    int maximumHeaderCount;
    int maximumAddressCount;
    int maximumStructuredHeaderBytes;
    long maximumDecodedContentBytes;
    bool measureRepeatableDecodedContent;
    /// <summary>
    /// Creates a new message analysis options object.
    /// </summary>
    public EmailMessageAnalysisOptions()
    {
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumAddressCount = DefaultMaximumAddressCount;
        maximumStructuredHeaderBytes = DefaultMaximumStructuredHeaderBytes;
        maximumDecodedContentBytes = DefaultMaximumDecodedContentBytes;
        measureRepeatableDecodedContent = true;
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
        {
            throw new ArgumentOutOfRangeException("value");
        }

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
    /// Gets the maximum address count.
    /// </summary>
    /// <returns>The current maximum address count.</returns>
    public int GetMaximumAddressCount()
    {
        return maximumAddressCount;
    }

    /// <summary>
    /// Sets the maximum address count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAddressCount(int value)
    {
        maximumAddressCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum structured header bytes.
    /// </summary>
    /// <returns>The current maximum structured header bytes.</returns>
    public int GetMaximumStructuredHeaderBytes()
    {
        return maximumStructuredHeaderBytes;
    }

    /// <summary>
    /// Sets the maximum structured header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumStructuredHeaderBytes(int value)
    {
        maximumStructuredHeaderBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum decoded content bytes.
    /// </summary>
    /// <returns>The current maximum decoded content bytes.</returns>
    public long GetMaximumDecodedContentBytes()
    {
        return maximumDecodedContentBytes;
    }

    /// <summary>
    /// Sets the maximum decoded content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDecodedContentBytes(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumDecodedContentBytes = value;
    }

    /// <summary>
    /// Gets the measure repeatable decoded content.
    /// </summary>
    /// <returns><c>true</c> when the measure repeatable decoded content setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetMeasureRepeatableDecodedContent()
    {
        return measureRepeatableDecodedContent;
    }

    /// <summary>
    /// Sets the measure repeatable decoded content used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the measure repeatable decoded content behavior.</param>
    public void SetMeasureRepeatableDecodedContent(bool value)
    {
        measureRepeatableDecodedContent = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailMessageAnalysisOptions Clone()
    {
        EmailMessageAnalysisOptions copy = new EmailMessageAnalysisOptions();
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumAddressCount(maximumAddressCount);
        copy.SetMaximumStructuredHeaderBytes(maximumStructuredHeaderBytes);
        copy.SetMaximumDecodedContentBytes(maximumDecodedContentBytes);
        copy.SetMeasureRepeatableDecodedContent(measureRepeatableDecodedContent);
        return copy;
    }
}
