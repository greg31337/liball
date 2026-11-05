using System.Collections.Generic;

namespace dns;

/// <summary>Holds all supported alternatives for one typed resource-record data value.</summary>
public class DnsRecordData
{
    /// <summary>The optional address schema.</summary>
    public DnsAddressData? Address;
    /// <summary>The optional single-name schema.</summary>
    public DnsNameData? Name;
    /// <summary>The optional two-name schema.</summary>
    public DnsTwoNameData? TwoName;
    /// <summary>The optional preference-and-name schema.</summary>
    public DnsPreferenceNameData? PreferenceName;
    /// <summary>The optional character-string sequence schema.</summary>
    public DnsCharacterStringsData? CharacterStrings;
    /// <summary>The optional opaque schema.</summary>
    public DnsOpaqueData? Opaque;
    /// <summary>The optional fixed-byte schema.</summary>
    public DnsFixedBytesData? FixedBytes;
    /// <summary>The optional SOA schema.</summary>
    public DnsSoaData? Soa;
    /// <summary>The optional WKS schema.</summary>
    public DnsWksData? Wks;
    /// <summary>The optional A6 schema.</summary>
    public DnsA6Data? A6;
    /// <summary>The optional PX schema.</summary>
    public DnsPxData? Px;
    /// <summary>The optional LOC schema.</summary>
    public DnsLocData? Loc;
    /// <summary>The optional SRV schema.</summary>
    public DnsSrvData? Srv;
    /// <summary>The optional NAPTR schema.</summary>
    public DnsNaptrData? Naptr;
    /// <summary>The optional CERT schema.</summary>
    public DnsCertData? Cert;
    /// <summary>The optional APL schema.</summary>
    public DnsAplData? Apl;
    /// <summary>The optional signature schema.</summary>
    public DnsSignatureData? Signature;
    /// <summary>The optional key schema.</summary>
    public DnsKeyData? Key;
    /// <summary>The optional delegation-signer schema.</summary>
    public DnsDsData? Ds;
    /// <summary>The optional SSH fingerprint schema.</summary>
    public DnsSshfpData? Sshfp;
    /// <summary>The optional IPSECKEY schema.</summary>
    public DnsIpsecKeyData? IpsecKey;
    /// <summary>The optional NSEC or NXT schema.</summary>
    public DnsNsecData? Nsec;
    /// <summary>The optional NSEC3 schema.</summary>
    public DnsNsec3Data? Nsec3;
    /// <summary>The optional NSEC3PARAM schema.</summary>
    public DnsNsec3ParamData? Nsec3Param;
    /// <summary>The optional DHCID schema.</summary>
    public DnsDhcidData? Dhcid;
    /// <summary>The optional TLSA-compatible schema.</summary>
    public DnsTlsaData? Tlsa;
    /// <summary>The optional HIP schema.</summary>
    public DnsHipData? Hip;
    /// <summary>The optional CSYNC schema.</summary>
    public DnsCsyncData? Csync;
    /// <summary>The optional ZONEMD schema.</summary>
    public DnsZonemdData? Zonemd;
    /// <summary>The optional SVCB-compatible schema.</summary>
    public DnsSvcbData? Svcb;
    /// <summary>The optional DSYNC schema.</summary>
    public DnsDsyncData? Dsync;
    /// <summary>The optional ILNP schema.</summary>
    public DnsIlNpData? IlNp;
    /// <summary>The optional EUI schema.</summary>
    public DnsEuiData? Eui;
    /// <summary>The optional URI schema.</summary>
    public DnsUriData? Uri;
    /// <summary>The optional CAA schema.</summary>
    public DnsCaaData? Caa;
    /// <summary>The optional AMTRELAY schema.</summary>
    public DnsAmtRelayData? AmtRelay;
    /// <summary>The optional RESINFO schema.</summary>
    public DnsResolverInformationData? ResolverInformation;
    /// <summary>The optional TKEY schema.</summary>
    public DnsTkeyData? Tkey;
    /// <summary>The optional TSIG schema.</summary>
    public DnsTsigData? Tsig;
    /// <summary>The optional EDNS OPT pseudo-record schema.</summary>
    public DnsEdnsData? Edns;
}

/// <summary>Holds an IPv4 or IPv6 address as exact network-order octets.</summary>
public class DnsAddressData
{
    /// <summary>The network-order address octets.</summary>
    public byte[] Address = [];
}

/// <summary>Holds resource-record data containing one DNS name.</summary>
public class DnsNameData
{
    /// <summary>The DNS name value.</summary>
    public DnsName Name = new DnsName();
}

/// <summary>Holds resource-record data containing two DNS names.</summary>
public class DnsTwoNameData
{
    /// <summary>The first DNS name.</summary>
    public DnsName FirstName = new DnsName();
    /// <summary>The second DNS name.</summary>
    public DnsName SecondName = new DnsName();
}

/// <summary>Holds a 16-bit preference followed by a DNS name.</summary>
public class DnsPreferenceNameData
{
    /// <summary>The unsigned 16-bit preference value.</summary>
    public int Preference;
    /// <summary>The preferred DNS name.</summary>
    public DnsName Name = new DnsName();
}

/// <summary>Holds an ordered sequence of length-prefixed character strings.</summary>
public class DnsCharacterStringsData
{
    /// <summary>The character strings as exact octets without length prefixes.</summary>
    public List<byte[]> Strings = new List<byte[]>();
}

/// <summary>Holds uninterpreted resource-record data for lossless round trips.</summary>
public class DnsOpaqueData
{
    /// <summary>The uninterpreted RDATA octets.</summary>
    public byte[] Bytes = [];
}

/// <summary>Holds a schema-defined fixed-width binary value.</summary>
public class DnsFixedBytesData
{
    /// <summary>The fixed-width value octets.</summary>
    public byte[] Bytes = [];
}
