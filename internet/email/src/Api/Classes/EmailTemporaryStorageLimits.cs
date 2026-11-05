namespace email;
/// <summary>
/// Represents a temporary storage limits.
/// </summary>
public class EmailTemporaryStorageLimits : IEmailTemporaryStorageLimitsContract
{
    /// <summary>
    /// The default memory threshold bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMemoryThresholdBytes = 1048576;
    /// <summary>
    /// The default max temporary bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxTemporaryBytes = 1073741824;
    /// <summary>
    /// The default max temporary file count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxTemporaryFileCount = 64;
    /// <summary>
    /// The default max open stream count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxOpenStreamCount = 64;
    /// <summary>
    /// The default max materialized bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxMaterializedBytes = 104857600;
    long memoryThresholdBytes;
    long maxTemporaryBytes;
    int maxTemporaryFileCount;
    int maxOpenStreamCount;
    long maxMaterializedBytes;
    /// <summary>
    /// Creates a new temporary storage limits.
    /// </summary>
    public EmailTemporaryStorageLimits()
    {
        memoryThresholdBytes = DefaultMemoryThresholdBytes;
        maxTemporaryBytes = DefaultMaxTemporaryBytes;
        maxTemporaryFileCount = DefaultMaxTemporaryFileCount;
        maxOpenStreamCount = DefaultMaxOpenStreamCount;
        maxMaterializedBytes = DefaultMaxMaterializedBytes;
    }

    /// <summary>
    /// Gets the memory threshold bytes.
    /// </summary>
    /// <returns>The current memory threshold bytes.</returns>
    public long GetMemoryThresholdBytes()
    {
        return memoryThresholdBytes;
    }

    /// <summary>
    /// Sets the memory threshold bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMemoryThresholdBytes(long value)
    {
        memoryThresholdBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the max temporary bytes.
    /// </summary>
    /// <returns>The current max temporary bytes.</returns>
    public long GetMaxTemporaryBytes()
    {
        return maxTemporaryBytes;
    }

    /// <summary>
    /// Sets the max temporary bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxTemporaryBytes(long value)
    {
        maxTemporaryBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max temporary file count.
    /// </summary>
    /// <returns>The current max temporary file count.</returns>
    public int GetMaxTemporaryFileCount()
    {
        return maxTemporaryFileCount;
    }

    /// <summary>
    /// Sets the max temporary file count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxTemporaryFileCount(int value)
    {
        maxTemporaryFileCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max open stream count.
    /// </summary>
    /// <returns>The current max open stream count.</returns>
    public int GetMaxOpenStreamCount()
    {
        return maxOpenStreamCount;
    }

    /// <summary>
    /// Sets the max open stream count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxOpenStreamCount(int value)
    {
        maxOpenStreamCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max materialized bytes.
    /// </summary>
    /// <returns>The current max materialized bytes.</returns>
    public long GetMaxMaterializedBytes()
    {
        return maxMaterializedBytes;
    }

    /// <summary>
    /// Sets the max materialized bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxMaterializedBytes(long value)
    {
        maxMaterializedBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTemporaryStorageLimits Clone()
    {
        EmailTemporaryStorageLimits copy = new EmailTemporaryStorageLimits();
        copy.SetMemoryThresholdBytes(memoryThresholdBytes);
        copy.SetMaxTemporaryBytes(maxTemporaryBytes);
        copy.SetMaxTemporaryFileCount(maxTemporaryFileCount);
        copy.SetMaxOpenStreamCount(maxOpenStreamCount);
        copy.SetMaxMaterializedBytes(maxMaterializedBytes);
        return copy;
    }
}
