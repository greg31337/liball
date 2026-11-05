using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailRfc2231Constants
{
    /// <summary>
    /// The default maximum segments used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSegments = 128;
    /// <summary>
    /// The default maximum decoded bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumDecodedBytes = 1048576;
}
