using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailResentOptionsConstants
{
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAddressCount = 1000;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderValueBytes = 65536;
}
