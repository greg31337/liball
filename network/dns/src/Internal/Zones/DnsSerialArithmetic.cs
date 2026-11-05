namespace dns;

internal class DnsSerialComparison
{
    internal bool Defined;
    internal int Comparison;
}

internal static class DnsSerialArithmetic
{
    internal static DnsSerialComparison CompareSerials(
        uint candidate,
        uint reference)
    {
        DnsSerialComparison result = new DnsSerialComparison();
        if (candidate == reference)
        {
            result.Defined = true;
            return result;
        }

        ulong distance;
        if (candidate > reference)
            distance = candidate - reference;
        else
            distance = 4294967296UL - reference + candidate;

        if (distance == 2147483648UL)
            return result;

        result.Defined = true;
        if (distance < 2147483648UL)
            result.Comparison = 1;
        else
            result.Comparison = -1;
        return result;
    }
}
