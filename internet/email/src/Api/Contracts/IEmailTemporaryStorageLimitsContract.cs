namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTemporaryStorageLimits"/>.
/// </summary>
public interface IEmailTemporaryStorageLimitsContract
{
    /// <summary>
    /// Gets the memory threshold bytes.
    /// </summary>
    /// <returns>The current memory threshold bytes.</returns>
    long GetMemoryThresholdBytes();
    /// <summary>
    /// Sets the memory threshold bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMemoryThresholdBytes(long value);
    /// <summary>
    /// Gets the max temporary bytes.
    /// </summary>
    /// <returns>The current max temporary bytes.</returns>
    long GetMaxTemporaryBytes();
    /// <summary>
    /// Sets the max temporary bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxTemporaryBytes(long value);
    /// <summary>
    /// Gets the max temporary file count.
    /// </summary>
    /// <returns>The current max temporary file count.</returns>
    int GetMaxTemporaryFileCount();
    /// <summary>
    /// Sets the max temporary file count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxTemporaryFileCount(int value);
    /// <summary>
    /// Gets the max open stream count.
    /// </summary>
    /// <returns>The current max open stream count.</returns>
    int GetMaxOpenStreamCount();
    /// <summary>
    /// Sets the max open stream count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxOpenStreamCount(int value);
    /// <summary>
    /// Gets the max materialized bytes.
    /// </summary>
    /// <returns>The current max materialized bytes.</returns>
    long GetMaxMaterializedBytes();
    /// <summary>
    /// Sets the max materialized bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxMaterializedBytes(long value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTemporaryStorageLimits Clone();
}
