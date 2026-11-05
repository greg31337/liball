using System.Collections.Generic;

namespace dns;

/// <summary>Defines data-only authorization and rate-limit policy for DNS operations.</summary>
public class DnsAuthorizationPolicy
{
    /// <summary>Allows ordinary authoritative queries.</summary>
    public bool AllowQueries = true;

    /// <summary>Allows recursive service for authorized clients.</summary>
    public bool AllowRecursion;

    /// <summary>Allows AXFR and IXFR operations.</summary>
    public bool AllowTransfers;

    /// <summary>Allows dynamic update operations.</summary>
    public bool AllowUpdates;

    /// <summary>Allows zone-change notifications.</summary>
    public bool AllowNotify;

    /// <summary>Requires an authenticated TSIG key for zone transfers.</summary>
    public bool RequireTsigForTransfers = true;

    /// <summary>Requires an authenticated TSIG key for dynamic updates.</summary>
    public bool RequireTsigForUpdates = true;

    /// <summary>Requires an authenticated TSIG key for notifications.</summary>
    public bool RequireTsigForNotify = true;

    /// <summary>The allowed client address octet arrays.</summary>
    public List<byte[]> AllowedClientAddresses = new List<byte[]>();

    /// <summary>The address family corresponding to each allowed client address.</summary>
    public List<int> AllowedClientAddressFamilies = new List<int>();

    /// <summary>The prefix length corresponding to each allowed client address.</summary>
    public List<int> AllowedClientPrefixLengths = new List<int>();

    /// <summary>The denied client address octet arrays.</summary>
    public List<byte[]> DeniedClientAddresses = new List<byte[]>();

    /// <summary>The address family corresponding to each denied client address.</summary>
    public List<int> DeniedClientAddressFamilies = new List<int>();

    /// <summary>The prefix length corresponding to each denied client address.</summary>
    public List<int> DeniedClientPrefixLengths = new List<int>();

    /// <summary>The TSIG key names permitted for restricted operations.</summary>
    public List<DnsName> AllowedTsigKeyNames = new List<DnsName>();

    /// <summary>The zone names permitted for restricted operations.</summary>
    public List<DnsName> AllowedZones = new List<DnsName>();

    /// <summary>The response limit per client and time window, where zero disables limiting.</summary>
    public int RateLimitResponses;

    /// <summary>The rate-limit accounting window in milliseconds.</summary>
    public int RateLimitWindowMilliseconds = 1000;

    /// <summary>The number of limited responses between deliberately slipped responses.</summary>
    public int RateLimitSlip;
}

/// <summary>Controls authoritative answer production and response sizing.</summary>
public class DnsAuthoritativeOptions
{
    /// <summary>The client, operation, TSIG, and rate-limit policy.</summary>
    public DnsAuthorizationPolicy Authorization = new DnsAuthorizationPolicy();

    /// <summary>Advertises recursive service availability.</summary>
    public bool RecursionAvailable;

    /// <summary>Serves available DNSSEC signatures and denial proofs.</summary>
    public bool ServeDnssec = true;

    /// <summary>Uses EDNS when the request contains a valid OPT record.</summary>
    public bool EnableEdns = true;

    /// <summary>Includes useful additional address and service records.</summary>
    public bool IncludeAdditionalRecords = true;

    /// <summary>Produces minimal responses when optional data can be omitted.</summary>
    public bool MinimalResponses;

    /// <summary>Produces a minimal answer for an ANY query.</summary>
    public bool MinimizeAnyResponses = true;

    /// <summary>Echoes the request recursion-desired flag in responses.</summary>
    public bool EchoRecursionDesired = true;

    /// <summary>Allows authoritative DNSSEC validation to set authenticated data.</summary>
    public bool SetAuthenticData;

    /// <summary>Requires a valid DNS Cookie before sending an amplified response.</summary>
    public bool RequireCookieForAmplification;

    /// <summary>The largest authoritative UDP response in octets.</summary>
    public int MaximumUdpResponseLength = DnsProtocolLimits.DefaultEdnsUdpPayloadSize;

    /// <summary>The largest authoritative TCP response in octets.</summary>
    public int MaximumTcpResponseLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>The largest number of alias links followed while answering.</summary>
    public int MaximumAliasDepth = 16;

    /// <summary>The largest total number of response resource records.</summary>
    public int MaximumResponseRecords = DnsProtocolLimits.MaximumSectionEntryCount;

    /// <summary>The DNSSEC algorithm, proof, and trust policy.</summary>
    public DnsSecurityPolicy Security = new DnsSecurityPolicy();
}

/// <summary>Holds one authoritative DNS request and client context.</summary>
public class DnsAuthoritativeRequest
{
    /// <summary>The decoded DNS request message.</summary>
    public DnsMessage Message = new DnsMessage();

    /// <summary>The exact received wire bytes.</summary>
    public byte[] MessageBytes = [];

    /// <summary>The remote client endpoint.</summary>
    public DnsEndpoint ClientEndpoint = new DnsEndpoint();

