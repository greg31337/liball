using System.Collections.Generic;

namespace dns;

/// <summary>Describes one configured recursive DNS server.</summary>
public class DnsResolverServer
{
    /// <summary>The network endpoint of the server.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The transport used to contact the server.</summary>
    public DnsTransportKind Transport;

    /// <summary>The optional certificate authentication name.</summary>
    public string? AuthenticationName;

    /// <summary>Indicates that the server can be selected.</summary>
    public bool Enabled = true;

    /// <summary>The server preference, where lower values are preferred.</summary>
    public int Priority;
}

/// <summary>Holds system-style stub resolver configuration.</summary>
public class DnsResolverConfiguration
{
    /// <summary>The ordered recursive DNS servers.</summary>
    public List<DnsResolverServer> Servers = new List<DnsResolverServer>();

    /// <summary>The ordered suffixes used for relative-name search.</summary>
    public List<DnsName> SearchDomains = new List<DnsName>();

    /// <summary>The number of dots that causes an initial absolute-name query.</summary>
    public int NameDots = 1;

    /// <summary>The maximum number of attempts for each configured server.</summary>
    public int Attempts = 2;

    /// <summary>The timeout for one attempt in milliseconds.</summary>
    public int TimeoutMilliseconds = 5000;

    /// <summary>Rotates the first selected server between resolutions.</summary>
    public bool RotateServers;

    /// <summary>Uses TCP without first attempting UDP.</summary>
    public bool UseTcp;

    /// <summary>Accepts the authenticated-data flag from a trusted recursive server.</summary>
    public bool TrustAuthenticData;

    /// <summary>Uses EDNS when querying a recursive server.</summary>
    public bool UseEdns = true;
}

/// <summary>Defines a local resolution policy for one DNS suffix.</summary>
public class DnsSpecialUsePolicy
{
    /// <summary>The absolute DNS suffix governed by this policy.</summary>
    public DnsName Suffix = new DnsName();

    /// <summary>Applies the policy to names below the configured suffix.</summary>
    public bool ApplyToSubdomains = true;

    /// <summary>The terminal outcome, or success when local record templates are supplied.</summary>
    public DnsResolveStatus Status = DnsResolveStatus.NameError;

    /// <summary>The local record-set templates used for a successful policy result.</summary>
    public List<DnsResourceRecordSet> RecordSets =
        new List<DnsResourceRecordSet>();

    /// <summary>The security status assigned to the local policy result.</summary>
    public DnsSecurityStatus SecurityStatus = DnsSecurityStatus.Insecure;
}

/// <summary>Controls one stub resolution operation.</summary>
public class DnsResolverOptions
{
    /// <summary>The server and search configuration.</summary>
    public DnsResolverConfiguration Configuration = new DnsResolverConfiguration();

    /// <summary>Allows answers to be read from and stored in the resolver cache.</summary>
    public bool UseCache = true;

    /// <summary>Allows expired cache entries within their stale lifetime.</summary>
    public bool AllowStale;

    /// <summary>Allows refresh of cache entries approaching expiration.</summary>
    public bool Prefetch;

    /// <summary>Follows CNAME and DNAME aliases.</summary>
    public bool FollowAliases = true;

    /// <summary>Requests DNSSEC records from recursive servers.</summary>
    public bool RequestDnssec;

    /// <summary>Disables upstream DNSSEC validation.</summary>
    public bool CheckingDisabled;

    /// <summary>Rejects unicast resolution of the special-use onion suffix.</summary>
    public bool RejectOnionNames = true;

    /// <summary>Handles localhost names without sending them to an upstream server.</summary>
    public bool HandleLocalhostNames = true;

    /// <summary>Ordered caller-supplied suffix policies evaluated before built-in policies.</summary>
    public List<DnsSpecialUsePolicy> SpecialUsePolicies =
        new List<DnsSpecialUsePolicy>();

    /// <summary>The maximum number of alias links followed.</summary>
    public int MaximumAliasDepth = 16;

    /// <summary>The total resolution timeout in milliseconds.</summary>
    public int TotalTimeoutMilliseconds = 5000;

    /// <summary>The maximum period after expiry that stale data can be used, in seconds.</summary>
    public int ServeStaleWindowSeconds;

