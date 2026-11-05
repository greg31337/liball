using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailOpenPgpMimeTests
{
    [Test]
    public void SignsAndVerifiesExactPgpMimeEntityOctets()
    {
        byte[] canonical = Bytes("Content-Type: text/plain; charset=us-ascii\r\nContent-Transfer-Encoding: 7bit\r\n\r\nsigned body\r\n");
        byte[] keyId = Convert.FromHexString("0123456789ABCDEF");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        EmailOpenPgpSigningCredential signer = new EmailOpenPgpSigningCredential(keyId, 19, 8, key);
        EmailOpenPgpMimeOptions options = new EmailOpenPgpMimeOptions();
        options.SetSignatureCreationTime(1700000000);

        EmailOpenPgpSigningResult signed = new EmailOpenPgpMimeSigner().SignWithContentAndCredentialAndOptions(ParseEntity(canonical), signer, options);
        EmailOpenPgpSigningResult defaultSigned = new EmailOpenPgpMimeSigner().SignWithContentAndCredential(ParseEntity(canonical), signer);
        MimeEntity root = ParseEntity(signed.GetMimeEntityBytes());
        EmailOpenPgpVerificationResult verified = new EmailOpenPgpMimeVerifier().VerifyWithRootAndKeysAndOptions(root, [new EmailOpenPgpVerificationKey(keyId, 19, key)], options);
        byte[] tamperedBytes = Replace(signed.GetMimeEntityBytes(), Bytes("signed body"), Bytes("forged body"));
        EmailOpenPgpVerificationResult tampered = new EmailOpenPgpMimeVerifier().VerifyWithRootAndKeysAndOptions(ParseEntity(tamperedBytes), [new EmailOpenPgpVerificationKey(keyId, 19, key)], options);
        EmailSecurityMultipart security = new EmailSecurityMultipartParser().ParseWithRoot(root).GetSecurityMultipart()!;

        Assert.Multiple(() =>
        {
            Assert.That(signed.IsSuccess(), Is.True);
            Assert.That(defaultSigned.IsSuccess(), Is.True);
            Assert.That(verified.IsSuccess(), Is.True);
            Assert.That(verified.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.Pass));
            Assert.That(verified.GetFormat(), Is.EqualTo(EmailOpenPgpSignedFormat.PgpMime));
            Assert.That(verified.GetContent(), Is.EqualTo(canonical));
            Assert.That(verified.GetSignerIdentifier(), Is.EqualTo(keyId));
            Assert.That(verified.GetHashAlgorithm(), Is.EqualTo(8));
            Assert.That(verified.GetPublicKeyAlgorithm(), Is.EqualTo(19));
            Assert.That(security!.GetProtocol(), Is.EqualTo("application/pgp-signature"));
            Assert.That(security.GetMicAlgorithms(), Is.EqualTo(new[] { "pgp-sha256" }));
            Assert.That(new EmailOpenPgpReader().ParseWithInput(signed.GetSignaturePacketBytes()).GetPacketAt(0).GetSignature()!.GetSignatureType(), Is.Zero);
            Assert.That(tampered.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.SignatureMismatch));
            Assert.That(tampered.GetIssues().ContainsCode(EmailIssueCode.OpenPgpSignatureMismatch), Is.True);
        });
    }

    [Test]
    public void RejectsUnsafeSignedMimeAndReportsMissingVerificationKey()
    {
        byte[] keyId = Convert.FromHexString("0123456789ABCDEF");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        EmailOpenPgpSigningCredential signer = new EmailOpenPgpSigningCredential(keyId, 19, 8, key);
        EmailOpenPgpSigningResult unsafeResult = new EmailOpenPgpMimeSigner().SignWithCanonicalMimeEntityAndCredential(Bytes("Content-Type: text/plain\n\ntrailing \n"), signer);
        EmailOpenPgpSigningResult signed = new EmailOpenPgpMimeSigner().SignWithCanonicalMimeEntityAndCredential(Bytes("Content-Type: text/plain\r\n\r\nbody"), signer);
        EmailOpenPgpVerificationResult missing = new EmailOpenPgpMimeVerifier().VerifyWithRootAndKeys(ParseEntity(signed.GetMimeEntityBytes()), []);

        Assert.Multiple(() =>
        {
            Assert.That(unsafeResult.IsSuccess(), Is.False);
            Assert.That(unsafeResult.GetIssues().ContainsCode(EmailIssueCode.OpenPgpSigningFailed), Is.True);
            Assert.That(missing.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.KeyNotFound));
            Assert.That(missing.GetContent(), Is.EqualTo(Bytes("Content-Type: text/plain\r\n\r\nbody")));
        });
    }

    [Test]
    public void EncryptsForMultipleRecipientsAndWithholdsTamperedPlaintext()
    {
        byte[] canonical = Bytes("Content-Type: text/plain\r\n\r\n" + new string('x', 400) + "\r\n");
        using RSA firstKey = RSA.Create(2048);
        using RSA secondKey = RSA.Create(2048);
        byte[] firstFingerprint = Enumerable.Range(0, 20).Select(value => (byte)value).ToArray();
        byte[] secondFingerprint = Enumerable.Range(20, 20).Select(value => (byte)value).ToArray();
        EmailOpenPgpMimeOptions options = LegacyRsaOptions();
        options.SetAeadChunkSizeOctet(0);

        MimeEntity contentEntity = ParseEntity(canonical);
        List<EmailOpenPgpEncryptionRecipient> recipients =
        [
            new EmailOpenPgpEncryptionRecipient(4, firstFingerprint, firstKey),
            new EmailOpenPgpEncryptionRecipient(4, secondFingerprint, secondKey)
        ];

        EmailOpenPgpEncryptionResult encrypted = new EmailOpenPgpMimeEncryptor().EncryptWithCanonicalMimeEntityAndRecipientsAndOptions(canonical,
            recipients, options)!;
        EmailOpenPgpEncryptionResult encryptedEntity = new EmailOpenPgpMimeEncryptor().EncryptWithContentAndRecipientsAndOptions(contentEntity, recipients, options)!;
        EmailOpenPgpEncryptionResult rejectedCanonical = new EmailOpenPgpMimeEncryptor().EncryptWithCanonicalMimeEntityAndRecipients(canonical, recipients)!;
        EmailOpenPgpEncryptionResult rejectedEntity = new EmailOpenPgpMimeEncryptor().EncryptWithContentAndRecipients(contentEntity, recipients)!;
        EmailOpenPgpDecryptionCredential second = new EmailOpenPgpDecryptionCredential(4, secondFingerprint, secondKey);
        EmailOpenPgpDecryptionResult decryptedMime = new EmailOpenPgpMimeDecryptor().DecryptWithRootAndCredentialsAndOptions(ParseEntity(encrypted!.GetMimeEntityBytes()), [second], options);
        EmailOpenPgpDecryptionResult defaultDecryptedMime = new EmailOpenPgpMimeDecryptor().DecryptWithRootAndCredentials(ParseEntity(encrypted.GetMimeEntityBytes()), [second]);
        EmailOpenPgpDecryptionResult decryptedInline = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(encrypted.GetOpenPgpMessageBytes(), [second], options);
        EmailOpenPgpDecryptionResult decryptedArmoredInline = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(encrypted.GetOpenPgpMessageBytes(), EmailOpenPgpArmorKind.Message, true), [second], options);
        EmailOpenPgpDecryptionResult missing = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(encrypted.GetOpenPgpMessageBytes(), [], options);
        EmailOpenPgpDecryptionResult malformed = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentials([1, 2, 3], []);
        EmailOpenPgpDecryptionResult unsupported = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentials(Join(Packet(13, Bytes("user")), Packet(9, [1])), []);
        using RSA wrongKey = RSA.Create(2048);
        EmailOpenPgpDecryptionResult unwrapFailed = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(encrypted.GetOpenPgpMessageBytes(), [new EmailOpenPgpDecryptionCredential(4, secondFingerprint, wrongKey)], options);
        byte[] tamperedMessage = encrypted.GetOpenPgpMessageBytes();
        tamperedMessage[tamperedMessage.Length - 1] ^= 1;
        EmailOpenPgpDecryptionResult tampered = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(tamperedMessage, [second], options);
        EmailOpenPgpReadResult packets = new EmailOpenPgpReader().ParseWithInput(encrypted.GetOpenPgpMessageBytes());

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.IsSuccess(), Is.True);
            Assert.That(encrypted.GetIssues().GetCount(), Is.Zero);
            Assert.That(encryptedEntity.IsSuccess(), Is.True);
            Assert.That(rejectedCanonical.IsSuccess(), Is.False);
            Assert.That(rejectedEntity.IsSuccess(), Is.False);
            Assert.That(encrypted.GetRecipientCount(), Is.EqualTo(2));
            Assert.That(packets.GetPacketCount(), Is.EqualTo(3));
            Assert.That(packets.GetPacketAt(0).GetSessionKey()!.GetVersion(), Is.EqualTo(6));
            Assert.That(packets.GetPacketAt(2).GetKind(), Is.EqualTo(EmailOpenPgpPacketKind.SymmetricallyEncryptedIntegrityProtectedData));
            Assert.That(packets.GetPacketAt(2).GetBody()[0], Is.EqualTo(2));
            Assert.That(decryptedMime.IsSuccess(), Is.True);
            Assert.That(decryptedMime.GetIssues().GetCount(), Is.Zero);
            Assert.That(defaultDecryptedMime.IsSuccess(), Is.True);
            Assert.That(second.GetKeyVersion(), Is.EqualTo(4));
            Assert.That(decryptedMime.IsAuthenticated(), Is.True);
            Assert.That(decryptedMime.GetContent(), Is.EqualTo(canonical));
            Assert.That(decryptedMime.GetRecipientIdentifier(), Is.EqualTo(secondFingerprint));
            Assert.That(decryptedInline.GetContent(), Is.EqualTo(canonical));
            Assert.That(decryptedArmoredInline.GetContent(), Is.EqualTo(canonical));
            Assert.That(missing.GetStatus(), Is.EqualTo(EmailOpenPgpDecryptionStatus.RecipientNotFound));
            Assert.That(malformed.GetStatus(), Is.EqualTo(EmailOpenPgpDecryptionStatus.Malformed));
            Assert.That(unsupported.GetStatus(), Is.EqualTo(EmailOpenPgpDecryptionStatus.UnsupportedAlgorithm));
            Assert.That(unwrapFailed.GetStatus(), Is.EqualTo(EmailOpenPgpDecryptionStatus.KeyUnwrapFailed));
            Assert.That(tampered.GetStatus(), Is.EqualTo(EmailOpenPgpDecryptionStatus.AuthenticationFailed));
            Assert.That(tampered.HasContent(), Is.False);
        });
    }

    [Test]
    public void ReadsLegacyIntegrityProtectedInlineEncryption()
    {
        using RSA key = RSA.Create(2048);
        byte[] fingerprint = Enumerable.Range(1, 20).Select(value => (byte)value).ToArray();
        byte[] content = Bytes("Content-Type: text/plain\r\n\r\nlegacy encrypted body\r\n");
        byte[] message = LegacyEncryptedMessage(content, fingerprint, key);

        EmailOpenPgpDecryptionResult decrypted = new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentials(message, [new EmailOpenPgpDecryptionCredential(4, fingerprint, key)]);

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.IsSuccess(), Is.True);
            Assert.That(decrypted.IsAuthenticated(), Is.True);
            Assert.That(decrypted.GetContent(), Is.EqualTo(content));
            Assert.That(decrypted.GetRecipientIdentifier(), Is.EqualTo(fingerprint.Skip(12).ToArray()));
        });
    }

    [Test]
    public void VerifiesReadOnlyInlineOnePassSignature()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] keyId = Convert.FromHexString("A0A1A2A3A4A5A6A7");
        byte[] content = Bytes("inline signed content\r\n");
        byte[] signature = SignaturePacket(content, 0, keyId, key);
        byte[] onePassBody = Join([3, 0, 8, 19], keyId, [1]);
        byte[] message = Join(Packet(4, onePassBody), LiteralPacket(content), signature);
        EmailOpenPgpOperationResult compressed = new EmailOpenPgpAlgorithms().CompressWithDataAndCompressionAlgorithm(message, 2);
        byte[] wrapped = Packet(8, Join([2], compressed.GetOutput()));
        byte[] armor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(wrapped, EmailOpenPgpArmorKind.Message, true);

        EmailOpenPgpVerificationResult result = new EmailOpenPgpMimeVerifier().VerifyInlineWithInputAndKeys(armor, [new EmailOpenPgpVerificationKey(keyId, 19, key)]);
        EmailOpenPgpVerificationResult withOptions = new EmailOpenPgpMimeVerifier().VerifyInlineWithInputAndKeysAndOptions(armor, [new EmailOpenPgpVerificationKey(keyId, 19, key)], new EmailOpenPgpMimeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetFormat(), Is.EqualTo(EmailOpenPgpSignedFormat.Inline));
            Assert.That(result.GetContent(), Is.EqualTo(content));
            Assert.That(result.GetSignerIdentifier(), Is.EqualTo(keyId));
            Assert.That(withOptions.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void VerifiesCleartextCanonicalizationAndDashEscapingReadOnly()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] keyId = Convert.FromHexString("1011121314151617");
        byte[] canonicalHashContent = Bytes("hello\r\n- dash");
        byte[] signature = SignaturePacket(canonicalHashContent, 1, keyId, key);
        byte[] armor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(signature, EmailOpenPgpArmorKind.Signature, true);
        byte[] cleartext = Join(Bytes("-----BEGIN PGP SIGNED MESSAGE-----\r\nHash: SHA256\r\n\r\nhello  \t\r\n- - dash\r\n"), armor);

        byte[] unsupportedPacket = signature.ToArray();
        unsupportedPacket[4] = 27;
        byte[] unsupportedArmor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(unsupportedPacket, EmailOpenPgpArmorKind.Signature, true);
        byte[] unsupportedCleartext = Join(Bytes("-----BEGIN PGP SIGNED MESSAGE-----\r\nHash: SHA256\r\n\r\nhello  \t\r\n- - dash\r\n"), unsupportedArmor);

        byte[] sha1Packet = signature.ToArray();
        sha1Packet[5] = 2;
        byte[] sha1Armor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(sha1Packet, EmailOpenPgpArmorKind.Signature, true);
        byte[] sha1Cleartext = Join(Bytes("-----BEGIN PGP SIGNED MESSAGE-----\r\nHash: SHA1\r\n\r\nhello  \t\r\n- - dash\r\n"), sha1Armor);

        EmailOpenPgpVerificationResult verified = new EmailOpenPgpMimeVerifier().VerifyCleartextWithInputAndKeys(cleartext, [new EmailOpenPgpVerificationKey(keyId, 19, key)]);
        EmailOpenPgpVerificationResult verifiedWithOptions = new EmailOpenPgpMimeVerifier().VerifyCleartextWithInputAndKeysAndOptions(cleartext, [new EmailOpenPgpVerificationKey(keyId, 19, key)], new EmailOpenPgpMimeOptions());
        byte[] misleading = Replace(cleartext, Bytes("Hash: SHA256"), Bytes("Note: SHA256"));
        EmailOpenPgpVerificationResult rejected = new EmailOpenPgpMimeVerifier().VerifyCleartextWithInputAndKeys(misleading, [new EmailOpenPgpVerificationKey(keyId, 19, key)]);
        EmailOpenPgpVerificationResult unsupported = new EmailOpenPgpMimeVerifier().VerifyCleartextWithInputAndKeys(unsupportedCleartext, [new EmailOpenPgpVerificationKey(keyId, 19, key)]);
        EmailOpenPgpVerificationResult policyRejected = new EmailOpenPgpMimeVerifier().VerifyCleartextWithInputAndKeys(sha1Cleartext, [new EmailOpenPgpVerificationKey(keyId, 19, key)]);

        Assert.Multiple(() =>
        {
            Assert.That(verified.IsSuccess(), Is.True);
            Assert.That(verifiedWithOptions.IsSuccess(), Is.True);
            Assert.That(verified.GetFormat(), Is.EqualTo(EmailOpenPgpSignedFormat.Cleartext));
            Assert.That(verified.GetContent(), Is.EqualTo(Bytes("hello  \t\r\n- dash")));
            Assert.That(rejected.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.Malformed));
            Assert.That(unsupported.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.UnsupportedAlgorithm));
            Assert.That(policyRejected.GetStatus(), Is.EqualTo(EmailOpenPgpVerificationStatus.PolicyRejected));
            Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.OpenPgpCleartextMalformed), Is.True);
        });
    }

    [Test]
    public void EnforcesMimeCryptographicLimits()
    {
        byte[] keyId = Convert.FromHexString("0123456789ABCDEF");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        EmailCryptographicLimits limits = new EmailCryptographicLimits();
        limits.SetMaxInputBytes(10);
        EmailOpenPgpMimeOptions options = new EmailOpenPgpMimeOptions();
        options.SetLimits(limits);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpMimeSigner().SignWithCanonicalMimeEntityAndCredentialAndOptions(Bytes("Content-Type: text/plain\r\n\r\nbody"), new EmailOpenPgpSigningCredential(keyId, 19, 8, key), options));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxOpenPgpMimeContentBytes"));
    }

    [Test]
    public void SignatureCreationTimeCanBeClearedFromMimeOptions()
    {
        EmailOpenPgpMimeOptions options = new EmailOpenPgpMimeOptions();
        options.SetSignatureCreationTime(1700000000);

        options.ClearSignatureCreationTime();

        Assert.That(options.HasSignatureCreationTime(), Is.False);
        Assert.Throws<InvalidOperationException>(() => options.GetSignatureCreationTime());
    }

    [Test]
    public void PublicVersionTwoEncryptionPrimitiveBuildsAuthenticatedPacketData()
    {
        byte[] sessionKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        byte[] plaintextPackets = LiteralPacket(Bytes("primitive content"));
        EmailOpenPgpMimeOptions options = new EmailOpenPgpMimeOptions();
        options.SetAeadChunkSizeOctet(0);
        EmailIssueList issues = new EmailIssueList();
        EmailOpenPgpMimeEncryptor encryptor = new EmailOpenPgpMimeEncryptor();

        byte[] encrypted = encryptor.EncryptV2(plaintextPackets, sessionKey, options, issues)!;
        byte[] sessionPayload = encryptor.SessionPayload(sessionKey, true, 9);

        Assert.That(encrypted, Is.Not.Null.And.Not.Empty);
        Assert.That(encrypted[0], Is.EqualTo(2));
        Assert.That(issues.GetCount(), Is.Zero);
        Assert.That(sessionPayload[0], Is.EqualTo(9));
        Assert.That(sessionPayload.AsSpan(1, sessionKey.Length).ToArray(), Is.EqualTo(sessionKey));
        int checksum = sessionKey.Sum(value => value) & 0xFFFF;
        Assert.That(sessionPayload[^2], Is.EqualTo((byte)(checksum >> 8)));
        Assert.That(sessionPayload[^1], Is.EqualTo((byte)checksum));
    }

    static EmailOpenPgpMimeOptions LegacyRsaOptions()
    {
        EmailOpenPgpAlgorithmOptions algorithms = new EmailOpenPgpAlgorithmOptions();
        algorithms.SetAllowDeprecatedRsaGeneration(true);
        EmailOpenPgpMimeOptions options = new EmailOpenPgpMimeOptions();
        options.SetAlgorithmOptions(algorithms);
        return options;
    }

    static byte[] SignaturePacket(byte[] content, int signatureType, byte[] keyId, ECDsa key)
    {
        byte[] hashed = [5, 0x82, 0x65, 0x53, 0xF1, 0x00];
        byte[] fields = Join([4, (byte)signatureType, 19, 8, 0, (byte)hashed.Length], hashed);
        byte[] trailer = [4, 0xFF, 0, 0, 0, (byte)fields.Length];
        byte[] digest = SHA256.HashData(Join(content, fields, trailer));
        byte[] raw = key.SignHash(digest, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        int half = raw.Length / 2;
        byte[] material = Join(Mpi(raw.AsSpan(0, half).ToArray()), Mpi(raw.AsSpan(half).ToArray()));
        byte[] unhashed = Join([9, 16], keyId);
        return Packet(2, Join(fields, [0, (byte)unhashed.Length], unhashed, [digest[0], digest[1]], material));
    }

    static byte[] LegacyEncryptedMessage(byte[] content, byte[] fingerprint, RSA rsa)
    {
        byte[] sessionKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        byte[] payload = SessionPayload(sessionKey, true);
        byte[] encryptedSession = rsa.Encrypt(payload, RSAEncryptionPadding.Pkcs1);
        byte[] keyId = fingerprint.Skip(12).ToArray();
        byte[] pkesk = Packet(1, Join([3], keyId, [1], Mpi(encryptedSession)));
        byte[] literal = LiteralPacket(content);
        byte[] prefix = Enumerable.Range(0, 16).Select(value => (byte)(0x80 + value)).ToArray();
        byte[] clear = Join(prefix, [prefix[14], prefix[15]], literal, [0xD3, 0x14]);
        clear = Join(clear, SHA1.HashData(clear));
        byte[] cipher = CfbEncrypt(clear, sessionKey);
        return Join(pkesk, Packet(18, Join([1], cipher)));
    }

    static byte[] CfbEncrypt(byte[] plaintext, byte[] key)
    {
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] feedback = new byte[16];
        byte[] encryptedFeedback = new byte[16];
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        int offset = 0;
        while (offset < plaintext.Length)
        {
            encryptor.TransformBlock(feedback, 0, 16, encryptedFeedback, 0);
            int count = Math.Min(16, plaintext.Length - offset);
            int index = 0;
            while (index < count) { ciphertext[offset + index] = (byte)(plaintext[offset + index] ^ encryptedFeedback[index]); index++; }
            if (count == 16) Array.Copy(ciphertext, offset, feedback, 0, 16);
            offset += count;
        }
        return ciphertext;
    }

    static byte[] SessionPayload(byte[] key, bool includeAlgorithm)
    {
        int sum = key.Sum(value => value) & 0xFFFF;
        return includeAlgorithm ? Join([9], key, [(byte)(sum >> 8), (byte)sum]) : Join(key, [(byte)(sum >> 8), (byte)sum]);
    }

    static byte[] LiteralPacket(byte[] content) { return Packet(11, Join([(byte)'b', 0, 0, 0, 0, 0], content)); }

    static byte[] Mpi(byte[] value)
    {
        int start = 0;
        while (start < value.Length && value[start] == 0) start++;
        if (start == value.Length) return [0, 0];
        int leading = 0;
        byte first = value[start];
        while ((first & 0x80) == 0) { leading++; first <<= 1; }
        int bits = (value.Length - start) * 8 - leading;
        return Join([(byte)(bits >> 8), (byte)bits], value.AsSpan(start).ToArray());
    }

    static byte[] Packet(int type, byte[] body)
    {
        if (body.Length < 192) return Join([(byte)(0xC0 | type), (byte)body.Length], body);
        if (body.Length <= 8383)
        {
            int adjusted = body.Length - 192;
            return Join([(byte)(0xC0 | type), (byte)((adjusted >> 8) + 192), (byte)adjusted], body);
        }
        return Join([(byte)(0xC0 | type), 255, (byte)(body.Length >> 24), (byte)(body.Length >> 16), (byte)(body.Length >> 8), (byte)body.Length], body);
    }

    static MimeEntity ParseEntity(byte[] bytes)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(bytes);
        Assert.That(result.IsComplete(), Is.True);
        return result.GetEntity()!;
    }

    static byte[] Replace(byte[] source, byte[] oldValue, byte[] newValue)
    {
        Assert.That(newValue.Length, Is.EqualTo(oldValue.Length));
        int offset = source.AsSpan().IndexOf(oldValue);
        Assert.That(offset, Is.GreaterThanOrEqualTo(0));
        byte[] result = source.ToArray();
        newValue.CopyTo(result, offset);
        return result;
    }

    static byte[] Join(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int position = 0;
        foreach (byte[] value in values) { value.CopyTo(result, position); position += value.Length; }
        return result;
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }
}