    /// <summary>The local server endpoint that received the request.</summary>
    public DnsEndpoint ServerEndpoint = new DnsEndpoint();

    /// <summary>The transport that received the request.</summary>
    public DnsTransportKind Transport;

    /// <summary>The authoritative answer and authorization policy.</summary>
    public DnsAuthoritativeOptions Options = new DnsAuthoritativeOptions();

    /// <summary>The optional TSIG authentication result for the request.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>The time at which the request was received.</summary>
    public DnsClockValue ReceivedAt = new DnsClockValue();

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds an authoritative answer and its construction metadata.</summary>
public class DnsAuthoritativeResult
{
    /// <summary>Indicates that authoritative processing completed successfully.</summary>
    public bool Success;

    /// <summary>The semantic kind of authoritative answer.</summary>
    public DnsAuthoritativeAnswerKind Kind;

    /// <summary>The recognized DNS response code.</summary>
    public DnsResponseCode ResponseCode;

    /// <summary>The raw complete response-code integer, including extended values.</summary>
    public int ResponseCodeValue;

    /// <summary>The generated DNS response message.</summary>
    public DnsMessage? Response;

    /// <summary>The encoded response bytes.</summary>
    public byte[] ResponseBytes = [];

    /// <summary>The apex of the selected authoritative zone.</summary>
    public DnsName? Zone;

    /// <summary>The record sets selected for the answer section.</summary>
    public List<DnsResourceRecordSet> AnswerRecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The record sets selected for the authority section.</summary>
    public List<DnsResourceRecordSet> AuthorityRecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The record sets selected for the additional section.</summary>
    public List<DnsResourceRecordSet> AdditionalRecordSets = new List<DnsResourceRecordSet>();

    /// <summary>Indicates that the selected zone was authoritative for the name.</summary>
    public bool Authoritative;

    /// <summary>Indicates that response data was removed to satisfy a size limit.</summary>
    public bool Truncated;

    /// <summary>The DNSSEC security state of the answer.</summary>
    public DnsSecurityStatus SecurityStatus;

    /// <summary>The optional response TSIG outcome.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>Diagnostics produced while constructing the answer.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Controls bounded UDP and TCP authoritative server loops.</summary>
public class DnsServerOptions
{
    /// <summary>The local endpoints on which the server listens.</summary>
    public List<DnsEndpoint> Endpoints = new List<DnsEndpoint>();

    /// <summary>Enables DNS service over UDP.</summary>
    public bool EnableUdp = true;

    /// <summary>Enables DNS service over TCP.</summary>
    public bool EnableTcp = true;

    /// <summary>The authoritative answer and authorization policy.</summary>
    public DnsAuthoritativeOptions Authoritative = new DnsAuthoritativeOptions();

    /// <summary>The TSIG keys available to server operations.</summary>
    public List<DnsTsigKey> TsigKeys = new List<DnsTsigKey>();

    /// <summary>The socket listen backlog for TCP.</summary>
    public int TcpListenBacklog = 128;

    /// <summary>The largest number of concurrent client operations.</summary>
    public int MaximumConcurrentRequests = 256;

    /// <summary>The largest accepted request in octets.</summary>
    public int MaximumRequestLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>The TCP client idle timeout in milliseconds.</summary>
    public int TcpIdleTimeoutMilliseconds = 10000;

    /// <summary>The maximum TCP connection lifetime in milliseconds.</summary>
    public int TcpConnectionLifetimeMilliseconds = 60000;

    /// <summary>The maximum time allowed to process one request in milliseconds.</summary>
    public int RequestTimeoutMilliseconds = 5000;

    /// <summary>The largest number of sequential requests accepted on one TCP connection.</summary>
    public int MaximumTcpRequestsPerConnection = 100;

    /// <summary>Indicates that graceful server shutdown has been requested.</summary>
    public bool StopRequested;
}

/// <summary>Holds server lifecycle, endpoint, and request-count state.</summary>
public class DnsServerRunResult
{
    /// <summary>The lifecycle or terminal server state.</summary>
    public DnsServerStatus Status;

    /// <summary>The endpoints successfully bound by the server.</summary>
    public List<DnsEndpoint> BoundEndpoints = new List<DnsEndpoint>();

    /// <summary>The server start time.</summary>
    public DnsClockValue StartedAt = new DnsClockValue();

    /// <summary>The server stop or observation time.</summary>
    public DnsClockValue CompletedAt = new DnsClockValue();

    /// <summary>The total number of accepted DNS requests.</summary>
    public long RequestCount;

    /// <summary>The number of accepted UDP requests.</summary>
    public long UdpRequestCount;

    /// <summary>The number of accepted TCP requests.</summary>
    public long TcpRequestCount;

    /// <summary>The number of requests that failed during processing.</summary>
    public long FailedRequestCount;

    /// <summary>Indicates that cancellation or a stop request ended the run.</summary>
    public bool Cancelled;

    /// <summary>Diagnostics produced by the server loop.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}