    /// <summary>The remaining TTL percentage at which a cache entry can be prefetched.</summary>
    public int PrefetchThresholdPercent = 10;
}

/// <summary>Holds one request to resolve a DNS name and record type.</summary>
public class DnsResolveRequest
{
    /// <summary>The name to resolve.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized requested record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw requested record-type integer, including unrecognized values.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized requested class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw requested class integer, including unrecognized values.</summary>
    public int ClassValue = 1;

    /// <summary>The resolver configuration and operation policy.</summary>
    public DnsResolverOptions Options = new DnsResolverOptions();

    /// <summary>The optional EDNS Client Subnet value.</summary>
    public DnsEdnsClientSubnet? ClientSubnet;

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Describes one CNAME or DNAME step in an alias chain.</summary>
public class DnsAliasStep
{
    /// <summary>The owner name at this step.</summary>
    public DnsName Owner = new DnsName();

    /// <summary>The target name produced by this step.</summary>
    public DnsName Target = new DnsName();

    /// <summary>The recognized alias record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw alias record-type integer.</summary>
    public int RecordTypeValue;

    /// <summary>Indicates that a DNAME record caused a synthesized CNAME step.</summary>
    public bool Synthesized;

    /// <summary>The optional source resource record.</summary>
    public DnsResourceRecord? Record;
}

/// <summary>Holds a complete stub or iterative resolution outcome.</summary>
public class DnsResolveResult
{
    /// <summary>The resolution outcome.</summary>
    public DnsResolveStatus Status;

    /// <summary>The final canonical name after following aliases.</summary>
    public DnsName? CanonicalName;

    /// <summary>The final DNS response when one was received.</summary>
    public DnsMessage? Response;

    /// <summary>The resulting resource-record sets.</summary>
    public List<DnsResourceRecordSet> RecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The ordered CNAME and DNAME alias steps.</summary>
    public List<DnsAliasStep> AliasSteps = new List<DnsAliasStep>();

    /// <summary>The delegations followed during iterative resolution.</summary>
    public List<DnsDelegation> Delegations = new List<DnsDelegation>();

    /// <summary>The DNSSEC validation status of the result.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>Indicates that the result was obtained from a cache.</summary>
    public bool FromCache;

    /// <summary>Indicates that an expired cache entry was deliberately served.</summary>
    public bool Stale;

    /// <summary>Indicates that a result TTL is available.</summary>
    public bool HasTimeToLive;

    /// <summary>The minimum remaining result TTL in seconds.</summary>
    public uint TimeToLive;

    /// <summary>The ordered upstream query outcomes.</summary>
    public List<DnsQueryResult> Queries = new List<DnsQueryResult>();

    /// <summary>Diagnostics produced during resolution.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds one positive, negative, or failure cache entry.</summary>
public class DnsCacheEntry
{
    /// <summary>The semantic cache entry kind.</summary>
    public DnsCacheEntryKind Kind;

    /// <summary>The canonical cache key name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized cache key record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw cache key record-type integer.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized cache key class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw cache key class integer.</summary>
    public int ClassValue = 1;

    /// <summary>The cached resource-record sets.</summary>
    public List<DnsResourceRecordSet> RecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The optional response retained with the cache entry.</summary>
    public DnsMessage? Message;

    /// <summary>The optional SOA record proving a negative response.</summary>
    public DnsResourceRecord? NegativeSoa;

    /// <summary>The DNSSEC validation status retained with the entry.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>The time at which the entry was stored.</summary>
    public DnsClockValue StoredAt = new DnsClockValue();

    /// <summary>The time at which the entry expires.</summary>
    public DnsClockValue ExpiresAt = new DnsClockValue();

    /// <summary>Indicates that a stale-use deadline is available.</summary>
    public bool HasStaleUntil;

    /// <summary>The last time at which the expired entry can be served stale.</summary>
    public DnsClockValue StaleUntil = new DnsClockValue();

    /// <summary>The original cache TTL in seconds.</summary>
    public uint OriginalTimeToLive;

    /// <summary>Indicates that the cached data came from an authoritative answer.</summary>
    public bool Authoritative;

