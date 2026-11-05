namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailHtmlLimitsConstants
{
    /// <summary>
    /// The default max input characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxInputCharacters = 5000000;
    /// <summary>
    /// The default max output characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxOutputCharacters = 5000000;
    /// <summary>
    /// The default max token count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxTokenCount = 1000000;
    /// <summary>
    /// The default max nesting depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxNestingDepth = 128;
    /// <summary>
    /// The default max attributes per tag used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxAttributesPerTag = 128;
    /// <summary>
    /// The default max url characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxUrlCharacters = 8192;
    /// <summary>
    /// The default max data URI count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxDataUriCount = 100;
    /// <summary>
    /// The default max data URI bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxDataUriBytes = 10485760;
}
