namespace dns;

/// <summary>Executes bounded DNS queries with transport fallback.</summary>
public class DnsQueryClient : IDnsQueryClient
{
    internal int NextServerOffset = -1;
    internal IDnsTransport? UdpTransport;
    internal IDnsTransport? TcpTransport;
    internal IDnsTransport? TlsTransport;
    internal IDnsTransport? HttpsTransport;
    internal IDnsTransport? QuicTransport;

    /// <summary>Creates a DNS query client.</summary>
    public DnsQueryClient()
    {
    }

    /// <summary>Executes one DNS query and applies configured transport fallback.</summary>
    /// <param name="request">The question, endpoint, flags, and transport policy.</param>
    /// <returns>The final response and ordered transport-attempt metadata.</returns>
    public DnsQueryResult Query(DnsQueryRequest request)
    {
        return QueryClientOperations.Query(request, this);
    }
}
