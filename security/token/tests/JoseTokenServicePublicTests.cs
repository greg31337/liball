using System.Text;
using token;

namespace token.Tests;

public class JoseTokenServicePublicTests
{
    [Test]
    public void HeadersRoundTripAndCriticalExtensionsRequireUnderstanding()
    {
        IJoseTokenOperations service = new JoseTokenService();
        JoseHeaderResult parsed = service.ReadJoseHeader(
            "{\"alg\":\"HS256\",\"crit\":[\"custom\"],\"custom\":true}",
            8);
        TokenTextResult text = service.WriteJoseHeader(parsed.Header!);
        TokenBytesResult utf8 = service.WriteJoseHeaderUtf8(parsed.Header!);
        JoseHeaderResult reparsed = service.ReadJoseHeaderUtf8(utf8.Bytes!, 8);
        TokenResult rejected = service.ValidateJoseCriticalHeaders(
            parsed.Header!,
            Array.Empty<JoseHeader>(),
            Array.Empty<string>());
        TokenResult accepted = service.ValidateJoseCriticalHeaders(
            parsed.Header!,
            Array.Empty<JoseHeader>(),
            new string[] { "custom" });
        JoseHeaderResult duplicate = service.ReadJoseHeader(
            "{\"alg\":\"HS256\",\"alg\":\"HS384\"}",
            8);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(text.Text, Is.EqualTo(
                "{\"alg\":\"HS256\",\"crit\":[\"custom\"],\"custom\":true}"));
            Assert.That(reparsed.Success, Is.True);
            AssertFailure(rejected.Error, "unknown_critical_header");
            Assert.That(accepted.Success, Is.True);
            AssertFailure(duplicate.Error, "duplicate_json_member");
        });
    }

    [Test]
    public void CompactJwsWithholdsPayloadUntilAuthorizedOrVerified()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = Encoding.UTF8.GetBytes("authenticated payload");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenKey key = Key(32, 11, JoseAlgorithms.HmacSha256);
        JoseHeader header = Header(service, "{\"alg\":\"HS256\"}");

        TokenTextResult signed = service.SignJwsCompact(
            payload,
            header,
            key,
            policy);
        TokenTextResult spanSigned = service.SignJwsCompactSpan(
            payload.AsSpan(),
            header,
            key,
            policy);
        JwsCompactResult parsed = service.ReadJwsCompact(
            signed.Text!,
            policy);
        JwsCompactResult deniedDiagnostic =
            service.ReadJwsCompactDiagnostic(signed.Text!, policy);
        TokenPolicy diagnosticPolicy = Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JwsCompactResult diagnostic = service.ReadJwsCompactDiagnostic(
            signed.Text!,
            diagnosticPolicy);
        JwsCompactResult verified = service.VerifyJwsCompact(
            signed.Text!,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(spanSigned.Success, Is.True);
            Assert.That(spanSigned.Text, Is.EqualTo(signed.Text));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token!.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.Payload, Is.Null);
            AssertFailure(
                deniedDiagnostic.Error,
                "unauthenticated_decode_not_allowed");
            Assert.That(diagnostic.Token!.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Token.SignatureVerified, Is.False);
            Assert.That(verified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.SignatureVerified, Is.True);
        });
    }

    [Test]
    public void DetachedUnencodedCompactJwsSupportsSpanPaths()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] selected = Encoding.UTF8.GetBytes("raw.detached.payload");
        byte[] container = new byte[selected.Length + 2];
        selected.CopyTo(container, 1);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.UnderstoodCriticalHeaders = new string[] { "b64" };
        TokenKey key = Key(32, 21, JoseAlgorithms.HmacSha256);
        JoseHeader header = Header(
            service,
            "{\"alg\":\"HS256\",\"b64\":false,\"crit\":[\"b64\"]}");

        TokenTextResult signed = service.SignDetachedJwsCompactSpan(
            container.AsSpan(1, selected.Length),
            header,
            key,
            policy);
        TokenTextResult arraySigned = service.SignDetachedJwsCompact(
            selected,
            header,
            key,
            policy);
        JwsCompactResult verified =
            service.VerifyDetachedJwsCompactSpan(
                signed.Text!,
                container.AsSpan(1, selected.Length),
                key,
                policy);
        JwsCompactResult wrongPayload = service.VerifyDetachedJwsCompact(
            signed.Text!,
            Encoding.UTF8.GetBytes("raw.detached.changed"),
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(arraySigned.Text, Is.EqualTo(signed.Text));
            Assert.That(signed.Text, Does.Contain(".."));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token!.Payload, Is.EqualTo(selected));
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.False);
            AssertFailure(wrongPayload.Error, "signature_mismatch");
        });
    }

    [Test]
    public void FlattenedJwsSupportsAttachedDetachedAndComponentWriting()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = Encoding.UTF8.GetBytes("flattened payload");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenKey key = Key(32, 31, JoseAlgorithms.HmacSha256);
        JoseHeader protectedHeader = Header(
            service,
            "{\"alg\":\"HS256\"}");
        JoseHeader unprotectedHeader = Header(
            service,
            "{\"kid\":\"flattened-key\"}");

        TokenTextResult attached = service.SignJwsFlattened(
            payload,
            protectedHeader,
            unprotectedHeader,
            key,
            policy);
        JwsFlattenedResult parsed = service.ReadJwsFlattened(
            attached.Text!,
            policy);
        TokenTextResult rewritten = service.WriteJwsFlattened(
            payload,
            protectedHeader,
            unprotectedHeader,
            parsed.Token!.Signature!,
            policy);
        JwsFlattenedResult verified = service.VerifyJwsFlattened(
            rewritten.Text!,
            key,
            policy);
        TokenPolicy diagnosticPolicy = Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JwsFlattenedResult diagnostic =
            service.ReadJwsFlattenedDiagnostic(
                attached.Text!,
                diagnosticPolicy);
        TokenTextResult detached = service.SignDetachedJwsFlattened(
            payload,
            protectedHeader,
            unprotectedHeader,
            key,
            policy);
        JwsFlattenedResult detachedParsed =
            service.ReadJwsFlattened(detached.Text!, policy);
        TokenTextResult detachedRewritten =
            service.WriteDetachedJwsFlattened(
                protectedHeader,
                unprotectedHeader,
                detachedParsed.Token!.Signature!,
                policy);
        JwsFlattenedResult detachedVerified =
            service.VerifyDetachedJwsFlattened(
                detachedRewritten.Text!,
                payload,
                key,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(rewritten.Success, Is.True);
            Assert.That(verified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.KeyId, Is.EqualTo("flattened-key"));
            Assert.That(diagnostic.Token!.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Token.SignatureVerified, Is.False);
            Assert.That(detached.Text, Does.Not.Contain("\"payload\""));
            Assert.That(detachedRewritten.Success, Is.True);
            Assert.That(detachedVerified.Token!.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void GeneralJwsRequiresEverySignatureAndSupportsMultipleKeys()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = Encoding.UTF8.GetBytes("shared payload");
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.HmacSha384);
        TokenKey firstKey = Key(32, 41, JoseAlgorithms.HmacSha256);
        TokenKey secondKey = Key(48, 51, JoseAlgorithms.HmacSha384);
        JoseHeader firstHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"kid\":\"first\"}");
        JoseHeader secondHeader = Header(
            service,
            "{\"alg\":\"HS384\",\"kid\":\"second\"}");

        TokenTextResult signed = service.SignJwsGeneral(
            payload,
            new JoseHeader[] { firstHeader, secondHeader },
            null,
            new TokenKey[] { firstKey, secondKey },
            policy);
        JwsGeneralResult parsed = service.ReadJwsGeneral(
            signed.Text!,
            policy);
        JwsGeneralResult verified = service.VerifyJwsGeneral(
            signed.Text!,
            new TokenKey[] { firstKey, secondKey },
            policy);
        JwsGeneralResult wrongKey = service.VerifyJwsGeneral(
            signed.Text!,
            new TokenKey[] { firstKey, firstKey },
            policy);
        byte[][] signatures = new byte[][]
        {
            parsed.Token!.Signatures[0].Signature!,
            parsed.Token.Signatures[1].Signature!
        };
        TokenTextResult rewritten = service.WriteJwsGeneral(
            payload,
            new JoseHeader[] { firstHeader, secondHeader },
            null,
            signatures,
            policy);
        TokenTextResult detached = service.SignDetachedJwsGeneral(
            payload,
            new JoseHeader[] { firstHeader, secondHeader },
            null,
            new TokenKey[] { firstKey, secondKey },
            policy);
        JwsGeneralResult detachedVerified =
            service.VerifyDetachedJwsGeneral(
                detached.Text!,
                payload,
                new TokenKey[] { firstKey, secondKey },
                policy);
        TokenTextResult detachedWritten = service.WriteDetachedJwsGeneral(
            new JoseHeader[] { firstHeader, secondHeader },
            null,
            signatures,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.HmacSha384);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JwsGeneralResult diagnostic = service.ReadJwsGeneralDiagnostic(
            signed.Text!,
            diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Token.Signatures, Has.Length.EqualTo(2));
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(
                verified.Token.Signatures[0].SignatureVerified,
                Is.True);
            Assert.That(
                verified.Token.Signatures[1].SignatureVerified,
                Is.True);
            Assert.That(wrongKey.Success, Is.False);
            Assert.That(rewritten.Success, Is.True);
            Assert.That(detachedVerified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(detachedWritten.Text, Does.Not.Contain("\"payload\""));
            Assert.That(diagnostic.Token!.Payload, Is.EqualTo(payload));
            Assert.That(
                diagnostic.Token.Signatures[0].SignatureVerified,
                Is.False);
        });
    }

    [Test]
    public void AttachedUnencodedPayloadsRoundTripAcrossEveryJwsForm()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = Encoding.UTF8.GetBytes("unencoded shared payload");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.UnderstoodCriticalHeaders = new string[] { "b64" };
        TokenKey firstKey = Key(32, 71, JoseAlgorithms.HmacSha256);
        TokenKey secondKey = Key(32, 81, JoseAlgorithms.HmacSha256);
        JoseHeader firstHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"kid\":\"first\","
            + "\"b64\":false,\"crit\":[\"b64\"]}");
        JoseHeader secondHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"kid\":\"second\","
            + "\"b64\":false,\"crit\":[\"b64\"]}");

        TokenTextResult compact = service.SignJwsCompact(
            payload,
            firstHeader,
            firstKey,
            policy);
        JwsCompactResult compactVerified = service.VerifyJwsCompact(
            compact.Text!,
            firstKey,
            policy);
        TokenTextResult flattened = service.SignJwsFlattened(
            payload,
            firstHeader,
            null,
            firstKey,
            policy);
        JwsFlattenedResult flattenedVerified = service.VerifyJwsFlattened(
            flattened.Text!,
            firstKey,
            policy);
        TokenTextResult general = service.SignJwsGeneral(
            payload,
            new JoseHeader[] { firstHeader, secondHeader },
            null,
            new TokenKey[] { firstKey, secondKey },
            policy);
        JwsGeneralResult generalVerified = service.VerifyJwsGeneral(
            general.Text!,
            new TokenKey[] { firstKey, secondKey },
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(compact.Success, Is.True);
            Assert.That(compact.Text, Does.Contain(".unencoded shared payload."));
            Assert.That(compactVerified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(
                compactVerified.Token.PayloadBase64UrlEncoded,
                Is.False);
            Assert.That(flattened.Success, Is.True);
            Assert.That(
                flattened.Text,
                Does.Contain("\"payload\":\"unencoded shared payload\""));
            Assert.That(flattenedVerified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(
                flattenedVerified.Token.PayloadBase64UrlEncoded,
                Is.False);
            Assert.That(general.Success, Is.True);
            Assert.That(generalVerified.Token!.Payload, Is.EqualTo(payload));
            Assert.That(
                generalVerified.Token.PayloadBase64UrlEncoded,
                Is.False);
            Assert.That(
                generalVerified.Token.Signatures[0].SignatureVerified,
                Is.True);
            Assert.That(
                generalVerified.Token.Signatures[1].SignatureVerified,
                Is.True);
        });
    }

    [Test]
    public void JwsHeaderPlacementAndCriticalProcessingFailClosed()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = new byte[] { 1, 2, 3 };
        TokenKey key = Key(32, 91, JoseAlgorithms.HmacSha256);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JoseHeader protectedHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"kid\":\"protected\"}");
        JoseHeader emptyHeader = Header(service, "{}");

        TokenTextResult overlap = service.SignJwsFlattened(
            payload,
            protectedHeader,
            Header(service, "{\"kid\":\"unprotected\"}"),
            key,
            policy);
        TokenTextResult unprotectedAlgorithm = service.SignJwsFlattened(
            payload,
            emptyHeader,
            Header(service, "{\"alg\":\"HS256\"}"),
            key,
            policy);
        TokenTextResult unprotectedCritical = service.SignJwsFlattened(
            payload,
            protectedHeader,
            Header(service, "{\"crit\":[\"custom\"],\"custom\":true}"),
            key,
            policy);
        JoseHeader criticalHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"crit\":[\"custom\"],"
            + "\"custom\":true}");
        TokenTextResult unknownCritical = service.SignJwsCompact(
            payload,
            criticalHeader,
            key,
            policy);
        TokenPolicy understood = Policy(JoseAlgorithms.HmacSha256);
        understood.UnderstoodCriticalHeaders = new string[] { "custom" };
        TokenTextResult accepted = service.SignJwsCompact(
            payload,
            criticalHeader,
            key,
            understood);
        JwsCompactResult verified = service.VerifyJwsCompact(
            accepted.Text!,
            key,
            understood);

        Assert.Multiple(() =>
        {
            AssertFailure(overlap.Error, "duplicate_header_parameter");
            AssertFailure(unprotectedAlgorithm.Error, "missing_algorithm");
            AssertFailure(
                unprotectedCritical.Error,
                "critical_header_not_protected");
            AssertFailure(unknownCritical.Error, "unknown_critical_header");
            Assert.That(accepted.Success, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token!.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void MalformedAndAdversarialJwsInputsReturnStableFailures()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenKey key = Key(32, 101, JoseAlgorithms.HmacSha256);
        JoseHeader header = Header(service, "{\"alg\":\"HS256\"}");
        byte[] payload = Encoding.UTF8.GetBytes("payload not for attackers");
        TokenTextResult signed = service.SignJwsCompact(
            payload,
            header,
            key,
            policy);
        string tampered = signed.Text!.Replace(
            "cGF5bG9hZCBub3QgZm9yIGF0dGFja2Vycw",
            "YXR0YWNrZXItY29udHJvbGxlZA");

        JwsCompactResult badCompact = service.ReadJwsCompact(
            "only.two",
            policy);
        JwsCompactResult tamperedCompact = service.VerifyJwsCompact(
            tampered,
            key,
            policy);
        JwsFlattenedResult badFlattened = service.ReadJwsFlattened(
            "{\"payload\":\"AA\",\"protected\":1,\"signature\":\"AA\"}",
            policy);
        JwsGeneralResult badGeneral = service.ReadJwsGeneral(
            "{\"signatures\":[{\"protected\":\"e30\","
            + "\"signature\":\"AA\",\"extra\":true}]}",
            policy);
        TokenTextResult flattened = service.SignJwsFlattened(
            payload,
            header,
            null,
            key,
            policy);
        TokenTextResult general = service.SignJwsGeneral(
            payload,
            new JoseHeader[] { header },
            null,
            new TokenKey[] { key },
            policy);
        JwsFlattenedResult deniedFlattenedDiagnostic =
            service.ReadJwsFlattenedDiagnostic(
                flattened.Text!,
                policy);
        JwsGeneralResult deniedGeneralDiagnostic =
            service.ReadJwsGeneralDiagnostic(
                general.Text!,
                policy);

        Assert.Multiple(() =>
        {
            AssertFailure(
                badCompact.Error,
                "invalid_compact_token");
            AssertFailure(tamperedCompact.Error, "signature_mismatch");
            Assert.That(tamperedCompact.Token, Is.Null);
            AssertFailure(badFlattened.Error, "invalid_protected_header");
            AssertFailure(badGeneral.Error, "invalid_signature_entry");
            AssertFailure(
                deniedFlattenedDiagnostic.Error,
                "unauthenticated_decode_not_allowed");
            AssertFailure(
                deniedGeneralDiagnostic.Error,
                "unauthenticated_decode_not_allowed");
        });
    }

    [Test]
    public void UnsecuredSerializationsRequireExplicitPolicyOptIn()
    {
        IJoseTokenOperations service = new JoseTokenService();
        byte[] payload = Encoding.UTF8.GetBytes("public unsecured payload");
        JoseHeader header = Header(service, "{\"alg\":\"none\"}");
        TokenPolicy denied = new TokenPolicy();
        TokenPolicy allowed = Policy(JoseAlgorithms.None);
        allowed.AllowUnsecuredTokens = true;

        TokenTextResult deniedCompact = service.CreateUnsecuredJwsCompact(
            payload,
            header,
            denied);
        TokenTextResult compact = service.CreateUnsecuredJwsCompactSpan(
            payload.AsSpan(),
            header,
            allowed);
        JwsCompactResult compactValidated =
            service.ValidateUnsecuredJwsCompact(compact.Text!, allowed);
        TokenTextResult flattened = service.CreateUnsecuredJwsFlattened(
            payload,
            header,
            null,
            allowed);
        JwsFlattenedResult flattenedValidated =
            service.ValidateUnsecuredJwsFlattened(
                flattened.Text!,
                allowed);
        TokenTextResult general = service.CreateUnsecuredJwsGeneral(
            payload,
            new JoseHeader[] { header, header },
            null,
            allowed);
        JwsGeneralResult generalValidated =
            service.ValidateUnsecuredJwsGeneral(general.Text!, allowed);

        Assert.Multiple(() =>
        {
            AssertFailure(
                deniedCompact.Error,
                "unsecured_tokens_not_allowed");
            Assert.That(compactValidated.Token!.Payload, Is.EqualTo(payload));
            Assert.That(compactValidated.Token.Unsecured, Is.True);
            Assert.That(flattenedValidated.Token!.Payload, Is.EqualTo(payload));
            Assert.That(generalValidated.Token!.Payload, Is.EqualTo(payload));
            Assert.That(generalValidated.Token.Signatures, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void AuthenticatedCompactJwsCanDirectNestedParsing()
    {
        IJoseTokenOperations service = new JoseTokenService();
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenKey key = Key(32, 61, JoseAlgorithms.HmacSha256);
        JoseHeader innerHeader = Header(
            service,
            "{\"alg\":\"HS256\"}");
        TokenTextResult inner = service.SignJwsCompact(
            Encoding.UTF8.GetBytes("inner"),
            innerHeader,
            key,
            policy);
        JoseHeader outerHeader = Header(
            service,
            "{\"alg\":\"HS256\",\"cty\":\"jose\"}");
        TokenTextResult outer = service.SignJwsCompact(
            Encoding.UTF8.GetBytes(inner.Text!),
            outerHeader,
            key,
            policy);
        JwsCompactResult verifiedOuter = service.VerifyJwsCompact(
            outer.Text!,
            key,
            policy);

        NestedJoseResult nested = service.ReadNestedJwsCompact(
            verifiedOuter,
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
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => service.ReadJoseHeader(null!, 8),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.SignJwsCompact(
                    Array.Empty<byte>(),
                    null!,
                    Key(32, 1, JoseAlgorithms.HmacSha256),
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadJwsGeneral("{}", null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadNestedJwsFlattened(
                    null!,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadNestedJwsGeneral(
                    null!,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadNestedJweFlattened(
                    null!,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadNestedJweGeneral(
                    null!,
                    policy),
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
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index++;
        }

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes;
        key.Algorithm = algorithm;
        return key;
    }

    static TokenPolicy Policy(string firstAlgorithm)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[] { firstAlgorithm };
        return policy;
    }

    static TokenPolicy Policy(string firstAlgorithm, string secondAlgorithm)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[]
        {
            firstAlgorithm,
            secondAlgorithm
        };
        return policy;
    }

    static void AssertFailure(TokenError? error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.Code, Is.EqualTo(code));
    }
}
