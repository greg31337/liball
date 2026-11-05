using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSigner"/>.
/// </summary>
public interface IEmailDkimSignerContract
{
    /// <summary>
    /// Determines whether the current value or state is RSA sha 1 signing supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is RSA sha 1 signing supported; otherwise, <c>false</c>.</returns>
    bool IsRsaSha1SigningSupported();
    /// <summary>
    /// Gets the RSA sha 1 signing support.
    /// </summary>
    /// <returns>The current RSA sha 1 signing support.</returns>
    EmailDkimSigningAttemptResult GetRsaSha1SigningSupport();
    /// <summary>
    /// Signs the RSA sha 1.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing attempt result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimSigningAttemptResult SignRsaSha1(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options);
    /// <summary>
    /// Determines whether the current value or state is ed 25519 sha 256 supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is ed 25519 sha 256 supported; otherwise, <c>false</c>.</returns>
    bool IsEd25519Sha256Supported();
    /// <summary>
    /// Gets the ed 25519 sha 256 support.
    /// </summary>
    /// <returns>The current ed 25519 sha 256 support.</returns>
    EmailDkimSigningAttemptResult GetEd25519Sha256Support();
    /// <summary>
    /// Signs the ed 25519 sha 256.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing attempt result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimSigningAttemptResult SignEd25519Sha256(EmailHeaders headers, ReadOnlySpan<byte> body, byte[] privateKey, EmailDkimSigningOptions options);
    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailDkimSigningResult Sign(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options);
    /// <summary>
    /// Signs the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing result produced by the operation.</returns>
    EmailDkimSigningResult SignAndInsert(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options);
}
