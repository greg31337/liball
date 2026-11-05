using System.Security.Cryptography;

namespace uuid;

internal interface IRandomSource
{
    void FillBytes(byte[] bytes);
}

internal class CryptographicRandomSource : IRandomSource
{
    public void FillBytes(byte[] bytes)
    {
        RandomNumberGenerator.Fill(bytes);
    }
}
