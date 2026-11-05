namespace dns;

/// <summary>Runs bounded UDP and TCP authoritative DNS service loops.</summary>
public interface IDnsServer
{
    /// <summary>Runs authoritative service until stopped, cancelled, or failed.</summary>
    /// <param name="options">The endpoints, protocols, limits, keys, and stop state.</param>
    /// <returns>The terminal lifecycle state and request counts.</returns>
    DnsServerRunResult RunServer(DnsServerOptions options);

    /// <summary>Requests graceful shutdown of active UDP and TCP service loops.</summary>
    void StopServer();
}
