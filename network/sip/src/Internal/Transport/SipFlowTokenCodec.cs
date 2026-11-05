namespace sip;

internal class SipFlowTokenCodec
{
    const int AuthenticationBytes = 16;
    byte[] secret;

    internal SipFlowTokenCodec(byte[] key)
    {
        if (key == null || key.Length < 20)
        {
            throw new System.ArgumentException(
                "The flow-token key must contain at least 20 bytes.", "key");
        }
        secret = CopyBytes(key);
    }

    internal string CreateToken(SipConnectionInfo connection,
        string instanceId, int registrationId)
    {
        byte[] payload = CreatePayload(connection, instanceId, registrationId);
        byte[] digest = System.Security.Cryptography.HMACSHA256.HashData(
            secret, payload);
        byte[] token = new byte[AuthenticationBytes + payload.Length];
        System.Array.Copy(digest, token, AuthenticationBytes);
        System.Array.Copy(payload, 0, token, AuthenticationBytes, payload.Length);
        string encoded = System.Convert.ToBase64String(token)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(digest);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(token);
        return encoded;
    }

    internal bool MatchesToken(string? token, SipConnectionInfo connection,
        string instanceId, int registrationId)
    {
        if (token == null || token.Length == 0)
        {
            return false;
        }
        string expected = CreateToken(connection, instanceId, registrationId);
        if (expected.Length != token.Length)
        {
            return false;
        }
        byte[] expectedBytes = System.Text.Encoding.ASCII.GetBytes(expected);
        byte[] suppliedBytes = System.Text.Encoding.ASCII.GetBytes(token);
        bool result = System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(expectedBytes, suppliedBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(expectedBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(suppliedBytes);
        return result;
    }

    byte[] CreatePayload(SipConnectionInfo connection, string instanceId,
        int registrationId)
    {
        SipEndpointAddress? local = connection.LocalAddress;
        SipEndpointAddress? remote = connection.RemoteAddress;
        string value = "1\n" + instanceId + "\n" + registrationId + "\n"
            + connection.Id + "\n" + (int)connection.Transport + "\n"
            + EndpointText(local) + "\n" + EndpointText(remote);
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string EndpointText(SipEndpointAddress? endpoint)
    {
        return endpoint == null ? ""
            : endpoint.Host + ":" + endpoint.Port + ":"
                + (int)endpoint.Transport + ":" + endpoint.Secure;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }
}
