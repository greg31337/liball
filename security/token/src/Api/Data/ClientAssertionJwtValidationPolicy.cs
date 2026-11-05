using System;
namespace token;

/// <summary>
/// Configures OAuth client assertion JWT validation according to RFC 7523.
/// </summary>
public class ClientAssertionJwtValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, lifetime, and callback settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the OAuth client identifier expected in both <c>iss</c> and <c>sub</c>.</summary>
    public string? ClientId;

    /// <summary>Contains the authorization server identifier expected in <c>aud</c>.</summary>
    public string? AuthorizationServerAudience;

    /// <summary>Indicates whether the assertion must contain a nonempty <c>jti</c> claim.</summary>
    public bool RequireJwtId;

    /// <summary>Contains the maximum future expiration interval in seconds.</summary>
    public long MaximumAssertionLifetimeSeconds;

    /// <summary>Initializes a client assertion policy with a five-minute lifetime limit.</summary>
    public ClientAssertionJwtValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        ClientId = null;
        AuthorizationServerAudience = null;
        RequireJwtId = false;
        MaximumAssertionLifetimeSeconds = 300;
    }

    /// <summary>Checks the client assertion policy for missing or invalid context.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_client_assertion_policy",
                "The client assertion validation policy is missing its JWT policy.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (!ValidText(ClientId)
            || !ValidText(AuthorizationServerAudience)
            || MaximumAssertionLifetimeSeconds <= 0
            || MaximumAssertionLifetimeSeconds > 86400)
        {
            return Failure(
                "invalid_client_assertion_policy",
                "The client assertion validation policy contains invalid context or limits.");
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
