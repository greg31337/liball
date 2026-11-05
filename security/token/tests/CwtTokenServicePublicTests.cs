using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CwtTokenServicePublicTests
{
    [Test]
    public void ClaimsRoundTripValidateAndMapThroughPublicService()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy tokenPolicy = Policy("HMAC 256/256");
        CwtClaimsResult created = service.CreateCwtClaims(
            new CborMapEntry[]
            {
                Entry(CwtClaimLabels.Subject, Text("subject")),
                Entry(CwtClaimLabels.Issuer, Text("issuer")),
                Entry(CwtClaimLabels.Audience, Text("api")),
                Entry(CwtClaimLabels.ExpirationTime, Unsigned(2000)),
                Entry(CwtClaimLabels.IssuedAt, Unsigned(900))
            },
            tokenPolicy);
        service.SetCwtIntegerClaim(
            created.Claims!,
            CwtClaimLabels.CwtId,
            Bytes(new byte[] { 1, 2, 3 }));
        service.SetCwtTextClaim(
            created.Claims!,
            "tenant",
            Text("example"));
        bool removed = service.RemoveCwtTextClaim(
            created.Claims!,
            "tenant");
        service.SetCwtIntegerClaim(
            created.Claims!,
            1000,
            Text("temporary"));
        bool removedInteger = service.RemoveCwtIntegerClaim(
            created.Claims!,
            1000);
        TokenBytesResult encoded = service.WriteCwtClaims(
            created.Claims!,
            tokenPolicy);
        CwtClaimsResult parsed = service.ReadCwtClaims(
            encoded.Bytes!,
            tokenPolicy);
        TokenBytesResult rewritten = service.WriteCwtClaims(
            parsed.Claims!,
            tokenPolicy);
        CwtValidationPolicy validationPolicy =
            ValidationPolicy(1000, "HMAC 256/256");
        validationPolicy.ValidateIssuer = true;
        validationPolicy.ValidIssuers = new string[] { "issuer" };
        validationPolicy.ValidateAudience = true;
        validationPolicy.ValidAudiences = new string[] { "api" };
        validationPolicy.RequireExpirationTime = true;
        validationPolicy.RequiredClaims = new int[]
        {
            CwtClaimLabels.Subject
        };
        TokenResult valid = service.ValidateRegisteredCwtClaims(
            parsed.Claims!,
            validationPolicy);
        ClaimMappingResult fromJwt =
            service.MapJwtClaimNameToCwt(JwtClaimNames.JwtId);
        ClaimMappingResult fromCwt =
            service.MapCwtClaimLabelToJwt(CwtClaimLabels.CwtId);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(removed, Is.True);
            Assert.That(removedInteger, Is.True);
            Assert.That(service.GetCwtIntegerClaim(
                created.Claims!,
                1000), Is.Null);
            Assert.That(service.GetCwtTextClaim(
                created.Claims!,
                "tenant"), Is.Null);
            Assert.That(encoded.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded.Bytes));
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.Issuer)!.Text, Is.EqualTo("issuer"));
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.CwtId)!.Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(valid.Success, Is.True);
            Assert.That(fromJwt.Mapped, Is.True);
            Assert.That(fromJwt.CwtLabel,
                Is.EqualTo(CwtClaimLabels.CwtId));
            Assert.That(fromCwt.Mapped, Is.True);
            Assert.That(fromCwt.JwtName,
                Is.EqualTo(JwtClaimNames.JwtId));
        });
    }

    [Test]
    public void ConfirmationAndRfc9597HeaderClaimsRoundTrip()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy("A128GCM");
        CwtClaims claims = Claims(service, policy);
        using ECDsa native = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            native.ExportParameters(false));
        TokenKey proofKey = new TokenKeyService()
            .ImportEc2Key(publicNative, false).Key!;
        proofKey.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        proofKey.HasCoseAlgorithm = true;
        byte[] keyId = new byte[] { 7, 8, 9 };

        TokenResult setProof = service.SetCwtProofOfPossessionKey(
            claims,
            proofKey);
        TokenResult setId = service.SetCwtConfirmationKeyId(
            claims,
            keyId);
        keyId[0] = 99;
        TokenResult confirmationValid =
            service.ValidateCwtConfirmation(claims);
        TokenKeyResult extractedProof =
            service.GetCwtProofOfPossessionKey(claims);
        TokenBytesResult extractedId =
            service.GetCwtConfirmationKeyId(claims);

        TokenKey encryptionKey = Symmetric(
            16,
            20,
            CoseAlgorithms.Aes128Gcm);
        TokenBytesResult encryptedKey = new CoseEncryptionService()
            .EncryptCoseEncrypt0(
                new byte[] { 1, 2, 3, 4 },
                Array.Empty<byte>(),
                Header(policy, CoseAlgorithms.Aes128Gcm, true, 0),
                encryptionKey,
                false,
                policy);
        TokenResult setEncrypted =
            service.SetCwtEncryptedConfirmationKey(
                claims,
                encryptedKey.Bytes!);
        TokenBytesResult extractedEncrypted =
            service.GetCwtEncryptedConfirmationKey(claims);
        TokenKeyResult replacedProof =
            service.GetCwtProofOfPossessionKey(claims);

        CoseHeader header = new CoseHeader();
        TokenResult setHeader =
            service.SetProtectedCwtClaimsInCoseHeader(
                header,
                claims,
                policy);
        CoseCwtClaimsResult headerClaims =
            service.ReadCwtClaimsFromCoseHeader(header, policy);
        TokenResult matching =
            service.ValidateCoseHeaderCwtClaims(
                header,
                claims,
                policy);
        CwtClaims mismatch = Claims(service, policy);
        service.SetCwtIntegerClaim(
            mismatch,
            CwtClaimLabels.Issuer,
            Text("different"));
        TokenResult mismatching =
            service.ValidateCoseHeaderCwtClaims(
                header,
                mismatch,
                policy);
        bool removed = service.RemoveCwtClaimsFromCoseHeader(header);
        TokenResult setUnprotected =
            service.SetUnprotectedCwtClaimsInCoseHeader(
                header,
                mismatch,
                policy);
        CoseCwtClaimsResult unprotected =
            service.ReadCwtClaimsFromCoseHeader(header, policy);

        Assert.Multiple(() =>
        {
            Assert.That(setProof.Success, Is.True);
            Assert.That(setId.Success, Is.True);
            Assert.That(confirmationValid.Success, Is.True);
            Assert.That(extractedProof.Success, Is.True);
            Assert.That(extractedProof.Key!.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(extractedId.Bytes,
                Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(encryptedKey.Success, Is.True);
            Assert.That(setEncrypted.Success, Is.True);
            Assert.That(extractedEncrypted.Bytes,
                Is.EqualTo(encryptedKey.Bytes));
            Assert.That(replacedProof.Success, Is.False);
            Assert.That(replacedProof.Error!.Code,
                Is.EqualTo("missing_confirmation_key"));
            Assert.That(setHeader.Success, Is.True);
            Assert.That(headerClaims.Success, Is.True);
            Assert.That(headerClaims.Present, Is.True);
            Assert.That(headerClaims.Protected, Is.True);
            Assert.That(matching.Success, Is.True);
            Assert.That(mismatching.Success, Is.False);
            Assert.That(mismatching.Error!.Code,
                Is.EqualTo("cwt_header_claim_mismatch"));
            Assert.That(removed, Is.True);
            Assert.That(setUnprotected.Success, Is.True);
            Assert.That(unprotected.Protected, Is.False);
        });

        new TokenKeyService().ClearKeyMaterial(extractedProof.Key!);
        new TokenKeyService().ClearKeyMaterial(encryptionKey);
    }

    [Test]
    public void AutomaticSinglePartyCwtsSignMacEncryptAndDiagnose()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM");
        CwtClaims claims = Claims(service, policy);
        using ECDsa native = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        TokenKey signing = new TokenKeyService()
            .ImportCoseEc2Key(
                native,
                CoseAlgorithms.EcdsaP256Sha256,
                true).Key!;
        TokenKey mac = Symmetric(
            32,
            40,
            CoseAlgorithms.HmacSha256);
        TokenKey encryption = Symmetric(
            16,
            80,
            CoseAlgorithms.Aes128Gcm);

        TokenBytesResult signed = service.CreateCwtWithKey(
            claims,
            signing,
            policy);
        TokenBytesResult maced = service.CreateCwtWithKey(
            claims,
            mac,
            policy);
        TokenBytesResult encrypted = service.CreateCwtWithKey(
            claims,
            encryption,
            policy);
        TokenKeyResolver resolver = Resolver(
            signing,
            mac,
            encryption);
        CwtValidationPolicy validationPolicy = ValidationPolicy(
            1000,
            "ESP256",
            "HMAC 256/256",
            "A128GCM");
        CwtValidationResult signedResult = service.ValidateCwt(
            signed.Bytes!,
            resolver,
            validationPolicy);
        CwtValidationResult macedResult = service.ValidateCwt(
            maced.Bytes!,
            resolver,
            validationPolicy);
        CwtValidationResult encryptedResult = service.ValidateCwt(
            encrypted.Bytes!,
            resolver,
            validationPolicy);
        TokenInspectionResult denied = service.ReadCwtDiagnostic(
            signed.Bytes!,
            policy);
        TokenPolicy diagnosticPolicy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM");
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        TokenInspectionResult diagnostic = service.ReadCwtDiagnostic(
            signed.Bytes!,
            diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(maced.Success, Is.True);
            Assert.That(encrypted.Success, Is.True);
            AssertSuccess(signedResult, CoseMessageKind.Sign1);
            AssertSuccess(macedResult, CoseMessageKind.Mac0);
            AssertSuccess(encryptedResult, CoseMessageKind.Encrypt0);
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error!.Code,
                Is.EqualTo("unauthenticated_decode_not_allowed"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.ClaimsAvailable, Is.True);
            Assert.That(diagnostic.ClaimsAuthenticated, Is.False);
            Assert.That(diagnostic.CwtClaims, Is.Not.Null);
            Assert.That(diagnostic.CwtClaims!.Values, Has.Length.EqualTo(2));
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(mac);
        keyService.ClearKeyMaterial(encryption);
    }

    [Test]
    public void MultiPartyCwtsUseSignMacAndEncryptLayers()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM",
            "direct");
        CwtClaims claims = Claims(service, policy);
        using ECDsa firstNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        TokenKey first = new TokenKeyService().ImportCoseEc2Key(
            firstNative,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key!;
        TokenKey second = new TokenKeyService().ImportCoseEc2Key(
            secondNative,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key!;
        CoseHeader firstHeader = Header(
            policy,
            CoseAlgorithms.EcdsaP256Sha256,
            true,
            1);
        CoseHeader secondHeader = Header(
            policy,
            CoseAlgorithms.EcdsaP256Sha256,
            true,
            2);
        CwtProtectionLayer sign = service.CreateCwtSignLayer(
            new CoseHeader(),
            new CoseHeader[] { firstHeader, secondHeader },
            new TokenKey[] { first, second });

        TokenKey mac = Symmetric(
            32,
            100,
            CoseAlgorithms.Direct);
        CwtProtectionLayer macLayer = service.CreateCwtMacLayer(
            Header(
                policy,
                CoseAlgorithms.HmacSha256,
                true,
                0),
            new CoseHeader[]
            {
                Header(policy, CoseAlgorithms.Direct, false, 3)
            },
            new TokenKey[] { mac },
            null);
        TokenKey encryption = Symmetric(
            16,
            140,
            CoseAlgorithms.Direct);
        CwtProtectionLayer encryptionLayer =
            service.CreateCwtEncryptLayer(
                Header(
                    policy,
                    CoseAlgorithms.Aes128Gcm,
                    true,
                    0),
                new CoseHeader[]
                {
                    Header(policy, CoseAlgorithms.Direct, false, 4)
                },
                new TokenKey[] { encryption },
                null);

        TokenBytesResult signed = service.CreateCwt(
            claims,
            new CwtProtectionLayer[] { sign },
            policy);
        TokenBytesResult maced = service.CreateCwt(
            claims,
            new CwtProtectionLayer[] { macLayer },
            policy);
        TokenBytesResult encrypted = service.CreateCwt(
            claims,
            new CwtProtectionLayer[] { encryptionLayer },
            policy);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            byte id = context.KeyIdBytes == null
                ? (byte)0
                : context.KeyIdBytes[0];
            if (id == 1)
            {
                return KeySuccess(first);
            }

            if (id == 2)
            {
                return KeySuccess(second);
            }

            if (id == 3)
            {
                return KeySuccess(mac);
            }

            if (id == 4)
            {
                return KeySuccess(encryption);
            }

            return KeyFailure("key_not_found");
        });
        CwtValidationPolicy validationPolicy = ValidationPolicy(
            1000,
            "ESP256",
            "HMAC 256/256",
            "A128GCM",
            "direct");

        CwtValidationResult signedResult = service.ValidateCwt(
            signed.Bytes!,
            resolver,
            validationPolicy);
        CwtValidationResult macedResult = service.ValidateCwt(
            maced.Bytes!,
            resolver,
            validationPolicy);
        CwtValidationResult encryptedResult = service.ValidateCwt(
            encrypted.Bytes!,
            resolver,
            validationPolicy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signedResult, CoseMessageKind.Sign);
            AssertSuccess(macedResult, CoseMessageKind.Mac);
            AssertSuccess(encryptedResult, CoseMessageKind.Encrypt);
        });

        TokenKeyService keyService = new TokenKeyService();
        keyService.ClearKeyMaterial(mac);
        keyService.ClearKeyMaterial(encryption);
    }

    [Test]
    public void TaggedNestedCwtBindsPerLayerExternalData()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy("ESP256", "A128GCM");
        CwtClaims claims = Claims(service, policy);
        using ECDsa native = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        TokenKey signing = new TokenKeyService().ImportCoseEc2Key(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key!;
        TokenKey encryption = Symmetric(
            16,
            180,
            CoseAlgorithms.Aes128Gcm);
        byte[] innerData = new byte[] { 1, 2 };
        byte[] outerData = new byte[] { 3, 4 };
        CwtProtectionLayer inner = service.CreateCwtSign1Layer(
            Header(
                policy,
                CoseAlgorithms.EcdsaP256Sha256,
                true,
                0),
            signing);
        inner.ExternalAuthenticatedData = innerData;
        CwtProtectionLayer outer = service.CreateCwtEncrypt0Layer(
            Header(
                policy,
                CoseAlgorithms.Aes128Gcm,
                true,
                0),
            encryption);
        outer.ExternalAuthenticatedData = outerData;

        TokenBytesResult token = service.CreateTaggedCwt(
            claims,
            new CwtProtectionLayer[] { inner, outer },
            policy);
        CwtValidationPolicy validationPolicy = ValidationPolicy(
            1000,
            "ESP256",
            "A128GCM");
        validationPolicy.RequireCwtTag = true;
        validationPolicy.LayerExternalAuthenticatedData = new byte[][]
        {
            outerData,
            innerData
        };
        CwtValidationResult validated = service.ValidateCwt(
            token.Bytes!,
            new TokenKeyResolver(context =>
                context.Operation == TokenKeyOperations.Decrypt
                    ? KeySuccess(encryption)
                    : KeySuccess(signing)),
            validationPolicy);
        CwtValidationPolicy wrongPolicy = ValidationPolicy(
            1000,
            "ESP256",
            "A128GCM");
        wrongPolicy.RequireCwtTag = true;
        wrongPolicy.LayerExternalAuthenticatedData = new byte[][]
        {
            new byte[] { 9 },
            innerData
        };
        CwtValidationResult wrongData = service.ValidateCwt(
            token.Bytes!,
            new TokenKeyResolver(context =>
                context.Operation == TokenKeyOperations.Decrypt
                    ? KeySuccess(encryption)
                    : KeySuccess(signing)),
            wrongPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.CwtTagged, Is.True);
            Assert.That(validated.Nested, Is.True);
            Assert.That(validated.ProtectionKinds, Is.EqualTo(new[]
            {
                CoseMessageKind.Encrypt0,
                CoseMessageKind.Sign1
            }));
            Assert.That(wrongData.Success, Is.False);
            Assert.That(wrongData.Error!.Code,
                Is.EqualTo("decryption_failed"));
        });

        new TokenKeyService().ClearKeyMaterial(encryption);
    }

    [Test]
    public void Rfc8392ClaimsVectorRoundTripsAndMalformedClaimsFailStably()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy();
        byte[] encoded = Convert.FromHexString(
            "A70175636F61703A2F2F61732E6578616D706C652E636F6D02656572696B77037818636F61703A2F2F6C696768742E6578616D706C652E636F6D041A5612AEB0051A5610D9F0061A5610D9F007420B71");

        CwtClaimsResult parsed = service.ReadCwtClaims(encoded, policy);
        TokenBytesResult rewritten = service.WriteCwtClaims(
            parsed.Claims!,
            policy);
        CwtClaimsResult nonMap = service.ReadCwtClaims(
            Convert.FromHexString("80"),
            policy);
        CwtClaimsResult invalidKey = service.ReadCwtClaims(
            Convert.FromHexString("A1410101"),
            policy);
        CwtClaimsResult duplicate = service.ReadCwtClaims(
            Convert.FromHexString("A2016161016162"),
            policy);
        CwtClaimsResult trailing = service.ReadCwtClaims(
            Convert.FromHexString("A0A0"),
            policy);
        CwtClaimsResult truncated = service.ReadCwtClaims(
            Convert.FromHexString("A101"),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.Issuer)!.Text,
                Is.EqualTo("coap://as.example.com"));
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.Subject)!.Text,
                Is.EqualTo("erikw"));
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.Audience)!.Text,
                Is.EqualTo("coap://light.example.com"));
            Assert.That(service.GetCwtIntegerClaim(
                parsed.Claims!,
                CwtClaimLabels.CwtId)!.Bytes,
                Is.EqualTo(new byte[] { 0x0B, 0x71 }));
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded));
            AssertClaimsFailure(nonMap, "cwt_claims_not_map");
            AssertClaimsFailure(invalidKey, "invalid_cwt_claim_key");
            AssertClaimsFailure(duplicate, "duplicate_cbor_map_key");
            AssertClaimsFailure(trailing, "trailing_cwt_claim_data");
            AssertClaimsFailure(truncated, "unexpected_end_of_cbor");
        });
    }

    [Test]
    public void RegisteredClaimLifetimeBoundariesUseClockSkew()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy tokenPolicy = Policy();
        CwtClaims expiredClaims = Claims(service, tokenPolicy);
        service.SetCwtIntegerClaim(
            expiredClaims,
            CwtClaimLabels.ExpirationTime,
            Unsigned(1000));
        CwtClaims earlyClaims = Claims(service, tokenPolicy);
        service.SetCwtIntegerClaim(
            earlyClaims,
            CwtClaimLabels.NotBefore,
            Unsigned(1001));
        CwtClaims futureClaims = Claims(service, tokenPolicy);
        service.SetCwtIntegerClaim(
            futureClaims,
            CwtClaimLabels.IssuedAt,
            Floating(1000.5));
        CwtClaims skewClaims = Claims(service, tokenPolicy);
        service.SetCwtIntegerClaim(
            skewClaims,
            CwtClaimLabels.ExpirationTime,
            Unsigned(996));
        service.SetCwtIntegerClaim(
            skewClaims,
            CwtClaimLabels.NotBefore,
            Unsigned(1005));
        service.SetCwtIntegerClaim(
            skewClaims,
            CwtClaimLabels.IssuedAt,
            Unsigned(1005));
        CwtValidationPolicy exact = ValidationPolicy(1000);
        CwtValidationPolicy skew = ValidationPolicy(1000);
        skew.TokenPolicy!.ClockSkewSeconds = 5;

        TokenResult expired = service.ValidateRegisteredCwtClaims(
            expiredClaims,
            exact);
        TokenResult tooEarly = service.ValidateRegisteredCwtClaims(
            earlyClaims,
            exact);
        TokenResult issuedInFuture = service.ValidateRegisteredCwtClaims(
            futureClaims,
            exact);
        TokenResult withinSkew = service.ValidateRegisteredCwtClaims(
            skewClaims,
            skew);

        Assert.Multiple(() =>
        {
            AssertResultFailure(expired, "token_expired");
            AssertResultFailure(tooEarly, "token_not_yet_valid");
            AssertResultFailure(issuedInFuture, "issued_at_in_future");
            Assert.That(withinSkew.Success, Is.True);
        });
    }

    [Test]
    public void ConfirmationRejectsMalformedAndAmbiguousMethods()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy policy = Policy();
        CwtClaims wrongRoot = Claims(service, policy);
        service.SetCwtIntegerClaim(
            wrongRoot,
            CwtClaimLabels.Confirmation,
            Text("not-a-map"));
        CwtClaims wrongId = Claims(service, policy);
        service.SetCwtIntegerClaim(
            wrongId,
            CwtClaimLabels.Confirmation,
            Map(
                CwtConfirmationMethodLabels.KeyId,
                Text("not-bytes")));
        CwtClaims missing = Claims(service, policy);

        TokenResult rootResult = service.ValidateCwtConfirmation(wrongRoot);
        TokenResult idResult = service.ValidateCwtConfirmation(wrongId);
        TokenKeyResult missingProof =
            service.GetCwtProofOfPossessionKey(missing);
        TokenResult malformedEncrypted =
            service.SetCwtEncryptedConfirmationKey(
                missing,
                Convert.FromHexString("8101"));

        Assert.Multiple(() =>
        {
            AssertResultFailure(rootResult, "confirmation_not_map");
            AssertResultFailure(idResult, "invalid_confirmation_key_id");
            Assert.That(missingProof.Success, Is.False);
            Assert.That(missingProof.Error!.Code,
                Is.EqualTo("missing_confirmation_claim"));
            AssertResultFailure(
                malformedEncrypted,
                "invalid_encrypted_confirmation_key");
        });
    }

    [Test]
    public void ValidationRejectsTamperingResolutionAndProtectionPolicy()
    {
        ICwtTokenOperations service = new CwtTokenService();
        TokenPolicy creationPolicy = Policy("HMAC 256/256");
        TokenKey key = Symmetric(
            32,
            210,
            CoseAlgorithms.HmacSha256);
        CwtProtectionLayer layer = service.CreateCwtMac0Layer(
            Header(
                creationPolicy,
                CoseAlgorithms.HmacSha256,
                true,
                0),
            key);
        TokenBytesResult token = service.CreateCwt(
            Claims(service, creationPolicy),
            new CwtProtectionLayer[] { layer },
            creationPolicy);
        byte[] tampered = token.Bytes!.ToArray();
        tampered[tampered.Length - 1] ^= 1;
        CwtValidationPolicy validationPolicy = ValidationPolicy(
            1000,
            "HMAC 256/256");
        CwtValidationPolicy disabledPolicy = ValidationPolicy(
            1000,
            "HMAC 256/256");
        disabledPolicy.AllowMacTokens = false;
        disabledPolicy.AllowSignedTokens = true;

        CwtValidationResult changed = service.ValidateCwt(
            tampered,
            Resolver(key),
            validationPolicy);
        CwtValidationResult unresolved = service.ValidateCwt(
            token.Bytes!,
            new TokenKeyResolver(context =>
                KeyFailure("key_not_found")),
            validationPolicy);
        CwtValidationResult disabled = service.ValidateCwt(
            token.Bytes!,
            Resolver(key),
            disabledPolicy);
        CwtValidationResult malformedTag = service.ValidateCwt(
            Convert.FromHexString("D83DA0"),
            Resolver(key),
            validationPolicy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(changed, "mac_mismatch");
            AssertValidationFailure(unresolved, "key_not_found");
            AssertValidationFailure(disabled, "maced_cwt_not_allowed");
            AssertValidationFailure(
                malformedTag,
                "invalid_cwt_tag_content");
        });

        new TokenKeyService().ClearKeyMaterial(key);
    }

    static CwtClaims Claims(
        ICwtTokenOperations service,
        TokenPolicy policy)
    {
        return service.CreateCwtClaims(
            new CborMapEntry[]
            {
                Entry(CwtClaimLabels.Issuer, Text("issuer")),
                Entry(CwtClaimLabels.Subject, Text("subject"))
            },
            policy).Claims!;
    }

    static CoseHeader Header(
        TokenPolicy policy,
        int algorithm,
        bool protectedAlgorithm,
        byte keyId)
    {
        CoseHeader header = new CoseHeader();
        CborMapEntry algorithmEntry = Entry(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        if (protectedAlgorithm)
        {
            header.ProtectedParameters =
                new CborMapEntry[] { algorithmEntry };
            if (keyId != 0)
            {
                header.UnprotectedParameters = new CborMapEntry[]
                {
                    Entry(
                        CoseHeaderLabels.KeyId,
                        Bytes(new byte[] { keyId }))
                };
            }
        }
        else
        {
            header.UnprotectedParameters = keyId == 0
                ? new CborMapEntry[] { algorithmEntry }
                : new CborMapEntry[]
                {
                    algorithmEntry,
                    Entry(
                        CoseHeaderLabels.KeyId,
                        Bytes(new byte[] { keyId }))
                };
        }

        TokenBytesResult prepared = new CoseTokenService()
            .WriteCoseHeader(header, policy);
        Assert.That(prepared.Success, Is.True);
        return header;
    }

    static TokenKey Symmetric(
        int length,
        int start,
        int algorithm)
    {
        byte[] material = new byte[length];
        int index = 0;
        while (index < material.Length)
        {
            material[index] = (byte)(start + index);
            index += 1;
        }

        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseSymmetricKey(material, algorithm);
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
                if (context.CoseAlgorithm == keys[index].CoseAlgorithm)
                {
                    return KeySuccess(keys[index]);
                }

                index += 1;
            }

            return KeyFailure("key_not_found");
        });
    }

    static CwtValidationPolicy ValidationPolicy(
        long now,
        params string[] algorithms)
    {
        CwtValidationPolicy policy = new CwtValidationPolicy();
        policy.TokenPolicy!.AllowedAlgorithms = algorithms;
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(() =>
            DateTimeOffset.FromUnixTimeSeconds(now));
        return policy;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
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
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
    }

    static CborItem Int(int value)
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

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Floating(double value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.FloatingPoint;
        item.FloatingPointValue = value;
        item.FloatingPointWidth = 8;
        return item;
    }

    static CborItem Map(int label, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = new CborMapEntry[]
        {
            Entry(label, value)
        };
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

    static void AssertSuccess(
        CwtValidationResult result,
        CoseMessageKind kind)
    {
        Assert.That(
            result.Success,
            Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Claims, Is.Not.Null);
        Assert.That(result.ProtectionKinds,
            Is.EqualTo(new CoseMessageKind[] { kind }));
    }

    static void AssertClaimsFailure(
        CwtClaimsResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Code, Is.EqualTo(code));
    }

    static void AssertResultFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Code, Is.EqualTo(code));
    }

    static void AssertValidationFailure(
        CwtValidationResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Code, Is.EqualTo(code));
    }
}
