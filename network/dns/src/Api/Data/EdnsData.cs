using System.Collections.Generic;

namespace dns;

/// <summary>Holds the header fields and options carried by an EDNS OPT pseudo-record.</summary>
public class DnsEdnsData
{
    /// <summary>The requestor UDP payload size.</summary>
    public int UdpPayloadSize = DnsProtocolLimits.DefaultEdnsUdpPayloadSize;

    /// <summary>The high eight bits of the extended DNS response code.</summary>
    public int ExtendedResponseCode;

    /// <summary>The EDNS protocol version.</summary>
    public int Version;

    /// <summary>The recognized EDNS flag bits.</summary>
    public DnsEdnsFlag Flags;

    /// <summary>The complete raw 16-bit EDNS flags field.</summary>
    public int FlagsValue;

    /// <summary>The ordered EDNS options.</summary>
    public List<DnsEdnsOption> Options = new List<DnsEdnsOption>();
}

/// <summary>Holds one typed or opaque EDNS option.</summary>
public class DnsEdnsOption
{
    /// <summary>The recognized EDNS option code.</summary>
    public DnsEdnsOptionCode Code;

    /// <summary>The raw EDNS option code, including unrecognized values.</summary>
    public int CodeValue;

    /// <summary>The original or caller-supplied opaque option bytes.</summary>
    public byte[] RawData = [];

    /// <summary>Requests encoding of opaque bytes instead of typed option data.</summary>
    public bool UseRawData;

    /// <summary>Indicates that an Update Lease value is present.</summary>
    public bool HasUpdateLease;

    /// <summary>The requested or granted Update Lease duration in seconds.</summary>
    public uint UpdateLeaseSeconds;

    /// <summary>Indicates that a distinct KEY-record Update Lease value is present.</summary>
    public bool HasUpdateKeyLease;

    /// <summary>The requested or granted KEY-record lease duration in seconds.</summary>
    public uint UpdateKeyLeaseSeconds;

    /// <summary>The Name Server Identifier bytes.</summary>
    public byte[] NameServerIdentifier = [];

    /// <summary>The raw DNSSEC Algorithm Understood values.</summary>
    public List<int> DnssecAlgorithmValues = new List<int>();

    /// <summary>The raw DS Hash Understood values.</summary>
    public List<int> DigestAlgorithmValues = new List<int>();

    /// <summary>The raw NSEC3 Hash Understood values.</summary>
    public List<int> Nsec3HashAlgorithmValues = new List<int>();

    /// <summary>The optional Client Subnet values.</summary>
    public DnsEdnsClientSubnet? ClientSubnet;

    /// <summary>Indicates that an Expire duration is present.</summary>
    public bool HasExpire;

    /// <summary>The remaining zone expiry duration in seconds.</summary>
    public uint ExpireSeconds;

    /// <summary>The optional DNS Cookie values.</summary>
    public DnsEdnsCookie? Cookie;

    /// <summary>The optional TCP Keepalive values.</summary>
    public DnsEdnsKeepalive? Keepalive;

    /// <summary>The EDNS padding octets.</summary>
    public byte[] Padding = [];

    /// <summary>The optional CHAIN closest-trust-point name.</summary>
    public DnsName? ChainName;

    /// <summary>The Key Tag option values.</summary>
    public List<int> KeyTagValues = new List<int>();

    /// <summary>The optional Extended DNS Error values.</summary>
    public DnsEdnsExtendedError? ExtendedError;

    /// <summary>The optional Report Channel agent domain.</summary>
    public DnsName? ReportChannel;

    /// <summary>The optional Zone Version values.</summary>
    public DnsEdnsZoneVersion? ZoneVersion;
}

/// <summary>Holds EDNS Client Subnet address and prefix values.</summary>
public class DnsEdnsClientSubnet
{
    /// <summary>The address-family number.</summary>
    public int AddressFamily;

    /// <summary>The number of significant source-address bits.</summary>
    public int SourcePrefixLength;

    /// <summary>The response scope prefix length.</summary>
    public int ScopePrefixLength;

    /// <summary>The minimally encoded, masked address octets.</summary>
    public byte[] Address = [];
}

/// <summary>Holds the client and optional server portions of a DNS Cookie.</summary>
public class DnsEdnsCookie
{
    /// <summary>The eight-octet client cookie.</summary>
    public byte[] ClientCookie = [];

    /// <summary>The optional server-cookie bytes.</summary>
    public byte[] ServerCookie = [];
}

/// <summary>Holds the optional timeout carried by EDNS TCP Keepalive.</summary>
public class DnsEdnsKeepalive
{
    /// <summary>Indicates that a response timeout is present.</summary>
    public bool HasTimeout;

    /// <summary>The timeout in units of 100 milliseconds.</summary>
    public int TimeoutUnits;
}

/// <summary>Holds an Extended DNS Error information code and optional text.</summary>
public class DnsEdnsExtendedError
{
    /// <summary>The recognized Extended DNS Error code.</summary>
    public DnsExtendedErrorCode Code;

    /// <summary>The raw Extended DNS Error code, including unrecognized values.</summary>
    public int CodeValue;

    /// <summary>The optional human-readable UTF-8 extra text.</summary>
    public string? ExtraText;
}

/// <summary>Holds response data from an EDNS Zone Version option.</summary>
public class DnsEdnsZoneVersion
{
    /// <summary>Indicates that response version data is present.</summary>
    public bool HasVersion;

    /// <summary>The number of trailing query-name labels that identify the zone.</summary>
    public int LabelCount;

    /// <summary>The zone-version type number.</summary>
    public int Type;

    /// <summary>The opaque zone-version bytes.</summary>
    public byte[] Version = [];
}

/// <summary>Holds the exception-less result of decoding or encoding one EDNS option.</summary>
public class DnsEdnsOptionResult
{
    /// <summary>Indicates that the EDNS option operation succeeded.</summary>
    public bool Success;

    /// <summary>The number of option bytes consumed or written.</summary>
    public int BytesProcessed;

    /// <summary>The optional decoded or encoded option.</summary>
    public DnsEdnsOption? Option;

    /// <summary>The encoded option bytes when encoding succeeds.</summary>
    public byte[] Bytes = [];

    /// <summary>Diagnostics produced while processing the option.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}
