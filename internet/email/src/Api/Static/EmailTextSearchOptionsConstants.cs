using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTextSearchOptionsConstants
{
    /// <summary>
    /// The default maximum query characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumQueryCharacters = 1024;
    /// <summary>
    /// The default maximum total characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumTotalCharacters = 10000000;
    /// <summary>
    /// The default maximum matches used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumMatches = 1000;
    /// <summary>
    /// The default maximum context characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumContextCharacters = 120;
}
