using System;
namespace token;

/// <summary>Validates application profiles built on JSON Web Tokens.</summary>
public interface IJwtProfileOperations
{
    /// <summary>Validates an OAuth JWT access token.</summary>
    /// <param name="token">The compact access token JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The expected issuer, audience, algorithms, and time policy.</param>
    /// <returns>The authenticated access token, or stable failure details.</returns>
    JwtValidationResult ValidateOAuthJwtAccessToken(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy);

    /// <summary>Validates an OpenID Connect ID Token.</summary>
    /// <param name="token">The compact ID Token.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The ID Token profile policy.</param>
    /// <returns>The authenticated ID Token, or stable failure details.</returns>
    JwtValidationResult ValidateOpenIdConnectIdToken(string token, ITokenKeyResolver resolver, OpenIdConnectIdTokenValidationPolicy policy);

    /// <summary>Validates a DPoP proof JWT.</summary>
    /// <param name="token">The compact proof JWT.</param>
    /// <param name="policy">The DPoP request and JWT policy.</param>
    /// <returns>The authenticated proof and public-key metadata, or stable failure details.</returns>
    DpopProofValidationResult ValidateDpopProofJwt(string token, DpopProofValidationPolicy policy);

    /// <summary>Validates a JWT-secured authorization request.</summary>
    /// <param name="token">The compact request JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The authorization request profile policy.</param>
    /// <returns>The authenticated request, or stable failure details.</returns>
    JwtValidationResult ValidateSecuredAuthorizationRequest(string token, ITokenKeyResolver resolver, JwtSecuredAuthorizationRequestValidationPolicy policy);

    /// <summary>Validates a JWT-secured authorization response.</summary>
    /// <param name="token">The compact response JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The authorization response profile policy.</param>
    /// <returns>The authenticated response, or stable failure details.</returns>
    JwtValidationResult ValidateSecuredAuthorizationResponse(string token, ITokenKeyResolver resolver, JwtSecuredAuthorizationResponseValidationPolicy policy);

    /// <summary>Validates a Security Event Token.</summary>
    /// <param name="token">The compact Security Event Token.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The Security Event Token profile policy.</param>
    /// <returns>The authenticated event token, or stable failure details.</returns>
    JwtValidationResult ValidateSecurityEventToken(string token, ITokenKeyResolver resolver, SecurityEventTokenValidationPolicy policy);

    /// <summary>Validates an OAuth client assertion JWT.</summary>
    /// <param name="token">The compact assertion JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The client assertion profile policy.</param>
    /// <returns>The authenticated assertion, or stable failure details.</returns>
    JwtValidationResult ValidateClientAssertionJwt(string token, ITokenKeyResolver resolver, ClientAssertionJwtValidationPolicy policy);

    /// <summary>Parses an RFC 9901 SD-JWT presentation under explicit limits.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB presentation.</param>
    /// <param name="policy">The parsing and disclosure resource policy.</param>
    /// <returns>The separated issuer JWT, disclosures, and optional Key Binding JWT.</returns>
    SelectiveDisclosureJwtResult ParseSelectiveDisclosureJwt(string text, SelectiveDisclosureJwtValidationPolicy policy);

    /// <summary>Validates an SD-JWT, its disclosure digests, and required key binding.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB presentation.</param>
    /// <param name="resolver">The issuer JWT validation key resolver.</param>
    /// <param name="policy">The disclosure, issuer, and key-binding policy.</param>
    /// <returns>The processed claims and validation metadata, or stable failure details.</returns>
    SelectiveDisclosureJwtResult ValidateSelectiveDisclosureJwt(string text, ITokenKeyResolver resolver, SelectiveDisclosureJwtValidationPolicy policy);
}
