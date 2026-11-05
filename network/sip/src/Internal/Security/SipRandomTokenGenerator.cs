namespace sip;

internal class SipRandomTokenGenerator
{
    const int MaximumTokenBytes = 1024;
    ISipRandomSource randomSource;

    internal SipRandomTokenGenerator(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }

        this.randomSource = randomSource;
    }

    internal string CreateHexToken(int byteCount)
    {
        byte[] bytes = CreateRandomBytes(byteCount);
        string token = System.Convert.ToHexString(bytes).ToLowerInvariant();
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        return token;
    }

    internal string CreateBase64UrlToken(int byteCount)
    {
        byte[] bytes = CreateRandomBytes(byteCount);
        string token = System.Convert.ToBase64String(bytes);
        token = token.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        return token;
    }

    byte[] CreateRandomBytes(int byteCount)
    {
        if (byteCount <= 0 || byteCount > MaximumTokenBytes)
        {
            throw new System.ArgumentOutOfRangeException("byteCount");
        }

        byte[] bytes = new byte[byteCount];
        randomSource.FillBytes(bytes);
        return bytes;
    }
}
