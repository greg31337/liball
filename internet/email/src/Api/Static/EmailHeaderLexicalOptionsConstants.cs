using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailHeaderLexicalOptionsConstants
{
    /// <summary>
    /// The default maximum tokens used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumTokens = 4096;
    /// <summary>
    /// The default maximum token bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumTokenBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
    /// <summary>
    /// The default maximum comment depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumCommentDepth = EmailParsingLimitsConstants.DefaultMaxNestingDepth;
}
