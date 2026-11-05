using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using token;

namespace token.Tests;

public class JsonWebSignatureFlattenedTests
{
    [Test]
    public void SignHmacProducesInteroperableFlattenedJson()
    {
        byte[] payload = Encoding.UTF8.GetBytes("flattened payload \u2603");
        byte[] keyBytes = Sequence(32, 1);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader protectedHeader = Header("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        JoseHeader unprotectedHeader = Header("{\"kid\":\"key-1\"}");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            payload,
            protectedHeader,
            unprotectedHeader,
            key,
            policy);

        using JsonDocument document = JsonDocument.Parse(signed.Text);
        JsonElement root = document.RootElement;
        string protectedSegment = root.GetProperty("protected").GetString();
        string payloadSegment = root.GetProperty("payload").GetString();
        string signatureSegment = root.GetProperty("signature").GetString();
        byte[] expected = HMACSHA256.HashData(
            keyBytes,
            Encoding.ASCII.GetBytes(protectedSegment + "." + payloadSegment));
        JwsFlattenedResult verified = JsonWebSignatureFlattened.Verify(
            signed.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(payloadSegment, Is.EqualTo(Base64Url.Encode(payload).Text));
            Assert.That(Base64Url.Decode(signatureSegment).Bytes, Is.EqualTo(expected));
            Assert.That(root.GetProperty("header").GetProperty("kid").GetString(),
                Is.EqualTo("key-1"));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.Kind, Is.EqualTo(TokenKind.JwsJson));
            Assert.That(verified.Token.KeyId, Is.EqualTo("key-1"));
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.SignatureVerified, Is.True);
        });
    }

    [Test]
    public void WriteParseAndDiagnosticReadPreserveComponents()
    {
        byte[] payload = new byte[] { 0, 1, 2, 255 };
        byte[] signature = Sequence(32, 10);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult written = JsonWebSignatureFlattened.Write(
            payload,
            Header("{\"alg\":\"HS256\"}"),
            Header("{\"kid\":\"writer-key\"}"),
            signature,
            policy);
        TokenPolicy diagnosticPolicy = Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;

        JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
            written.Text,
            policy);
        JwsFlattenedResult diagnostic = JsonWebSignatureFlattened.ReadDiagnostic(
            written.Text,
            diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.Payload, Is.Null);
            Assert.That(parsed.Token.Signature, Is.EqualTo(signature));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Token.SignatureVerified, Is.False);
            Assert.That(diagnostic.Token.UnprotectedHeader.Get("kid").StringValue,
                Is.EqualTo("writer-key"));
        });
    }

    [Test]
    public void RsaAndEcdsaFlattenedSignaturesVerifyWithPublicKeys()
    {
        byte[] payload = Sequence(23, 20);
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey rsaSigning = RsaKeyImporterOperations.Import(privateRsa, true).Key;
        TokenKey rsaVerification = RsaKeyImporterOperations.Import(publicRsa, false).Key;
        TokenPolicy rsaPolicy = Policy(JoseAlgorithms.RsaPssSha256);
        TokenTextResult rsaText = JsonWebSignatureFlattened.Sign(
            payload,
            Header("{\"alg\":\"PS256\"}"),
            null,
            rsaSigning,
            rsaPolicy);
        JwsFlattenedResult rsaResult = JsonWebSignatureFlattened.Verify(
            rsaText.Text,
            rsaVerification,
            rsaPolicy);

        using ECDsa privateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEc = ECDsa.Create(privateEc.ExportParameters(false));
        TokenKey ecSigning = EcdsaKeyImporter.Import(privateEc, true).Key;
        TokenKey ecVerification = EcdsaKeyImporter.Import(publicEc, false).Key;
        TokenPolicy ecPolicy = Policy(JoseAlgorithms.EcdsaSha256);
        TokenTextResult ecText = JsonWebSignatureFlattened.Sign(
            payload,
            Header("{\"alg\":\"ES256\"}"),
            null,
            ecSigning,
            ecPolicy);
        JwsFlattenedResult ecResult = JsonWebSignatureFlattened.Verify(
            ecText.Text,
            ecVerification,
            ecPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(rsaText.Success, Is.True);
            Assert.That(rsaResult.Success, Is.True);
            Assert.That(rsaResult.Token.Payload, Is.EqualTo(payload));
            Assert.That(ecText.Success, Is.True);
            Assert.That(ecResult.Success, Is.True);
            Assert.That(ecResult.Token.Signature, Has.Length.EqualTo(64));
            Assert.That(ecResult.Token.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void UnencodedAttachedPayloadRoundTripsAsJsonString()
    {
        byte[] payload = Encoding.UTF8.GetBytes("raw . payload \u2603");
        byte[] keyBytes = Sequence(32, 30);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            payload, header, null, key, policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);
        JwsFlattenedResult verified = JsonWebSignatureFlattened.Verify(
            signed.Text, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("payload").GetString(),
                Is.EqualTo("raw . payload \u2603"));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void DetachedUnencodedPayloadSupportsArbitraryBytes()
    {
        byte[] payload = new byte[] { 0, 255, 46, 128, 10 };
        byte[] keyBytes = Sequence(32, 40);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignatureFlattened.SignDetached(
            payload, header, null, key, policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);
        JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
            signed.Text, policy);
        JwsFlattenedResult verified = JsonWebSignatureFlattened.VerifyDetached(
            signed.Text, payload, key, policy);
        JwsFlattenedResult wrongMode = JsonWebSignatureFlattened.Verify(
            signed.Text, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(document.RootElement.TryGetProperty("payload", out _), Is.False);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.DetachedPayload, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
            AssertFailure(wrongMode, "missing_payload");
        });
    }

    [Test]
    public void VerificationRejectsPayloadAndSignatureTampering()
    {
        byte[] keyBytes = Sequence(32, 50);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            Encoding.ASCII.GetBytes("original"),
            Header("{\"alg\":\"HS256\"}"),
            null,
            key,
            policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);
        JsonElement root = document.RootElement;
        string protectedText = root.GetProperty("protected").GetString();
        string signatureText = root.GetProperty("signature").GetString();
        string payloadTampered = "{\"payload\":\""
            + Base64Url.Encode(Encoding.ASCII.GetBytes("changed")).Text
            + "\",\"protected\":\"" + protectedText
            + "\",\"signature\":\"" + signatureText + "\"}";
        byte[] changedSignature = Base64Url.Decode(signatureText).Bytes;
        changedSignature[0] ^= 1;
        string signatureTampered = "{\"payload\":\""
            + root.GetProperty("payload").GetString()
            + "\",\"protected\":\"" + protectedText
            + "\",\"signature\":\""
            + Base64Url.Encode(changedSignature).Text + "\"}";

        JwsFlattenedResult wrongPayload = JsonWebSignatureFlattened.Verify(
            payloadTampered, key, policy);
        JwsFlattenedResult wrongSignature = JsonWebSignatureFlattened.Verify(
            signatureTampered, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongPayload, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
        });
    }

    [TestCase("{\"payload\":\"AA\",\"signature\":\"AA\"}", "missing_algorithm")]
    [TestCase("{\"payload\":\"AA\",\"protected\":1,\"signature\":\"AA\"}", "invalid_protected_header")]
    [TestCase("{\"payload\":\"AA\",\"protected\":\"AA\",\"signature\":\"AA\"}", "invalid_json")]
    [TestCase("{\"payload\":\"AA\",\"protected\":\"e30\",\"signature\":\"AA\"}", "missing_algorithm")]
    [TestCase("{\"payload\":\"AA\",\"protected\":\"e30\",\"signatures\":[] ,\"signature\":\"AA\"}", "invalid_jws_json_member")]
    public void ParseRejectsMalformedFlattenedShape(string json, string code)
    {
        JwsFlattenedResult result = JsonWebSignatureFlattened.Parse(
            json,
            Policy(JoseAlgorithms.HmacSha256));

        AssertFailure(result, code);
    }

    [Test]
    public void HeadersRejectOverlapAndUnprotectedSecurityParameters()
    {
        byte[] keyBytes = Sequence(32, 60);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JoseHeader protectedHeader = Header("{\"alg\":\"HS256\",\"kid\":\"one\"}");

        TokenTextResult overlap = JsonWebSignatureFlattened.Sign(
            new byte[] { 1 }, protectedHeader,
            Header("{\"kid\":\"two\"}"), key, policy);
        TokenTextResult unprotectedCrit = JsonWebSignatureFlattened.Sign(
            new byte[] { 1 }, Header("{\"alg\":\"HS256\"}"),
            Header("{\"crit\":[\"x\"],\"x\":true}"), key, policy);
        TokenTextResult unprotectedB64 = JsonWebSignatureFlattened.Sign(
            new byte[] { 1 }, Header("{\"alg\":\"HS256\"}"),
            Header("{\"b64\":false}"), key, policy);
        TokenTextResult unprotectedAlg = JsonWebSignatureFlattened.Sign(
            new byte[] { 1 }, Header("{}"),
            Header("{\"alg\":\"HS256\"}"), key, policy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(overlap, "duplicate_header_parameter");
            AssertTextFailure(unprotectedCrit, "critical_header_not_protected");
            AssertTextFailure(unprotectedB64, "payload_encoding_not_protected");
            AssertTextFailure(unprotectedAlg, "missing_algorithm");
        });
    }

    [Test]
    public void OperationsEnforcePoliciesAndNullArguments()
    {
        byte[] payload = new byte[] { 1 };
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 70)).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy allowed = Policy(JoseAlgorithms.HmacSha256);
        TokenPolicy denied = new TokenPolicy();
        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            payload, header, null, key, allowed);

        Assert.Multiple(() =>
        {
            AssertTextFailure(JsonWebSignatureFlattened.Sign(
                payload, header, null, key, denied), "algorithm_not_allowed");
            AssertFailure(JsonWebSignatureFlattened.Verify(
                signed.Text, key, denied), "algorithm_not_allowed");
            Assert.That(() => JsonWebSignatureFlattened.Parse(null, allowed),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Parse("{}", null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Sign(
                null, header, null, key, allowed), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Sign(
                payload, null, null, key, allowed), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Sign(
                payload, header, null, null, allowed), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Verify(
                null, key, allowed), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureFlattened.Verify(
                signed.Text, null, allowed), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void UnsecuredFlattenedJwsCanBeReadButRequiresPolicyToCreateAndValidate()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}");
        JoseHeader header = Header("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        TokenPolicy allowed = Policy(JoseAlgorithms.None);
        allowed.AllowUnsecuredTokens = true;
        TokenPolicy disabled = Policy(JoseAlgorithms.None);
        TokenPolicy missingAllowlist = new TokenPolicy();
        missingAllowlist.AllowUnsecuredTokens = true;

        TokenTextResult created = JsonWebSignatureFlattened.CreateUnsecured(
            payload, header, Header("{\"kid\":\"unsecured\"}"), allowed);
        JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(created.Text);
        JwsFlattenedResult validated = JsonWebSignatureFlattened.ValidateUnsecured(
            created.Text, allowed);
        TokenTextResult disabledCreation = JsonWebSignatureFlattened.CreateUnsecured(
            payload, header, null, disabled);
        JwsFlattenedResult disabledValidation =
            JsonWebSignatureFlattened.ValidateUnsecured(created.Text, disabled);
        TokenTextResult deniedAlgorithm = JsonWebSignatureFlattened.CreateUnsecured(
            payload, header, null, missingAllowlist);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 91)).Key;
        JwsFlattenedResult ordinaryVerification = JsonWebSignatureFlattened.Verify(
            created.Text, key, allowed);

        using JsonDocument document = JsonDocument.Parse(created.Text);
        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("signature").GetString(),
                Is.Empty);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.Unsecured, Is.True);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.KeyId, Is.EqualTo("unsecured"));
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Token.Payload, Is.EqualTo(payload));
            Assert.That(validated.Token.SignatureVerified, Is.False);
            AssertTextFailure(disabledCreation, "unsecured_tokens_not_allowed");
            AssertFailure(disabledValidation, "unsecured_tokens_not_allowed");
            AssertTextFailure(deniedAlgorithm, "algorithm_not_allowed");
            AssertFailure(ordinaryVerification, "unsecured_jws_not_supported");
        });
    }

    [Test]
    public void UnsecuredFlattenedJwsEnforcesAlgorithmAndEmptySignature()
    {
        byte[] payload = Encoding.UTF8.GetBytes("raw . payload \u2603");
        JoseHeader header = Header(
            "{\"alg\":\"none\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = Policy(JoseAlgorithms.None);
        policy.AllowUnsecuredTokens = true;
        TokenTextResult created = JsonWebSignatureFlattened.CreateUnsecured(
            payload, header, null, policy);
        JwsFlattenedResult validated = JsonWebSignatureFlattened.ValidateUnsecured(
            created.Text, policy);
        string nonempty = created.Text.Replace(
            "\"signature\":\"\"",
            "\"signature\":\"AA\"");
        JwsFlattenedResult invalidSignature = JsonWebSignatureFlattened.Parse(
            nonempty, policy);
        TokenTextResult wrongAlgorithm =
            JsonWebSignatureFlattened.CreateUnsecured(
                payload, Header("{\"alg\":\"HS256\"}"), null, policy);
        TokenTextResult ordinaryWrite = JsonWebSignatureFlattened.Write(
            payload, header, null, Array.Empty<byte>(), policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(validated.Token.Payload, Is.EqualTo(payload));
            AssertFailure(invalidSignature, "invalid_unsecured_signature");
            AssertTextFailure(wrongAlgorithm, "invalid_unsecured_algorithm");
            AssertTextFailure(ordinaryWrite, "unsecured_jws_not_supported");
        });
    }

    [Test]
    public void OperationsEnforceJsonDepthAndEncodedSizeLimits()
    {
        byte[] keyBytes = Sequence(32, 80);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader protectedHeader = Header("{\"alg\":\"HS256\"}");
        JoseHeader nestedUnprotected = Header("{\"custom\":{}}");
        TokenPolicy depthPolicy = Policy(JoseAlgorithms.HmacSha256);
        depthPolicy.MaximumJsonDepth = 1;
        TokenTextResult tooDeep = JsonWebSignatureFlattened.Sign(
            new byte[] { 1 },
            protectedHeader,
            nestedUnprotected,
            key,
            depthPolicy);

        TokenPolicy generous = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult ordinary = JsonWebSignatureFlattened.Sign(
            Sequence(20, 90), protectedHeader, null, key, generous);
        TokenPolicy limited = Policy(JoseAlgorithms.HmacSha256);
        limited.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(ordinary.Text) - 1;
        TokenTextResult writeTooLarge = JsonWebSignatureFlattened.Sign(
            Sequence(20, 90), protectedHeader, null, key, limited);
        JwsFlattenedResult readTooLarge = JsonWebSignatureFlattened.Parse(
            ordinary.Text, limited);

        Assert.Multiple(() =>
        {
            AssertTextFailure(tooDeep, "json_depth_exceeded");
            Assert.That(ordinary.Success, Is.True);
            AssertTextFailure(writeTooLarge, "token_too_large");
            AssertFailure(readTooLarge, "token_too_large");
        });
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static byte[] Sequence(int length, int seed)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = (byte)(seed + index);
            index += 1;
        }

        return result;
    }

    static void AssertFailure(JwsFlattenedResult result, string code)
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
