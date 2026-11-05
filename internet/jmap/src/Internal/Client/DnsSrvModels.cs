using System.Collections.Generic;

namespace jmap;

internal class DnsSrvRecord
{
    internal int Priority;
    internal int Weight;
    internal int Port;
    internal string? Target;
}

internal class DnsSrvResponse
{
    internal int ResponseCode;
    internal List<DnsSrvRecord> Records = new List<DnsSrvRecord>();
}

internal class DnsNameResult
{
    internal string? Name;
    internal int NextOffset;
}
