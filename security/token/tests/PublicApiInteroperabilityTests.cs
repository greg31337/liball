using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class PublicApiInteroperabilityTests
{
    const string Rfc8392Claims =
        "A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B71";
    const string Rfc8392SymmetricKey =
        "A4205820403697DE87AF64611C1D32A05DAB0FE1FCB715A86AB435F1EC99192D795693880104024C53796D6D65747269633235360304";
    const string Rfc8392MacedCwt =
        "D83DD18443A10104A1044C53796D6D65747269633235365850A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B7148093101EF6D789200";
    const string Rfc9052Sign1 =
        "D28443A10126A10442313154546869732069732074686520636F6E74656E742E58408EB33E4CA31D1C465AB05AAC34CC6B23D58FEF5C083106C4D25A91AEF0B0117E2AF9A291AA32E14AB834DC56ED2A223444547E01F11D3B0916E5A4C345CACB36";

    [Test]
    public void Rfc7520AndRfc8392VectorsValidateThroughPublicServices()
    {
        ITokenKeyService keys = new TokenKeyService();
        IJoseTokenOperations jose = new JoseTokenService();
        ICwtTokenOperations cwt = new CwtTokenService();
        JwkResult joseKey = keys.ParseJwk(
            "{\"kty\":\"oct\","
            + "\"kid\":\"018c0ae5-4d9b-471b-bfd6-eef314bc7037\","
            + "\"use\":\"sig\",\"alg\":\"HS256\","
            + "\"k\":\"hJtXIZ2uSN5kbQfbtTNWbpdmhkV8FJG-Onbc6mxCcYg\"}");
        string payload =
            "SXTigJlzIGEgZGFuZ2Vyb3VzIGJ1c2luZXNzLCBGcm9kbywgZ29pbmcgb3V0IH"
            + "lvdXIgZG9vci4gWW91IHN0ZXAgb250byB0aGUgcm9hZCwgYW5kIGlmIHlvdSBk"
            + "b24ndCBrZWVwIHlvdXIgZmVldCwgdGhlcmXigJlzIG5vIGtub3dpbmcgd2hlcm"
            + "UgeW91IG1pZ2h0IGJlIHN3ZXB0IG9mZiB0by4";
        string protectedHeader =
            "eyJhbGciOiJIUzI1NiIsImtpZCI6IjAxOGMwYWU1LTRkOWItNDcxYi1iZmQ2"
            + "LWVlZjMxNGJjNzAzNyJ9";
        string signature =
            "s0h6KThzkfBBBkLspW1h84VsJZFTsPPqMDA7g1Md7p0";
        JwsCompactResult verified = jose.VerifyJwsCompact(
            protectedHeader + "." + payload + "." + signature,
            joseKey.Key!,
            Policy(JoseAlgorithms.HmacSha256));

        CoseKeyResult coseKey = keys.ParseCoseKey(
            Convert.FromHexString(Rfc8392SymmetricKey));
        CwtValidationPolicy validationPolicy =
            new CwtValidationPolicy();
        validationPolicy.TokenPolicy!.AllowedAlgorithms =
            new string[] { "HMAC 256/64" };
        validationPolicy.TokenPolicy.ClockSkewSeconds = 0;
        validationPolicy.Clock = new TokenClock(() =>
            DateTimeOffset.FromUnixTimeSeconds(1444000000));
        CwtValidationResult validated = cwt.ValidateCwt(
            Convert.FromHexString(Rfc8392MacedCwt),
            Resolver(coseKey.Key!),
            validationPolicy);
        CwtClaimsResult claims = cwt.ReadCwtClaims(
            Convert.FromHexString(Rfc8392Claims),
            validationPolicy.TokenPolicy);
        CoseHeader macHeader = Header(
            validationPolicy.TokenPolicy,
            CoseAlgorithms.HmacSha256Truncated64,
            Encoding.ASCII.GetBytes("Symmetric256"));
        TokenBytesResult recreated = cwt.CreateTaggedCwt(
            claims.Claims!,
            new CwtProtectionLayer[]
            {
                cwt.CreateCwtMac0Layer(macHeader, coseKey.Key!)
            },
            validationPolicy.TokenPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(joseKey.Success, Is.True);
            Assert.That(verified.Success, Is.True,
                Error(verified.Error));
            Assert.That(Encoding.UTF8.GetString(
                verified.Token!.Payload!),
                Does.StartWith("It’s a dangerous business, Frodo"));
            Assert.That(coseKey.Success, Is.True);
            Assert.That(validated.Success, Is.True,
                Error(validated.Error));
            Assert.That(validated.CwtTagged, Is.True);
            Assert.That(validated.Claims!.Get(
                CwtClaimLabels.Subject)!.Text,
                Is.EqualTo("erikw"));
            Assert.That(recreated.Success, Is.True,
                Error(recreated.Error));
            Assert.That(recreated.Bytes,
                Is.EqualTo(Convert.FromHexString(Rfc8392MacedCwt)));
        });

        keys.ClearKeyMaterial(joseKey.Key!);
        keys.ClearKeyMaterial(coseKey.Key!);
    }

    [Test]
    public void Rfc9052Rfc9053AndRfc9338UsePublicCoseServices()
    {
        ITokenKeyService keys = new TokenKeyService();
        ICoseSigningOperations signing = new CoseSigningService();
        ICoseTokenOperations cose = new CoseTokenService();
        ITokenCborCodec cbor = new TokenCborCodec();
        JwkResult vectorKey = keys.ParseJwk(
            "{\"kty\":\"EC\",\"kid\":\"11\",\"crv\":\"P-256\","
            + "\"x\":\"usWxHK2PmfnHKwXPS54m0kTcGJ90UiglWiGahtagnv8\","
            + "\"y\":\"IBOL-C3BttVivg-lSreASjpkttcsz-1rb7btKLv8EX4\","
            + "\"alg\":\"ES256\"}");
        TokenPolicy vectorPolicy = Policy(JoseAlgorithms.EcdsaSha256);
        CoseSign1Result vector = signing.VerifyCoseSign1(
            Convert.FromHexString(Rfc9052Sign1),
            Array.Empty<byte>(),
            vectorKey.Key!,
            true,
            vectorPolicy);

        using ECDsa native = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            native.ExportParameters(false));
        TokenKeyResult privateKey = keys.ImportCoseEc2Key(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            true);
        TokenKeyResult publicKey = keys.ImportCoseEc2Key(
            publicNative,
            CoseAlgorithms.EcdsaP256Sha256,
            false);
        TokenPolicy counterPolicy = Policy("ESP256");
        CoseCounterSignatureTarget target =
            new CoseCounterSignatureTarget();
        target.BodyProtected = Convert.FromHexString("A10126");
        target.Payload = Encoding.UTF8.GetBytes(
            "RFC 9338 countersigned content");
        target.OtherFields = new byte[][]
        {
            new byte[] { 1, 2, 3 }
        };
        CoseHeader counterHeader = Header(
            counterPolicy,
            CoseAlgorithms.EcdsaP256Sha256,
            new byte[] { 9 });
        byte[] externalData = Encoding.UTF8.GetBytes("notary context");
        CoseCounterSignatureResult created =
            signing.CreateCoseCounterSignature(
                target,
                externalData,
                counterHeader,
                privateKey.Key!,
                true,
                false,
                counterPolicy);
        TokenBytesResult encoded = signing.WriteCoseCounterSignature(
            created.CounterSignature!,
            true,
            counterPolicy);
        CoseCounterSignatureResult parsed =
            signing.ReadCoseCounterSignature(
                encoded.Bytes!,
                counterPolicy);
        TokenResult counterVerified =
            signing.VerifyCoseCounterSignature(
                target,
                externalData,
                parsed.CounterSignature!,
                publicKey.Key!,
                false,
                counterPolicy);
        TokenBytesResult structure =
            cose.EncodeCoseCounterSignatureStructure(
                target.BodyProtected,
                parsed.CounterSignature!.Header.ProtectedBytes,
                externalData,
                target.Payload,
                target.OtherFields,
                counterPolicy);
        CborReadResult decoded = cbor.ReadCbor(
            structure.Bytes!,
            4096,
            16);

        Assert.Multiple(() =>
        {
            Assert.That(vectorKey.Success, Is.True);
            Assert.That(vector.Success, Is.True, Error(vector.Error));
            Assert.That(vector.Message!.SignatureVerified, Is.True);
            Assert.That(Encoding.UTF8.GetString(
                vector.Message.Payload!),
                Is.EqualTo("This is the content."));
            Assert.That(created.Success, Is.True,
                Error(created.Error));
            Assert.That(encoded.Success, Is.True,
                Error(encoded.Error));
            Assert.That(parsed.Success, Is.True,
                Error(parsed.Error));
            Assert.That(parsed.CounterSignature!.Tagged, Is.True);
            Assert.That(counterVerified.Success, Is.True,
                Error(counterVerified.Error));
            Assert.That(decoded.Success, Is.True,
                Error(decoded.Error));
            Assert.That(decoded.Item!.ArrayValue![0].Text,
                Is.EqualTo("CounterSignatureV2"));
            Assert.That(decoded.Item.ArrayValue,
                Has.Length.EqualTo(6));
        });

        keys.ClearKeyMaterial(vectorKey.Key!);
        keys.ClearKeyMaterial(privateKey.Key!);
        keys.ClearKeyMaterial(publicKey.Key!);
    }

    [Test]
    public void Rfc9597ProtectedClaimsAuthenticateThroughPublicCwtService()
    {
        ICwtTokenOperations cwt = new CwtTokenService();
        ITokenKeyService keys = new TokenKeyService();
        TokenPolicy policy = Policy("HMAC 256/256");
        CwtClaimsResult claims = cwt.CreateCwtClaims(
            new CborMapEntry[]
            {
                Entry(CwtClaimLabels.Issuer, Text("issuer")),
                Entry(CwtClaimLabels.Subject, Text("subject"))
            },
            policy);
        TokenKeyResult key = keys.ImportCoseSymmetricKey(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256);
        CoseHeader header = Header(
            policy,
            CoseAlgorithms.HmacSha256,
            new byte[] { 1 });
        TokenResult set = cwt.SetProtectedCwtClaimsInCoseHeader(
            header,
            claims.Claims!,
            policy);
        TokenBytesResult token = cwt.CreateTaggedCwt(
            claims.Claims!,
            new CwtProtectionLayer[]
            {
                cwt.CreateCwtMac0Layer(header, key.Key!)
            },
            policy);
        CwtValidationPolicy validationPolicy =
            new CwtValidationPolicy();
        validationPolicy.TokenPolicy!.AllowedAlgorithms =
            new string[] { "HMAC 256/256" };
        CwtValidationResult validated = cwt.ValidateCwt(
            token.Bytes!,
            Resolver(key.Key!),
            validationPolicy);
        CoseCwtClaimsResult replicated =
            cwt.ReadCwtClaimsFromCoseHeader(
                validated.Headers[0],
                policy);
        TokenResult matched = cwt.ValidateCoseHeaderCwtClaims(
            validated.Headers[0],
            validated.Claims!,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(claims.Success, Is.True);
            Assert.That(key.Success, Is.True);
            Assert.That(set.Success, Is.True, Error(set.Error));
            Assert.That(token.Success, Is.True, Error(token.Error));
            Assert.That(validated.Success, Is.True,
                Error(validated.Error));
            Assert.That(replicated.Success, Is.True,
                Error(replicated.Error));
            Assert.That(replicated.Present, Is.True);
            Assert.That(replicated.Protected, Is.True);
            Assert.That(replicated.Claims!.Get(
                CwtClaimLabels.Subject)!.Text,
                Is.EqualTo("subject"));
            Assert.That(matched.Success, Is.True, Error(matched.Error));
        });

        keys.ClearKeyMaterial(key.Key!);
    }

    [Test]
    public void Rfc9901OpenIdConnectAndJarmExamplesValidatePublicly()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        ITokenEncodingService encoding = new TokenEncodingService();
        ITokenKeyService keys = new TokenKeyService();
        TokenKeyResult key = keys.ImportJoseSymmetricKey(
            Sequence(32, 70),
            JoseAlgorithms.HmacSha256);
        key.Key!.KeyId = "profile-key";
        TokenPolicy creationPolicy = Policy(
            JoseAlgorithms.HmacSha256);
        string disclosure = encoding.EncodeBase64Url(
            Encoding.UTF8.GetBytes(
                "[\"salt-1\",\"sub\",\"Alice\"]")).Text!;
        string digest = encoding.EncodeBase64Url(
            SHA256.HashData(
                Encoding.ASCII.GetBytes(disclosure))).Text!;
        JwtClaimsResult sdClaims = jwt.ReadJwtClaims(
            "{\"_sd_alg\":\"sha-256\",\"_sd\":[\""
            + digest + "\"]}",
            creationPolicy);
        string issuer = jwt.CreateSignedJwtWithHeader(
            sdClaims.Claims!,
            key.Key,
            JoseHeader("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        string presentation = issuer + "~" + disclosure + "~";
        SelectiveDisclosureJwtValidationPolicy sdPolicy =
            new SelectiveDisclosureJwtValidationPolicy();
        sdPolicy.JwtPolicy = JwtPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        SelectiveDisclosureJwtResult sd =
            profiles.ValidateSelectiveDisclosureJwt(
                presentation,
                Resolver(key.Key),
                sdPolicy);

        string idToken = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,"
                + "\"iat\":900,\"nonce\":\"nonce-1\"}",
                creationPolicy).Claims!,
            key.Key,
            JoseHeader("{\"typ\":\"JWT\"}"),
            creationPolicy).Text!;
        OpenIdConnectIdTokenValidationPolicy idPolicy =
            new OpenIdConnectIdTokenValidationPolicy();
        idPolicy.JwtPolicy = JwtPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        idPolicy.JwtPolicy.ValidIssuers =
            new string[] { "issuer" };
        idPolicy.ClientId = "client-1";
        idPolicy.Nonce = "nonce-1";
        JwtValidationResult idResult =
            profiles.ValidateOpenIdConnectIdToken(
                idToken,
                Resolver(key.Key),
                idPolicy);

        string response = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"https://as.example\","
                + "\"aud\":\"client-1\",\"exp\":1500,"
                + "\"code\":\"authorization-code\","
                + "\"state\":\"request-state\"}",
                creationPolicy).Claims!,
            key.Key,
            creationPolicy).Text!;
        JwtSecuredAuthorizationResponseValidationPolicy jarmPolicy =
            new JwtSecuredAuthorizationResponseValidationPolicy();
        jarmPolicy.JwtPolicy = JwtPolicy(
            1100,
            JoseAlgorithms.HmacSha256);
        jarmPolicy.Issuer = "https://as.example";
        jarmPolicy.ClientId = "client-1";
        jarmPolicy.State = "request-state";
        JwtValidationResult jarm =
            profiles.ValidateSecuredAuthorizationResponse(
                response,
                Resolver(key.Key),
                jarmPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(sd.Success, Is.True, Error(sd.Error));
            Assert.That(sd.DigestsVerified, Is.True);
            Assert.That(sd.Claims!.Get(
                JwtClaimNames.Subject)!.StringValue,
                Is.EqualTo("Alice"));
            Assert.That(idResult.Success, Is.True,
                Error(idResult.Error));
            Assert.That(idResult.SignatureValidated, Is.True);
            Assert.That(idResult.Claims!.Get(
                JwtClaimNames.Subject)!.StringValue,
                Is.EqualTo("subject"));
            Assert.That(jarm.Success, Is.True, Error(jarm.Error));
            Assert.That(jarm.Claims!.Get("code")!.StringValue,
                Is.EqualTo("authorization-code"));
        });

        keys.ClearKeyMaterial(key.Key);
    }

    [Test]
    public void PublicCodecsGenerateDeterministicJsonAndCborVectors()
    {
        ITokenJsonCodec json = new TokenJsonCodec();
        ITokenCborCodec cbor = new TokenCborCodec();
        JsonValueResult jsonValue = json.ReadJsonText(
            "{\"z\":0,\"a\":1}",
            1024,
            8);
        TokenTextResult jsonVector = json.WriteJsonText(
            jsonValue.Value!,
            1024,
            8);
        CborReadResult cborValue = cbor.ReadCbor(
            Convert.FromHexString("A2617A00616101"),
            1024,
            8);
        TokenBytesResult cborVector = cbor.WriteDeterministicCbor(
            cborValue.Item!,
            1024,
            8);
        TokenTextResult jsonAgain = json.WriteJsonText(
            jsonValue.Value!,
            1024,
            8);
        TokenBytesResult cborAgain = cbor.WriteDeterministicCbor(
            cborValue.Item!,
            1024,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(jsonValue.Success, Is.True);
            Assert.That(jsonVector.Success, Is.True,
                Error(jsonVector.Error));
            Assert.That(jsonVector.Text,
                Is.EqualTo("{\"a\":1,\"z\":0}"));
            Assert.That(jsonAgain.Text, Is.EqualTo(jsonVector.Text));
            Assert.That(cborValue.Success, Is.True,
                Error(cborValue.Error));
            Assert.That(cborVector.Success, Is.True,
                Error(cborVector.Error));
            Assert.That(Convert.ToHexString(cborVector.Bytes!),
                Is.EqualTo("A2616101617A00"));
            Assert.That(cborAgain.Bytes, Is.EqualTo(cborVector.Bytes));
        });
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JwtValidationPolicy JwtPolicy(
        long now,
        params string[] algorithms)
    {
        JwtValidationPolicy policy = new JwtValidationPolicy();
        policy.TokenPolicy!.AllowedAlgorithms = algorithms;
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(() =>
            DateTimeOffset.FromUnixTimeSeconds(now));
        return policy;
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

    static JoseHeader JoseHeader(string json)
    {
        JoseHeaderResult result = new JoseTokenService()
            .ReadJoseHeader(json, 16);
        Assert.That(result.Success, Is.True, Error(result.Error));
        return result.Header!;
    }

    static CoseHeader Header(
        TokenPolicy policy,
        int algorithm,
        byte[] keyId)
    {
        CoseHeader header = new CoseHeader();
        header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Integer(algorithm))
        };
        header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.KeyId, Bytes(keyId))
        };
        TokenBytesResult prepared = new CoseTokenService()
            .WriteCoseHeader(header, policy);
        Assert.That(prepared.Success, Is.True,
            Error(prepared.Error));
        return header;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Integer(label), value);
    }

    static CborItem Integer(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

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

    static byte[] Sequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }

    static string Error(TokenError? error)
    {
        return error == null
            ? "no error"
            : error.Code + ": " + error.Message;
    }
}
