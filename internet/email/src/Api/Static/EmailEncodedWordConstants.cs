using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailEncodedWordConstants
{
    /// <summary>
    /// The maximum word bytes value exposed by this API.
    /// </summary>
    public const int MaximumWordBytes = 75;
    /// <summary>
    /// The default maximum encoded words used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumEncodedWords = 128;
    /// <summary>
    /// The default maximum decoded bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumDecodedBytes = 1048576;
}
