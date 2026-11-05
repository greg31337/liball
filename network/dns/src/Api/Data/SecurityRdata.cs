using System.Collections.Generic;

namespace dns;

/// <summary>Holds SIG or RRSIG resource-record data.</summary>
public class DnsSignatureData
{
    /// <summary>The recognized covered record type.</summary>
    public DnsRecordType TypeCovered;
    /// <summary>The raw covered record type.</summary>
    public int TypeCoveredValue;
    /// <summary>The recognized signing algorithm.</summary>
    public DnsDnssecAlgorithm Algorithm;
    /// <summary>The raw signing algorithm number.</summary>
    public int AlgorithmValue;
    /// <summary>The owner-name label count.</summary>
    public int Labels;
    /// <summary>The original record-set TTL.</summary>
    public uint OriginalTimeToLive;
    /// <summary>The signature expiration time as DNS serial time.</summary>
    public uint SignatureExpiration;
    /// <summary>The signature inception time as DNS serial time.</summary>
    public uint SignatureInception;
    /// <summary>The signing key tag.</summary>
    public int KeyTag;
    /// <summary>The signer name.</summary>
    public DnsName SignerName = new DnsName();
    /// <summary>The signature bytes.</summary>
    public byte[] Signature = [];
}

/// <summary>Holds KEY, DNSKEY, or CDNSKEY resource-record data.</summary>
public class DnsKeyData
{
    /// <summary>The key flags.</summary>
    public int Flags;
    /// <summary>The key protocol value.</summary>
    public int Protocol;
    /// <summary>The recognized key algorithm.</summary>
    public DnsDnssecAlgorithm Algorithm;
    /// <summary>The raw key algorithm number.</summary>
    public int AlgorithmValue;
    /// <summary>The public-key bytes.</summary>
    public byte[] PublicKey = [];
}

/// <summary>Holds DS-compatible delegation digest data.</summary>
public class DnsDsData
{
    /// <summary>The referenced key tag.</summary>
    public int KeyTag;
    /// <summary>The recognized key algorithm.</summary>
    public DnsDnssecAlgorithm Algorithm;
    /// <summary>The raw key algorithm number.</summary>
    public int AlgorithmValue;
    /// <summary>The recognized digest algorithm.</summary>
    public DnsDigestAlgorithm DigestAlgorithm;
    /// <summary>The raw digest algorithm number.</summary>
    public int DigestAlgorithmValue;
    /// <summary>The digest bytes.</summary>
    public byte[] Digest = [];
}

/// <summary>Holds SSHFP resource-record data.</summary>
public class DnsSshfpData
{
    /// <summary>The SSH public-key algorithm number.</summary>
    public int Algorithm;
    /// <summary>The fingerprint type number.</summary>
    public int FingerprintType;
    /// <summary>The fingerprint bytes.</summary>
    public byte[] Fingerprint = [];
}

/// <summary>Holds IPSECKEY resource-record data.</summary>
public class DnsIpsecKeyData
{
    /// <summary>The IPsec key precedence.</summary>
    public int Precedence;
    /// <summary>The gateway type number.</summary>
    public int GatewayType;
    /// <summary>The public-key algorithm number.</summary>
    public int Algorithm;
    /// <summary>The optional IPv4 or IPv6 gateway octets.</summary>
    public byte[] GatewayAddress = [];
    /// <summary>The optional gateway DNS name.</summary>
    public DnsName? GatewayName;
    /// <summary>The public-key bytes.</summary>
    public byte[] PublicKey = [];
}

/// <summary>Holds NXT or NSEC next-name and type data.</summary>
public class DnsNsecData
{
    /// <summary>The next owner name.</summary>
    public DnsName NextDomainName = new DnsName();
    /// <summary>The raw record types present at the owner name.</summary>
    public List<int> RecordTypeValues = new List<int>();
}

/// <summary>Holds NSEC3 hashed denial data.</summary>
public class DnsNsec3Data
{
    /// <summary>The recognized hash algorithm.</summary>
    public DnsNsec3HashAlgorithm HashAlgorithm;
    /// <summary>The raw hash algorithm number.</summary>
    public int HashAlgorithmValue;
    /// <summary>The NSEC3 flags.</summary>
    public int Flags;
    /// <summary>The additional hash iteration count.</summary>
    public int Iterations;
    /// <summary>The hash salt.</summary>
    public byte[] Salt = [];
    /// <summary>The next hashed owner name.</summary>
    public byte[] NextHashedOwnerName = [];
    /// <summary>The raw record types present at the original owner name.</summary>
    public List<int> RecordTypeValues = new List<int>();
}

/// <summary>Holds NSEC3PARAM hashing parameters.</summary>
public class DnsNsec3ParamData
{
    /// <summary>The recognized hash algorithm.</summary>
    public DnsNsec3HashAlgorithm HashAlgorithm;
    /// <summary>The raw hash algorithm number.</summary>
    public int HashAlgorithmValue;
    /// <summary>The parameter flags.</summary>
    public int Flags;
    /// <summary>The additional hash iteration count.</summary>
    public int Iterations;
    /// <summary>The hash salt.</summary>
    public byte[] Salt = [];
}

/// <summary>Holds DHCID identifier and digest data.</summary>
public class DnsDhcidData
{
    /// <summary>The identifier type code.</summary>
    public int IdentifierType;
    /// <summary>The digest type code.</summary>
    public int DigestType;
    /// <summary>The digest bytes.</summary>
    public byte[] Digest = [];
}

/// <summary>Holds TLSA or SMIMEA certificate-association data.</summary>
public class DnsTlsaData
{
    /// <summary>The certificate usage code.</summary>
    public int CertificateUsage;
    /// <summary>The selector code.</summary>
    public int Selector;
    /// <summary>The matching type code.</summary>
    public int MatchingType;
    /// <summary>The certificate-association bytes.</summary>
    public byte[] AssociationData = [];
}

/// <summary>Holds HIP resource-record data.</summary>
public class DnsHipData
{
    /// <summary>The public-key algorithm number.</summary>
    public int PublicKeyAlgorithm;
    /// <summary>The host identity tag.</summary>
    public byte[] HostIdentityTag = [];
    /// <summary>The public-key bytes.</summary>
    public byte[] PublicKey = [];
    /// <summary>The rendezvous-server names.</summary>
    public List<DnsName> RendezvousServers = new List<DnsName>();
}

/// <summary>Holds CSYNC child-to-parent synchronization data.</summary>
public class DnsCsyncData
{
    /// <summary>The child SOA serial number.</summary>
    public uint Serial;
    /// <summary>The CSYNC flags.</summary>
    public int Flags;
    /// <summary>The raw synchronized record types.</summary>
    public List<int> RecordTypeValues = new List<int>();
}

/// <summary>Holds ZONEMD zone-digest data.</summary>
public class DnsZonemdData
{
    /// <summary>The zone serial number.</summary>
    public uint Serial;
    /// <summary>The digest scheme number.</summary>
    public int Scheme;
    /// <summary>The hash algorithm number.</summary>
    public int HashAlgorithm;
    /// <summary>The zone digest bytes.</summary>
    public byte[] Digest = [];
}
