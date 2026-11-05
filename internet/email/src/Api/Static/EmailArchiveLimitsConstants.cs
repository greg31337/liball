namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailArchiveLimitsConstants
{
    /// <summary>
    /// The default max archive bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxArchiveBytes = 1073741824;
    /// <summary>
    /// The default max entry bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxEntryBytes = 104857600;
    /// <summary>
    /// The default max entry count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxEntryCount = 100000;
    /// <summary>
    /// The default max partial fragment count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxPartialFragmentCount = 1000;
    /// <summary>
    /// The default max reassembled bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxReassembledBytes = 104857600;
    /// <summary>
    /// The default max TNEF attribute count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxTnefAttributeCount = 10000;
    /// <summary>
    /// The default max path characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxPathCharacters = 4096;
}
