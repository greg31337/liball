using System;
namespace token;

/// <summary>Validates OAuth, OpenID Connect, security-event, and selective-disclosure JWT profiles.</summary>
public class JwtProfileService : IJwtProfileOperations
{
    /// <summary>Validates an OAuth JWT access token.</summary>
    public JwtValidationResult ValidateOAuthJwtAccessToken(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy)
    {
        return OAuthJwtAccessTokenValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates an OpenID Connect ID Token.</summary>
    public JwtValidationResult ValidateOpenIdConnectIdToken(string token, ITokenKeyResolver resolver, OpenIdConnectIdTokenValidationPolicy policy)
    {
        return OpenIdConnectIdTokenValidator.Validate(token, resolver, policy);
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

    /// <summary>Validates a Security Event Token.</summary>
    public JwtValidationResult ValidateSecurityEventToken(string token, ITokenKeyResolver resolver, SecurityEventTokenValidationPolicy policy)
    {
        return SecurityEventTokenValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates an OAuth client assertion JWT.</summary>
    public JwtValidationResult ValidateClientAssertionJwt(string token, ITokenKeyResolver resolver, ClientAssertionJwtValidationPolicy policy)
    {
        return ClientAssertionJwtValidator.Validate(token, resolver, policy);
    }

    /// <summary>Parses an RFC 9901 SD-JWT presentation under explicit limits.</summary>
    public SelectiveDisclosureJwtResult ParseSelectiveDisclosureJwt(string text, SelectiveDisclosureJwtValidationPolicy policy)
    {
        return SelectiveDisclosureJwt.Parse(text, policy);
    }

    /// <summary>Validates an SD-JWT, its disclosure digests, and required key binding.</summary>
    public SelectiveDisclosureJwtResult ValidateSelectiveDisclosureJwt(string text, ITokenKeyResolver resolver, SelectiveDisclosureJwtValidationPolicy policy)
    {
        return SelectiveDisclosureJwt.Validate(text, resolver, policy);
    }
}
