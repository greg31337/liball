namespace email;
/// <summary>
/// Represents a HTML limits.
/// </summary>
public class EmailHtmlLimits : IEmailHtmlLimitsContract
{
    /// <summary>
    /// The default max input characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxInputCharacters = 5000000;
    /// <summary>
    /// The default max output characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxOutputCharacters = 5000000;
    /// <summary>
    /// The default max token count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxTokenCount = 1000000;
    /// <summary>
    /// The default max nesting depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxNestingDepth = 128;
    /// <summary>
    /// The default max attributes per tag used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxAttributesPerTag = 128;
    /// <summary>
    /// The default max url characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxUrlCharacters = 8192;
    /// <summary>
    /// The default max data URI count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxDataUriCount = 100;
    /// <summary>
    /// The default max data URI bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxDataUriBytes = 10485760;
    int maxInputCharacters;
    int maxOutputCharacters;
    int maxTokenCount;
    int maxNestingDepth;
    int maxAttributesPerTag;
    int maxUrlCharacters;
    int maxDataUriCount;
    long maxDataUriBytes;
    /// <summary>
    /// Creates a new HTML limits.
    /// </summary>
    public EmailHtmlLimits()
    {
        maxInputCharacters = DefaultMaxInputCharacters;
        maxOutputCharacters = DefaultMaxOutputCharacters;
        maxTokenCount = DefaultMaxTokenCount;
        maxNestingDepth = DefaultMaxNestingDepth;
        maxAttributesPerTag = DefaultMaxAttributesPerTag;
        maxUrlCharacters = DefaultMaxUrlCharacters;
        maxDataUriCount = DefaultMaxDataUriCount;
        maxDataUriBytes = DefaultMaxDataUriBytes;
    }

    /// <summary>
    /// Gets the max input characters.
    /// </summary>
    /// <returns>The current max input characters.</returns>
    public int GetMaxInputCharacters()
    {
        return maxInputCharacters;
    }

    /// <summary>
    /// Sets the max input characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxInputCharacters(int value)
    {
        maxInputCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max output characters.
    /// </summary>
    /// <returns>The current max output characters.</returns>
    public int GetMaxOutputCharacters()
    {
        return maxOutputCharacters;
    }

    /// <summary>
    /// Sets the max output characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxOutputCharacters(int value)
    {
        maxOutputCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max token count.
    /// </summary>
    /// <returns>The current max token count.</returns>
    public int GetMaxTokenCount()
    {
        return maxTokenCount;
    }

    /// <summary>
    /// Sets the max token count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxTokenCount(int value)
    {
        maxTokenCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max nesting depth.
    /// </summary>
    /// <returns>The current max nesting depth.</returns>
    public int GetMaxNestingDepth()
    {
        return maxNestingDepth;
    }

    /// <summary>
    /// Sets the max nesting depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxNestingDepth(int value)
    {
        maxNestingDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max attributes per tag.
    /// </summary>
    /// <returns>The current max attributes per tag.</returns>
    public int GetMaxAttributesPerTag()
    {
        return maxAttributesPerTag;
    }

    /// <summary>
    /// Sets the max attributes per tag used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxAttributesPerTag(int value)
    {
        maxAttributesPerTag = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max url characters.
    /// </summary>
    /// <returns>The current max url characters.</returns>
    public int GetMaxUrlCharacters()
    {
        return maxUrlCharacters;
    }

    /// <summary>
    /// Sets the max url characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxUrlCharacters(int value)
    {
        maxUrlCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max data URI count.
    /// </summary>
    /// <returns>The current max data URI count.</returns>
    public int GetMaxDataUriCount()
    {
        return maxDataUriCount;
    }

    /// <summary>
    /// Sets the max data URI count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxDataUriCount(int value)
    {
        maxDataUriCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max data URI bytes.
    /// </summary>
    /// <returns>The current max data URI bytes.</returns>
    public long GetMaxDataUriBytes()
    {
        return maxDataUriBytes;
    }

    /// <summary>
    /// Sets the max data URI bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxDataUriBytes(long value)
    {
        maxDataUriBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHtmlLimits Clone()
    {
        EmailHtmlLimits copy = new EmailHtmlLimits();
        copy.SetMaxInputCharacters(maxInputCharacters);
        copy.SetMaxOutputCharacters(maxOutputCharacters);
        copy.SetMaxTokenCount(maxTokenCount);
        copy.SetMaxNestingDepth(maxNestingDepth);
        copy.SetMaxAttributesPerTag(maxAttributesPerTag);
        copy.SetMaxUrlCharacters(maxUrlCharacters);
        copy.SetMaxDataUriCount(maxDataUriCount);
        copy.SetMaxDataUriBytes(maxDataUriBytes);
        return copy;
    }
}
