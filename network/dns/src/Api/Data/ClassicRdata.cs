namespace dns;

/// <summary>Holds SOA resource-record data.</summary>
public class DnsSoaData
{
    /// <summary>The primary authoritative name server.</summary>
    public DnsName PrimaryNameServer = new DnsName();
    /// <summary>The responsible mailbox name.</summary>
    public DnsName ResponsibleMailbox = new DnsName();
    /// <summary>The zone serial number.</summary>
    public uint Serial;
    /// <summary>The refresh interval in seconds.</summary>
    public uint Refresh;
    /// <summary>The retry interval in seconds.</summary>
    public uint Retry;
    /// <summary>The expiry interval in seconds.</summary>
    public uint Expire;
    /// <summary>The SOA minimum value in seconds.</summary>
    public uint Minimum;
}

/// <summary>Holds WKS resource-record data.</summary>
public class DnsWksData
{
    /// <summary>The four-octet IPv4 address.</summary>
    public byte[] Address = [];
    /// <summary>The IP protocol number.</summary>
    public int Protocol;
    /// <summary>The service-port bitmap.</summary>
    public byte[] PortBitmap = [];
}
