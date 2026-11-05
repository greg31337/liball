using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcCanonicalizer"/>.
/// </summary>
public interface IEmailArcCanonicalizerContract
{
    /// <summary>
    /// Determines whether the instance can onicalize message signature.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader);
    /// <summary>
    /// Determines whether the instance can onicalize message signature.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options);
    /// <summary>
    /// Determines whether the instance can onicalize seal.
    /// </summary>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "sealInstance">The seal instance used by the operation.</param>
    /// <returns>The ARC seal canonicalization result produced by the operation.</returns>
    EmailArcSealCanonicalizationResult CanonicalizeSealWithCollectionAndSealInstance(EmailArcSetCollectionResult collection, int sealInstance);
    /// <summary>
    /// Determines whether the instance can onicalize seal.
    /// </summary>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "sealInstance">The seal instance used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC seal canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcSealCanonicalizationResult CanonicalizeSealWithCollectionAndSealInstanceAndOptions(EmailArcSetCollectionResult collection, int sealInstance, EmailArcSealCanonicalizationOptions options);
}
