using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using token;

namespace token.Tests;

public class JsonWebEncryptionGeneralTests
{
    [Test]
    public void MixedKeyManagementRecipientsDecryptSharedCiphertext()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("general JWE payload \u2603");
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
        TokenKey aesKey = SymmetricKeyImporterOperations.Import(Sequence(16, 10)).Key;
        TokenKey gcmKey = SymmetricKeyImporterOperations.Import(Sequence(24, 30)).Key;
        TokenKey password = SymmetricKeyImporterOperations.Import(Sequence(24, 60)).Key;
        using ECDsa privateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEc = ECDsa.Create(privateEc.ExportParameters(false));
        TokenKey ecdhPublic = EcdsaKeyImporter.ImportJose(
            publicEc,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            false).Key;
        TokenKey ecdhPrivate = EcdsaKeyImporter.ImportJose(
            privateEc,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            true).Key;
        JoseHeader[] headers = new JoseHeader[]
        {
            Header("{\"alg\":\"RSA-OAEP-256\",\"kid\":\"rsa\"}"),
            Header("{\"alg\":\"A128KW\",\"kid\":\"aes\"}"),
            Header("{\"alg\":\"A192GCMKW\",\"kid\":\"gcm\"}"),
            Header("{\"alg\":\"PBES2-HS256+A128KW\",\"kid\":\"password\"}"),
            Header("{\"alg\":\"ECDH-ES+A128KW\",\"kid\":\"ecdh\"}")
        };
        TokenKey[] encryptionKeys = new TokenKey[]
        {
            rsaPublic, aesKey, gcmKey, password, ecdhPublic
        };
        TokenKey[] decryptionKeys = new TokenKey[]
        {
            rsaPrivate, aesKey, gcmKey, password, ecdhPrivate
        };
        TokenPolicy policy = Policy(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192GcmKeyWrap,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\",\"typ\":\"JWT\"}"),
            Header("{\"cty\":\"text/plain\"}"),
            headers,
            encryptionKeys,
            policy);
        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(
            encrypted.Text,
            policy);
        JweGeneralResult all = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            decryptionKeys,
            policy);
        JweGeneralResult selected = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            4,
            ecdhPrivate,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);
        JsonElement recipients = document.RootElement.GetProperty("recipients");

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.Kind, Is.EqualTo(TokenKind.JweJson));
            Assert.That(parsed.Token.Recipients, Has.Length.EqualTo(5));
            Assert.That(parsed.Token.CiphertextAvailable, Is.False);
            Assert.That(parsed.Token.Recipients[0].KeyId, Is.EqualTo("rsa"));
            Assert.That(parsed.Token.Recipients[4].Algorithm,
                Is.EqualTo(JoseAlgorithms.EcdhEsAes128KeyWrap));
            Assert.That(recipients[2].GetProperty("header")
                .TryGetProperty("iv", out _), Is.True);
            Assert.That(recipients[2].GetProperty("header")
                .TryGetProperty("tag", out _), Is.True);
            Assert.That(recipients[3].GetProperty("header")
                .TryGetProperty("p2s", out _), Is.True);
            Assert.That(recipients[3].GetProperty("header")
                .TryGetProperty("p2c", out _), Is.True);
            Assert.That(recipients[4].GetProperty("header")
                .TryGetProperty("epk", out _), Is.True);
            Assert.That(all.Success, Is.True);
            Assert.That(all.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(all.Token.Recipients.Select(x => x.Decrypted),
                Is.All.True);
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(selected.Token.Recipients[4].Decrypted, Is.True);
            Assert.That(selected.Token.Recipients[0].Decrypted, Is.False);
        });
    }

    [Test]
    public void WriteParseAndDiagnosticReadPreserveGeneralComponents()
    {
        byte[] ciphertext = new byte[] { 0, 1, 2, 255 };
        byte[][] encryptedKeys = new byte[][]
        {
            Sequence(24, 1),
            Sequence(40, 30)
        };
        byte[] initializationVector = Sequence(16, 80);
        byte[] authenticationTag = Sequence(16, 100);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192KeyWrap,
            JoseAlgorithms.Aes128CbcHmacSha256);
        TokenTextResult written = JsonWebEncryptionGeneral.Write(
            ciphertext,
            Header("{\"enc\":\"A128CBC-HS256\"}"),
            Header("{\"typ\":\"JWT\"}"),
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\",\"kid\":\"one\"}"),
                Header("{\"alg\":\"A192KW\",\"kid\":\"two\"}")
            },
            encryptedKeys,
            initializationVector,
            authenticationTag,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192KeyWrap,
            JoseAlgorithms.Aes128CbcHmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;

        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(
            written.Text,
            policy);
        JweGeneralResult diagnostic = JsonWebEncryptionGeneral.ReadDiagnostic(
            written.Text,
            diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.Recipients[0].EncryptedKey,
                Is.EqualTo(encryptedKeys[0]));
            Assert.That(parsed.Token.Recipients[1].EncryptedKey,
                Is.EqualTo(encryptedKeys[1]));
            Assert.That(parsed.Token.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(parsed.Token.AuthenticationTag,
                Is.EqualTo(authenticationTag));
            Assert.That(parsed.Token.Ciphertext, Is.Null);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.Ciphertext, Is.EqualTo(ciphertext));
            Assert.That(diagnostic.Token.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void WriteIncludesSharedAdditionalAuthenticatedData()
    {
        byte[] externalData = new byte[] { 0, 1, 128, 255 };
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult written = JsonWebEncryptionGeneral.Write(
            new byte[] { 1, 2, 3 },
            externalData,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new byte[][]
            {
                Sequence(24, 20),
                Sequence(24, 50)
            },
            Sequence(12, 80),
            Sequence(16, 100),
            policy);
        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(
            written.Text,
            policy);
        using JsonDocument document = JsonDocument.Parse(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("aad").GetString(),
                Is.EqualTo(Base64Url.Encode(externalData).Text));
            Assert.That(parsed.Token.AdditionalAuthenticatedData,
                Is.EqualTo(externalData));
            Assert.That(parsed.Token.AdditionalAuthenticatedDataPresent, Is.True);
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256)]
    public void SharedProtectedAlgorithmRoundTripsMultipleRecipients(
        string encryptionAlgorithm)
    {
        byte[] plaintext = Sequence(43, 7);
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 50)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            encryptionAlgorithm);

        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            plaintext,
            Header("{\"alg\":\"A128KW\",\"enc\":\""
                + encryptionAlgorithm + "\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"kid\":\"one\"}"),
                Header("{\"kid\":\"two\"}")
            },
            new TokenKey[] { first, second },
            policy);
        JweGeneralResult decrypted = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            new TokenKey[] { first, second },
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.Recipients[0].Algorithm,
                Is.EqualTo(JoseAlgorithms.Aes128KeyWrap));
            Assert.That(decrypted.Token.Recipients[1].KeyId, Is.EqualTo("two"));
        });
    }

    [TestCase(JoseAlgorithms.Direct)]
    [TestCase(JoseAlgorithms.EcdhEs)]
    public void SingleRecipientSupportsDirectKeyManagement(string algorithm)
    {
        byte[] plaintext = Sequence(27, 4);
        TokenKey encryptionKey;
        TokenKey decryptionKey;
        ECDsa privateEc = null;
        ECDsa publicEc = null;
        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            encryptionKey = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
            decryptionKey = encryptionKey;
        }
        else
        {
            privateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            publicEc = ECDsa.Create(privateEc.ExportParameters(false));
            encryptionKey = EcdsaKeyImporter.ImportJose(
                publicEc,
                algorithm,
                false).Key;
            decryptionKey = EcdsaKeyImporter.ImportJose(
                privateEc,
                algorithm,
                true).Key;
        }

        TokenPolicy policy = Policy(algorithm, JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[] { Header("{\"alg\":\"" + algorithm + "\"}") },
            new TokenKey[] { encryptionKey },
            policy);
        JweGeneralResult decrypted = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            0,
            decryptionKey,
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("recipients")[0]
                .TryGetProperty("encrypted_key", out _), Is.False);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
        });
        publicEc?.Dispose();
        privateEc?.Dispose();
    }

    [TestCase(JoseAlgorithms.Direct)]
    [TestCase(JoseAlgorithms.EcdhEs)]
    public void MultipleRecipientsRejectDirectKeyManagement(string algorithm)
    {
        TokenKey first;
        TokenKey second;
        ECDsa firstEc = null;
        ECDsa secondEc = null;
        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
            second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        }
        else
        {
            firstEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            secondEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            first = EcdsaKeyImporter.ImportJose(firstEc, algorithm, false).Key;
            second = EcdsaKeyImporter.ImportJose(secondEc, algorithm, false).Key;
        }

        TokenTextResult result = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"" + algorithm + "\"}"),
                Header("{\"alg\":\"" + algorithm + "\"}")
            },
            new TokenKey[] { first, second },
            Policy(algorithm, JoseAlgorithms.Aes128Gcm));

        AssertTextFailure(result, "direct_multi_recipient_not_supported");
        firstEc?.Dispose();
        secondEc?.Dispose();
    }

    [Test]
    public void DecryptsIndependentlyConstructedMultipleRecipientSerialization()
    {
        byte[] plaintext = Encoding.ASCII.GetBytes("independent general JWE");
        byte[] contentKey = Sequence(16, 90);
        TokenKey firstKey = SymmetricKeyImporterOperations.Import(Sequence(16, 10)).Key;
        TokenKey secondKey = SymmetricKeyImporterOperations.Import(Sequence(24, 30)).Key;
        byte[] firstWrapped = AesKeyWrap.Wrap(
            contentKey, firstKey, JoseAlgorithms.Aes128KeyWrap).Bytes;
        byte[] secondWrapped = AesKeyWrap.Wrap(
            contentKey, secondKey, JoseAlgorithms.Aes192KeyWrap).Bytes;
        string protectedSegment = Base64Url.Encode(
            Encoding.UTF8.GetBytes("{\"enc\":\"A128GCM\"}")).Text;
        byte[] initializationVector = Sequence(12, 70);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (AesGcm aes = new AesGcm(contentKey, 16))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                ciphertext,
                tag,
                Encoding.ASCII.GetBytes(protectedSegment));
        }

        string json = "{\"protected\":\"" + protectedSegment
            + "\",\"recipients\":[{\"header\":{\"alg\":\"A128KW\"},"
            + "\"encrypted_key\":\"" + Base64Url.Encode(firstWrapped).Text
            + "\"},{\"header\":{\"alg\":\"A192KW\"},\"encrypted_key\":\""
            + Base64Url.Encode(secondWrapped).Text + "\"}],\"iv\":\""
            + Base64Url.Encode(initializationVector).Text
            + "\",\"ciphertext\":\"" + Base64Url.Encode(ciphertext).Text
            + "\",\"tag\":\"" + Base64Url.Encode(tag).Text + "\"}";
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        JweGeneralResult result = JsonWebEncryptionGeneral.Decrypt(
            json,
            new TokenKey[] { firstKey, secondKey },
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(result.Token.AuthenticationTagVerified, Is.True);
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256)]
    public void ExternalAdditionalAuthenticatedDataRoundTripsForAllRecipients(
        string encryptionAlgorithm)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("shared AAD plaintext \u2603");
        byte[] externalData = new byte[] { 0, 1, 46, 128, 255 };
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 14)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(24, 40)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192KeyWrap,
            encryptionAlgorithm);

        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            plaintext,
            externalData,
            Header("{\"enc\":\"" + encryptionAlgorithm + "\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A192KW\"}")
            },
            new TokenKey[] { first, second },
            policy);
        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(
            encrypted.Text,
            policy);
        JweGeneralResult decrypted = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            new TokenKey[] { first, second },
            policy);
        using JsonDocument document = JsonDocument.Parse(encrypted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("aad").GetString(),
                Is.EqualTo(Base64Url.Encode(externalData).Text));
            Assert.That(parsed.Token.AdditionalAuthenticatedDataPresent, Is.True);
            Assert.That(parsed.Token.AdditionalAuthenticatedData,
                Is.EqualTo(externalData));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
        });
    }

    [Test]
    public void ExternalAdditionalAuthenticatedDataTamperingFailsEveryRecipient()
    {
        byte[] externalData = Encoding.ASCII.GetBytes("shared context");
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 15)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 50)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1, 2, 3 },
            externalData,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);
        System.Text.Json.Nodes.JsonObject changed = JsonNode.Parse(
            encrypted.Text).AsObject();
        changed["aad"] = Base64Url.Encode(
            Encoding.ASCII.GetBytes("changed context")).Text;

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                changed.ToJsonString(), 0, first, policy),
                "decryption_failed");
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                changed.ToJsonString(), 1, second, policy),
                "decryption_failed");
        });
    }

    [Test]
    public void CompressionRoundTripsAndEnforcesExpansionLimit()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(new string('A', 1024));
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        policy.AllowCompression = true;
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            plaintext,
            Header("{\"enc\":\"A128GCM\",\"zip\":\"DEF\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);
        JweGeneralResult decrypted = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            new TokenKey[] { first, second },
            policy);
        TokenPolicy limited = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        limited.AllowCompression = true;
        limited.MaximumDecompressedSizeInBytes = 100;
        limited.MaximumCompressionRatio = 1000;

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                encrypted.Text, 0, first, limited),
                "decompression_limit_exceeded");
        });
    }

    [Test]
    public void SelectedAndAllRecipientDecryptionFailClosed()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenKey wrong = SymmetricKeyImporterOperations.Import(Sequence(16, 40)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1, 2, 3 },
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);

        JweGeneralResult selectedWrong = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text, 1, wrong, policy);
        JweGeneralResult allWrong = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            new TokenKey[] { first, wrong },
            policy);
        JweGeneralResult countMismatch = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            new TokenKey[] { first },
            policy);
        JweGeneralResult indexFailure = JsonWebEncryptionGeneral.Decrypt(
            encrypted.Text,
            2,
            first,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(selectedWrong, "unwrap_failed");
            AssertFailure(allWrong, "unwrap_failed");
            AssertFailure(countMismatch, "recipient_count_mismatch");
            AssertFailure(indexFailure, "recipient_index_out_of_range");
        });
    }

    [Test]
    public void TamperingSharedComponentsFailsAuthentication()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            Encoding.ASCII.GetBytes("authenticated"),
            Header("{\"enc\":\"A128GCM\",\"kid\":\"a\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                RewriteProtected(encrypted.Text, "kid", "b"),
                0, first, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                FlipComponent(encrypted.Text, "iv"),
                0, first, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                FlipComponent(encrypted.Text, "ciphertext"),
                0, first, policy), "decryption_failed");
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                FlipComponent(encrypted.Text, "tag"),
                0, first, policy), "decryption_failed");
        });
    }

    [Test]
    public void RecipientHeadersRejectOverlapAndInconsistentContentAlgorithms()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            JoseAlgorithms.Aes256Gcm);
        TokenTextResult overlap = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\",\"kid\":\"shared\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\",\"kid\":\"duplicate\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);
        TokenTextResult inconsistent = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1 },
            Header("{\"typ\":\"JWT\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\",\"enc\":\"A128GCM\"}"),
                Header("{\"alg\":\"A128KW\",\"enc\":\"A256GCM\"}")
            },
            new TokenKey[] { first, second },
            policy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(overlap, "duplicate_header_parameter");
            AssertTextFailure(inconsistent, "inconsistent_content_encryption");
        });
    }

    [TestCase("{}", "invalid_recipients")]
    [TestCase("{\"recipients\":[]}", "invalid_recipients")]
    [TestCase("{\"recipients\":[1]}", "invalid_recipient_entry")]
    [TestCase("{\"recipients\":[{\"signature\":\"AA\"}]}", "invalid_recipient_entry")]
    [TestCase("{\"recipients\":[{}],\"header\":{}}", "invalid_jwe_json_member")]
    public void ParseRejectsMalformedGeneralShape(string json, string code)
    {
        JweGeneralResult result = JsonWebEncryptionGeneral.Parse(json);

        AssertFailure(result, code);
    }

    [Test]
    public void DiagnosticPolicyAllowlistCountsAndNullsAreEnforced()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(Sequence(16, 1)).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            new byte[] { 1 },
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { first, second },
            policy);
        TokenPolicy missingContent = Policy(JoseAlgorithms.Aes128KeyWrap);

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebEncryptionGeneral.ReadDiagnostic(
                encrypted.Text, policy), "unauthenticated_decode_not_allowed");
            AssertFailure(JsonWebEncryptionGeneral.Decrypt(
                encrypted.Text, 0, first, missingContent),
                "algorithm_not_allowed");
            Assert.That(() => JsonWebEncryptionGeneral.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionGeneral.Decrypt(
                null, 0, first, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionGeneral.Decrypt(
                encrypted.Text, 0, null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionGeneral.Decrypt(
                encrypted.Text, (TokenKey[])null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionGeneral.Encrypt(
                null, Header("{}"), null, Array.Empty<JoseHeader>(),
                Array.Empty<TokenKey>(), policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebEncryptionGeneral.Encrypt(
                new byte[] { 1 }, Header("{}"), null,
                new JoseHeader[] { Header("{}") },
                Array.Empty<TokenKey>(), policy), Throws.ArgumentException);
            Assert.That(() => JsonWebEncryptionGeneral.Encrypt(
                new byte[] { 1 }, null, Header("{}"), null,
                new JoseHeader[] { Header("{}") },
                new TokenKey[] { first }, policy), Throws.ArgumentNullException);
        });
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
        System.Text.Json.Nodes.JsonObject header = JsonNode.Parse(
            Utf8Codec.Decode(bytes).Text).AsObject();
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
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }

    static void AssertFailure(JweGeneralResult result, string code)
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
