using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides operations for working with openPGP MIME encryptor.
/// </summary>
public class EmailOpenPgpMimeEncryptor : IEmailOpenPgpMimeEncryptorContract
{
    internal static EmailOpenPgpEncryptionResult? EncryptImplementationWithContentAndRecipients(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients)
    {
        return EncryptImplementationWithContentAndRecipientsAndOptions(content, recipients, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    public EmailOpenPgpEncryptionResult? EncryptWithContentAndRecipients(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients)
    {
        return EmailOpenPgpMimeEncryptor.EncryptImplementationWithContentAndRecipients(content, recipients);
    }

    internal static EmailOpenPgpEncryptionResult? EncryptImplementationWithContentAndRecipientsAndOptions(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options)
    {
        if (content == null)
            throw new ArgumentNullException("content");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[]? canonical = EmailOpenPgpMimeUtility.CanonicalEntity(content, options.GetLimits());
        if (canonical == null)
            return Failure(0, new EmailIssueList(), "The MIME entity could not be serialized canonically.");
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => EncryptImplementationWithCanonicalMimeEntityAndRecipientsAndOptions(canonical, recipients, options));
        CryptographicOperations.ZeroMemory(canonical);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (EmailOpenPgpEncryptionResult? )captured.GetValue();
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpEncryptionResult? EncryptWithContentAndRecipientsAndOptions(MimeEntity content, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeEncryptor.EncryptImplementationWithContentAndRecipientsAndOptions(content, recipients, options);
    }

    internal static EmailOpenPgpEncryptionResult? EncryptImplementationWithCanonicalMimeEntityAndRecipients(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients)
    {
        return EncryptImplementationWithCanonicalMimeEntityAndRecipientsAndOptions(canonicalMimeEntity, recipients, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    public EmailOpenPgpEncryptionResult? EncryptWithCanonicalMimeEntityAndRecipients(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients)
    {
        return EmailOpenPgpMimeEncryptor.EncryptImplementationWithCanonicalMimeEntityAndRecipients(canonicalMimeEntity, recipients);
    }

    internal static EmailOpenPgpEncryptionResult? EncryptImplementationWithCanonicalMimeEntityAndRecipientsAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options)
    {
        ValidateRecipients(recipients);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        EmailOpenPgpMimeUtility.Bound(canonicalMimeEntity.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMimeContentBytes");
        if (recipients.Count > limits.GetMaxRecipientCount())
            throw new EmailLimitException("MaxRecipientCount", limits.GetMaxRecipientCount(), recipients.Count);
        EmailIssueList issues = new EmailIssueList();
        if (recipients.Count == 0)
            return Failure(0, issues, "At least one OpenPGP encryption recipient is required.");
        if (!new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Aead, EmailOpenPgpAlgorithmsConstants.AeadGcm).IsSupported())
            return Failure(0, issues, "AES-GCM is unavailable from the runtime provider.");
        byte[] sessionKey = RandomNumberGenerator.GetBytes(32);
        byte[] sessionPayload = SessionPayloadImplementation(sessionKey, false, EmailOpenPgpAlgorithmsConstants.SymmetricAes256);
        byte[] literal = EmailOpenPgpMimeUtility.LiteralPacket(canonicalMimeEntity);
        EmailOpenPgpMimeUtility.Bound(literal.Length, limits.GetMaxInputBytes(), "MaxOpenPgpLiteralPacketBytes");
        List<byte[]> packets = new List<byte[]>();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            int configuredChunkBytes = 1 << (snapshot.GetAeadChunkSizeOctet() + 6);
            long operationCount = recipients.Count + (literal.LongLength + configuredChunkBytes - 1) / configuredChunkBytes + 2;
            if (operationCount > limits.GetMaxOperationCount())
                throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), operationCount);
            int index = 0;
            while (index < recipients.Count)
            {
                EmailOpenPgpEncryptionRecipient recipient = recipients[index];
                EmailOpenPgpOperationResult? wrapped = new EmailOpenPgpAlgorithms().EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(sessionPayload, EmailOpenPgpAlgorithmsConstants.PublicKeyRsa, recipient.GetPublicKey(), snapshot.GetAlgorithmOptions());
                if (!(wrapped ?? throw new InvalidOperationException()).IsSuccess())
                {
                    issues.AddRange(wrapped.GetIssues());
                    wrapped.Dispose();
                    return Failure(index, issues, "An OpenPGP recipient session key could not be wrapped.");
                }

                byte[] fingerprint = recipient.GetFingerprint();
                byte[] wrappedKey = wrapped.GetOutput();
                byte[] body = new byte[4 + fingerprint.Length + wrappedKey.Length];
                body[0] = 6;
                body[1] = (byte)(fingerprint.Length + 1);
                body[2] = (byte)recipient.GetKeyVersion();
                fingerprint.CopyTo(body, 3);
                body[3 + fingerprint.Length] = EmailOpenPgpAlgorithmsConstants.PublicKeyRsa;
                wrappedKey.CopyTo(body, 4 + fingerprint.Length);
                CryptographicOperations.ZeroMemory(wrappedKey);
                wrapped.Dispose();
                packets.Add(EmailOpenPgpMimeUtility.Packet(1, body));
                index++;
            }

            byte[]? seipd = EncryptV2Implementation(literal, sessionKey, snapshot, issues);
            if (seipd == null)
                return Failure(recipients.Count, issues, "The authenticated OpenPGP data packet could not be encrypted.");
            packets.Add(EmailOpenPgpMimeUtility.Packet(18, seipd));
            byte[] message = EmailOpenPgpMimeUtility.Join(packets.ToArray());
            EmailOpenPgpMimeUtility.Bound(message.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMessageBytes");
            byte[] armor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(message, EmailOpenPgpArmorKind.Message, snapshot.GetIncludeArmorChecksum());
            byte[] mime = EmailOpenPgpMimeUtility.EncryptedMime(armor);
            EmailOpenPgpMimeUtility.Bound(mime.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMimeOutputBytes");
            return new EmailOpenPgpEncryptionResult(mime, message, recipients.Count, issues);
        });
        CryptographicOperations.ZeroMemory(sessionKey);
        CryptographicOperations.ZeroMemory(sessionPayload);
        CryptographicOperations.ZeroMemory(literal);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (EmailOpenPgpEncryptionResult? )captured.GetValue();
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailOpenPgpEncryptionResult? EncryptWithCanonicalMimeEntityAndRecipientsAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, List<EmailOpenPgpEncryptionRecipient> recipients, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeEncryptor.EncryptImplementationWithCanonicalMimeEntityAndRecipientsAndOptions(canonicalMimeEntity, recipients, options);
    }

    internal static byte[]? EncryptV2Implementation(byte[] plaintextPackets, byte[] sessionKey, EmailOpenPgpMimeOptions options, EmailIssueList issues)
    {
        int chunkOctet = options.GetAeadChunkSizeOctet();
        int chunkBytes = 1 << (chunkOctet + 6);
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] info = [0xD2, 2, EmailOpenPgpAlgorithmsConstants.SymmetricAes256, EmailOpenPgpAlgorithmsConstants.AeadGcm, (byte)chunkOctet];
        byte[]? derived = DeriveMessageKeyImplementation(sessionKey, salt, info, 36);
        byte[] key = EmailOpenPgpMimeUtility.Slice(derived, 0, 32);
        byte[] iv = EmailOpenPgpMimeUtility.Slice(derived, 32, 4);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            using (MemoryStream encrypted = new MemoryStream())
            {
                int offset = 0;
                ulong chunkIndex = 0;
                while (offset < plaintextPackets.Length)
                {
                    int count = Math.Min(chunkBytes, plaintextPackets.Length - offset);
                    byte[] nonce = NonceImplementation(iv, chunkIndex);
                    EmailOpenPgpOperationResult result = new EmailOpenPgpAlgorithms().EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(new ReadOnlySpan<byte>(plaintextPackets, offset, count), key, EmailOpenPgpAlgorithmsConstants.SymmetricAes256, EmailOpenPgpAlgorithmsConstants.AeadGcm, nonce, info, options.GetAlgorithmOptions());
                    if (!result.IsSuccess())
                    {
                        issues.AddRange(result.GetIssues());
                        result.Dispose();
                        return null;
                    }

                    byte[] output = result.GetOutput();
                    result.Dispose();
                    encrypted.Write(output, 0, output.Length);
                    CryptographicOperations.ZeroMemory(output);
                    offset += count;
                    chunkIndex++;
                }

                byte[] summaryAad = new byte[13];
                info.CopyTo(summaryAad, 0);
                EmailOpenPgpMimeUtility.WriteUnsigned(summaryAad, 5, (ulong)plaintextPackets.Length, 8);
                EmailOpenPgpOperationResult summary = new EmailOpenPgpAlgorithms().EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte>.Empty, key, EmailOpenPgpAlgorithmsConstants.SymmetricAes256, EmailOpenPgpAlgorithmsConstants.AeadGcm, NonceImplementation(iv, chunkIndex), summaryAad, options.GetAlgorithmOptions());
                if (!summary.IsSuccess())
                {
                    issues.AddRange(summary.GetIssues());
                    summary.Dispose();
                    return null;
                }

                byte[] summaryTag = summary.GetOutput();
                summary.Dispose();
                encrypted.Write(summaryTag, 0, summaryTag.Length);
                CryptographicOperations.ZeroMemory(summaryTag);
                byte[] header = new byte[36];
                header[0] = 2;
                header[1] = EmailOpenPgpAlgorithmsConstants.SymmetricAes256;
                header[2] = EmailOpenPgpAlgorithmsConstants.AeadGcm;
                header[3] = (byte)chunkOctet;
                salt.CopyTo(header, 4);
                return EmailOpenPgpMimeUtility.Join([header, encrypted.ToArray()]);
            }
        });
        CryptographicOperations.ZeroMemory(derived);
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(iv);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (byte[]? )captured.GetValue();
    }

    /// <summary>
    /// Encrypts the v 2.
    /// </summary>
    /// <param name = "plaintextPackets">The plaintext packets used by the operation.</param>
    /// <param name = "sessionKey">The session key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[]? EncryptV2(byte[] plaintextPackets, byte[] sessionKey, EmailOpenPgpMimeOptions options, EmailIssueList issues)
    {
        return EmailOpenPgpMimeEncryptor.EncryptV2Implementation(plaintextPackets, sessionKey, options, issues);
    }

    internal static byte[]? DeriveMessageKeyImplementation(byte[] inputKey, byte[] salt, byte[] info, int outputBytes)
    {
        byte[] extract = HMACSHA256.HashData(salt, inputKey);
        byte[] output = new byte[outputBytes];
        byte[] previous = new byte[0];
        int written = 0;
        byte counter = 1;
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            while (written < output.Length)
            {
                byte[] material = EmailOpenPgpMimeUtility.Join([previous, info, [counter]]);
                EmailCapturedOperation blockOperation = EmailOperationCapture.RunWithOperationUsingFunc(() => HMACSHA256.HashData(extract, material));
                CryptographicOperations.ZeroMemory(material);
                if (!blockOperation.IsSuccess())
                    throw (blockOperation.GetException() ?? throw new InvalidOperationException());
                byte[]? block = (byte[]? )blockOperation.GetValue();
                int count = Math.Min((block ?? throw new InvalidOperationException()).Length, output.Length - written);
                Array.Copy(block, 0, output, written, count);
                written += count;
                if (previous.Length > 0)
                    CryptographicOperations.ZeroMemory(previous);
                previous = block;
                counter++;
            }

            return output;
        });
        CryptographicOperations.ZeroMemory(extract);
        if (previous.Length > 0)
            CryptographicOperations.ZeroMemory(previous);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (byte[]? )captured.GetValue();
    }

    /// <summary>
    /// Derives the message key.
    /// </summary>
    /// <param name = "inputKey">The input key used by the operation.</param>
    /// <param name = "salt">The salt used by the operation.</param>
    /// <param name = "info">The info used by the operation.</param>
    /// <param name = "outputBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[]? DeriveMessageKey(byte[] inputKey, byte[] salt, byte[] info, int outputBytes)
    {
        return EmailOpenPgpMimeEncryptor.DeriveMessageKeyImplementation(inputKey, salt, info, outputBytes);
    }

    internal static byte[] NonceImplementation(byte[] iv, ulong chunkIndex)
    {
        byte[] nonce = new byte[12];
        iv.CopyTo(nonce, 0);
        EmailOpenPgpMimeUtility.WriteUnsigned(nonce, 4, chunkIndex, 8);
        return nonce;
    }

    /// <summary>
    /// Builds the nonce required by the selected cryptographic operation.
    /// </summary>
    /// <param name = "iv">The iv used by the operation.</param>
    /// <param name = "chunkIndex">The zero-based chunk index.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] Nonce(byte[] iv, ulong chunkIndex)
    {
        return EmailOpenPgpMimeEncryptor.NonceImplementation(iv, chunkIndex);
    }

    internal static byte[] SessionPayloadImplementation(byte[] sessionKey, bool includeAlgorithm, int symmetricAlgorithm)
    {
        int offset = includeAlgorithm ? 1 : 0;
        byte[] value = new byte[offset + sessionKey.Length + 2];
        if (includeAlgorithm)
            value[0] = (byte)symmetricAlgorithm;
        sessionKey.CopyTo(value, offset);
        int checksum = 0;
        int index = 0;
        while (index < sessionKey.Length)
        {
            checksum = (checksum + sessionKey[index]) & 0xFFFF;
            index++;
        }

        value[value.Length - 2] = (byte)(checksum >> 8);
        value[value.Length - 1] = (byte)(checksum & 255);
        return value;
    }

    /// <summary>
    /// Builds the encrypted session-key payload for the selected recipients.
    /// </summary>
    /// <param name = "sessionKey">The session key used by the operation.</param>
    /// <param name = "includeAlgorithm">Whether to enable the include algorithm behavior.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] SessionPayload(byte[] sessionKey, bool includeAlgorithm, int symmetricAlgorithm)
    {
        return EmailOpenPgpMimeEncryptor.SessionPayloadImplementation(sessionKey, includeAlgorithm, symmetricAlgorithm);
    }

    static void ValidateRecipients(List<EmailOpenPgpEncryptionRecipient> recipients)
    {
        if (recipients == null)
            throw new ArgumentNullException("recipients");
        int index = 0;
        while (index < recipients.Count)
        {
            if (recipients[index] == null)
                throw new ArgumentException("The recipient list cannot contain null entries.", "recipients");
            index++;
        }
    }

    static EmailOpenPgpEncryptionResult Failure(int recipientCount, EmailIssueList issues, string description)
    {
        issues.Add(EmailOpenPgpMimeUtility.Issue(EmailIssueCode.OpenPgpEncryptionFailed, description, "No encrypted MIME or OpenPGP message was released."));
        return new EmailOpenPgpEncryptionResult(new byte[0], new byte[0], recipientCount, issues);
    }
}
