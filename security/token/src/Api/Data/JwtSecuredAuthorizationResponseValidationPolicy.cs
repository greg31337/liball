using System;
namespace token;

/// <summary>
/// Configures JWT-secured OAuth authorization response validation.
/// </summary>
public class JwtSecuredAuthorizationResponseValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, lifetime, and callback settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the expected authorization server issuer.</summary>
    public string? Issuer;

    /// <summary>Contains the client identifier expected in the response audience.</summary>
    public string? ClientId;

    /// <summary>Contains the state sent in the request, or <c>null</c> when none was sent.</summary>
    public string? State;

    /// <summary>Contains the maximum future expiration interval in seconds.</summary>
    public long MaximumResponseLifetimeSeconds;

    /// <summary>Initializes a JARM policy with the recommended ten-minute lifetime limit.</summary>
    public JwtSecuredAuthorizationResponseValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        Issuer = null;
        ClientId = null;
        State = null;
        MaximumResponseLifetimeSeconds = 600;
    }

    /// <summary>Checks the JARM policy for missing or invalid context.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_authorization_response_policy",
                "The JWT-secured authorization response policy is missing its JWT policy.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (!ValidText(Issuer)
            || !ValidText(ClientId)
            || State != null && !ValidText(State)
            || MaximumResponseLifetimeSeconds <= 0
            || MaximumResponseLifetimeSeconds > 86400)
        {
            return Failure(
                "invalid_authorization_response_policy",
                "The JWT-secured authorization response policy contains invalid context or limits.");
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
