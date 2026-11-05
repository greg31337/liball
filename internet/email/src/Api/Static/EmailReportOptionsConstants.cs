using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailReportOptionsConstants
{
    /// <summary>
    /// The default maximum human readable characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHumanReadableCharacters = 1000000;
    /// <summary>
    /// The default maximum report bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumReportBytes = 10485760;
    /// <summary>
    /// The default maximum original header bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumOriginalHeaderBytes = 1048576;
}