    /// <summary>The cache credibility used when replacing competing data.</summary>
    public int Credibility;
}

/// <summary>Holds the result of looking up one resolver cache key.</summary>
public class DnsCacheLookup
{
    /// <summary>Indicates that a usable cache entry was found.</summary>
    public bool Found;

    /// <summary>Indicates that the entry has not expired.</summary>
    public bool Fresh;

    /// <summary>Indicates that the entry is expired but within its stale lifetime.</summary>
    public bool Stale;

    /// <summary>Indicates that the entry should be refreshed before expiration.</summary>
    public bool NeedsPrefetch;

    /// <summary>The matching cache entry.</summary>
    public DnsCacheEntry? Entry;

    /// <summary>Indicates that a remaining TTL is available.</summary>
    public bool HasRemainingTimeToLive;

    /// <summary>The remaining TTL in seconds.</summary>
    public uint RemainingTimeToLive;

    /// <summary>The time used to age the cache entry.</summary>
    public DnsClockValue LookedUpAt = new DnsClockValue();
}

/// <summary>Holds one delegation and its usable name-server information.</summary>
public class DnsDelegation
{
    /// <summary>The delegated zone name.</summary>
    public DnsName Zone = new DnsName();

    /// <summary>The authoritative name-server names.</summary>
    public List<DnsName> NameServers = new List<DnsName>();

    /// <summary>The accepted in-bailiwick glue addresses.</summary>
    public List<DnsAddressResult> GlueAddresses = new List<DnsAddressResult>();

    /// <summary>The authority record sets that established the delegation.</summary>
    public List<DnsResourceRecordSet> AuthorityRecordSets = new List<DnsResourceRecordSet>();

    /// <summary>Indicates that the delegation is DNSSEC-secure.</summary>
    public bool Secure;

    /// <summary>Indicates that the delegation data passed bailiwick checks.</summary>
    public bool InBailiwick;

    /// <summary>Indicates that the selected name server is lame for the zone.</summary>
    public bool Lame;

    /// <summary>The delegation depth from the starting point.</summary>
    public int Depth;
}

/// <summary>Holds the root delegation used to begin iterative resolution.</summary>
public class DnsRootHints
{
    /// <summary>The root name-server delegation and addresses.</summary>
    public DnsDelegation Root = new DnsDelegation();

    /// <summary>The optional source description of the root hints.</summary>
    public string? Source;

    /// <summary>The time at which the root hints were loaded.</summary>
    public DnsClockValue LoadedAt = new DnsClockValue();

    /// <summary>Indicates that the root hints passed structural validation.</summary>
    public bool Valid;
}

/// <summary>Controls bounded iterative recursive resolution.</summary>
public class DnsRecursiveOptions
{
    /// <summary>The largest number of upstream queries.</summary>
    public int MaximumQueries = 64;

    /// <summary>The largest amount of implementation-defined resolver work.</summary>
    public int MaximumWork = 10000;

    /// <summary>The largest recursion depth.</summary>
    public int MaximumDepth = 32;

    /// <summary>The largest number of alias links followed.</summary>
    public int MaximumAliasDepth = 16;

    /// <summary>The largest number of delegations followed.</summary>
    public int MaximumReferralDepth = 32;

    /// <summary>The total recursive resolution timeout in milliseconds.</summary>
    public int TotalTimeoutMilliseconds = 10000;

    /// <summary>The advertised EDNS UDP payload size.</summary>
    public int EdnsUdpPayloadSize = DnsProtocolLimits.DefaultEdnsUdpPayloadSize;

    /// <summary>Uses QNAME minimization while following delegations.</summary>
    public bool MinimizeQueryName = true;

    /// <summary>Allows a full-name query after a minimized query fails compatibly.</summary>
    public bool AllowMinimizationFallback = true;

    /// <summary>Randomizes ASCII letter case in query names.</summary>
    public bool RandomizeQueryCase = true;

    /// <summary>Uses EDNS for iterative queries.</summary>
    public bool UseEdns = true;

    /// <summary>Allows retry without EDNS after a compatible protocol failure.</summary>
    public bool AllowEdnsDowngrade = true;

    /// <summary>Allows retry over TCP after a truncated UDP answer.</summary>
    public bool AllowTcpFallback = true;

