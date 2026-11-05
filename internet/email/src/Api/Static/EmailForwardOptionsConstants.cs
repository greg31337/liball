using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailForwardOptionsConstants
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 100;
    /// <summary>
    /// The default maximum header characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCharacters = 100000;
    /// <summary>
    /// The default maximum body characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumBodyCharacters = 1000000;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum attachment bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumAttachmentBytes = 104857600;
}
