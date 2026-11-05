using System.Collections.Generic;

namespace dns;

/// <summary>Holds a portable DNS server network endpoint.</summary>
public class DnsEndpoint
{
    /// <summary>The optional DNS host or authentication name.</summary>
    public string? Host;

    /// <summary>Indicates that a numeric network address is supplied.</summary>
    public bool HasAddress;

    /// <summary>The numeric IPv4 or IPv6 address octets.</summary>
    public byte[] Address = [];

    /// <summary>The address family, where four and six identify IP versions.</summary>
    public int AddressFamily;

    /// <summary>The transport port.</summary>
    public int Port = DnsPorts.Dns;
}

/// <summary>Holds UTC and monotonic readings captured at one logical instant.</summary>
public class DnsClockValue
{
    /// <summary>The UTC time as Unix milliseconds.</summary>
    public long UnixTimeMilliseconds;

    /// <summary>The monotonic process-relative time in milliseconds.</summary>
    public long MonotonicMilliseconds;
}

/// <summary>Controls bounded transport exchange and connection reuse.</summary>
public class DnsTransportOptions
{
    /// <summary>The requested DNS transport.</summary>
    public DnsTransportKind Kind;

    /// <summary>The timeout for one transport attempt in milliseconds.</summary>
    public int AttemptTimeoutMilliseconds = 2000;

    /// <summary>The total timeout across retries and fallback in milliseconds.</summary>
    public int TotalTimeoutMilliseconds = 5000;

    /// <summary>The largest number of transport attempts.</summary>
    public int MaximumAttempts = 3;

    /// <summary>The largest accepted response in octets.</summary>
    public int MaximumResponseLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>Uses a connected UDP socket to filter response sources.</summary>
    public bool ConnectUdpSocket = true;

    /// <summary>Allows a TCP-family connection to be reused for sequential queries.</summary>
    public bool ReuseConnections = true;

    /// <summary>The maximum idle connection lifetime in milliseconds.</summary>
    public int IdleConnectionTimeoutMilliseconds = 10000;

    /// <summary>The maximum total connection lifetime in milliseconds.</summary>
    public int ConnectionLifetimeMilliseconds = 60000;

    /// <summary>The DNS-over-TLS options.</summary>
    public DnsTlsOptions Tls = new DnsTlsOptions();

    /// <summary>The DNS-over-HTTPS options.</summary>
    public DnsHttpsOptions Https = new DnsHttpsOptions();

    /// <summary>The DNS-over-QUIC options.</summary>
    public DnsQuicOptions Quic = new DnsQuicOptions();
}

/// <summary>Controls DNS-over-TLS authentication.</summary>
public class DnsTlsOptions
{
    /// <summary>The optional certificate authentication name and TLS SNI value.</summary>
    public string? AuthenticationName;

    /// <summary>Requires platform certificate-chain validation.</summary>
    public bool ValidateCertificate = true;

    /// <summary>Checks certificate revocation status when supported.</summary>
    public bool CheckCertificateRevocation = true;

    /// <summary>Allows the authenticated connection to be reused.</summary>
    public bool ReuseConnection = true;
}

/// <summary>Controls DNS-over-HTTPS request and bootstrap behavior.</summary>
public class DnsHttpsOptions
{
    /// <summary>The absolute HTTPS resolver URI.</summary>
    public string? Uri;

    /// <summary>Uses HTTP GET instead of the default POST request.</summary>
    public bool UseGet;

    /// <summary>The DNS wire-format HTTP media type.</summary>
    public string MediaType = "application/dns-message";

    /// <summary>The optional numeric endpoint used to bootstrap the HTTPS origin.</summary>
    public DnsEndpoint? BootstrapEndpoint;

    /// <summary>Allows redirects after validating their destination policy.</summary>
    public bool AllowRedirects;

    /// <summary>The largest number of accepted redirects.</summary>
    public int MaximumRedirects;
}

/// <summary>Controls DNS-over-QUIC authentication and connection behavior.</summary>
public class DnsQuicOptions
{
    /// <summary>The optional certificate authentication name.</summary>
    public string? AuthenticationName;

    /// <summary>Requires platform certificate-chain validation.</summary>
    public bool ValidateCertificate = true;

    /// <summary>Allows zero round-trip data when the platform and server permit it.</summary>
    public bool AllowZeroRoundTripData;

    /// <summary>The QUIC idle timeout in milliseconds.</summary>
    public int IdleTimeoutMilliseconds = 10000;
}

/// <summary>Holds one low-level DNS transport exchange request.</summary>
public class DnsTransportRequest
{
    /// <summary>The remote DNS endpoint.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The requested transport and limits.</summary>
    public DnsTransportOptions Options = new DnsTransportOptions();

    /// <summary>The encoded DNS query bytes.</summary>
    public byte[] QueryBytes = [];

    /// <summary>The optional decoded query used for correlation.</summary>
    public DnsMessage? QueryMessage;

    /// <summary>Indicates that a total monotonic deadline is supplied.</summary>
    public bool HasDeadline;

    /// <summary>The total operation deadline.</summary>
    public DnsClockValue Deadline = new DnsClockValue();

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds one low-level DNS transport exchange outcome.</summary>
public class DnsTransportResult
{
    /// <summary>The transport outcome.</summary>
    public DnsTransportStatus Status;