    /// <summary>Requests and validates DNSSEC data.</summary>
    public bool ValidateDnssec = true;

    /// <summary>The trust anchors, algorithms, times, and DNSSEC work limits.</summary>
    public DnsSecurityPolicy Security = new DnsSecurityPolicy();
}

/// <summary>Holds an address lookup result with DNS metadata.</summary>
public class DnsAddressResult
{
    /// <summary>The resolution outcome.</summary>
    public DnsResolveStatus Status;

    /// <summary>The final canonical name after following aliases.</summary>
    public DnsName? CanonicalName;

    /// <summary>The IPv4 or IPv6 address octet arrays.</summary>
    public List<byte[]> Addresses = new List<byte[]>();

    /// <summary>The address family corresponding to each address.</summary>
    public List<int> AddressFamilies = new List<int>();

    /// <summary>The DNS response that supplied the addresses.</summary>
    public DnsMessage? Response;

    /// <summary>The DNSSEC validation status of the result.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>Indicates that a result TTL is available.</summary>
    public bool HasTimeToLive;

    /// <summary>The minimum remaining address TTL in seconds.</summary>
    public uint TimeToLive;

    /// <summary>The ordered aliases followed during lookup.</summary>
    public List<DnsAliasStep> AliasSteps = new List<DnsAliasStep>();
}

/// <summary>Holds a mail-exchanger lookup result with DNS metadata.</summary>
public class DnsMailResult
{
    /// <summary>The resolution outcome.</summary>
    public DnsResolveStatus Status;

    /// <summary>The queried mail domain after following aliases.</summary>
    public DnsName? CanonicalName;

    /// <summary>The ordered preference and mail-exchanger values.</summary>
    public List<DnsPreferenceNameData> Exchanges = new List<DnsPreferenceNameData>();

    /// <summary>The DNS response that supplied the result.</summary>
    public DnsMessage? Response;

    /// <summary>The DNSSEC validation status of the result.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>Indicates that a result TTL is available.</summary>
    public bool HasTimeToLive;

    /// <summary>The minimum remaining mail-exchanger TTL in seconds.</summary>
    public uint TimeToLive;
}

/// <summary>Holds one resolved SRV, SVCB, or HTTPS service endpoint.</summary>
public class DnsServiceEndpoint
{
    /// <summary>The service selection priority.</summary>
    public int Priority;

    /// <summary>The relative selection weight among equal priorities.</summary>
    public int Weight;

    /// <summary>The service port.</summary>
    public int Port;

    /// <summary>The service target name.</summary>
    public DnsName Target = new DnsName();

    /// <summary>The resolved IPv4 or IPv6 address octet arrays.</summary>
    public List<byte[]> Addresses = new List<byte[]>();

    /// <summary>The address family corresponding to each address.</summary>
    public List<int> AddressFamilies = new List<int>();

    /// <summary>The optional SVCB or HTTPS binding data.</summary>
    public DnsSvcbData? ServiceBinding;

    /// <summary>The DNSSEC validation status of the endpoint.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>Indicates that an endpoint TTL is available.</summary>
    public bool HasTimeToLive;

    /// <summary>The minimum remaining endpoint TTL in seconds.</summary>
    public uint TimeToLive;
}

/// <summary>Holds a service or text lookup result with DNS metadata.</summary>
public class DnsServiceResult
{
    /// <summary>The resolution outcome.</summary>
    public DnsResolveStatus Status;

    /// <summary>The queried service name after following aliases.</summary>
    public DnsName? CanonicalName;

    /// <summary>The ordered resolved service endpoints.</summary>
    public List<DnsServiceEndpoint> Endpoints = new List<DnsServiceEndpoint>();

    /// <summary>The character-string byte values returned by a text lookup.</summary>
    public List<byte[]> TextValues = new List<byte[]>();

    /// <summary>The DNS response that supplied the result.</summary>
    public DnsMessage? Response;

    /// <summary>The DNSSEC validation status of the result.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>Indicates that a result TTL is available.</summary>
    public bool HasTimeToLive;

    /// <summary>The minimum remaining service or text TTL in seconds.</summary>
    public uint TimeToLive;
}
