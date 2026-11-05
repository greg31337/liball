using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimSigningOptionsConstants
{
    /// <summary>
    /// The default minimum RSA key bits used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMinimumRsaKeyBits = EmailDkimKeyParserOptionsConstants.DefaultMinimumRsaKeyBits;
    /// <summary>
    /// The default maximum RSA key bits used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumRsaKeyBits = EmailDkimKeyParserOptionsConstants.DefaultMaximumRsaKeyBits;
    /// <summary>
    /// The default maximum signature header bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSignatureHeaderBytes = 65536;
    /// <summary>
    /// The default maximum signed header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSignedHeaderCount = EmailDkimHeaderCanonicalizationOptionsConstants.DefaultMaximumSelectedHeaderCount;
}
