namespace sip;

internal class SipNonceCodec
{
    const int AuthenticationByteCount = 32;
    const int NonceByteCount = 57;
    const int NonceCharacterCount = 76;
    const int OpaqueByteCount = 24;
    const int PayloadByteCount = 25;
    const int RandomByteCount = 16;
    const byte Version = 1;
    ISipRandomSource randomSource;
    ISipUnixTimeSource timeSource;

    internal SipNonceCodec(ISipRandomSource randomSource,
        ISipUnixTimeSource timeSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        if (timeSource == null)
        {
            throw new System.ArgumentNullException("timeSource");
        }

        this.randomSource = randomSource;
        this.timeSource = timeSource;
    }

    internal string CreateNonce(SipNonceOptions? options)
    {
        SipNonceOptions checkedOptions = ValidateOptions(options);
        long issuedAt = timeSource.GetUnixTimeSeconds();
        if (issuedAt < 0)
        {
            throw new System.InvalidOperationException(
                "The nonce time source returned a negative Unix timestamp.");
        }

        byte[] randomBytes = new byte[RandomByteCount];
        randomSource.FillBytes(randomBytes);
        byte[] payload = new byte[PayloadByteCount];
        payload[0] = Version;
        WriteUInt64(payload, 1, (ulong)issuedAt);
        System.Array.Copy(randomBytes, 0, payload, 9, randomBytes.Length);

        string realm = checkedOptions.Realm == null ? "" : checkedOptions.Realm;
        byte[] authenticated = CreateAuthenticatedBytes(payload, realm);
        byte[] key = CopyBytes(checkedOptions.CurrentKey);
        byte[] authentication = System.Security.Cryptography.HMACSHA256.HashData(
            key, authenticated);
        byte[] nonceBytes = new byte[NonceByteCount];
        System.Array.Copy(payload, nonceBytes, payload.Length);
        System.Array.Copy(authentication, 0, nonceBytes, payload.Length,
            authentication.Length);
        string nonce = EncodeBase64Url(nonceBytes);

        System.Security.Cryptography.CryptographicOperations.ZeroMemory(randomBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(payload);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(authenticated);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(authentication);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(nonceBytes);
        return nonce;
    }

    internal string CreateOpaqueData()
    {
        byte[] bytes = new byte[OpaqueByteCount];
        randomSource.FillBytes(bytes);
        string opaque = EncodeBase64Url(bytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        return opaque;
    }

    internal SipAuthenticationStatus ValidateNonce(string? nonce,
        SipNonceOptions? options)
    {
        SipNonceOptions checkedOptions = ValidateOptions(options);
        byte[]? decoded = DecodeNonce(nonce);
        if (decoded == null)
        {
            return SipAuthenticationStatus.Failed;
        }

        byte[] payload = new byte[PayloadByteCount];
        byte[] suppliedAuthentication = new byte[AuthenticationByteCount];
        System.Array.Copy(decoded, payload, payload.Length);
        System.Array.Copy(decoded, payload.Length, suppliedAuthentication, 0,
            suppliedAuthentication.Length);

        string realm = checkedOptions.Realm == null ? "" : checkedOptions.Realm;
        byte[] authenticated = CreateAuthenticatedBytes(payload, realm);
        byte[] currentKey = CopyBytes(checkedOptions.CurrentKey);
        byte[] previousKey = checkedOptions.PreviousKey.Length == 0
            ? CopyBytes(checkedOptions.CurrentKey)
            : CopyBytes(checkedOptions.PreviousKey);
        byte[] currentAuthentication =
            System.Security.Cryptography.HMACSHA256.HashData(currentKey, authenticated);
        byte[] previousAuthentication =
            System.Security.Cryptography.HMACSHA256.HashData(previousKey, authenticated);
        bool currentMatches = System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(currentAuthentication, suppliedAuthentication);
        bool previousMatches = System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(previousAuthentication, suppliedAuthentication);
        bool authentic = currentMatches | previousMatches;

        ulong unsignedIssuedAt = ReadUInt64(payload, 1);
        long now = timeSource.GetUnixTimeSeconds();
        SipAuthenticationStatus status = SipAuthenticationStatus.Failed;
        if (authentic && payload[0] == Version && unsignedIssuedAt <= long.MaxValue
            && now >= 0)
        {
            long issuedAt = (long)unsignedIssuedAt;
            long skew = checkedOptions.ClockSkewSeconds;
            long lifetime = checkedOptions.LifetimeSeconds;
            if (issuedAt > now && issuedAt - now > skew)
            {
                status = SipAuthenticationStatus.Failed;
            }
            else if (now > issuedAt && now - issuedAt > lifetime + skew)
            {
                status = SipAuthenticationStatus.StaleNonce;
            }
            else
            {
                status = SipAuthenticationStatus.Success;
            }
        }

        System.Security.Cryptography.CryptographicOperations.ZeroMemory(decoded);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(payload);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            suppliedAuthentication);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(authenticated);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(currentKey);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(previousKey);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            currentAuthentication);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            previousAuthentication);
        return status;
    }

    static SipNonceOptions ValidateOptions(SipNonceOptions? options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.Realm == null)
        {
            throw new System.ArgumentException(
                "A nonce realm is required.", "options");
        }
        if (options.CurrentKey == null || options.CurrentKey.Length < 32)
        {
            throw new System.ArgumentException(
                "The current nonce key must contain at least 32 bytes.", "options");
        }
        if (options.PreviousKey == null
            || (options.PreviousKey.Length != 0 && options.PreviousKey.Length < 32))
        {
            throw new System.ArgumentException(
                "The previous nonce key must be empty or contain at least 32 bytes.",
                "options");
        }
        if (options.LifetimeSeconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "options", "The nonce lifetime must be positive.");
        }
        if (options.ClockSkewSeconds < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "options", "The nonce clock skew cannot be negative.");
        }

        return options;
    }

    static byte[] CreateAuthenticatedBytes(byte[] payload, string realm)
    {
        byte[] realmBytes = System.Text.Encoding.UTF8.GetBytes(realm);
        byte[] result = new byte[payload.Length + realmBytes.Length];
        System.Array.Copy(payload, result, payload.Length);
        System.Array.Copy(realmBytes, 0, result, payload.Length, realmBytes.Length);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(realmBytes);
        return result;
    }

    static byte[]? DecodeNonce(string? nonce)
    {
        if (nonce == null || nonce.Length != NonceCharacterCount)
        {
            return null;
        }

        int index = 0;
        while (index < nonce.Length)
        {
            if (!IsBase64UrlCharacter(nonce[index]))
            {
                return null;
            }
            index = index + 1;
        }

        string base64 = nonce.Replace('-', '+').Replace('_', '/');
        byte[] decoded = System.Convert.FromBase64String(base64);
        if (decoded.Length != NonceByteCount)
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(decoded);
            return null;
        }
        return decoded;
    }

    static bool IsBase64UrlCharacter(char value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '0' && value <= '9'
            || value == '-' || value == '_';
    }

    static string EncodeBase64Url(byte[] bytes)
    {
        return System.Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static void WriteUInt64(byte[] destination, int offset, ulong value)
    {
        destination[offset] = (byte)((value >> 56) & 255);
        destination[offset + 1] = (byte)((value >> 48) & 255);
        destination[offset + 2] = (byte)((value >> 40) & 255);
        destination[offset + 3] = (byte)((value >> 32) & 255);
        destination[offset + 4] = (byte)((value >> 24) & 255);
        destination[offset + 5] = (byte)((value >> 16) & 255);
        destination[offset + 6] = (byte)((value >> 8) & 255);
        destination[offset + 7] = (byte)(value & 255);
    }

    static ulong ReadUInt64(byte[] source, int offset)
    {
        return (ulong)source[offset] << 56
            | (ulong)source[offset + 1] << 48
            | (ulong)source[offset + 2] << 40
            | (ulong)source[offset + 3] << 32
            | (ulong)source[offset + 4] << 24
            | (ulong)source[offset + 5] << 16
            | (ulong)source[offset + 6] << 8
            | source[offset + 7];
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }
}
