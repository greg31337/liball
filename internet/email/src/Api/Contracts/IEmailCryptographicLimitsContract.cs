namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCryptographicLimits"/>.
/// </summary>
public interface IEmailCryptographicLimitsContract
{
    /// <summary>
    /// Gets the max input bytes.
    /// </summary>
    /// <returns>The current max input bytes.</returns>
    long GetMaxInputBytes();
    /// <summary>
    /// Sets the max input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxInputBytes(long value);
    /// <summary>
    /// Gets the max signature count.
    /// </summary>
    /// <returns>The current max signature count.</returns>
    int GetMaxSignatureCount();
    /// <summary>
    /// Sets the max signature count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxSignatureCount(int value);
    /// <summary>
    /// Gets the max recipient count.
    /// </summary>
    /// <returns>The current max recipient count.</returns>
    int GetMaxRecipientCount();
    /// <summary>
    /// Sets the max recipient count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxRecipientCount(int value);
    /// <summary>
    /// Gets the max certificate bytes.
    /// </summary>
    /// <returns>The current max certificate bytes.</returns>
    long GetMaxCertificateBytes();
    /// <summary>
    /// Sets the max certificate bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxCertificateBytes(long value);
    /// <summary>
    /// Gets the max key bytes.
    /// </summary>
    /// <returns>The current max key bytes.</returns>
    long GetMaxKeyBytes();
    /// <summary>
    /// Sets the max key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxKeyBytes(long value);
    /// <summary>
    /// Gets the max ASN.1 depth.
    /// </summary>
    /// <returns>The current max ASN.1 depth.</returns>
    int GetMaxAsn1Depth();
    /// <summary>
    /// Sets the max ASN.1 depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxAsn1Depth(int value);
    /// <summary>
    /// Gets the max ASN.1 element count.
    /// </summary>
    /// <returns>The current max ASN.1 element count.</returns>
    int GetMaxAsn1ElementCount();
    /// <summary>
    /// Sets the max ASN.1 element count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxAsn1ElementCount(int value);
    /// <summary>
    /// Gets the max OpenPGP packet count.
    /// </summary>
    /// <returns>The current max OpenPGP packet count.</returns>
    int GetMaxOpenPgpPacketCount();
    /// <summary>
    /// Sets the max OpenPGP packet count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxOpenPgpPacketCount(int value);
    /// <summary>
    /// Gets the max decompressed bytes.
    /// </summary>
    /// <returns>The current max decompressed bytes.</returns>
    long GetMaxDecompressedBytes();
    /// <summary>
    /// Sets the max decompressed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxDecompressedBytes(long value);
    /// <summary>
    /// Gets the max operation count.
    /// </summary>
    /// <returns>The current max operation count.</returns>
    int GetMaxOperationCount();
    /// <summary>
    /// Sets the max operation count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxOperationCount(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCryptographicLimits Clone();
}
