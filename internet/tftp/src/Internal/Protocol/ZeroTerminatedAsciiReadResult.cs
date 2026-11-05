namespace tftp;

internal class ZeroTerminatedAsciiReadResult
{
    internal string Value;
    internal int NextOffset;

    internal ZeroTerminatedAsciiReadResult(string value, int nextOffset)
    {
        Value = value;
        NextOffset = nextOffset;
    }
}
