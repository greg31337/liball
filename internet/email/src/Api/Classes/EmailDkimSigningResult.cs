using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM signing operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimSigningResult : IEmailDkimSigningResultContract
{
    EmailHeader header;
    EmailDkimSignature signature;
    EmailDkimBodyCanonicalizationResult bodyCanonicalization;
    EmailDkimHeaderCanonicalizationResult headerCanonicalization;
    byte[] headerHash;
    byte[] signatureBytes;
    long timestamp;
    /// <summary>
    /// Creates a new DKIM signing result.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "bodyCanonicalization">The body canonicalization used by the operation.</param>
    /// <param name = "headerCanonicalization">The header canonicalization used by the operation.</param>
    /// <param name = "headerHash">The header hash used by the operation.</param>
    /// <param name = "signatureBytes">The source bytes or data to process.</param>
    /// <param name = "timestamp">The timestamp used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimSigningResult(EmailHeader header, EmailDkimSignature? signature, EmailDkimBodyCanonicalizationResult bodyCanonicalization, EmailDkimHeaderCanonicalizationResult headerCanonicalization, byte[] headerHash, byte[] signatureBytes, long timestamp)
    {
        if (header == null || signature == null || bodyCanonicalization == null || headerCanonicalization == null || headerHash == null || signatureBytes == null)
            throw new ArgumentNullException("header");
        if (timestamp < 0)
            throw new ArgumentOutOfRangeException("timestamp");
        this.header = header.Clone();
        this.signature = signature;
        this.bodyCanonicalization = bodyCanonicalization;
        this.headerCanonicalization = headerCanonicalization;
        this.headerHash = Copy(headerHash);
        this.signatureBytes = Copy(signatureBytes);
        this.timestamp = timestamp;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailHeader GetHeader()
    {
        return header.Clone();
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    public EmailDkimSignature GetSignature()
    {
        return signature;
    }

    /// <summary>
    /// Gets the body canonicalization.
    /// </summary>
    /// <returns>The current body canonicalization.</returns>
    public EmailDkimBodyCanonicalizationResult GetBodyCanonicalization()
    {
        return bodyCanonicalization;
    }

    /// <summary>
    /// Gets the header canonicalization.
    /// </summary>
    /// <returns>The current header canonicalization.</returns>
    public EmailDkimHeaderCanonicalizationResult GetHeaderCanonicalization()
    {
        return headerCanonicalization;
    }

    /// <summary>
    /// Gets the header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current header hash bytes.</returns>
    public byte[] GetHeaderHashBytes()
    {
        return Copy(headerHash);
    }

    /// <summary>
    /// Gets the signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signature bytes.</returns>
    public byte[] GetSignatureBytes()
    {
        return Copy(signatureBytes);
    }

    /// <summary>
    /// Gets the body hash Base64.
    /// </summary>
    /// <returns>The current body hash Base64.</returns>
    public string GetBodyHashBase64()
    {
        return Convert.ToBase64String(bodyCanonicalization.GetHash().GetDigestBytes());
    }

    /// <summary>
    /// Gets the signature Base64.
    /// </summary>
    /// <returns>The current signature Base64.</returns>
    public string GetSignatureBase64()
    {
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public long GetTimestamp()
    {
        return timestamp;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
