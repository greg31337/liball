namespace dns;

/// <summary>Exchanges one encoded DNS query through a specific transport.</summary>
public interface IDnsTransport
{
    /// <summary>Performs one bounded DNS transport exchange.</summary>
    /// <param name="request">The endpoint, query bytes, deadline, and transport options.</param>
    /// <returns>The response bytes and transport metadata.</returns>
    DnsTransportResult Exchange(DnsTransportRequest request);

    /// <summary>Closes reusable connections and releases transport resources.</summary>
    void CloseTransport();
}
