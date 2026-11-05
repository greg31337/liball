namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailCryptographicLimitsConstants
{
    /// <summary>
    /// The default max input bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxInputBytes = 104857600;
    /// <summary>
    /// The default max signature count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxSignatureCount = 32;
    /// <summary>
    /// The default max recipient count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxRecipientCount = 1000;
    /// <summary>
    /// The default max certificate bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxCertificateBytes = 4194304;
    /// <summary>
    /// The default max key bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxKeyBytes = 1048576;
    /// <summary>
    /// The default max ASN.1 depth used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxAsn1Depth = 64;
    /// <summary>
    /// The default max ASN.1 element count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxAsn1ElementCount = 100000;
    /// <summary>
    /// The default max OpenPGP packet count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxOpenPgpPacketCount = 100000;
    /// <summary>
    /// The default max decompressed bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaxDecompressedBytes = 104857600;
    /// <summary>
    /// The default max operation count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaxOperationCount = 256;
}
