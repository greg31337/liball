namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailParsingLimitsConstants
{
    /// <summary>
    /// The default max message bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxMessageBytes = 104857600;
    /// <summary>
    /// The default max header bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxHeaderBytes = 262144;
    /// <summary>
    /// The default max header field count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxHeaderFieldCount = 1000;
    /// <summary>
    /// The default max line bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxLineBytes = 65536;
    /// <summary>
    /// The default max nesting depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxNestingDepth = 30;
    /// <summary>
    /// The default max part count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxPartCount = 1000;
    /// <summary>
    /// The default max parameters per header used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxParametersPerHeader = 100;
    /// <summary>
    /// The default max addresses per header used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxAddressesPerHeader = 1000;
    /// <summary>
    /// The default max encoded words per header used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxEncodedWordsPerHeader = 256;
}
