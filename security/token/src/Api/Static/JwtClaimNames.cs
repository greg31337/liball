using System;
namespace token;

/// <summary>
/// Provides registered JWT claim names.
/// </summary>
public static class JwtClaimNames
{
    /// <summary>The issuer claim.</summary>
    public const string Issuer = "iss";

    /// <summary>The subject claim.</summary>
    public const string Subject = "sub";

    /// <summary>The audience claim.</summary>
    public const string Audience = "aud";

    /// <summary>The expiration time claim.</summary>
    public const string ExpirationTime = "exp";

    /// <summary>The not-before claim.</summary>
    public const string NotBefore = "nbf";

    /// <summary>The issued-at claim.</summary>
    public const string IssuedAt = "iat";

    /// <summary>The JWT identifier claim.</summary>
    public const string JwtId = "jti";

    /// <summary>The authorized party claim.</summary>
    public const string AuthorizedParty = "azp";

    /// <summary>The nonce claim.</summary>
    public const string Nonce = "nonce";

    /// <summary>The authentication time claim.</summary>
    public const string AuthenticationTime = "auth_time";

    /// <summary>The access token hash claim.</summary>
    public const string AccessTokenHash = "at_hash";

    /// <summary>The authorization code hash claim.</summary>
    public const string CodeHash = "c_hash";

    /// <summary>The authentication context class reference claim.</summary>
    public const string AuthenticationContextClassReference = "acr";

    /// <summary>The authentication methods references claim.</summary>
    public const string AuthenticationMethodsReferences = "amr";

    /// <summary>The confirmation claim.</summary>
    public const string Confirmation = "cnf";

    /// <summary>The OAuth scope claim.</summary>
    public const string Scope = "scope";

    /// <summary>The OAuth client identifier claim.</summary>
    public const string ClientId = "client_id";

    /// <summary>The DPoP HTTP method claim.</summary>
    public const string HttpMethod = "htm";

    /// <summary>The DPoP HTTP target URI claim.</summary>
    public const string HttpUri = "htu";

    /// <summary>The DPoP access token hash claim.</summary>
    public const string DpopAccessTokenHash = "ath";

    /// <summary>The Security Event Token events claim.</summary>
    public const string SecurityEvents = "events";

    /// <summary>The Security Event Token time-of-event claim.</summary>
    public const string TimeOfEvent = "toe";

    /// <summary>The Security Event Token transaction identifier claim.</summary>
    public const string TransactionId = "txn";

    /// <summary>The SD-JWT array of object-property Disclosure digests.</summary>
    public const string SelectiveDisclosureDigests = "_sd";

    /// <summary>The SD-JWT array-element Disclosure digest placeholder.</summary>
    public const string SelectiveDisclosureArrayElement = "...";

    /// <summary>The SD-JWT Disclosure hash algorithm claim.</summary>
    public const string SelectiveDisclosureAlgorithm = "_sd_alg";

    /// <summary>The SD-JWT Key Binding JWT hash claim.</summary>
    public const string SelectiveDisclosureHash = "sd_hash";
}
