using System;
namespace token;

/// <summary>Provides JWT claim, validation, and profile operations.</summary>
public interface IJwtTokenOperations
{
    /// <summary>Creates validated JWT claims from a JSON object.</summary>
    /// <param name="values">The claim members to copy.</param>
    /// <returns>The validated independent claim set, or stable failure details.</returns>
    JwtClaimsResult CreateJwtClaims(JsonObject values);

    /// <summary>Reads a strict JWT claim set under token resource limits.</summary>
    /// <param name="json">The claim-set JSON text.</param>
    /// <param name="policy">The JSON depth and token-size policy.</param>
    /// <returns>The parsed claims, or stable failure details.</returns>
    JwtClaimsResult ReadJwtClaims(string json, TokenPolicy policy);

    /// <summary>Reads strict UTF-8 JWT claims under token resource limits.</summary>
    /// <param name="utf8">The claim-set JSON bytes.</param>
    /// <param name="policy">The JSON depth and token-size policy.</param>
    /// <returns>The parsed claims, or stable failure details.</returns>
    JwtClaimsResult ReadJwtClaimsUtf8(byte[] utf8, TokenPolicy policy);

    /// <summary>Writes JWT claims deterministically as compact JSON.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <param name="policy">The JSON depth and token-size policy.</param>
    /// <returns>The compact JSON text, or stable failure details.</returns>
    TokenTextResult WriteJwtClaims(JwtClaims claims, TokenPolicy policy);

    /// <summary>Writes JWT claims deterministically as compact UTF-8 JSON.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <param name="policy">The JSON depth and token-size policy.</param>
    /// <returns>The compact JSON bytes, or stable failure details.</returns>
    TokenBytesResult WriteJwtClaimsUtf8(JwtClaims claims, TokenPolicy policy);

    /// <summary>Reads a JWT NumericDate value.</summary>
    /// <param name="value">The JSON number value.</param>
    /// <returns>The converted instant and exact numeric representation, or stable failure details.</returns>
    JwtNumericDateResult ReadJwtNumericDate(JsonValue value);

    /// <summary>Reads strict JSON NumericDate number text.</summary>
    /// <param name="numberText">The exact JSON number text.</param>
    /// <returns>The converted instant and exact numeric representation, or stable failure details.</returns>
    JwtNumericDateResult ReadJwtNumericDateText(string numberText);

    /// <summary>Converts NumericDate seconds to a UTC instant.</summary>
    /// <param name="seconds">The seconds since the Unix epoch.</param>
    /// <returns>The converted instant and canonical numeric representation.</returns>
    JwtNumericDateResult ReadJwtNumericDateSeconds(decimal seconds);

    /// <summary>Converts an instant to a canonical JWT NumericDate.</summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>The canonical NumericDate and normalized UTC instant.</returns>
    JwtNumericDateResult CreateJwtNumericDate(DateTimeOffset value);

    /// <summary>Converts the current time from a clock to a canonical JWT NumericDate.</summary>
    /// <param name="clock">The clock supplying the current instant.</param>
    /// <returns>The canonical NumericDate and normalized UTC instant.</returns>
    JwtNumericDateResult CreateJwtNumericDateFromClock(ITokenClock clock);

    /// <summary>Creates a JSON number value for an instant.</summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>A JSON NumericDate value.</returns>
    JsonValue CreateJwtNumericDateValue(DateTimeOffset value);

    /// <summary>Creates a signed compact JWT using key metadata.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="signingKey">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed compact JWT, or stable failure details.</returns>
    TokenTextResult CreateSignedJwt(JwtClaims claims, TokenKey signingKey, TokenPolicy policy);

    /// <summary>Creates a signed compact JWT with a caller-supplied protected header.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="signingKey">The signing key.</param>
    /// <param name="protectedHeader">The protected JWS header to copy and complete.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The signed compact JWT, or stable failure details.</returns>
    TokenTextResult CreateSignedJwtWithHeader(JwtClaims claims, TokenKey signingKey, JoseHeader protectedHeader, TokenPolicy policy);

    /// <summary>Creates an encrypted compact JWT using key metadata.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="encryptionKey">The encryption or key-management key.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encrypted compact JWT, or stable failure details.</returns>
    TokenTextResult CreateEncryptedJwt(JwtClaims claims, TokenKey encryptionKey, string contentEncryptionAlgorithm, TokenPolicy policy);

    /// <summary>Creates an encrypted compact JWT with a caller-supplied protected header.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="encryptionKey">The encryption or key-management key.</param>
    /// <param name="protectedHeader">The protected JWE header to copy and complete.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encrypted compact JWT, or stable failure details.</returns>
    TokenTextResult CreateEncryptedJwtWithHeader(JwtClaims claims, TokenKey encryptionKey, JoseHeader protectedHeader, TokenPolicy policy);

    /// <summary>Creates a signed compact JWT nested inside an encrypted compact JWT.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="signingKey">The inner signing key.</param>
    /// <param name="encryptionKey">The outer encryption or key-management key.</param>
    /// <param name="contentEncryptionAlgorithm">The outer content-encryption algorithm.</param>
    /// <param name="policy">The algorithm, nesting, and resource policy.</param>
    /// <returns>The signed-then-encrypted JWT, or stable failure details.</returns>
    TokenTextResult CreateSignedThenEncryptedJwt(JwtClaims claims, TokenKey signingKey, TokenKey encryptionKey, string contentEncryptionAlgorithm, TokenPolicy policy);

