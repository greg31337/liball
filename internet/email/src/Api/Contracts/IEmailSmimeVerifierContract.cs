using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeVerifier"/>.
/// </summary>
public interface IEmailSmimeVerifierContract
{
    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "cmsSignature">The cms signature used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    EmailSmimeVerificationResult VerifyDetachedWithContentAndCmsSignature(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature);
    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "cmsSignature">The cms signature used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeVerificationResult VerifyDetachedWithContentAndCmsSignatureAndOptions(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature, EmailSmimeVerificationOptions options);
    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    EmailSmimeVerificationResult VerifyDetachedWithMultipart(EmailSecurityMultipart multipart);
    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeVerificationResult VerifyDetachedWithMultipartAndOptions(EmailSecurityMultipart multipart, EmailSmimeVerificationOptions options);
    /// <summary>
    /// Verifies the opaque.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    EmailSmimeVerificationResult VerifyOpaqueWithCms(ReadOnlySpan<byte> cms);
    /// <summary>
    /// Verifies the opaque.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeVerificationResult VerifyOpaqueWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeVerificationOptions options);
}
