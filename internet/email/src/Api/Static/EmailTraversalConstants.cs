using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTraversalConstants
{
    /// <summary>
    /// The default maximum depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumDepth = EmailParsingLimitsConstants.DefaultMaxNestingDepth;
    /// <summary>
    /// The default maximum entity count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumEntityCount = EmailParsingLimitsConstants.DefaultMaxPartCount;
}
