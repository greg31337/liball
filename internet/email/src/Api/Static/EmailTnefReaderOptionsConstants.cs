using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTnefReaderOptionsConstants
{
    /// <summary>
    /// The default read buffer bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultReadBufferBytes = 8192;
    /// <summary>
    /// The default maximum property count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumPropertyCount = 10000;
    /// <summary>
    /// The default maximum property value count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumPropertyValueCount = 10000;
    /// <summary>
    /// The default maximum recipient count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumRecipientCount = 10000;
    /// <summary>
    /// The default maximum embedded depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumEmbeddedDepth = 16;
    /// <summary>
    /// The default maximum named property characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumNamedPropertyCharacters = 4096;
}
