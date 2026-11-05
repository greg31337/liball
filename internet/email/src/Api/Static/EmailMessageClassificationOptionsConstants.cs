using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailMessageClassificationOptionsConstants
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum header value bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderValueBytes = 65536;
    /// <summary>
    /// The default maximum subject characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSubjectCharacters = 4096;
}
