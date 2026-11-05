using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Configures RFC 9449 DPoP proof validation for one HTTP request.
/// </summary>
public class DpopProofValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic controls, limits, clock, and callbacks.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the case-sensitive HTTP method of the current request.</summary>
    public string? HttpMethod;

    /// <summary>Contains the absolute HTTPS URI of the current request.</summary>
    public string? HttpUri;

    /// <summary>Contains the server-provided nonce that the proof must carry, or <c>null</c>.</summary>
    public string? Nonce;

    /// <summary>Contains the presented access token whose <c>ath</c> claim must match, or <c>null</c>.</summary>
    public string? AccessToken;

    /// <summary>Contains the expected base64url JWK SHA-256 thumbprint, or <c>null</c>.</summary>
    public string? ExpectedJwkThumbprint;

    /// <summary>Contains the maximum accepted proof age in seconds.</summary>
    public long MaximumProofAgeSeconds;

    /// <summary>Contains the maximum accepted number of characters in the <c>jti</c> claim.</summary>
    public int MaximumJwtIdLength;

    /// <summary>Initializes a DPoP policy with a five-minute proof lifetime.</summary>
    public DpopProofValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        HttpMethod = null;
        HttpUri = null;
        Nonce = null;
        AccessToken = null;
        ExpectedJwkThumbprint = null;
        MaximumProofAgeSeconds = 300;
        MaximumJwtIdLength = 128;
    }

    /// <summary>Checks the DPoP request context and cryptographic policy.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_dpop_policy",
                "The DPoP JWT validation policy is missing.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (!ValidHttpMethod(HttpMethod)
            || !ValidRequestUri(HttpUri)
            || MaximumProofAgeSeconds <= 0
            || MaximumProofAgeSeconds > 86400
            || MaximumJwtIdLength <= 0
            || Nonce != null && !ValidNonce(Nonce)
            || AccessToken != null && !ValidAscii(AccessToken)
            || AccessToken != null && ExpectedJwkThumbprint == null
            || ExpectedJwkThumbprint != null
                && !ValidThumbprint(ExpectedJwkThumbprint))
        {
            return Failure(
                "invalid_dpop_policy",
                "The DPoP validation policy contains an invalid request context or limit.");
        }

        return Success();
    }

    internal static bool ValidRequestUri(string? value)
    {
        if (value == null
            || !Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return false;
        }

        Uri uri = new Uri(value, UriKind.Absolute);
        return string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ValidHttpMethod(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool letterOrDigit = character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
            bool punctuation = character == '!'
                || character == '#'
                || character == '$'
                || character == '%'
                || character == '&'
                || character == '\''
                || character == '*'
                || character == '+'
                || character == '-'
                || character == '.'
                || character == '^'
                || character == '_'
                || character == '`'
                || character == '|'
                || character == '~';
            if (!letterOrDigit && !punctuation)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool ValidNonce(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 0x21
                || character == 0x22
                || character == 0x5c
                || character > 0x7e)
            {
                return false;
            }

            index += 1;
        }

        return true;
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
            if (value[index] > 0x7f)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool ValidThumbprint(string value)
    {
        TokenBytesResult decoded = Base64Url.Decode(value);
        return decoded.Success && (decoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 32;
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
