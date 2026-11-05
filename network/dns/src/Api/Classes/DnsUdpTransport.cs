namespace dns;

/// <summary>Exchanges DNS messages over bounded IPv4 or IPv6 UDP sockets.</summary>
public class DnsUdpTransport : IDnsTransport
{
    internal object SyncRoot = new object();
    internal bool Closed;

    /// <summary>Creates a UDP DNS transport.</summary>
    public DnsUdpTransport()
    {
    }

    /// <summary>Performs one bounded UDP DNS exchange.</summary>
    /// <param name="request">The endpoint, encoded query, deadline, and UDP options.</param>
    /// <returns>The correlated response or terminal UDP outcome.</returns>
    public DnsTransportResult Exchange(DnsTransportRequest request)
    {
        lock (this.SyncRoot)
        {
            return UdpTransportOperations.Exchange(request, this);
        }
    }

    /// <summary>Prevents subsequent exchanges from being started.</summary>
    public void CloseTransport()
    {
        lock (this.SyncRoot)
        {
            this.Closed = true;
        }
    }
}
