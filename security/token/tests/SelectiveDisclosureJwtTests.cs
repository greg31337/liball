using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class SelectiveDisclosureJwtTests
{
    [Test]
    public void ParseSeparatesIssuerDisclosuresAndKeyBinding()
    {
        TokenKey key = HmacKey(1);
        string issuer = Signed("{}", key);
        string objectDisclosure = Disclosure(
            "[\"salt-1\",\"name\",\"Alice\"]");
        string arrayDisclosure = Disclosure("[\"salt-2\",42]");
        string keyBinding = Signed("{\"iat\":1000}", key, "kb+jwt");

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Parse(
            issuer + "~" + objectDisclosure + "~" + arrayDisclosure
                + "~" + keyBinding);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.SdJwt));
            Assert.That(result.IssuerSignedJwt, Is.EqualTo(issuer));
            Assert.That(result.KeyBindingJwt, Is.EqualTo(keyBinding));
            Assert.That(result.Disclosures, Has.Length.EqualTo(2));
            Assert.That(result.Disclosures[0].Salt, Is.EqualTo("salt-1"));
            Assert.That(result.Disclosures[0].ClaimName, Is.EqualTo("name"));
            Assert.That(result.Disclosures[0].Value.StringValue,
                Is.EqualTo("Alice"));
            Assert.That(result.Disclosures[0].ArrayElement, Is.False);
            Assert.That(result.Disclosures[1].ArrayElement, Is.True);
            Assert.That(result.Disclosures[1].Value.NumberText,
                Is.EqualTo("42"));
            Assert.That(result.IssuerJwt, Is.Null);
            Assert.That(result.DigestsVerified, Is.False);
        });
    }

    [Test]
    public void ParseAcceptsRequiredTrailingTildeWithoutDisclosures()
    {
        string issuer = Signed("{\"iss\":\"issuer\"}", HmacKey(1));

        SelectiveDisclosureJwtResult result =
            SelectiveDisclosureJwt.Parse(issuer + "~");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Disclosures, Is.Empty);
            Assert.That(result.KeyBindingJwt, Is.Null);
        });
    }

    [Test]
    public void OfficialDisclosureDigestVectorIsVerified()
    {
        const string disclosure =
            "WyJfMjZiYzRMVC1hYzZxMktJNmNCVzVlcyIsICJmYW1pbHlfbmFtZSIsICJNw7ZiaXVzIl0";
        const string digest =
            "X9yH0Ajrdm1Oij4tWso9UzzKJvPoDxwmuEcO3XAdRC0";
        string claims = "{\"given_name\":\"Alice\",\"_sd\":[\""
            + digest + "\"]}";
        TokenKey key = HmacKey(1);

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~" + disclosure + "~",
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DigestsVerified, Is.True);
            Assert.That(result.HashAlgorithm,
                Is.EqualTo(SelectiveDisclosureHashAlgorithms.Sha256));
            Assert.That(result.IssuerJwt.SignatureValidated, Is.True);
            Assert.That(result.Claims.Get("given_name").StringValue,
                Is.EqualTo("Alice"));
            Assert.That(result.Claims.Get("family_name").StringValue,
                Is.EqualTo("Möbius"));
            Assert.That(result.Claims.Get("_sd"), Is.Null);
        });
    }

    [Test]
    public void RecursiveObjectAndArrayDisclosuresAreProcessed()
    {
        string country = Disclosure("[\"salt-country\",\"country\",\"DE\"]");
        string countryDigest = Digest(country, "sha-256");
        string addressJson = "[\"salt-address\",\"address\",{\"_sd\":[\""
            + countryDigest + "\"]}]";
        string address = Disclosure(addressJson);
        string nationality = Disclosure("[\"salt-nationality\",\"FR\"]");
        string claims = "{\"_sd_alg\":\"sha-256\",\"_sd\":[\""
            + Digest(address, "sha-256") + "\"],\"nationalities\":[\"DE\","
            + "{\"...\":\"" + Digest(nationality, "sha-256")
            + "\"},\"US\"]}";
        TokenKey key = HmacKey(1);

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~" + address + "~" + country + "~"
                + nationality + "~",
            Resolver(key),
            Policy());

        JsonValue nationalities = result.Claims.Get("nationalities");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Get("_sd_alg"), Is.Null);
            Assert.That(
                result.Claims.Get("address").ObjectValue.Get("country").StringValue,
                Is.EqualTo("DE"));
            Assert.That(
                nationalities.ArrayValue.Select(value => value.StringValue),
                Is.EqualTo(new[] { "DE", "FR", "US" }));
        });
    }

    [Test]
    public void OmittedDecoysAndArrayElementsAreRemoved()
    {
        string decoy = Base64Url.Encode(Enumerable.Repeat((byte)7, 32).ToArray()).Text;
        string claims = "{\"_sd\":[\"" + decoy
            + "\"],\"values\":[1,{\"...\":\"" + Base64Url.Encode(
                Enumerable.Repeat((byte)8, 32).ToArray()).Text + "\"},3]}";
        TokenKey key = HmacKey(1);

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~",
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Get("_sd"), Is.Null);
            Assert.That(
                result.Claims.Get("values").ArrayValue
                    .Select(value => value.NumberText),
                Is.EqualTo(new[] { "1", "3" }));
        });
    }

    [Test]
    public void Sha384CanBeAllowedExplicitly()
    {
        string disclosure = Disclosure("[\"salt\",\"role\",\"admin\"]");
        string claims = "{\"_sd_alg\":\"sha-384\",\"_sd\":[\""
            + Digest(disclosure, "sha-384") + "\"]}";
        TokenKey key = HmacKey(1);
        SelectiveDisclosureJwtValidationPolicy allowed = Policy();
        allowed.AllowedHashAlgorithms = new[] { "sha-256", "sha-384" };

        SelectiveDisclosureJwtResult accepted = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~" + disclosure + "~",
            Resolver(key),
            allowed);
        SelectiveDisclosureJwtResult rejected = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~" + disclosure + "~",
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.HashAlgorithm, Is.EqualTo("sha-384"));
            Assert.That(accepted.Claims.Get("role").StringValue,
                Is.EqualTo("admin"));
            AssertFailure(rejected, "unsupported_sd_jwt_hash_algorithm");
        });
    }

    [TestCase("not-an-sd-jwt", "invalid_sd_jwt_serialization")]
    [TestCase("~", "invalid_sd_jwt_serialization")]
    public void InvalidCombinedSerializationIsRejected(string text, string code)
    {
        AssertFailure(SelectiveDisclosureJwt.Parse(text), code);
    }

    [Test]
    public void EmptyDisclosureAndInvalidKeyBindingAreRejected()
    {
        string issuer = Signed("{}", HmacKey(1));

        Assert.Multiple(() =>
        {
            AssertFailure(
                SelectiveDisclosureJwt.Parse(issuer + "~~"),
                "invalid_sd_jwt_serialization");
            AssertFailure(
                SelectiveDisclosureJwt.Parse(issuer + "~not-a-jwt"),
                "invalid_sd_jwt_key_binding");
        });
    }

    [TestCase("bad=", "invalid_sd_jwt_disclosure")]
    [TestCase("{}", "invalid_sd_jwt_disclosure")]
    [TestCase("[\"salt\"]", "invalid_sd_jwt_disclosure")]
    [TestCase("[1,\"value\"]", "invalid_sd_jwt_disclosure")]
    [TestCase("[\"salt\",1,\"value\"]", "invalid_sd_jwt_disclosure")]
    [TestCase("[\"salt\",\"_sd\",\"value\"]", "invalid_sd_jwt_disclosure")]
    [TestCase("[\"salt\",\"...\",\"value\"]", "invalid_sd_jwt_disclosure")]
    public void InvalidDisclosuresAreRejected(string value, string code)
    {
        string encoded = value.EndsWith("=", StringComparison.Ordinal)
            ? value
            : Disclosure(value);
        string issuer = Signed("{}", HmacKey(1));

        AssertFailure(
            SelectiveDisclosureJwt.Parse(issuer + "~" + encoded + "~"),
            code);
    }

    [Test]
    public void TamperedAndUnreferencedDisclosuresAreRejected()
    {
        string original = Disclosure("[\"salt\",\"role\",\"admin\"]");
        string altered = Disclosure("[\"salt\",\"role\",\"user\"]");
        string claims = "{\"_sd\":[\"" + Digest(original, "sha-256")
            + "\"]}";
        TokenKey key = HmacKey(1);

        AssertFailure(
            SelectiveDisclosureJwt.Validate(
                Signed(claims, key) + "~" + altered + "~",
                Resolver(key),
                Policy()),
            "unreferenced_sd_jwt_disclosure");
    }

    [Test]
    public void DuplicateDisclosuresAndEmbeddedDigestsAreRejected()
    {
        string disclosure = Disclosure("[\"salt\",\"role\",\"admin\"]");
        string digest = Digest(disclosure, "sha-256");
        string oneDigest = "{\"_sd\":[\"" + digest + "\"]}";
        string duplicateDigest = "{\"_sd\":[\"" + digest + "\",\""
            + digest + "\"]}";
        TokenKey key = HmacKey(1);

        Assert.Multiple(() =>
        {
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    Signed(oneDigest, key) + "~" + disclosure + "~"
                        + disclosure + "~",
                    Resolver(key),
                    Policy()),
                "duplicate_sd_jwt_disclosure");
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    Signed(duplicateDigest, key) + "~" + disclosure + "~",
                    Resolver(key),
                    Policy()),
                "duplicate_sd_jwt_digest");
        });
    }

    [Test]
    public void ClaimCollisionsAndWrongDisclosureContextsAreRejected()
    {
        string property = Disclosure("[\"salt\",\"role\",\"admin\"]");
        string element = Disclosure("[\"salt\",\"admin\"]");
        string collision = "{\"role\":\"user\",\"_sd\":[\""
            + Digest(property, "sha-256") + "\"]}";
        string wrongContext = "{\"_sd\":[\""
            + Digest(element, "sha-256") + "\"]}";
        TokenKey key = HmacKey(1);

        Assert.Multiple(() =>
        {
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    Signed(collision, key) + "~" + property + "~",
                    Resolver(key),
                    Policy()),
                "sd_jwt_claim_collision");
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    Signed(wrongContext, key) + "~" + element + "~",
                    Resolver(key),
                    Policy()),
                "invalid_sd_jwt_disclosure_context");
        });
    }

    [Test]
    public void MalformedDigestStructuresAreRejected()
    {
        string validDigest = Base64Url.Encode(
            Enumerable.Repeat((byte)9, 32).ToArray()).Text;
        TokenKey key = HmacKey(1);
        string invalidArray = Signed("{\"_sd\":true}", key) + "~";
        string invalidDigest = Signed("{\"_sd\":[\"short\"]}", key) + "~";
        string nestedAlgorithm = Signed(
            "{\"nested\":{\"_sd_alg\":\"sha-256\"}}",
            key) + "~";
        string malformedPlaceholder = Signed(
            "{\"values\":[{\"...\":\"" + validDigest
                + "\",\"other\":true}]}",
            key) + "~";

        Assert.Multiple(() =>
        {
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    invalidArray,
                    Resolver(key),
                    Policy()),
                "invalid_sd_jwt_digest_array");
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    invalidDigest,
                    Resolver(key),
                    Policy()),
                "invalid_sd_jwt_digest");
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    nestedAlgorithm,
                    Resolver(key),
                    Policy()),
                "nested_sd_jwt_hash_algorithm");
            AssertFailure(
                SelectiveDisclosureJwt.Validate(
                    malformedPlaceholder,
                    Resolver(key),
                    Policy()),
                "invalid_sd_jwt_array_placeholder");
        });
    }

    [Test]
    public void DisclosedClaimsAreValidatedAfterDigestProcessing()
    {
        string issuer = Disclosure("[\"salt-iss\",\"iss\",\"issuer\"]");
        string expiration = Disclosure("[\"salt-exp\",\"exp\",1200]");
        string email = Disclosure(
            "[\"salt-email\",\"email\",\"alice@example.com\"]");
        string claims = "{\"_sd\":[\"" + Digest(issuer, "sha-256")
            + "\",\"" + Digest(expiration, "sha-256") + "\",\""
            + Digest(email, "sha-256") + "\"]}";
        TokenKey key = HmacKey(1);
        SelectiveDisclosureJwtValidationPolicy policy = Policy();
        policy.JwtPolicy.ValidateIssuer = true;
        policy.JwtPolicy.ValidIssuers = new[] { "issuer" };
        policy.JwtPolicy.RequireExpirationTime = true;
        policy.JwtPolicy.TokenPolicy.RequiredClaims = new[] { "email" };

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Validate(
            Signed(claims, key) + "~" + issuer + "~" + expiration + "~"
                + email + "~",
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Get("iss").StringValue,
                Is.EqualTo("issuer"));
            Assert.That(result.Claims.Get("exp").NumberText,
                Is.EqualTo("1200"));
            Assert.That(result.Claims.Get("email").StringValue,
                Is.EqualTo("alice@example.com"));
        });
    }

    [Test]
    public void IssuerSignatureMustValidateBeforeClaimsAreExposed()
    {
        string disclosure = Disclosure("[\"salt\",\"role\",\"admin\"]");
        string claims = "{\"_sd\":[\"" + Digest(disclosure, "sha-256")
            + "\"]}";

        SelectiveDisclosureJwtResult result = SelectiveDisclosureJwt.Validate(
            Signed(claims, HmacKey(1)) + "~" + disclosure + "~",
            Resolver(HmacKey(2)),
            Policy());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.IssuerJwt, Is.Null);
    }

    [Test]
    public void PolicyAndResourceLimitsAreEnforced()
    {
        SelectiveDisclosureJwtValidationPolicy empty = Policy();
        empty.AllowedHashAlgorithms = Array.Empty<string>();
        SelectiveDisclosureJwtValidationPolicy unsupported = Policy();
        unsupported.AllowedHashAlgorithms = new[] { "sha-1" };
        SelectiveDisclosureJwtValidationPolicy duplicate = Policy();
        duplicate.AllowedHashAlgorithms = new[] { "sha-256", "sha-256" };
        SelectiveDisclosureJwtValidationPolicy count = Policy();
        count.MaximumDisclosures = 0;
        SelectiveDisclosureJwtValidationPolicy size = Policy();
        size.MaximumDisclosureSizeInBytes = 1;
        string issuer = Signed("{}", HmacKey(1));
        string disclosure = Disclosure("[\"salt\",\"name\",\"Alice\"]");

        Assert.Multiple(() =>
        {
            AssertPolicyFailure(empty);
            AssertPolicyFailure(unsupported);
            AssertPolicyFailure(duplicate);
            AssertFailure(
                SelectiveDisclosureJwt.Parse(
                    issuer + "~" + disclosure + "~",
                    count),
                "too_many_sd_jwt_disclosures");
            AssertFailure(
                SelectiveDisclosureJwt.Parse(
                    issuer + "~" + disclosure + "~",
                    size),
                "sd_jwt_disclosure_too_large");
        });
    }

    [Test]
    public void PublicMethodsRequireArguments()
    {
        TokenKey key = HmacKey(1);
        string text = Signed("{}", key) + "~";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => SelectiveDisclosureJwt.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => SelectiveDisclosureJwt.Parse(text, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => SelectiveDisclosureJwt.Validate(
                    null,
                    Resolver(key),
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => SelectiveDisclosureJwt.Validate(
                    text,
                    null,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => SelectiveDisclosureJwt.Validate(
                    text,
                    Resolver(key),
                    null),
                Throws.ArgumentNullException);
        });
    }

    static string Disclosure(string json)
    {
        return Base64Url.Encode(Encoding.UTF8.GetBytes(json)).Text;
    }

    static string Digest(string disclosure, string algorithm)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(disclosure);
        byte[] digest;
        if (string.Equals(algorithm, "sha-384", StringComparison.Ordinal))
        {
            digest = SHA384.HashData(bytes);
        }
        else if (string.Equals(algorithm, "sha-512", StringComparison.Ordinal))
        {
            digest = SHA512.HashData(bytes);
        }
        else
        {
            digest = SHA256.HashData(bytes);
        }

        return Base64Url.Encode(digest).Text;
    }

    static string Signed(string claims, TokenKey key)
    {
        return Signed(claims, key, "example+sd-jwt");
    }

    static string Signed(string claims, TokenKey key, string type)
    {
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"HS256\",\"typ\":\"" + type + "\"}").Header;
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(claims),
            header,
            key,
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
    }

    static SelectiveDisclosureJwtValidationPolicy Policy()
    {
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        tokenPolicy.ClockSkewSeconds = 0;
        SelectiveDisclosureJwtValidationPolicy policy =
            new SelectiveDisclosureJwtValidationPolicy();
        policy.JwtPolicy = JwtValidationPolicy.Create(tokenPolicy);
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        return policy;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKey HmacKey(int start)
    {
        return SymmetricKeyImporterOperations.ImportJose(
            Enumerable.Range(start, 32).Select(value => (byte)value).ToArray(),
            JoseAlgorithms.HmacSha256).Key;
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

    static void AssertFailure(SelectiveDisclosureJwtResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Claims, Is.Null);
        });
    }

    static void AssertPolicyFailure(
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        TokenResult result = policy.Validate();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_sd_jwt_policy"));
        });
    }
}
