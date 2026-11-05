using System.Collections.Generic;

namespace dns;

/// <summary>Controls bounded DNSSEC validation and signing policy.</summary>
public class DnsSecurityPolicy
{
    /// <summary>The raw DNSSEC algorithm numbers with explicit local policies.</summary>
    public List<int> AlgorithmValues = new List<int>();

    /// <summary>The local policies corresponding to the DNSSEC algorithm numbers.</summary>
    public List<DnsAlgorithmPolicy> AlgorithmPolicies = new List<DnsAlgorithmPolicy>();

    /// <summary>The raw DS digest algorithm numbers with explicit local policies.</summary>
    public List<int> DigestAlgorithmValues = new List<int>();

    /// <summary>The local policies corresponding to the DS digest algorithm numbers.</summary>
    public List<DnsAlgorithmPolicy> DigestAlgorithmPolicies = new List<DnsAlgorithmPolicy>();

    /// <summary>The configured positive trust anchors.</summary>
    public List<DnsTrustAnchor> TrustAnchors = new List<DnsTrustAnchor>();

    /// <summary>The policy applied to an unlisted DNSSEC algorithm.</summary>
    public DnsAlgorithmPolicy DefaultAlgorithmPolicy;

    /// <summary>The policy applied to an unlisted DS digest algorithm.</summary>
    public DnsAlgorithmPolicy DefaultDigestAlgorithmPolicy;

    /// <summary>Indicates that an explicit DNS serial validation time is supplied.</summary>
    public bool HasValidationTime;

    /// <summary>The validation time as an unsigned DNS serial timestamp.</summary>
    public uint ValidationTime;

    /// <summary>The accepted clock variance for signatures in seconds.</summary>
    public int ClockSkewSeconds = 300;

    /// <summary>The largest number of signature validations in one operation.</summary>
    public int MaximumSignatureValidations = 64;

    /// <summary>The largest accepted NSEC3 iteration count.</summary>
    public int MaximumNsec3Iterations = 100;

    /// <summary>The largest number of denial proof records processed.</summary>
    public int MaximumProofRecords = 64;

    /// <summary>Validates signatures and chains of trust.</summary>
    public bool ValidateSignatures = true;

    /// <summary>Validates authenticated denial through NSEC or NSEC3.</summary>
    public bool ValidateDenial = true;

    /// <summary>Treats an unsupported required algorithm as bogus instead of indeterminate.</summary>
    public bool TreatUnsupportedAsBogus;
}

/// <summary>Holds one configured DS or DNSKEY trust anchor.</summary>
public class DnsTrustAnchor
{
    /// <summary>The absolute name at which the anchor applies.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The optional DNSKEY anchor data.</summary>
    public DnsKeyData? Key;

    /// <summary>The optional DS anchor data.</summary>
    public DnsDsData? DelegationSigner;

    /// <summary>The optional source description for the anchor.</summary>
    public string? Source;

    /// <summary>Indicates that the anchor can be selected.</summary>
    public bool Enabled = true;

    /// <summary>Indicates that the anchor key has been revoked.</summary>
    public bool Revoked;

    /// <summary>Indicates that a first accepted DNS serial time is supplied.</summary>
    public bool HasValidFrom;

    /// <summary>The first accepted DNS serial time.</summary>
    public uint ValidFrom;

    /// <summary>Indicates that a last accepted DNS serial time is supplied.</summary>
    public bool HasValidUntil;

    /// <summary>The last accepted DNS serial time.</summary>
    public uint ValidUntil;

    /// <summary>The time at which the anchor was loaded.</summary>
    public DnsClockValue LoadedAt = new DnsClockValue();
}

/// <summary>Holds caller-supplied public and optional private DNSSEC key material.</summary>
public class DnsKeyMaterial
{
    /// <summary>The owner name of the DNSKEY.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized DNS class of the key.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw DNS class integer, including unrecognized values.</summary>
    public int ClassValue = 1;

    /// <summary>The public DNSKEY data.</summary>
    public DnsKeyData Key = new DnsKeyData();

    /// <summary>The calculated DNSKEY key tag.</summary>
    public int KeyTag;

