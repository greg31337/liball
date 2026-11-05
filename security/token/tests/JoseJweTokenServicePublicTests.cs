using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using token;

namespace token.Tests;

public class JoseJweTokenServicePublicTests
{
    [Test]
    public void CompactJweSupportsSpanCompressionDiagnosticsAndDecryption()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] selected = Encoding.UTF8.GetBytes(
            "compressed compact plaintext compressed compact plaintext");
        byte[] container = new byte[selected.Length + 2];
        selected.CopyTo(container, 1);
        TokenKey key = Key(16, 10, JoseAlgorithms.Direct);
        JoseHeader header = Header(
            service,
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"DEF\"}");
        TokenPolicy disabled = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult denied = service.EncryptJweCompact(
            selected,
            header,
            key,
            disabled);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.AllowCompression = true;

        TokenTextResult encrypted = service.EncryptJweCompactSpan(
            container.AsSpan(1, selected.Length),
            header,
            key,
            policy);
        JweCompactResult parsed = service.ReadJweCompact(
            encrypted.Text!,
            policy);
        JweCompactResult deniedDiagnostic =
            service.ReadJweCompactDiagnostic(encrypted.Text!, policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        diagnosticPolicy.AllowCompression = true;
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JweCompactResult diagnostic = service.ReadJweCompactDiagnostic(
            encrypted.Text!,
            diagnosticPolicy);
        JweCompactResult decrypted = service.DecryptJweCompact(
            encrypted.Text!,
            key,
            policy);
        TokenPolicy limitedPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        limitedPolicy.AllowCompression = true;
        limitedPolicy.MaximumDecompressedSizeInBytes = 10;
        limitedPolicy.MaximumCompressionRatio = 1000;
        JweCompactResult expansionRejected = service.DecryptJweCompact(
            encrypted.Text!,
            key,
            limitedPolicy);
        TokenKey wrongKey = Key(16, 80, JoseAlgorithms.Direct);
        JweCompactResult rejected = service.DecryptJweCompact(
            encrypted.Text!,
            wrongKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(denied.Error, "compression_disabled");
            Assert.That(encrypted.Success, Is.True);
            Assert.That(parsed.Token!.CiphertextAvailable, Is.False);
            Assert.That(parsed.Token.Ciphertext, Is.Null);
            Assert.That(parsed.Token.CompressionAlgorithm, Is.EqualTo("DEF"));
            AssertFailure(
                deniedDiagnostic.Error,
                "unauthenticated_decode_not_allowed");
            Assert.That(diagnostic.Token!.CiphertextAvailable, Is.True);
            Assert.That(diagnostic.Token.Ciphertext, Is.Not.Empty);
            Assert.That(diagnostic.Token.AuthenticationTagVerified, Is.False);
            Assert.That(decrypted.Token!.Plaintext, Is.EqualTo(selected));
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
            AssertFailure(
                expansionRejected.Error,
                "decompression_limit_exceeded");
            AssertFailure(rejected.Error, "decryption_failed");
        });
    }

    [Test]
    public void FlattenedJweAuthenticatesExternalDataAndSupportsComponentWriting()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] plaintext = Encoding.UTF8.GetBytes("flattened plaintext");
        byte[] aad = Encoding.UTF8.GetBytes("external authenticated data");
        TokenKey key = Key(16, 20, JoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader protectedHeader = Header(
            service,
            "{\"enc\":\"A128GCM\"}");
        JoseHeader sharedHeader = Header(
            service,
            "{\"typ\":\"application/example\"}");
        JoseHeader recipientHeader = Header(
            service,
            "{\"alg\":\"A128KW\",\"kid\":\"recipient-one\"}");

        TokenTextResult encrypted =
            service.EncryptJweFlattenedWithAuthenticatedData(
                plaintext,
                aad,
                protectedHeader,
                sharedHeader,
                recipientHeader,
                key,
                policy);
        JweFlattenedResult parsed = service.ReadJweFlattened(
            encrypted.Text!,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JweFlattenedResult diagnostic =
            service.ReadJweFlattenedDiagnostic(
                encrypted.Text!,
                diagnosticPolicy);
        JweFlattenedResult decrypted = service.DecryptJweFlattened(
            encrypted.Text!,
            key,
            policy);
        TokenTextResult rewritten =
            service.WriteJweFlattenedWithAuthenticatedData(
                diagnostic.Token!.Ciphertext!,
                diagnostic.Token.AdditionalAuthenticatedData!,
                diagnostic.Token.ProtectedHeader!,
                diagnostic.Token.SharedUnprotectedHeader,
                diagnostic.Token.RecipientHeader,
                diagnostic.Token.EncryptedKey!,
                diagnostic.Token.InitializationVector!,
                diagnostic.Token.AuthenticationTag!,
                policy);
        JweFlattenedResult rewrittenDecrypted =
            service.DecryptJweFlattened(rewritten.Text!, key, policy);
        TokenTextResult alteredAad =
            service.WriteJweFlattenedWithAuthenticatedData(
                diagnostic.Token.Ciphertext!,
                Encoding.UTF8.GetBytes("altered authenticated data"),
                diagnostic.Token.ProtectedHeader!,
                diagnostic.Token.SharedUnprotectedHeader,
                diagnostic.Token.RecipientHeader,
                diagnostic.Token.EncryptedKey!,
                diagnostic.Token.InitializationVector!,
                diagnostic.Token.AuthenticationTag!,
                policy);
        JweFlattenedResult alteredRejected =
            service.DecryptJweFlattened(alteredAad.Text!, key, policy);

        TokenTextResult simple = service.EncryptJweFlattened(
            plaintext,
            protectedHeader,
            null,
            recipientHeader,
            key,
            policy);
        JweFlattenedResult simpleDiagnostic =
            service.ReadJweFlattenedDiagnostic(
                simple.Text!,
                diagnosticPolicy);
        TokenTextResult simpleWritten = service.WriteJweFlattened(
            simpleDiagnostic.Token!.Ciphertext!,
            simpleDiagnostic.Token.ProtectedHeader!,
            simpleDiagnostic.Token.SharedUnprotectedHeader,
            simpleDiagnostic.Token.RecipientHeader,
            simpleDiagnostic.Token.EncryptedKey!,
            simpleDiagnostic.Token.InitializationVector!,
            simpleDiagnostic.Token.AuthenticationTag!,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Token!.CiphertextAvailable, Is.False);
            Assert.That(parsed.Token.AdditionalAuthenticatedData, Is.EqualTo(aad));
            Assert.That(parsed.Token.KeyId, Is.EqualTo("recipient-one"));
            Assert.That(decrypted.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(rewrittenDecrypted.Token!.Plaintext, Is.EqualTo(plaintext));
            AssertFailure(alteredRejected.Error, "decryption_failed");
            Assert.That(simpleWritten.Success, Is.True);
        });
    }

    [Test]
    public void GeneralJweSupportsMultipleRecipientsSelectionAndAllRecipientDecryption()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] plaintext = Encoding.UTF8.GetBytes("shared recipient plaintext");
        byte[] aad = Encoding.UTF8.GetBytes("shared external aad");
        TokenKey firstKey = Key(16, 30, JoseAlgorithms.Aes128KeyWrap);
        TokenKey secondKey = Key(16, 60, JoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader protectedHeader = Header(
            service,
            "{\"enc\":\"A128GCM\"}");
        JoseHeader firstRecipient = Header(
            service,
            "{\"alg\":\"A128KW\",\"kid\":\"first\"}");
        JoseHeader secondRecipient = Header(
            service,
            "{\"alg\":\"A128KW\",\"kid\":\"second\"}");
        JoseHeader?[] recipients = new JoseHeader?[]
        {
            firstRecipient,
            secondRecipient
        };
        TokenKey[] keys = new TokenKey[] { firstKey, secondKey };

        TokenTextResult encrypted =
            service.EncryptJweGeneralWithAuthenticatedData(
                plaintext,
                aad,
                protectedHeader,
                null,
                recipients,
                keys,
                policy);
        JweGeneralResult parsed = service.ReadJweGeneral(
            encrypted.Text!,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JweGeneralResult diagnostic = service.ReadJweGeneralDiagnostic(
            encrypted.Text!,
            diagnosticPolicy);
        JweGeneralResult selected = service.DecryptJweGeneralRecipient(
            encrypted.Text!,
            1,
            secondKey,
            policy);
        JweGeneralResult all = service.DecryptJweGeneralAll(
            encrypted.Text!,
            keys,
            policy);
        JweGeneralResult invalidIndex =
            service.DecryptJweGeneralRecipient(
                encrypted.Text!,
                2,
                secondKey,
                policy);
        byte[][] encryptedKeys = new byte[][]
        {
            diagnostic.Token!.Recipients[0].EncryptedKey!,
            diagnostic.Token.Recipients[1].EncryptedKey!
        };
        TokenTextResult rewritten =
            service.WriteJweGeneralWithAuthenticatedData(
                diagnostic.Token.Ciphertext!,
                diagnostic.Token.AdditionalAuthenticatedData!,
                diagnostic.Token.ProtectedHeader!,
                diagnostic.Token.SharedUnprotectedHeader,
                recipients,
                encryptedKeys,
                diagnostic.Token.InitializationVector!,
                diagnostic.Token.AuthenticationTag!,
                policy);
        JweGeneralResult rewrittenAll = service.DecryptJweGeneralAll(
            rewritten.Text!,
            keys,
            policy);

        TokenTextResult simple = service.EncryptJweGeneral(
            plaintext,
            protectedHeader,
            null,
            recipients,
            keys,
            policy);
        JweGeneralResult simpleDiagnostic = service.ReadJweGeneralDiagnostic(
            simple.Text!,
            diagnosticPolicy);
        byte[][] simpleEncryptedKeys = new byte[][]
        {
            simpleDiagnostic.Token!.Recipients[0].EncryptedKey!,
            simpleDiagnostic.Token.Recipients[1].EncryptedKey!
        };
        TokenTextResult simpleWritten = service.WriteJweGeneral(
            simpleDiagnostic.Token.Ciphertext!,
            simpleDiagnostic.Token.ProtectedHeader!,
            simpleDiagnostic.Token.SharedUnprotectedHeader,
            recipients,
            simpleEncryptedKeys,
            simpleDiagnostic.Token.InitializationVector!,
            simpleDiagnostic.Token.AuthenticationTag!,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Token!.Recipients, Has.Length.EqualTo(2));
            Assert.That(parsed.Token.CiphertextAvailable, Is.False);
            Assert.That(diagnostic.Token.CiphertextAvailable, Is.True);
            Assert.That(selected.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(selected.Token.Recipients[0].Decrypted, Is.False);
            Assert.That(selected.Token.Recipients[1].Decrypted, Is.True);
            Assert.That(all.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(all.Token.Recipients[0].Decrypted, Is.True);
            Assert.That(all.Token.Recipients[1].Decrypted, Is.True);
            AssertFailure(invalidIndex.Error, "recipient_index_out_of_range");
            Assert.That(rewrittenAll.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(simpleWritten.Success, Is.True);
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    [TestCase(JoseAlgorithms.Aes192CbcHmacSha384, 48)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void CompactJweRoundTripsEveryContentEncryptionAlgorithm(
        string contentAlgorithm,
        int keyLength)
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "public content-encryption round trip");
        TokenKey key = Key(keyLength, 110, JoseAlgorithms.Direct);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            contentAlgorithm);
        JoseHeader header = Header(
            service,
            "{\"alg\":\"dir\",\"enc\":\"" + contentAlgorithm + "\"}");

        TokenTextResult encrypted = service.EncryptJweCompact(
            plaintext,
            header,
            key,
            policy);
        JweCompactResult decrypted = service.DecryptJweCompact(
            encrypted.Text!,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.Text!.Split('.'), Has.Length.EqualTo(5));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
            Assert.That(
                decrypted.Token.EncryptionAlgorithm,
                Is.EqualTo(contentAlgorithm));
        });
    }

    [Test]
    public void CompactJweRoundTripsEveryKeyManagementFamily()
    {
        IJoseTokenOperations service = new JoseTokenService();
        ITokenKeyService keyService = new TokenKeyService();
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "public key-management round trip");

        TokenKey aesWrapKey = Key(
            16,
            120,
            JoseAlgorithms.Aes128KeyWrap);
        TokenPolicy aesWrapPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128CbcHmacSha256);
        TokenTextResult aesWrapped = service.EncryptJweCompact(
            plaintext,
            Header(
                service,
                "{\"alg\":\"A128KW\",\"enc\":\"A128CBC-HS256\"}"),
            aesWrapKey,
            aesWrapPolicy);
        JweCompactResult aesUnwrapped = service.DecryptJweCompact(
            aesWrapped.Text!,
            aesWrapKey,
            aesWrapPolicy);

        TokenKey aesGcmWrapKey = Key(
            24,
            130,
            JoseAlgorithms.Aes192GcmKeyWrap);
        TokenPolicy aesGcmWrapPolicy = Policy(
            JoseAlgorithms.Aes192GcmKeyWrap,
            JoseAlgorithms.Aes192Gcm);
        TokenTextResult aesGcmWrapped = service.EncryptJweCompact(
            plaintext,
            Header(
                service,
                "{\"alg\":\"A192GCMKW\",\"enc\":\"A192GCM\"}"),
            aesGcmWrapKey,
            aesGcmWrapPolicy);
        JweCompactResult aesGcmUnwrapped = service.DecryptJweCompact(
            aesGcmWrapped.Text!,
            aesGcmWrapKey,
            aesGcmWrapPolicy);

        using RSA rsa = RSA.Create(2048);
        TokenKey rsaPublic = keyService.ImportJoseRsaKey(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            false).Key!;
        TokenKey rsaPrivate = keyService.ImportJoseRsaKey(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key!;
        TokenPolicy rsaPolicy = Policy(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        TokenTextResult rsaEncrypted = service.EncryptJweCompact(
            plaintext,
            Header(
                service,
                "{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\"}"),
            rsaPublic,
            rsaPolicy);
        JweCompactResult rsaDecrypted = service.DecryptJweCompact(
            rsaEncrypted.Text!,
            rsaPrivate,
            rsaPolicy);

        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ecPublic = keyService.ImportJoseEc2Key(
            ec,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            false).Key!;
        TokenKey ecPrivate = keyService.ImportJoseEc2Key(
            ec,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            true).Key!;
        TokenPolicy ecPolicy = Policy(
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult ecEncrypted = service.EncryptJweCompact(
            plaintext,
            Header(
                service,
                "{\"alg\":\"ECDH-ES+A128KW\",\"enc\":\"A128GCM\"}"),
            ecPublic,
            ecPolicy);
        JweCompactResult ecDecrypted = service.DecryptJweCompact(
            ecEncrypted.Text!,
            ecPrivate,
            ecPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(aesWrapped.Success, Is.True);
            Assert.That(aesUnwrapped.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(aesGcmWrapped.Success, Is.True);
            Assert.That(aesGcmUnwrapped.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(rsaEncrypted.Success, Is.True);
            Assert.That(rsaDecrypted.Token!.Plaintext, Is.EqualTo(plaintext));
            Assert.That(ecEncrypted.Success, Is.True);
            Assert.That(ecDecrypted.Token!.Plaintext, Is.EqualTo(plaintext));
        });
    }

    [TestCase(JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap)]
    [TestCase(JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap)]
    [TestCase(JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap)]
    public void Pbes2TaggedPasswordKeysImportAndRoundTrip(string algorithm)
    {
        IJoseTokenOperations service = new JoseTokenService();
        ITokenKeyService keyService = new TokenKeyService();
        TokenKeyResult imported = keyService.ImportJoseSymmetricKey(
            Bytes(24, 140),
            algorithm);
        Assert.That(imported.Success, Is.True, imported.Error?.Code);
        TokenPolicy policy = Policy(algorithm, JoseAlgorithms.Aes128Gcm);
        JoseHeader header = Header(
            service,
            "{\"alg\":\"" + algorithm + "\",\"enc\":\"A128GCM\"}");
        byte[] plaintext = Encoding.UTF8.GetBytes("tagged PBES2 password");

        TokenTextResult encrypted = service.EncryptJweCompact(
            plaintext,
            header,
            imported.Key!,
            policy);
        JweCompactResult decrypted = service.DecryptJweCompact(
            encrypted.Text!,
            imported.Key!,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Key!.Algorithm, Is.EqualTo(algorithm));
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token!.Plaintext, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void JweHeadersMalformedInputsAndResourceLimitsFailClosed()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenKey key = Key(16, 150, JoseAlgorithms.Direct);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader protectedHeader = Header(
            service,
            "{\"enc\":\"A128GCM\",\"kid\":\"protected\"}");
        JoseHeader recipientHeader = Header(service, "{\"alg\":\"dir\"}");
        byte[] plaintext = Encoding.UTF8.GetBytes("bounded plaintext");

        TokenTextResult overlap = service.EncryptJweFlattened(
            plaintext,
            protectedHeader,
            Header(service, "{\"kid\":\"shared\"}"),
            recipientHeader,
            key,
            policy);
        TokenTextResult misplacedCritical = service.EncryptJweFlattened(
            plaintext,
            Header(service, "{\"enc\":\"A128GCM\"}"),
            Header(service, "{\"crit\":[\"custom\"],\"custom\":true}"),
            recipientHeader,
            key,
            policy);
        JoseHeader criticalHeader = Header(
            service,
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"crit\":[\"custom\"],\"custom\":true}");
        TokenTextResult unknownCritical = service.EncryptJweCompact(
            plaintext,
            criticalHeader,
            key,
            policy);
        JweCompactResult malformedCompact = service.ReadJweCompact(
            "only.two",
            policy);
        JweFlattenedResult malformedFlattened = service.ReadJweFlattened(
            "{\"protected\":1}",
            policy);
        JweGeneralResult malformedGeneral = service.ReadJweGeneral(
            "{\"recipients\":[]}",
            policy);
        TokenTextResult encrypted = service.EncryptJweCompact(
            plaintext,
            Header(
                service,
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            key,
            policy);
        TokenPolicy limited = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        limited.MaximumTokenSizeInBytes = encrypted.Text!.Length - 1;
        JweCompactResult tooLarge = service.ReadJweCompact(
            encrypted.Text,
            limited);

        Assert.Multiple(() =>
        {
            AssertFailure(overlap.Error, "duplicate_header_parameter");
            AssertFailure(
                misplacedCritical.Error,
                "critical_header_not_protected");
            AssertFailure(unknownCritical.Error, "unknown_critical_header");
            AssertFailure(malformedCompact.Error, "invalid_compact_token");
            AssertFailure(
                malformedFlattened.Error,
                "invalid_protected_header");
            AssertFailure(malformedGeneral.Error, "invalid_recipients");
            AssertFailure(tooLarge.Error, "token_too_large");
        });
    }

    [Test]
    public void GeneralJweRejectsAuthenticatedTamperingWithoutPlaintext()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] plaintext = Encoding.UTF8.GetBytes("authenticated plaintext");
        TokenKey firstKey = Key(16, 160, JoseAlgorithms.Aes128KeyWrap);
        TokenKey secondKey = Key(16, 180, JoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader protectedHeader = Header(
            service,
            "{\"enc\":\"A128GCM\"}");
        JoseHeader?[] recipients = new JoseHeader?[]
        {
            Header(service, "{\"alg\":\"A128KW\",\"kid\":\"first\"}"),
            Header(service, "{\"alg\":\"A128KW\",\"kid\":\"second\"}")
        };
        TokenKey[] keys = new TokenKey[] { firstKey, secondKey };
        TokenTextResult encrypted = service.EncryptJweGeneral(
            plaintext,
            protectedHeader,
            null,
            recipients,
            keys,
            policy);
        System.Text.Json.Nodes.JsonObject tamperedObject =
            JsonNode.Parse(encrypted.Text!)!.AsObject();
        string tag = tamperedObject["tag"]!.GetValue<string>();
        tamperedObject["tag"] = (tag[0] == 'A' ? "B" : "A") + tag[1..];
        string tampered = tamperedObject.ToJsonString();

        JweGeneralResult selected = service.DecryptJweGeneralRecipient(
            tampered,
            0,
            firstKey,
            policy);
        JweGeneralResult all = service.DecryptJweGeneralAll(
            tampered,
            keys,
            policy);
        JweGeneralResult countMismatch = service.DecryptJweGeneralAll(
            encrypted.Text!,
            new TokenKey[] { firstKey },
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(selected.Error, "decryption_failed");
            Assert.That(selected.Token, Is.Null);
            AssertFailure(all.Error, "decryption_failed");
            Assert.That(all.Token, Is.Null);
            AssertFailure(countMismatch.Error, "recipient_count_mismatch");
        });
    }

    [Test]
    public void AuthenticatedJsonJweFormsCanDirectNestedParsing()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKey signingKey = Key(32, 200, JoseAlgorithms.HmacSha256);
        TokenTextResult inner = service.SignJwsCompact(
            Encoding.UTF8.GetBytes("nested JSON JWE payload"),
            Header(service, "{\"alg\":\"HS256\"}"),
            signingKey,
            policy);
        byte[] nestedPayload = Encoding.UTF8.GetBytes(inner.Text!);
        TokenKey encryptionKey = Key(16, 220, JoseAlgorithms.Direct);
        JoseHeader protectedHeader = Header(
            service,
            "{\"enc\":\"A128GCM\",\"cty\":\"jose\"}");
        JoseHeader recipientHeader = Header(service, "{\"alg\":\"dir\"}");

        TokenTextResult flattened = service.EncryptJweFlattened(
            nestedPayload,
            protectedHeader,
            null,
            recipientHeader,
            encryptionKey,
            policy);
        JweFlattenedResult flattenedDecrypted = service.DecryptJweFlattened(
            flattened.Text!,
            encryptionKey,
            policy);
        NestedJoseResult flattenedNested = service.ReadNestedJweFlattened(
            flattenedDecrypted,
            policy);

        TokenTextResult general = service.EncryptJweGeneral(
            nestedPayload,
            protectedHeader,
            null,
            new JoseHeader?[] { recipientHeader },
            new TokenKey[] { encryptionKey },
            policy);
        JweGeneralResult generalDecrypted = service.DecryptJweGeneralRecipient(
            general.Text!,
            0,
            encryptionKey,
            policy);
        NestedJoseResult generalNested = service.ReadNestedJweGeneral(
            generalDecrypted,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattenedNested.Success, Is.True);
            Assert.That(
                flattenedNested.Token!.Kind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(flattenedNested.Token.CompactJws, Is.Not.Null);
            Assert.That(generalNested.Success, Is.True);
            Assert.That(
                generalNested.Token!.Kind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(generalNested.Token.CompactJws, Is.Not.Null);
        });
    }

    [Test]
    public void AuthenticatedCompactJweCanDirectNestedParsing()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKey signingKey = Key(32, 40, JoseAlgorithms.HmacSha256);
        JoseHeader signingHeader = Header(
            service,
            "{\"alg\":\"HS256\"}");
        TokenTextResult inner = service.SignJwsCompact(
            Encoding.UTF8.GetBytes("nested payload"),
            signingHeader,
            signingKey,
            policy);
        TokenKey encryptionKey = Key(16, 70, JoseAlgorithms.Direct);
        JoseHeader encryptionHeader = Header(
            service,
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\"jose\"}");
        TokenTextResult outer = service.EncryptJweCompact(
            Encoding.UTF8.GetBytes(inner.Text!),
            encryptionHeader,
            encryptionKey,
            policy);
        JweCompactResult decrypted = service.DecryptJweCompact(
            outer.Text!,
            encryptionKey,
            policy);

        NestedJoseResult nested = service.ReadNestedJweCompact(
            decrypted,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(nested.Success, Is.True);
            Assert.That(nested.Token!.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(nested.Token.CompactJws, Is.Not.Null);
            Assert.That(nested.Token.CompactJws!.PayloadAvailable, Is.False);
        });
    }

    [Test]
    public void NullReferencesRemainProgrammerErrors()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKey key = Key(16, 1, JoseAlgorithms.Direct);
        JoseHeader header = Header(
            service,
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => service.EncryptJweCompact(
                    null!,
                    header,
                    key,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.DecryptJweCompact("a.b.c.d.e", null!, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadJweGeneral("{}", null!),
                Throws.ArgumentNullException);
        });
    }

    static JoseHeader Header(IJoseTokenOperations service, string json)
    {
        JoseHeaderResult result = service.ReadJoseHeader(json, 16);
        Assert.That(result.Success, Is.True);
        return result.Header!;
    }

    static TokenKey Key(int length, byte start, string algorithm)
    {
        byte[] bytes = Bytes(length, start);

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes;
        key.Algorithm = algorithm;
        return key;
    }

    static byte[] Bytes(int length, byte start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index++;
        }

        return bytes;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static void AssertFailure(TokenError? error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.Code, Is.EqualTo(code));
    }
}
