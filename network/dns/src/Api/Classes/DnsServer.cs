namespace dns;

/// <summary>Runs bounded authoritative DNS service loops.</summary>
public class DnsServer : IDnsServer
{
    internal IDnsZoneStore ZoneStore;
    internal object StateLock = new object();
    internal bool Running;
    internal bool StopRequested;

    /// <summary>Creates an authoritative server over one zone store.</summary>
    /// <param name="zoneStore">The hosted zones served to DNS clients.</param>
    public DnsServer(IDnsZoneStore zoneStore)
    {
        if (zoneStore == null)
        {
            throw new System.ArgumentNullException("zoneStore");
        }

        this.ZoneStore = zoneStore;
    }

    /// <summary>Runs authoritative service until stopped, cancelled, or failed.</summary>
    /// <param name="options">The endpoints, protocols, limits, keys, and stop state.</param>
    /// <returns>The terminal lifecycle state and request counts.</returns>
    public DnsServerRunResult RunServer(DnsServerOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        lock (this.StateLock)
        {
            if (this.Running)
            {
                throw new System.InvalidOperationException(
                    "The DNS server is already running.");
            }

            this.Running = true;
            this.StopRequested = false;
        }

        DnsServerRunResult result = DnsServerOperations.Run(
            this,
            options,
            this.ZoneStore);
        lock (this.StateLock)
        {
            this.Running = false;
        }

        return result;
    }

    /// <summary>Requests graceful shutdown of active UDP and TCP service loops.</summary>
    public void StopServer()
    {
        lock (this.StateLock)
        {
            this.StopRequested = true;
        }
    }

    internal bool ReadStopRequested()
    {
        lock (this.StateLock)
        {
            return this.StopRequested;
        }
    }
}
