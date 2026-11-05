using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailArchiveIndexOptionsConstants
{
    /// <summary>
    /// The default maximum selected header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSelectedHeaderCount = 100;
    /// <summary>
    /// The default maximum selected header characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSelectedHeaderCharacters = 65536;
}
