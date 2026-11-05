using System.Collections.Generic;

namespace dns;

/// <summary>Holds one SVCB or HTTPS service binding.</summary>
public class DnsSvcbData
{
    /// <summary>The service priority, where zero selects alias mode.</summary>
    public int Priority;
    /// <summary>The service target name.</summary>
    public DnsName TargetName = new DnsName();
    /// <summary>The ordered service parameters.</summary>
    public List<DnsSvcbParameter> Parameters = new List<DnsSvcbParameter>();
}

/// <summary>Holds one typed or unknown Service Binding parameter.</summary>
public class DnsSvcbParameter
{
    /// <summary>The recognized parameter key.</summary>
    public DnsSvcbParameterKey Key;
    /// <summary>The raw parameter key integer.</summary>
    public int KeyValue;
    /// <summary>The raw parameter value for lossless preservation.</summary>
    public byte[] RawValue = [];
    /// <summary>The mandatory parameter-key values.</summary>
    public List<int> MandatoryKeyValues = new List<int>();
    /// <summary>The ALPN identifier byte strings.</summary>
    public List<byte[]> AlpnIdentifiers = new List<byte[]>();
    /// <summary>Indicates the empty no-default-ALPN parameter.</summary>
    public bool NoDefaultAlpn;
    /// <summary>Indicates that a port value is present.</summary>
    public bool HasPort;
    /// <summary>The alternative service port.</summary>
    public int Port;
    /// <summary>The IPv4 address hints.</summary>
    public List<byte[]> Ipv4Hints = new List<byte[]>();
    /// <summary>The Encrypted ClientHello configuration bytes.</summary>
    public byte[] Ech = [];
    /// <summary>The IPv6 address hints.</summary>
    public List<byte[]> Ipv6Hints = new List<byte[]>();
    /// <summary>The optional DNS-over-HTTPS path template.</summary>
    public string? DohPath;
    /// <summary>The Oblivious HTTP configuration bytes.</summary>
    public byte[] ObliviousHttp = [];
    /// <summary>Requests encoding of the raw value instead of a typed value.</summary>
    public bool UseRawValue;
}

/// <summary>Holds DSYNC endpoint-discovery data.</summary>
public class DnsDsyncData
{
    /// <summary>The record type that triggers synchronization.</summary>
    public DnsRecordType RecordType;
    /// <summary>The raw synchronization record type.</summary>
    public int RecordTypeValue;
    /// <summary>The synchronization scheme number.</summary>
    public int Scheme;
    /// <summary>The synchronization endpoint port.</summary>
    public int Port;
    /// <summary>The synchronization endpoint target.</summary>
    public DnsName Target = new DnsName();
}

/// <summary>Holds an ILNP preference and node or locator value.</summary>
public class DnsIlNpData
{
    /// <summary>The ILNP preference.</summary>
    public int Preference;
    /// <summary>The network-order node or locator octets.</summary>
    public byte[] Value = [];
}

/// <summary>Holds an EUI-48 or EUI-64 address.</summary>
public class DnsEuiData
{
    /// <summary>The EUI address octets.</summary>
    public byte[] Address = [];
}

/// <summary>Holds URI resource-record data.</summary>
public class DnsUriData
{
    /// <summary>The URI priority.</summary>
    public int Priority;
    /// <summary>The URI weight.</summary>
    public int Weight;
    /// <summary>The URI target octets.</summary>
    public byte[] Target = [];
}

/// <summary>Holds CAA resource-record data.</summary>
public class DnsCaaData
{
    /// <summary>The CAA flags octet.</summary>
    public int Flags;
    /// <summary>The property tag octets.</summary>
    public byte[] Tag = [];
    /// <summary>The property value octets.</summary>
    public byte[] Value = [];
}

/// <summary>Holds AMTRELAY discovery and relay data.</summary>
public class DnsAmtRelayData
{
    /// <summary>The relay precedence.</summary>
    public int Precedence;
    /// <summary>Indicates that relay discovery is optional.</summary>
    public bool DiscoveryOptional;
    /// <summary>The relay type number.</summary>
    public int RelayType;
    /// <summary>The optional IPv4 or IPv6 relay address.</summary>
    public byte[] RelayAddress = [];
    /// <summary>The optional relay DNS name.</summary>
    public DnsName? RelayName;
}

/// <summary>Holds RESINFO key-value character strings.</summary>
public class DnsResolverInformationData
{
    /// <summary>The ordered key or key-value strings as exact octets.</summary>
    public List<byte[]> Values = new List<byte[]>();
}

/// <summary>Holds TKEY transaction-key negotiation data.</summary>
public class DnsTkeyData
{
    /// <summary>The key algorithm name.</summary>
    public DnsName AlgorithmName = new DnsName();
    /// <summary>The key inception time.</summary>
    public uint Inception;
    /// <summary>The key expiration time.</summary>
    public uint Expiration;
    /// <summary>The key negotiation mode.</summary>
    public int Mode;
    /// <summary>The key negotiation error code.</summary>
    public int Error;
    /// <summary>The key bytes.</summary>
    public byte[] Key = [];
    /// <summary>The other-data bytes.</summary>
    public byte[] OtherData = [];
}

/// <summary>Holds TSIG transaction-signature data.</summary>
public class DnsTsigData
{
    /// <summary>The message-authentication algorithm name.</summary>
    public DnsName AlgorithmName = new DnsName();
    /// <summary>The unsigned 48-bit signing time.</summary>
    public ulong TimeSigned;
    /// <summary>The permitted signing-time variance in seconds.</summary>
    public int Fudge;
    /// <summary>The message-authentication code bytes.</summary>
    public byte[] Mac = [];
    /// <summary>The original DNS message identifier.</summary>
    public int OriginalIdentifier;
    /// <summary>The recognized TSIG error.</summary>
    public DnsTsigError Error;
    /// <summary>The raw TSIG error integer.</summary>
    public int ErrorValue;
    /// <summary>The other-data bytes.</summary>
    public byte[] OtherData = [];
}
