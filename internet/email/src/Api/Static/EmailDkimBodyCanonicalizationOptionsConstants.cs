using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimBodyCanonicalizationOptionsConstants
{
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumInputBytes = EmailCryptographicLimitsConstants.DefaultMaxInputBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumCanonicalizedBytes = EmailCryptographicLimitsConstants.DefaultMaxInputBytes + 2;
    /// <summary>
    /// The default maximum hashed bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumHashedBytes = DefaultMaximumCanonicalizedBytes;
}
