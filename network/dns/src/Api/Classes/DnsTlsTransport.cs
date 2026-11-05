using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace dns;

/// <summary>Exchanges length-prefixed DNS messages over reusable authenticated TLS connections.</summary>
public class DnsTlsTransport : IDnsTransport
{
    internal object SyncRoot = new object();
    internal bool Closed;
    internal Socket? Connection;
    internal SslStream? Stream;
    internal IPEndPoint? ConnectionEndpoint;
    internal string? ConnectionAuthenticationName;
    internal bool ConnectionValidated;
    internal bool ConnectionCheckRevocation;
    internal DnsClockValue ConnectionCreatedAt = new DnsClockValue();
    internal DnsClockValue ConnectionLastUsedAt = new DnsClockValue();

    /// <summary>Creates a DNS-over-TLS transport.</summary>
    public DnsTlsTransport()
    {
    }

    /// <summary>Performs one bounded DNS-over-TLS exchange.</summary>
    /// <param name="request">The endpoint, encoded query, deadline, and TLS options.</param>
    /// <returns>The correlated response or terminal TLS outcome.</returns>
    public DnsTransportResult Exchange(DnsTransportRequest request)
    {
        lock (this.SyncRoot)
        {
            return TlsTransportOperations.Exchange(request, this);
        }
    }

    /// <summary>Closes the reusable TLS connection and prevents subsequent exchanges.</summary>
    public void CloseTransport()
    {
        lock (this.SyncRoot)
        {
            this.Closed = true;
            if (this.Stream != null)
            {
                this.Stream.Close();
                this.Stream = null;
            }

            if (this.Connection != null)
            {
                this.Connection.Close();
                this.Connection = null;
            }

            this.ConnectionEndpoint = null;
            this.ConnectionAuthenticationName = null;
        }
    }
}
