using System.Text;
using token;

namespace token.Tests;

public class UniversalTokenTests
{
    [Test]
    public void InspectReturnsOnlyUnauthenticatedCompactJoseHeaders()
    {
        string jws = CompactJws("sig-key", "secret payload");
        string jweHeader = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"kid\":\"enc-key\"}"))
            .Text;
        string jwe = jweHeader + "..AA.c2VjcmV0.AA";
        TokenPolicy policy = new TokenPolicy();

        TokenInspectionResult signature = UniversalToken.Inspect(jws, policy);
        TokenInspectionResult encryption = UniversalToken.Inspect(jwe, policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signature, TokenKind.JwsCompact, 1);
            Assert.That(signature.HeadersAuthenticated, Is.False);
            AssertClaimsUnavailable(signature, null);
            AssertJoseHeader(
                signature.Headers[0],
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected,
                "sig-key");
            AssertSuccess(encryption, TokenKind.JweCompact, 1);
            Assert.That(encryption.HeadersAuthenticated, Is.False);
            AssertClaimsUnavailable(encryption, null);
            AssertJoseHeader(
                encryption.Headers[0],
                TokenHeaderRole.Message,
                TokenHeaderProtection.Protected,
                "enc-key");
        });
    }

    [Test]
    public void OptInInspectsCompactJwtClaimsWithoutAuthenticatingThem()
    {
        TokenPolicy policy = DiagnosticPolicy();
        string token = CompactJws(
            "claims-key",
            "{\"iss\":\"issuer\",\"sub\":\"alice\"}");

        TokenInspectionResult result = UniversalToken.Inspect(token, policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.JwsCompact, 1);
            AssertJwtClaims(result, "alice");
            Assert.That(result.Headers[0].JoseHeader.Get("kid").StringValue,
                Is.EqualTo("claims-key"));
        });
    }

    [Test]
    public void OptInInspectsFlattenedAndGeneralJwsClaims()
    {
        string claims = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"sub\":\"alice\"}")).Text;
        string protectedHeader = ProtectedHeader("claims-key");
        string flattened =
            "{\"payload\":\"" + claims + "\",\"protected\":\""
            + protectedHeader + "\",\"signature\":\"AA\"}";
        string general =
            "{\"payload\":\"" + claims + "\",\"signatures\":[{"
            + "\"protected\":\"" + protectedHeader
            + "\",\"signature\":\"AA\"}]}";
        TokenPolicy policy = DiagnosticPolicy();

        TokenInspectionResult flattenedResult = UniversalToken.Inspect(
            flattened,
            policy);
        TokenInspectionResult generalResult = UniversalToken.Inspect(
            general,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(flattenedResult, TokenKind.JwsJson, 1);
            AssertJwtClaims(flattenedResult, "alice");
            AssertSuccess(generalResult, TokenKind.JwsJson, 1);
            AssertJwtClaims(generalResult, "alice");
        });
    }

    [Test]
    public void OptInInspectsIssuerClaimsWithoutApplyingSdJwtDisclosures()
    {
        string issuer = CompactJws(
            "issuer-key",
            "{\"sub\":\"alice\",\"_sd\":[\"unverified-digest\"]}");

        TokenInspectionResult result = UniversalToken.Inspect(
            issuer + "~",
            DiagnosticPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.SdJwt, 1);
            AssertJwtClaims(result, "alice");
            Assert.That(result.JwtClaims.Get("_sd"), Is.Not.Null);
        });
    }

    [Test]
    public void InspectReturnsEveryGeneralJwsHeaderInSerializationOrder()
    {
        string first = ProtectedHeader("first");
        string second = ProtectedHeader("second");
        string json =
            "{\"payload\":\"c2VjcmV0\",\"signatures\":["
            + "{\"protected\":\"" + first
            + "\",\"header\":{\"hint\":\"one\"},\"signature\":\"AA\"},"
            + "{\"protected\":\"" + second
            + "\",\"header\":{\"hint\":\"two\"},\"signature\":\"AQ\"}]}";

        TokenInspectionResult result = UniversalToken.Inspect(
            json,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.JwsJson, 4);
            AssertJoseHeader(
                result.Headers[0],
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected,
                "first");
            Assert.That(
                result.Headers[1].JoseHeader.Get("hint").StringValue,
                Is.EqualTo("one"));
            AssertJoseHeader(
                result.Headers[2],
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected,
                "second");
            Assert.That(
                result.Headers[3].JoseHeader.Get("hint").StringValue,
                Is.EqualTo("two"));
            Assert.That(result.Headers[1].Protection,
                Is.EqualTo(TokenHeaderProtection.Unprotected));
            Assert.That(result.Headers[3].Protection,
                Is.EqualTo(TokenHeaderProtection.Unprotected));
        });
    }

    [Test]
    public void InspectDistinguishesSharedAndRecipientJweHeaders()
    {
        string protectedHeader = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"enc\":\"A128GCM\"}")) .Text;
        string json =
            "{\"protected\":\"" + protectedHeader + "\","
            + "\"unprotected\":{\"typ\":\"JWT\"},"
            + "\"header\":{\"alg\":\"dir\",\"kid\":\"recipient-key\"},"
            + "\"iv\":\"AA\",\"ciphertext\":\"c2VjcmV0\",\"tag\":\"AA\"}";

        TokenInspectionResult result = UniversalToken.Inspect(
            json,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.JweJson, 3);
            Assert.That(result.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(result.Headers[0].Protection,
                Is.EqualTo(TokenHeaderProtection.Protected));
            Assert.That(result.Headers[1].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(result.Headers[1].Protection,
                Is.EqualTo(TokenHeaderProtection.Unprotected));
            AssertJoseHeader(
                result.Headers[2],
                TokenHeaderRole.Recipient,
                TokenHeaderProtection.Unprotected,
                "recipient-key");
        });
    }

    [Test]
    public void InspectSupportsFlattenedJwsAndGeneralJweJson()
    {
        string protectedJws = ProtectedHeader("flattened-key");
        string flattenedJws =
            "{\"payload\":\"c2VjcmV0\",\"protected\":\""
            + protectedJws
            + "\",\"header\":{\"hint\":\"visible\"},\"signature\":\"AA\"}";
        string protectedJwe = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"enc\":\"A128GCM\"}")).Text;
        string generalJwe =
            "{\"protected\":\"" + protectedJwe + "\","
            + "\"recipients\":["
            + "{\"header\":{\"alg\":\"dir\",\"kid\":\"first\"},"
            + "\"encrypted_key\":\"\"},"
            + "{\"header\":{\"alg\":\"dir\",\"kid\":\"second\"},"
            + "\"encrypted_key\":\"\"}],"
            + "\"iv\":\"AA\",\"ciphertext\":\"c2VjcmV0\",\"tag\":\"AA\"}";

        TokenInspectionResult signature = UniversalToken.Inspect(
            flattenedJws,
            new TokenPolicy());
        TokenInspectionResult encryption = UniversalToken.Inspect(
            generalJwe,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(signature, TokenKind.JwsJson, 2);
            AssertJoseHeader(
                signature.Headers[0],
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected,
                "flattened-key");
            Assert.That(signature.Headers[1].Protection,
                Is.EqualTo(TokenHeaderProtection.Unprotected));
            AssertSuccess(encryption, TokenKind.JweJson, 3);
            AssertJoseHeader(
                encryption.Headers[1],
                TokenHeaderRole.Recipient,
                TokenHeaderProtection.Unprotected,
                "first");
            AssertJoseHeader(
                encryption.Headers[2],
                TokenHeaderRole.Recipient,
                TokenHeaderProtection.Unprotected,
                "second");
        });
    }

    [Test]
    public void InspectSelectiveDisclosureIncludesIssuerAndKeyBindingHeaders()
    {
        string issuer = CompactJws("issuer-key", "issuer payload");
        string binding = CompactJws("binding-key", "binding payload");

        TokenInspectionResult result = UniversalToken.Inspect(
            issuer + "~" + binding,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.SdJwt, 2);
            AssertJoseHeader(
                result.Headers[0],
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected,
                "issuer-key");
            AssertJoseHeader(
                result.Headers[1],
                TokenHeaderRole.KeyBinding,
                TokenHeaderProtection.Protected,
                "binding-key");
        });
    }

    [Test]
    public void InspectBytesReturnsCoseAndCwtHeadersWithoutPayloadModels()
    {
        byte[] cose = Convert.FromHexString(
            "D28441A0A201260442313154546869732069732074686520636F6E74656E742E"
            + "584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425"
            + "317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F");
        byte[] cwt = new byte[cose.Length + 2];
        cwt[0] = 0xD8;
        cwt[1] = 0x3D;
        cose.CopyTo(cwt, 2);
        TokenPolicy policy = new TokenPolicy();

        TokenInspectionResult coseResult = UniversalToken.InspectBytes(
            cose,
            policy);
        TokenInspectionResult cwtResult = UniversalToken.InspectBytes(
            cwt,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(coseResult, TokenKind.Cose, 1);
            AssertSuccess(cwtResult, TokenKind.Cwt, 1);
            AssertCoseHeader(coseResult.Headers[0]);
            AssertCoseHeader(cwtResult.Headers[0]);
            Assert.That(coseResult.HeadersAuthenticated, Is.False);
            Assert.That(cwtResult.HeadersAuthenticated, Is.False);
        });
    }

    [Test]
    public void OptInInspectsCwtClaimsFromCoseAndTaggedCwt()
    {
        byte[] cose = CoseSign1Claims("alice");
        byte[] cwt = CwtTagged(cose);
        TokenPolicy policy = DiagnosticPolicy();

        TokenInspectionResult coseResult = UniversalToken.InspectBytes(
            cose,
            policy);
        TokenInspectionResult cwtResult = UniversalToken.InspectBytes(
            cwt,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(coseResult, TokenKind.Cose, 1);
            AssertCwtClaims(coseResult, "alice");
            AssertSuccess(cwtResult, TokenKind.Cwt, 1);
            AssertCwtClaims(cwtResult, "alice");
        });
    }

    [Test]
    public void OptInTraversesUnencryptedNestedCwtProtection()
    {
        CoseMac0 outer = new CoseMac0();
        outer.Payload = CoseSign1Claims("nested-subject");
        outer.Tag = new byte[] { 9 };
        TokenBytesResult written = outer.Write();

        TokenInspectionResult result = UniversalToken.InspectBytes(
            CwtTagged(written.Bytes),
            DiagnosticPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.Cwt, 1);
            AssertCwtClaims(result, "nested-subject");
        });
    }

    [Test]
    public void OptInInspectsClaimsAcrossUnencryptedCoseMessageKinds()
    {
        byte[] payload = CwtClaimsBytes("all-kinds");
        CoseMac0 mac0 = new CoseMac0();
        mac0.Payload = payload;
        mac0.Tag = new byte[] { 1 };
        CoseSign sign = new CoseSign();
        sign.Payload = payload;
        CoseSignature signature = new CoseSignature();
        signature.Signature = new byte[] { 2 };
        sign.Signatures = new CoseSignature[] { signature };
        CoseMac mac = new CoseMac();
        mac.Payload = payload;
        mac.Tag = new byte[] { 3 };
        mac.Recipients = new CoseRecipient[] { new CoseRecipient() };
        TokenBytesResult mac0Bytes = mac0.Write();
        TokenBytesResult signBytes = sign.Write();
        TokenBytesResult macBytes = mac.Write();
        TokenPolicy policy = DiagnosticPolicy();

        TokenInspectionResult mac0Result = UniversalToken.InspectBytes(
            mac0Bytes.Bytes,
            policy);
        TokenInspectionResult signResult = UniversalToken.InspectBytes(
            signBytes.Bytes,
            policy);
        TokenInspectionResult macResult = UniversalToken.InspectBytes(
            macBytes.Bytes,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(mac0Bytes.Success, Is.True);
            Assert.That(signBytes.Success, Is.True);
            Assert.That(macBytes.Success, Is.True);
            AssertCwtClaims(mac0Result, "all-kinds");
            AssertCwtClaims(signResult, "all-kinds");
            AssertCwtClaims(macResult, "all-kinds");
        });
    }

    [Test]
    public void EncryptedAndDetachedPayloadsNeverExposeClaims()
    {
        string jweHeader = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}")).Text;
        string jwe = jweHeader + "..AA.e30.AA";
        string detached =
            "{\"protected\":\"" + ProtectedHeader("detached")
            + "\",\"signature\":\"AA\"}";
        CoseEncrypt0 encrypted = new CoseEncrypt0();
        encrypted.Ciphertext = new byte[] { 0xA1, 0x02, 0x65, 0x61, 0x6C, 0x69, 0x63, 0x65 };
        TokenBytesResult encryptedBytes = encrypted.Write();
        TokenPolicy policy = DiagnosticPolicy();

        TokenInspectionResult jweResult = UniversalToken.Inspect(jwe, policy);
        TokenInspectionResult detachedResult = UniversalToken.Inspect(
            detached,
            policy);
        TokenInspectionResult coseResult = UniversalToken.InspectBytes(
            encryptedBytes.Bytes,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encryptedBytes.Success, Is.True);
            AssertSuccess(jweResult, TokenKind.JweCompact, 1);
            AssertClaimsUnavailable(jweResult, "encrypted_payload");
            AssertSuccess(detachedResult, TokenKind.JwsJson, 1);
            AssertClaimsUnavailable(detachedResult, "detached_payload");
            AssertSuccess(coseResult, TokenKind.Cose, 1);
            AssertClaimsUnavailable(coseResult, "encrypted_payload");
        });
    }

    [Test]
    public void InvalidOptedInClaimSetDoesNotExposePartialClaims()
    {
        TokenInspectionResult result = UniversalToken.Inspect(
            CompactJws("key", "not-json"),
            DiagnosticPolicy());

        Assert.Multiple(() =>
        {
            AssertSuccess(result, TokenKind.JwsCompact, 1);
            Assert.That(result.ClaimsAvailable, Is.False);
            Assert.That(result.ClaimsAuthenticated, Is.False);
            Assert.That(result.JwtClaims, Is.Null);
            Assert.That(result.CwtClaims, Is.Null);
            Assert.That(result.ClaimsError, Is.Not.Null);
        });
    }

    [Test]
    public void InspectBytesReturnsBodyAndSignerHeadersForCoseSign()
    {
        CoseSign message = new CoseSign();
        message.Header.SetUnprotected(
            CoseHeaderLabels.ContentType,
            Text("application/example"));
        CoseSignature signature = new CoseSignature();
        signature.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Negative(CoseAlgorithms.EcdsaP256Sha256));
        signature.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(Encoding.ASCII.GetBytes("signer")));
        signature.Signature = new byte[] { 1, 2, 3 };
        message.Signatures = new CoseSignature[] { signature };
        message.Payload = Encoding.UTF8.GetBytes("secret payload");
        TokenBytesResult written = message.Write();

        TokenInspectionResult result = UniversalToken.InspectBytes(
            written.Bytes,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.Cose, 2);
            Assert.That(result.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(result.Headers[0].CoseHeader.ContentTypeText,
                Is.EqualTo("application/example"));
            Assert.That(result.Headers[1].Role,
                Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(result.Headers[1].CoseHeader.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(result.Headers[1].CoseHeader.KeyId,
                Is.EqualTo(Encoding.ASCII.GetBytes("signer")));
        });
    }

    [TestCase("D08340A04101", 1)]
    [TestCase("D18440A041014102", 1)]
    [TestCase("D28440A041014102", 1)]
    [TestCase("D8608440A04101818340A040", 2)]
    [TestCase("D8618540A041014102818340A040", 2)]
    [TestCase("D8628440A04101818340A04102", 2)]
    public void InspectBytesSupportsEveryTaggedCoseMessageShape(
        string hex,
        int headerCount)
    {
        TokenInspectionResult result = UniversalToken.InspectBytes(
            Convert.FromHexString(hex),
            new TokenPolicy());

        AssertSuccess(result, TokenKind.Cose, headerCount);
        Assert.That(result.Headers[0].CoseHeader, Is.Not.Null);
    }

    [Test]
    public void InspectBytesAcceptsUtf8TextTokens()
    {
        string token = CompactJws("utf8-key", "secret");

        TokenInspectionResult result = UniversalToken.InspectBytes(
            Encoding.UTF8.GetBytes(token),
            new TokenPolicy());

        AssertSuccess(result, TokenKind.JwsCompact, 1);
        Assert.That(
            result.Headers[0].JoseHeader.Get("kid").StringValue,
            Is.EqualTo("utf8-key"));
    }

    [Test]
    public void InspectionEnforcesPolicyAndRejectsUnknownTokens()
    {
        TokenPolicy limited = new TokenPolicy();
        limited.MaximumTokenSizeInBytes = 8;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumJsonDepth = 0;

        TokenInspectionResult tooLarge = UniversalToken.Inspect(
            CompactJws("key", "payload"),
            limited);
        TokenInspectionResult badPolicy = UniversalToken.Inspect(
            "opaque",
            invalid);
        TokenInspectionResult unknown = UniversalToken.Inspect(
            "opaque",
            new TokenPolicy());
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 1;
        TokenInspectionResult tooDeep = UniversalToken.InspectBytes(
            Convert.FromHexString("D28440A041014102"),
            shallow);

        Assert.Multiple(() =>
        {
            AssertFailure(tooLarge, "token_too_large");
            AssertFailure(badPolicy, "invalid_policy");
            AssertFailure(unknown, "unknown_token");
            AssertFailure(tooDeep, "cbor_depth_exceeded");
        });
    }

    [Test]
    public void PublicInspectionMethodsRejectNullInputs()
    {
        TokenPolicy policy = new TokenPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => UniversalToken.Inspect(null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.Inspect("token", null),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.InspectBytes(null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.InspectBytes(new byte[0], null),
                Throws.ArgumentNullException);
        });
    }

    static string CompactJws(string keyId, string payload)
    {
        return ProtectedHeader(keyId)
            + "."
            + Base64Url.Encode(Encoding.UTF8.GetBytes(payload)).Text
            + ".AA";
    }

    static string ProtectedHeader(string keyId)
    {
        return Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"alg\":\"HS256\",\"kid\":\"" + keyId + "\"}")) .Text;
    }

    static TokenPolicy DiagnosticPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowUnauthenticatedDecode = true;
        return policy;
    }

    static byte[] CoseSign1Claims(string subject)
    {
        CoseSign1 message = new CoseSign1();
        message.Payload = CwtClaimsBytes(subject);
        message.Signature = new byte[] { 1 };
        TokenBytesResult written = message.Write();
        Assert.That(written.Success, Is.True);
        return written.Bytes;
    }

    static byte[] CwtClaimsBytes(string subject)
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text(subject));
        TokenBytesResult written = claims.Write();
        Assert.That(written.Success, Is.True);
        return written.Bytes;
    }

    static byte[] CwtTagged(byte[] cose)
    {
        byte[] result = new byte[cose.Length + 2];
        result[0] = 0xD8;
        result[1] = 0x3D;
        cose.CopyTo(result, 2);
        return result;
    }

    static CborItem Negative(int value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = (ulong)(-value - 1);
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static void AssertJoseHeader(
        TokenHeaderInspection header,
        TokenHeaderRole role,
        TokenHeaderProtection protection,
        string keyId)
    {
        Assert.Multiple(() =>
        {
            Assert.That(header.Role, Is.EqualTo(role));
            Assert.That(header.Protection, Is.EqualTo(protection));
            Assert.That(header.JoseHeader, Is.Not.Null);
            Assert.That(header.CoseHeader, Is.Null);
            Assert.That(
                header.JoseHeader.Get("kid").StringValue,
                Is.EqualTo(keyId));
        });
    }

    static void AssertCoseHeader(TokenHeaderInspection header)
    {
        Assert.Multiple(() =>
        {
            Assert.That(header.Role, Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(header.Protection,
                Is.EqualTo(TokenHeaderProtection.Combined));
            Assert.That(header.JoseHeader, Is.Null);
            Assert.That(header.CoseHeader, Is.Not.Null);
            Assert.That(header.CoseHeader.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha256));
            Assert.That(header.CoseHeader.KeyId,
                Is.EqualTo(Encoding.ASCII.GetBytes("11")));
        });
    }

    static void AssertJwtClaims(
        TokenInspectionResult result,
        string subject)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.ClaimsAvailable, Is.True);
            Assert.That(result.ClaimsAuthenticated, Is.False);
            Assert.That(result.ClaimsError, Is.Null);
            Assert.That(result.JwtClaims, Is.Not.Null);
            Assert.That(result.CwtClaims, Is.Null);
            Assert.That(result.JwtClaims.Get("sub").StringValue,
                Is.EqualTo(subject));
        });
    }

    static void AssertCwtClaims(
        TokenInspectionResult result,
        string subject)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.ClaimsAvailable, Is.True);
            Assert.That(result.ClaimsAuthenticated, Is.False);
            Assert.That(result.ClaimsError, Is.Null);
            Assert.That(result.JwtClaims, Is.Null);
            Assert.That(result.CwtClaims, Is.Not.Null);
            Assert.That(result.CwtClaims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo(subject));
        });
    }

    static void AssertClaimsUnavailable(
        TokenInspectionResult result,
        string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.ClaimsAvailable, Is.False);
            Assert.That(result.ClaimsAuthenticated, Is.False);
            Assert.That(result.JwtClaims, Is.Null);
            Assert.That(result.CwtClaims, Is.Null);
            if (errorCode == null)
            {
                Assert.That(result.ClaimsError, Is.Null);
            }
            else
            {
                Assert.That(result.ClaimsError, Is.Not.Null);
                Assert.That(result.ClaimsError.Code, Is.EqualTo(errorCode));
            }
        });
    }

    static void AssertSuccess(
        TokenInspectionResult result,
        TokenKind kind,
        int headerCount)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.Headers, Has.Length.EqualTo(headerCount));
            Assert.That(result.HeadersAuthenticated, Is.False);
        });
    }

    static void AssertFailure(TokenInspectionResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Kind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.Headers, Is.Empty);
            Assert.That(result.HeadersAuthenticated, Is.False);
        });
    }
}
