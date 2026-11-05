using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailCmsReaderConstants
{
    /// <summary>
    /// The data object identifier value exposed by this API.
    /// </summary>
    public const string DataObjectIdentifier = "1.2.840.113549.1.7.1";
    /// <summary>
    /// The signed data object identifier value exposed by this API.
    /// </summary>
    public const string SignedDataObjectIdentifier = "1.2.840.113549.1.7.2";
    /// <summary>
    /// The enveloped data object identifier value exposed by this API.
    /// </summary>
    public const string EnvelopedDataObjectIdentifier = "1.2.840.113549.1.7.3";
    /// <summary>
    /// The signed and enveloped data object identifier value exposed by this API.
    /// </summary>
    public const string SignedAndEnvelopedDataObjectIdentifier = "1.2.840.113549.1.7.4";
    /// <summary>
    /// The digested data object identifier value exposed by this API.
    /// </summary>
    public const string DigestedDataObjectIdentifier = "1.2.840.113549.1.7.5";
    /// <summary>
    /// The encrypted data object identifier value exposed by this API.
    /// </summary>
    public const string EncryptedDataObjectIdentifier = "1.2.840.113549.1.7.6";
    /// <summary>
    /// The authenticated data object identifier value exposed by this API.
    /// </summary>
    public const string AuthenticatedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.2";
    /// <summary>
    /// The compressed data object identifier value exposed by this API.
    /// </summary>
    public const string CompressedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.9";
    /// <summary>
    /// The auth enveloped data object identifier value exposed by this API.
    /// </summary>
    public const string AuthEnvelopedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.23";
}
