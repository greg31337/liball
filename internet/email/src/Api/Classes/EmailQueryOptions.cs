using System;

namespace email;
/// <summary>
/// Controls how query operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailQueryOptions : IEmailQueryOptionsContract
{
    /// <summary>
    /// The default maximum query characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumQueryCharacters = 1024;
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum field characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumFieldCharacters = 1000000;
    /// <summary>
    /// The default maximum body characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumBodyCharacters = 10000000;
    /// <summary>
    /// The default maximum body bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumBodyBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
    /// <summary>
    /// The default maximum query depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumQueryDepth = 64;
    /// <summary>
    /// The default maximum query node count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumQueryNodeCount = 1000;
    /// <summary>
    /// The default maximum children per node used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumChildrenPerNode = 100;
    int maximumDepth;
    int maximumEntityCount;
    int maximumQueryCharacters;
    int maximumHeaderCount;
    int maximumFieldCharacters;
    int maximumBodyCharacters;
    long maximumBodyBytes;
    int maximumQueryDepth;
    int maximumQueryNodeCount;
    int maximumChildrenPerNode;
    bool caseSensitive;
    bool includeTextAttachments;
    bool includeHtml;
    bool requireKnownDateZone;
    /// <summary>
    /// Creates a new query options object.
    /// </summary>
    public EmailQueryOptions()
    {
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        maximumQueryCharacters = DefaultMaximumQueryCharacters;
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumFieldCharacters = DefaultMaximumFieldCharacters;
        maximumBodyCharacters = DefaultMaximumBodyCharacters;
        maximumBodyBytes = DefaultMaximumBodyBytes;
        maximumQueryDepth = DefaultMaximumQueryDepth;
        maximumQueryNodeCount = DefaultMaximumQueryNodeCount;
        maximumChildrenPerNode = DefaultMaximumChildrenPerNode;
        caseSensitive = false;
        includeTextAttachments = false;
        includeHtml = true;
        requireKnownDateZone = true;
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
    /// Gets the maximum query characters.
    /// </summary>
    /// <returns>The current maximum query characters.</returns>
    public int GetMaximumQueryCharacters()
    {
        return maximumQueryCharacters;
    }

    /// <summary>
    /// Sets the maximum query characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumQueryCharacters(int value)
    {
        maximumQueryCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Gets the maximum field characters.
    /// </summary>
    /// <returns>The current maximum field characters.</returns>
    public int GetMaximumFieldCharacters()
    {
        return maximumFieldCharacters;
    }

    /// <summary>
    /// Sets the maximum field characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFieldCharacters(int value)
    {
        maximumFieldCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum body characters.
    /// </summary>
    /// <returns>The current maximum body characters.</returns>
    public int GetMaximumBodyCharacters()
    {
        return maximumBodyCharacters;
    }

    /// <summary>
    /// Sets the maximum body characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumBodyCharacters(int value)
    {
        maximumBodyCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum body bytes.
    /// </summary>
    /// <returns>The current maximum body bytes.</returns>
    public long GetMaximumBodyBytes()
    {
        return maximumBodyBytes;
    }

    /// <summary>
    /// Sets the maximum body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumBodyBytes(long value)
    {
        if (value < 1 || value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumBodyBytes = value;
    }

    /// <summary>
    /// Gets the maximum query depth.
    /// </summary>
    /// <returns>The current maximum query depth.</returns>
    public int GetMaximumQueryDepth()
    {
        return maximumQueryDepth;
    }

    /// <summary>
    /// Sets the maximum query depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumQueryDepth(int value)
    {
        maximumQueryDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum query node count.
    /// </summary>
    /// <returns>The current maximum query node count.</returns>
    public int GetMaximumQueryNodeCount()
    {
        return maximumQueryNodeCount;
    }

    /// <summary>
    /// Sets the maximum query node count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumQueryNodeCount(int value)
    {
        maximumQueryNodeCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum children per node.
    /// </summary>
    /// <returns>The current maximum children per node.</returns>
    public int GetMaximumChildrenPerNode()
    {
        return maximumChildrenPerNode;
    }

    /// <summary>
    /// Sets the maximum children per node used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumChildrenPerNode(int value)
    {
        maximumChildrenPerNode = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the case sensitive.
    /// </summary>
    /// <returns><c>true</c> when the case sensitive setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetCaseSensitive()
    {
        return caseSensitive;
    }

    /// <summary>
    /// Sets the case sensitive used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case sensitive behavior.</param>
    public void SetCaseSensitive(bool value)
    {
        caseSensitive = value;
    }

    /// <summary>
    /// Gets the include text attachments.
    /// </summary>
    /// <returns><c>true</c> when the include text attachments setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeTextAttachments()
    {
        return includeTextAttachments;
    }

    /// <summary>
    /// Sets the include text attachments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include text attachments behavior.</param>
    public void SetIncludeTextAttachments(bool value)
    {
        includeTextAttachments = value;
    }

    /// <summary>
    /// Gets the include HTML.
    /// </summary>
    /// <returns><c>true</c> when the include HTML setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeHtml()
    {
        return includeHtml;
    }

    /// <summary>
    /// Sets the include HTML used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include HTML behavior.</param>
    public void SetIncludeHtml(bool value)
    {
        includeHtml = value;
    }

    /// <summary>
    /// Gets the require known date zone.
    /// </summary>
    /// <returns><c>true</c> when the require known date zone setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireKnownDateZone()
    {
        return requireKnownDateZone;
    }

    /// <summary>
    /// Sets the require known date zone used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require known date zone behavior.</param>
    public void SetRequireKnownDateZone(bool value)
    {
        requireKnownDateZone = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailQueryOptions Clone()
    {
        EmailQueryOptions copy = new EmailQueryOptions();
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetMaximumQueryCharacters(maximumQueryCharacters);
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumFieldCharacters(maximumFieldCharacters);
        copy.SetMaximumBodyCharacters(maximumBodyCharacters);
        copy.SetMaximumBodyBytes(maximumBodyBytes);
        copy.SetMaximumQueryDepth(maximumQueryDepth);
        copy.SetMaximumQueryNodeCount(maximumQueryNodeCount);
        copy.SetMaximumChildrenPerNode(maximumChildrenPerNode);
        copy.SetCaseSensitive(caseSensitive);
        copy.SetIncludeTextAttachments(includeTextAttachments);
        copy.SetIncludeHtml(includeHtml);
        copy.SetRequireKnownDateZone(requireKnownDateZone);
        return copy;
    }
}
