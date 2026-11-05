namespace sip;

internal static class SipDigestHash
{
    internal static byte[] HashBytes(
        SipDigestAlgorithm algorithm, byte[]? value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        switch (algorithm)
        {
            case SipDigestAlgorithm.Md5:
            case SipDigestAlgorithm.Md5Sess:
                return System.Security.Cryptography.MD5.HashData(value);
            case SipDigestAlgorithm.Sha256:
            case SipDigestAlgorithm.Sha256Sess:
                return System.Security.Cryptography.SHA256.HashData(value);
            case SipDigestAlgorithm.Sha512256:
            case SipDigestAlgorithm.Sha512256Sess:
                return SipSha512256.Hash(value);
            default:
                throw new System.ArgumentOutOfRangeException("algorithm");
        }
    }

    internal static byte[] HashText(
        SipDigestAlgorithm algorithm, string? value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        byte[] result = HashBytes(algorithm, bytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    internal static string HashHex(
        SipDigestAlgorithm algorithm, byte[]? value)
    {
        byte[] hash = HashBytes(algorithm, value);
        string result = System.Convert.ToHexString(hash).ToLowerInvariant();
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(hash);
        return result;
    }

    internal static string HashTextHex(
        SipDigestAlgorithm algorithm, string? value)
    {
        byte[] hash = HashText(algorithm, value);
        string result = System.Convert.ToHexString(hash).ToLowerInvariant();
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(hash);
        return result;
    }

    internal static int HashByteCount(SipDigestAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case SipDigestAlgorithm.Md5:
            case SipDigestAlgorithm.Md5Sess:
                return 16;
            case SipDigestAlgorithm.Sha256:
            case SipDigestAlgorithm.Sha256Sess:
            case SipDigestAlgorithm.Sha512256:
            case SipDigestAlgorithm.Sha512256Sess:
                return 32;
            default:
                throw new System.ArgumentOutOfRangeException("algorithm");
        }
    }

    internal static bool UsesSessionKey(SipDigestAlgorithm algorithm)
    {
        if (algorithm == SipDigestAlgorithm.Md5Sess
            || algorithm == SipDigestAlgorithm.Sha256Sess
            || algorithm == SipDigestAlgorithm.Sha512256Sess)
        {
            return true;
        }
        if (algorithm == SipDigestAlgorithm.Md5
            || algorithm == SipDigestAlgorithm.Sha256
            || algorithm == SipDigestAlgorithm.Sha512256)
        {
            return false;
        }
        throw new System.ArgumentOutOfRangeException("algorithm");
    }

    internal static bool FixedTimeEqualsBytes(
        byte[]? expected, byte[]? supplied)
    {
        if (expected == null || supplied == null)
        {
            return false;
        }
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            expected, supplied);
    }

    internal static bool FixedTimeEqualsHex(
        SipDigestAlgorithm algorithm, string? expected, string? supplied)
    {
        int length = HashByteCount(algorithm) * 2;
        if (!IsLowerHex(expected, length) || !IsLowerHex(supplied, length))
        {
            return false;
        }
        string expectedText = expected == null ? "" : expected;
        string suppliedText = supplied == null ? "" : supplied;
        byte[] expectedBytes = System.Text.Encoding.ASCII.GetBytes(expectedText);
        byte[] suppliedBytes = System.Text.Encoding.ASCII.GetBytes(suppliedText);
        bool result = System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(expectedBytes, suppliedBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(expectedBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(suppliedBytes);
        return result;
    }

    static bool IsLowerHex(string? value, int length)
    {
        if (value == null || value.Length != length)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character < '0' || character > '9'
                && (character < 'a' || character > 'f'))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
