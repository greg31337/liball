using System.Text;
using token;

namespace token.Tests;

public class UnknownCriticalHeaderNegativeTests
{
    const string Extension = "urn:example:critical";

    [Test]
    public void JoseJsonSerializationsRejectUnknownCriticalHeaders()
    {
        TokenPolicy writingPolicy = JosePolicy();
        writingPolicy.UnderstoodCriticalHeaders = new[] { Extension };
        TokenPolicy readingPolicy = JosePolicy();
        JoseHeader signatureHeader = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"" + Extension
            + "\"],\"" + Extension + "\":true}");
        JoseHeader encryptionHeader = Header(
            "{\"enc\":\"A128GCM\",\"crit\":[\"" + Extension
            + "\"],\"" + Extension + "\":true}");
        JoseHeader recipientHeader = Header("{\"alg\":\"dir\"}");
        byte[] payload = new byte[] { 1 };
        byte[] signature = Sequence(32);
        byte[] initializationVector = Sequence(12);
        byte[] authenticationTag = Sequence(16);

        TokenTextResult flattenedJws = JsonWebSignatureFlattened.Write(
            payload,
            signatureHeader,
            null,
            signature,
            writingPolicy);
        TokenTextResult generalJws = JsonWebSignatureGeneral.Write(
            payload,
            new[] { signatureHeader },
            null,
            new[] { signature },
            writingPolicy);
        TokenTextResult flattenedJwe = JsonWebEncryptionFlattened.Write(
            payload,
            encryptionHeader,
            null,
            recipientHeader,
            System.Array.Empty<byte>(),
            initializationVector,
            authenticationTag,
            writingPolicy);
        TokenTextResult generalJwe = JsonWebEncryptionGeneral.Write(
            payload,
            encryptionHeader,
            null,
            new[] { recipientHeader },
            new[] { System.Array.Empty<byte>() },
            initializationVector,
            authenticationTag,
            writingPolicy);

        JwsFlattenedResult flattenedJwsResult =
            JsonWebSignatureFlattened.Parse(
                flattenedJws.Text,
                readingPolicy);
        JwsGeneralResult generalJwsResult = JsonWebSignatureGeneral.Parse(
            generalJws.Text,
            readingPolicy);
        JweFlattenedResult flattenedJweResult =
            JsonWebEncryptionFlattened.Parse(
                flattenedJwe.Text,
                readingPolicy);
        JweGeneralResult generalJweResult = JsonWebEncryptionGeneral.Parse(
            generalJwe.Text,
            readingPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(flattenedJws.Success, Is.True);
            Assert.That(generalJws.Success, Is.True);
            Assert.That(flattenedJwe.Success, Is.True);
            Assert.That(generalJwe.Success, Is.True);
            AssertJwsFailure(flattenedJwsResult);
            AssertJwsFailure(generalJwsResult);
            AssertJweFailure(flattenedJweResult);
            AssertJweFailure(generalJweResult);
        });
    }

    [Test]
    public void CompactJwtValidationRejectsUnknownCriticalHeadersBeforeResolution()
    {
        TokenPolicy creationPolicy = JosePolicy();
        creationPolicy.UnderstoodCriticalHeaders = new[] { Extension };
        TokenKey signingKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32),
            JoseAlgorithms.HmacSha256).Key;
        TokenKey encryptionKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16),
            JoseAlgorithms.Direct).Key;
        TokenTextResult signed = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"signed\"}"),
            Header(
                "{\"alg\":\"HS256\",\"typ\":\"JWT\",\"crit\":[\""
                + Extension + "\"],\"" + Extension + "\":true}"),
            signingKey,
            creationPolicy);
        TokenTextResult encrypted = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes("{\"sub\":\"encrypted\"}"),
            Header(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
                + "\"typ\":\"JWT\",\"crit\":[\"" + Extension
                + "\"],\"" + Extension + "\":true}"),
            encryptionKey,
            creationPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(signingKey);
        });
        JwtValidationPolicy validationPolicy = JwtValidationPolicy.Create(
            JosePolicy());

        JwtValidationResult signedResult = JwtValidator.ValidateSigned(
            signed.Text,
            resolver,
            validationPolicy);
        JwtValidationResult encryptedResult = JwtValidator.ValidateEncrypted(
            encrypted.Text,
            resolver,
            validationPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(encrypted.Success, Is.True);
            AssertJwtFailure(signedResult);
            AssertJwtFailure(encryptedResult);
            Assert.That(resolverCalls, Is.Zero);
        });

        signingKey.ReleaseOwnedMaterial();
        encryptionKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void CwtValidationRejectsUnknownCriticalHeadersWithoutReturningClaims()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32),
            CoseAlgorithms.HmacSha256).Key;
        TokenPolicy tokenPolicy = new TokenPolicy();
        tokenPolicy.AllowedAlgorithms = new[] { "HMAC 256/256" };
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        TokenBytesResult created = CborWebToken.Create(
            claims,
            key,
            tokenPolicy);
        CoseMac0 message = CoseMac0.Read(created.Bytes).Message;
        message.Header.SetProtected(
            CoseHeaderLabels.Critical,
            Array(Text(Extension)));
        message.Header.SetProtectedByTextLabel(Extension, Boolean(true));
        TokenBytesResult unknownCritical = message.Write();
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });

        CwtValidationResult result = CborWebToken.Validate(
            unknownCritical.Bytes,
            resolver,
            CwtValidationPolicy.Create(tokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(unknownCritical.Success, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Claims, Is.Null);
            Assert.That(result.Headers, Is.Empty);
            AssertError(result.Error, "unknown_cose_critical_header");
            Assert.That(resolverCalls, Is.EqualTo(1));
        });

        key.ReleaseOwnedMaterial();
    }

    static TokenPolicy JosePolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm
        };
        return policy;
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static byte[] Sequence(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index += 1;
        }

        return bytes;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static void AssertJwsFailure(JwsFlattenedResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, "unknown_critical_header");
    }

    static void AssertJwsFailure(JwsGeneralResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, "unknown_critical_header");
    }

    static void AssertJweFailure(JweFlattenedResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, "unknown_critical_header");
    }

    static void AssertJweFailure(JweGeneralResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        AssertError(result.Error, "unknown_critical_header");
    }

    static void AssertJwtFailure(JwtValidationResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        AssertError(result.Error, "unknown_critical_header");
    }

    static void AssertError(TokenError error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo(code));
    }
}
