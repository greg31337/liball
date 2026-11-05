namespace dns;

/// <summary>Exchanges framed DNS messages on bidirectional QUIC streams.</summary>
public class DnsQuicTransport : IDnsTransport
{
    internal object SyncRoot = new object();
    internal bool Closed;
    internal IQuicExchangeProvider? ExchangeProvider;

    /// <summary>Creates a DNS-over-QUIC transport.</summary>
    public DnsQuicTransport()
    {
    }

    /// <summary>Performs one bounded DNS-over-QUIC exchange.</summary>
    /// <param name="request">The endpoint, encoded query, deadline, and QUIC options.</param>
    /// <returns>The correlated response or terminal QUIC outcome.</returns>
    public DnsTransportResult Exchange(DnsTransportRequest request)
    {
        lock (this.SyncRoot)
        {
            return QuicTransportOperations.Exchange(request, this);
        }
    }

    /// <summary>Prevents subsequent DNS-over-QUIC exchanges.</summary>
    public void CloseTransport()
    {
        lock (this.SyncRoot)
        {
            this.Closed = true;
        }
    }
}