    /// <summary>Creates a signed-then-encrypted JWT with explicit headers for both layers.</summary>
    /// <param name="claims">The JWT claims.</param>
    /// <param name="signingKey">The inner signing key.</param>
    /// <param name="encryptionKey">The outer encryption or key-management key.</param>
    /// <param name="signingHeader">The inner protected JWS header.</param>
    /// <param name="encryptionHeader">The outer protected JWE header.</param>
    /// <param name="policy">The algorithm, nesting, and resource policy.</param>
    /// <returns>The signed-then-encrypted JWT, or stable failure details.</returns>
    TokenTextResult CreateSignedThenEncryptedJwtWithHeaders(JwtClaims claims, TokenKey signingKey, TokenKey encryptionKey, JoseHeader signingHeader, JoseHeader encryptionHeader, TokenPolicy policy);

    /// <summary>Validates a supported signed, encrypted, or nested compact JWT.</summary>
    /// <param name="token">The compact JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>The authenticated claims and JOSE metadata, or stable failure details.</returns>
    JwtValidationResult ValidateJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy);

    /// <summary>Validates a signed compact JWT.</summary>
    /// <param name="token">The compact signed JWT.</param>
    /// <param name="resolver">The signature validation key resolver.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>The authenticated claims and JWS metadata, or stable failure details.</returns>
    JwtValidationResult ValidateSignedJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy);

    /// <summary>Validates an encrypted or signed-then-encrypted compact JWT.</summary>
    /// <param name="token">The compact encrypted JWT.</param>
    /// <param name="resolver">The decryption and nested signature key resolver.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>The authenticated claims and JWE metadata, or stable failure details.</returns>
    JwtValidationResult ValidateEncryptedJwt(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy);

    /// <summary>Validates an OAuth JWT access token.</summary>
    /// <param name="token">The compact access token JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The expected issuer, audience, algorithms, and time policy.</param>
    /// <returns>The authenticated access token, or stable failure details.</returns>
    JwtValidationResult ValidateOAuthJwtAccessToken(string token, ITokenKeyResolver resolver, JwtValidationPolicy policy);

    /// <summary>Validates an OAuth client assertion JWT.</summary>
    /// <param name="token">The compact assertion JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The client assertion profile policy.</param>
    /// <returns>The authenticated JWT metadata, or stable failure details.</returns>
    JwtValidationResult ValidateClientAssertionJwt(
        string token,
        ITokenKeyResolver resolver,
        ClientAssertionJwtValidationPolicy policy);

    /// <summary>Validates a DPoP proof JWT.</summary>
    /// <param name="token">The compact proof JWT.</param>
    /// <param name="policy">The DPoP request and JWT policy.</param>
    /// <returns>The authenticated proof and public-key metadata, or stable failure details.</returns>
    DpopProofValidationResult ValidateDpopProofJwt(
        string token,
        DpopProofValidationPolicy policy);

    /// <summary>Validates a JWT-secured authorization request.</summary>
    /// <param name="token">The compact request JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The authorization request profile policy.</param>
    /// <returns>The authenticated JWT metadata, or stable failure details.</returns>
    JwtValidationResult ValidateSecuredAuthorizationRequest(
        string token,
        ITokenKeyResolver resolver,
        JwtSecuredAuthorizationRequestValidationPolicy policy);

    /// <summary>Validates a JWT-secured authorization response.</summary>
    /// <param name="token">The compact response JWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The authorization response profile policy.</param>
    /// <returns>The authenticated JWT metadata, or stable failure details.</returns>
    JwtValidationResult ValidateSecuredAuthorizationResponse(
        string token,
        ITokenKeyResolver resolver,
        JwtSecuredAuthorizationResponseValidationPolicy policy);

    /// <summary>Validates an OpenID Connect ID Token.</summary>
    /// <param name="token">The compact ID Token.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The ID Token profile policy.</param>
    /// <returns>The authenticated JWT metadata, or stable failure details.</returns>
    JwtValidationResult ValidateOpenIdConnectIdToken(
        string token,
        ITokenKeyResolver resolver,
        OpenIdConnectIdTokenValidationPolicy policy);

    /// <summary>Validates a Security Event Token.</summary>
    /// <param name="token">The compact Security Event Token.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The Security Event Token profile policy.</param>
    /// <returns>The authenticated JWT metadata, or stable failure details.</returns>
    JwtValidationResult ValidateSecurityEventToken(
        string token,
        ITokenKeyResolver resolver,
        SecurityEventTokenValidationPolicy policy);

    /// <summary>Parses an RFC 9901 SD-JWT presentation under explicit limits.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB presentation.</param>
    /// <param name="policy">The parsing and disclosure resource policy.</param>
    /// <returns>The separated issuer JWT, disclosures, and optional Key Binding JWT.</returns>
    SelectiveDisclosureJwtResult ParseSelectiveDisclosureJwt(
        string text,
        SelectiveDisclosureJwtValidationPolicy policy);

    /// <summary>Validates an SD-JWT and its supplied disclosures.</summary>
    /// <param name="text">The combined SD-JWT presentation.</param>
    /// <param name="resolver">The Issuer JWT validation key resolver.</param>
    /// <param name="policy">The SD-JWT validation and disclosure policy.</param>
    /// <returns>The processed claims and disclosure metadata, or stable failure details.</returns>
    SelectiveDisclosureJwtResult ValidateSelectiveDisclosureJwt(
        string text,
        ITokenKeyResolver resolver,
        SelectiveDisclosureJwtValidationPolicy policy);
}
