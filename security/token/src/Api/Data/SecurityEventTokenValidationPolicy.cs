using System;
namespace token;

/// <summary>
/// Configures RFC 8417 Security Event Token validation.
/// </summary>
public class SecurityEventTokenValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, issuer, audience, time, and replay settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Indicates whether the recommended SET audience claim is required.</summary>
    public bool RequireAudience;

    /// <summary>Indicates whether the protected header must explicitly identify a SET.</summary>
    public bool RequireExplicitType;

    /// <summary>Indicates whether a JWS signature is required in addition to any encryption.</summary>
    public bool RequireSignature;

    /// <summary>Contains event identifier URIs accepted by this recipient, or an empty array for any URI.</summary>
    public string[] ValidEventTypes;

    /// <summary>An optional callback that validates each authenticated event identifier and payload.</summary>
    public Func<string, JsonObject, TokenResult>? EventValidator;

    /// <summary>Initializes a SET policy with audience and signature requirements enabled.</summary>
    public SecurityEventTokenValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        RequireAudience = true;
        RequireExplicitType = false;
        RequireSignature = true;
        ValidEventTypes = Array.Empty<string>();
        EventValidator = null;
    }

    /// <summary>Checks the SET policy for missing or invalid configuration.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_security_event_policy",
                "The Security Event Token policy is missing its JWT policy.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if ((JwtPolicy.ValidIssuers ?? throw new InvalidOperationException("A required value is null.")).Length == 0
            || RequireAudience && (JwtPolicy.ValidAudiences ?? throw new InvalidOperationException("A required value is null.")).Length == 0
            || ValidEventTypes == null
            || !ValidEventTypeList(ValidEventTypes))
        {
            return Failure(
                "invalid_security_event_policy",
                "SET validation requires a valid issuer, audience, and event-type configuration.");
        }

        return Success();
    }

    internal static bool ValidEventTypeList(string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (!ValidEventType(value))
            {
                return false;
            }

            int previous = 0;
            while (previous < index)
            {
                if (string.Equals(
                    values[previous],
                    value,
                    StringComparison.Ordinal))
                {
                    return false;
                }

                previous += 1;
            }

            index += 1;
        }

        return true;
    }

    internal static bool ValidEventType(string value)
    {
        return value != null
            && value.Length > 0
            && Utf8Codec.Encode(value).Success
            && Uri.IsWellFormedUriString(value, UriKind.Absolute);
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
