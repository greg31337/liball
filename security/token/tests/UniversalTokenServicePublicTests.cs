using System.Text;
using token;

namespace token.Tests;

public class UniversalTokenServicePublicTests
{
    [Test]
    public void CompactJwtDetectsInspectsCreatesAndValidates()
    {
        IUniversalTokenOperations service = new UniversalTokenService();
        TokenKey key = JoseKey(
            32,
            1,
            JoseAlgorithms.HmacSha256,
            "signing-key");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JwtClaims claims = JwtClaimsFromJson(
            "{\"iss\":\"issuer\",\"sub\":\"alice\","
            + "\"aud\":[\"api\",\"admin\"],\"iat\":1}",
            policy);
        UniversalTokenCreationRequest request =
            new UniversalTokenCreationRequest();
        request.Kind = TokenKind.JwsCompact;
        request.JwtClaims = claims;
        request.Keys = new TokenKey[] { key };

        UniversalTokenCreationResult created =
            service.CreateToken(request, policy);
        TokenKindResult detected =
            service.DetectTextToken(created.Text!);
        TokenInspectionResult inspected =
            service.InspectTextToken(created.Text!, policy);
        TokenPolicy diagnosticPolicy =
            Policy(JoseAlgorithms.HmacSha256);
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        TokenInspectionResult diagnostic =
            service.InspectTextToken(
                created.Text!,
                diagnosticPolicy);
        TokenKeyResolutionContext? observed = null;
        TokenValidationResult validated =
            service.ValidateTextToken(
                created.Text!,
                new TokenKeyResolver(context =>
                {
                    observed = context;
                    return KeySuccess(key);
                }),
                policy);
        TokenKindResult unknown =
            service.DetectTextToken("opaque input");

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Kind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(created.Bytes, Is.Null);
            Assert.That(detected.Success, Is.True);
            Assert.That(detected.Kind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(inspected.Success, Is.True);
            Assert.That(inspected.Headers, Has.Length.EqualTo(1));
            Assert.That(inspected.ClaimsAvailable, Is.False);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.ClaimsAvailable, Is.True);
            Assert.That(diagnostic.ClaimsAuthenticated, Is.False);
            Assert.That(diagnostic.JwtClaims, Is.Not.Null);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Kind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(validated.Subject, Is.EqualTo("alice"));
            Assert.That(validated.Issuer, Is.EqualTo("issuer"));
            Assert.That(validated.Audiences,
                Is.EqualTo(new string[] { "api", "admin" }));
            Assert.That(validated.Algorithm,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(validated.KeyId,
                Is.EqualTo("signing-key"));
            Assert.That(validated.SignatureValidated, Is.True);
            Assert.That(validated.Diagnostics, Is.Empty);
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed!.Operation,
                Is.EqualTo(TokenKeyOperations.Verify));
            Assert.That(unknown.Success, Is.True);
            Assert.That(unknown.Kind, Is.EqualTo(TokenKind.Unknown));
        });

        new TokenKeyService().ClearKeyMaterial(key);
    }

    [Test]
    public void CompactNestedJwtAndGeneralJweUseUnifiedCreation()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        TokenKey signing = JoseKey(
            32,
            1,
            JoseAlgorithms.HmacSha256,
            "signing");
        TokenKey encryption = JoseKey(
            16,
            50,
            JoseAlgorithms.Direct,
            "encryption");
        TokenPolicy nestedPolicy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        UniversalTokenCreationRequest nested =
            new UniversalTokenCreationRequest();
        nested.Kind = TokenKind.JweCompact;
        nested.Nested = true;
        nested.JwtClaims = JwtClaimsFromJson(
            "{\"sub\":\"nested\"}",
            nestedPolicy);
        nested.Keys = new TokenKey[] { signing, encryption };
        nested.ContentEncryptionAlgorithm =
            JoseAlgorithms.Aes128Gcm;

        UniversalTokenCreationResult nestedToken =
            service.CreateToken(nested, nestedPolicy);
        TokenValidationResult nestedResult =
            service.ValidateTextToken(
                nestedToken.Text!,
                Resolver(signing, encryption),
                nestedPolicy);

        TokenKey first = JoseKey(
            16,
            90,
            JoseAlgorithms.Aes128KeyWrap,
            "first");
        TokenKey second = JoseKey(
            16,
            130,
            JoseAlgorithms.Aes128KeyWrap,
            "second");
        TokenPolicy jsonPolicy = Policy(
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        UniversalTokenCreationRequest general =
            new UniversalTokenCreationRequest();
        general.Kind = TokenKind.JweJson;
        general.JwtClaims = JwtClaimsFromJson(
            "{\"sub\":\"json-encrypted\"}",
            jsonPolicy);
        general.JoseProtectedHeaders = new JoseHeader[]
        {
            JoseHeaderFromJson(
                "{\"enc\":\"A128GCM\"}",
                jsonPolicy)
        };
        general.JoseUnprotectedHeaders = new JoseHeader?[]
        {
            JoseHeaderFromJson(
                "{\"alg\":\"A128KW\",\"kid\":\"first\"}",
                jsonPolicy),
            JoseHeaderFromJson(
                "{\"alg\":\"A128KW\",\"kid\":\"second\"}",
                jsonPolicy)
        };
        general.Keys = new TokenKey[] { first, second };
        general.ExternalAuthenticatedData =
            Encoding.UTF8.GetBytes("jwe aad");

        UniversalTokenCreationResult generalToken =
            service.CreateToken(general, jsonPolicy);
        TokenValidationResult generalResult =
            service.ValidateTextToken(
                generalToken.Text!,
                Resolver(first, second),
                jsonPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(nestedToken.Success, Is.True);
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Kind,
                Is.EqualTo(TokenKind.JweCompact));
            Assert.That(nestedResult.InnerKind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(nestedResult.Subject,
                Is.EqualTo("nested"));
            Assert.That(nestedResult.SignatureValidated, Is.True);
            Assert.That(nestedResult.EncryptionValidated, Is.True);
            Assert.That(nestedResult.Nested, Is.True);
            Assert.That(generalToken.Success, Is.True);
            Assert.That(service.DetectTextToken(
                generalToken.Text!).Kind,
                Is.EqualTo(TokenKind.JweJson));
            Assert.That(generalResult.Success, Is.True);
            Assert.That(generalResult.Kind,
                Is.EqualTo(TokenKind.JweJson));
            Assert.That(generalResult.Subject,
                Is.EqualTo("json-encrypted"));
            Assert.That(generalResult.KeyId,
                Is.EqualTo("first"));
            Assert.That(generalResult.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes128Gcm));
            Assert.That(generalResult.EncryptionValidated, Is.True);
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(signing);
        keyService.ClearKeyMaterial(encryption);
        keyService.ClearKeyMaterial(first);
        keyService.ClearKeyMaterial(second);
    }

    [Test]
    public void GeneralAndDetachedJwsCreationPreservesParticipants()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        TokenKey first = JoseKey(
            32,
            1,
            JoseAlgorithms.HmacSha256,
            "first");
        TokenKey second = JoseKey(
            32,
            50,
            JoseAlgorithms.HmacSha256,
            "second");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        UniversalTokenCreationRequest general =
            new UniversalTokenCreationRequest();
        general.Kind = TokenKind.JwsJson;
        general.JwtClaims = JwtClaimsFromJson(
            "{\"sub\":\"multi-signed\"}",
            policy);
        general.JoseProtectedHeaders = new JoseHeader[]
        {
            JoseHeaderFromJson(
                "{\"alg\":\"HS256\",\"kid\":\"first\"}",
                policy),
            JoseHeaderFromJson(
                "{\"alg\":\"HS256\",\"kid\":\"second\"}",
                policy)
        };
        general.Keys = new TokenKey[] { first, second };

        UniversalTokenCreationResult generalToken =
            service.CreateToken(general, policy);
        TokenValidationResult generalResult =
            service.ValidateTextToken(
                generalToken.Text!,
                Resolver(first, second),
                policy);

        byte[] detachedPayload =
            Encoding.UTF8.GetBytes("{\"sub\":\"detached\"}");
        UniversalTokenCreationRequest detached =
            new UniversalTokenCreationRequest();
        detached.Kind = TokenKind.JwsCompact;
        detached.Payload = detachedPayload;
        detached.JoseProtectedHeaders = new JoseHeader[]
        {
            JoseHeaderFromJson(
                "{\"alg\":\"HS256\",\"kid\":\"first\"}",
                policy)
        };
        detached.Keys = new TokenKey[] { first };
        detached.Detached = true;
        UniversalTokenCreationResult detachedToken =
            service.CreateToken(detached, policy);
        JwsCompactResult verified =
            new JoseTokenService().VerifyDetachedJwsCompact(
                detachedToken.Text!,
                detachedToken.DetachedContent!,
                first,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(generalToken.Success, Is.True);
            Assert.That(generalResult.Success, Is.True);
            Assert.That(generalResult.Kind,
                Is.EqualTo(TokenKind.JwsJson));
            Assert.That(generalResult.KeyIds,
                Is.EqualTo(new string[] { "first", "second" }));
            Assert.That(generalResult.Algorithms, Has.Length.EqualTo(2));
            Assert.That(generalResult.Headers, Has.Length.EqualTo(2));
            Assert.That(detachedToken.Success, Is.True);
            Assert.That(detachedToken.DetachedContent,
                Is.EqualTo(detachedPayload));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token!.Payload,
                Is.EqualTo(detachedPayload));
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(first);
        keyService.ClearKeyMaterial(second);
    }

    [Test]
    public void TaggedCoseAndDetachedEncryptionRouteThroughPublicEngines()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        TokenPolicy policy = Policy(
            "HMAC 256/256",
            "A128GCM");
        ICwtTokenOperations cwt = new CwtTokenService();
        CwtClaims claims = CwtClaimsSet(cwt, policy, "cose");
        byte[] payload = cwt.WriteCwtClaims(claims, policy).Bytes!;
        TokenKey mac = CoseKey(
            32,
            1,
            CoseAlgorithms.HmacSha256);
        UniversalTokenCreationRequest macRequest =
            new UniversalTokenCreationRequest();
        macRequest.Kind = TokenKind.Cose;
        macRequest.CoseKind = CoseMessageKind.Mac0;
        macRequest.Payload = payload;
        macRequest.CoseHeader = CoseHeaderFor(
            policy,
            CoseAlgorithms.HmacSha256);
        macRequest.Keys = new TokenKey[] { mac };
        macRequest.Tagged = true;

        UniversalTokenCreationResult macToken =
            service.CreateToken(macRequest, policy);
        TokenKindResult detected =
            service.DetectBinaryToken(macToken.Bytes!);
        TokenValidationResult validated =
            service.ValidateBinaryToken(
                macToken.Bytes!,
                Resolver(mac),
                policy);

        TokenKey encryption = CoseKey(
            16,
            70,
            CoseAlgorithms.Aes128Gcm);
        byte[] plaintext = Encoding.UTF8.GetBytes("detached secret");
        byte[] externalData = Encoding.UTF8.GetBytes("cose aad");
        UniversalTokenCreationRequest encryptionRequest =
            new UniversalTokenCreationRequest();
        encryptionRequest.Kind = TokenKind.Cose;
        encryptionRequest.CoseKind = CoseMessageKind.Encrypt0;
        encryptionRequest.Payload = plaintext;
        encryptionRequest.CoseHeader = CoseHeaderFor(
            policy,
            CoseAlgorithms.Aes128Gcm);
        encryptionRequest.Keys = new TokenKey[] { encryption };
        encryptionRequest.ExternalAuthenticatedData = externalData;
        encryptionRequest.Detached = true;
        encryptionRequest.Tagged = true;

        UniversalTokenCreationResult encrypted =
            service.CreateToken(encryptionRequest, policy);
        CoseEncrypt0Result decrypted =
            new CoseEncryptionService().DecryptDetachedCoseEncrypt0(
                encrypted.Bytes!,
                encrypted.DetachedContent!,
                externalData,
                encryption,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(macToken.Success, Is.True);
            Assert.That(macToken.Text, Is.Null);
            Assert.That(detected.Success, Is.True);
            Assert.That(detected.Kind, Is.EqualTo(TokenKind.Cose));
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Kind, Is.EqualTo(TokenKind.Cose));
            Assert.That(validated.CwtClaims, Is.Not.Null);
            Assert.That(validated.Subject, Is.EqualTo("cose"));
            Assert.That(validated.SignatureValidated, Is.True);
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.DetachedContent, Is.Not.Empty);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Message!.Plaintext,
                Is.EqualTo(plaintext));
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(mac);
        keyService.ClearKeyMaterial(encryption);
    }

    [Test]
    public void TaggedNestedCwtCreatesDetectsAndValidates()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        ICwtTokenOperations cwt = new CwtTokenService();
        TokenPolicy policy = Policy(
            "HMAC 256/256",
            "A128GCM");
        TokenKey mac = CoseKey(
            32,
            1,
            CoseAlgorithms.HmacSha256);
        TokenKey encryption = CoseKey(
            16,
            70,
            CoseAlgorithms.Aes128Gcm);
        CwtProtectionLayer inner = cwt.CreateCwtMac0Layer(
            CoseHeaderFor(policy, CoseAlgorithms.HmacSha256),
            mac);
        CwtProtectionLayer outer = cwt.CreateCwtEncrypt0Layer(
            CoseHeaderFor(policy, CoseAlgorithms.Aes128Gcm),
            encryption);
        UniversalTokenCreationRequest request =
            new UniversalTokenCreationRequest();
        request.Kind = TokenKind.Cwt;
        request.CwtClaims = CwtClaimsSet(cwt, policy, "nested-cwt");
        request.CwtProtectionLayers = new CwtProtectionLayer[]
        {
            inner,
            outer
        };
        request.Tagged = true;

        UniversalTokenCreationResult created =
            service.CreateToken(request, policy);
        TokenKindResult detected =
            service.DetectBinaryToken(created.Bytes!);
        TokenValidationResult validated =
            service.ValidateBinaryToken(
                created.Bytes!,
                new TokenKeyResolver(context =>
                    context.Operation == TokenKeyOperations.Decrypt
                        ? KeySuccess(encryption)
                        : KeySuccess(mac)),
                policy);
        TokenPolicy inspectionPolicy = Policy(
            "HMAC 256/256",
            "A128GCM");
        inspectionPolicy.AllowUnauthenticatedDecode = true;
        TokenInspectionResult inspected =
            service.InspectBinaryToken(
                created.Bytes!,
                inspectionPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Kind, Is.EqualTo(TokenKind.Cwt));
            Assert.That(detected.Success, Is.True);
            Assert.That(detected.Kind, Is.EqualTo(TokenKind.Cwt));
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Kind, Is.EqualTo(TokenKind.Cwt));
            Assert.That(validated.Subject,
                Is.EqualTo("nested-cwt"));
            Assert.That(validated.Nested, Is.True);
            Assert.That(validated.SignatureValidated, Is.True);
            Assert.That(validated.EncryptionValidated, Is.True);
            Assert.That(inspected.Success, Is.True);
            Assert.That(inspected.ClaimsAvailable, Is.False);
            Assert.That(inspected.ClaimsError, Is.Not.Null);
            Assert.That(inspected.ClaimsError!.Code,
                Is.EqualTo("encrypted_payload"));
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(mac);
        keyService.ClearKeyMaterial(encryption);
    }

    [Test]
    public void UnsupportedCreationAndOpaqueValidationReturnStableDiagnostics()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        TokenPolicy policy = new TokenPolicy();
        UniversalTokenCreationRequest unknown =
            new UniversalTokenCreationRequest();
        UniversalTokenCreationRequest selective =
            new UniversalTokenCreationRequest();
        selective.Kind = TokenKind.SdJwt;
        UniversalTokenCreationRequest detachedJwe =
            new UniversalTokenCreationRequest();
        detachedJwe.Kind = TokenKind.JweCompact;
        detachedJwe.Detached = true;

        UniversalTokenCreationResult unknownResult =
            service.CreateToken(unknown, policy);
        UniversalTokenCreationResult selectiveResult =
            service.CreateToken(selective, policy);
        UniversalTokenCreationResult detachedResult =
            service.CreateToken(detachedJwe, policy);
        TokenValidationResult opaque =
            service.ValidateTextToken(
                "opaque input",
                new TokenKeyResolver(context =>
                    KeyFailure("unexpected_resolution")),
                policy);
        TokenValidationResult binary =
            service.ValidateBinaryToken(
                new byte[] { 0xFF, 0xFE },
                new TokenKeyResolver(context =>
                    KeyFailure("unexpected_resolution")),
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(unknownResult.Success, Is.False);
            Assert.That(unknownResult.Error!.Code,
                Is.EqualTo("unsupported_token_creation"));
            Assert.That(selectiveResult.Success, Is.False);
            Assert.That(selectiveResult.Error!.Code,
                Is.EqualTo("unsupported_token_creation"));
            Assert.That(detachedResult.Success, Is.False);
            Assert.That(detachedResult.Error!.Code,
                Is.EqualTo("unsupported_detached_creation"));
            Assert.That(opaque.Success, Is.False);
            Assert.That(opaque.Error!.Code,
                Is.EqualTo("unknown_token"));
            Assert.That(opaque.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(opaque.Diagnostics[0].Stage,
                Is.EqualTo(TokenValidationDiagnosticStage.Inspection));
            Assert.That(binary.Success, Is.False);
            Assert.That(binary.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(binary.Diagnostics[0].Kind,
                Is.EqualTo(TokenKind.Unknown));
        });
    }

    [Test]
    public void CreationRejectsNullEntriesInRequiredParticipantArrays()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        TokenPolicy policy = new TokenPolicy();
        UniversalTokenCreationRequest protectedHeader =
            new UniversalTokenCreationRequest();
        protectedHeader.JoseProtectedHeaders =
            new JoseHeader[] { null! };
        UniversalTokenCreationRequest coseParticipant =
            new UniversalTokenCreationRequest();
        coseParticipant.CoseParticipantHeaders =
            new CoseHeader[] { null! };
        UniversalTokenCreationRequest key =
            new UniversalTokenCreationRequest();
        key.Keys = new TokenKey[] { null! };
        UniversalTokenCreationRequest protectionLayer =
            new UniversalTokenCreationRequest();
        protectionLayer.CwtProtectionLayers =
            new CwtProtectionLayer[] { null! };

        UniversalTokenCreationResult protectedResult =
            service.CreateToken(protectedHeader, policy);
        UniversalTokenCreationResult participantResult =
            service.CreateToken(coseParticipant, policy);
        UniversalTokenCreationResult keyResult =
            service.CreateToken(key, policy);
        UniversalTokenCreationResult layerResult =
            service.CreateToken(protectionLayer, policy);

        Assert.Multiple(() =>
        {
            Assert.That(protectedResult.Error!.Code,
                Is.EqualTo("invalid_universal_creation_request"));
            Assert.That(participantResult.Error!.Code,
                Is.EqualTo("invalid_universal_creation_request"));
            Assert.That(keyResult.Error!.Code,
                Is.EqualTo("invalid_universal_creation_request"));
            Assert.That(layerResult.Error!.Code,
                Is.EqualTo("invalid_universal_creation_request"));
        });
    }

    [Test]
    public void DetectionRecognizesEveryTextAndBinaryTokenKind()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        const string protectedJws = "eyJhbGciOiJIUzI1NiJ9";
        const string protectedJwe =
            "eyJhbGciOiJkaXIiLCJlbmMiOiJBMTI4R0NNIn0";
        const string compactJws =
            "eyJhbGciOiJIUzI1NiJ9.e30.AA";
        const string compactJwe =
            "eyJhbGciOiJkaXIiLCJlbmMiOiJBMTI4R0NNIn0..AA..AA";
        string jsonJws =
            "{\"payload\":\"e30\",\"protected\":\""
            + protectedJws
            + "\",\"signature\":\"AA\"}";
        string jsonJwe =
            "{\"protected\":\""
            + protectedJwe
            + "\",\"iv\":\"AA\",\"ciphertext\":\"\","
            + "\"tag\":\"AA\"}";
        string selectiveDisclosure = compactJws + "~";
        byte[] taggedCose = Convert.FromHexString(
            "D28440A0410140");
        byte[] taggedCwt = Convert.FromHexString(
            "D83DD28440A0410140");

        Assert.Multiple(() =>
        {
            AssertDetected(
                service.DetectTextToken(compactJws),
                TokenKind.JwsCompact);
            AssertDetected(
                service.DetectTextToken(compactJwe),
                TokenKind.JweCompact);
            AssertDetected(
                service.DetectTextToken(jsonJws),
                TokenKind.JwsJson);
            AssertDetected(
                service.DetectTextToken(jsonJwe),
                TokenKind.JweJson);
            AssertDetected(
                service.DetectTextToken(selectiveDisclosure),
                TokenKind.SdJwt);
            AssertDetected(
                service.DetectBinaryToken(
                    Encoding.UTF8.GetBytes(compactJws)),
                TokenKind.JwsCompact);
            AssertDetected(
                service.DetectBinaryToken(
                    Encoding.UTF8.GetBytes(compactJwe)),
                TokenKind.JweCompact);
            AssertDetected(
                service.DetectBinaryToken(
                    Encoding.UTF8.GetBytes(jsonJws)),
                TokenKind.JwsJson);
            AssertDetected(
                service.DetectBinaryToken(
                    Encoding.UTF8.GetBytes(jsonJwe)),
                TokenKind.JweJson);
            AssertDetected(
                service.DetectBinaryToken(
                    Encoding.UTF8.GetBytes(selectiveDisclosure)),
                TokenKind.SdJwt);
            AssertDetected(
                service.DetectBinaryToken(taggedCose),
                TokenKind.Cose);
            AssertDetected(
                service.DetectBinaryToken(taggedCwt),
                TokenKind.Cwt);
            AssertUnknown(service.DetectTextToken("opaque input"));
            AssertUnknown(service.DetectBinaryToken(
                new byte[] { 0xFF, 0xFE }));
        });
    }

    [Test]
    public void InspectionRequiresClaimOptInAndEnforcesResourceLimits()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        const string token =
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhbGljZSJ9.AA";
        TokenPolicy safePolicy = new TokenPolicy();
        TokenPolicy claimsPolicy = new TokenPolicy();
        claimsPolicy.AllowUnauthenticatedDecode = true;
        TokenPolicy textLimit = new TokenPolicy();
        textLimit.MaximumTokenSizeInBytes =
            Encoding.UTF8.GetByteCount(token) - 1;
        TokenPolicy cborLimit = new TokenPolicy();
        cborLimit.MaximumCborDepth = 1;

        TokenInspectionResult safe = service.InspectTextToken(
            token,
            safePolicy);
        TokenInspectionResult claims = service.InspectTextToken(
            token,
            claimsPolicy);
        TokenInspectionResult tooLarge = service.InspectTextToken(
            token,
            textLimit);
        TokenInspectionResult tooDeep = service.InspectBinaryToken(
            Convert.FromHexString("D28440A041014102"),
            cborLimit);
        TokenInspectionResult opaque = service.InspectTextToken(
            "opaque input",
            safePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(safe.Success, Is.True);
            Assert.That(safe.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(safe.ClaimsAvailable, Is.False);
            Assert.That(safe.JwtClaims, Is.Null);
            Assert.That(claims.Success, Is.True);
            Assert.That(claims.ClaimsAvailable, Is.True);
            Assert.That(claims.ClaimsAuthenticated, Is.False);
            Assert.That(claims.JwtClaims!.Get(
                JwtClaimNames.Subject)!.StringValue,
                Is.EqualTo("alice"));
            AssertInspectionFailure(tooLarge, "token_too_large");
            AssertInspectionFailure(tooDeep, "cbor_depth_exceeded");
            AssertInspectionFailure(opaque, "unknown_token");
        });
    }

    [Test]
    public void UniversalJwtMetadataMatchesFamilyValidationAndReportsMissingKeys()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey key = JoseKey(
            32,
            220,
            JoseAlgorithms.HmacSha256,
            "family-key");
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JwtClaims claims = JwtClaimsFromJson(
            "{\"iss\":\"issuer\",\"sub\":\"family\","
            + "\"aud\":[\"api\",\"admin\"]}",
            policy);
        TokenTextResult token = jwt.CreateSignedJwt(
            claims,
            key,
            policy);
        TokenKeyResolver resolver = Resolver(key);
        JwtValidationPolicy familyPolicy =
            new JwtValidationPolicy();
        familyPolicy.TokenPolicy!.AllowedAlgorithms =
            new string[] { JoseAlgorithms.HmacSha256 };

        JwtValidationResult family = jwt.ValidateJwt(
            token.Text!,
            resolver,
            familyPolicy);
        TokenValidationResult universal = service.ValidateTextToken(
            token.Text!,
            resolver,
            policy);
        TokenValidationResult missing = service.ValidateTextToken(
            token.Text!,
            new TokenKeyResolver(context =>
                KeyFailure("family_key_missing")),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            Assert.That(family.Success, Is.True);
            Assert.That(universal.Success, Is.True);
            Assert.That(universal.Kind, Is.EqualTo(family.Kind));
            Assert.That(universal.InnerKind,
                Is.EqualTo(family.InnerKind));
            Assert.That(universal.Algorithm,
                Is.EqualTo(family.Algorithm));
            Assert.That(universal.KeyId,
                Is.EqualTo(family.SigningKeyId));
            Assert.That(universal.Subject,
                Is.EqualTo(family.Claims!.Get(
                    JwtClaimNames.Subject)!.StringValue));
            Assert.That(universal.Issuer, Is.EqualTo("issuer"));
            Assert.That(universal.Audiences,
                Is.EqualTo(new string[] { "api", "admin" }));
            Assert.That(universal.SignatureValidated,
                Is.EqualTo(family.SignatureValidated));
            AssertValidationFailure(
                missing,
                "family_key_missing",
                TokenValidationDiagnosticStage.Validation,
                TokenKind.JwsCompact);
        });

        new TokenKeyService().ClearKeyMaterial(key);
    }

    [Test]
    public void BinaryValidationEnforcesSizeAndProtectionNestingLimits()
    {
        IUniversalTokenOperations service =
            new UniversalTokenService();
        ICwtTokenOperations cwt = new CwtTokenService();
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenKey key = CoseKey(
            32,
            10,
            CoseAlgorithms.HmacSha256);
        CwtProtectionLayer[] layers = new CwtProtectionLayer[9];
        int index = 0;
        while (index < layers.Length)
        {
            layers[index] = cwt.CreateCwtMac0Layer(
                CoseHeaderFor(
                    policy,
                    CoseAlgorithms.HmacSha256),
                key);
            index += 1;
        }

        UniversalTokenCreationRequest request =
            new UniversalTokenCreationRequest();
        request.Kind = TokenKind.Cwt;
        request.CwtClaims = CwtClaimsSet(
            cwt,
            policy,
            "too-deep");
        request.CwtProtectionLayers = layers;
        request.Tagged = true;
        UniversalTokenCreationResult token = service.CreateToken(
            request,
            policy);
        TokenValidationResult nested = service.ValidateBinaryToken(
            token.Bytes!,
            Resolver(key),
            policy);
        TokenPolicy sizePolicy = Policy("HMAC 256/256");
        sizePolicy.MaximumTokenSizeInBytes = token.Bytes!.Length - 1;
        TokenValidationResult tooLarge = service.ValidateBinaryToken(
            token.Bytes!,
            Resolver(key),
            sizePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            Assert.That(token.Kind, Is.EqualTo(TokenKind.Cwt));
            AssertValidationFailure(
                nested,
                "cwt_protection_layer_limit_exceeded",
                TokenValidationDiagnosticStage.Validation,
                TokenKind.Cwt);
            AssertValidationFailure(
                tooLarge,
                "token_too_large",
                TokenValidationDiagnosticStage.Inspection,
                TokenKind.Unknown);
        });

        new TokenKeyService().ClearKeyMaterial(key);
    }

    static JwtClaims JwtClaimsFromJson(
        string json,
        TokenPolicy policy)
    {
        JwtClaimsResult result =
            new JwtTokenService().ReadJwtClaims(json, policy);
        Assert.That(result.Success, Is.True);
        return result.Claims!;
    }

    static CwtClaims CwtClaimsSet(
        ICwtTokenOperations service,
        TokenPolicy policy,
        string subject)
    {
        CwtClaimsResult result = service.CreateCwtClaims(
            new CborMapEntry[]
            {
                new CborMapEntry(
                    CborInteger(CwtClaimLabels.Issuer),
                    CborText("issuer")),
                new CborMapEntry(
                    CborInteger(CwtClaimLabels.Subject),
                    CborText(subject))
            },
            policy);
        Assert.That(result.Success, Is.True);
        return result.Claims!;
    }

    static JoseHeader JoseHeaderFromJson(
        string json,
        TokenPolicy policy)
    {
        JoseHeaderResult result =
            new JoseTokenService().ReadJoseHeader(
                json,
                policy.MaximumJsonDepth);
        Assert.That(result.Success, Is.True);
        return result.Header!;
    }

    static CoseHeader CoseHeaderFor(
        TokenPolicy policy,
        int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.ProtectedParameters = new CborMapEntry[]
        {
            new CborMapEntry(
                CborInteger(CoseHeaderLabels.Algorithm),
                CborInteger(algorithm))
        };
        TokenBytesResult prepared =
            new CoseTokenService().WriteCoseHeader(header, policy);
        Assert.That(prepared.Success, Is.True);
        return header;
    }

    static TokenKey JoseKey(
        int length,
        int start,
        string algorithm,
        string keyId)
    {
        TokenKeyResult imported =
            new TokenKeyService().ImportJoseSymmetricKey(
                Sequence(length, start),
                algorithm);
        Assert.That(imported.Success, Is.True);
        imported.Key!.KeyId = keyId;
        return imported.Key;
    }

    static TokenKey CoseKey(
        int length,
        int start,
        int algorithm)
    {
        TokenKeyResult imported =
            new TokenKeyService().ImportCoseSymmetricKey(
                Sequence(length, start),
                algorithm);
        Assert.That(imported.Success, Is.True);
        return imported.Key!;
    }

    static TokenKeyResolver Resolver(params TokenKey[] keys)
    {
        return new TokenKeyResolver(context =>
        {
            int index = 0;
            while (index < keys.Length)
            {
                TokenKey key = keys[index];
                if (context.KeyId != null
                    && context.KeyId == key.KeyId)
                {
                    return KeySuccess(key);
                }

                if (context.HasCoseAlgorithm
                    && context.CoseAlgorithm == key.CoseAlgorithm)
                {
                    return KeySuccess(key);
                }

                index += 1;
            }

            return KeyFailure("key_not_found");
        });
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
        result.Error = new TokenError(
            code,
            "Key resolution failed.",
            -1);
        return result;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
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

    static CborItem CborInteger(int value)
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

    static CborItem CborText(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static void AssertDetected(
        TokenKindResult result,
        TokenKind kind)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Kind, Is.EqualTo(kind));
    }

    static void AssertUnknown(TokenKindResult result)
    {
        AssertDetected(result, TokenKind.Unknown);
    }

    static void AssertInspectionFailure(
        TokenInspectionResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Code, Is.EqualTo(code));
    }

    static void AssertValidationFailure(
        TokenValidationResult result,
        string code,
        TokenValidationDiagnosticStage stage,
        TokenKind kind)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0].Stage, Is.EqualTo(stage));
        Assert.That(result.Diagnostics[0].Kind, Is.EqualTo(kind));
        Assert.That(result.Diagnostics[0].Error,
            Is.SameAs(result.Error));
    }
}
