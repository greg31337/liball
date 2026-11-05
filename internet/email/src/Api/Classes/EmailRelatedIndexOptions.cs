using System;

namespace email;
/// <summary>
/// Controls how related index operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailRelatedIndexOptions : IEmailRelatedIndexOptionsContract
{
    /// <summary>
    /// The default maximum references used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumReferences = 10000;
    int maximumDepth;
    int maximumEntityCount;
    int maximumHtmlBytes;
    int maximumReferences;
    int maximumUrlCharacters;
    /// <summary>
    /// Creates a new related index options object.
    /// </summary>
    public EmailRelatedIndexOptions()
    {
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        maximumHtmlBytes = EmailHtmlLimitsConstants.DefaultMaxInputCharacters;
        maximumReferences = DefaultMaximumReferences;
        maximumUrlCharacters = EmailHtmlLimitsConstants.DefaultMaxUrlCharacters;
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
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEntityCount(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumEntityCount = value;
    }

    /// <summary>
    /// Gets the maximum HTML bytes.
    /// </summary>
    /// <returns>The current maximum HTML bytes.</returns>
    public int GetMaximumHtmlBytes()
    {
        return maximumHtmlBytes;
    }

    /// <summary>
    /// Sets the maximum HTML bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumHtmlBytes(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumHtmlBytes = value;
    }

    /// <summary>
    /// Gets the maximum references.
    /// </summary>
    /// <returns>The current maximum references.</returns>
    public int GetMaximumReferences()
    {
        return maximumReferences;
    }

    /// <summary>
    /// Sets the maximum references used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumReferences(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumReferences = value;
    }

    /// <summary>
    /// Gets the maximum url characters.
    /// </summary>
    /// <returns>The current maximum url characters.</returns>
    public int GetMaximumUrlCharacters()
    {
        return maximumUrlCharacters;
    }

    /// <summary>
    /// Sets the maximum url characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumUrlCharacters(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumUrlCharacters = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailRelatedIndexOptions Clone()
    {
        EmailRelatedIndexOptions copy = new EmailRelatedIndexOptions();
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetMaximumHtmlBytes(maximumHtmlBytes);
        copy.SetMaximumReferences(maximumReferences);
        copy.SetMaximumUrlCharacters(maximumUrlCharacters);
        return copy;
    }
}
