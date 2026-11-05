using System.Text;
using token;

namespace token.Tests;

public class SerializationRoundTripTests
{
    [Test]
    public void JwkJwksAndCoseKeyRoundTripDeterministically()
    {
        TokenKey joseFirst = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        joseFirst.KeyId = "first";
        joseFirst.Use = TokenKeyUses.Signature;
        joseFirst.KeyOperations = new[]
        {
            TokenKeyOperations.Sign,
            TokenKeyOperations.Verify
        };
        TokenKey joseSecond = SymmetricKeyImporterOperations.ImportJose(
            Sequence(48, 40),
            JoseAlgorithms.HmacSha384).Key;
        joseSecond.KeyId = "second";
        TokenKey cose = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 90),
            CoseAlgorithms.HmacSha256).Key;
        cose.KeyIdBytes = new byte[] { 1, 2, 3 };
        cose.CoseKeyOperations = new[]
        {
            CoseKeyLabels.OperationMacCreate,
            CoseKeyLabels.OperationMacVerify
        };

        TokenTextResult jwk = JsonWebKey.Write(joseFirst);
        JwkResult parsedJwk = JsonWebKey.Read(jwk.Text);
        TokenTextResult rewrittenJwk = JsonWebKey.Write(parsedJwk.Key);
        TokenTextResult jwks = JsonWebKeySet.Write(
            new[] { joseFirst, joseSecond });
        JwksResult parsedJwks = JsonWebKeySet.Read(jwks.Text);
        TokenTextResult rewrittenJwks = parsedJwks.Set.Write();
        TokenBytesResult coseKey = CoseKey.Write(cose);
        CoseKeyResult parsedCoseKey = CoseKey.Read(coseKey.Bytes);
        TokenBytesResult rewrittenCoseKey = CoseKey.Write(
            parsedCoseKey.Key);

        Assert.Multiple(() =>
        {
            Assert.That(jwk.Success, Is.True);
            Assert.That(parsedJwk.Success, Is.True);
            Assert.That(rewrittenJwk.Text, Is.EqualTo(jwk.Text));
            Assert.That(parsedJwk.Key.KeyId, Is.EqualTo("first"));
            Assert.That(parsedJwk.Key.KeyOperations,
                Is.EqualTo(joseFirst.KeyOperations));
            Assert.That(jwks.Success, Is.True);
            Assert.That(parsedJwks.Success, Is.True);
            Assert.That(rewrittenJwks.Text, Is.EqualTo(jwks.Text));
            Assert.That(parsedJwks.Set.Keys, Has.Length.EqualTo(2));
            Assert.That(parsedJwks.Set.Keys[1].KeyId,
                Is.EqualTo("second"));
            Assert.That(coseKey.Success, Is.True);
            Assert.That(parsedCoseKey.Success, Is.True);
            Assert.That(rewrittenCoseKey.Bytes,
                Is.EqualTo(coseKey.Bytes));
            Assert.That(parsedCoseKey.Key.KeyIdBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(parsedCoseKey.Key.CoseKeyOperations,
                Is.EqualTo(cose.CoseKeyOperations));
        });

        joseFirst.ReleaseOwnedMaterial();
        joseSecond.ReleaseOwnedMaterial();
        cose.ReleaseOwnedMaterial();
        parsedJwk.Key.ReleaseOwnedMaterial();
        parsedJwks.Set.ReleaseOwnedMaterial();
        parsedCoseKey.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void JwtAndCwtRoundTripThroughAuthenticatedValidation()
    {
        TokenKey jwtKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        jwtKey.KeyId = "jwt-key";
        TokenPolicy jwtTokenPolicy = Policy(
            JoseAlgorithms.HmacSha256);
        JwtClaims jwtClaims = JwtClaims.Read(
            "{\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":[\"api\"],\"private\":true}").Claims;
        TokenTextResult jwt = JsonWebToken.CreateSigned(
            jwtClaims,
            jwtKey,
            jwtTokenPolicy);
        JwtValidationResult validatedJwt = JsonWebToken.ValidateSigned(
            jwt.Text,
            Resolver(jwtKey),
            JwtValidationPolicy.Create(jwtTokenPolicy));

        TokenKey cwtKey = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 60),
            CoseAlgorithms.HmacSha256).Key;
        cwtKey.KeyIdBytes = new byte[] { 4, 5, 6 };
        TokenPolicy cwtTokenPolicy = Policy("HMAC 256/256");
        CwtClaims cwtClaims = new CwtClaims();
        cwtClaims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        cwtClaims.Set(CwtClaimLabels.Subject, Text("subject"));
        cwtClaims.Set(CwtClaimLabels.Audience, Text("api"));
        TokenBytesResult cwt = CborWebToken.Create(
            cwtClaims,
            cwtKey,
            cwtTokenPolicy);
        CwtValidationResult validatedCwt = CborWebToken.Validate(
            cwt.Bytes,
            Resolver(cwtKey),
            CwtValidationPolicy.Create(cwtTokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(jwt.Success, Is.True);
            Assert.That(validatedJwt.Success, Is.True,
                Error(validatedJwt.Error));
            Assert.That(validatedJwt.SignatureValidated, Is.True);
            Assert.That(validatedJwt.Claims.Get(JwtClaimNames.Subject)
                .StringValue, Is.EqualTo("subject"));
            Assert.That(validatedJwt.Claims.Get("private").BooleanValue,
                Is.True);
            Assert.That(cwt.Success, Is.True);
            Assert.That(validatedCwt.Success, Is.True,
                Error(validatedCwt.Error));
            Assert.That(validatedCwt.ProtectionKinds,
                Is.EqualTo(new[] { CoseMessageKind.Mac0 }));
            Assert.That(validatedCwt.Claims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo("subject"));
            Assert.That(validatedCwt.Claims.Get(CwtClaimLabels.Audience).Text,
                Is.EqualTo("api"));
        });

        jwtKey.ReleaseOwnedMaterial();
        cwtKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void JwsRoundTripsThroughParseVerifyAndRecreation()
    {
        byte[] payload = Encoding.UTF8.GetBytes("JWS round trip payload");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = "jws-key";
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"HS256\",\"kid\":\"jws-key\","
            + "\"typ\":\"example+jws\"}").Header;

        TokenTextResult written = JsonWebSignature.Sign(
            payload,
            header,
            key,
            policy);
        JwsCompactResult parsed = JsonWebSignature.Parse(
            written.Text,
            policy);
        JwsCompactResult verified = JsonWebSignature.Verify(
            written.Text,
            key,
            policy);
        TokenTextResult recreated = JsonWebSignature.Sign(
            verified.Token.Payload,
            parsed.Token.ProtectedHeader,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.KeyId, Is.EqualTo("jws-key"));
            Assert.That(verified.Success, Is.True,
                Error(verified.Error));
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
            Assert.That(verified.Token.SignatureVerified, Is.True);
            Assert.That(recreated.Text, Is.EqualTo(written.Text));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void JweRoundTripsThroughDiagnosticReconstructionAndDecryption()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "JWE round trip payload");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, 1),
            JoseAlgorithms.Direct).Key;
        key.KeyId = "jwe-key";
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"kid\":\"jwe-key\",\"typ\":\"example+jwe\"}")
            .Header;

        TokenTextResult written = JsonWebEncryption.Encrypt(
            plaintext,
            header,
            key,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        JweCompactResult diagnostic = JsonWebEncryption.ReadDiagnostic(
            written.Text,
            diagnosticPolicy);
        TokenBytesResult protectedBytes =
            diagnostic.Token.ProtectedHeader.WriteUtf8();
        TokenTextResult reconstructed = JoseCompact.WriteJwe(
            protectedBytes.Bytes,
            diagnostic.Token.EncryptedKey,
            diagnostic.Token.InitializationVector,
            diagnostic.Token.Ciphertext,
            diagnostic.Token.AuthenticationTag);
        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            reconstructed.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.CiphertextAvailable, Is.True);
            Assert.That(diagnostic.Token.AuthenticationTagVerified,
                Is.False);
            Assert.That(protectedBytes.Success, Is.True);
            Assert.That(reconstructed.Text, Is.EqualTo(written.Text));
            Assert.That(decrypted.Success, Is.True,
                Error(decrypted.Error));
            Assert.That(decrypted.Token.Plaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
        });

        key.ReleaseOwnedMaterial();
    }

    [TestCase(
        CoseMessageKind.Sign1,
        "D28441A0A201260442313154546869732069732074686520636F6E74656E742E"
        + "584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90"
        + "C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")]
    [TestCase(
        CoseMessageKind.Mac0,
        "D18443A10104A10442010243030405420607")]
    [TestCase(
        CoseMessageKind.Encrypt0,
        "D08343A10101A10542010243030405")]
    [TestCase(
        CoseMessageKind.Sign,
        "D8628440A04101818343A10126A1044109420203")]
    [TestCase(
        CoseMessageKind.Mac,
        "D8618543A10104A104410943010203480405060708090A0B818340A2012204410C420D0E")]
    [TestCase(
        CoseMessageKind.Encrypt,
        "D8608443A10101A10542010243030405818340A20122044109420607")]
    public void CoseMessageModelsRoundTripExactEncoding(
        CoseMessageKind kind,
        string hex)
    {
        byte[] encoded = Convert.FromHexString(hex);
        CoseMessageResult detected = CoseMessage.Read(encoded);
        TokenBytesResult rewritten = RewriteCose(encoded, kind);

        Assert.Multiple(() =>
        {
            Assert.That(detected.Success, Is.True,
                Error(detected.Error));
            Assert.That(detected.Kind, Is.EqualTo(kind));
            Assert.That(detected.Tagged, Is.True);
            Assert.That(rewritten.Success, Is.True,
                Error(rewritten.Error));
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded));
        });
    }

    static TokenBytesResult RewriteCose(
        byte[] encoded,
        CoseMessageKind kind)
    {
        if (kind == CoseMessageKind.Sign1)
        {
            CoseSign1Result result = CoseSign1.Read(encoded);
            Assert.That(result.Success, Is.True, Error(result.Error));
            return result.Message.Write();
        }

        if (kind == CoseMessageKind.Mac0)
        {
            CoseMac0Result result = CoseMac0.Read(encoded);
            Assert.That(result.Success, Is.True, Error(result.Error));
            return result.Message.Write();
        }

        if (kind == CoseMessageKind.Encrypt0)
        {
            CoseEncrypt0Result result = CoseEncrypt0.Read(encoded);
            Assert.That(result.Success, Is.True, Error(result.Error));
            return result.Message.Write();
        }

        if (kind == CoseMessageKind.Sign)
        {
            CoseSignResult result = CoseSign.Read(encoded);
            Assert.That(result.Success, Is.True, Error(result.Error));
            return result.Message.Write();
        }

        if (kind == CoseMessageKind.Mac)
        {
            CoseMacResult result = CoseMac.Read(encoded);
            Assert.That(result.Success, Is.True, Error(result.Error));
            return result.Message.Write();
        }

        CoseEncryptResult encryption = CoseEncrypt.Read(encoded);
        Assert.That(encryption.Success, Is.True, Error(encryption.Error));
        return encryption.Message.Write();
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
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

    static TokenKeyResolver Resolver(TokenKey key)
    {
        return new TokenKeyResolver(context => KeySuccess(key));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static string Error(TokenError error)
    {
        return error == null ? "no error" : error.Code;
    }
}
