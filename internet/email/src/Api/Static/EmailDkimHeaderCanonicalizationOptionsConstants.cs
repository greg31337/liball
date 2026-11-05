using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimHeaderCanonicalizationOptionsConstants
{
    /// <summary>
    /// The default maximum selected header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSelectedHeaderCount = EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount;
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumInputBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumCanonicalizedBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
}
