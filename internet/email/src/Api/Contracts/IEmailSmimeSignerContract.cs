using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeSigner"/>.
/// </summary>
public interface IEmailSmimeSignerContract
{
    /// <summary>
    /// Signs the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    EmailSmimeSigningResult SignDetachedWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers);
    /// <summary>
    /// Signs the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    EmailSmimeSigningResult SignDetachedWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options);
    /// <summary>
    /// Signs the opaque.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    EmailSmimeSigningResult SignOpaqueWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers);
    /// <summary>
    /// Signs the opaque.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    EmailSmimeSigningResult SignOpaqueWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options);
}
