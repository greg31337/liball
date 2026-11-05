using System.Text;
using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseEncryptionServicePublicTests
{
    [Test]
    public void Encrypt0SupportsSecureReadsAttachedAndDetachedCiphertext()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy("A128GCM");
        byte[] plaintext = Encoding.UTF8.GetBytes("encrypt0 plaintext");
        byte[] externalData = Encoding.UTF8.GetBytes("encrypt0 aad");
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm,
            TokenKeyUses.Encryption);

        TokenBytesResult attached = service.EncryptCoseEncrypt0(
            plaintext,
            externalData,
            Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
            key,
            true,
            policy);
        CoseEncrypt0Result parsed = service.ReadCoseEncrypt0(
            attached.Bytes,
            policy);
        CoseEncrypt0Result denied =
            service.ReadCoseEncrypt0Diagnostic(attached.Bytes, policy);
        TokenPolicy diagnosticPolicy = Policy("A128GCM");
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        CoseEncrypt0Result diagnostic =
            service.ReadCoseEncrypt0Diagnostic(
                attached.Bytes,
                diagnosticPolicy);
        CoseEncrypt0Result decrypted = service.DecryptCoseEncrypt0(
            attached.Bytes,
            externalData,
            key,
            policy);
        CoseEncrypt0Result wrongData = service.DecryptCoseEncrypt0(
            attached.Bytes,
            new byte[] { 9 },
            key,
            policy);
        TokenBytesResult rewritten = service.WriteCoseEncrypt0(
            diagnostic.Message,
            false,
            policy);
        CoseDetachedEncryptionResult detached =
            service.EncryptDetachedCoseEncrypt0(
                plaintext,
                externalData,
                Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
                key,
                false,
                policy);
        CoseEncrypt0Result detachedDecrypted =
            service.DecryptDetachedCoseEncrypt0(
                detached.Bytes,
                detached.Ciphertext,
                externalData,
                key,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Ciphertext, Is.Null);
            Assert.That(parsed.Message.CiphertextAvailable, Is.False);
            Assert.That(parsed.Message.PlaintextAvailable, Is.False);
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error.Code,
                Is.EqualTo("unauthenticated_decode_not_allowed"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Message.Ciphertext, Is.Not.Empty);
            Assert.That(diagnostic.Message.CiphertextAvailable, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Message.PlaintextAvailable, Is.True);
            Assert.That(decrypted.Message.CiphertextAvailable, Is.True);
            Assert.That(decrypted.Message.AuthenticationTagVerified, Is.True);
            Assert.That(wrongData.Success, Is.False);
            Assert.That(wrongData.Error.Code,
                Is.EqualTo("decryption_failed"));
            Assert.That(rewritten.Success, Is.True);
            Assert.That(rewritten.Bytes[0], Is.EqualTo(0x83));
            Assert.That(detached.Success, Is.True);
            Assert.That(detachedDecrypted.Success, Is.True);
            Assert.That(detachedDecrypted.Message.Plaintext,
                Is.EqualTo(plaintext));
            Assert.That(detachedDecrypted.Message.Ciphertext,
                Is.EqualTo(detached.Ciphertext));
            Assert.That(detachedDecrypted.Message.CiphertextAvailable,
                Is.True);
        });
    }

    [Test]
    public void EncryptSupportsMultipleRecipientsNestedWritingAndDetachedCiphertext()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy("A128GCM", "A128KW", "direct");
        byte[] plaintext = Encoding.UTF8.GetBytes("recipient plaintext");
        byte[] externalData = Encoding.UTF8.GetBytes("recipient aad");
        TokenKey first = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap,
            TokenKeyUses.Encryption);
        TokenKey second = Symmetric(
            Sequence(16, 31),
            CoseAlgorithms.Aes128KeyWrap,
            TokenKeyUses.Encryption);
        CoseHeader[] recipientHeaders = new CoseHeader[]
        {
            Header(policy, CoseAlgorithms.Aes128KeyWrap, false, 1),
            Header(policy, CoseAlgorithms.Aes128KeyWrap, false, 2)
        };
        TokenKey[] keys = new TokenKey[] { first, second };

        TokenBytesResult attached = service.EncryptCoseEncrypt(
            plaintext,
            externalData,
            Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
            recipientHeaders,
            keys,
            null,
            true,
            policy);
        CoseEncryptResult parsed = service.ReadCoseEncrypt(
            attached.Bytes,
            policy);
        CoseEncryptResult selected = service.DecryptCoseEncryptRecipient(
            attached.Bytes,
            externalData,
            1,
            second,
            null,
            policy);
        CoseEncryptResult all = service.DecryptCoseEncryptAll(
            attached.Bytes,
            externalData,
            keys,
            null,
            policy);
        TokenPolicy diagnosticPolicy =
            Policy("A128GCM", "A128KW", "direct");
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        CoseEncryptResult diagnostic = service.ReadCoseEncryptDiagnostic(
            attached.Bytes,
            diagnosticPolicy);
        CoseRecipient child = new CoseRecipient();
        child.Header = Header(
            diagnosticPolicy,
            CoseAlgorithms.Direct,
            false,
            3);
        child.EncryptedKeyAbsent = true;
        child.EncryptedKey = null;
        diagnostic.Message.Recipients[0].Recipients =
            new CoseRecipient[] { child };
        TokenBytesResult nested = service.WriteCoseEncrypt(
            diagnostic.Message,
            false,
            diagnosticPolicy);
        CoseEncryptResult nestedRead = service.ReadCoseEncryptDiagnostic(
            nested.Bytes,
            diagnosticPolicy);
        CoseDetachedEncryptionResult detached =
            service.EncryptDetachedCoseEncrypt(
                plaintext,
                externalData,
                Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
                recipientHeaders,
                keys,
                null,
                false,
                policy);
        CoseEncryptResult detachedAll =
            service.DecryptDetachedCoseEncryptAll(
                detached.Bytes,
                detached.Ciphertext,
                externalData,
                keys,
                null,
                policy);
        CoseEncryptResult detachedSelected =
            service.DecryptDetachedCoseEncryptRecipient(
                detached.Bytes,
                detached.Ciphertext,
                externalData,
                0,
                first,
                null,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Ciphertext, Is.Null);
            Assert.That(parsed.Message.CiphertextAvailable, Is.False);
            Assert.That(parsed.Message.Recipients, Has.Length.EqualTo(2));
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Message.Plaintext, Is.EqualTo(plaintext));
            Assert.That(selected.Message.Recipients[0].Decrypted, Is.False);
            Assert.That(selected.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(all.Success, Is.True);
            Assert.That(all.Message.Recipients[0].Decrypted, Is.True);
            Assert.That(all.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(diagnostic.Message.CiphertextAvailable, Is.True);
            Assert.That(nested.Success, Is.True);
            Assert.That(nestedRead.Success, Is.True);
            Assert.That(nestedRead.Message.Recipients[0].Recipients,
                Has.Length.EqualTo(1));
            Assert.That(nestedRead.Message.Recipients[0].Recipients[0]
                .EncryptedKeyAbsent, Is.True);
            Assert.That(detached.Success, Is.True);
            Assert.That(detachedAll.Success, Is.True);
            Assert.That(detachedAll.Message.Plaintext,
                Is.EqualTo(plaintext));
            Assert.That(detachedAll.Message.CiphertextAvailable, Is.True);
            Assert.That(detachedSelected.Success, Is.True);
            Assert.That(detachedSelected.Message.Plaintext,
                Is.EqualTo(plaintext));
        });
    }

    [TestCase(CoseAlgorithms.Aes128Gcm, "A128GCM", 16)]
    [TestCase(CoseAlgorithms.Aes192Gcm, "A192GCM", 24)]
    [TestCase(CoseAlgorithms.Aes256Gcm, "A256GCM", 32)]
    public void Encrypt0RoundTripsEveryAesGcmAlgorithm(
        int algorithm,
        string algorithmName,
        int keySize)
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy(algorithmName);
        TokenKey key = Symmetric(
            Sequence(keySize, 70),
            algorithm,
            TokenKeyUses.Encryption);
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "public COSE content algorithm");

        TokenBytesResult encrypted = service.EncryptCoseEncrypt0(
            plaintext,
            System.Array.Empty<byte>(),
            Header(policy, algorithm, true, 0),
            key,
            true,
            policy);
        CoseEncrypt0Result decrypted = service.DecryptCoseEncrypt0(
            encrypted.Bytes!,
            System.Array.Empty<byte>(),
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Message.AuthenticationTagVerified, Is.True);
            Assert.That(decrypted.Message.Header.Algorithm, Is.EqualTo(algorithm));
        });
    }

    [TestCase(CoseAlgorithms.HmacSha256Truncated64, "HMAC 256/64", 32, 8)]
    [TestCase(CoseAlgorithms.HmacSha256, "HMAC 256/256", 32, 32)]
    [TestCase(CoseAlgorithms.HmacSha384, "HMAC 384/384", 48, 48)]
    [TestCase(CoseAlgorithms.HmacSha512, "HMAC 512/512", 64, 64)]
    public void Mac0RoundTripsEveryHmacAlgorithm(
        int algorithm,
        string algorithmName,
        int keySize,
        int tagSize)
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy(algorithmName);
        TokenKey key = Symmetric(
            Sequence(keySize, 90),
            algorithm,
            TokenKeyUses.Signature);
        byte[] payload = Encoding.UTF8.GetBytes("public COSE MAC algorithm");

        TokenBytesResult created = service.CreateCoseMac0(
            payload,
            System.Array.Empty<byte>(),
            Header(policy, algorithm, true, 0),
            key,
            true,
            policy);
        CoseMac0Result verified = service.VerifyCoseMac0(
            created.Bytes!,
            System.Array.Empty<byte>(),
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message!.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.Tag, Has.Length.EqualTo(tagSize));
            Assert.That(verified.Message.TagVerified, Is.True);
        });
    }

    [Test]
    public void DirectAndDirectHkdfRecipientModesRoundTripPublicly()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        byte[] plaintext = Encoding.UTF8.GetBytes("recipient mode payload");
        TokenPolicy directPolicy = Policy("A128GCM", "direct");
        TokenKey directKey = Symmetric(
            Sequence(16, 110),
            CoseAlgorithms.Direct,
            TokenKeyUses.Encryption);
        TokenBytesResult direct = service.EncryptCoseEncrypt(
            plaintext,
            System.Array.Empty<byte>(),
            Header(
                directPolicy,
                CoseAlgorithms.Aes128Gcm,
                true,
                0),
            new CoseHeader[]
            {
                Header(directPolicy, CoseAlgorithms.Direct, false, 1)
            },
            new TokenKey[] { directKey },
            null,
            true,
            directPolicy);
        CoseEncryptResult directDecrypted =
            service.DecryptCoseEncryptRecipient(
                direct.Bytes!,
                System.Array.Empty<byte>(),
                0,
                directKey,
                null,
                directPolicy);

        TokenPolicy hkdfPolicy = Policy(
            "A128GCM",
            "direct+HKDF-SHA-256");
        TokenKey hkdfKey = Symmetric(
            Sequence(32, 130),
            CoseAlgorithms.DirectHkdfSha256,
            TokenKeyUses.Encryption);
        hkdfKey.KeyOperations = new string[]
        {
            TokenKeyOperations.DeriveKey
        };
        CoseHeader hkdfRecipient = Header(
            hkdfPolicy,
            CoseAlgorithms.DirectHkdfSha256,
            false,
            2);
        hkdfRecipient.UnprotectedParameters = new CborMapEntry[]
        {
            hkdfRecipient.UnprotectedParameters![0],
            hkdfRecipient.UnprotectedParameters[1],
            Entry(
                CoseHeaderLabels.Salt,
                Bytes(Sequence(16, 3)))
        };
        TokenBytesResult hkdf = service.EncryptCoseEncrypt(
            plaintext,
            System.Array.Empty<byte>(),
            Header(hkdfPolicy, CoseAlgorithms.Aes128Gcm, true, 0),
            new CoseHeader[]
            {
                hkdfRecipient
            },
            new TokenKey[] { hkdfKey },
            null,
            true,
            hkdfPolicy);
        Assert.That(hkdf.Success, Is.True, hkdf.Error?.Code);
        CoseEncryptResult hkdfDecrypted =
            service.DecryptCoseEncryptRecipient(
                hkdf.Bytes!,
                System.Array.Empty<byte>(),
                0,
                hkdfKey,
                null,
                hkdfPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Success, Is.True);
            Assert.That(
                directDecrypted.Message!.Plaintext,
                Is.EqualTo(plaintext));
            Assert.That(
                directDecrypted.Message.Recipients[0].EncryptedKey,
                Is.Empty);
            Assert.That(hkdf.Success, Is.True);
            Assert.That(
                hkdfDecrypted.Message!.Plaintext,
                Is.EqualTo(plaintext));
            Assert.That(
                hkdfDecrypted.Message.Recipients[0].Decrypted,
                Is.True);
        });
    }

    [Test]
    public void TamperedMalformedAndOversizedCoseMessagesFailClosed()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy encryptionPolicy = Policy("A128GCM");
        TokenKey encryptionKey = Symmetric(
            Sequence(16, 150),
            CoseAlgorithms.Aes128Gcm,
            TokenKeyUses.Encryption);
        TokenBytesResult encrypted = service.EncryptCoseEncrypt0(
            Encoding.UTF8.GetBytes("authenticated ciphertext"),
            System.Array.Empty<byte>(),
            Header(
                encryptionPolicy,
                CoseAlgorithms.Aes128Gcm,
                true,
                0),
            encryptionKey,
            true,
            encryptionPolicy);
        byte[] tamperedCiphertext = encrypted.Bytes!.ToArray();
        tamperedCiphertext[^1] ^= 1;
        CoseEncrypt0Result tamperedEncryption =
            service.DecryptCoseEncrypt0(
                tamperedCiphertext,
                System.Array.Empty<byte>(),
                encryptionKey,
                encryptionPolicy);
        TokenPolicy small = Policy("A128GCM");
        small.MaximumTokenSizeInBytes = encrypted.Bytes.Length - 1;
        CoseEncrypt0Result oversized = service.ReadCoseEncrypt0(
            encrypted.Bytes,
            small);
        CoseEncrypt0 invalidEncryption = new CoseEncrypt0();
        invalidEncryption.Ciphertext = null;
        TokenBytesResult malformedEncryption = service.WriteCoseEncrypt0(
            invalidEncryption,
            true,
            encryptionPolicy);

        TokenPolicy macPolicy = Policy("HMAC 256/256");
        TokenKey macKey = Symmetric(
            Sequence(32, 170),
            CoseAlgorithms.HmacSha256,
            TokenKeyUses.Signature);
        TokenBytesResult mac = service.CreateCoseMac0(
            Encoding.UTF8.GetBytes("authenticated payload"),
            System.Array.Empty<byte>(),
            Header(macPolicy, CoseAlgorithms.HmacSha256, true, 0),
            macKey,
            true,
            macPolicy);
        byte[] tamperedTag = mac.Bytes!.ToArray();
        tamperedTag[^1] ^= 1;
        CoseMac0Result tamperedMac = service.VerifyCoseMac0(
            tamperedTag,
            System.Array.Empty<byte>(),
            macKey,
            macPolicy);
        CoseMac0 invalidMac = new CoseMac0();
        invalidMac.Payload = null;
        TokenBytesResult malformedMac = service.WriteCoseMac0(
            invalidMac,
            true,
            macPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(tamperedEncryption.Success, Is.False);
            Assert.That(
                tamperedEncryption.Error!.Code,
                Is.EqualTo("decryption_failed"));
            Assert.That(tamperedEncryption.Message, Is.Null);
            Assert.That(oversized.Success, Is.False);
            Assert.That(oversized.Error!.Code, Is.EqualTo("cbor_size_exceeded"));
            Assert.That(malformedEncryption.Success, Is.False);
            Assert.That(
                malformedEncryption.Error!.Code,
                Is.EqualTo("invalid_cose_encrypt0_ciphertext"));
            Assert.That(tamperedMac.Success, Is.False);
            Assert.That(tamperedMac.Error!.Code, Is.EqualTo("mac_mismatch"));
            Assert.That(tamperedMac.Message, Is.Null);
            Assert.That(malformedMac.Success, Is.False);
            Assert.That(
                malformedMac.Error!.Code,
                Is.EqualTo("invalid_cose_mac0_payload"));
        });
    }

    [Test]
    public void Mac0SupportsSecureReadsAttachedAndDetachedPayloads()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy("HMAC 256/256");
        byte[] payload = Encoding.UTF8.GetBytes("mac0 payload");
        byte[] externalData = Encoding.UTF8.GetBytes("mac0 aad");
        TokenKey key = Symmetric(
            Sequence(32, 4),
            CoseAlgorithms.HmacSha256,
            TokenKeyUses.Signature);

        TokenBytesResult attached = service.CreateCoseMac0(
            payload,
            externalData,
            Header(policy, CoseAlgorithms.HmacSha256, true, 0),
            key,
            true,
            policy);
        CoseMac0Result parsed = service.ReadCoseMac0(
            attached.Bytes,
            policy);
        CoseMac0Result denied = service.ReadCoseMac0Diagnostic(
            attached.Bytes,
            policy);
        TokenPolicy diagnosticPolicy = Policy("HMAC 256/256");
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        CoseMac0Result diagnostic = service.ReadCoseMac0Diagnostic(
            attached.Bytes,
            diagnosticPolicy);
        CoseMac0Result verified = service.VerifyCoseMac0(
            attached.Bytes,
            externalData,
            key,
            policy);
        CoseMac0Result wrongData = service.VerifyCoseMac0(
            attached.Bytes,
            new byte[] { 9 },
            key,
            policy);
        TokenBytesResult rewritten = service.WriteCoseMac0(
            diagnostic.Message,
            false,
            policy);
        TokenBytesResult detached = service.CreateDetachedCoseMac0(
            payload,
            externalData,
            Header(policy, CoseAlgorithms.HmacSha256, true, 0),
            key,
            false,
            policy);
        CoseMac0Result detachedVerified =
            service.VerifyDetachedCoseMac0(
                detached.Bytes,
                payload,
                externalData,
                key,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(parsed.Message.PayloadAvailable, Is.False);
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error.Code,
                Is.EqualTo("unauthenticated_decode_not_allowed"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Message.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Message.PayloadAvailable, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.PayloadAvailable, Is.True);
            Assert.That(verified.Message.TagVerified, Is.True);
            Assert.That(wrongData.Success, Is.False);
            Assert.That(wrongData.Error.Code, Is.EqualTo("mac_mismatch"));
            Assert.That(rewritten.Success, Is.True);
            Assert.That(rewritten.Bytes[0], Is.EqualTo(0x84));
            Assert.That(detached.Success, Is.True);
            Assert.That(detachedVerified.Success, Is.True);
            Assert.That(detachedVerified.Message.Payload,
                Is.EqualTo(payload));
            Assert.That(detachedVerified.Message.PayloadAvailable, Is.True);
        });
    }

    [Test]
    public void MacSupportsMultipleRecipientsAttachedAndDetachedPayloads()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        byte[] payload = Encoding.UTF8.GetBytes("recipient mac payload");
        byte[] externalData = Encoding.UTF8.GetBytes("recipient mac aad");
        TokenKey first = Symmetric(
            Sequence(16, 11),
            CoseAlgorithms.Aes128KeyWrap,
            TokenKeyUses.Encryption);
        TokenKey second = Symmetric(
            Sequence(16, 41),
            CoseAlgorithms.Aes128KeyWrap,
            TokenKeyUses.Encryption);
        CoseHeader[] recipientHeaders = new CoseHeader[]
        {
            Header(policy, CoseAlgorithms.Aes128KeyWrap, false, 1),
            Header(policy, CoseAlgorithms.Aes128KeyWrap, false, 2)
        };
        TokenKey[] keys = new TokenKey[] { first, second };

        TokenBytesResult attached = service.CreateCoseMac(
            payload,
            externalData,
            Header(policy, CoseAlgorithms.HmacSha256, true, 0),
            recipientHeaders,
            keys,
            null,
            true,
            policy);
        CoseMacResult parsed = service.ReadCoseMac(
            attached.Bytes,
            policy);
        CoseMacResult selected = service.VerifyCoseMacRecipient(
            attached.Bytes,
            externalData,
            1,
            second,
            null,
            policy);
        CoseMacResult all = service.VerifyCoseMacAll(
            attached.Bytes,
            externalData,
            keys,
            null,
            policy);
        TokenPolicy diagnosticPolicy =
            Policy("HMAC 256/256", "A128KW");
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        CoseMacResult diagnostic = service.ReadCoseMacDiagnostic(
            attached.Bytes,
            diagnosticPolicy);
        TokenBytesResult rewritten = service.WriteCoseMac(
            diagnostic.Message,
            false,
            policy);
        TokenBytesResult detached = service.CreateDetachedCoseMac(
            payload,
            externalData,
            Header(policy, CoseAlgorithms.HmacSha256, true, 0),
            recipientHeaders,
            keys,
            null,
            false,
            policy);
        CoseMacResult detachedAll = service.VerifyDetachedCoseMacAll(
            detached.Bytes,
            payload,
            externalData,
            keys,
            null,
            policy);
        CoseMacResult detachedSelected =
            service.VerifyDetachedCoseMacRecipient(
                detached.Bytes,
                payload,
                externalData,
                0,
                first,
                null,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(parsed.Message.PayloadAvailable, Is.False);
            Assert.That(parsed.Message.Recipients, Has.Length.EqualTo(2));
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Message.Payload, Is.EqualTo(payload));
            Assert.That(selected.Message.PayloadAvailable, Is.True);
            Assert.That(selected.Message.Recipients[0].Decrypted, Is.False);
            Assert.That(selected.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(all.Success, Is.True);
            Assert.That(all.Message.Recipients[0].Decrypted, Is.True);
            Assert.That(all.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Message.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Message.PayloadAvailable, Is.True);
            Assert.That(rewritten.Success, Is.True);
            Assert.That(rewritten.Bytes[0], Is.EqualTo(0x85));
            Assert.That(detached.Success, Is.True);
            Assert.That(detachedAll.Success, Is.True);
            Assert.That(detachedAll.Message.Payload, Is.EqualTo(payload));
            Assert.That(detachedAll.Message.PayloadAvailable, Is.True);
            Assert.That(detachedSelected.Success, Is.True);
            Assert.That(detachedSelected.Message.Payload,
                Is.EqualTo(payload));
        });
    }

    [Test]
    public void EncryptSupportsEphemeralDirectEcdhRecipients()
    {
        ICoseEncryptionOperations service = new CoseEncryptionService();
        TokenPolicy policy = Policy(
            "A128GCM",
            "ECDH-ES + HKDF-256");
        byte[] plaintext = Encoding.UTF8.GetBytes("ecdh recipient");
        using ECDsa recipientPrivateNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = ECDsa.Create(
            recipientPrivateNative.ExportParameters(false));
        TokenKey recipientPrivate = EcKey(
            recipientPrivateNative,
            true);
        TokenKey recipientPublic = EcKey(
            recipientPublicNative,
            false);

        TokenBytesResult encrypted = service.EncryptCoseEncrypt(
            plaintext,
            System.Array.Empty<byte>(),
            Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
            new CoseHeader[]
            {
                Header(
                    policy,
                    CoseAlgorithms.EcdhEsHkdfSha256,
                    false,
                    1)
            },
            new TokenKey[] { recipientPublic },
            null,
            true,
            policy);
        CoseEncryptResult decrypted =
            service.DecryptCoseEncryptRecipient(
                encrypted.Bytes,
                System.Array.Empty<byte>(),
                0,
                recipientPrivate,
                null,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Message.Recipients[0].EncryptedKey,
                Is.Empty);
            Assert.That(decrypted.Message.Recipients[0].Decrypted, Is.True);
        });
    }

    static CoseHeader Header(
        TokenPolicy policy,
        int algorithm,
        bool protectedAlgorithm,
        byte keyId)
    {
        CoseHeader header = new CoseHeader();
        CborMapEntry algorithmEntry = Entry(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        if (protectedAlgorithm)
        {
            header.ProtectedParameters =
                new CborMapEntry[] { algorithmEntry };
        }
        else
        {
            header.UnprotectedParameters = keyId == 0
                ? new CborMapEntry[] { algorithmEntry }
                : new CborMapEntry[]
                {
                    algorithmEntry,
                    Entry(
                        CoseHeaderLabels.KeyId,
                        Bytes(new byte[] { keyId }))
                };
        }

        TokenBytesResult prepared = new CoseTokenService()
            .WriteCoseHeader(header, policy);
        Assert.That(prepared.Success, Is.True);
        return header;
    }

    static TokenKey Symmetric(
        byte[] material,
        int algorithm,
        string use)
    {
        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseSymmetricKey(material, algorithm);
        Assert.That(imported.Success, Is.True);
        imported.Key.Use = use;
        return imported.Key;
    }

    static TokenKey EcKey(ECDsa native, bool privateMaterial)
    {
        TokenKeyResult imported = new TokenKeyService()
            .ImportEc2Key(native, privateMaterial);
        Assert.That(imported.Success, Is.True);
        imported.Key.Use = TokenKeyUses.Encryption;
        if (privateMaterial)
        {
            imported.Key.KeyOperations =
                new string[] { TokenKeyOperations.DeriveKey };
        }

        return imported.Key;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }
}
