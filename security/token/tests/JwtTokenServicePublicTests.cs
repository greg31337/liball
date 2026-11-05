using System.Text;
using token;

namespace token.Tests;

public class JwtTokenServicePublicTests
{
    [Test]
    public void ClaimsAndNumericDatesRoundTripThroughPublicService()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenPolicy policy = new TokenPolicy();
        string json = "{\"iss\":\"https://issuer.example\",\"aud\":[\"api\"],\"private\":true}";
        JwtClaimsResult textClaims = service.ReadJwtClaims(json, policy);
        JwtClaimsResult utf8Claims = service.ReadJwtClaimsUtf8(
            Encoding.UTF8.GetBytes(json),
            policy);
        TokenTextResult written = service.WriteJwtClaims(
            textClaims.Claims!,
            policy);
        TokenBytesResult writtenUtf8 = service.WriteJwtClaimsUtf8(
            utf8Claims.Claims!,
            policy);
        JsonValue subject = StringValue("subject");
        JsonObject values = new JsonObject(new JsonMember[]
        {
            new JsonMember(JwtClaimNames.Subject, subject)
        });
        JwtClaimsResult created = service.CreateJwtClaims(values);

        JwtNumericDateResult fromText = service.ReadJwtNumericDateText("1700.25");
        JwtNumericDateResult fromSeconds = service.ReadJwtNumericDateSeconds(1700.25m);
        DateTimeOffset instant = new DateTimeOffset(
            1970,
            1,
            1,
            2,
            28,
            20,
            TimeSpan.FromHours(1));
        JwtNumericDateResult fromInstant = service.CreateJwtNumericDate(instant);
        JwtNumericDateResult fromClock = service.CreateJwtNumericDateFromClock(
            new TokenClock(() => instant));
        JsonValue numericValue = service.CreateJwtNumericDateValue(instant);
        JwtNumericDateResult fromValue = service.ReadJwtNumericDate(
            numericValue);

        Assert.Multiple(() =>
        {
            Assert.That(textClaims.Success, Is.True);
            Assert.That(utf8Claims.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(json));
            Assert.That(
                Encoding.UTF8.GetString(writtenUtf8.Bytes!),
                Is.EqualTo(json));
            Assert.That(created.Success, Is.True);
            Assert.That(created.Claims!.Values.Members[0].Name,
                Is.EqualTo(JwtClaimNames.Subject));
            Assert.That(fromText.Success, Is.True);
            Assert.That(fromText.Seconds, Is.EqualTo(1700.25m));
            Assert.That(fromSeconds.DateTime, Is.EqualTo(fromText.DateTime));
            Assert.That(fromInstant.NumberText, Is.EqualTo("5300"));
            Assert.That(fromInstant.DateTime.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(fromClock.NumberText, Is.EqualTo("5300"));
            Assert.That(numericValue.Kind, Is.EqualTo(TokenJsonValueKind.Number));
            Assert.That(fromValue.NumberText, Is.EqualTo("5300"));
        });
    }

    [Test]
    public void ClaimsAndNumericDatesRejectMalformedAndOutOfPolicyInputs()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenPolicy policy = new TokenPolicy();
        string claimsJson = "{\"iss\":\"issuer\",\"private\":{\"nested\":true}}";
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(claimsJson) - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumJsonDepth = 1;
        JsonValue stringDate = StringValue("1");

