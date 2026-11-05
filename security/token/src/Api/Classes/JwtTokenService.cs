using System;
namespace token;

/// <summary>Provides JWT claim conversion, creation, and authenticated validation operations.</summary>
public class JwtTokenService : IJwtTokenOperations
{
    /// <summary>Creates validated JWT claims from a JSON object.</summary>
    public JwtClaimsResult CreateJwtClaims(JsonObject values)
    {
        return JwtClaims.FromObject(values);
    }

    /// <summary>Reads a strict JWT claim set under token resource limits.</summary>
    public JwtClaimsResult ReadJwtClaims(string json, TokenPolicy policy)
    {
        return JwtClaims.Read(json, policy);
    }

    /// <summary>Reads strict UTF-8 JWT claims under token resource limits.</summary>
    public JwtClaimsResult ReadJwtClaimsUtf8(byte[] utf8, TokenPolicy policy)
    {
        return JwtClaims.Read(utf8, policy);
    }

    /// <summary>Writes JWT claims deterministically as compact JSON.</summary>
    public TokenTextResult WriteJwtClaims(JwtClaims claims, TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.WriteWithPolicy(policy);
    }

    /// <summary>Writes JWT claims deterministically as compact UTF-8 JSON.</summary>
    public TokenBytesResult WriteJwtClaimsUtf8(JwtClaims claims, TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.WriteUtf8WithPolicy(policy);
    }

    /// <summary>Reads a JWT NumericDate value.</summary>
    public JwtNumericDateResult ReadJwtNumericDate(JsonValue value)
    {
        return JwtNumericDate.ToDateTimeOffset(value);
    }

    /// <summary>Reads strict JSON NumericDate number text.</summary>
    public JwtNumericDateResult ReadJwtNumericDateText(string numberText)
    {
        return JwtNumericDate.ToDateTimeOffset(numberText);
    }

    /// <summary>Converts NumericDate seconds to a UTC instant.</summary>
    public JwtNumericDateResult ReadJwtNumericDateSeconds(decimal seconds)
    {
        return JwtNumericDate.ToDateTimeOffset(seconds);
    }

    /// <summary>Converts an instant to a canonical JWT NumericDate.</summary>
    public JwtNumericDateResult CreateJwtNumericDate(DateTimeOffset value)
    {
        return JwtNumericDate.FromDateTimeOffset(value);
    }

    /// <summary>Converts the current time from a clock to a canonical JWT NumericDate.</summary>
    public JwtNumericDateResult CreateJwtNumericDateFromClock(ITokenClock clock)
    {
        return JwtNumericDate.FromClock(clock);
    }

    /// <summary>Creates a JSON number value for an instant.</summary>
    public JsonValue CreateJwtNumericDateValue(DateTimeOffset value)
    {
        return JwtNumericDate.ToJsonValue(value);
    }

    /// <summary>Creates a signed compact JWT using key metadata.</summary>
    public TokenTextResult CreateSignedJwt(JwtClaims claims, TokenKey signingKey, TokenPolicy policy)
    {
        return JwtCreator.CreateSigned(claims, signingKey, policy);
    }

    /// <summary>Creates a signed compact JWT with a caller-supplied protected header.</summary>
    public TokenTextResult CreateSignedJwtWithHeader(JwtClaims claims, TokenKey signingKey, JoseHeader protectedHeader, TokenPolicy policy)
    {
        return JwtCreator.CreateSigned(
            claims,
            signingKey,
            protectedHeader,
            policy);
    }

    /// <summary>Creates an encrypted compact JWT using key metadata.</summary>
    public TokenTextResult CreateEncryptedJwt(JwtClaims claims, TokenKey encryptionKey, string contentEncryptionAlgorithm, TokenPolicy policy)
    {
        return JwtCreator.CreateEncrypted(
            claims,
            encryptionKey,
            contentEncryptionAlgorithm,
            policy);
    }

    /// <summary>Creates an encrypted compact JWT with a caller-supplied protected header.</summary>
    public TokenTextResult CreateEncryptedJwtWithHeader(JwtClaims claims, TokenKey encryptionKey, JoseHeader protectedHeader, TokenPolicy policy)
    {
        return JwtCreator.CreateEncrypted(
            claims,
            encryptionKey,
            protectedHeader,
            policy);
    }

