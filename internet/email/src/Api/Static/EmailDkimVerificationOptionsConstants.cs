using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimVerificationOptionsConstants
{
    /// <summary>
    /// The default maximum signature count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSignatureCount = 32;
    /// <summary>
    /// The default maximum signature header bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSignatureHeaderBytes = 65536;
    /// <summary>
    /// The default maximum signed header count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSignedHeaderCount = EmailDkimHeaderCanonicalizationOptionsConstants.DefaultMaximumSelectedHeaderCount;
}
