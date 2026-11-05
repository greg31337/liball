using System.Security.Cryptography;

namespace diameter;

internal interface IPeerRetryJitterSource
{
    int ReadPeerRetryJitterValue(int maximumExclusive);
}

internal class SecurePeerRetryJitterSource : IPeerRetryJitterSource
{
    public int ReadPeerRetryJitterValue(int maximumExclusive)
    {
        return RandomNumberGenerator.GetInt32(maximumExclusive);
    }
}
