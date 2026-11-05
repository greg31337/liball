namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDecodingLimitsConstants
{
    /// <summary>
    /// The default max decoded bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxDecodedBytes = 104857600;
    /// <summary>
    /// The default max decompressed bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxDecompressedBytes = 104857600;
    /// <summary>
    /// The default max expansion ratio used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxExpansionRatio = 100;
    /// <summary>
    /// The default max text characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxTextCharacters = 10000000;
    /// <summary>
    /// The default max charset name length used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxCharsetNameLength = 128;
}
