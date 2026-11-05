namespace dns;

/// <summary>Executes correlated DNS queries with retries and transport fallback.</summary>
public interface IDnsQueryClient
{
    /// <summary>Executes one DNS query across its configured servers and transports.</summary>
    /// <param name="request">The question, servers, flags, and retry policy.</param>
    /// <returns>The final response and all attempt metadata.</returns>
    DnsQueryResult Query(DnsQueryRequest request);
}
