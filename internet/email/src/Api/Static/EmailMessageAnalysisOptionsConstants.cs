using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailMessageAnalysisOptionsConstants
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAddressCount = 10000;
    /// <summary>
    /// The default maximum structured header bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumStructuredHeaderBytes = 1048576;
    /// <summary>
    /// The default maximum decoded content bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumDecodedContentBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
}