        JwtClaimsResult duplicate = service.ReadJwtClaims(
            "{\"iss\":\"one\",\"iss\":\"two\"}",
            policy);
        JwtClaimsResult invalidType = service.ReadJwtClaims(
            "{\"exp\":\"2000\"}",
            policy);
        JwtClaimsResult nonObject = service.ReadJwtClaims("[]", policy);
        JwtClaimsResult invalidUtf8 = service.ReadJwtClaimsUtf8(
            new byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0xC0, 0x80, 0x7D },
            policy);
        JwtClaimsResult tooLarge = service.ReadJwtClaims(claimsJson, small);
        JwtClaimsResult tooDeep = service.ReadJwtClaims(claimsJson, shallow);
        JwtNumericDateResult malformed = service.ReadJwtNumericDateText("01");
        JwtNumericDateResult outOfRange = service.ReadJwtNumericDateText(
            "253402300800");
        JwtNumericDateResult wrongKind = service.ReadJwtNumericDate(stringDate);
        JwtNumericDateResult minimum = service.ReadJwtNumericDateText(
            "-62135596800");
        JwtNumericDateResult maximum = service.ReadJwtNumericDateText(
            "253402300799.9999999");

        Assert.Multiple(() =>
        {
            AssertFailure(duplicate.Error, "duplicate_json_member");
            AssertFailure(invalidType.Error, "invalid_claim_type");
            AssertFailure(nonObject.Error, "json_root_not_object");
            AssertFailure(invalidUtf8.Error, "invalid_utf8");
            AssertFailure(tooLarge.Error, "token_too_large");
            AssertFailure(tooDeep.Error, "json_depth_exceeded");
            AssertFailure(malformed.Error, "invalid_numeric_date");
            AssertFailure(outOfRange.Error, "numeric_date_out_of_range");
            AssertFailure(wrongKind.Error, "invalid_numeric_date");
            Assert.That(minimum.DateTime, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(maximum.DateTime, Is.EqualTo(DateTimeOffset.MaxValue));
        });
    }

    [Test]
    public void SignedJwtAppliesResolverAndEveryClaimPolicyControl()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey signingKey = Key(
            32,
            10,
            JoseAlgorithms.HmacSha256,
            "signing-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"iss\":\"https://issuer.example\",\"aud\":[\"api\",\"other\"],"
            + "\"sub\":\"subject\",\"exp\":2000,\"iat\":900,\"jti\":\"token-1\"}",
            creationPolicy).Claims!;
        TokenTextResult defaultToken = service.CreateSignedJwt(
            claims,
            signingKey,
            creationPolicy);
        JoseHeader header = Header("{\"typ\":\"at+jwt\"}");
        TokenTextResult token = service.CreateSignedJwtWithHeader(
            claims,
            signingKey,
            header,
            creationPolicy);
        TokenKeyResolutionContext? resolvedContext = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolvedContext = context;
            return KeySuccess(signingKey);
        });
        int subjectChecks = 0;
        int claimChecks = 0;
        int replayChecks = 0;
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new string[] { "https://issuer.example" };
        policy.ValidateAudience = true;
        policy.ValidAudiences = new string[] { "api" };
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new string[] { "at+jwt" };
        policy.RequireExpirationTime = true;
        policy.TokenPolicy!.RequiredClaims = new string[]
        {
            JwtClaimNames.Subject
        };
        policy.SubjectValidator = authenticatedClaims =>
        {
            subjectChecks += 1;
            JsonValue? value = FindClaim(
                authenticatedClaims,
                JwtClaimNames.Subject);
            return value != null && value.StringValue == "subject"
                ? Success()
                : Failure("subject_mismatch");
        };
        policy.ClaimValidator = (name, value) =>
        {
            claimChecks += 1;
            return Success();
        };
        policy.ReplayValidator = (identifier, expiration) =>
        {
            replayChecks += 1;
            return identifier == "token-1"
                && expiration == DateTimeOffset.UnixEpoch.AddSeconds(2000)
                ? Success()
                : Failure("unexpected_replay_values");
        };

        JwtValidationResult automatic = service.ValidateJwt(
            token.Text!,
            resolver,
            policy);
        JwtValidationResult signed = service.ValidateSignedJwt(
            token.Text!,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(defaultToken.Success, Is.True);
            Assert.That(token.Success, Is.True);
            Assert.That(automatic.Success, Is.True);
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.SignatureValidated, Is.True);
            Assert.That(signed.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(signed.SigningKeyId, Is.EqualTo("signing-key"));
            Assert.That(subjectChecks, Is.EqualTo(2));
            Assert.That(claimChecks, Is.EqualTo(12));
            Assert.That(replayChecks, Is.EqualTo(2));
            Assert.That(resolvedContext, Is.Not.Null);
            Assert.That(resolvedContext!.Issuer,
                Is.EqualTo("https://issuer.example"));
            Assert.That(resolvedContext.Audiences,
                Is.EqualTo(new string[] { "api", "other" }));
            Assert.That(resolvedContext.Operation,
                Is.EqualTo(TokenKeyOperations.Verify));
        });
    }

    [Test]
    public void SignedJwtReturnsStablePolicyAndApplicationFailures()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey key = Key(
            32,
            20,
            JoseAlgorithms.HmacSha256,
            null);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"sub\":\"subject\",\"exp\":2000,\"jti\":\"id-1\"}",
            creationPolicy).Claims!;
        string token = service.CreateSignedJwt(
            claims,
            key,
            creationPolicy).Text!;
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        JwtValidationPolicy subjectPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        subjectPolicy.SubjectValidator = authenticated =>
            Failure("application_subject_rejected");
        JwtValidationPolicy missingSubjectResult = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        missingSubjectResult.SubjectValidator = authenticated => null!;
        JwtValidationPolicy claimPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        claimPolicy.ClaimValidator = (name, value) => new TokenResult();
        JwtValidationPolicy replayPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        replayPolicy.ReplayValidator = (identifier, expiration) =>
            Failure("application_replay_rejected");
        JwtValidationPolicy contradictorySubject = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        contradictorySubject.SubjectValidator = authenticated =>
            ContradictorySuccess();
        JwtValidationPolicy contradictoryClaim = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        contradictoryClaim.ClaimValidator = (name, value) =>
            ContradictorySuccess();
        JwtValidationPolicy contradictoryReplay = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        contradictoryReplay.ReplayValidator = (identifier, expiration) =>
            ContradictorySuccess();
        JwtValidationPolicy requiredPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        requiredPolicy.TokenPolicy!.RequiredClaims = new string[] { "tenant" };
        JwtValidationPolicy algorithmPolicy = ValidationPolicy(1000);

        JwtValidationResult subjectRejected = service.ValidateJwt(
            token,
            resolver,
            subjectPolicy);
        JwtValidationResult invalidSubjectResult = service.ValidateJwt(
            token,
            resolver,
            missingSubjectResult);
        JwtValidationResult claimRejected = service.ValidateJwt(
            token,
            resolver,
            claimPolicy);
        JwtValidationResult replayRejected = service.ValidateJwt(
            token,
            resolver,
            replayPolicy);
        JwtValidationResult subjectInvalid = service.ValidateJwt(
            token,
            resolver,
            contradictorySubject);
        JwtValidationResult claimInvalid = service.ValidateJwt(
            token,
            resolver,
            contradictoryClaim);
        JwtValidationResult replayInvalid = service.ValidateJwt(
            token,
            resolver,
            contradictoryReplay);
        JwtValidationResult requiredRejected = service.ValidateJwt(
            token,
            resolver,
            requiredPolicy);
        JwtValidationResult algorithmRejected = service.ValidateJwt(
            token,
            resolver,
            algorithmPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(
                subjectRejected.Error,
                "application_subject_rejected");
            AssertFailure(
                invalidSubjectResult.Error,
                "invalid_subject_validation_result");
            AssertFailure(claimRejected.Error, "claim_rejected");
            AssertFailure(
                replayRejected.Error,
                "application_replay_rejected");
            AssertFailure(
                subjectInvalid.Error,
                "invalid_subject_validation_result");
            AssertFailure(
                claimInvalid.Error,
                "invalid_claim_validation_result");
            AssertFailure(
                replayInvalid.Error,
                "invalid_replay_validation_result");
            AssertFailure(requiredRejected.Error, "missing_required_claim");
            AssertFailure(algorithmRejected.Error, "algorithm_not_allowed");
            Assert.That(subjectRejected.Claims, Is.Null);
            Assert.That(claimRejected.Claims, Is.Null);
        });
    }

    [Test]
    public void SignedJwtEnforcesIssuerAudienceLifetimeAndClockBoundaries()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey key = Key(
            32,
            30,
            JoseAlgorithms.HmacSha256,
            "policy-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"iss\":\"issuer\",\"aud\":[\"api\",\"secondary\"],"
            + "\"sub\":\"subject\",\"exp\":2000,\"nbf\":900,"
            + "\"iat\":900,\"jti\":\"policy-token\"}",
            creationPolicy).Claims!;
        string token = service.CreateSignedJwt(
            claims,
            key,
            creationPolicy).Text!;
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        JwtValidationPolicy acceptedPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        acceptedPolicy.ValidateIssuer = true;
        acceptedPolicy.ValidIssuers = new string[] { "issuer" };
        acceptedPolicy.ValidateAudience = true;
        acceptedPolicy.ValidAudiences = new string[] { "api" };
        JwtValidationPolicy issuerPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        issuerPolicy.ValidateIssuer = true;
        issuerPolicy.ValidIssuers = new string[] { "Issuer" };
        JwtValidationPolicy audiencePolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        audiencePolicy.ValidateAudience = true;
        audiencePolicy.ValidAudiences = new string[] { "missing" };
        JwtValidationPolicy expirationPolicy = ValidationPolicy(
            2000,
            JoseAlgorithms.HmacSha256);
        JwtValidationPolicy withinExpirationSkew = ValidationPolicy(
            2004,
            JoseAlgorithms.HmacSha256);
        withinExpirationSkew.TokenPolicy!.ClockSkewSeconds = 5;
        JwtValidationPolicy expirationSkewBoundary = ValidationPolicy(
            2005,
            JoseAlgorithms.HmacSha256);
        expirationSkewBoundary.TokenPolicy!.ClockSkewSeconds = 5;
        JwtValidationPolicy notBeforePolicy = ValidationPolicy(
            899,
            JoseAlgorithms.HmacSha256);
        JwtValidationPolicy issuedAtPolicy = ValidationPolicy(
            899,
            JoseAlgorithms.HmacSha256);
        issuedAtPolicy.ValidateNotBefore = false;
        JwtValidationPolicy maximumAgePolicy = ValidationPolicy(
            1001,
            JoseAlgorithms.HmacSha256);
        maximumAgePolicy.MaximumTokenAgeSeconds = 100;
        JwtValidationPolicy missingClaimPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        missingClaimPolicy.TokenPolicy!.RequiredClaims = new string[]
        {
            "tenant"
        };
        JwtValidationPolicy typePolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        typePolicy.ValidateTokenType = true;
        typePolicy.ValidTokenTypes = new string[] { "at+jwt" };

        JwtValidationResult accepted = service.ValidateSignedJwt(
            token,
            resolver,
            acceptedPolicy);
        JwtValidationResult issuerRejected = service.ValidateSignedJwt(
            token,
            resolver,
            issuerPolicy);
        JwtValidationResult audienceRejected = service.ValidateSignedJwt(
            token,
            resolver,
            audiencePolicy);
        JwtValidationResult expired = service.ValidateSignedJwt(
            token,
            resolver,
            expirationPolicy);
        JwtValidationResult withinSkew = service.ValidateSignedJwt(
            token,
            resolver,
            withinExpirationSkew);
        JwtValidationResult skewExpired = service.ValidateSignedJwt(
            token,
            resolver,
            expirationSkewBoundary);
        JwtValidationResult notYetValid = service.ValidateSignedJwt(
            token,
            resolver,
            notBeforePolicy);
        JwtValidationResult issuedInFuture = service.ValidateSignedJwt(
            token,
            resolver,
            issuedAtPolicy);
        JwtValidationResult tooOld = service.ValidateSignedJwt(
            token,
            resolver,
            maximumAgePolicy);
        JwtValidationResult missingClaim = service.ValidateSignedJwt(
            token,
            resolver,
            missingClaimPolicy);
        JwtValidationResult wrongType = service.ValidateSignedJwt(
            token,
            resolver,
            typePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Claims, Is.Not.Null);
            AssertFailure(issuerRejected.Error, "issuer_mismatch");
            AssertFailure(audienceRejected.Error, "audience_mismatch");
            AssertFailure(expired.Error, "token_expired");
            Assert.That(withinSkew.Success, Is.True);
            AssertFailure(skewExpired.Error, "token_expired");
            AssertFailure(notYetValid.Error, "token_not_yet_valid");
            AssertFailure(issuedInFuture.Error, "issued_at_in_future");
            AssertFailure(tooOld.Error, "token_too_old");
            AssertFailure(missingClaim.Error, "missing_required_claim");
            AssertFailure(wrongType.Error, "token_type_mismatch");
            Assert.That(issuerRejected.Claims, Is.Null);
            Assert.That(expired.Claims, Is.Null);
        });
    }

    [Test]
    public void SignedJwtResolverTamperingAndMalformedInputsFailClosed()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey key = Key(
            32,
            40,
            JoseAlgorithms.HmacSha256,
            "resolver-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"sub\":\"authenticated\"}",
            creationPolicy).Claims!;
        string token = service.CreateSignedJwt(
            claims,
            key,
            creationPolicy).Text!;
        string tampered = TamperCompactSegment(token, 2);
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        int malformedResolverCalls = 0;
        TokenKeyResult successWithoutKey = new TokenKeyResult();
        successWithoutKey.Success = true;
        TokenKey wrongKey = Key(
            32,
            70,
            JoseAlgorithms.HmacSha256,
            "wrong-key");

        JwtValidationResult customFailure = service.ValidateJwt(
            token,
            new TokenKeyResolver(
                context => KeyFailure("application_key_unavailable")),
            policy);
        JwtValidationResult missingFailure = service.ValidateJwt(
            token,
            new TokenKeyResolver(context => new TokenKeyResult()),
            policy);
        JwtValidationResult invalidSuccess = service.ValidateJwt(
            token,
            new TokenKeyResolver(context => successWithoutKey),
            policy);
        JwtValidationResult wrongKeyResult = service.ValidateJwt(
            token,
            new TokenKeyResolver(context => KeySuccess(wrongKey)),
            policy);
        JwtValidationResult tamperedResult = service.ValidateJwt(
            tampered,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);
        JwtValidationResult malformed = service.ValidateJwt(
            "only.two",
            new TokenKeyResolver(context =>
            {
                malformedResolverCalls += 1;
                return KeyFailure("unexpected_resolution");
            }),
            policy);
        JwtValidationPolicy signedDisabled = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        signedDisabled.AllowSignedTokens = false;
        signedDisabled.AllowNestedTokens = false;
        JwtValidationResult disabled = service.ValidateJwt(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            signedDisabled);

        Assert.Multiple(() =>
        {
            AssertFailure(customFailure.Error, "application_key_unavailable");
            AssertFailure(missingFailure.Error, "key_resolution_failed");
            AssertFailure(invalidSuccess.Error, "invalid_key_resolution_result");
            AssertFailure(wrongKeyResult.Error, "signature_mismatch");
            AssertFailure(tamperedResult.Error, "signature_mismatch");
            Assert.That(tamperedResult.Claims, Is.Null);
            AssertFailure(malformed.Error, "invalid_compact_token");
            Assert.That(malformedResolverCalls, Is.Zero);
            AssertFailure(disabled.Error, "signed_tokens_not_allowed");
        });
    }

    [Test]
    public void EncryptedJwtCreationAndValidationUsePublicService()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey encryptionKey = Key(
            16,
            50,
            JoseAlgorithms.Direct,
            "encryption-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"iss\":\"issuer\",\"sub\":\"encrypted\"}",
            creationPolicy).Claims!;
        TokenTextResult defaultToken = service.CreateEncryptedJwt(
            claims,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy);
        TokenTextResult explicitToken = service.CreateEncryptedJwtWithHeader(
            claims,
            encryptionKey,
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"typ\":\"JWT\"}"),
            creationPolicy);
        TokenTextResult mismatchedToken = service.CreateEncryptedJwtWithHeader(
            claims,
            encryptionKey,
            Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"iss\":\"other\"}"),
            creationPolicy);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(encryptionKey));
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JwtValidationResult automatic = service.ValidateJwt(
            defaultToken.Text!,
            resolver,
            policy);
        JwtValidationResult encrypted = service.ValidateEncryptedJwt(
            explicitToken.Text!,
            resolver,
            policy);
        JwtValidationResult replicationRejected =
            service.ValidateEncryptedJwt(
                mismatchedToken.Text!,
                resolver,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(defaultToken.Success, Is.True);
            Assert.That(explicitToken.Success, Is.True);
            Assert.That(mismatchedToken.Success, Is.True);
            Assert.That(automatic.Success, Is.True);
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(encrypted.EncryptionValidated, Is.True);
            Assert.That(encrypted.SignatureValidated, Is.False);
            Assert.That(encrypted.EncryptionKeyId,
                Is.EqualTo("encryption-key"));
            Assert.That(
                FindClaim(encrypted.Claims!, JwtClaimNames.Subject)!.StringValue,
                Is.EqualTo("encrypted"));
            AssertFailure(
                replicationRejected.Error,
                "replicated_claim_mismatch");
        });
    }

    [Test]
    public void EncryptedJwtValidatesAllReplicatedClaimsExactly()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey encryptionKey = Key(
            16,
            60,
            JoseAlgorithms.Direct,
            "replication-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims matchingClaims = service.ReadJwtClaims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":[\"api\",\"secondary\"]}",
            creationPolicy).Claims!;
        JwtClaims missingClaims = service.ReadJwtClaims(
            "{\"sub\":\"subject\"}",
            creationPolicy).Claims!;
        JoseHeader matchingHeader = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":[\"api\",\"secondary\"]}");
        JoseHeader missingHeader = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"iss\":\"issuer\"}");
        JoseHeader mismatchedHeader = Header(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
            + "\"aud\":[\"secondary\",\"api\"]}");
        TokenTextResult matchingToken = service.CreateEncryptedJwtWithHeader(
            matchingClaims,
            encryptionKey,
            matchingHeader,
            creationPolicy);
        TokenTextResult missingToken = service.CreateEncryptedJwtWithHeader(
            missingClaims,
            encryptionKey,
            missingHeader,
            creationPolicy);
        TokenTextResult mismatchedToken = service.CreateEncryptedJwtWithHeader(
            matchingClaims,
            encryptionKey,
            mismatchedHeader,
            creationPolicy);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(encryptionKey));
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtValidationPolicy disabled = ValidationPolicy(
            1000,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        disabled.ValidateHeaderClaimReplication = false;

        JwtValidationResult matching = service.ValidateEncryptedJwt(
            matchingToken.Text!,
            resolver,
            policy);
        JwtValidationResult missing = service.ValidateEncryptedJwt(
            missingToken.Text!,
            resolver,
            policy);
        JwtValidationResult mismatched = service.ValidateEncryptedJwt(
            mismatchedToken.Text!,
            resolver,
            policy);
        JwtValidationResult mismatchAllowed = service.ValidateEncryptedJwt(
            mismatchedToken.Text!,
            resolver,
            disabled);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Success, Is.True);
            Assert.That(matching.EncryptionValidated, Is.True);
            AssertFailure(missing.Error, "missing_replicated_claim");
            AssertFailure(mismatched.Error, "replicated_claim_mismatch");
            Assert.That(mismatchAllowed.Success, Is.True);
        });
    }

    [Test]
    public void SignedThenEncryptedJwtSupportsDefaultAndExplicitHeaders()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenKey signingKey = Key(
            32,
            80,
            JoseAlgorithms.HmacSha256,
            "signing-key");
        TokenKey encryptionKey = Key(
            16,
            120,
            JoseAlgorithms.Direct,
            "encryption-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims claims = service.ReadJwtClaims(
            "{\"sub\":\"nested\"}",
            creationPolicy).Claims!;
        TokenTextResult defaultToken = service.CreateSignedThenEncryptedJwt(
            claims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy);
        TokenTextResult explicitToken =
            service.CreateSignedThenEncryptedJwtWithHeaders(
                claims,
                signingKey,
                encryptionKey,
                Header("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"),
                Header("{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\"JWT\"}"),
                creationPolicy);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            if (context.Operation == TokenKeyOperations.Verify)
            {
                return KeySuccess(signingKey);
            }

            return KeySuccess(encryptionKey);
        });
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JwtValidationResult defaultValidated = service.ValidateJwt(
            defaultToken.Text!,
            resolver,
            policy);
        JwtValidationResult explicitValidated = service.ValidateEncryptedJwt(
            explicitToken.Text!,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(defaultToken.Success, Is.True);
            Assert.That(explicitToken.Success, Is.True);
            Assert.That(defaultValidated.Success, Is.True);
            Assert.That(explicitValidated.Success, Is.True);
            Assert.That(explicitValidated.Nested, Is.True);
            Assert.That(explicitValidated.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(explicitValidated.InnerKind,
                Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(explicitValidated.SignatureValidated, Is.True);
            Assert.That(explicitValidated.EncryptionValidated, Is.True);
            Assert.That(explicitValidated.SigningKeyId,
                Is.EqualTo("signing-key"));
            Assert.That(explicitValidated.EncryptionKeyId,
                Is.EqualTo("encryption-key"));
        });
    }

    [Test]
    public void NullReferencesRemainProgrammerErrors()
    {
        IJwtTokenOperations service = new JwtTokenService();
        TokenPolicy tokenPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtValidationPolicy validationPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => new TokenKeyResult());

        Assert.Multiple(() =>
        {
            Assert.That(
                () => service.ReadJwtClaims(null!, tokenPolicy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.WriteJwtClaims(null!, tokenPolicy),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ReadJwtNumericDateText(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.CreateJwtNumericDateFromClock(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.ValidateJwt(null!, resolver, validationPolicy),
                Throws.ArgumentNullException);
        });
    }

    static JoseHeader Header(string json)
    {
        IJoseTokenOperations jose = new JoseTokenService();
        JoseHeaderResult result = jose.ReadJoseHeader(json, 16);
        Assert.That(result.Success, Is.True);
        return result.Header!;
    }

    static JsonValue? FindClaim(JwtClaims claims, string name)
    {
        int index = 0;
        while (index < claims.Values.Members.Length)
        {
            JsonMember member = claims.Values.Members[index];
            if (member.Name == name)
            {
                return member.Value;
            }

            index += 1;
        }

        return null;
    }

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static TokenKey Key(
        int length,
        byte start,
        string algorithm,
        string? keyId)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes;
        key.Algorithm = algorithm;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JwtValidationPolicy ValidationPolicy(
        decimal now,
        params string[] algorithms)
    {
        JwtValidationPolicy policy = new JwtValidationPolicy();
        policy.TokenPolicy!.AllowedAlgorithms = algorithms;
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds((double)now));
        return policy;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static TokenKeyResult KeyFailure(string code)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static string TamperCompactSegment(string token, int segmentIndex)
    {
        string[] segments = token.Split('.');
        string segment = segments[segmentIndex];
        segments[segmentIndex] = (segment[0] == 'A' ? "B" : "A")
            + segment[1..];
        return string.Join('.', segments);
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static TokenResult ContradictorySuccess()
    {
        TokenResult result = Failure("unexpected_success_error");
        result.Success = true;
        return result;
    }

    static void AssertFailure(TokenError? error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.Code, Is.EqualTo(code));
    }
}
