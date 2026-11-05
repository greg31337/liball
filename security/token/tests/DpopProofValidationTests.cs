using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class DpopProofValidationTests
{
    [Test]
    public void ValidProofAuthenticatesRequestAndReturnsPublicKeyBinding()
    {
        TestKeys keys = Keys();
        DpopProofValidationPolicy policy = Policy();
        policy.HttpUri = "https://api.example.test:443/a/../resource?view=full#ignored";
        string proof = Proof(
            keys,
            "{\"jti\":\"proof-1\",\"htm\":\"GET\","
                + "\"htu\":\"https://API.EXAMPLE.TEST/resource\",\"iat\":1000}");

        DpopProofValidationResult result = DpopProofValidator.Validate(
            proof,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Jwt.Success, Is.True);
            Assert.That(result.Jwt.SignatureValidated, Is.True);
            Assert.That(result.Jwt.Algorithm, Is.EqualTo(JoseAlgorithms.EcdsaSha256));
            Assert.That(result.Jwt.TokenType, Is.EqualTo("dpop+jwt"));
            Assert.That(result.Jwt.Claims.Get(JwtClaimNames.JwtId).StringValue,
                Is.EqualTo("proof-1"));
            Assert.That(result.PublicKey.ContainsPublicMaterial(), Is.True);
            Assert.That(result.PublicKey.ContainsPrivateMaterial(), Is.False);
            Assert.That(result.JwkThumbprint, Is.EqualTo(Thumbprint(keys.Public)));
        });

        result.PublicKey.ReleaseOwnedMaterial();
        keys.Dispose();
    }

    [TestCase(null, "missing_dpop_type")]
    [TestCase("DPoP+JWT", "invalid_dpop_type")]
    [TestCase("application/dpop+jwt", "invalid_dpop_type")]
    [TestCase("JWT", "invalid_dpop_type")]
    public void ProofRequiresExactProtectedType(string? type, string code)
    {
        TestKeys keys = Keys();
        string proof = Proof(
            keys,
            RequiredClaims(),
            type,
            PublicJwk(keys));

        AssertFailure(
            DpopProofValidator.Validate(proof, Policy()),
            code);

        keys.Dispose();
    }

    [Test]
    public void ProofRequiresRegisteredAsymmetricAlgorithmAndPublicJwk()
    {
        TokenKey symmetric = SymmetricKey();
        string symmetricJwk = JsonWebKey.Write(symmetric).Text;
        string hmacProof = SignedProof(
            symmetric,
            RequiredClaims(),
            "dpop+jwt",
            symmetricJwk,
            JoseAlgorithms.HmacSha256);
        DpopProofValidationPolicy hmacPolicy = Policy();
        hmacPolicy.JwtPolicy.TokenPolicy.AllowedAlgorithms =
            new[] { JoseAlgorithms.HmacSha256 };

        TestKeys privateKeys = Keys();
        string privateJwk = JsonWebKey.WritePrivate(privateKeys.Private).Text;
        string privateProof = Proof(
            privateKeys,
            RequiredClaims(),
            "dpop+jwt",
            privateJwk);

        TestKeys signer = Keys();
        TestKeys embedded = Keys();
        string mismatched = Proof(
            signer,
            RequiredClaims(),
            "dpop+jwt",
            PublicJwk(embedded));

        Assert.Multiple(() =>
        {
            AssertFailure(
                DpopProofValidator.Validate(hmacProof, hmacPolicy),
                "invalid_dpop_algorithm");
            AssertFailure(
                DpopProofValidator.Validate(privateProof, Policy()),
                "invalid_dpop_jwk");
            AssertFailure(
                DpopProofValidator.Validate(mismatched, Policy()),
                "signature_mismatch");
        });

        symmetric.ReleaseOwnedMaterial();
        privateKeys.Dispose();
        signer.Dispose();
        embedded.Dispose();
    }

    [TestCase("{\"htm\":\"GET\",\"htu\":\"https://api.example.test/resource\",\"iat\":1000}", "jti")]
    [TestCase("{\"jti\":\"proof\",\"htu\":\"https://api.example.test/resource\",\"iat\":1000}", "htm")]
    [TestCase("{\"jti\":\"proof\",\"htm\":\"GET\",\"iat\":1000}", "htu")]
    [TestCase("{\"jti\":\"proof\",\"htm\":\"GET\",\"htu\":\"https://api.example.test/resource\"}", "iat")]
    public void ProofRequiresCoreClaims(string claims, string missing)
    {
        TestKeys keys = Keys();
        DpopProofValidationResult result = DpopProofValidator.Validate(
            Proof(keys, claims),
            Policy());

        AssertFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));

        keys.Dispose();
    }

    [Test]
    public void ProofBindsCaseSensitiveMethodAndNormalizedQuerylessUri()
    {
        TestKeys keys = Keys();
        string wrongMethod = Proof(
            keys,
            "{\"jti\":\"method\",\"htm\":\"get\","
                + "\"htu\":\"https://api.example.test/resource\",\"iat\":1000}");
        string wrongUri = Proof(
            keys,
            "{\"jti\":\"uri\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/other\",\"iat\":1000}");
        string queryInClaim = Proof(
            keys,
            "{\"jti\":\"query\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/resource?x=1\",\"iat\":1000}");

        Assert.Multiple(() =>
        {
            AssertFailure(
                DpopProofValidator.Validate(wrongMethod, Policy()),
                "dpop_http_method_mismatch");
            AssertFailure(
                DpopProofValidator.Validate(wrongUri, Policy()),
                "dpop_http_uri_mismatch");
            AssertFailure(
                DpopProofValidator.Validate(queryInClaim, Policy()),
                "invalid_dpop_http_uri");
        });

        keys.Dispose();
    }

    [Test]
    public void ProofEnforcesShortIssuedAtWindow()
    {
        TestKeys keys = Keys();
        string old = Proof(
            keys,
            "{\"jti\":\"old\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/resource\",\"iat\":799}");
        string future = Proof(
            keys,
            "{\"jti\":\"future\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/resource\",\"iat\":1101}");
        DpopProofValidationPolicy policy = Policy();
        policy.MaximumProofAgeSeconds = 300;

        Assert.Multiple(() =>
        {
            AssertFailure(
                DpopProofValidator.Validate(old, policy),
                "token_too_old");
            AssertFailure(
                DpopProofValidator.Validate(future, policy),
                "issued_at_in_future");
        });

        keys.Dispose();
    }

    [Test]
    public void ProofValidatesNonceAccessTokenHashAndKeyThumbprint()
    {
        TestKeys keys = Keys();
        string accessToken = "access-token-value";
        string hash = AccessTokenHash(accessToken);
        string proof = Proof(
            keys,
            "{\"jti\":\"bound\",\"htm\":\"GET\","
                + "\"htu\":\"https://api.example.test/resource\",\"iat\":1000,"
                + "\"nonce\":\"server-nonce\",\"ath\":\"" + hash + "\"}");
        DpopProofValidationPolicy accepted = Policy();
        accepted.Nonce = "server-nonce";
        accepted.AccessToken = accessToken;
        accepted.ExpectedJwkThumbprint = Thumbprint(keys.Public);

        DpopProofValidationResult success = DpopProofValidator.Validate(
            proof,
            accepted);
        Assert.That(success.Success, Is.True);
        success.PublicKey.ReleaseOwnedMaterial();

        DpopProofValidationPolicy wrongNonce = BoundPolicy(
            accessToken,
            Thumbprint(keys.Public));
        wrongNonce.Nonce = "other-nonce";
        DpopProofValidationPolicy wrongToken = BoundPolicy(
            "other-token",
            Thumbprint(keys.Public));
        wrongToken.Nonce = "server-nonce";
        TestKeys other = Keys();
        DpopProofValidationPolicy wrongKey = BoundPolicy(
            accessToken,
            Thumbprint(other.Public));
        wrongKey.Nonce = "server-nonce";

        Assert.Multiple(() =>
        {
            AssertFailure(
                DpopProofValidator.Validate(proof, wrongNonce),
                "dpop_nonce_mismatch");
            AssertFailure(
                DpopProofValidator.Validate(proof, wrongToken),
                "dpop_access_token_hash_mismatch");
            AssertFailure(
                DpopProofValidator.Validate(proof, wrongKey),
                "dpop_jwk_thumbprint_mismatch");
        });

        keys.Dispose();
        other.Dispose();
    }

    [Test]
    public void RequiredNonceAndAccessTokenHashCannotBeOmitted()
    {
        TestKeys keys = Keys();
        string proof = Proof(keys, RequiredClaims());
        DpopProofValidationPolicy nonce = Policy();
        nonce.Nonce = "required";
        DpopProofValidationPolicy accessToken = BoundPolicy(
            "access-token",
            Thumbprint(keys.Public));

        Assert.Multiple(() =>
        {
            AssertFailure(
                DpopProofValidator.Validate(proof, nonce),
                "missing_dpop_nonce");
            AssertFailure(
                DpopProofValidator.Validate(proof, accessToken),
                "missing_dpop_access_token_hash");
        });

        keys.Dispose();
    }

    [Test]
    public void ReplayCallbackReceivesBoundedIdentifierAndRejectsReuse()
    {
        TestKeys keys = Keys();
        string proof = Proof(keys, RequiredClaims());
        DpopProofValidationPolicy policy = Policy();
        HashSet<string> used = new HashSet<string>();
        string observed = null;
        DateTimeOffset expiration = DateTimeOffset.MinValue;
        policy.JwtPolicy.ReplayValidator = (identifier, expires) =>
        {
            observed = identifier;
            expiration = expires;
            TokenResult result = new TokenResult();
            result.Success = used.Add(identifier);
            return result;
        };

        DpopProofValidationResult first = DpopProofValidator.Validate(
            proof,
            policy);
        DpopProofValidationResult second = DpopProofValidator.Validate(
            proof,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(observed, Is.EqualTo("proof-id"));
            Assert.That(expiration,
                Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(1400)));
            AssertFailure(second, "dpop_proof_replay_detected");
        });

        first.PublicKey.ReleaseOwnedMaterial();
        keys.Dispose();
    }

    [Test]
    public void ReplayCallbackRejectsContradictorySuccessDetails()
    {
        TestKeys keys = Keys();
        string proof = Proof(keys, RequiredClaims());
        DpopProofValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            TokenResult result = new TokenResult();
            result.Success = true;
            result.Error = new TokenError(
                "unexpected_success_error",
                "Unexpected error details.",
                -1);
            return result;
        };

        AssertFailure(
            DpopProofValidator.Validate(proof, policy),
            "invalid_replay_validation_result");

        keys.Dispose();
    }

    [Test]
    public void PolicyRejectsUnsafeOrIncompleteRequestContext()
    {
        DpopProofValidationPolicy missingMethod = Policy();
        missingMethod.HttpMethod = null;
        DpopProofValidationPolicy invalidMethod = Policy();
        invalidMethod.HttpMethod = "GET /";
        DpopProofValidationPolicy insecureUri = Policy();
        insecureUri.HttpUri = "http://api.example.test/resource";
        DpopProofValidationPolicy invalidAge = Policy();
        invalidAge.MaximumProofAgeSeconds = 0;
        DpopProofValidationPolicy invalidNonce = Policy();
        invalidNonce.Nonce = "bad nonce";
        DpopProofValidationPolicy incompleteBinding = Policy();
        incompleteBinding.AccessToken = "access-token";

        Assert.Multiple(() =>
        {
            AssertPolicyFailure(missingMethod);
            AssertPolicyFailure(invalidMethod);
            AssertPolicyFailure(insecureUri);
            AssertPolicyFailure(invalidAge);
            AssertPolicyFailure(invalidNonce);
            AssertPolicyFailure(incompleteBinding);
        });
    }

    [Test]
    public void ValidationRequiresArguments()
    {
        TestKeys keys = Keys();
        string proof = Proof(keys, RequiredClaims());

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DpopProofValidator.Validate(null, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => DpopProofValidator.Validate(proof, null),
                Throws.ArgumentNullException);
        });

        keys.Dispose();
    }

    static string RequiredClaims()
    {
        return "{\"jti\":\"proof-id\",\"htm\":\"GET\","
            + "\"htu\":\"https://api.example.test/resource\",\"iat\":1000}";
    }

    static DpopProofValidationPolicy Policy()
    {
        DpopProofValidationPolicy policy = new DpopProofValidationPolicy();
        TokenPolicy tokenPolicy = new TokenPolicy();
        tokenPolicy.AllowedAlgorithms = new[] { JoseAlgorithms.EcdsaSha256 };
        tokenPolicy.ClockSkewSeconds = 0;
        policy.JwtPolicy = JwtValidationPolicy.Create(tokenPolicy);
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1100));
        policy.HttpMethod = "GET";
        policy.HttpUri = "https://api.example.test/resource?view=full";
        policy.MaximumProofAgeSeconds = 300;
        return policy;
    }

    static DpopProofValidationPolicy BoundPolicy(
        string accessToken,
        string thumbprint)
    {
        DpopProofValidationPolicy policy = Policy();
        policy.AccessToken = accessToken;
        policy.ExpectedJwkThumbprint = thumbprint;
        return policy;
    }

    static string Proof(TestKeys keys, string claims)
    {
        return Proof(
            keys,
            claims,
            "dpop+jwt",
            PublicJwk(keys));
    }

    static string Proof(
        TestKeys keys,
        string claims,
        string? type,
        string jwk)
    {
        return SignedProof(
            keys.Private,
            claims,
            type,
            jwk,
            JoseAlgorithms.EcdsaSha256);
    }

    static string SignedProof(
        TokenKey key,
        string claims,
        string? type,
        string jwk,
        string algorithm)
    {
        string typeMember = type == null
            ? string.Empty
            : "\"typ\":\"" + type + "\",";
        string header = "{" + typeMember
            + "\"alg\":\"" + algorithm + "\",\"jwk\":" + jwk + "}";
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[] { algorithm };
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(claims),
            JoseHeader.Read(header).Header,
            key,
            policy).Text;
    }

    static string PublicJwk(TestKeys keys)
    {
        return JsonWebKey.WritePublic(keys.Public).Text;
    }

    static string Thumbprint(TokenKey key)
    {
        return Base64Url.Encode(JsonWebKey.Thumbprint(key).Bytes).Text;
    }

    static string AccessTokenHash(string value)
    {
        return Base64Url.Encode(
            SHA256.HashData(Encoding.ASCII.GetBytes(value))).Text;
    }

    static TestKeys Keys()
    {
        ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECDsa publicNative = ECDsa.Create();
        publicNative.ImportParameters(native.ExportParameters(false));
        TestKeys result = new TestKeys();
        result.NativePrivate = native;
        result.NativePublic = publicNative;
        result.Private = EcdsaKeyImporter.ImportJose(
            native,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        result.Public = EcdsaKeyImporter.ImportJose(
            publicNative,
            JoseAlgorithms.EcdsaSha256,
            false).Key;
        return result;
    }

    static TokenKey SymmetricKey()
    {
        return SymmetricKeyImporterOperations.ImportJose(
            Enumerable.Range(1, 32).Select(value => (byte)value).ToArray(),
            JoseAlgorithms.HmacSha256).Key;
    }

    static void AssertPolicyFailure(DpopProofValidationPolicy policy)
    {
        TokenResult result = policy.Validate();
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("invalid_dpop_policy"));
    }

    static void AssertFailure(DpopProofValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Jwt, Is.Null);
        Assert.That(result.PublicKey, Is.Null);
        Assert.That(result.JwkThumbprint, Is.Null);
    }

    class TestKeys
    {
        public ECDsa NativePrivate;
        public ECDsa NativePublic;
        public TokenKey Private;
        public TokenKey Public;

        public void Dispose()
        {
            NativePrivate.Dispose();
            NativePublic.Dispose();
        }
    }
}
