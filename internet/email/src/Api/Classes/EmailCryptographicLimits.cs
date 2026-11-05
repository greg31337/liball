namespace email;
/// <summary>
/// Represents a cryptographic limits.
/// </summary>
public class EmailCryptographicLimits : IEmailCryptographicLimitsContract
{
    /// <summary>
    /// The default max input bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxInputBytes = 104857600;
    /// <summary>
    /// The default max signature count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxSignatureCount = 32;
    /// <summary>
    /// The default max recipient count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxRecipientCount = 1000;
    /// <summary>
    /// The default max certificate bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxCertificateBytes = 4194304;
    /// <summary>
    /// The default max key bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxKeyBytes = 1048576;
    /// <summary>
    /// The default max ASN.1 depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxAsn1Depth = 64;
    /// <summary>
    /// The default max ASN.1 element count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxAsn1ElementCount = 100000;
    /// <summary>
    /// The default max OpenPGP packet count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxOpenPgpPacketCount = 100000;
    /// <summary>
    /// The default max decompressed bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxDecompressedBytes = 104857600;
    /// <summary>
    /// The default max operation count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxOperationCount = 256;
    long maxInputBytes;
    int maxSignatureCount;
    int maxRecipientCount;
    long maxCertificateBytes;
    long maxKeyBytes;
    int maxAsn1Depth;
    int maxAsn1ElementCount;
    int maxOpenPgpPacketCount;
    long maxDecompressedBytes;
    int maxOperationCount;
    /// <summary>
    /// Creates a new cryptographic limits.
    /// </summary>
    public EmailCryptographicLimits()
    {
        maxInputBytes = DefaultMaxInputBytes;
        maxSignatureCount = DefaultMaxSignatureCount;
        maxRecipientCount = DefaultMaxRecipientCount;
        maxCertificateBytes = DefaultMaxCertificateBytes;
        maxKeyBytes = DefaultMaxKeyBytes;
        maxAsn1Depth = DefaultMaxAsn1Depth;
        maxAsn1ElementCount = DefaultMaxAsn1ElementCount;
        maxOpenPgpPacketCount = DefaultMaxOpenPgpPacketCount;
        maxDecompressedBytes = DefaultMaxDecompressedBytes;
        maxOperationCount = DefaultMaxOperationCount;
    }

    /// <summary>
    /// Gets the max input bytes.
    /// </summary>
    /// <returns>The current max input bytes.</returns>
    public long GetMaxInputBytes()
    {
        return maxInputBytes;
    }

    /// <summary>
    /// Sets the max input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxInputBytes(long value)
    {
        maxInputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max signature count.
    /// </summary>
    /// <returns>The current max signature count.</returns>
    public int GetMaxSignatureCount()
    {
        return maxSignatureCount;
    }

    /// <summary>
    /// Sets the max signature count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxSignatureCount(int value)
    {
        maxSignatureCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max recipient count.
    /// </summary>
    /// <returns>The current max recipient count.</returns>
    public int GetMaxRecipientCount()
    {
        return maxRecipientCount;
    }

    /// <summary>
    /// Sets the max recipient count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxRecipientCount(int value)
    {
        maxRecipientCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max certificate bytes.
    /// </summary>
    /// <returns>The current max certificate bytes.</returns>
    public long GetMaxCertificateBytes()
    {
        return maxCertificateBytes;
    }

    /// <summary>
    /// Sets the max certificate bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxCertificateBytes(long value)
    {
        maxCertificateBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max key bytes.
    /// </summary>
    /// <returns>The current max key bytes.</returns>
    public long GetMaxKeyBytes()
    {
        return maxKeyBytes;
    }

    /// <summary>
    /// Sets the max key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxKeyBytes(long value)
    {
        maxKeyBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max ASN.1 depth.
    /// </summary>
    /// <returns>The current max ASN.1 depth.</returns>
    public int GetMaxAsn1Depth()
    {
        return maxAsn1Depth;
    }

    /// <summary>
    /// Sets the max ASN.1 depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxAsn1Depth(int value)
    {
        maxAsn1Depth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max ASN.1 element count.
    /// </summary>
    /// <returns>The current max ASN.1 element count.</returns>
    public int GetMaxAsn1ElementCount()
    {
        return maxAsn1ElementCount;
    }

    /// <summary>
    /// Sets the max ASN.1 element count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxAsn1ElementCount(int value)
    {
        maxAsn1ElementCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max OpenPGP packet count.
    /// </summary>
    /// <returns>The current max OpenPGP packet count.</returns>
    public int GetMaxOpenPgpPacketCount()
    {
        return maxOpenPgpPacketCount;
    }

    /// <summary>
    /// Sets the max OpenPGP packet count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxOpenPgpPacketCount(int value)
    {
        maxOpenPgpPacketCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max decompressed bytes.
    /// </summary>
    /// <returns>The current max decompressed bytes.</returns>
    public long GetMaxDecompressedBytes()
    {
        return maxDecompressedBytes;
    }

    /// <summary>
    /// Sets the max decompressed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxDecompressedBytes(long value)
    {
        maxDecompressedBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max operation count.
    /// </summary>
    /// <returns>The current max operation count.</returns>
    public int GetMaxOperationCount()
    {
        return maxOperationCount;
    }

    /// <summary>
    /// Sets the max operation count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxOperationCount(int value)
    {
        maxOperationCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCryptographicLimits Clone()
    {
        EmailCryptographicLimits copy = new EmailCryptographicLimits();
        copy.SetMaxInputBytes(maxInputBytes);
        copy.SetMaxSignatureCount(maxSignatureCount);
        copy.SetMaxRecipientCount(maxRecipientCount);
        copy.SetMaxCertificateBytes(maxCertificateBytes);
        copy.SetMaxKeyBytes(maxKeyBytes);
        copy.SetMaxAsn1Depth(maxAsn1Depth);
        copy.SetMaxAsn1ElementCount(maxAsn1ElementCount);
        copy.SetMaxOpenPgpPacketCount(maxOpenPgpPacketCount);
        copy.SetMaxDecompressedBytes(maxDecompressedBytes);
        copy.SetMaxOperationCount(maxOperationCount);
        return copy;
    }
}
