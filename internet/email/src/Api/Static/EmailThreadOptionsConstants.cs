using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailThreadOptionsConstants
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum identifier count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumIdentifierCount = 1000;
    /// <summary>
    /// The default maximum identifier bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumIdentifierBytes = 1048576;
    /// <summary>
    /// The default maximum subject bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSubjectBytes = 1048576;
    /// <summary>
    /// The default maximum subject characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSubjectCharacters = 10000;
    /// <summary>
    /// The default maximum graph message count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumGraphMessageCount = 10000;
    /// <summary>
    /// The default maximum graph identifier count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumGraphIdentifierCount = 100000;
}
