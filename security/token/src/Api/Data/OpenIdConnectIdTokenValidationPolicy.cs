using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Configures OpenID Connect ID Token profile validation.
/// </summary>
public class OpenIdConnectIdTokenValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, issuer, time, and callback settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the relying party client identifier expected in <c>aud</c> and <c>azp</c>.</summary>
    public string? ClientId;

    /// <summary>Contains the nonce sent in the authentication request, or <c>null</c> when none was sent.</summary>
    public string? Nonce;

    /// <summary>Contains the maximum authentication age in seconds, or zero when <c>auth_time</c> is not required.</summary>
    public long MaximumAuthenticationAgeSeconds;

    /// <summary>Contains an access token whose <c>at_hash</c> must be validated, or <c>null</c>.</summary>
    public string? AccessToken;

    /// <summary>Contains an authorization code whose <c>c_hash</c> must be validated, or <c>null</c>.</summary>
    public string? AuthorizationCode;

    /// <summary>Initializes an empty OpenID Connect ID Token validation policy.</summary>
    public OpenIdConnectIdTokenValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        ClientId = null;
        Nonce = null;
        MaximumAuthenticationAgeSeconds = 0;
        AccessToken = null;
        AuthorizationCode = null;
    }

    /// <summary>Checks the policy for missing or invalid OpenID Connect settings.</summary>
    /// <returns>A successful result when the policy can be used, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_openid_id_token_policy",
                "The OpenID Connect ID Token JWT policy is missing.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (!ValidText(ClientId)
            || (JwtPolicy.ValidIssuers ?? throw new InvalidOperationException("A required value is null.")).Length == 0
            || MaximumAuthenticationAgeSeconds < 0
            || Nonce != null && !ValidText(Nonce)
            || AccessToken != null && !ValidAscii(AccessToken)
            || AuthorizationCode != null && !ValidAscii(AuthorizationCode))
        {
            return Failure(
                "invalid_openid_id_token_policy",
                "The OpenID Connect ID Token policy contains an invalid value.");
        }

        return Success();
    }

    internal static bool ValidText(string? value)
    {
        return value != null
            && value.Length > 0
            && Utf8Codec.Encode(value).Success;
    }

    internal static bool ValidAscii(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
