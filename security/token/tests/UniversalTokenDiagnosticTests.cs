using System.Text;
using token;

namespace token.Tests;

public class UniversalTokenDiagnosticTests
{
    [Test]
    public void SuccessfulValidationHasNoDiagnostics()
    {
        TokenKey key = HmacKey("key", 1);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = Sign("{\"sub\":\"alice\"}", key, policy);

        TokenValidationResult result = UniversalToken.Validate(
            signed.Text,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void DiagnosticsIdentifyPolicyInspectionAndKeyResolutionStages()
    {
        TokenKey key = HmacKey("key", 1);
        TokenPolicy invalid = Policy(JoseAlgorithms.HmacSha256);
        invalid.MaximumJsonDepth = 0;
        TokenValidationResult policyResult = UniversalToken.Validate(
            "token",
            new TokenKeyResolver(context => KeySuccess(key)),
            invalid);
        TokenValidationResult inspectionResult = UniversalToken.Validate(
            "opaque",
            new TokenKeyResolver(context => KeySuccess(key)),
            Policy(JoseAlgorithms.HmacSha256));
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}"),
            Header("{\"alg\":\"HS256\",\"kid\":\"key\"}"),
            null,
            key,
            policy);
        TokenValidationResult keyResult = UniversalToken.Validate(
            signed.Text,
            new TokenKeyResolver(context => KeyFailure("key_not_found")),
            policy);

        AssertDiagnostic(
            policyResult,
            TokenValidationDiagnosticStage.Policy,
            TokenKind.Unknown,
            -1,
            "invalid_policy");
        AssertDiagnostic(
            inspectionResult,
            TokenValidationDiagnosticStage.Inspection,
            TokenKind.Unknown,
            -1,
            "unknown_token");
        AssertDiagnostic(
            keyResult,
            TokenValidationDiagnosticStage.KeyResolution,
            TokenKind.JwsJson,
            -1,
            "key_not_found");

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void DiagnosticsDistinguishAuthenticationAndClaimFailures()
    {
        TokenKey signing = HmacKey("key", 1);
        TokenKey wrong = HmacKey("key", 50);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignatureFlattened.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}"),
            Header("{\"alg\":\"HS256\",\"kid\":\"key\"}"),
            null,
            signing,
            policy);
        TokenValidationResult authentication = UniversalToken.Validate(
            signed.Text,
            new TokenKeyResolver(context => KeySuccess(wrong)),
            policy);
        TokenPolicy required = Policy(JoseAlgorithms.HmacSha256);
        required.RequiredClaims = new[] { "iss" };
        TokenValidationResult claims = UniversalToken.Validate(
            signed.Text,
            new TokenKeyResolver(context => KeySuccess(signing)),
            required);

        AssertDiagnostic(
            authentication,
            TokenValidationDiagnosticStage.Authentication,
            TokenKind.JwsJson,
            -1,
            "signature_mismatch");
        AssertDiagnostic(
            claims,
            TokenValidationDiagnosticStage.Claims,
            TokenKind.JwsJson,
            -1,
            "missing_required_claim");

        signing.ReleaseOwnedMaterial();
        wrong.ReleaseOwnedMaterial();
    }

    [Test]
    public void GeneralJweDiagnosticsPreserveEveryRecipientAttempt()
    {
        TokenKey first = KeyWrapKey(1);
        TokenKey second = KeyWrapKey(40);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult encrypted = JsonWebEncryptionGeneral.Encrypt(
            Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}"),
            Header("{\"enc\":\"A128GCM\"}"),
            null,
            new JoseHeader[]
            {
                Header("{\"alg\":\"A128KW\",\"kid\":\"first\"}"),
                Header("{\"alg\":\"A128KW\",\"kid\":\"second\"}")
            },
            new TokenKey[] { first, second },
            policy);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            KeyFailure("missing_" + context.KeyId));

        TokenValidationResult result = UniversalToken.Validate(
            encrypted.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("missing_second"));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Stage,
                Is.EqualTo(TokenValidationDiagnosticStage.KeyResolution));
            Assert.That(result.Diagnostics[0].Kind,
                Is.EqualTo(TokenKind.JweJson));
            Assert.That(result.Diagnostics[0].ParticipantIndex, Is.EqualTo(0));
            Assert.That(result.Diagnostics[0].Error.Code,
                Is.EqualTo("missing_first"));
            Assert.That(result.Diagnostics[1].ParticipantIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Error,
                Is.SameAs(result.Error));
        });

        first.ReleaseOwnedMaterial();
        second.ReleaseOwnedMaterial();
    }

    [Test]
    public void BinaryValidationReportsDetectedKindWithUnderlyingFailure()
    {
        TokenKey key = CoseKey();
        TokenPolicy policy = Policy("HMAC 256/256");
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, CborText("alice"));
        TokenBytesResult token = CborWebToken.Create(claims, key, policy);

        TokenValidationResult result = UniversalToken.ValidateBytes(
            token.Bytes,
            new TokenKeyResolver(context => KeyFailure("cwt_key_missing")),
            policy);

        AssertDiagnostic(
            result,
            TokenValidationDiagnosticStage.Validation,
            TokenKind.Cose,
            -1,
            "cwt_key_missing");

        key.ReleaseOwnedMaterial();
    }

    static TokenTextResult Sign(
        string claims,
        TokenKey key,
        TokenPolicy policy)
    {
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(claims),
            Header("{\"alg\":\"HS256\",\"kid\":\"key\"}"),
            key,
            policy);
    }

    static TokenKey HmacKey(string keyId, int seed)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, seed),
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = keyId;
        return key;
    }

    static TokenKey KeyWrapKey(int seed)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, seed),
            JoseAlgorithms.Aes128KeyWrap).Key;
        return key;
    }

    static TokenKey CoseKey()
    {
        return SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256).Key;
    }

    static byte[] Sequence(int count, int seed)
    {
        return Enumerable.Range(seed, count)
            .Select(value => (byte)value)
            .ToArray();
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static CborItem CborText(string text)
    {
        CborItem result = new CborItem();
        result.Kind = TokenCborValueKind.TextString;
        result.Text = text;
        return result;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy result = new TokenPolicy();
        result.AllowedAlgorithms = algorithms;
        return result;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static TokenKeyResult KeyFailure(string code)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, "Key resolution failed.", -1);
        return result;
    }

    static void AssertDiagnostic(
        TokenValidationResult result,
        TokenValidationDiagnosticStage stage,
        TokenKind kind,
        int participantIndex,
        string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Stage, Is.EqualTo(stage));
            Assert.That(result.Diagnostics[0].Kind, Is.EqualTo(kind));
            Assert.That(result.Diagnostics[0].ParticipantIndex,
                Is.EqualTo(participantIndex));
            Assert.That(result.Diagnostics[0].Error, Is.SameAs(result.Error));
        });
    }
}
