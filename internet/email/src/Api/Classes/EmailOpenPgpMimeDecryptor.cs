using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with openPGP MIME decryptor.
/// </summary>
public class EmailOpenPgpMimeDecryptor : IEmailOpenPgpMimeDecryptorContract
{
    internal static EmailOpenPgpDecryptionResult DecryptImplementationWithRootAndCredentials(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials)
    {
        return DecryptImplementationWithRootAndCredentialsAndOptions(root, credentials, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    public EmailOpenPgpDecryptionResult DecryptWithRootAndCredentials(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials)
    {
        return EmailOpenPgpMimeDecryptor.DecryptImplementationWithRootAndCredentials(root, credentials);
    }

    internal static EmailOpenPgpDecryptionResult DecryptImplementationWithRootAndCredentialsAndOptions(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        ValidateCredentials(credentials);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        EmailSecurityMultipartParseResult parsed = new EmailSecurityMultipartParser().ParseWithRoot(root);
        if (!parsed.IsSuccess() || !(parsed.GetSecurityMultipart() ?? throw new InvalidOperationException()).IsEncrypted() || !string.Equals((parsed.GetSecurityMultipart() ?? throw new InvalidOperationException()).GetProtocol(), "application/pgp-encrypted", StringComparison.OrdinalIgnoreCase))
            return Failure(EmailOpenPgpDecryptionStatus.Malformed, new byte[0], issues, EmailIssueCode.OpenPgpMimeMalformed, "The entity is not a valid OpenPGP multipart/encrypted object.");
        EmailSecurityMultipart? security = parsed.GetSecurityMultipart();
        byte[]? control = EmailOpenPgpMimeUtility.EntityContent((security ?? throw new InvalidOperationException()).GetEncryptionControlPart(), snapshot.GetLimits());
        if (control == null || !string.Equals(Encoding.ASCII.GetString(control).Trim(), "Version: 1", StringComparison.Ordinal))
            return Failure(EmailOpenPgpDecryptionStatus.Malformed, new byte[0], issues, EmailIssueCode.OpenPgpMimeMalformed, "The PGP/MIME encryption control part does not contain exactly Version: 1.");
        byte[]? encrypted = EmailOpenPgpMimeUtility.EntityContent(security.GetEncryptedContentPart(), snapshot.GetLimits());
        if (encrypted == null)
            return Failure(EmailOpenPgpDecryptionStatus.Malformed, new byte[0], issues, EmailIssueCode.OpenPgpMimeMalformed, "The PGP/MIME encrypted data part has no decodable content.");
        return DecryptMessage(encrypted, credentials, snapshot, issues);
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpDecryptionResult DecryptWithRootAndCredentialsAndOptions(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeDecryptor.DecryptImplementationWithRootAndCredentialsAndOptions(root, credentials, options);
    }

    internal static EmailOpenPgpDecryptionResult DecryptInlineImplementationWithInputAndCredentials(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials)
    {
        return DecryptInlineImplementationWithInputAndCredentialsAndOptions(input, credentials, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Decrypts the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    public EmailOpenPgpDecryptionResult DecryptInlineWithInputAndCredentials(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials)
    {
        return EmailOpenPgpMimeDecryptor.DecryptInlineImplementationWithInputAndCredentials(input, credentials);
    }

    internal static EmailOpenPgpDecryptionResult DecryptInlineImplementationWithInputAndCredentialsAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options)
    {
        ValidateCredentials(credentials);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailOpenPgpMimeUtility.Bound(input.Length, snapshot.GetLimits().GetMaxInputBytes(), "MaxOpenPgpInlineBytes");
        return DecryptMessage(input.ToArray(), credentials, snapshot, new EmailIssueList());
    }

    /// <summary>
    /// Decrypts the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpDecryptionResult DecryptInlineWithInputAndCredentialsAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeDecryptor.DecryptInlineImplementationWithInputAndCredentialsAndOptions(input, credentials, options);
    }

    static EmailOpenPgpDecryptionResult DecryptMessage(byte[] encrypted, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options, EmailIssueList issues)
    {
        EmailCryptographicWorkBudget work = new EmailCryptographicWorkBudget(options.GetLimits());
        EmailOpenPgpReaderOptions readerOptions = new EmailOpenPgpReaderOptions();
        readerOptions.SetLimits(options.GetLimits());
        EmailOpenPgpReadResult message = new EmailOpenPgpReader().ParseWithInputAndOptions(encrypted, readerOptions);
        if (!message.IsComplete() || message.GetPacketCount() < 2)
            return Failure(EmailOpenPgpDecryptionStatus.Malformed, new byte[0], issues, EmailIssueCode.OpenPgpMimeMalformed, "The encrypted OpenPGP packet sequence is malformed or incomplete.");
        EmailOpenPgpPacket encryptedPacket = message.GetPacketAt(message.GetPacketCount() - 1);
        if (encryptedPacket.GetTypeIdentifier() != 18)
            return Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm, new byte[0], issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "Only integrity-protected OpenPGP encrypted data packets are accepted.");
        int packetIndex = 0;
        while (packetIndex < message.GetPacketCount() - 1)
        {
            if (message.GetPacketAt(packetIndex).GetSessionKey() == null || !(message.GetPacketAt(packetIndex).GetSessionKey() ?? throw new InvalidOperationException()).IsPublicKeyEncrypted())
                return Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm, new byte[0], issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "The encrypted message contains an unsupported session-key packet.");
            packetIndex++;
        }

        byte[] seipd = encryptedPacket.GetBody();
        if (seipd.Length < 2 || seipd[0] != 1 && seipd[0] != 2)
            return Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm, new byte[0], issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "The integrity-protected data packet version is unsupported.");
        bool recipientMatched = false;
        bool unwrapAttempted = false;
        bool supportedSessionPacket = false;
        packetIndex = 0;
        while (packetIndex < message.GetPacketCount() - 1)
        {
            EmailOpenPgpSessionKeyPacket? sessionPacket = message.GetPacketAt(packetIndex).GetSessionKey();
            if ((sessionPacket ?? throw new InvalidOperationException()).GetPublicKeyAlgorithm() != EmailOpenPgpAlgorithmsConstants.PublicKeyRsa)
            {
                packetIndex++;
                continue;
            }

            supportedSessionPacket = true;
            if (seipd[0] == 2 && sessionPacket.GetVersion() != 6 || seipd[0] == 1 && sessionPacket.GetVersion() != 3)
                return Failure(EmailOpenPgpDecryptionStatus.Malformed, sessionPacket.GetRecipientIdentifier(), issues, EmailIssueCode.OpenPgpMimeMalformed, "The session-key and encrypted-data packet versions are inconsistent.");
            int credentialIndex = 0;
            while (credentialIndex < credentials.Count)
            {
                EmailOpenPgpDecryptionCredential credential = credentials[credentialIndex];
                if (MatchesRecipient(sessionPacket, credential))
                {
                    recipientMatched = true;
                    unwrapAttempted = true;
                    work.Charge(1);
                    EmailOpenPgpOperationResult unwrapped = new EmailOpenPgpAlgorithms().DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(sessionPacket.GetEncryptedSessionKey(), EmailOpenPgpAlgorithmsConstants.PublicKeyRsa, credential.GetPrivateKey(), options.GetAlgorithmOptions());
                    if (unwrapped.IsSuccess())
                    {
                        int symmetricAlgorithm = seipd[0] == 2 && seipd.Length > 1 ? seipd[1] : -1;
                        byte[] unwrappedPayload = unwrapped.GetOutput();
                        byte[]? sessionKey = SessionKey(unwrappedPayload, sessionPacket.GetVersion(), symmetricAlgorithm);
                        CryptographicOperations.ZeroMemory(unwrappedPayload);
                        if (sessionKey != null)
                        {
                            EmailOpenPgpDecryptionAttempt? attempt = DecryptSeipd(seipd, symmetricAlgorithm, sessionKey, options, work);
                            CryptographicOperations.ZeroMemory(sessionKey);
                            if ((attempt ?? throw new InvalidOperationException()).IsSuccess())
                            {
                                byte[]? packetPlaintext = attempt.GetPlaintext();
                                byte[]? content = ExtractLiteral(packetPlaintext, options, work);
                                EmailOpenPgpDecryptionResult result = content == null ? Failure(EmailOpenPgpDecryptionStatus.Malformed, sessionPacket.GetRecipientIdentifier(), issues, EmailIssueCode.OpenPgpMimeMalformed, "Authenticated plaintext does not contain a supported OpenPGP literal message.") : new EmailOpenPgpDecryptionResult(EmailOpenPgpDecryptionStatus.Success, content, sessionPacket.GetRecipientIdentifier(), true, issues);
                                CryptographicOperations.ZeroMemory(packetPlaintext);
                                if (content != null)
                                    CryptographicOperations.ZeroMemory(content);
                                unwrapped.Dispose();
                                return result;
                            }

                            EmailOpenPgpDecryptionStatus failureStatus = attempt.GetFailureStatus();
                            EmailIssueCode code = failureStatus == EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm ? EmailIssueCode.OpenPgpAlgorithmUnsupported : EmailIssueCode.OpenPgpAuthenticationFailed;
                            unwrapped.Dispose();
                            return Failure(failureStatus, sessionPacket.GetRecipientIdentifier(), issues, code, failureStatus == EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm ? "The encrypted data algorithms are unsupported." : "OpenPGP encrypted-data authentication failed.");
                        }
                    }

                    unwrapped.Dispose();
                }

                credentialIndex++;
            }

            packetIndex++;
        }

        if (!supportedSessionPacket)
            return Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm, new byte[0], issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "No session-key packet uses a supported public-key algorithm.");
        if (!recipientMatched)
            return Failure(EmailOpenPgpDecryptionStatus.RecipientNotFound, new byte[0], issues, EmailIssueCode.OpenPgpRecipientNotFound, "No caller-supplied private key matches an OpenPGP recipient identifier.");
        return Failure(unwrapAttempted ? EmailOpenPgpDecryptionStatus.KeyUnwrapFailed : EmailOpenPgpDecryptionStatus.RecipientNotFound, new byte[0], issues, EmailIssueCode.OpenPgpAuthenticationFailed, "The matching recipient session key could not be authenticated and unwrapped.");
    }

    static EmailOpenPgpDecryptionAttempt? DecryptSeipd(byte[] body, int symmetricAlgorithm, byte[] sessionKey, EmailOpenPgpMimeOptions options, EmailCryptographicWorkBudget work)
    {
        if (body[0] == 1)
            return DecryptV1(body, sessionKey, work);
        return DecryptV2(body, symmetricAlgorithm, sessionKey, options, work);
    }

    static EmailOpenPgpDecryptionAttempt? DecryptV2(byte[] body, int symmetricAlgorithm, byte[] sessionKey, EmailOpenPgpMimeOptions options, EmailCryptographicWorkBudget work)
    {
        if (body.Length < 68 || body[2] != EmailOpenPgpAlgorithmsConstants.AeadGcm || body[3] > 16 || !ValidKeyLength(symmetricAlgorithm, sessionKey.Length))
            return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm);
        work.Charge(1);
        int chunkBytes = 1 << (body[3] + 6);
        byte[] salt = EmailOpenPgpMimeUtility.Slice(body, 4, 32);
        byte[] info = [0xD2, 2, (byte)symmetricAlgorithm, EmailOpenPgpAlgorithmsConstants.AeadGcm, body[3]];
        byte[]? derived = new EmailOpenPgpMimeEncryptor().DeriveMessageKey(sessionKey, salt, info, sessionKey.Length + 4);
        byte[] key = EmailOpenPgpMimeUtility.Slice(derived, 0, sessionKey.Length);
        byte[] iv = EmailOpenPgpMimeUtility.Slice(derived, sessionKey.Length, 4);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            using (MemoryStream output = new MemoryStream())
            {
                int position = 36;
                ulong chunkIndex = 0;
                while (body.Length - position > 16)
                {
                    work.Charge(1);
                    int beforeSummary = body.Length - position - 16;
                    int recordBytes = Math.Min(chunkBytes + 16, beforeSummary);
                    if (recordBytes <= 16)
                        return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
                    EmailOpenPgpOperationResult result = new EmailOpenPgpAlgorithms().DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(new ReadOnlySpan<byte>(body, position, recordBytes), key, symmetricAlgorithm, EmailOpenPgpAlgorithmsConstants.AeadGcm, new EmailOpenPgpMimeEncryptor().Nonce(iv, chunkIndex), info, options.GetAlgorithmOptions());
                    if (!result.IsSuccess())
                    {
                        result.Dispose();
                        return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
                    }

                    byte[] chunk = result.GetOutput();
                    result.Dispose();
                    output.Write(chunk, 0, chunk.Length);
                    CryptographicOperations.ZeroMemory(chunk);
                    EmailOpenPgpMimeUtility.Bound(output.Length, options.GetLimits().GetMaxInputBytes(), "MaxOpenPgpDecryptedPacketBytes");
                    position += recordBytes;
                    chunkIndex++;
                }

                if (body.Length - position != 16)
                    return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
                byte[] summaryAad = new byte[13];
                info.CopyTo(summaryAad, 0);
                EmailOpenPgpMimeUtility.WriteUnsigned(summaryAad, 5, (ulong)output.Length, 8);
                work.Charge(1);
                EmailOpenPgpOperationResult summary = new EmailOpenPgpAlgorithms().DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(new ReadOnlySpan<byte>(body, position, 16), key, symmetricAlgorithm, EmailOpenPgpAlgorithmsConstants.AeadGcm, new EmailOpenPgpMimeEncryptor().Nonce(iv, chunkIndex), summaryAad, options.GetAlgorithmOptions());
                bool successful = summary.IsSuccess();
                summary.Dispose();
                return successful ? EmailOpenPgpDecryptionAttempt.Success(output.ToArray()) : EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
            }
        });
        CryptographicOperations.ZeroMemory(derived);
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(iv);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (EmailOpenPgpDecryptionAttempt? )captured.GetValue();
    }

    static EmailOpenPgpDecryptionAttempt DecryptV1(byte[] body, byte[] sessionKey, EmailCryptographicWorkBudget work)
    {
        if (body.Length < 1 + 18 + 22 || !ValidAesKeyBytes(sessionKey.Length))
            return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm);
        work.Charge(2);
        EmailCapturedOperation decryption = EmailOperationCapture.RunWithOperationUsingFunc(() => CfbDecrypt(new ReadOnlySpan<byte>(body, 1, body.Length - 1), sessionKey));
        if (!decryption.IsSuccess())
            return EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
        byte[]? decrypted = (byte[]? )decryption.GetValue();
        EmailOpenPgpDecryptionAttempt attempt = EmailOpenPgpDecryptionAttempt.Failure(EmailOpenPgpDecryptionStatus.AuthenticationFailed);
        if ((decrypted ?? throw new InvalidOperationException()).Length >= 40 && decrypted[14] == decrypted[16] && decrypted[15] == decrypted[17])
        {
            int marker = decrypted.Length - 22;
            if (decrypted[marker] == 0xD3 && decrypted[marker + 1] == 0x14)
            {
                byte[] actual = SHA1.HashData(new ReadOnlySpan<byte>(decrypted, 0, decrypted.Length - 20));
                bool valid = CryptographicOperations.FixedTimeEquals(actual, new ReadOnlySpan<byte>(decrypted, decrypted.Length - 20, 20));
                CryptographicOperations.ZeroMemory(actual);
                if (valid)
                    attempt = EmailOpenPgpDecryptionAttempt.Success(EmailOpenPgpMimeUtility.Slice(decrypted, 18, marker - 18));
            }
        }

        CryptographicOperations.ZeroMemory(decrypted);
        return attempt;
    }

    static byte[] CfbDecrypt(ReadOnlySpan<byte> ciphertext, byte[] key)
    {
        byte[] plaintext = new byte[ciphertext.Length];
        byte[] feedback = new byte[16];
        byte[] encryptedFeedback = new byte[16];
        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                int offset = 0;
                while (offset < ciphertext.Length)
                {
                    encryptor.TransformBlock(feedback, 0, 16, encryptedFeedback, 0);
                    int count = Math.Min(16, ciphertext.Length - offset);
                    int index = 0;
                    while (index < count)
                    {
                        plaintext[offset + index] = (byte)(ciphertext[offset + index] ^ encryptedFeedback[index]);
                        index++;
                    }

                    if (count == 16)
                        ciphertext.Slice(offset, 16).CopyTo(feedback);
                    offset += count;
                }
            }
        }

        CryptographicOperations.ZeroMemory(feedback);
        CryptographicOperations.ZeroMemory(encryptedFeedback);
        return plaintext;
    }

    static byte[]? ExtractLiteral(byte[]? packetBytes, EmailOpenPgpMimeOptions options, EmailCryptographicWorkBudget work)
    {
        EmailOpenPgpReaderOptions readerOptions = new EmailOpenPgpReaderOptions();
        readerOptions.SetLimits(options.GetLimits());
        EmailOpenPgpReadResult parsed = new EmailOpenPgpReader().ParseWithInputAndOptions(packetBytes, readerOptions);
        int depth = 0;
        while (parsed.IsComplete() && parsed.GetPacketCount() == 1 && parsed.GetPacketAt(0).GetCompressedData() != null)
        {
            if (depth++ >= options.GetLimits().GetMaxAsn1Depth())
                return null;
            work.Charge(1);
            EmailOpenPgpCompressedData? compressed = parsed.GetPacketAt(0).GetCompressedData();
            EmailOpenPgpOperationResult? operation = new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithmAndOptions((compressed ?? throw new InvalidOperationException()).GetCompressedData(), compressed.GetAlgorithm(), options.GetAlgorithmOptions());
            if (!(operation ?? throw new InvalidOperationException()).IsSuccess())
            {
                operation.Dispose();
                return null;
            }

            byte[] decompressed = operation.GetOutput();
            operation.Dispose();
            parsed = new EmailOpenPgpReader().ParseWithInputAndOptions(decompressed, readerOptions);
            CryptographicOperations.ZeroMemory(decompressed);
        }

        if (!parsed.IsComplete())
            return null;
        if (parsed.GetPacketCount() >= 1 && parsed.GetPacketAt(0).GetLiteralData() != null)
        {
            int index = 1;
            while (index < parsed.GetPacketCount() && parsed.GetPacketAt(index).GetTypeIdentifier() == 21)
                index++;
            return index == parsed.GetPacketCount() ? (parsed.GetPacketAt(0).GetLiteralData() ?? throw new InvalidOperationException()).GetData() : null;
        }

        if (parsed.GetPacketCount() == 3 && parsed.GetPacketAt(0).GetTypeIdentifier() == 4 && parsed.GetPacketAt(1).GetLiteralData() != null && parsed.GetPacketAt(2).GetSignature() != null)
            return (parsed.GetPacketAt(1).GetLiteralData() ?? throw new InvalidOperationException()).GetData();
        return null;
    }

    static byte[]? SessionKey(byte[] payload, int packetVersion, int expectedAlgorithm)
    {
        int offset = packetVersion == 3 ? 1 : 0;
        if (packetVersion != 3 && packetVersion != 6 || payload.Length < offset + 3)
            return null;
        int algorithm = packetVersion == 3 ? payload[0] : expectedAlgorithm;
        int keyBytes = KeyBytes(algorithm);
        if (keyBytes == 0 || payload.Length != offset + keyBytes + 2)
            return null;
        int checksum = 0;
        int index = 0;
        while (index < keyBytes)
        {
            checksum = (checksum + payload[offset + index]) & 0xFFFF;
            index++;
        }

        if (payload[payload.Length - 2] != (byte)(checksum >> 8) || payload[payload.Length - 1] != (byte)(checksum & 255))
            return null;
        return EmailOpenPgpMimeUtility.Slice(payload, offset, keyBytes);
    }

    static bool MatchesRecipient(EmailOpenPgpSessionKeyPacket packet, EmailOpenPgpDecryptionCredential credential)
    {
        byte[] requested = packet.GetRecipientIdentifier();
        if (requested.Length == 0)
            return true;
        return packet.GetVersion() == 3 ? requested.AsSpan().SequenceEqual(credential.GetKeyIdentifier()) : requested.AsSpan().SequenceEqual(credential.GetFingerprint());
    }

    static int KeyBytes(int algorithm)
    {
        return algorithm == 7 ? 16 : algorithm == 8 ? 24 : algorithm == 9 ? 32 : 0;
    }

    static bool ValidKeyLength(int algorithm, int bytes)
    {
        return KeyBytes(algorithm) == bytes;
    }

    static bool ValidAesKeyBytes(int bytes)
    {
        return bytes == 16 || bytes == 24 || bytes == 32;
    }

    static void ValidateCredentials(List<EmailOpenPgpDecryptionCredential> credentials)
    {
        if (credentials == null)
            throw new ArgumentNullException("credentials");
        int index = 0;
        while (index < credentials.Count)
        {
            if (credentials[index] == null)
                throw new ArgumentException("The credential list cannot contain null entries.", "credentials");
            index++;
        }
    }

    static EmailOpenPgpDecryptionResult Failure(EmailOpenPgpDecryptionStatus status, byte[] recipientIdentifier, EmailIssueList issues, EmailIssueCode code, string description)
    {
        issues.Add(EmailOpenPgpMimeUtility.Issue(code, description, "No unauthenticated plaintext was released."));
        return new EmailOpenPgpDecryptionResult(status, null, recipientIdentifier, false, issues);
    }
}