    /// <summary>Indicates that caller-owned private key bytes are supplied.</summary>
    public bool HasPrivateKey;

    /// <summary>The caller-owned private key bytes.</summary>
    public byte[] PrivateKey = [];

    /// <summary>The optional format name of the private key bytes.</summary>
    public string? PrivateKeyFormat;

    /// <summary>Indicates that the key is selected for signing DNSKEY record sets.</summary>
    public bool KeySigningKey;

    /// <summary>Indicates that the key is selected for signing other zone record sets.</summary>
    public bool ZoneSigningKey;

    /// <summary>Indicates that a signing inception time is supplied.</summary>
    public bool HasInception;

    /// <summary>The signing inception time as an unsigned DNS serial timestamp.</summary>
    public uint Inception;

    /// <summary>Indicates that a signing expiration time is supplied.</summary>
    public bool HasExpiration;

    /// <summary>The signing expiration time as an unsigned DNS serial timestamp.</summary>
    public uint Expiration;
}

/// <summary>Holds an RRset or zone DNSSEC signing request.</summary>
public class DnsSignatureRequest
{
    /// <summary>The optional complete zone to sign.</summary>
    public DnsZone? Zone;

    /// <summary>The individual record sets to sign.</summary>
    public List<DnsResourceRecordSet> RecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The caller-supplied signing keys.</summary>
    public List<DnsKeyMaterial> Keys = new List<DnsKeyMaterial>();

    /// <summary>The signer name written into generated signatures.</summary>
    public DnsName SignerName = new DnsName();

    /// <summary>The DNSSEC algorithm and resource limits.</summary>
    public DnsSecurityPolicy Policy = new DnsSecurityPolicy();

    /// <summary>Indicates that an original TTL override is supplied.</summary>
    public bool HasOriginalTimeToLive;

    /// <summary>The original TTL written into generated signatures.</summary>
    public uint OriginalTimeToLive;

    /// <summary>Indicates that a common signature inception time is supplied.</summary>
    public bool HasInception;

    /// <summary>The common signature inception as an unsigned DNS serial timestamp.</summary>
    public uint Inception;

    /// <summary>Indicates that a common signature expiration time is supplied.</summary>
    public bool HasExpiration;

    /// <summary>The common signature expiration as an unsigned DNS serial timestamp.</summary>
    public uint Expiration;

    /// <summary>Generates authenticated denial records for a complete zone.</summary>
    public bool GenerateDenialRecords;

    /// <summary>Uses NSEC3 instead of NSEC for generated denial records.</summary>
    public bool UseNsec3;

    /// <summary>The optional NSEC3 parameters used for denial record generation.</summary>
    public DnsNsec3ParamData? Nsec3Parameters;
}

/// <summary>Holds a DNSSEC validation or signing outcome.</summary>
public class DnsSecurityResult
{
    /// <summary>Indicates that the requested security operation completed successfully.</summary>
    public bool Success;

    /// <summary>The resulting DNSSEC security state.</summary>
    public DnsSecurityStatus Status;

    /// <summary>The optional zone to which the security result applies.</summary>
    public DnsName? Zone;

    /// <summary>The optional message containing validated or generated security data.</summary>
    public DnsMessage? Message;

    /// <summary>The record sets successfully covered by the operation.</summary>
    public List<DnsResourceRecordSet> ValidatedRecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The NSEC or NSEC3 proof records used by validation.</summary>
    public List<DnsResourceRecord> ProofRecords = new List<DnsResourceRecord>();

    /// <summary>The RRSIG or denial records generated by signing.</summary>
    public List<DnsResourceRecord> GeneratedRecords = new List<DnsResourceRecord>();

    /// <summary>The trust anchors used to establish the result.</summary>
    public List<DnsTrustAnchor> TrustAnchors = new List<DnsTrustAnchor>();

    /// <summary>The recognized algorithm most directly associated with the result.</summary>
    public DnsDnssecAlgorithm Algorithm;

    /// <summary>The raw algorithm number, including unrecognized values.</summary>
    public int AlgorithmValue;

    /// <summary>Indicates that a signing key tag is available.</summary>
    public bool HasKeyTag;

