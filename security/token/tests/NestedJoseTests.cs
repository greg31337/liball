using System.Text;
using token;

namespace token.Tests;

public class NestedJoseTests
{
    [TestCase("JWT", JoseMediaTypes.Jwt)]
    [TestCase("application/jwt", JoseMediaTypes.Jwt)]
    [TestCase("at+jwt", JoseMediaTypes.AccessTokenJwt)]
    [TestCase("APPLICATION/JOSE", JoseMediaTypes.Jose)]
    public void VerifiedCompactJwsRoutesCompactNestedContent(
        string contentType,
        string normalized)
    {
        TokenKey key = HmacKey(1);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string inner = JsonWebSignature.Sign(
            Encoding.ASCII.GetBytes("inner payload"),
            Header("{\"alg\":\"HS256\"}"),
            key,
            policy).Text;
        TokenTextResult outerText = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(inner),
            Header("{\"alg\":\"HS256\",\"cty\":\""
                + contentType + "\"}"),
            key,
            policy);
        JwsCompactResult outer = JsonWebSignature.Verify(
            outerText.Text,
            key,
            policy);

        NestedJoseResult result = NestedJose.Read(outer, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.ContentType, Is.EqualTo(contentType));
            Assert.That(result.Token.NormalizedContentType,
                Is.EqualTo(normalized));
            Assert.That(result.Token.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Token.CompactJws, Is.Not.Null);
            Assert.That(result.Token.CompactJws.PayloadAvailable, Is.False);
            Assert.That(result.Token.CompactJws.SignatureVerified, Is.False);
            Assert.That(result.Token.CompactJwe, Is.Null);
        });
    }

    [Test]
    public void AuthenticatedCompactJweRoutesNestedCompactJwe()
    {
        TokenKey innerKey = SymmetricKeyImporterOperations.Import(Sequence(16, 10)).Key;
        TokenKey outerKey = SymmetricKeyImporterOperations.Import(Sequence(16, 40)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string inner = JsonWebEncryption.Encrypt(
            Encoding.ASCII.GetBytes("inner secret"),
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            innerKey,
            policy).Text;
        TokenTextResult outerText = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes(inner),
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\","
                + "\"cty\":\"application/jose\"}"),
            outerKey,
            policy);
        JweCompactResult outer = JsonWebEncryption.Decrypt(
            outerText.Text,
            outerKey,
            policy);

        NestedJoseResult result = NestedJose.Read(outer, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.Token.CompactJwe, Is.Not.Null);
            Assert.That(result.Token.CompactJwe.PlaintextAvailable, Is.False);
            Assert.That(result.Token.CompactJwe.AuthenticationTagVerified,
                Is.False);
        });
    }

    [Test]
    public void JoseJsonContentTypeDispatchesAllJsonSerializations()
    {
        TokenKey hmac = HmacKey(2);
        TokenKey direct = SymmetricKeyImporterOperations.Import(Sequence(16, 70)).Key;
        TokenKey wrapOne = SymmetricKeyImporterOperations.Import(Sequence(16, 90)).Key;
        TokenKey wrapTwo = SymmetricKeyImporterOperations.Import(Sequence(16, 120)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        byte[] payload = Encoding.ASCII.GetBytes("inner");
        string flattenedJws = JsonWebSignatureFlattened.Sign(
            payload,
            Header("{\"alg\":\"HS256\"}"),
            null,
            hmac,
            policy).Text;
        string generalJws = JsonWebSignatureGeneral.Sign(
            payload,
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\"}"),
                Header("{\"alg\":\"HS256\"}")
            },
            null,
            new TokenKey[] { hmac, hmac },
            policy).Text;
        string flattenedJwe = JsonWebEncryptionFlattened.Encrypt(
            payload,
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            null,
            null,
            direct,
            policy).Text;
        string generalJwe = JsonWebEncryptionGeneral.Encrypt(
            payload,
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\"}"),
                Header("{\"alg\":\"A128KW\"}")
            },
            new TokenKey[] { wrapOne, wrapTwo },
            policy).Text;

        NestedJoseResult flattenedJwsResult = WrapInCompactJws(
            flattenedJws, "JOSE+JSON", hmac, policy);
        NestedJoseResult generalJwsResult = WrapInCompactJws(
            generalJws, "application/jose+json", hmac, policy);
        NestedJoseResult flattenedJweResult = WrapInCompactJws(
            flattenedJwe, "JOSE+JSON", hmac, policy);
        NestedJoseResult generalJweResult = WrapInCompactJws(
            generalJwe, "application/jose+json", hmac, policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattenedJwsResult.Success, Is.True);
            Assert.That(flattenedJwsResult.Token.FlattenedJws, Is.Not.Null);
            Assert.That(flattenedJwsResult.Token.GeneralJws, Is.Null);
            Assert.That(generalJwsResult.Success, Is.True);
            Assert.That(generalJwsResult.Token.GeneralJws, Is.Not.Null);
            Assert.That(flattenedJweResult.Success, Is.True);
            Assert.That(flattenedJweResult.Token.FlattenedJwe, Is.Not.Null);
            Assert.That(generalJweResult.Success, Is.True);
            Assert.That(generalJweResult.Token.GeneralJwe, Is.Not.Null);
        });
    }

    [Test]
    public void FlattenedAndGeneralJwsOutersRequireVerifiedSignatures()
    {
        TokenKey key = HmacKey(3);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string inner = CompactJws("payload", key, policy);
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"HS256\",\"cty\":\"JWT\"}");
        TokenTextResult flattenedText = JsonWebSignatureFlattened.Sign(
            Encoding.UTF8.GetBytes(inner),
            protectedHeader,
            null,
            key,
            policy);
        JwsFlattenedResult flattened = JsonWebSignatureFlattened.Verify(
            flattenedText.Text,
            key,
            policy);
        TokenTextResult generalText = JsonWebSignatureGeneral.Sign(
            Encoding.UTF8.GetBytes(inner),
            new JoseHeader[] { protectedHeader, protectedHeader },
            null,
            new TokenKey[] { key, key },
            policy);
        JwsGeneralResult general = JsonWebSignatureGeneral.Verify(
            generalText.Text,
            new TokenKey[] { key, key },
            policy);

        NestedJoseResult flattenedResult = NestedJose.Read(flattened, policy);
        NestedJoseResult generalResult = NestedJose.Read(general, policy);
        NestedJoseResult unverified = NestedJose.Read(
            JsonWebSignatureFlattened.Parse(flattenedText.Text, policy),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattenedResult.Success, Is.True);
            Assert.That(flattenedResult.Token.CompactJws, Is.Not.Null);
            Assert.That(generalResult.Success, Is.True);
            Assert.That(generalResult.Token.CompactJws, Is.Not.Null);
            AssertFailure(unverified, "outer_token_not_authenticated");
        });
    }

    [Test]
    public void FlattenedAndGeneralJweOutersRequireAuthenticatedDecryption()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, 20)).Key;
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string inner = CompactJws(
            "payload",
            HmacKey(4),
            Policy(JoseAlgorithms.HmacSha256));
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\"JWT\"}");
        TokenTextResult flattenedText = JsonWebEncryptionFlattened.Encrypt(
            Encoding.UTF8.GetBytes(inner),
            protectedHeader,
            null,
            null,
            key,
            policy);
        JweFlattenedResult flattened = JsonWebEncryptionFlattened.Decrypt(
            flattenedText.Text,
            key,
            policy);
        TokenTextResult generalText = JsonWebEncryptionGeneral.Encrypt(
            Encoding.UTF8.GetBytes(inner),
            protectedHeader,
            null,
            new JoseHeader[] { null },
            new TokenKey[] { key },
            policy);
        JweGeneralResult general = JsonWebEncryptionGeneral.Decrypt(
            generalText.Text,
            0,
            key,
            policy);

        NestedJoseResult flattenedResult = NestedJose.Read(flattened, policy);
        NestedJoseResult generalResult = NestedJose.Read(general, policy);
        NestedJoseResult unauthenticated = NestedJose.Read(
            JsonWebEncryptionFlattened.Parse(flattenedText.Text, policy),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattenedResult.Success, Is.True);
            Assert.That(generalResult.Success, Is.True);
            AssertFailure(unauthenticated, "outer_token_not_authenticated");
        });
    }

    [Test]
    public void CompactOuterParsingOrDiagnosticReadCannotExposeNestedContent()
    {
        TokenKey key = HmacKey(5);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string inner = CompactJws("payload", key, policy);
        string outerText = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(inner),
            Header("{\"alg\":\"HS256\",\"cty\":\"JWT\"}"),
            key,
            policy).Text;
        TokenPolicy diagnosticPolicy = Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;

        NestedJoseResult parsed = NestedJose.Read(
            JsonWebSignature.Parse(outerText, policy),
            policy);
        NestedJoseResult diagnostic = NestedJose.Read(
            JsonWebSignature.ReadDiagnostic(outerText, diagnosticPolicy),
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(parsed, "outer_token_not_authenticated");
            AssertFailure(diagnostic, "outer_token_not_authenticated");
        });
    }

    [Test]
    public void ContentTypeMustBeProtectedKnownAndSupported()
    {
        TokenKey key = HmacKey(6);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string inner = CompactJws("payload", key, policy);
        JwsCompactResult missing = VerifyOuter(inner, null, key, policy);
        JwsCompactResult unknown = VerifyOuter(
            inner, "application/example", key, policy);
        JwsCompactResult unsupported = VerifyOuter(
            inner, JoseMediaTypes.JwkJson, key, policy);
        TokenTextResult unprotectedText = JsonWebSignatureFlattened.Sign(
            Encoding.UTF8.GetBytes(inner),
            Header("{\"alg\":\"HS256\"}"),
            Header("{\"cty\":\"JWT\"}"),
            key,
            policy);
        JwsFlattenedResult unprotected = JsonWebSignatureFlattened.Verify(
            unprotectedText.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(NestedJose.Read(missing, policy),
                "missing_nested_content_type");
            AssertFailure(NestedJose.Read(unknown, policy),
                "unknown_nested_content_type");
            AssertFailure(NestedJose.Read(unsupported, policy),
                "unsupported_nested_content_type");
            AssertFailure(NestedJose.Read(unprotected, policy),
                "missing_nested_content_type");
        });
    }

    [Test]
    public void GeneralJwsRequiresConsistentProtectedContentTypes()
    {
        TokenKey key = HmacKey(7);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string inner = CompactJws("payload", key, policy);
        TokenTextResult text = JsonWebSignatureGeneral.Sign(
            Encoding.UTF8.GetBytes(inner),
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\",\"cty\":\"JWT\"}"),
                Header("{\"alg\":\"HS256\",\"cty\":\"JOSE\"}")
            },
            null,
            new TokenKey[] { key, key },
            policy);
        JwsGeneralResult verified = JsonWebSignatureGeneral.Verify(
            text.Text,
            new TokenKey[] { key, key },
            policy);

        NestedJoseResult result = NestedJose.Read(verified, policy);

        AssertFailure(result, "inconsistent_nested_content_type");
    }

    [Test]
    public void ContentTypeAndInnerSerializationMustMatch()
    {
        TokenKey key = HmacKey(8);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string compact = CompactJws("payload", key, policy);
        string json = JsonWebSignatureFlattened.Sign(
            Encoding.ASCII.GetBytes("payload"),
            Header("{\"alg\":\"HS256\"}"),
            null,
            key,
            policy).Text;

        NestedJoseResult compactExpected = NestedJose.Read(
            VerifyOuter(json, "JWT", key, policy),
            policy);
        NestedJoseResult jsonExpected = NestedJose.Read(
            VerifyOuter(compact, "JOSE+JSON", key, policy),
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(compactExpected, "nested_content_type_mismatch");
            AssertFailure(jsonExpected, "nested_content_type_mismatch");
        });
    }

    [Test]
    public void NestedContentMustBeUtf8AndArgumentsMustBePresent()
    {
        TokenKey key = HmacKey(9);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult text = JsonWebSignature.Sign(
            new byte[] { 255, 254 },
            Header("{\"alg\":\"HS256\",\"cty\":\"JWT\"}"),
            key,
            policy);
        JwsCompactResult verified = JsonWebSignature.Verify(
            text.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(NestedJose.Read(verified, policy),
                "invalid_nested_token_encoding");
            Assert.That(() => NestedJose.Read((JwsCompactResult)null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => NestedJose.Read(verified, null),
                Throws.ArgumentNullException);
        });
    }

    static NestedJoseResult WrapInCompactJws(
        string inner,
        string contentType,
        TokenKey key,
        TokenPolicy policy)
    {
        return NestedJose.Read(
            VerifyOuter(inner, contentType, key, policy),
            policy);
    }

    static JwsCompactResult VerifyOuter(
        string inner,
        string contentType,
        TokenKey key,
        TokenPolicy policy)
    {
        string header = contentType == null
            ? "{\"alg\":\"HS256\"}"
            : "{\"alg\":\"HS256\",\"cty\":\""
                + contentType + "\"}";
        TokenTextResult text = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(inner),
            Header(header),
            key,
            policy);
        return JsonWebSignature.Verify(text.Text, key, policy);
    }

    static string CompactJws(
        string payload,
        TokenKey key,
        TokenPolicy policy)
    {
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(payload),
            Header("{\"alg\":\"HS256\"}"),
            key,
            policy).Text;
    }

    static TokenKey HmacKey(int start)
    {
        return SymmetricKeyImporterOperations.Import(Sequence(32, start)).Key;
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

    static void AssertFailure(NestedJoseResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error?.Code, Is.EqualTo(code));
            Assert.That(result.Token, Is.Null);
        });
    }
}
