using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailReportParseOptionsConstants
{
    /// <summary>
    /// The default maximum report bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumReportBytes = 10485760;
    /// <summary>
    /// The default maximum part count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumPartCount = 100;
    /// <summary>
    /// The default maximum field block count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumFieldBlockCount = 1000;
    /// <summary>
    /// The default maximum field count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumFieldCount = 10000;
    /// <summary>
    /// The default maximum fields per block used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumFieldsPerBlock = 1000;
    /// <summary>
    /// The default maximum field block bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumFieldBlockBytes = 262144;
    /// <summary>
    /// The default maximum line bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumLineBytes = 65536;
}
