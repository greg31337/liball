using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailQueryOptionsConstants
{
    /// <summary>
    /// The default maximum query characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumQueryCharacters = 1024;
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum field characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumFieldCharacters = 1000000;
    /// <summary>
    /// The default maximum body characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumBodyCharacters = 10000000;
    /// <summary>
    /// The default maximum body bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumBodyBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
    /// <summary>
    /// The default maximum query depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumQueryDepth = 64;
    /// <summary>
    /// The default maximum query node count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumQueryNodeCount = 1000;
    /// <summary>
    /// The default maximum children per node used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumChildrenPerNode = 100;
}
