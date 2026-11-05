using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using token;

namespace token.Tests;

public class JsonWebSignatureGeneralTests
{
    [Test]
    public void SignAndVerifyMultipleAlgorithmsWithPublicVerificationKeys()
    {
        byte[] payload = Encoding.UTF8.GetBytes("shared general payload \u2603");
        byte[] hmacBytes = Sequence(32, 1);
        TokenKey hmac = SymmetricKeyImporterOperations.Import(hmacBytes).Key;
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        using ECDsa privateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEc = ECDsa.Create(privateEc.ExportParameters(false));
        TokenKey rsaSign = RsaKeyImporterOperations.Import(privateRsa, true).Key;
        TokenKey rsaVerify = RsaKeyImporterOperations.Import(publicRsa, false).Key;
        TokenKey ecSign = EcdsaKeyImporter.Import(privateEc, true).Key;
        TokenKey ecVerify = EcdsaKeyImporter.Import(publicEc, false).Key;
        JoseHeader[] protectedHeaders = new JoseHeader[]
        {
            Header("{\"alg\":\"HS256\"}"),
            Header("{\"alg\":\"PS256\"}"),
            Header("{\"alg\":\"ES256\"}")
        };
        JoseHeader[] unprotectedHeaders = new JoseHeader[]
        {
            Header("{\"kid\":\"hmac\"}"),
            Header("{\"kid\":\"rsa\"}"),
            Header("{\"kid\":\"ec\"}")
        };
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.RsaPssSha256,
            JoseAlgorithms.EcdsaSha256);

        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload,
            protectedHeaders,
            unprotectedHeaders,
            new TokenKey[] { hmac, rsaSign, ecSign },
            policy);
        JwsGeneralResult verified = JsonWebSignatureGeneral.Verify(
            signed.Text,
            new TokenKey[] { hmac, rsaVerify, ecVerify },
            policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(document.RootElement.GetProperty("signatures").GetArrayLength(),
                Is.EqualTo(3));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.Kind, Is.EqualTo(TokenKind.JwsJson));
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.Signatures, Has.Length.EqualTo(3));
            Assert.That(verified.Token.Signatures.Select(x => x.Algorithm),
                Is.EqualTo(new string[] { "HS256", "PS256", "ES256" }));
            Assert.That(verified.Token.Signatures.Select(x => x.KeyId),
                Is.EqualTo(new string[] { "hmac", "rsa", "ec" }));
            Assert.That(verified.Token.Signatures.All(x => x.SignatureVerified),
                Is.True);
            Assert.That(verified.Token.Signatures[2].Signature,
                Has.Length.EqualTo(64));
        });
    }

    [Test]
    public void HmacEntriesAuthenticateEachExactProtectedSegment()
    {
        byte[] payload = Encoding.ASCII.GetBytes("same payload");
        byte[] firstKey = Sequence(32, 20);
        byte[] secondKey = Sequence(32, 40);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload,
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\",\"kid\":\"one\"}"),
                Header("{\"kid\":\"two\",\"alg\":\"HS256\"}")
            },
            null,
            new TokenKey[]
            {
                SymmetricKeyImporterOperations.Import(firstKey).Key,
                SymmetricKeyImporterOperations.Import(secondKey).Key
            },
            policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);
        string payloadSegment = document.RootElement.GetProperty("payload").GetString();
        JsonElement signatures = document.RootElement.GetProperty("signatures");

        string firstProtected = signatures[0].GetProperty("protected").GetString();
        string secondProtected = signatures[1].GetProperty("protected").GetString();
        byte[] firstExpected = HMACSHA256.HashData(
            firstKey,
            Encoding.ASCII.GetBytes(firstProtected + "." + payloadSegment));
        byte[] secondExpected = HMACSHA256.HashData(
            secondKey,
            Encoding.ASCII.GetBytes(secondProtected + "." + payloadSegment));

        Assert.Multiple(() =>
        {
            Assert.That(firstProtected, Is.Not.EqualTo(secondProtected));
            Assert.That(Base64Url.Decode(
                signatures[0].GetProperty("signature").GetString()).Bytes,
                Is.EqualTo(firstExpected));
            Assert.That(Base64Url.Decode(
                signatures[1].GetProperty("signature").GetString()).Bytes,
                Is.EqualTo(secondExpected));
        });
    }

    [Test]
    public void WriteParseAndDiagnosticReadPreserveMultipleEntries()
    {
        byte[] payload = new byte[] { 0, 1, 2, 255 };
        byte[][] signatures = new byte[][]
        {
            Sequence(32, 60),
            Sequence(32, 90)
        };
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult written = JsonWebSignatureGeneral.Write(
            payload,
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\"}"),
                Header("{\"alg\":\"HS256\"}")
            },
            new JoseHeader[] { Header("{\"kid\":\"a\"}"), null },
            signatures,
            policy);
        TokenPolicy diagnosticPolicy = Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;

        JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(
            written.Text, policy);
        JwsGeneralResult diagnostic = JsonWebSignatureGeneral.ReadDiagnostic(
            written.Text, diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.Signatures[0].Signature,
                Is.EqualTo(signatures[0]));
            Assert.That(parsed.Token.Signatures[1].Signature,
                Is.EqualTo(signatures[1]));
            Assert.That(parsed.Token.Signatures[0].KeyId, Is.EqualTo("a"));
            Assert.That(diagnostic.Token.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Token.Signatures.All(x => !x.SignatureVerified),
                Is.True);
        });
    }

    [Test]
    public void DetachedUnencodedPayloadSupportsMultipleSignatures()
    {
        byte[] payload = new byte[] { 0, 255, 46, 128, 10 };
        byte[] firstKey = Sequence(32, 110);
        byte[] secondKey = Sequence(32, 150);
        JoseHeader[] headers = new JoseHeader[]
        {
            Header("{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}"),
            Header("{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}")
        };
        TokenKey[] keys = new TokenKey[]
        {
            SymmetricKeyImporterOperations.Import(firstKey).Key,
            SymmetricKeyImporterOperations.Import(secondKey).Key
        };
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignatureGeneral.SignDetached(
            payload, headers, null, keys, policy);
        JwsGeneralResult verified = JsonWebSignatureGeneral.VerifyDetached(
            signed.Text, payload, keys, policy);
        JwsGeneralResult wrongMode = JsonWebSignatureGeneral.Verify(
            signed.Text, keys, policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(document.RootElement.TryGetProperty("payload", out _), Is.False);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.DetachedPayload, Is.True);
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.Signatures.All(x => x.SignatureVerified),
                Is.True);
            AssertFailure(wrongMode, "missing_payload");
        });
    }

    [Test]
    public void VerificationFailsClosedWhenAnySignatureOrPayloadIsChanged()
    {
        byte[] payload = Encoding.ASCII.GetBytes("protected by two keys");
        byte[] firstKey = Sequence(32, 3);
        byte[] secondKey = Sequence(32, 43);
        TokenKey[] keys = new TokenKey[]
        {
            SymmetricKeyImporterOperations.Import(firstKey).Key,
            SymmetricKeyImporterOperations.Import(secondKey).Key
        };
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload,
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\"}"),
                Header("{\"alg\":\"HS256\"}")
            },
            null,
            keys,
            policy);
        using JsonDocument document = JsonDocument.Parse(signed.Text);
        JsonElement root = document.RootElement;
        JsonElement signatures = root.GetProperty("signatures");
        byte[] changed = Base64Url.Decode(
            signatures[1].GetProperty("signature").GetString()).Bytes;
        changed[0] ^= 1;
        string signatureTampered = GeneralJson(
            root.GetProperty("payload").GetString(),
            new string[]
            {
                EntryJson(signatures[0]),
                EntryJson(signatures[1], Base64Url.Encode(changed).Text)
            });
        string payloadTampered = GeneralJson(
            Base64Url.Encode(Encoding.ASCII.GetBytes("changed payload")).Text,
            new string[] { EntryJson(signatures[0]), EntryJson(signatures[1]) });

        JwsGeneralResult wrongSignature = JsonWebSignatureGeneral.Verify(
            signatureTampered, keys, policy);
        JwsGeneralResult wrongPayload = JsonWebSignatureGeneral.Verify(
            payloadTampered, keys, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongSignature, "signature_mismatch");
            AssertFailure(wrongPayload, "signature_mismatch");
        });
    }

    [Test]
    public void MixedPayloadEncodingIsRejectedForCreationAndParsing()
    {
        byte[] payload = Encoding.ASCII.GetBytes("shared");
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 5)).Key;
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JoseHeader encoded = Header("{\"alg\":\"HS256\"}");
        JoseHeader raw = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenTextResult rejected = JsonWebSignatureGeneral.Sign(
            payload,
            new JoseHeader[] { encoded, raw },
            null,
            new TokenKey[] { key, key },
            policy);
        TokenTextResult encodedEntry = JsonWebSignatureFlattened.Write(
            payload, encoded, null, Sequence(32, 8), policy);
        TokenTextResult rawEntry = JsonWebSignatureFlattened.Write(
            payload, raw, null, Sequence(32, 9), policy);
        using JsonDocument first = JsonDocument.Parse(encodedEntry.Text);
        using JsonDocument second = JsonDocument.Parse(rawEntry.Text);
        string mixed = GeneralJson(
            first.RootElement.GetProperty("payload").GetString(),
            new string[] { EntryJson(first.RootElement), EntryJson(second.RootElement) });

        JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(mixed, policy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(rejected, "inconsistent_payload_encoding");
            AssertFailure(parsed, "inconsistent_payload_encoding");
        });
    }

    [TestCase("{}", "invalid_signatures")]
    [TestCase("{\"signatures\":[]}", "invalid_signatures")]
    [TestCase("{\"signatures\":[1]}", "invalid_signature_entry")]
    [TestCase("{\"signatures\":[{\"protected\":\"e30\",\"signature\":\"AA\",\"extra\":1}]}", "invalid_signature_entry")]
    [TestCase("{\"signature\":\"AA\",\"signatures\":[]}", "invalid_jws_json_member")]
    public void ParseRejectsMalformedGeneralShape(string json, string code)
    {
        JwsGeneralResult result = JsonWebSignatureGeneral.Parse(json, new TokenPolicy());

        AssertFailure(result, code);
    }

    [Test]
    public void CountsKeysAndPoliciesAreEnforced()
    {
        byte[] payload = new byte[] { 1 };
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 7)).Key;
        JoseHeader[] headers = new JoseHeader[]
        {
            Header("{\"alg\":\"HS256\"}"),
            Header("{\"alg\":\"HS256\"}")
        };
        TokenPolicy allowed = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult countMismatch = JsonWebSignatureGeneral.Sign(
            payload, headers, null, new TokenKey[] { key }, allowed);
        TokenTextResult headerCountMismatch = JsonWebSignatureGeneral.Write(
            payload, headers, new JoseHeader[] { null },
            new byte[][] { Sequence(32, 1), Sequence(32, 2) }, allowed);
        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload, headers, null, new TokenKey[] { key, key }, allowed);
        JwsGeneralResult keyMismatch = JsonWebSignatureGeneral.Verify(
            signed.Text, new TokenKey[] { key }, allowed);
        TokenPolicy denied = new TokenPolicy();
        JwsGeneralResult deniedResult = JsonWebSignatureGeneral.Verify(
            signed.Text, new TokenKey[] { key, key }, denied);

        Assert.Multiple(() =>
        {
            AssertTextFailure(countMismatch, "signature_count_mismatch");
            AssertTextFailure(headerCountMismatch, "signature_count_mismatch");
            AssertFailure(keyMismatch, "signature_key_count_mismatch");
            AssertFailure(deniedResult, "algorithm_not_allowed");
        });
    }

    [Test]
    public void UnsecuredGeneralJwsSupportsPolicyGatedCreationAndValidation()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}");
        JoseHeader[] headers = new JoseHeader[]
        {
            Header("{\"alg\":\"none\",\"typ\":\"JWT\"}"),
            Header("{\"typ\":\"JWT\",\"alg\":\"none\"}")
        };
        JoseHeader[] unprotected = new JoseHeader[]
        {
            Header("{\"kid\":\"first\"}"),
            null
        };
        TokenPolicy allowed = Policy(JoseAlgorithms.None);
        allowed.AllowUnsecuredTokens = true;
        TokenPolicy disabled = Policy(JoseAlgorithms.None);

        TokenTextResult created = JsonWebSignatureGeneral.CreateUnsecured(
            payload, headers, unprotected, allowed);
        JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(created.Text);
        JwsGeneralResult validated = JsonWebSignatureGeneral.ValidateUnsecured(
            created.Text, allowed);
        TokenTextResult deniedCreation = JsonWebSignatureGeneral.CreateUnsecured(
            payload, headers, unprotected, disabled);
        JwsGeneralResult deniedValidation = JsonWebSignatureGeneral.ValidateUnsecured(
            created.Text, disabled);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 82)).Key;
        JwsGeneralResult ordinaryVerification = JsonWebSignatureGeneral.Verify(
            created.Text, new TokenKey[] { key, key }, allowed);

        using JsonDocument document = JsonDocument.Parse(created.Text);
        JsonElement signatures = document.RootElement.GetProperty("signatures");
        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(signatures.GetArrayLength(), Is.EqualTo(2));
            Assert.That(signatures[0].GetProperty("signature").GetString(), Is.Empty);
            Assert.That(signatures[1].GetProperty("signature").GetString(), Is.Empty);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.Unsecured, Is.True);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.Signatures[0].Unsecured, Is.True);
            Assert.That(parsed.Token.Signatures[1].Unsecured, Is.True);
            Assert.That(parsed.Token.Signatures[0].KeyId, Is.EqualTo("first"));
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Token.Payload, Is.EqualTo(payload));
            Assert.That(validated.Token.Signatures.All(x => !x.SignatureVerified),
                Is.True);
            AssertTextFailure(deniedCreation, "unsecured_tokens_not_allowed");
            AssertFailure(deniedValidation, "unsecured_tokens_not_allowed");
            AssertFailure(ordinaryVerification, "unsecured_jws_not_supported");
        });
    }

    [Test]
    public void UnsecuredGeneralValidationRejectsSignedEntriesAndOrdinaryWriteBypass()
    {
        byte[] payload = new byte[] { 1 };
        JoseHeader signedHeader = Header("{\"alg\":\"HS256\"}");
        JoseHeader noneHeader = Header("{\"alg\":\"none\"}");
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 84)).Key;
        TokenPolicy signedPolicy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload,
            new JoseHeader[] { signedHeader },
            null,
            new TokenKey[] { key },
            signedPolicy);
        TokenPolicy unsecuredPolicy = Policy(JoseAlgorithms.None);
        unsecuredPolicy.AllowUnsecuredTokens = true;

        JwsGeneralResult rejected = JsonWebSignatureGeneral.ValidateUnsecured(
            signed.Text, unsecuredPolicy);
        TokenTextResult bypass = JsonWebSignatureGeneral.Write(
            payload,
            new JoseHeader[] { noneHeader },
            null,
            new byte[][] { Array.Empty<byte>() },
            unsecuredPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(rejected, "invalid_unsecured_algorithm");
            AssertTextFailure(bypass, "unsecured_jws_not_supported");
        });
    }

    [Test]
    public void DiagnosticReadAndSafetyLimitsAreEnforced()
    {
        byte[] payload = Sequence(20, 12);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 22)).Key;
        JoseHeader[] headers = new JoseHeader[]
        {
            Header("{\"alg\":\"HS256\"}"),
            Header("{\"alg\":\"HS256\"}")
        };
        TokenKey[] keys = new TokenKey[] { key, key };
        TokenPolicy generous = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureGeneral.Sign(
            payload, headers, null, keys, generous);
        TokenPolicy diagnostic = Policy(JoseAlgorithms.HmacSha256);
        diagnostic.AllowUnauthenticatedDecode = true;
        TokenPolicy small = Policy(JoseAlgorithms.HmacSha256);
        small.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(signed.Text) - 1;
        TokenPolicy shallow = Policy(JoseAlgorithms.HmacSha256);
        shallow.MaximumJsonDepth = 1;

        JwsGeneralResult deniedDiagnostic = JsonWebSignatureGeneral.ReadDiagnostic(
            signed.Text, generous);
        JwsGeneralResult allowedDiagnostic = JsonWebSignatureGeneral.ReadDiagnostic(
            signed.Text, diagnostic);
        JwsGeneralResult tooLarge = JsonWebSignatureGeneral.Parse(
            signed.Text, small);
        TokenTextResult tooDeep = JsonWebSignatureGeneral.Sign(
            payload, headers, null, keys, shallow);

        Assert.Multiple(() =>
        {
            AssertFailure(deniedDiagnostic, "unauthenticated_decode_not_allowed");
            Assert.That(allowedDiagnostic.Success, Is.True);
            Assert.That(allowedDiagnostic.Token.Payload, Is.EqualTo(payload));
            AssertFailure(tooLarge, "token_too_large");
            AssertTextFailure(tooDeep, "json_depth_exceeded");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        byte[] payload = new byte[] { 1 };
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 10)).Key;
        JoseHeader[] headers = new JoseHeader[] { Header("{\"alg\":\"HS256\"}") };
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebSignatureGeneral.Parse(null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Parse("{}", null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Sign(
                null, headers, null, new TokenKey[] { key }, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Sign(
                payload, null, null, new TokenKey[] { key }, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Sign(
                payload, headers, null, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Verify(
                null, new TokenKey[] { key }, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignatureGeneral.Verify(
                "{}", null, policy), Throws.ArgumentNullException);
        });
    }

    static string EntryJson(JsonElement entry)
    {
        return EntryJson(entry, entry.GetProperty("signature").GetString());
    }

    static string EntryJson(JsonElement entry, string signature)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"protected\":");
        builder.Append(JsonSerializer.Serialize(entry.GetProperty("protected").GetString()));
        if (entry.TryGetProperty("header", out JsonElement header))
        {
            builder.Append(",\"header\":");
            builder.Append(header.GetRawText());
        }

        builder.Append(",\"signature\":");
        builder.Append(JsonSerializer.Serialize(signature));
        builder.Append('}');
        return builder.ToString();
    }

    static string GeneralJson(string payload, string[] entries)
    {
        return "{\"payload\":" + JsonSerializer.Serialize(payload)
            + ",\"signatures\":[" + string.Join(',', entries) + "]}";
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

    static void AssertFailure(JwsGeneralResult result, string code)
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
