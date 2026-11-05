using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailReplyOptionsConstants
{
    /// <summary>
    /// The default maximum reference count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumReferenceCount = 100;
    /// <summary>
    /// The default maximum quoted characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumQuotedCharacters = 1000000;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum attachment bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumAttachmentBytes = 104857600;
}
