using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailArcSetCollectionOptionsConstants
{
    /// <summary>
    /// The default maximum set count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSetCount = 50;
    /// <summary>
    /// The default maximum ARC header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumArcHeaderCount = 150;
    /// <summary>
    /// The default maximum header bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderBytes = 65536;
}
