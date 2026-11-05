using System;
namespace token;

/// <summary>
/// Provides registered JOSE, JWK, and JWT application media types and type helpers.
/// </summary>
internal static class JoseMediaTypesOperations
{
    /// <summary>The compact JOSE media type.</summary>
    internal const string Jose = JoseMediaTypes.Jose;

    /// <summary>The JOSE JSON serialization media type.</summary>
    internal const string JoseJson = JoseMediaTypes.JoseJson;

    /// <summary>The JSON Web Key media type.</summary>
    internal const string JwkJson = JoseMediaTypes.JwkJson;

    /// <summary>The JSON Web Key Set media type.</summary>
    internal const string JwkSetJson = JoseMediaTypes.JwkSetJson;

    /// <summary>The JSON Web Token media type.</summary>
    internal const string Jwt = JoseMediaTypes.Jwt;

    /// <summary>The OAuth access token JWT profile media type.</summary>
    internal const string AccessTokenJwt = JoseMediaTypes.AccessTokenJwt;

    /// <summary>The DPoP proof JWT media type.</summary>
    internal const string DpopJwt = JoseMediaTypes.DpopJwt;

    /// <summary>The OAuth JWT-secured authorization request media type.</summary>
    internal const string AuthorizationRequestJwt = JoseMediaTypes.AuthorizationRequestJwt;

    /// <summary>The security event JWT media type.</summary>
    internal const string SecurityEventJwt = JoseMediaTypes.SecurityEventJwt;

    /// <summary>The SD-JWT media type.</summary>
    internal const string SdJwt = JoseMediaTypes.SdJwt;

    /// <summary>The SD-JWT JSON serialization media type.</summary>
    internal const string SdJwtJson = JoseMediaTypes.SdJwtJson;

    /// <summary>The key binding JWT media type.</summary>
    internal const string KeyBindingJwt = JoseMediaTypes.KeyBindingJwt;

    static readonly string[] all = new string[]
    {
        AccessTokenJwt,
        "application/client-authentication+jwt",
        DpopJwt,
        "application/eat+jwt",
        "application/entity-statement+jwt",
        "application/explicit-registration-response+jwt",
        Jose,
        JoseJson,
        JwkJson,
        JwkSetJson,
        "application/jwk-set+jwt",
        Jwt,
        KeyBindingJwt,
        "application/logout+jwt",
        AuthorizationRequestJwt,
        "application/provided-claims+jwt",
        "application/resolve-response+jwt",
        SdJwt,
        SdJwtJson,
        SecurityEventJwt,
        "application/statuslist+jwt",
        "application/token-introspection+jwt",
        "application/trust-mark+jwt",
        "application/trust-mark-delegation+jwt",
        "application/trust-mark-status-response+jwt",
        "application/vc+jwt",
        "application/vc+sd-jwt",
        "application/vp+jwt",
        "application/vp+sd-jwt"
    };

    /// <summary>
    /// Determines whether a media type is registered for JOSE, JWK, or a JWT profile.
    /// </summary>
    /// <param name="mediaType">The media type, compared case-insensitively.</param>
    /// <returns><c>true</c> when the media type is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(string mediaType)
    {
        return RegistryLookup.Contains(all, mediaType, true);
    }

    /// <summary>
    /// Normalizes a full application media type or abbreviated <c>typ</c>/<c>cty</c> value.
    /// </summary>
    /// <param name="value">A full media type or an application subtype such as <c>JWT</c>.</param>
    /// <returns>The canonical registered media type, or <c>null</c> when the value is unknown.</returns>
    internal static string? Normalize(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string candidate = value.IndexOf('/') < 0 ? "application/" + value : value;
        int index = 0;
        while (index < all.Length)
        {
            if (string.Equals(all[index], candidate, StringComparison.OrdinalIgnoreCase))
            {
                return all[index];
            }

            index += 1;
        }

        return null;
    }

    /// <summary>
    /// Returns all registered JOSE, JWK, and JWT application media types known by this library.
    /// </summary>
    /// <returns>A new array containing the media types.</returns>
    internal static string[] GetAll()
    {
        return RegistryLookup.Copy(all);
    }
}
