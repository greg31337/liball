using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailCalendarParseOptionsConstants
{
    /// <summary>
    /// The default maximum calendar bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumCalendarBytes = 10485760;
    /// <summary>
    /// The default maximum physical line bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumPhysicalLineBytes = 65536;
    /// <summary>
    /// The default maximum unfolded line bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumUnfoldedLineBytes = 262144;
    /// <summary>
    /// The default maximum content lines used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumContentLines = 10000;
    /// <summary>
    /// The default maximum properties used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumProperties = 10000;
    /// <summary>
    /// The default maximum components used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumComponents = 1000;
    /// <summary>
    /// The default maximum parameters per property used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumParametersPerProperty = 128;
    /// <summary>
    /// The default maximum parameter values used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumParameterValues = 512;
}
