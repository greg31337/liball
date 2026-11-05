using System.Net.Http;

namespace dns;

/// <summary>Exchanges DNS wire messages using DNS over HTTPS.</summary>
public class DnsHttpsTransport : IDnsTransport
{
    internal object SyncRoot = new object();
    internal bool Closed;
    internal HttpMessageHandler? MessageHandler;

    /// <summary>Creates a DNS-over-HTTPS transport.</summary>
    public DnsHttpsTransport()
    {
    }

    /// <summary>Performs one bounded DNS-over-HTTPS exchange.</summary>
    /// <param name="request">The encoded query, HTTPS URI, limits, and redirect policy.</param>
    /// <returns>The correlated DNS response or terminal HTTPS outcome.</returns>
    public DnsTransportResult Exchange(DnsTransportRequest request)
    {
        lock (this.SyncRoot)
        {
            return HttpsTransportOperations.Exchange(request, this);
        }
    }

    /// <summary>Prevents subsequent DNS-over-HTTPS exchanges.</summary>
    public void CloseTransport()
    {
        lock (this.SyncRoot)
        {
            this.Closed = true;
        }
    }
}
