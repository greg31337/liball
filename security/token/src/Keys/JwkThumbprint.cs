using System;
using System.Security.Cryptography;

namespace token;

internal static class JwkThumbprint
{
    const string Sha256UriPrefix = "urn:ietf:params:oauth:jwk-thumbprint:sha-256:";

    internal static TokenBytesResult Calculate(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenTextResult canonical = JwkWriter.Write(
            key,
            JwkWriteProjection.Thumbprint);
        if (!canonical.Success)
        {
            return Failure(canonical.Error);
        }

        TokenBytesResult utf8 = Utf8Codec.Encode((canonical.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!utf8.Success)
        {
            return Failure(utf8.Error);
        }

        byte[] digest = SHA256.HashData((utf8.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        CryptographicOperations.ZeroMemory(utf8.Bytes);
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = digest;
        return result;
    }

    internal static TokenTextResult CalculateUri(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenBytesResult thumbprint = Calculate(key);
        if (!thumbprint.Success)
        {
            return TextFailure(thumbprint.Error);
        }

        TokenTextResult encoded = Base64Url.Encode((thumbprint.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        CryptographicOperations.ZeroMemory(thumbprint.Bytes);
        if (!encoded.Success)
        {
            return TextFailure(encoded.Error);
        }

        TokenTextResult result = new TokenTextResult();
        result.Success = true;
        result.Text = Sha256UriPrefix + encoded.Text;
        return result;
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }
}
