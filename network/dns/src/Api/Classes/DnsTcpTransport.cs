using System.Net;
using System.Net.Sockets;

namespace dns;

/// <summary>Exchanges length-prefixed DNS messages over reusable TCP connections.</summary>
public class DnsTcpTransport : IDnsTransport
{
    internal object SyncRoot = new object();
    internal bool Closed;
    internal Socket? Connection;
    internal IPEndPoint? ConnectionEndpoint;
    internal DnsClockValue ConnectionCreatedAt = new DnsClockValue();
    internal DnsClockValue ConnectionLastUsedAt = new DnsClockValue();

    /// <summary>Creates a TCP DNS transport.</summary>
    public DnsTcpTransport()
    {
    }

    /// <summary>Performs one bounded DNS-over-TCP exchange.</summary>
    /// <param name="request">The endpoint, encoded query, deadline, and TCP options.</param>
    /// <returns>The correlated response or terminal TCP outcome.</returns>
    public DnsTransportResult Exchange(DnsTransportRequest request)
    {
        lock (this.SyncRoot)
        {
            return TcpTransportOperations.Exchange(request, this);
        }
    }

    /// <summary>Closes the reusable TCP connection and prevents subsequent exchanges.</summary>
    public void CloseTransport()
    {
        lock (this.SyncRoot)
        {
            this.Closed = true;
            if (this.Connection != null)
            {
                this.Connection.Close();
                this.Connection = null;
                this.ConnectionEndpoint = null;
            }
        }
    }
}
