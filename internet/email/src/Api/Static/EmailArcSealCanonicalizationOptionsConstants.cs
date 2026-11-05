using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailArcSealCanonicalizationOptionsConstants
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 150;
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumInputBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumCanonicalizedBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
}
