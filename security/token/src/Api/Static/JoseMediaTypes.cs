using System;
namespace token;

/// <summary>
/// Provides registered JOSE, JWK, and JWT application media types.
/// </summary>
public static class JoseMediaTypes
{
    /// <summary>The compact JOSE media type.</summary>
    public const string Jose = "application/jose";

    /// <summary>The JOSE JSON serialization media type.</summary>
    public const string JoseJson = "application/jose+json";

    /// <summary>The JSON Web Key media type.</summary>
    public const string JwkJson = "application/jwk+json";

    /// <summary>The JSON Web Key Set media type.</summary>
    public const string JwkSetJson = "application/jwk-set+json";

    /// <summary>The JSON Web Token media type.</summary>
    public const string Jwt = "application/jwt";

    /// <summary>The OAuth access token JWT profile media type.</summary>
    public const string AccessTokenJwt = "application/at+jwt";

    /// <summary>The DPoP proof JWT media type.</summary>
    public const string DpopJwt = "application/dpop+jwt";

    /// <summary>The OAuth JWT-secured authorization request media type.</summary>
    public const string AuthorizationRequestJwt = "application/oauth-authz-req+jwt";

    /// <summary>The security event JWT media type.</summary>
    public const string SecurityEventJwt = "application/secevent+jwt";

    /// <summary>The SD-JWT media type.</summary>
    public const string SdJwt = "application/sd-jwt";

    /// <summary>The SD-JWT JSON serialization media type.</summary>
    public const string SdJwtJson = "application/sd-jwt+json";

    /// <summary>The key binding JWT media type.</summary>
    public const string KeyBindingJwt = "application/kb+jwt";
}
