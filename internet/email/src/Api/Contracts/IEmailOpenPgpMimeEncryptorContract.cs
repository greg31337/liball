using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpMimeEncryptor"/>.
/// </summary>
public interface IEmailOpenPgpMimeEncryptorContract
{
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    EmailOpenPgpEncryptionResult? EncryptWithContentAndRecipients(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients);
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpEncryptionResult? EncryptWithContentAndRecipientsAndOptions(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    EmailOpenPgpEncryptionResult? EncryptWithCanonicalMimeEntityAndRecipients(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients);
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailOpenPgpEncryptionResult? EncryptWithCanonicalMimeEntityAndRecipientsAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Encrypts the v 2.
    /// </summary>
    /// <param name = "plaintextPackets">The plaintext packets used by the operation.</param>
    /// <param name = "sessionKey">The session key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[]? EncryptV2(byte[] plaintextPackets, byte[] sessionKey, EmailOpenPgpMimeOptions options, EmailIssueList issues);
    /// <summary>
    /// Derives the message key.
    /// </summary>
    /// <param name = "inputKey">The input key used by the operation.</param>
    /// <param name = "salt">The salt used by the operation.</param>
    /// <param name = "info">The info used by the operation.</param>
    /// <param name = "outputBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[]? DeriveMessageKey(byte[] inputKey, byte[] salt, byte[] info, int outputBytes);
    /// <summary>
    /// Builds the nonce required by the selected cryptographic operation.
    /// </summary>
    /// <param name = "iv">The iv used by the operation.</param>
    /// <param name = "chunkIndex">The zero-based chunk index.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] Nonce(byte[] iv, ulong chunkIndex);
    /// <summary>
    /// Builds the encrypted session-key payload for the selected recipients.
    /// </summary>
    /// <param name = "sessionKey">The session key used by the operation.</param>
    /// <param name = "includeAlgorithm">Whether to enable the include algorithm behavior.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] SessionPayload(byte[] sessionKey, bool includeAlgorithm, int symmetricAlgorithm);
}
