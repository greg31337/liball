using System.Security.Cryptography;

namespace diameter;

internal interface IPeerWatchdogJitterSource
{
    int ReadPeerWatchdogJitterValue(int maximumExclusive);
}

internal class SecurePeerWatchdogJitterSource : IPeerWatchdogJitterSource
{
    public int ReadPeerWatchdogJitterValue(int maximumExclusive)
    {
        return RandomNumberGenerator.GetInt32(maximumExclusive);
    }
}
