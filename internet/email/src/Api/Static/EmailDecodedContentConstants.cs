using System;
using System.IO;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDecodedContentConstants
{
    /// <summary>
    /// The default maximum bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultBufferBytes = 81920;
}
