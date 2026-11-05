using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailComposerOptionsConstants
{
    /// <summary>
    /// The default maximum address count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAddressCount = 1000;
    /// <summary>
    /// The default maximum custom field count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumCustomFieldCount = 100;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderValueBytes = 65536;
    /// <summary>
    /// The default maximum alternative count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAlternativeCount = 32;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum body bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumBodyBytes = 104857600;
}
