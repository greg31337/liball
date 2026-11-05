using token;

namespace token.Tests;

public class JwtValidationPolicyTests
{
    [Test]
    public void ValidationResultDefaultsToUnsuccessfulAndUnauthenticated()
    {
        JwtValidationResult result = new JwtValidationResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.Claims, Is.Null);
            Assert.That(result.ProtectedHeader, Is.Null);
            Assert.That(result.InnerProtectedHeader, Is.Null);
            Assert.That(result.Algorithm, Is.Null);
            Assert.That(result.KeyManagementAlgorithm, Is.Null);
            Assert.That(result.EncryptionAlgorithm, Is.Null);
            Assert.That(result.TokenType, Is.Null);
            Assert.That(result.SigningKeyId, Is.Null);
            Assert.That(result.EncryptionKeyId, Is.Null);
            Assert.That(result.SignatureValidated, Is.False);
            Assert.That(result.EncryptionValidated, Is.False);
            Assert.That(result.Nested, Is.False);
        });
    }

    [Test]
    public void ValidationResultCanRepresentNestedAuthenticatedState()
    {
        JwtValidationResult result = new JwtValidationResult();
        result.Success = true;
        result.Kind = TokenKind.JweCompact;
        result.InnerKind = TokenKind.JwsCompact;
        result.Claims = JwtClaims.Read("{\"sub\":\"subject\"}").Claims;
        result.ProtectedHeader = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\"JWT\"}").Header;
        result.InnerProtectedHeader = JoseHeader.Read(
            "{\"alg\":\"HS256\",\"typ\":\"JWT\"}").Header;
        result.Algorithm = JoseAlgorithms.HmacSha256;
        result.KeyManagementAlgorithm = JoseAlgorithms.Direct;
        result.EncryptionAlgorithm = JoseAlgorithms.Aes128Gcm;
        result.TokenType = "JWT";
        result.SigningKeyId = "signing";
        result.EncryptionKeyId = "encryption";
        result.SignatureValidated = true;
        result.EncryptionValidated = true;
        result.Nested = true;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("subject"));
            Assert.That(result.ProtectedHeader.Get(JoseHeaderNames.ContentType).StringValue,
                Is.EqualTo("JWT"));
            Assert.That(result.InnerProtectedHeader.Get(JoseHeaderNames.Algorithm).StringValue,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
        });
    }

    [Test]
    public void ValidationPolicyUsesConservativeDefaults()
    {
        JwtValidationPolicy policy = new JwtValidationPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(policy.TokenPolicy, Is.Not.Null);
            Assert.That(policy.Clock, Is.Not.Null);
            Assert.That(policy.ValidateIssuer, Is.False);
            Assert.That(policy.ValidateAudience, Is.False);
            Assert.That(policy.ValidateExpirationTime, Is.True);
            Assert.That(policy.ValidateNotBefore, Is.True);
            Assert.That(policy.ValidateIssuedAt, Is.True);
            Assert.That(policy.RequireExpirationTime, Is.False);
            Assert.That(policy.ValidateTokenType, Is.False);
            Assert.That(policy.ValidateHeaderClaimReplication, Is.True);
            Assert.That(policy.AllowSignedTokens, Is.True);
            Assert.That(policy.AllowEncryptedTokens, Is.True);
            Assert.That(policy.AllowNestedTokens, Is.True);
            Assert.That(policy.MaximumTokenAgeSeconds, Is.Zero);
            Assert.That(policy.ValidIssuers, Is.Empty);
            Assert.That(policy.ValidAudiences, Is.Empty);
            Assert.That(policy.ValidTokenTypes, Is.Empty);
            Assert.That(policy.SubjectValidator, Is.Null);
            Assert.That(policy.ClaimValidator, Is.Null);
            Assert.That(policy.ReplayValidator, Is.Null);
            Assert.That(policy.Validate().Success, Is.True);
        });
    }

    [Test]
    public void TokenPolicyConstructorCopiesMutableConfiguration()
    {
        TokenPolicy source = new TokenPolicy();
        source.MaximumTokenSizeInBytes = 12345;
        source.MaximumJsonDepth = 12;
        source.ClockSkewSeconds = 17;
        source.AllowCompression = true;
        source.AllowedRemoteKeyHosts = new[] { "keys.example" };
        source.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };
        source.UnderstoodCriticalHeaders = new[] { "urn:example:critical" };
        source.RequiredClaims = new[] { JwtClaimNames.Subject };

        JwtValidationPolicy policy = JwtValidationPolicy.Create(source);
        source.AllowedRemoteKeyHosts[0] = "changed.example";
        source.AllowedAlgorithms[0] = JoseAlgorithms.None;
        source.UnderstoodCriticalHeaders[0] = "changed";
        source.RequiredClaims[0] = JwtClaimNames.Issuer;

        Assert.Multiple(() =>
        {
            Assert.That(policy.TokenPolicy, Is.Not.SameAs(source));
            Assert.That(policy.TokenPolicy.MaximumTokenSizeInBytes, Is.EqualTo(12345));
            Assert.That(policy.TokenPolicy.MaximumJsonDepth, Is.EqualTo(12));
            Assert.That(policy.TokenPolicy.ClockSkewSeconds, Is.EqualTo(17));
            Assert.That(policy.TokenPolicy.AllowCompression, Is.True);
            Assert.That(policy.TokenPolicy.AllowedRemoteKeyHosts,
                Is.EqualTo(new[] { "keys.example" }));
            Assert.That(policy.TokenPolicy.AllowedAlgorithms,
                Is.EqualTo(new[] { JoseAlgorithms.HmacSha256 }));
            Assert.That(policy.TokenPolicy.UnderstoodCriticalHeaders,
                Is.EqualTo(new[] { "urn:example:critical" }));
            Assert.That(policy.TokenPolicy.RequiredClaims,
                Is.EqualTo(new[] { JwtClaimNames.Subject }));
        });
    }

    [Test]
    public void CopyCreatesIndependentMutablePolicyStateAndRetainsCallbacks()
    {
        JwtValidationPolicy original = new JwtValidationPolicy();
        original.TokenPolicy.AllowedAlgorithms = new[] { JoseAlgorithms.HmacSha256 };
        original.TokenPolicy.RequiredClaims = new[] { JwtClaimNames.Subject };
        original.ValidateIssuer = true;
        original.ValidIssuers = new[] { "issuer" };
        original.ValidateAudience = true;
        original.ValidAudiences = new[] { "audience" };
        original.ValidateTokenType = true;
        original.ValidTokenTypes = new[] { "JWT" };
        original.RequireExpirationTime = true;
        original.MaximumTokenAgeSeconds = 3600;
        original.Clock = new TokenClock(() => DateTimeOffset.UnixEpoch);
        original.SubjectValidator = claims => Success();
        original.ClaimValidator = (name, value) => Success();
        original.ReplayValidator = (identifier, expiration) => Success();

        JwtValidationPolicy copy = original.Copy();
        copy.TokenPolicy.AllowedAlgorithms[0] = JoseAlgorithms.None;
        copy.TokenPolicy.RequiredClaims[0] = JwtClaimNames.Issuer;
        copy.ValidIssuers[0] = "changed-issuer";
        copy.ValidAudiences[0] = "changed-audience";
        copy.ValidTokenTypes[0] = "at+jwt";

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.TokenPolicy, Is.Not.SameAs(original.TokenPolicy));
            Assert.That(original.TokenPolicy.AllowedAlgorithms[0],
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(original.TokenPolicy.RequiredClaims[0],
                Is.EqualTo(JwtClaimNames.Subject));
            Assert.That(original.ValidIssuers[0], Is.EqualTo("issuer"));
            Assert.That(original.ValidAudiences[0], Is.EqualTo("audience"));
            Assert.That(original.ValidTokenTypes[0], Is.EqualTo("JWT"));
            Assert.That(copy.Clock, Is.SameAs(original.Clock));
            Assert.That(copy.SubjectValidator, Is.SameAs(original.SubjectValidator));
            Assert.That(copy.ClaimValidator, Is.SameAs(original.ClaimValidator));
            Assert.That(copy.ReplayValidator, Is.SameAs(original.ReplayValidator));
            Assert.That(copy.RequireExpirationTime, Is.True);
            Assert.That(copy.MaximumTokenAgeSeconds, Is.EqualTo(3600));
            Assert.That(copy.Validate().Success, Is.True);
        });
    }

    [Test]
    public void ValidateAcceptsConfiguredIssuerAudienceTypeAndMaximumAge()
    {
        JwtValidationPolicy policy = new JwtValidationPolicy();
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "issuer-one", "issuer-two" };
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api", "admin" };
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "JWT", "at+jwt" };
        policy.RequireExpirationTime = true;
        policy.MaximumTokenAgeSeconds = 600;
        policy.TokenPolicy.RequiredClaims = new[]
        {
            JwtClaimNames.Subject,
            JwtClaimNames.JwtId
        };

        TokenResult result = policy.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
        });
    }

    [Test]
    public void ValidateRejectsMissingAndContradictoryConfiguration()
    {
        JwtValidationPolicy missingBase = new JwtValidationPolicy();
        missingBase.TokenPolicy = null;
        JwtValidationPolicy missingClock = new JwtValidationPolicy();
        missingClock.Clock = null;
        JwtValidationPolicy negativeSkew = new JwtValidationPolicy();
        negativeSkew.TokenPolicy.ClockSkewSeconds = -1;
        JwtValidationPolicy negativeAge = new JwtValidationPolicy();
        negativeAge.MaximumTokenAgeSeconds = -1;
        JwtValidationPolicy noForms = new JwtValidationPolicy();
        noForms.AllowSignedTokens = false;
        noForms.AllowEncryptedTokens = false;
        noForms.AllowNestedTokens = false;
        JwtValidationPolicy invalidNested = new JwtValidationPolicy();
        invalidNested.AllowSignedTokens = false;
        JwtValidationPolicy requiredWithoutCheck = new JwtValidationPolicy();
        requiredWithoutCheck.RequireExpirationTime = true;
        requiredWithoutCheck.ValidateExpirationTime = false;
        JwtValidationPolicy ageWithoutIssuedAt = new JwtValidationPolicy();
        ageWithoutIssuedAt.MaximumTokenAgeSeconds = 1;
        ageWithoutIssuedAt.ValidateIssuedAt = false;

        Assert.Multiple(() =>
        {
            AssertInvalid(missingBase);
            AssertInvalid(missingClock);
            AssertInvalid(negativeSkew);
            AssertInvalid(negativeAge);
            AssertInvalid(noForms);
            AssertInvalid(invalidNested);
            AssertInvalid(requiredWithoutCheck);
            AssertInvalid(ageWithoutIssuedAt);
        });
    }

    [Test]
    public void ValidateRejectsEnabledChecksWithoutValuesAndInvalidNameLists()
    {
        JwtValidationPolicy issuer = new JwtValidationPolicy();
        issuer.ValidateIssuer = true;
        JwtValidationPolicy audience = new JwtValidationPolicy();
        audience.ValidateAudience = true;
        JwtValidationPolicy type = new JwtValidationPolicy();
        type.ValidateTokenType = true;
        JwtValidationPolicy nullList = new JwtValidationPolicy();
        nullList.ValidIssuers = null;
        JwtValidationPolicy emptyValue = new JwtValidationPolicy();
        emptyValue.ValidAudiences = new[] { "" };
        JwtValidationPolicy nullValue = new JwtValidationPolicy();
        nullValue.ValidTokenTypes = new string[] { null };
        JwtValidationPolicy duplicate = new JwtValidationPolicy();
        duplicate.TokenPolicy.RequiredClaims = new[] { "sub", "sub" };

        Assert.Multiple(() =>
        {
            AssertInvalid(issuer);
            AssertInvalid(audience);
            AssertInvalid(type);
            AssertInvalid(nullList);
            AssertInvalid(emptyValue);
            AssertInvalid(nullValue);
            AssertInvalid(duplicate);
        });
    }

    [Test]
    public void CopyPreservesAnInvalidMissingBasePolicyForValidation()
    {
        JwtValidationPolicy original = new JwtValidationPolicy();
        original.TokenPolicy = null;

        JwtValidationPolicy copy = original.Copy();

        Assert.Multiple(() =>
        {
            Assert.That(copy.TokenPolicy, Is.Null);
            AssertInvalid(copy);
        });
    }

    [Test]
    public void ConstructorRejectsNullBasePolicy()
    {
        Assert.That(
            () => JwtValidationPolicy.Create(null),
            Throws.ArgumentNullException);
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static void AssertInvalid(JwtValidationPolicy policy)
    {
        TokenResult result = policy.Validate();
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo("invalid_policy"));
    }
}
