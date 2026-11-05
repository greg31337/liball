using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimKeyParserOptionsConstants
{
    /// <summary>
    /// The default maximum tags used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumTags = 64;
    /// <summary>
    /// The default minimum RSA key bits used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMinimumRsaKeyBits = 1024;
    /// <summary>
    /// The default maximum RSA key bits used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumRsaKeyBits = 4096;
}
