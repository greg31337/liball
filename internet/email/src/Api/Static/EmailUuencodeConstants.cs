using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailUuencodeConstants
{
    /// <summary>
    /// The default maximum line bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumLineBytes = 1024;
    /// <summary>
    /// The maximum file name bytes value exposed by this API.
    /// </summary>
    public const int MaximumFileNameBytes = 255;
}