    /// <summary>Creates a signed compact JWT nested inside an encrypted compact JWT.</summary>
    public TokenTextResult CreateSignedThenEncryptedJwt(JwtClaims claims, TokenKey signingKey, TokenKey encryptionKey, string contentEncryptionAlgorithm, TokenPolicy policy)
    {
        return JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            contentEncryptionAlgorithm,
            policy);
    }

    /// <summary>Creates a signed-then-encrypted JWT with explicit headers for both layers.</summary>
    public TokenTextResult CreateSignedThenEncryptedJwtWithHeaders(JwtClaims claims, TokenKey signingKey, TokenKey encryptionKey, JoseHeader signingHeader, JoseHeader encryptionHeader, TokenPolicy policy)
    {
        return JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            signingHeader,
            encryptionHeader,
            policy);
    }

    /// <summary>Validates a supported signed, encrypted, or nested compact JWT.</summary>
    public JwtValidationResult ValidateJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy)
    {
        return JwtValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates a signed compact JWT.</summary>
    public JwtValidationResult ValidateSignedJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy)
    {
        return JwtValidator.ValidateSigned(token, resolver, policy);
    }

    /// <summary>Validates an encrypted or signed-then-encrypted compact JWT.</summary>
    public JwtValidationResult ValidateEncryptedJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy)
    {
        return JwtValidator.ValidateEncrypted(token, resolver, policy);
    }

    /// <summary>Validates an OAuth JWT access token.</summary>
    public JwtValidationResult ValidateOAuthJwtAccessToken(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy)
    {
        return OAuthJwtAccessTokenValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates an OAuth client assertion JWT.</summary>
    public JwtValidationResult ValidateClientAssertionJwt(string token, ITokenKeyResolver resolver, ClientAssertionJwtValidationPolicy policy)
    {
        return ClientAssertionJwtValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates a DPoP proof JWT.</summary>
    public DpopProofValidationResult ValidateDpopProofJwt(string token, DpopProofValidationPolicy policy)
    {
        return DpopProofValidator.Validate(token, policy);
    }

    /// <summary>Validates a JWT-secured authorization request.</summary>
    public JwtValidationResult ValidateSecuredAuthorizationRequest(string token, ITokenKeyResolver resolver, JwtSecuredAuthorizationRequestValidationPolicy policy)
    {
        return JwtSecuredAuthorizationRequestValidator.Validate(
            token,
            resolver,
            policy);
    }

    /// <summary>Validates a JWT-secured authorization response.</summary>
    public JwtValidationResult ValidateSecuredAuthorizationResponse(string token, ITokenKeyResolver resolver, JwtSecuredAuthorizationResponseValidationPolicy policy)
    {
        return JwtSecuredAuthorizationResponseValidator.Validate(
            token,
            resolver,
            policy);
    }

    /// <summary>Validates an OpenID Connect ID Token.</summary>
    public JwtValidationResult ValidateOpenIdConnectIdToken(string token, ITokenKeyResolver resolver, OpenIdConnectIdTokenValidationPolicy policy)
    {
        return OpenIdConnectIdTokenValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates a Security Event Token.</summary>
    public JwtValidationResult ValidateSecurityEventToken(string token, ITokenKeyResolver resolver, SecurityEventTokenValidationPolicy policy)
    {
        return SecurityEventTokenValidator.Validate(token, resolver, policy);
    }

    /// <summary>Parses an RFC 9901 SD-JWT presentation under explicit limits.</summary>
    public SelectiveDisclosureJwtResult ParseSelectiveDisclosureJwt(string text, SelectiveDisclosureJwtValidationPolicy policy)
    {
        return SelectiveDisclosureJwt.Parse(text, policy);
    }

    /// <summary>Validates an SD-JWT and its supplied disclosures.</summary>
    public SelectiveDisclosureJwtResult ValidateSelectiveDisclosureJwt(string text, ITokenKeyResolver resolver, SelectiveDisclosureJwtValidationPolicy policy)
    {
        return SelectiveDisclosureJwt.Validate(text, resolver, policy);
    }
}
