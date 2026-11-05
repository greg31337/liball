using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC sealing operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcSealingResult : IEmailArcSealingResultContract
{
    int instance;
    EmailArcChainStatus chainStatus;
    EmailHeader authenticationResultsHeader;
    EmailArcAuthenticationResults authenticationResults;
    EmailHeader messageSignatureHeader;
    EmailDkimSignature messageSignature;
    EmailHeader sealHeader;
    EmailArcSeal seal;
    EmailDkimBodyCanonicalizationResult bodyCanonicalization;
    EmailDkimHeaderCanonicalizationResult messageHeaderCanonicalization;
    EmailArcSealCanonicalizationResult sealCanonicalization;
    byte[] messageHeaderHash;
    byte[] messageSignatureBytes;
    byte[] sealHeaderHash;
    byte[] sealSignatureBytes;
    EmailArcSetCollectionResult collection;
    int priorArcHeaderCount;
    long timestamp;
    /// <summary>
    /// Creates a new ARC sealing result.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "authenticationResultsHeader">The authentication results header used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "messageSignatureHeader">The message signature header used by the operation.</param>
    /// <param name = "messageSignature">The message signature used by the operation.</param>
    /// <param name = "sealHeader">The seal header used by the operation.</param>
    /// <param name = "seal">The seal used by the operation.</param>
    /// <param name = "bodyCanonicalization">The body canonicalization used by the operation.</param>
    /// <param name = "messageHeaderCanonicalization">The message header canonicalization used by the operation.</param>
    /// <param name = "sealCanonicalization">The seal canonicalization used by the operation.</param>
    /// <param name = "messageHeaderHash">The message header hash used by the operation.</param>
    /// <param name = "messageSignatureBytes">The source bytes or data to process.</param>
    /// <param name = "sealHeaderHash">The seal header hash used by the operation.</param>
    /// <param name = "sealSignatureBytes">The source bytes or data to process.</param>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "priorArcHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "timestamp">The timestamp used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSealingResult(int instance, EmailArcChainStatus chainStatus, EmailHeader authenticationResultsHeader, EmailArcAuthenticationResults? authenticationResults, EmailHeader messageSignatureHeader, EmailDkimSignature? messageSignature, EmailHeader sealHeader, EmailArcSeal? seal, EmailDkimBodyCanonicalizationResult bodyCanonicalization, EmailDkimHeaderCanonicalizationResult messageHeaderCanonicalization, EmailArcSealCanonicalizationResult sealCanonicalization, byte[] messageHeaderHash, byte[] messageSignatureBytes, byte[] sealHeaderHash, byte[] sealSignatureBytes, EmailArcSetCollectionResult collection, int priorArcHeaderCount, long timestamp)
    {
        if (instance < 1 || instance > 50 || priorArcHeaderCount < 0 || timestamp < 0)
            throw new ArgumentOutOfRangeException("instance");
        if (chainStatus < EmailArcChainStatus.None || chainStatus > EmailArcChainStatus.Fail)
            throw new ArgumentOutOfRangeException("chainStatus");
        if (authenticationResultsHeader == null || authenticationResults == null || messageSignatureHeader == null || messageSignature == null || sealHeader == null || seal == null || bodyCanonicalization == null || messageHeaderCanonicalization == null || sealCanonicalization == null || messageHeaderHash == null || messageSignatureBytes == null || sealHeaderHash == null || sealSignatureBytes == null || collection == null)
            throw new ArgumentNullException("authenticationResultsHeader");
        this.instance = instance;
        this.chainStatus = chainStatus;
        this.authenticationResultsHeader = authenticationResultsHeader.Clone();
        this.authenticationResults = authenticationResults;
        this.messageSignatureHeader = messageSignatureHeader.Clone();
        this.messageSignature = messageSignature;
        this.sealHeader = sealHeader.Clone();
        this.seal = seal;
        this.bodyCanonicalization = bodyCanonicalization;
        this.messageHeaderCanonicalization = messageHeaderCanonicalization;
        this.sealCanonicalization = sealCanonicalization;
        this.messageHeaderHash = Copy(messageHeaderHash);
        this.messageSignatureBytes = Copy(messageSignatureBytes);
        this.sealHeaderHash = Copy(sealHeaderHash);
        this.sealSignatureBytes = Copy(sealSignatureBytes);
        this.collection = collection;
        this.priorArcHeaderCount = priorArcHeaderCount;
        this.timestamp = timestamp;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return instance;
    }

    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    public EmailArcChainStatus GetChainStatus()
    {
        return chainStatus;
    }

    /// <summary>
    /// Gets the authentication results header.
    /// </summary>
    /// <returns>The current authentication results header.</returns>
    public EmailHeader GetAuthenticationResultsHeader()
    {
        return authenticationResultsHeader.Clone();
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    public EmailArcAuthenticationResults GetAuthenticationResults()
    {
        return authenticationResults;
    }

    /// <summary>
    /// Gets the message signature header.
    /// </summary>
    /// <returns>The current message signature header.</returns>
    public EmailHeader GetMessageSignatureHeader()
    {
        return messageSignatureHeader.Clone();
    }

    /// <summary>
    /// Gets the message signature.
    /// </summary>
    /// <returns>The current message signature.</returns>
    public EmailDkimSignature GetMessageSignature()
    {
        return messageSignature;
    }

    /// <summary>
    /// Gets the seal header.
    /// </summary>
    /// <returns>The current seal header.</returns>
    public EmailHeader GetSealHeader()
    {
        return sealHeader.Clone();
    }

    /// <summary>
    /// Gets the seal.
    /// </summary>
    /// <returns>The current seal.</returns>
    public EmailArcSeal GetSeal()
    {
        return seal;
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
    /// Gets the message header canonicalization.
    /// </summary>
    /// <returns>The current message header canonicalization.</returns>
    public EmailDkimHeaderCanonicalizationResult GetMessageHeaderCanonicalization()
    {
        return messageHeaderCanonicalization;
    }

    /// <summary>
    /// Gets the seal canonicalization.
    /// </summary>
    /// <returns>The current seal canonicalization.</returns>
    public EmailArcSealCanonicalizationResult GetSealCanonicalization()
    {
        return sealCanonicalization;
    }

    /// <summary>
    /// Gets the message header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message header hash bytes.</returns>
    public byte[] GetMessageHeaderHashBytes()
    {
        return Copy(messageHeaderHash);
    }

    /// <summary>
    /// Gets the message signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message signature bytes.</returns>
    public byte[] GetMessageSignatureBytes()
    {
        return Copy(messageSignatureBytes);
    }

    /// <summary>
    /// Gets the seal header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal header hash bytes.</returns>
    public byte[] GetSealHeaderHashBytes()
    {
        return Copy(sealHeaderHash);
    }

    /// <summary>
    /// Gets the seal signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal signature bytes.</returns>
    public byte[] GetSealSignatureBytes()
    {
        return Copy(sealSignatureBytes);
    }

    /// <summary>
    /// Gets the collection.
    /// </summary>
    /// <returns>The current collection.</returns>
    public EmailArcSetCollectionResult GetCollection()
    {
        return collection;
    }

    /// <summary>
    /// Gets the prior ARC header count.
    /// </summary>
    /// <returns>The current prior ARC header count.</returns>
    public int GetPriorArcHeaderCount()
    {
        return priorArcHeaderCount;
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public long GetTimestamp()
    {
        return timestamp;
    }

    /// <summary>
    /// Gets the headers in insertion order.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current headers in insertion order.</returns>
    public List<EmailHeader> GetHeadersInInsertionOrder()
    {
        List<EmailHeader> headers = new List<EmailHeader>();
        headers.Add(sealHeader.Clone());
        headers.Add(messageSignatureHeader.Clone());
        headers.Add(authenticationResultsHeader.Clone());
        return headers;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
