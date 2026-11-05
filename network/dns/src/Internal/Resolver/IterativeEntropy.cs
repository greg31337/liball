namespace dns;

internal interface IDnsIterativeEntropy
{
    ushort NextIdentifier();

    int NextCaseBit();
}

internal class DnsIterativeEntropy : IDnsIterativeEntropy
{
    internal DnsIterativeEntropy()
    {
    }

    public ushort NextIdentifier()
    {
        return TransportEntropy.NextIdentifier();
    }

    public int NextCaseBit()
    {
        return TransportEntropy.NextInt32(2);
    }
}
