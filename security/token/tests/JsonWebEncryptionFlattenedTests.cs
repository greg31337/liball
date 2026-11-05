using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using token;

namespace token.Tests;

public class JsonWebEncryptionFlattenedTests
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
        byte[] plaintext = Encoding.UTF8.GetBytes("flattened JWE payload \u2603");
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(keySize, 1)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            encryptionAlgorithm);
        JoseHeader protectedHeader = Header(
            "{\"enc\":\"" + encryptionAlgorithm + "\",\"typ\":\"JWT\"}");
        JoseHeader sharedHeader = Header("{\"kid\":\"shared-key\"}");
        JoseHeader recipientHeader = Header("{\"alg\":\"dir\"}");

        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            protectedHeader,
            sharedHeader,
            recipientHeader,
            key,
            policy);
        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            encrypted.Text,
            policy);
        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            encrypted.Text,
            key,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(document.RootElement.TryGetProperty(
                "encrypted_key", out _), Is.False);
            Assert.That(document.RootElement.GetProperty("header")
                .GetProperty("alg").GetString(), Is.EqualTo("dir"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.Kind, Is.EqualTo(TokenKind.JweJson));
            Assert.That(parsed.Token.Algorithm, Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(parsed.Token.EncryptionAlgorithm,
                Is.EqualTo(encryptionAlgorithm));
            Assert.That(parsed.Token.KeyId, Is.EqualTo("shared-key"));
            Assert.That(parsed.Token.CiphertextAvailable, Is.False);
            Assert.That(parsed.Token.PlaintextAvailable, Is.False);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.PlaintextAvailable, Is.True);
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
            Assert.That(decrypted.Token.CiphertextAvailable, Is.False);
        });
    }

    [Test]
    public void WriteParseAndDiagnosticReadPreserveComponents()
    {
        byte[] encryptedKey = Sequence(24, 10);
        byte[] initializationVector = Sequence(12, 40);
        byte[] ciphertext = new byte[] { 0, 1, 2, 255 };
        byte[] authenticationTag = Sequence(16, 60);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult written = JsonWebEncryptionFlattened.Write(
            ciphertext,
            Header("{\"enc\":\"A128GCM\"}"),
            Header("{\"typ\":\"JWT\"}"),
            Header("{\"alg\":\"A128KW\",\"kid\":\"recipient-1\"}"),
            encryptedKey,
            initializationVector,
            authenticationTag,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;

        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            written.Text,
            policy);
        JweFlattenedResult diagnostic =
            JsonWebEncryptionFlattened.ReadDiagnostic(
                written.Text,
                diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.EncryptedKey, Is.EqualTo(encryptedKey));
            Assert.That(parsed.Token.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(parsed.Token.AuthenticationTag,
                Is.EqualTo(authenticationTag));
            Assert.That(parsed.Token.Ciphertext, Is.Null);
            Assert.That(parsed.Token.KeyId, Is.EqualTo("recipient-1"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.Ciphertext, Is.EqualTo(ciphertext));
            Assert.That(diagnostic.Token.CiphertextAvailable, Is.True);
            Assert.That(diagnostic.Token.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void WriteIncludesDecodedAdditionalAuthenticatedDataAsBase64Url()
    {
        byte[] externalData = new byte[] { 0, 46, 128, 255 };
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult written = JsonWebEncryptionFlattened.Write(
            new byte[] { 1, 2 },
            externalData,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            Header("{\"alg\":\"A128KW\"}"),
            Sequence(24, 1),
            Sequence(12, 30),
            Sequence(16, 50),
            policy);
        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            written.Text,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.AdditionalAuthenticatedData,
                Is.EqualTo(externalData));
            Assert.That(parsed.Token.AdditionalAuthenticatedDataPresent, Is.True);
        });
    }

    [Test]
    public void DecryptsIndependentlyConstructedAesGcmSerialization()
    {
        byte[] keyBytes = Sequence(16, 90);
        byte[] plaintext = Encoding.UTF8.GetBytes("independent flattened JWE");
        byte[] initializationVector = Sequence(12, 110);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] authenticationTag = new byte[16];
        string protectedJson = "{\"enc\":\"A128GCM\"}";
        string protectedSegment = Base64Url.Encode(
            Encoding.UTF8.GetBytes(protectedJson)).Text;
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                ciphertext,
                authenticationTag,
                Encoding.ASCII.GetBytes(protectedSegment));
        }

        string json = "{\"protected\":\"" + protectedSegment
            + "\",\"header\":{\"alg\":\"dir\"},\"iv\":\""
            + Base64Url.Encode(initializationVector).Text
            + "\",\"ciphertext\":\"" + Base64Url.Encode(ciphertext).Text
            + "\",\"tag\":\"" + Base64Url.Encode(authenticationTag).Text
            + "\"}";
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JweFlattenedResult result = JsonWebEncryptionFlattened.Decrypt(
            json,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(result.Token.AuthenticationTagVerified, Is.True);
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    public void ExternalAdditionalAuthenticatedDataRoundTrips(
        string encryptionAlgorithm,
        int keySize)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("AAD protected plaintext");
        byte[] additionalAuthenticatedData = new byte[]
        {
            0, 1, 2, 46, 128, 255
        };
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(keySize, 11)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            encryptionAlgorithm);

        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            additionalAuthenticatedData,
            Header("{\"alg\":\"dir\",\"enc\":\""
                + encryptionAlgorithm + "\"}"),
            null,
            null,
            key,
            policy);
        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            encrypted.Text,
            policy);
        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            encrypted.Text,
            key,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("aad").GetString(),
                Is.EqualTo(Base64Url.Encode(additionalAuthenticatedData).Text));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.AdditionalAuthenticatedDataPresent, Is.True);
            Assert.That(parsed.Token.AdditionalAuthenticatedData,
                Is.EqualTo(additionalAuthenticatedData));
            Assert.That(parsed.Token.AuthenticationTagVerified, Is.False);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.AdditionalAuthenticatedData,
                Is.EqualTo(additionalAuthenticatedData));
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
        });
    }

    [Test]
    public void EmptyAdditionalAuthenticatedDataRemainsPresentAndAuthenticated()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 12)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1, 2, 3 },
            Array.Empty<byte>(),
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            null,
            null,
            key,
            policy);
        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            encrypted.Text,
            policy);
        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            encrypted.Text,
            key,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("aad").GetString(),
                Is.Empty);
            Assert.That(parsed.Token.AdditionalAuthenticatedDataPresent, Is.True);
            Assert.That(parsed.Token.AdditionalAuthenticatedData, Is.Empty);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void DecryptsIndependentSerializationWithExternalAad()
    {
        byte[] keyBytes = Sequence(16, 40);
        byte[] plaintext = Encoding.ASCII.GetBytes("external authenticated data");
        byte[] externalData = Encoding.UTF8.GetBytes("context \u2603");
        string protectedSegment = Base64Url.Encode(
            Encoding.UTF8.GetBytes("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}")).Text;
        string aadSegment = Base64Url.Encode(externalData).Text;
        byte[] initializationVector = Sequence(12, 80);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                ciphertext,
                tag,
                Encoding.ASCII.GetBytes(protectedSegment + "." + aadSegment));
        }

        string json = "{\"protected\":\"" + protectedSegment
            + "\",\"aad\":\"" + aadSegment + "\",\"iv\":\""
            + Base64Url.Encode(initializationVector).Text
            + "\",\"ciphertext\":\"" + Base64Url.Encode(ciphertext).Text
            + "\",\"tag\":\"" + Base64Url.Encode(tag).Text + "\"}";
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JweFlattenedResult result = JsonWebEncryptionFlattened.Decrypt(
            json,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(result.Token.AdditionalAuthenticatedData,
                Is.EqualTo(externalData));
        });
    }

    [Test]
    public void AdditionalAuthenticatedDataTamperingFailsClosed()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 13)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1, 2, 3 },
            Encoding.ASCII.GetBytes("bound context"),
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            null,
            null,
            key,
            policy);
        System.Text.Json.Nodes.JsonObject changed = JsonNode.Parse(
            encrypted.Text).AsObject();
        changed["aad"] = Base64Url.Encode(
            Encoding.ASCII.GetBytes("other context")).Text;
        System.Text.Json.Nodes.JsonObject removed = JsonNode.Parse(
            encrypted.Text).AsObject();
        removed.Remove("aad");

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                changed.ToJsonString(), key, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                removed.ToJsonString(), key, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionFlattened.Parse(
                RewriteMember(encrypted.Text, "aad", "*"), policy),
                "invalid_additional_authenticated_data");
            AssertFailure(JsonWebEncryptionFlattened.Parse(
                RewriteNumberMember(encrypted.Text, "aad", 1), policy),
                "invalid_additional_authenticated_data");
        });
    }

    [Test]
    public void RsaAndAesKeyWrapRecipientsRoundTrip()
    {
        byte[] plaintext = Sequence(35, 2);
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey rsaPublic = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaOaepSha256,
            false).Key;
        TokenKey rsaPrivate = RsaKeyImporterOperations.ImportJose(
            privateRsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        TokenPolicy rsaPolicy = Policy(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        TokenTextResult rsaEncrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A256GCM\"}"),
            null,
            Header("{\"alg\":\"RSA-OAEP-256\"}"),
            rsaPublic,
            rsaPolicy);
        JweFlattenedResult rsaDecrypted = JsonWebEncryptionFlattened.Decrypt(
            rsaEncrypted.Text,
            rsaPrivate,
            rsaPolicy);

        TokenKey aesKey = SymmetricKeyImporterOperations.Import(Sequence(16, 9)).Key;
        TokenPolicy aesPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128CbcHmacSha256);
        TokenTextResult aesEncrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128CBC-HS256\"}"),
            null,
            Header("{\"alg\":\"A128KW\"}"),
            aesKey,
            aesPolicy);
        JweFlattenedResult aesDecrypted = JsonWebEncryptionFlattened.Decrypt(
            aesEncrypted.Text,
            aesKey,
            aesPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(rsaEncrypted.Success, Is.True);
            Assert.That(rsaDecrypted.Success, Is.True);
            Assert.That(rsaDecrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(aesEncrypted.Success, Is.True);
            Assert.That(aesDecrypted.Success, Is.True);
            Assert.That(aesDecrypted.Token.Plaintext, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void GeneratedAesGcmAndPbes2ParametersUseRecipientHeader()
    {
        byte[] plaintext = Sequence(29, 3);
        TokenKey gcmKey = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy gcmPolicy = Policy(
            JoseAlgorithms.Aes128GcmKeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult gcmEncrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            Header("{\"alg\":\"A128GCMKW\"}"),
            gcmKey,
            gcmPolicy);
        JweFlattenedResult gcmDecrypted = JsonWebEncryptionFlattened.Decrypt(
            gcmEncrypted.Text,
            gcmKey,
            gcmPolicy);

        TokenKey password = SymmetricKeyImporterOperations.Import(Sequence(24, 30)).Key;
        TokenPolicy pbes2Policy = Policy(
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult pbes2Encrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            Header("{\"alg\":\"PBES2-HS256+A128KW\"}"),
            password,
            pbes2Policy);
        JweFlattenedResult pbes2Decrypted = JsonWebEncryptionFlattened.Decrypt(
            pbes2Encrypted.Text,
            password,
            pbes2Policy);
        using JsonDocument gcmDocument = JsonDocument.Parse(gcmEncrypted.Text);
        using JsonDocument pbes2Document = JsonDocument.Parse(pbes2Encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(gcmEncrypted.Success, Is.True);
            Assert.That(gcmDocument.RootElement.GetProperty("header")
                .TryGetProperty("iv", out _), Is.True);
            Assert.That(gcmDocument.RootElement.GetProperty("header")
                .TryGetProperty("tag", out _), Is.True);
            Assert.That(gcmDecrypted.Success, Is.True);
            Assert.That(gcmDecrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(pbes2Encrypted.Success, Is.True);
            Assert.That(pbes2Document.RootElement.GetProperty("header")
                .TryGetProperty("p2s", out _), Is.True);
            Assert.That(pbes2Document.RootElement.GetProperty("header")
                .GetProperty("p2c").GetInt32(), Is.GreaterThan(0));
            Assert.That(pbes2Decrypted.Success, Is.True);
            Assert.That(pbes2Decrypted.Token.Plaintext, Is.EqualTo(plaintext));
        });
    }

    [TestCase(JoseAlgorithms.EcdhEs)]
    [TestCase(JoseAlgorithms.EcdhEsAes128KeyWrap)]
    public void EcdhEncryptionPlacesPublicEphemeralKeyInRecipientHeader(
        string algorithm)
    {
        byte[] plaintext = Sequence(31, 4);
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublic = ECDsa.Create(
            recipient.ExportParameters(false));
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient,
            algorithm,
            true).Key;
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            recipientPublic,
            algorithm,
            false).Key;
        TokenPolicy policy = Policy(algorithm, JoseAlgorithms.Aes128Gcm);

        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            Header("{\"alg\":\"" + algorithm + "\"}"),
            publicKey,
            policy);
        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            encrypted.Text,
            privateKey,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("header")
                .GetProperty("epk").ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void CompressionIsAppliedAndBoundedAfterAuthentication()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(new string('A', 1024));
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 5)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.AllowCompression = true;
        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\",\"zip\":\"DEF\"}"),
            null,
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);
        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            encrypted.Text,
            key,
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

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, key, disabled), "compression_disabled");
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, key, limited), "decompression_limit_exceeded");
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    public void DecryptRejectsAuthenticatedComponentTamperingWithoutPlaintext(
        string encryptionAlgorithm,
        int keySize)
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(keySize, 6)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            encryptionAlgorithm);
        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            Encoding.ASCII.GetBytes("authenticated plaintext"),
            Header("{\"enc\":\"" + encryptionAlgorithm + "\",\"kid\":\"a\"}"),
            null,
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);

        JweFlattenedResult headerFailure = JsonWebEncryptionFlattened.Decrypt(
            RewriteProtected(encrypted.Text, "kid", "b"),
            key,
            policy);
        JweFlattenedResult ivFailure = JsonWebEncryptionFlattened.Decrypt(
            FlipComponent(encrypted.Text, "iv"),
            key,
            policy);
        JweFlattenedResult ciphertextFailure = JsonWebEncryptionFlattened.Decrypt(
            FlipComponent(encrypted.Text, "ciphertext"),
            key,
            policy);
        JweFlattenedResult tagFailure = JsonWebEncryptionFlattened.Decrypt(
            FlipComponent(encrypted.Text, "tag"),
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
    public void HeadersRejectOverlapCriticalMisplacementAndWrongRegistryUse()
    {
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 7)).Key;

        TokenTextResult overlap = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\",\"kid\":\"one\"}"),
            Header("{\"kid\":\"two\"}"),
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);
        TokenTextResult critical = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\"}"),
            Header("{\"crit\":[\"example\"],\"example\":true}"),
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);
        TokenTextResult jwsParameter = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\",\"b64\":false}"),
            null,
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(overlap, "duplicate_header_parameter");
            AssertTextFailure(critical, "critical_header_not_protected");
            AssertTextFailure(jwsParameter, "invalid_jwe_header_parameter");
        });
    }

    [TestCase("{}", "missing_algorithm")]
    [TestCase("{\"protected\":1}", "invalid_protected_header")]
    [TestCase("{\"protected\":\"e30\",\"recipients\":[]}", "invalid_jwe_json_member")]
    [TestCase("{\"protected\":\"e30\",\"recipients\":[]}", "invalid_jwe_json_member")]
    public void ParseRejectsMalformedFlattenedShape(string json, string code)
    {
        JweFlattenedResult result = JsonWebEncryptionFlattened.Parse(json);

        AssertFailure(result, code);
    }

    [Test]
    public void DiagnosticPolicyAlgorithmPolicyWrongKeyAndNullsFailClosed()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 8)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionFlattened.Encrypt(
            new byte[] { 1, 2, 3 },
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            Header("{\"alg\":\"dir\"}"),
            key,
            policy);
        TokenKey wrongKey = SymmetricKeyImporterOperations.Import(Sequence(16, 9)).Key;
        TokenPolicy missingAlgorithm = Policy(JoseAlgorithms.Direct);

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryptionFlattened.ReadDiagnostic(
                encrypted.Text, policy), "unauthenticated_decode_not_allowed");
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, wrongKey, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, key, missingAlgorithm), "algorithm_not_allowed");
            Assert.That(() => JsonWebEncryptionFlattened.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionFlattened.Decrypt(
                null, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionFlattened.Decrypt(
                encrypted.Text, key, null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionFlattened.Encrypt(
                null, Header("{}"), null, null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionFlattened.Encrypt(
                new byte[] { 1 }, null, Header("{}"), null, null, key, policy),
                Throws.ArgumentNullException);
        });
    }

    static string RewriteMember(string json, string name, string value)
    {
        System.Text.Json.Nodes.JsonObject root = JsonNode.Parse(json).AsObject();
        root[name] = value;
        return root.ToJsonString();
    }

    static string RewriteNumberMember(string json, string name, int value)
    {
        System.Text.Json.Nodes.JsonObject root = JsonNode.Parse(json).AsObject();
        root[name] = value;
        return root.ToJsonString();
    }

    static string FlipComponent(string json, string name)
    {
        System.Text.Json.Nodes.JsonObject root = JsonNode.Parse(json).AsObject();
        byte[] bytes = Base64Url.Decode(root[name].GetValue<string>()).Bytes;
        bytes[0] ^= 1;
        root[name] = Base64Url.Encode(bytes).Text;
        return root.ToJsonString();
    }

    static string RewriteProtected(string json, string name, string value)
    {
        System.Text.Json.Nodes.JsonObject root = JsonNode.Parse(json).AsObject();
        byte[] bytes = Base64Url.Decode(
            root["protected"].GetValue<string>()).Bytes;
        string headerJson = Utf8Codec.Decode(bytes).Text;
        System.Text.Json.Nodes.JsonObject header = JsonNode.Parse(
            headerJson).AsObject();
        header[name] = value;
        root["protected"] = Base64Url.Encode(
            Encoding.UTF8.GetBytes(header.ToJsonString())).Text;
        return root.ToJsonString();
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Header;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static byte[] Sequence(int count, int start)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < count)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        return bytes;
    }

    static void AssertFailure(JweFlattenedResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error?.Code, Is.EqualTo(code));
            Assert.That(result.Token, Is.Null);
        });
    }

    static void AssertTextFailure(TokenTextResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error?.Code, Is.EqualTo(code));
            Assert.That(result.Text, Is.Null);
        });
    }
}
