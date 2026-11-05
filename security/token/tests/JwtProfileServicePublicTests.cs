using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtProfileServicePublicTests
{
    [Test]
    public void OAuthAccessTokenProfileValidatesThroughBothPublicContracts()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey key = HmacKey(10, "issuer-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = jwt.ReadJwtClaims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\","
            + "\"exp\":2000,\"iat\":900,\"jti\":\"id-1\","
            + "\"client_id\":\"client-1\"}",
            creationPolicy).Claims!;
        string token = jwt.CreateSignedJwtWithHeader(
            claims,
            key,
            Header("{\"typ\":\"at+jwt\"}"),
            creationPolicy).Text!;
        JwtValidationPolicy policy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        policy.ValidIssuers = new string[] { "issuer" };
        policy.ValidAudiences = new string[] { "api" };
        TokenKeyResolver resolver = Resolver(key);

        JwtValidationResult dedicated =
            profiles.ValidateOAuthJwtAccessToken(
                token,
                resolver,
                policy);
        JwtValidationResult combined = jwt.ValidateOAuthJwtAccessToken(
            token,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(dedicated.Success, Is.True);
            Assert.That(combined.Success, Is.True);
            Assert.That(dedicated.SignatureValidated, Is.True);
            Assert.That(dedicated.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(dedicated.SigningKeyId, Is.EqualTo("issuer-key"));
        });
    }

    [Test]
    public void EveryProfileRouteReturnsItsStablePolicyFailure()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        TokenKeyResolver resolver = Resolver(HmacKey(20, null));

        JwtValidationResult openId = profiles.ValidateOpenIdConnectIdToken(
            "token",
            resolver,
            new OpenIdConnectIdTokenValidationPolicy());
        DpopProofValidationResult dpop = profiles.ValidateDpopProofJwt(
            "token",
            new DpopProofValidationPolicy());
        JwtValidationResult request =
            profiles.ValidateSecuredAuthorizationRequest(
                "token",
                resolver,
                new JwtSecuredAuthorizationRequestValidationPolicy());
        JwtValidationResult response =
            profiles.ValidateSecuredAuthorizationResponse(
                "token",
                resolver,
                new JwtSecuredAuthorizationResponseValidationPolicy());
        JwtValidationResult securityEvent =
            profiles.ValidateSecurityEventToken(
                "token",
                resolver,
                new SecurityEventTokenValidationPolicy());
        JwtValidationResult assertion =
            profiles.ValidateClientAssertionJwt(
                "token",
                resolver,
                new ClientAssertionJwtValidationPolicy());

        Assert.Multiple(() =>
        {
            AssertFailure(openId.Error, "invalid_openid_id_token_policy");
            AssertFailure(dpop.Error, "invalid_dpop_policy");
            AssertFailure(
                request.Error,
                "invalid_authorization_request_policy");
            AssertFailure(
                response.Error,
                "invalid_authorization_response_policy");
            AssertFailure(
                securityEvent.Error,
                "invalid_security_event_policy");
            AssertFailure(
                assertion.Error,
                "invalid_client_assertion_policy");
        });
    }

    [Test]
    public void OpenIdClientAssertionJarJarmAndSetValidatePublicly()
    {
        const string eventType =
            "https://schemas.example/events/account-disabled";
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey key = HmacKey(25, "profile-key");
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);

        string idToken = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900,"
                + "\"nonce\":\"nonce-1\"}",
                creationPolicy).Claims!,
            key,
            Header("{\"typ\":\"JWT\"}"),
            creationPolicy).Text!;
        string assertion = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"client-1\",\"sub\":\"client-1\","
                + "\"aud\":\"https://as.example/token\","
                + "\"exp\":1200,\"jti\":\"assertion-1\"}",
                creationPolicy).Claims!,
            key,
            creationPolicy).Text!;
        string request = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
                + "\"client_id\":\"client-1\",\"response_type\":\"code\","
                + "\"redirect_uri\":\"https://client.example/callback\"}",
                creationPolicy).Claims!,
            key,
            Header("{\"typ\":\"oauth-authz-req+jwt\"}"),
            creationPolicy).Text!;
        string response = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"authorization-code\","
                + "\"state\":\"request-state\"}",
                creationPolicy).Claims!,
            key,
            creationPolicy).Text!;
        string securityEvent = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
                + "\"aud\":\"feed-1\",\"events\":{\"" + eventType
                + "\":{\"reason\":\"fraud\"}}}",
                creationPolicy).Claims!,
            key,
            Header("{\"typ\":\"secevent+jwt\"}"),
            creationPolicy).Text!;
        TokenKeyResolver resolver = Resolver(key);
        OpenIdConnectIdTokenValidationPolicy idPolicy = OpenIdPolicy();
        ClientAssertionJwtValidationPolicy assertionPolicy =
            ClientAssertionPolicy();
        assertionPolicy.RequireJwtId = true;
        JwtSecuredAuthorizationRequestValidationPolicy requestPolicy =
            AuthorizationRequestPolicy();
        requestPolicy.RequireExplicitType = true;
        JwtSecuredAuthorizationResponseValidationPolicy responsePolicy =
            AuthorizationResponsePolicy();
        int eventChecks = 0;
        int replayChecks = 0;
        SecurityEventTokenValidationPolicy eventPolicy = SecurityEventPolicy(
            eventType);
        eventPolicy.EventValidator = (type, payload) =>
        {
            eventChecks += 1;
            return type == eventType
                && payload.Get("reason")?.StringValue == "fraud"
                ? Success()
                : Failure("unexpected_event");
        };
        eventPolicy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            replayChecks += 1;
            return identifier == "set-1"
                ? Success()
                : Failure("unexpected_set_identifier");
        };

        JwtValidationResult idResult =
            profiles.ValidateOpenIdConnectIdToken(
                idToken,
                resolver,
                idPolicy);
        JwtValidationResult assertionResult =
            profiles.ValidateClientAssertionJwt(
                assertion,
                resolver,
                assertionPolicy);
        JwtValidationResult requestResult =
            profiles.ValidateSecuredAuthorizationRequest(
                request,
                resolver,
                requestPolicy);
        JwtValidationResult responseResult =
            profiles.ValidateSecuredAuthorizationResponse(
                response,
                resolver,
                responsePolicy);
        JwtValidationResult eventResult =
            profiles.ValidateSecurityEventToken(
                securityEvent,
                resolver,
                eventPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(idResult.Success, Is.True);
            Assert.That(idResult.SignatureValidated, Is.True);
            Assert.That(idResult.TokenType, Is.EqualTo("JWT"));
            Assert.That(assertionResult.Success, Is.True);
            Assert.That(requestResult.Success, Is.True);
            Assert.That(
                FindClaim(requestResult.Claims!, "response_type")!.StringValue,
                Is.EqualTo("code"));
            Assert.That(responseResult.Success, Is.True);
            Assert.That(
                FindClaim(responseResult.Claims!, "code")!.StringValue,
                Is.EqualTo("authorization-code"));
            Assert.That(eventResult.Success, Is.True);
            Assert.That(eventResult.TokenType, Is.EqualTo("secevent+jwt"));
            Assert.That(eventChecks, Is.EqualTo(1));
            Assert.That(replayChecks, Is.EqualTo(1));
        });
    }

    [Test]
    public void SignedProfileBindingsAndReplayChecksFailClosed()
    {
        const string eventType =
            "https://schemas.example/events/account-disabled";
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey key = HmacKey(27, null);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        TokenKeyResolver resolver = Resolver(key);
        string idToken = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900,"
                + "\"nonce\":\"nonce-1\"}",
                creationPolicy).Claims!,
            key,
            creationPolicy).Text!;
        string assertion = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"client-1\",\"sub\":\"other-client\","
                + "\"aud\":\"https://as.example/token\",\"exp\":1200}",
                creationPolicy).Claims!,
            key,
            creationPolicy).Text!;
        string request = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
                + "\"client_id\":\"other-client\","
                + "\"response_type\":\"code\"}",
                creationPolicy).Claims!,
            key,
            Header("{\"typ\":\"oauth-authz-req+jwt\"}"),
            creationPolicy).Text!;
        string response = jwt.CreateSignedJwt(
            jwt.ReadJwtClaims(
                "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"code\",\"state\":\"wrong\"}",
                creationPolicy).Claims!,
            key,
            creationPolicy).Text!;
        string securityEvent = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
                + "\"aud\":\"feed-1\",\"events\":{\"" + eventType
                + "\":{}}}",
                creationPolicy).Claims!,
            key,
            Header("{\"typ\":\"secevent+jwt\"}"),
            creationPolicy).Text!;
        OpenIdConnectIdTokenValidationPolicy wrongNonce = OpenIdPolicy();
        wrongNonce.Nonce = "different";
        SecurityEventTokenValidationPolicy replayPolicy = SecurityEventPolicy(
            eventType);
        replayPolicy.JwtPolicy.ReplayValidator =
            (identifier, expiration) => new TokenResult();

        JwtValidationResult nonceRejected =
            profiles.ValidateOpenIdConnectIdToken(
                idToken,
                resolver,
                wrongNonce);
        JwtValidationResult assertionRejected =
            profiles.ValidateClientAssertionJwt(
                assertion,
                resolver,
                ClientAssertionPolicy());
        JwtValidationResult requestRejected =
            profiles.ValidateSecuredAuthorizationRequest(
                request,
                resolver,
                AuthorizationRequestPolicy());
        JwtValidationResult responseRejected =
            profiles.ValidateSecuredAuthorizationResponse(
                response,
                resolver,
                AuthorizationResponsePolicy());
        JwtValidationResult replayRejected =
            profiles.ValidateSecurityEventToken(
                securityEvent,
                resolver,
                replayPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(nonceRejected.Error, "nonce_mismatch");
            AssertFailure(
                assertionRejected.Error,
                "client_assertion_subject_mismatch");
            AssertFailure(
                requestRejected.Error,
                "authorization_request_client_id_mismatch");
            AssertFailure(
                responseRejected.Error,
                "authorization_response_state_mismatch");
            AssertFailure(
                replayRejected.Error,
                "security_event_replay_detected");
            Assert.That(replayRejected.Claims, Is.Null);
        });
    }

    [Test]
    public void DpopPublicRouteBindsRequestTokenKeyAndReplayPolicy()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        ITokenKeyService keys = new TokenKeyService();
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey privateKey = keys.ImportJoseEc2Key(
            ec,
            JoseAlgorithms.EcdsaSha256,
            true).Key!;
        TokenKey publicKey = keys.ImportJoseEc2Key(
            ec,
            JoseAlgorithms.EcdsaSha256,
            false).Key!;
        string publicJwk = keys.WritePublicJwk(publicKey).Text!;
        string thumbprint = Base64Url(
            keys.CalculateJwkThumbprint(publicKey).Bytes!);
        string accessToken = "bound-access-token";
        string accessTokenHash = Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)));
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.EcdsaSha256);
        JwtClaims claims = jwt.ReadJwtClaims(
            "{\"jti\":\"proof-1\",\"htm\":\"GET\","
            + "\"htu\":\"https://api.example.test/resource\","
            + "\"iat\":1000,\"nonce\":\"server-nonce\","
            + "\"ath\":\"" + accessTokenHash + "\"}",
            creationPolicy).Claims!;
        string proof = jwt.CreateSignedJwtWithHeader(
            claims,
            privateKey,
            Header(
                "{\"typ\":\"dpop+jwt\",\"jwk\":" + publicJwk + "}"),
            creationPolicy).Text!;
        int replayChecks = 0;
        DpopProofValidationPolicy policy = DpopPolicy(
            JoseAlgorithms.EcdsaSha256);
        policy.Nonce = "server-nonce";
        policy.AccessToken = accessToken;
        policy.ExpectedJwkThumbprint = thumbprint;
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            replayChecks += 1;
            return identifier == "proof-1"
                ? Success()
                : Failure("unexpected_proof_identifier");
        };
        DpopProofValidationPolicy wrongMethod = DpopPolicy(
            JoseAlgorithms.EcdsaSha256);
        wrongMethod.HttpMethod = "POST";
        DpopProofValidationPolicy replayRejected = DpopPolicy(
            JoseAlgorithms.EcdsaSha256);
        replayRejected.JwtPolicy.ReplayValidator =
            (identifier, expiration) => new TokenResult();

        DpopProofValidationResult accepted = profiles.ValidateDpopProofJwt(
            proof,
            policy);
        DpopProofValidationResult methodRejected =
            profiles.ValidateDpopProofJwt(proof, wrongMethod);
        DpopProofValidationResult replayFailure =
            profiles.ValidateDpopProofJwt(proof, replayRejected);

        TokenKey hmacKey = HmacKey(29, null);
        string privateSymmetricJwk = keys.WritePrivateJwk(hmacKey).Text!;
        TokenPolicy hmacCreation = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        string hmacProof = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"jti\":\"symmetric\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/resource\","
                + "\"iat\":1000}",
                hmacCreation).Claims!,
            hmacKey,
            Header(
                "{\"typ\":\"dpop+jwt\",\"jwk\":"
                + privateSymmetricJwk + "}"),
            hmacCreation).Text!;
        DpopProofValidationResult symmetricRejected =
            profiles.ValidateDpopProofJwt(
                hmacProof,
                DpopPolicy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Jwt.SignatureValidated, Is.True);
            Assert.That(accepted.JwkThumbprint, Is.EqualTo(thumbprint));
            Assert.That(accepted.PublicKey!.NativePublicMaterial, Is.True);
            Assert.That(accepted.PublicKey.NativePrivateMaterial, Is.False);
            Assert.That(replayChecks, Is.EqualTo(1));
            AssertFailure(
                methodRejected.Error,
                "dpop_http_method_mismatch");
            AssertFailure(
                replayFailure.Error,
                "dpop_proof_replay_detected");
            AssertFailure(
                symmetricRejected.Error,
                "invalid_dpop_algorithm");
        });

        if (accepted.PublicKey != null)
        {
            keys.ClearKeyMaterial(accepted.PublicKey);
        }
    }

    [Test]
    public void SelectiveDisclosureParsingAndDigestValidationArePublic()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey issuerKey = HmacKey(30, null);
        string disclosure = Base64Url(
            Encoding.UTF8.GetBytes("[\"salt-1\",\"sub\",\"Alice\"]"));
        string digest = Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(disclosure)));
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = jwt.ReadJwtClaims(
            "{\"_sd_alg\":\"sha-256\",\"_sd\":[\"" + digest + "\"]}",
            creationPolicy).Claims!;
        string issuer = jwt.CreateSignedJwtWithHeader(
            claims,
            issuerKey,
            Header("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        string presentation = issuer + "~" + disclosure + "~";
        SelectiveDisclosureJwtValidationPolicy policy = SdPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        int subjectChecks = 0;
        policy.JwtPolicy.SubjectValidator = processed =>
        {
            subjectChecks += 1;
            JsonValue? subject = FindClaim(processed, JwtClaimNames.Subject);
            return subject != null && subject.StringValue == "Alice"
                ? Success()
                : Failure("subject_not_disclosed");
        };

        SelectiveDisclosureJwtResult parsed =
            profiles.ParseSelectiveDisclosureJwt(presentation, policy);
        SelectiveDisclosureJwtResult parsedCombined =
            jwt.ParseSelectiveDisclosureJwt(presentation, policy);
        SelectiveDisclosureJwtResult validated =
            profiles.ValidateSelectiveDisclosureJwt(
                presentation,
                Resolver(issuerKey),
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsedCombined.Success, Is.True);
            Assert.That(parsed.Disclosures, Has.Length.EqualTo(1));
            Assert.That(parsed.DigestsVerified, Is.False);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.DigestsVerified, Is.True);
            Assert.That(validated.KeyBindingValidated, Is.False);
            Assert.That(
                FindClaim(validated.Claims!, JwtClaimNames.Subject)!.StringValue,
                Is.EqualTo("Alice"));
            Assert.That(subjectChecks, Is.EqualTo(1));
        });
    }

    [Test]
    public void SelectiveDisclosureTamperingDuplicationAndHashPolicyFailClosed()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        TokenKey issuerKey = HmacKey(35, null);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        string disclosure = Base64Url(
            Encoding.UTF8.GetBytes("[\"salt\",\"role\",\"admin\"]"));
        string altered = Base64Url(
            Encoding.UTF8.GetBytes("[\"salt\",\"role\",\"user\"]"));
        string digest = Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(disclosure)));
        string issuer = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"_sd_alg\":\"sha-256\",\"_sd\":[\""
                + digest + "\"]}",
                creationPolicy).Claims!,
            issuerKey,
            Header("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        SelectiveDisclosureJwtValidationPolicy policy = SdPolicy(
            1000,
            JoseAlgorithms.HmacSha256);

        SelectiveDisclosureJwtResult tampered =
            profiles.ValidateSelectiveDisclosureJwt(
                issuer + "~" + altered + "~",
                Resolver(issuerKey),
                policy);
        SelectiveDisclosureJwtResult duplicate =
            profiles.ValidateSelectiveDisclosureJwt(
                issuer + "~" + disclosure + "~" + disclosure + "~",
                Resolver(issuerKey),
                policy);

        string sha384Disclosure = Base64Url(
            Encoding.UTF8.GetBytes("[\"salt-384\",\"tier\",\"gold\"]"));
        string sha384Digest = Base64Url(
            SHA384.HashData(Encoding.ASCII.GetBytes(sha384Disclosure)));
        string sha384Issuer = jwt.CreateSignedJwtWithHeader(
            jwt.ReadJwtClaims(
                "{\"_sd_alg\":\"sha-384\",\"_sd\":[\""
                + sha384Digest + "\"]}",
                creationPolicy).Claims!,
            issuerKey,
            Header("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        string sha384Presentation = sha384Issuer + "~"
            + sha384Disclosure + "~";
        SelectiveDisclosureJwtResult hashRejected =
            profiles.ValidateSelectiveDisclosureJwt(
                sha384Presentation,
                Resolver(issuerKey),
                policy);
        SelectiveDisclosureJwtValidationPolicy sha384Allowed = SdPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        sha384Allowed.AllowedHashAlgorithms = new string[]
        {
            SelectiveDisclosureHashAlgorithms.Sha256,
            SelectiveDisclosureHashAlgorithms.Sha384
        };
        SelectiveDisclosureJwtResult hashAccepted =
            profiles.ValidateSelectiveDisclosureJwt(
                sha384Presentation,
                Resolver(issuerKey),
                sha384Allowed);

        Assert.Multiple(() =>
        {
            AssertFailure(
                tampered.Error,
                "unreferenced_sd_jwt_disclosure");
            AssertFailure(
                duplicate.Error,
                "duplicate_sd_jwt_disclosure");
            AssertFailure(
                hashRejected.Error,
                "unsupported_sd_jwt_hash_algorithm");
            Assert.That(hashAccepted.Success, Is.True);
            Assert.That(hashAccepted.HashAlgorithm, Is.EqualTo("sha-384"));
            Assert.That(
                FindClaim(hashAccepted.Claims!, "tier")!.StringValue,
                Is.EqualTo("gold"));
        });
    }

    [Test]
    public void RequiredKeyBindingAuthenticatesHolderAndTransaction()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        ITokenKeyService keys = new TokenKeyService();
        TokenKey issuerKey = HmacKey(40, null);
        using RSA holderRsa = RSA.Create(2048);
        TokenKey holderKey = keys.ImportJoseRsaKey(
            holderRsa,
            JoseAlgorithms.RsaSha256,
            true).Key!;
        string publicJwk = keys.WritePublicJwk(holderKey).Text!;
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.RsaSha256);
        JwtClaims issuerClaims = jwt.ReadJwtClaims(
            "{\"_sd_alg\":\"sha-256\",\"cnf\":{\"jwk\":"
            + publicJwk + "}}",
            creationPolicy).Claims!;
        string issuer = jwt.CreateSignedJwtWithHeader(
            issuerClaims,
            issuerKey,
            Header("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        string sdJwt = issuer + "~";
        string hash = Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(sdJwt)));
        string keyBinding = KeyBindingJwt(
            jwt,
            holderKey,
            hash,
            "verifier",
            "nonce-1",
            1000,
            creationPolicy);
        string presentation = sdJwt + keyBinding;
        SelectiveDisclosureJwtValidationPolicy policy = KeyBindingPolicy();

        SelectiveDisclosureJwtResult result =
            profiles.ValidateSelectiveDisclosureJwt(
                presentation,
                Resolver(issuerKey),
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DigestsVerified, Is.True);
            Assert.That(result.KeyBindingValidated, Is.True);
            Assert.That(result.KeyBindingJwtValidation, Is.Not.Null);
            Assert.That(
                result.KeyBindingJwtValidation!.SignatureValidated,
                Is.True);
            Assert.That(result.KeyBindingJwtValidation.TokenType,
                Is.EqualTo("kb+jwt"));
            Assert.That(result.Claims, Is.Not.Null);
        });
    }

    [Test]
    public void RequiredKeyBindingRejectsMissingNonceHashAndStaleProofs()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        IJwtTokenOperations jwt = new JwtTokenService();
        ITokenKeyService keys = new TokenKeyService();
        TokenKey issuerKey = HmacKey(50, null);
        using RSA holderRsa = RSA.Create(2048);
        TokenKey holderKey = keys.ImportJoseRsaKey(
            holderRsa,
            JoseAlgorithms.RsaSha256,
            true).Key!;
        string publicJwk = keys.WritePublicJwk(holderKey).Text!;
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.RsaSha256);
        JwtClaims issuerClaims = jwt.ReadJwtClaims(
            "{\"cnf\":{\"jwk\":" + publicJwk + "}}",
            creationPolicy).Claims!;
        string issuer = jwt.CreateSignedJwtWithHeader(
            issuerClaims,
            issuerKey,
            Header("{\"typ\":\"example+sd-jwt\"}"),
            creationPolicy).Text!;
        string sdJwt = issuer + "~";
        string hash = Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(sdJwt)));
        string validBinding = KeyBindingJwt(
            jwt,
            holderKey,
            hash,
            "verifier",
            "nonce-1",
            1000,
            creationPolicy);
        string badHashBinding = KeyBindingJwt(
            jwt,
            holderKey,
            "wrong-hash",
            "verifier",
            "nonce-1",
            1000,
            creationPolicy);
        string staleBinding = KeyBindingJwt(
            jwt,
            holderKey,
            hash,
            "verifier",
            "nonce-1",
            800,
            creationPolicy);
        SelectiveDisclosureJwtValidationPolicy wrongNonce =
            KeyBindingPolicy();
        wrongNonce.ExpectedKeyBindingNonce = "different";

        SelectiveDisclosureJwtResult missing =
            profiles.ValidateSelectiveDisclosureJwt(
                sdJwt,
                Resolver(issuerKey),
                KeyBindingPolicy());
        SelectiveDisclosureJwtResult nonceMismatch =
            profiles.ValidateSelectiveDisclosureJwt(
                sdJwt + validBinding,
                Resolver(issuerKey),
                wrongNonce);
        SelectiveDisclosureJwtResult hashMismatch =
            profiles.ValidateSelectiveDisclosureJwt(
                sdJwt + badHashBinding,
                Resolver(issuerKey),
                KeyBindingPolicy());
        SelectiveDisclosureJwtResult stale =
            profiles.ValidateSelectiveDisclosureJwt(
                sdJwt + staleBinding,
                Resolver(issuerKey),
                KeyBindingPolicy());

        Assert.Multiple(() =>
        {
            AssertFailure(missing.Error, "missing_sd_jwt_key_binding");
            AssertFailure(
                nonceMismatch.Error,
                "sd_jwt_key_binding_nonce_mismatch");
            AssertFailure(
                hashMismatch.Error,
                "sd_jwt_key_binding_hash_mismatch");
            AssertFailure(stale.Error, "token_too_old");
            Assert.That(missing.Claims, Is.Null);
            Assert.That(nonceMismatch.KeyBindingValidated, Is.False);
        });
    }

    [Test]
    public void PublicProfileMethodsRequireReferenceArguments()
    {
        IJwtProfileOperations profiles = new JwtProfileService();
        TokenKeyResolver resolver = Resolver(HmacKey(60, null));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => profiles.ValidateOAuthJwtAccessToken(
                    null!,
                    resolver,
                    new JwtValidationPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => profiles.ValidateDpopProofJwt(
                    null!,
                    new DpopProofValidationPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => profiles.ParseSelectiveDisclosureJwt(
                    null!,
                    new SelectiveDisclosureJwtValidationPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => profiles.ValidateSelectiveDisclosureJwt(
                    "token~",
                    null!,
                    new SelectiveDisclosureJwtValidationPolicy()),
                Throws.ArgumentNullException);
        });
    }

    static string KeyBindingJwt(
        IJwtTokenOperations jwt,
        TokenKey holderKey,
        string hash,
        string audience,
        string nonce,
        int issuedAt,
        TokenPolicy policy)
    {
        JwtClaims claims = jwt.ReadJwtClaims(
            "{\"iat\":" + issuedAt + ",\"aud\":\"" + audience
            + "\",\"nonce\":\"" + nonce + "\",\"sd_hash\":\""
            + hash + "\"}",
            policy).Claims!;
        return jwt.CreateSignedJwtWithHeader(
            claims,
            holderKey,
            Header("{\"typ\":\"kb+jwt\"}"),
            policy).Text!;
    }

    static OpenIdConnectIdTokenValidationPolicy OpenIdPolicy()
    {
        OpenIdConnectIdTokenValidationPolicy policy =
            new OpenIdConnectIdTokenValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        policy.JwtPolicy.ValidIssuers = new string[] { "issuer" };
        policy.ClientId = "client-1";
        policy.Nonce = "nonce-1";
        return policy;
    }

    static ClientAssertionJwtValidationPolicy ClientAssertionPolicy()
    {
        ClientAssertionJwtValidationPolicy policy =
            new ClientAssertionJwtValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(
            1000,
            JoseAlgorithms.HmacSha256);
        policy.ClientId = "client-1";
        policy.AuthorizationServerAudience = "https://as.example/token";
        return policy;
    }

    static JwtSecuredAuthorizationRequestValidationPolicy
        AuthorizationRequestPolicy()
    {
        JwtSecuredAuthorizationRequestValidationPolicy policy =
            new JwtSecuredAuthorizationRequestValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(
            1100,
            JoseAlgorithms.HmacSha256);
        policy.ClientId = "client-1";
        policy.AuthorizationServerIssuer = "https://as.example";
        return policy;
    }

    static JwtSecuredAuthorizationResponseValidationPolicy
        AuthorizationResponsePolicy()
    {
        JwtSecuredAuthorizationResponseValidationPolicy policy =
            new JwtSecuredAuthorizationResponseValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(
            1100,
            JoseAlgorithms.HmacSha256);
        policy.Issuer = "https://as.example";
        policy.ClientId = "client-1";
        policy.State = "request-state";
        return policy;
    }

    static SecurityEventTokenValidationPolicy SecurityEventPolicy(
        string eventType)
    {
        SecurityEventTokenValidationPolicy policy =
            new SecurityEventTokenValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(
            1100,
            JoseAlgorithms.HmacSha256);
        policy.JwtPolicy.ValidIssuers = new string[] { "issuer" };
        policy.JwtPolicy.ValidAudiences = new string[] { "feed-1" };
        policy.ValidEventTypes = new string[] { eventType };
        return policy;
    }

    static DpopProofValidationPolicy DpopPolicy(string algorithm)
    {
        DpopProofValidationPolicy policy = new DpopProofValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(1100, algorithm);
        policy.HttpMethod = "GET";
        policy.HttpUri = "https://api.example.test/resource?view=full";
        policy.MaximumProofAgeSeconds = 300;
        return policy;
    }

    static SelectiveDisclosureJwtValidationPolicy KeyBindingPolicy()
    {
        SelectiveDisclosureJwtValidationPolicy policy = SdPolicy(
            1050,
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.RsaSha256);
        policy.RequireKeyBinding = true;
        policy.ExpectedKeyBindingAudience = "verifier";
        policy.ExpectedKeyBindingNonce = "nonce-1";
        policy.MaximumKeyBindingAgeSeconds = 100;
        policy.JwtPolicy.TokenPolicy!.ClockSkewSeconds = 0;
        return policy;
    }

    static SelectiveDisclosureJwtValidationPolicy SdPolicy(
        int now,
        params string[] algorithms)
    {
        SelectiveDisclosureJwtValidationPolicy policy =
            new SelectiveDisclosureJwtValidationPolicy();
        policy.JwtPolicy.TokenPolicy!.AllowedAlgorithms = algorithms;
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(now));
        return policy;
    }

    static JwtValidationPolicy ValidationPolicy(
        int now,
        params string[] algorithms)
    {
        JwtValidationPolicy policy = new JwtValidationPolicy();
        policy.TokenPolicy!.AllowedAlgorithms = algorithms;
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(now));
        return policy;
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = new JoseTokenService().ReadJoseHeader(
            json,
            16);
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

    static TokenKey HmacKey(int start, string? keyId)
    {
        byte[] bytes = new byte[32];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        TokenKey key = new TokenKeyService().ImportJoseSymmetricKey(
            bytes,
            JoseAlgorithms.HmacSha256).Key!;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResolver Resolver(TokenKey key)
    {
        return new TokenKeyResolver(context => KeySuccess(key));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static string Base64Url(byte[] bytes)
    {
        return new TokenEncodingService().EncodeBase64Url(bytes).Text!;
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

    static void AssertFailure(TokenError? error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.Code, Is.EqualTo(code));
    }
}
