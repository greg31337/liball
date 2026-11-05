namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTemporaryStorageLimitsConstants
{
    /// <summary>
    /// The default memory threshold bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMemoryThresholdBytes = 1048576;
    /// <summary>
    /// The default max temporary bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxTemporaryBytes = 1073741824;
    /// <summary>
    /// The default max temporary file count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxTemporaryFileCount = 64;
    /// <summary>
    /// The default max open stream count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxOpenStreamCount = 64;
    /// <summary>
    /// The default max materialized bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxMaterializedBytes = 104857600;
}
