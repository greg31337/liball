using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class JwtGenerator
{
    const string Algorithm = "none";
    const string Type = "JWT";
    const string Issuer = "https://example.com/issuer";
    const string Audience = "https://example.com/audience";

    internal static string AlgorithmLabel(int maximumOutputSize)
    {
        Guard.OutputSize(Algorithm.Length, maximumOutputSize, "jwtAlgorithm");
        return Algorithm;
    }

    internal static JwtRecord Generate(
        FakeRandom random,
        DateTimeOffset issuedAt,
        TimeSpan lifetime,
        int maximumOutputSize)
    {
        if (lifetime.Ticks < TimeSpan.TicksPerSecond)
        {
            throw new ArgumentOutOfRangeException(
                "lifetime",
                lifetime,
                "lifetime must be at least one whole second.");
        }

        long issuedSeconds = issuedAt.ToUnixTimeSeconds();
        long lifetimeSeconds = lifetime.Ticks / TimeSpan.TicksPerSecond;
        long expirySeconds = issuedSeconds + lifetimeSeconds;
        string subject = InternetIdentityGenerator.Username(random, maximumOutputSize);
        string jwtId = "fixture-" +
            random.ULong().ToString("x16", CultureInfo.InvariantCulture) +
            random.ULong().ToString("x16", CultureInfo.InvariantCulture);
        string headerJson = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
        string payloadJson = "{\"iss\":\"" + Issuer +
            "\",\"sub\":\"" + subject +
            "\",\"aud\":\"" + Audience +
            "\",\"iat\":" + issuedSeconds.ToString(CultureInfo.InvariantCulture) +
            ",\"nbf\":" + issuedSeconds.ToString(CultureInfo.InvariantCulture) +
            ",\"exp\":" + expirySeconds.ToString(CultureInfo.InvariantCulture) +
            ",\"jti\":\"" + jwtId +
            "\",\"synthetic\":true}";
        string headerSegment = Base64Url(headerJson);
        string payloadSegment = Base64Url(payloadJson);
        string token = headerSegment + "." + payloadSegment + ".";

        Guard.OutputSize(headerJson.Length, maximumOutputSize, "jwt");
        Guard.OutputSize(payloadJson.Length, maximumOutputSize, "jwt");
        Guard.OutputSize(jwtId.Length, maximumOutputSize, "jwt");
        Guard.OutputSize(token.Length, maximumOutputSize, "jwt");

        JwtRecord result = new JwtRecord();
        result.Token = token;
        result.HeaderJson = headerJson;
        result.PayloadJson = payloadJson;
        result.Algorithm = Algorithm;
        result.Type = Type;
        result.Issuer = Issuer;
        result.Subject = subject;
        result.Audience = Audience;
        result.IssuedAtUnixSeconds = issuedSeconds;
        result.NotBeforeUnixSeconds = issuedSeconds;
        result.ExpiresAtUnixSeconds = expirySeconds;
        result.JwtId = jwtId;
        result.IsSigned = false;
        result.IsAuthenticated = false;
        result.IsSynthetic = true;
        return result;
    }

    static string Base64Url(string value)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        int length = encoded.Length;
        while (length > 0 && encoded[length - 1] == '=')
        {
            length = length - 1;
        }
        if (length != encoded.Length)
        {
            encoded = encoded.Substring(0, length);
        }
        return encoded.Replace('+', '-').Replace('/', '_');
    }
}
