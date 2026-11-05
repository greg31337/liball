namespace dns;

internal interface IDnsLookupEntropy
{
    int NextIndex(int maximumExclusive);
}

internal class DnsLookupEntropy : IDnsLookupEntropy
{
    internal DnsLookupEntropy()
    {
    }

    public int NextIndex(int maximumExclusive)
    {
        return TransportEntropy.NextInt32(maximumExclusive);
    }
}
