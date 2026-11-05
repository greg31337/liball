using System.Collections.Generic;

namespace dns;

/// <summary>Holds the obsolete A6 IPv6-prefix schema.</summary>
public class DnsA6Data
{
    /// <summary>The number of leading address bits supplied by the prefix name.</summary>
    public int PrefixLength;
    /// <summary>The suffix address octets present after the prefix length.</summary>
    public byte[] AddressSuffix = [];
    /// <summary>The optional prefix DNS name.</summary>
    public DnsName? PrefixName;
}

/// <summary>Holds PX X.400 mapping data.</summary>
public class DnsPxData
{
    /// <summary>The mapping preference.</summary>
    public int Preference;
    /// <summary>The RFC 822 mapping name.</summary>
    public DnsName Map822 = new DnsName();
    /// <summary>The X.400 mapping name.</summary>
    public DnsName MapX400 = new DnsName();
}

/// <summary>Holds LOC version-zero wire values.</summary>
public class DnsLocData
{
    /// <summary>The LOC format version.</summary>
    public int Version;
    /// <summary>The encoded size value.</summary>
    public int Size;
    /// <summary>The encoded horizontal precision.</summary>
    public int HorizontalPrecision;
    /// <summary>The encoded vertical precision.</summary>
    public int VerticalPrecision;
    /// <summary>The encoded latitude.</summary>
    public uint Latitude;
    /// <summary>The encoded longitude.</summary>
    public uint Longitude;
    /// <summary>The encoded altitude.</summary>
    public uint Altitude;
}

/// <summary>Holds SRV service-location data.</summary>
public class DnsSrvData
{
    /// <summary>The service priority.</summary>
    public int Priority;
    /// <summary>The service weight.</summary>
    public int Weight;
    /// <summary>The service port.</summary>
    public int Port;
    /// <summary>The service target name.</summary>
    public DnsName Target = new DnsName();
}

/// <summary>Holds NAPTR rewriting data.</summary>
public class DnsNaptrData
{
    /// <summary>The NAPTR order.</summary>
    public int Order;
    /// <summary>The NAPTR preference.</summary>
    public int Preference;
    /// <summary>The flags character string.</summary>
    public byte[] Flags = [];
    /// <summary>The services character string.</summary>
    public byte[] Services = [];
    /// <summary>The regular-expression character string.</summary>
    public byte[] RegularExpression = [];
    /// <summary>The replacement DNS name.</summary>
    public DnsName Replacement = new DnsName();
}

/// <summary>Holds CERT resource-record data.</summary>
public class DnsCertData
{
    /// <summary>The certificate type code.</summary>
    public int CertificateType;
    /// <summary>The key tag.</summary>
    public int KeyTag;
    /// <summary>The recognized security algorithm.</summary>
    public DnsDnssecAlgorithm Algorithm;
    /// <summary>The raw security algorithm number.</summary>
    public int AlgorithmValue;
    /// <summary>The certificate bytes.</summary>
    public byte[] Certificate = [];
}

/// <summary>Holds one APL address-prefix item.</summary>
public class DnsAplItem
{
    /// <summary>The address-family number.</summary>
    public int AddressFamily;
    /// <summary>The prefix length in bits.</summary>
    public int PrefixLength;
    /// <summary>Indicates a negated prefix.</summary>
    public bool Negated;
    /// <summary>The minimally encoded address octets.</summary>
    public byte[] Address = [];
}

/// <summary>Holds an ordered address-prefix list.</summary>
public class DnsAplData
{
    /// <summary>The address-prefix items.</summary>
    public List<DnsAplItem> Items = new List<DnsAplItem>();
}
