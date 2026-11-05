using System;
namespace token;

/// <summary>
/// Configures RFC 9101 JWT-secured authorization request validation.
/// </summary>
public class JwtSecuredAuthorizationRequestValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, lifetime, and callback settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the client identifier from the outer authorization request.</summary>
    public string? ClientId;

    /// <summary>Contains the expected authorization server issuer identifier.</summary>
    public string? AuthorizationServerIssuer;

    /// <summary>Indicates whether the protected header must explicitly identify a Request Object.</summary>
    public bool RequireExplicitType;

    /// <summary>Indicates whether the recommended issuer and audience claims are required.</summary>
    public bool RequireIssuerAndAudience;

    /// <summary>Initializes a Request Object policy with secure profile defaults.</summary>
    public JwtSecuredAuthorizationRequestValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        ClientId = null;
        AuthorizationServerIssuer = null;
        RequireExplicitType = false;
        RequireIssuerAndAudience = true;
    }

    /// <summary>Checks the Request Object policy for missing or invalid context.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_authorization_request_policy",
                "The JWT-secured authorization request policy is missing its JWT policy.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (!ValidText(ClientId) || !ValidText(AuthorizationServerIssuer))
        {
            return Failure(
                "invalid_authorization_request_policy",
                "A JWT-secured authorization request requires a client and authorization server identifier.");
        }

        return Success();
    }

    internal static bool ValidText(string? value)
    {
        return value != null
            && value.Length > 0
            && Utf8Codec.Encode(value).Success;
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