    /// <summary>The remote endpoint used for the exchange.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The transport that produced the result.</summary>
    public DnsTransportKind Transport;

    /// <summary>The received DNS response bytes.</summary>
    public byte[] ResponseBytes = [];

    /// <summary>The optional decoded DNS response.</summary>
    public DnsMessage? ResponseMessage;

    /// <summary>The operation start time.</summary>
    public DnsClockValue StartedAt = new DnsClockValue();

    /// <summary>The operation completion time.</summary>
    public DnsClockValue CompletedAt = new DnsClockValue();

    /// <summary>The elapsed monotonic duration in milliseconds.</summary>
    public long ElapsedMilliseconds;

    /// <summary>Indicates that the received response was truncated.</summary>
    public bool Truncated;

    /// <summary>Indicates that another transport was used after the initial attempt.</summary>
    public bool UsedFallback;

    /// <summary>The transport that caused fallback to the final transport.</summary>
    public DnsTransportKind FallbackFrom;

    /// <summary>Indicates that the operation exceeded its deadline.</summary>
    public bool TimedOut;

    /// <summary>Indicates that caller cancellation ended the operation.</summary>
    public bool Cancelled;

    /// <summary>Protocol and transport diagnostics.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds a complete high-level DNS query request.</summary>
public class DnsQueryRequest
{
    /// <summary>The DNS question to send.</summary>
    public DnsQuestion Question = new DnsQuestion();

    /// <summary>The optional caller-supplied complete query message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional TSIG key used to authenticate the query and response.</summary>
    public DnsTsigKey? TsigKey;

    /// <summary>The ordered candidate DNS servers.</summary>
    public List<DnsEndpoint> Servers = new List<DnsEndpoint>();

    /// <summary>The transport, retry, and encrypted-protocol options.</summary>
    public DnsTransportOptions TransportOptions = new DnsTransportOptions();

    /// <summary>Requests recursive service from the server.</summary>
    public bool RecursionDesired = true;

    /// <summary>Requests EDNS use for the query.</summary>
    public bool UseEdns = true;

    /// <summary>Requests DNSSEC records through the EDNS DO flag.</summary>
    public bool RequestDnssec;

    /// <summary>Disables upstream DNSSEC validation through the CD flag.</summary>
    public bool CheckingDisabled;

    /// <summary>Allows automatic retry over TCP after UDP truncation.</summary>
    public bool AllowTcpFallback = true;

    /// <summary>Allows retry without EDNS after a compatible protocol failure.</summary>
    public bool AllowEdnsDowngrade = true;

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds the result of a high-level DNS query across all attempts.</summary>
public class DnsQueryResult
{
    /// <summary>Indicates that a valid correlated response was received.</summary>
    public bool Success;

    /// <summary>The terminal transport status.</summary>
    public DnsTransportStatus Status;

    /// <summary>The optional final response message.</summary>
    public DnsMessage? Response;

    /// <summary>The final response wire bytes.</summary>
    public byte[] ResponseBytes = [];

    /// <summary>The optional TSIG signing or response-authentication result.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>The ordered low-level transport attempts.</summary>
    public List<DnsTransportResult> Attempts = new List<DnsTransportResult>();

    /// <summary>The resulting per-server health and capability states.</summary>
    public List<DnsServerState> ServerStates = new List<DnsServerState>();

    /// <summary>Indicates that UDP-to-TCP or another transport fallback occurred.</summary>
    public bool UsedFallback;

    /// <summary>Indicates that EDNS was disabled after an incompatible response.</summary>
    public bool EdnsDowngraded;

    /// <summary>The total elapsed duration in milliseconds.</summary>
    public long ElapsedMilliseconds;

    /// <summary>Query and response diagnostics.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds observed health and protocol capabilities for one DNS server.</summary>
public class DnsServerState
{
    /// <summary>The server endpoint.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The preferred transport.</summary>
    public DnsTransportKind PreferredTransport;

    /// <summary>The number of attempted exchanges.</summary>
    public int AttemptCount;

    /// <summary>The number of consecutive failed exchanges.</summary>
    public int ConsecutiveFailures;

    /// <summary>Indicates that the server has completed a successful exchange.</summary>
    public bool HasSuccessfulExchange;

    /// <summary>The most recent attempt time.</summary>
    public DnsClockValue LastAttempt = new DnsClockValue();

    /// <summary>The most recent successful exchange time.</summary>
    public DnsClockValue LastSuccess = new DnsClockValue();

    /// <summary>Indicates that the server is known to support EDNS.</summary>
    public bool SupportsEdns;

    /// <summary>Indicates that EDNS capability has been determined.</summary>
    public bool HasEdnsCapability;

    /// <summary>The largest successful EDNS UDP payload size.</summary>
    public int EdnsUdpPayloadSize;

    /// <summary>Indicates that a temporary suppression deadline is supplied.</summary>
    public bool HasSuppressedUntil;

    /// <summary>The monotonic deadline until which the server is suppressed.</summary>
    public DnsClockValue SuppressedUntil = new DnsClockValue();
}
