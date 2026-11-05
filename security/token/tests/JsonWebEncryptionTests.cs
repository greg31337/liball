using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JsonWebEncryptionTests
{
    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    [TestCase(JoseAlgorithms.Aes192CbcHmacSha384, 48)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void DirectEncryptionRoundTripsAllContentAlgorithms(
        string encryptionAlgorithm,
        int keySize)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("compact JWE payload \u2603");
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(keySize, 1)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            encryptionAlgorithm);

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"dir\",\"enc\":\""
                + encryptionAlgorithm + "\"}"),
            key,
            policy);
        JweCompactResult parsed = JsonWebEncryption.Parse(encrypted.Text);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            policy);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            key.SymmetricKey,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.Text.Split('.'), Has.Length.EqualTo(5));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.EncryptedKey, Is.Empty);
            Assert.That(parsed.Token.CiphertextAvailable, Is.False);
            Assert.That(diagnostic.Token.CiphertextAvailable, Is.True);
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.PlaintextAvailable, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(authenticated.Token.AuthenticationTagVerified, Is.True);
            Assert.That(authenticated.Token.CiphertextAvailable, Is.False);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void RsaOaepEncryptionProducesDecryptableContentKey()
    {
        byte[] plaintext = Sequence(37, 20);
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaOaepSha256,
            false).Key;
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            privateRsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\"}"),
            publicKey,
            policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            privateKey,
            policy);
        RsaKeyManagementResult managed = RsaKeyManagement.Decrypt(
            diagnostic.Token.EncryptedKey,
            privateKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            managed.ContentEncryptionKey,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(diagnostic.Token.EncryptedKey, Has.Length.EqualTo(256));
            Assert.That(managed.Success, Is.True);
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void AesKeyWrapEncryptionProducesDecryptableContentKey()
    {
        byte[] plaintext = Sequence(41, 30);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 2)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128CbcHmacSha256);

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"A128KW\",\"enc\":\"A128CBC-HS256\"}"),
            key,
            policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            policy);
        TokenBytesResult contentKey = AesKeyWrap.Unwrap(
            diagnostic.Token.EncryptedKey,
            key,
            JoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            contentKey.Bytes,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(contentKey.Success, Is.True);
            Assert.That(contentKey.Bytes, Has.Length.EqualTo(32));
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void AesGcmKeyWrapAddsAuthenticatedProtectedParameters()
    {
        byte[] plaintext = Sequence(29, 40);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(24, 3)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes192GcmKeyWrap,
            JoseAlgorithms.Aes192Gcm);
        JoseHeader header = Header(
            "{\"alg\":\"A192GCMKW\",\"enc\":\"A192GCM\"}");

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            header,
            key,
            policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            policy);
        byte[] keyIv = HeaderBytes(
            diagnostic.Token.ProtectedHeader,
            JoseHeaderNames.InitializationVector);
        byte[] keyTag = HeaderBytes(
            diagnostic.Token.ProtectedHeader,
            JoseHeaderNames.AuthenticationTag);
        TokenBytesResult contentKey = AesGcmKeyWrap.Unwrap(
            diagnostic.Token.EncryptedKey,
            keyIv,
            keyTag,
            key,
            JoseAlgorithms.Aes192GcmKeyWrap);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            contentKey.Bytes,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(keyIv, Has.Length.EqualTo(12));
            Assert.That(keyTag, Has.Length.EqualTo(16));
            Assert.That(header.Get(JoseHeaderNames.InitializationVector), Is.Null);
            Assert.That(header.Get(JoseHeaderNames.AuthenticationTag), Is.Null);
            Assert.That(contentKey.Success, Is.True);
            Assert.That(contentKey.Bytes, Has.Length.EqualTo(24));
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void Pbes2EncryptionAddsSaltAndIterationHeaders()
    {
        byte[] plaintext = Sequence(31, 50);
        TokenKey password = SymmetricKeyImporterOperations.Import(Sequence(24, 4)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"PBES2-HS256+A128KW\",\"enc\":\"A128GCM\"}"),
            password,
            policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            password,
            policy);
        byte[] salt = HeaderBytes(
            diagnostic.Token.ProtectedHeader,
            JoseHeaderNames.Pbes2SaltInput);
        int count = int.Parse(diagnostic.Token.ProtectedHeader.Get(
            JoseHeaderNames.Pbes2Count).NumberText);
        TokenBytesResult contentKey = Pbes2AlgorithmsOperations.Unwrap(
            diagnostic.Token.EncryptedKey,
            password.SymmetricKey,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            salt,
            count);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            contentKey.Bytes,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(salt, Has.Length.EqualTo(Pbes2Algorithms.GeneratedSaltInputSize));
            Assert.That(count, Is.EqualTo(Pbes2Algorithms.DefaultIterationCount));
            Assert.That(contentKey.Success, Is.True);
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(JoseAlgorithms.EcdhEs, 0)]
    [TestCase(JoseAlgorithms.EcdhEsAes128KeyWrap, 24)]
    public void EcdhEncryptionAddsPublicEphemeralKeyAndRoundTrips(
        string algorithm,
        int encryptedKeySize)
    {
        byte[] plaintext = Sequence(33, 60);
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublic = ECDsa.Create(
            recipient.ExportParameters(false));
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient, algorithm, true).Key;
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            recipientPublic, algorithm, false).Key;
        byte[] partyU = Encoding.UTF8.GetBytes("sender");
        byte[] partyV = Encoding.UTF8.GetBytes("recipient");
        JoseHeader header = Header(
            "{\"alg\":\"" + algorithm + "\",\"enc\":\"A128GCM\","
            + "\"apu\":\"" + Base64Url.Encode(partyU).Text + "\","
            + "\"apv\":\"" + Base64Url.Encode(partyV).Text + "\"}");
        TokenPolicy policy = Policy(algorithm, JoseAlgorithms.Aes128Gcm);

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext, header, publicKey, policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            privateKey,
            policy);
        JsonValue ephemeralValue = diagnostic.Token.ProtectedHeader.Get(
            JoseHeaderNames.EphemeralPublicKey);
        string ephemeralJson = JoseHeader.Create(ephemeralValue.ObjectValue).Write().Text;
        JwkResult ephemeral = JsonWebKey.Read(ephemeralJson);
        EcdhKeyManagementResult managed = encryptedKeySize == 0
            ? EcdhAlgorithms.Decrypt(
                privateKey,
                ephemeral.Key,
                algorithm,
                JoseAlgorithms.Aes128Gcm,
                partyU,
                partyV)
            : EcdhAlgorithms.Decrypt(
                diagnostic.Token.EncryptedKey,
                privateKey,
                ephemeral.Key,
                algorithm,
                JoseAlgorithms.Aes128Gcm,
                partyU,
                partyV);
        TokenBytesResult decrypted = DecryptContent(
            diagnostic.Token,
            managed.ContentEncryptionKey,
            encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(ephemeralValue.Kind, Is.EqualTo(TokenJsonValueKind.Object));
            Assert.That(ephemeral.Key.ContainsPrivateMaterial(), Is.False);
            Assert.That(diagnostic.Token.EncryptedKey,
                Has.Length.EqualTo(encryptedKeySize));
            Assert.That(managed.Success, Is.True);
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
        ephemeral.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void CompressionOccursBeforeAuthenticatedEncryption()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(new string('A', 1024));
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 8)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.AllowCompression = true;

        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"DEF\"}"),
            key,
            policy);
        JweCompactResult diagnostic = Diagnostic(encrypted.Text);
        JweCompactResult authenticated = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            policy);
        TokenBytesResult compressed = DecryptContent(
            diagnostic.Token,
            key.SymmetricKey,
            encrypted.Text);
        TokenBytesResult decompressed = JoseCompression.Decompress(
            compressed.Bytes,
            JoseCompressionAlgorithms.Deflate,
            policy);
        TokenPolicy disabled = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenPolicy limited = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        limited.AllowCompression = true;
        limited.MaximumDecompressedSizeInBytes = 100;
        limited.MaximumCompressionRatio = 1000;
        JweCompactResult disabledResult = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            disabled);
        JweCompactResult limitedResult = JsonWebEncryption.Decrypt(
            encrypted.Text,
            key,
            limited);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(compressed.Bytes.Length, Is.LessThan(plaintext.Length));
            Assert.That(decompressed.Success, Is.True);
            Assert.That(decompressed.Bytes, Is.EqualTo(plaintext));
            Assert.That(authenticated.Success, Is.True);
            Assert.That(authenticated.Token.Plaintext, Is.EqualTo(plaintext));
            AssertFailure(disabledResult, "compression_disabled");
            AssertFailure(limitedResult, "decompression_limit_exceeded");
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    public void DecryptRejectsTamperedAuthenticatedComponentsWithoutPlaintext(
        string encryptionAlgorithm,
        int keySize)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("authenticated content");
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(keySize, 11)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            encryptionAlgorithm);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"dir\",\"enc\":\""
                + encryptionAlgorithm + "\",\"kid\":\"a\"}"),
            key,
            policy);
        JoseCompactSerialization serialization = JoseCompact.Parse(
            encrypted.Text).Serialization;
        byte[] changedIv = serialization.GetDecodedSegmentBytes(2);
        byte[] changedCiphertext = serialization.GetDecodedSegmentBytes(3);
        byte[] changedTag = serialization.GetDecodedSegmentBytes(4);
        changedIv[0] ^= 1;
        changedCiphertext[0] ^= 1;
        changedTag[0] ^= 1;
        JoseHeader changedHeader = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        changedHeader.Set(JoseHeaderNames.KeyId, StringValue("b"));

        JweCompactResult headerFailure = JsonWebEncryption.Decrypt(
            Rewrite(encrypted.Text, changedHeader, null, null, null, null),
            key,
            policy);
        JweCompactResult ivFailure = JsonWebEncryption.Decrypt(
            Rewrite(encrypted.Text, null, null, changedIv, null, null),
            key,
            policy);
        JweCompactResult ciphertextFailure = JsonWebEncryption.Decrypt(
            Rewrite(encrypted.Text, null, null, null, changedCiphertext, null),
            key,
            policy);
        JweCompactResult tagFailure = JsonWebEncryption.Decrypt(
            Rewrite(encrypted.Text, null, null, null, null, changedTag),
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(headerFailure, "decryption_failed");
            AssertFailure(ivFailure, "decryption_failed");
            AssertFailure(ciphertextFailure, "decryption_failed");
            AssertFailure(tagFailure, "decryption_failed");
        });
    }

    [Test]
    public void DecryptEnforcesAllowlistKeyMetadataAndNullArguments()
    {
        byte[] plaintext = new byte[] { 1, 2, 3 };
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 12)).Key;
        TokenPolicy allowed = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            key,
            allowed);
        TokenPolicy missingContent = Policy(JoseAlgorithms.Direct);
        TokenPolicy wrongCase = Policy("DIR", JoseAlgorithms.Aes128Gcm);
        TokenKey wrongKey = SymmetricKeyImporterOperations.Import(Sequence(16, 13)).Key;
        TokenKey encryptOnly = SymmetricKeyImporterOperations.Import(Sequence(16, 12)).Key;
        encryptOnly.KeyOperations = new string[] { TokenKeyOperations.Encrypt };

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Decrypt(
                encrypted.Text, key, new TokenPolicy()),
                "algorithm_not_allowed");
            AssertFailure(JsonWebEncryption.Decrypt(
                encrypted.Text, key, missingContent),
                "algorithm_not_allowed");
            AssertFailure(JsonWebEncryption.Decrypt(
                encrypted.Text, key, wrongCase),
                "algorithm_not_allowed");
            AssertFailure(JsonWebEncryption.Decrypt(
                encrypted.Text, wrongKey, allowed),
                "decryption_failed");
            AssertFailure(JsonWebEncryption.Decrypt(
                encrypted.Text, encryptOnly, allowed),
                "key_operation_not_permitted");
            Assert.That(() => JsonWebEncryption.Decrypt(null, key, allowed),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Decrypt(
                encrypted.Text, null, allowed), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Decrypt(
                encrypted.Text, key, null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void DecryptRequiresAesGcmKeyWrapParameters()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 14)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128GcmKeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            new byte[] { 1 },
            Header("{\"alg\":\"A128GCMKW\",\"enc\":\"A128GCM\"}"),
            key,
            policy);
        JoseHeader missingIv = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        missingIv.Remove(JoseHeaderNames.InitializationVector);
        JoseHeader missingTag = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        missingTag.Remove(JoseHeaderNames.AuthenticationTag);
        JoseHeader invalidIv = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        invalidIv.Set(JoseHeaderNames.InitializationVector, StringValue("*"));

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, missingIv, null, null, null, null),
                key,
                policy),
                "missing_key_initialization_vector");
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, missingTag, null, null, null, null),
                key,
                policy),
                "missing_key_authentication_tag");
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, invalidIv, null, null, null, null),
                key,
                policy),
                "invalid_key_initialization_vector");
        });
    }

    [Test]
    public void DecryptRequiresValidPbes2Parameters()
    {
        TokenKey password = SymmetricKeyImporterOperations.Import(Sequence(24, 15)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            new byte[] { 1 },
            Header("{\"alg\":\"PBES2-HS256+A128KW\",\"enc\":\"A128GCM\"}"),
            password,
            policy);
        JoseHeader missingSalt = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        missingSalt.Remove(JoseHeaderNames.Pbes2SaltInput);
        JoseHeader missingCount = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        missingCount.Remove(JoseHeaderNames.Pbes2Count);
        JoseHeader invalidCount = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        invalidCount.Set(JoseHeaderNames.Pbes2Count, NumberValue("1.5"));

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, missingSalt, null, null, null, null),
                password,
                policy),
                "missing_pbes2_salt");
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, missingCount, null, null, null, null),
                password,
                policy),
                "missing_pbes2_count");
            AssertFailure(JsonWebEncryption.Decrypt(
                Rewrite(encrypted.Text, invalidCount, null, null, null, null),
                password,
                policy),
                "invalid_pbes2_count");
        });
    }

    [Test]
    public void DecryptRequiresEcdhEphemeralKey()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            new byte[] { 1 },
            Header("{\"alg\":\"ECDH-ES\",\"enc\":\"A128GCM\"}"),
            key,
            policy);
        JoseHeader header = JsonWebEncryption.Parse(
            encrypted.Text).Token.ProtectedHeader;
        header.Remove(JoseHeaderNames.EphemeralPublicKey);

        JweCompactResult result = JsonWebEncryption.Decrypt(
            Rewrite(encrypted.Text, header, null, null, null, null),
            key,
            policy);

        AssertFailure(result, "missing_ephemeral_key");
    }

    [Test]
    public void ParseReadsProtectedMetadataAndWithholdsCiphertext()
    {
        byte[] encryptedKey = new byte[] { 1, 2, 3 };
        byte[] initializationVector = Sequence(12, 10);
        byte[] ciphertext = Encoding.UTF8.GetBytes("encrypted payload");
        byte[] authenticationTag = Sequence(16, 30);
        string text = Compact(
            "{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\","
            + "\"kid\":\"key-1\",\"typ\":\"JWT\"}",
            encryptedKey,
            initializationVector,
            ciphertext,
            authenticationTag);

        JweCompactResult result = JsonWebEncryption.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Token.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.Token.Algorithm,
                Is.EqualTo(JoseAlgorithms.RsaOaepSha256));
            Assert.That(result.Token.AlgorithmMetadata.Known, Is.True);
            Assert.That(result.Token.AlgorithmAvailable, Is.True);
            Assert.That(result.Token.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes256Gcm));
            Assert.That(result.Token.EncryptionAlgorithmMetadata.Known, Is.True);
            Assert.That(result.Token.EncryptionAlgorithmAvailable, Is.True);
            Assert.That(result.Token.KeyId, Is.EqualTo("key-1"));
            Assert.That(result.Token.ProtectedHeader.Get(JoseHeaderNames.Type)
                .StringValue, Is.EqualTo("JWT"));
            Assert.That(result.Token.EncryptedKey, Is.EqualTo(encryptedKey));
            Assert.That(result.Token.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(result.Token.AuthenticationTag,
                Is.EqualTo(authenticationTag));
            Assert.That(result.Token.CiphertextAvailable, Is.False);
            Assert.That(result.Token.Ciphertext, Is.Null);
            Assert.That(result.Token.PlaintextAvailable, Is.False);
            Assert.That(result.Token.Plaintext, Is.Null);
            Assert.That(result.Token.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void DiagnosticReadRequiresPolicyAndExposesCiphertext()
    {
        byte[] ciphertext = new byte[] { 0, 1, 2, 255 };
        string text = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            ciphertext,
            Sequence(16, 20));
        TokenPolicy deniedPolicy = new TokenPolicy();
        TokenPolicy allowedPolicy = new TokenPolicy();
        allowedPolicy.AllowUnauthenticatedDecode = true;

        JweCompactResult denied = JsonWebEncryption.ReadDiagnostic(
            text, deniedPolicy);
        JweCompactResult allowed = JsonWebEncryption.ReadDiagnostic(
            text, allowedPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(denied, "unauthenticated_decode_not_allowed");
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Token.CiphertextAvailable, Is.True);
            Assert.That(allowed.Token.Ciphertext, Is.EqualTo(ciphertext));
            Assert.That(allowed.Token.PlaintextAvailable, Is.False);
            Assert.That(allowed.Token.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void DiagnosticReadDistinguishesEmptyCiphertextFromWithheldCiphertext()
    {
        string text = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            Array.Empty<byte>(),
            Sequence(16, 20));
        TokenPolicy policy = new TokenPolicy();
        policy.AllowUnauthenticatedDecode = true;

        JweCompactResult parsed = JsonWebEncryption.Parse(text);
        JweCompactResult diagnostic = JsonWebEncryption.ReadDiagnostic(text, policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.CiphertextAvailable, Is.False);
            Assert.That(parsed.Token.Ciphertext, Is.Null);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.CiphertextAvailable, Is.True);
            Assert.That(diagnostic.Token.Ciphertext, Is.Empty);
        });
    }

    [Test]
    public void ParseRetainsUnknownAlgorithmMetadata()
    {
        string text = Compact(
            "{\"alg\":\"urn:example:key-management\","
            + "\"enc\":\"urn:example:content-encryption\"}",
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 });

        JweCompactResult result = JsonWebEncryption.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.AlgorithmMetadata.Known, Is.False);
            Assert.That(result.Token.AlgorithmAvailable, Is.False);
            Assert.That(result.Token.EncryptionAlgorithmMetadata.Known, Is.False);
            Assert.That(result.Token.EncryptionAlgorithmAvailable, Is.False);
        });
    }

    [Test]
    public void ParseReportsCompressionMetadataWithoutDecompressing()
    {
        string knownText = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"DEF\"}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            new byte[] { 2 },
            Sequence(16, 20));
        string unknownText = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"GZIP\"}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            new byte[] { 2 },
            Sequence(16, 20));

        JweCompactResult known = JsonWebEncryption.Parse(knownText);
        JweCompactResult unknown = JsonWebEncryption.Parse(unknownText);

        Assert.Multiple(() =>
        {
            Assert.That(known.Success, Is.True);
            Assert.That(known.Token.CompressionAlgorithm,
                Is.EqualTo(JoseCompressionAlgorithms.Deflate));
            Assert.That(known.Token.CompressionAlgorithmKnown, Is.True);
            Assert.That(known.Token.CompressionAlgorithmAvailable, Is.True);
            Assert.That(unknown.Success, Is.True);
            Assert.That(unknown.Token.CompressionAlgorithm, Is.EqualTo("GZIP"));
            Assert.That(unknown.Token.CompressionAlgorithmKnown, Is.False);
            Assert.That(unknown.Token.CompressionAlgorithmAvailable, Is.False);
        });
    }

    [Test]
    public void ParseAcceptsOnlyCallerUnderstoodCriticalExtensions()
    {
        string text = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"crit\":[\"urn:example:flag\"],\"urn:example:flag\":true}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            new byte[] { 2 },
            Sequence(16, 20));
        TokenPolicy defaultPolicy = new TokenPolicy();
        TokenPolicy understoodPolicy = new TokenPolicy();
        understoodPolicy.UnderstoodCriticalHeaders = new string[]
        {
            "urn:example:flag"
        };

        JweCompactResult unknown = JsonWebEncryption.Parse(text, defaultPolicy);
        JweCompactResult understood = JsonWebEncryption.Parse(
            text, understoodPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(unknown, "unknown_critical_header");
            Assert.That(understood.Success, Is.True);
        });
    }

    [TestCase("{}", "missing_algorithm")]
    [TestCase("{\"alg\":\"\",\"enc\":\"A128GCM\"}", "invalid_algorithm")]
    [TestCase("{\"alg\":\"dir\"}", "missing_encryption_algorithm")]
    [TestCase("{\"alg\":\"dir\",\"enc\":\"\"}", "invalid_encryption_algorithm")]
    [TestCase("{\"alg\":\"HS256\",\"enc\":\"A128GCM\"}", "invalid_jwe_algorithm")]
    [TestCase("{\"alg\":\"dir\",\"enc\":\"A128KW\"}", "invalid_content_encryption_algorithm")]
    [TestCase("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"b64\":true}", "invalid_jwe_header_parameter")]
    public void ParseRejectsInvalidJweHeaderSemantics(string header, string code)
    {
        string text = Compact(
            header,
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 });

        JweCompactResult result = JsonWebEncryption.Parse(text);

        AssertFailure(result, code);
    }

    [Test]
    public void ParseEnforcesEncryptedKeyShapeForKnownAlgorithms()
    {
        string directWithKey = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}",
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 });
        string ecdhWithKey = Compact(
            "{\"alg\":\"ECDH-ES\",\"enc\":\"A128GCM\"}",
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 });
        string wrappingWithoutKey = Compact(
            "{\"alg\":\"A128KW\",\"enc\":\"A128GCM\"}",
            Array.Empty<byte>(),
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 });

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Parse(directWithKey),
                "invalid_encrypted_key");
            AssertFailure(JsonWebEncryption.Parse(ecdhWithKey),
                "invalid_encrypted_key");
            AssertFailure(JsonWebEncryption.Parse(wrappingWithoutKey),
                "missing_encrypted_key");
        });
    }

    [Test]
    public void ParseRejectsWrongShapeAndMissingRequiredSegments()
    {
        string jws = JoseCompact.WriteJws(
            Encoding.UTF8.GetBytes("{\"alg\":\"HS256\"}"),
            new byte[] { 1 },
            new byte[] { 2 }).Text;
        string missingHeader = JoseCompact.WriteJwe(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 }).Text;
        string missingIv = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}",
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 2 },
            new byte[] { 3 });
        string missingTag = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}",
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 },
            Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Parse(jws), "invalid_jwe_compact");
            AssertFailure(JsonWebEncryption.Parse(missingHeader),
                "missing_protected_header");
            AssertFailure(JsonWebEncryption.Parse(missingIv),
                "missing_initialization_vector");
            AssertFailure(JsonWebEncryption.Parse(missingTag),
                "missing_authentication_tag");
            AssertFailure(JsonWebEncryption.Parse("one.two"),
                "invalid_compact_token");
            AssertFailure(JsonWebEncryption.Parse("e30..AA.A+.AA"),
                "invalid_compact_token");
        });
    }

    [Test]
    public void ParseEnforcesDepthSizePolicyAndNullArguments()
    {
        string text = Compact(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"custom\":{}}",
            Array.Empty<byte>(),
            Sequence(12, 1),
            new byte[] { 2 },
            Sequence(16, 20));
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumJsonDepth = 1;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(text) - 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.AllowedAlgorithms = null;

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryption.Parse(text, shallow),
                "json_depth_exceeded");
            AssertFailure(JsonWebEncryption.Parse(text, small), "token_too_large");
            AssertFailure(JsonWebEncryption.Parse(text, invalid), "invalid_policy");
            Assert.That(() => JsonWebEncryption.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Parse(text, null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.ReadDiagnostic(null, small),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.ReadDiagnostic(text, null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void EncryptRequiresExactAlgorithmAllowlistAndLegacyOptIn()
    {
        byte[] plaintext = new byte[] { 1 };
        TokenKey directKey = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        JoseHeader directHeader = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}");
        TokenPolicy denied = new TokenPolicy();
        TokenPolicy missingContent = Policy(JoseAlgorithms.Direct);
        TokenPolicy wrongCase = Policy("DIR", JoseAlgorithms.Aes128Gcm);
        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;
        TokenPolicy legacy = Policy(
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm);
        TokenPolicy legacyAllowed = Policy(
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm);
        legacyAllowed.AllowLegacyRsaPkcs1Encryption = true;
        TokenTextResult compatibility = JsonWebEncryption.Encrypt(
            plaintext,
            Header("{\"alg\":\"RSA1_5\",\"enc\":\"A128GCM\"}"),
            rsaKey,
            legacyAllowed);
        JweCompactResult compatibilityDenied = JsonWebEncryption.Decrypt(
            compatibility.Text,
            rsaKey,
            legacy);
        JweCompactResult compatibilityDecrypted = JsonWebEncryption.Decrypt(
            compatibility.Text,
            rsaKey,
            legacyAllowed);

        Assert.Multiple(() =>
        {
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext, directHeader, directKey, denied),
                "algorithm_not_allowed");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext, directHeader, directKey, missingContent),
                "algorithm_not_allowed");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext, directHeader, directKey, wrongCase),
                "algorithm_not_allowed");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"alg\":\"RSA1_5\",\"enc\":\"A128GCM\"}"),
                rsaKey,
                legacy),
                "compatibility_required");
            Assert.That(compatibility.Success, Is.True);
            AssertFailure(compatibilityDenied, "compatibility_required");
            Assert.That(compatibilityDecrypted.Success, Is.True);
            Assert.That(compatibilityDecrypted.Token.Plaintext,
                Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void EncryptRejectsInvalidHeadersGeneratedConflictsAndCompressionPolicy()
    {
        byte[] plaintext = new byte[] { 1 };
        TokenKey directKey = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey wrapKey = SymmetricKeyImporterOperations.Import(Sequence(16, 2)).Key;
        TokenPolicy directPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenPolicy wrapPolicy = Policy(
            JoseAlgorithms.Aes128GcmKeyWrap,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"enc\":\"A128GCM\"}"),
                directKey,
                directPolicy),
                "missing_algorithm");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"alg\":\"dir\"}"),
                directKey,
                directPolicy),
                "missing_encryption_algorithm");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"alg\":\"HS256\",\"enc\":\"A128GCM\"}"),
                directKey,
                Policy(JoseAlgorithms.HmacSha256, JoseAlgorithms.Aes128Gcm)),
                "invalid_jwe_algorithm");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"DEF\"}"),
                directKey,
                directPolicy),
                "compression_disabled");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext,
                Header("{\"alg\":\"A128GCMKW\",\"enc\":\"A128GCM\",\"iv\":\"AA\"}"),
                wrapKey,
                wrapPolicy),
                "conflicting_header_parameter");
        });
    }

    [Test]
    public void EncryptEnforcesSizeDepthKeyMetadataAndNullArguments()
    {
        byte[] plaintext = Sequence(20, 1);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 2)).Key;
        JoseHeader header = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}");
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenPolicy plaintextLimit = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        plaintextLimit.MaximumTokenSizeInBytes = plaintext.Length - 1;
        TokenPolicy encodedLimit = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        encodedLimit.MaximumTokenSizeInBytes = 80;
        TokenKey decryptOnly = SymmetricKeyImporterOperations.Import(Sequence(16, 2)).Key;
        decryptOnly.KeyOperations = new string[] { TokenKeyOperations.Decrypt };

        Assert.Multiple(() =>
        {
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext, header, key, plaintextLimit), "token_too_large");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                new byte[] { 1 }, header, key, encodedLimit), "token_too_large");
            AssertTextFailure(JsonWebEncryption.Encrypt(
                plaintext, header, decryptOnly, policy),
                "key_operation_not_permitted");
            Assert.That(() => JsonWebEncryption.Encrypt(
                null, header, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Encrypt(
                plaintext, null, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Encrypt(
                plaintext, header, null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryption.Encrypt(
                plaintext, header, key, null), Throws.ArgumentNullException);
        });
    }

    static JweCompactResult Diagnostic(string text)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowUnauthenticatedDecode = true;
        return JsonWebEncryption.ReadDiagnostic(text, policy);
    }

    static TokenBytesResult DecryptContent(
        JweCompactToken token,
        byte[] contentEncryptionKey,
        string compact)
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(contentEncryptionKey).Key;
        key.Algorithm = token.EncryptionAlgorithm;
        key.Use = TokenKeyUses.Encryption;
        key.KeyOperations = new string[] { TokenKeyOperations.Decrypt };
        byte[] additionalAuthenticatedData = Encoding.ASCII.GetBytes(
            compact.Split('.')[0]);
        if (string.Equals(
            token.EncryptionAlgorithm,
            JoseAlgorithms.Aes128Gcm,
            StringComparison.Ordinal)
            || string.Equals(
                token.EncryptionAlgorithm,
                JoseAlgorithms.Aes192Gcm,
                StringComparison.Ordinal)
            || string.Equals(
                token.EncryptionAlgorithm,
                JoseAlgorithms.Aes256Gcm,
                StringComparison.Ordinal))
        {
            return AesGcmContentEncryption.Decrypt(
                token.Ciphertext,
                token.InitializationVector,
                token.AuthenticationTag,
                additionalAuthenticatedData,
                key,
                token.EncryptionAlgorithm);
        }

        return AesCbcHmacContentEncryption.Decrypt(
            token.Ciphertext,
            token.InitializationVector,
            token.AuthenticationTag,
            additionalAuthenticatedData,
            key,
            token.EncryptionAlgorithm);
    }

    static byte[] HeaderBytes(JoseHeader header, string name)
    {
        return Base64Url.Decode(header.Get(name).StringValue).Bytes;
    }

    static string Rewrite(
        string compact,
        JoseHeader protectedHeader,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] ciphertext,
        byte[] authenticationTag)
    {
        JoseCompactSerialization parsed = JoseCompact.Parse(compact).Serialization;
        byte[] headerBytes = protectedHeader == null
            ? parsed.GetDecodedSegmentBytes(0)
            : protectedHeader.WriteUtf8().Bytes;
        return JoseCompact.WriteJwe(
            headerBytes,
            encryptedKey == null
                ? parsed.GetDecodedSegmentBytes(1)
                : encryptedKey,
            initializationVector == null
                ? parsed.GetDecodedSegmentBytes(2)
                : initializationVector,
            ciphertext == null
                ? parsed.GetDecodedSegmentBytes(3)
                : ciphertext,
            authenticationTag == null
                ? parsed.GetDecodedSegmentBytes(4)
                : authenticationTag).Text;
    }

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static JsonValue NumberValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = value;
        return result;
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy result = new TokenPolicy();
        result.AllowedAlgorithms = algorithms;
        return result;
    }

    static string Compact(
        string header,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] ciphertext,
        byte[] authenticationTag)
    {
        return JoseCompact.WriteJwe(
            Encoding.UTF8.GetBytes(header),
            encryptedKey,
            initializationVector,
            ciphertext,
            authenticationTag).Text;
    }

    static byte[] Sequence(int length, int seed)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(seed + index);
            index += 1;
        }

        return result;
    }

    static void AssertFailure(JweCompactResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertTextFailure(TokenTextResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
