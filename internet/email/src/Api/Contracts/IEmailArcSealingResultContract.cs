using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSealingResult"/>.
/// </summary>
public interface IEmailArcSealingResultContract
{
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    EmailArcChainStatus GetChainStatus();
    /// <summary>
    /// Gets the authentication results header.
    /// </summary>
    /// <returns>The current authentication results header.</returns>
    EmailHeader GetAuthenticationResultsHeader();
    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    EmailArcAuthenticationResults GetAuthenticationResults();
    /// <summary>
    /// Gets the message signature header.
    /// </summary>
    /// <returns>The current message signature header.</returns>
    EmailHeader GetMessageSignatureHeader();
    /// <summary>
    /// Gets the message signature.
    /// </summary>
    /// <returns>The current message signature.</returns>
    EmailDkimSignature GetMessageSignature();
    /// <summary>
    /// Gets the seal header.
    /// </summary>
    /// <returns>The current seal header.</returns>
    EmailHeader GetSealHeader();
    /// <summary>
    /// Gets the seal.
    /// </summary>
    /// <returns>The current seal.</returns>
    EmailArcSeal GetSeal();
    /// <summary>
    /// Gets the body canonicalization.
    /// </summary>
    /// <returns>The current body canonicalization.</returns>
    EmailDkimBodyCanonicalizationResult GetBodyCanonicalization();
    /// <summary>
    /// Gets the message header canonicalization.
    /// </summary>
    /// <returns>The current message header canonicalization.</returns>
    EmailDkimHeaderCanonicalizationResult GetMessageHeaderCanonicalization();
    /// <summary>
    /// Gets the seal canonicalization.
    /// </summary>
    /// <returns>The current seal canonicalization.</returns>
    EmailArcSealCanonicalizationResult GetSealCanonicalization();
    /// <summary>
    /// Gets the message header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message header hash bytes.</returns>
    byte[] GetMessageHeaderHashBytes();
    /// <summary>
    /// Gets the message signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message signature bytes.</returns>
    byte[] GetMessageSignatureBytes();
    /// <summary>
    /// Gets the seal header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal header hash bytes.</returns>
    byte[] GetSealHeaderHashBytes();
    /// <summary>
    /// Gets the seal signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal signature bytes.</returns>
    byte[] GetSealSignatureBytes();
    /// <summary>
    /// Gets the collection.
    /// </summary>
    /// <returns>The current collection.</returns>
    EmailArcSetCollectionResult GetCollection();
    /// <summary>
    /// Gets the prior ARC header count.
    /// </summary>
    /// <returns>The current prior ARC header count.</returns>
    int GetPriorArcHeaderCount();
    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    long GetTimestamp();
    /// <summary>
    /// Gets the headers in insertion order.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current headers in insertion order.</returns>
    List<EmailHeader> GetHeadersInInsertionOrder();
}