    /// <summary>The signing key tag.</summary>
    public int KeyTag;

    /// <summary>Indicates that an Extended DNS Error value is available.</summary>
    public bool HasExtendedError;

    /// <summary>The recognized Extended DNS Error code.</summary>
    public DnsExtendedErrorCode ExtendedError;

    /// <summary>The raw Extended DNS Error code, including unrecognized values.</summary>
    public int ExtendedErrorValue;

    /// <summary>Diagnostics produced by the security operation.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds a caller-supplied TSIG shared secret and algorithm policy.</summary>
public class DnsTsigKey
{
    /// <summary>The canonical TSIG key name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The canonical TSIG algorithm name.</summary>
    public DnsName AlgorithmName = new DnsName();

    /// <summary>The caller-owned shared secret bytes.</summary>
    public byte[] Secret = [];

    /// <summary>The requested MAC length, where zero selects the full algorithm output.</summary>
    public int MacLength;

    /// <summary>The permitted signing-time variance in seconds.</summary>
    public int Fudge = 300;

    /// <summary>Indicates that a first accepted signing time is supplied.</summary>
    public bool HasValidFrom;

    /// <summary>The first accepted unsigned 48-bit signing time.</summary>
    public ulong ValidFrom;

    /// <summary>Indicates that a last accepted signing time is supplied.</summary>
    public bool HasValidUntil;

    /// <summary>The last accepted unsigned 48-bit signing time.</summary>
    public ulong ValidUntil;

    /// <summary>Indicates that the key can be selected.</summary>
    public bool Enabled = true;
}

/// <summary>Holds a TSIG signing or verification outcome and MAC-chain state.</summary>
public class DnsTsigResult
{
    /// <summary>Indicates that the TSIG operation completed successfully.</summary>
    public bool Success;

    /// <summary>Indicates that the message authentication code was verified.</summary>
    public bool Authenticated;

    /// <summary>The recognized TSIG error.</summary>
    public DnsTsigError Error;

    /// <summary>The raw TSIG error integer, including unrecognized values.</summary>
    public int ErrorValue;

    /// <summary>The optional signed or verified DNS message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional TSIG resource-record data.</summary>
    public DnsTsigData? Tsig;

    /// <summary>The canonical name of the key used by the operation.</summary>
    public DnsName? KeyName;

    /// <summary>The message authentication code produced or verified.</summary>
    public byte[] Mac = [];

    /// <summary>The preceding message authentication code used for chaining.</summary>
    public byte[] PriorMac = [];

    /// <summary>Indicates that the peer signing time is available.</summary>
    public bool HasTimeSigned;

    /// <summary>The peer unsigned 48-bit signing time.</summary>
    public ulong TimeSigned;

    /// <summary>Indicates that a server time from a BADTIME response is available.</summary>
    public bool HasServerTime;

    /// <summary>The unsigned 48-bit server time from a BADTIME response.</summary>
    public ulong ServerTime;

    /// <summary>Diagnostics produced by TSIG processing.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds a ZONEMD calculation or verification outcome.</summary>
public class DnsZoneDigestResult
{
    /// <summary>Indicates that the zone-digest operation completed successfully.</summary>
    public bool Success;

    /// <summary>Indicates that a calculated digest matched the expected digest.</summary>
    public bool Verified;

    /// <summary>The zone SOA serial included in the digest record.</summary>
    public uint Serial;

    /// <summary>The ZONEMD digest scheme number.</summary>
    public int Scheme;

    /// <summary>The ZONEMD hash algorithm number.</summary>
    public int HashAlgorithm;

    /// <summary>The calculated zone digest bytes.</summary>
    public byte[] Digest = [];

    /// <summary>The optional calculated or verified ZONEMD data.</summary>
    public DnsZonemdData? RecordData;

    /// <summary>The number of canonical resource records included in the digest.</summary>
    public int IncludedRecordCount;

    /// <summary>The number of ZONEMD or otherwise excluded records.</summary>
    public int ExcludedRecordCount;

    /// <summary>Diagnostics produced by zone-digest processing.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}
