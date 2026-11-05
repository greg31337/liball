using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimCanonicalizer"/>.
/// </summary>
public interface IEmailDkimCanonicalizerContract
{
    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signedHeaderNames">The signed header names used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimCanonicalizationKind kind);
    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signedHeaderNames">The signed header names used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options);
    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader);
    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options);
    /// <summary>
    /// Determines whether the instance can onicalize body.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The DKIM body canonicalization output produced by the operation.</returns>
    EmailDkimBodyCanonicalizationOutput CanonicalizeBodyWithInputAndKind(ReadOnlySpan<byte> input, EmailDkimCanonicalizationKind kind);
    /// <summary>
    /// Determines whether the instance can onicalize body.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM body canonicalization output produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimBodyCanonicalizationOutput CanonicalizeBodyWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimBodyCanonicalizationOptions options);
}
