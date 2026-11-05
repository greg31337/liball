using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how ARC set collection operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailArcSetCollectionOptions : IEmailArcSetCollectionOptionsContract
{
    /// <summary>
    /// The default maximum set count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSetCount = 50;
    /// <summary>
    /// The default maximum ARC header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumArcHeaderCount = 150;
    /// <summary>
    /// The default maximum header bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderBytes = 65536;
    int maximumSetCount;
    int maximumArcHeaderCount;
    int maximumHeaderBytes;
    int maximumTags;
    /// <summary>
    /// Creates a new ARC set collection options object.
    /// </summary>
    public EmailArcSetCollectionOptions()
    {
        maximumSetCount = DefaultMaximumSetCount;
        maximumArcHeaderCount = DefaultMaximumArcHeaderCount;
        maximumHeaderBytes = DefaultMaximumHeaderBytes;
        maximumTags = EmailSignatureParserOptionsConstants.DefaultMaximumTags;
    }

    /// <summary>
    /// Gets the maximum set count.
    /// </summary>
    /// <returns>The current maximum set count.</returns>
    public int GetMaximumSetCount()
    {
        return maximumSetCount;
    }

    /// <summary>
    /// Sets the maximum set count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumSetCount(int value)
    {
        if (value < 1 || value > 50)
            throw new ArgumentOutOfRangeException("value");
        maximumSetCount = value;
    }

    /// <summary>
    /// Gets the maximum ARC header count.
    /// </summary>
    /// <returns>The current maximum ARC header count.</returns>
    public int GetMaximumArcHeaderCount()
    {
        return maximumArcHeaderCount;
    }

    /// <summary>
    /// Sets the maximum ARC header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumArcHeaderCount(int value)
    {
        maximumArcHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum header bytes.
    /// </summary>
    /// <returns>The current maximum header bytes.</returns>
    public int GetMaximumHeaderBytes()
    {
        return maximumHeaderBytes;
    }

    /// <summary>
    /// Sets the maximum header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderBytes(int value)
    {
        maximumHeaderBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum tags.
    /// </summary>
    /// <returns>The current maximum tags.</returns>
    public int GetMaximumTags()
    {
        return maximumTags;
    }

    /// <summary>
    /// Sets the maximum tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumTags(int value)
    {
        maximumTags = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailArcSetCollectionOptions Clone()
    {
        EmailArcSetCollectionOptions copy = new EmailArcSetCollectionOptions();
        copy.SetMaximumSetCount(maximumSetCount);
        copy.SetMaximumArcHeaderCount(maximumArcHeaderCount);
        copy.SetMaximumHeaderBytes(maximumHeaderBytes);
        copy.SetMaximumTags(maximumTags);
        return copy;
    }
}
