namespace email;
/// <summary>
/// Represents an archive limits.
/// </summary>
public class EmailArchiveLimits : IEmailArchiveLimitsContract
{
    /// <summary>
    /// The default max archive bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxArchiveBytes = 1073741824;
    /// <summary>
    /// The default max entry bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxEntryBytes = 104857600;
    /// <summary>
    /// The default max entry count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxEntryCount = 100000;
    /// <summary>
    /// The default max partial fragment count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxPartialFragmentCount = 1000;
    /// <summary>
    /// The default max reassembled bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxReassembledBytes = 104857600;
    /// <summary>
    /// The default max TNEF attribute count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxTnefAttributeCount = 10000;
    /// <summary>
    /// The default max path characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxPathCharacters = 4096;
    long maxArchiveBytes;
    long maxEntryBytes;
    int maxEntryCount;
    int maxPartialFragmentCount;
    long maxReassembledBytes;
    int maxTnefAttributeCount;
    int maxPathCharacters;
    /// <summary>
    /// Creates a new archive limits.
    /// </summary>
    public EmailArchiveLimits()
    {
        maxArchiveBytes = DefaultMaxArchiveBytes;
        maxEntryBytes = DefaultMaxEntryBytes;
        maxEntryCount = DefaultMaxEntryCount;
        maxPartialFragmentCount = DefaultMaxPartialFragmentCount;
        maxReassembledBytes = DefaultMaxReassembledBytes;
        maxTnefAttributeCount = DefaultMaxTnefAttributeCount;
        maxPathCharacters = DefaultMaxPathCharacters;
    }

    /// <summary>
    /// Gets the max archive bytes.
    /// </summary>
    /// <returns>The current max archive bytes.</returns>
    public long GetMaxArchiveBytes()
    {
        return maxArchiveBytes;
    }

    /// <summary>
    /// Sets the max archive bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxArchiveBytes(long value)
    {
        maxArchiveBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max entry bytes.
    /// </summary>
    /// <returns>The current max entry bytes.</returns>
    public long GetMaxEntryBytes()
    {
        return maxEntryBytes;
    }

    /// <summary>
    /// Sets the max entry bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxEntryBytes(long value)
    {
        maxEntryBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max entry count.
    /// </summary>
    /// <returns>The current max entry count.</returns>
    public int GetMaxEntryCount()
    {
        return maxEntryCount;
    }

    /// <summary>
    /// Sets the max entry count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxEntryCount(int value)
    {
        maxEntryCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max partial fragment count.
    /// </summary>
    /// <returns>The current max partial fragment count.</returns>
    public int GetMaxPartialFragmentCount()
    {
        return maxPartialFragmentCount;
    }

    /// <summary>
    /// Sets the max partial fragment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxPartialFragmentCount(int value)
    {
        maxPartialFragmentCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max reassembled bytes.
    /// </summary>
    /// <returns>The current max reassembled bytes.</returns>
    public long GetMaxReassembledBytes()
    {
        return maxReassembledBytes;
    }

    /// <summary>
    /// Sets the max reassembled bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxReassembledBytes(long value)
    {
        maxReassembledBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max TNEF attribute count.
    /// </summary>
    /// <returns>The current max TNEF attribute count.</returns>
    public int GetMaxTnefAttributeCount()
    {
        return maxTnefAttributeCount;
    }

    /// <summary>
    /// Sets the max TNEF attribute count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxTnefAttributeCount(int value)
    {
        maxTnefAttributeCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max path characters.
    /// </summary>
    /// <returns>The current max path characters.</returns>
    public int GetMaxPathCharacters()
    {
        return maxPathCharacters;
    }

    /// <summary>
    /// Sets the max path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxPathCharacters(int value)
    {
        maxPathCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailArchiveLimits Clone()
    {
        EmailArchiveLimits copy = new EmailArchiveLimits();
        copy.SetMaxArchiveBytes(maxArchiveBytes);
        copy.SetMaxEntryBytes(maxEntryBytes);
        copy.SetMaxEntryCount(maxEntryCount);
        copy.SetMaxPartialFragmentCount(maxPartialFragmentCount);
        copy.SetMaxReassembledBytes(maxReassembledBytes);
        copy.SetMaxTnefAttributeCount(maxTnefAttributeCount);
        copy.SetMaxPathCharacters(maxPathCharacters);
        return copy;
    }
}
