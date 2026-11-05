using System;
using System.Net;

namespace diameter;

/// <summary>Resolves Diameter discovery records through a specific DNS server.</summary>
public class DiameterDnsResolver : IDnsResolver
{
    internal DnsClient Client;

    /// <summary>Initializes a resolver for a DNS server endpoint.</summary>
    /// <param name="serverAddress">The IPv4 or IPv6 DNS server address.</param>
    /// <param name="serverPort">The positive UDP and TCP DNS server port.</param>
    public DiameterDnsResolver(IPAddress serverAddress, int serverPort)
    {
        if (serverAddress == null)
        {
            throw new ArgumentException("A DNS server address is required.");
        }

        if (serverPort <= 0 || serverPort > 65535)
        {
            throw new ArgumentOutOfRangeException("serverPort");
        }

        Client = new DnsClient(serverAddress, serverPort);
    }

    /// <summary>Resolves Naming Authority Pointer records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    public DiameterDnsQueryResult ResolveDiameterNaptrRecords(
        string name,
        int timeoutMilliseconds)
    {
        return Client.ResolveDnsQuery(
            name,
            DnsMessageCodec.NaptrRecordType,
            timeoutMilliseconds);
    }

    /// <summary>Resolves Service Locator records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    public DiameterDnsQueryResult ResolveDiameterSrvRecords(
        string name,
        int timeoutMilliseconds)
    {
        return Client.ResolveDnsQuery(
            name,
            DnsMessageCodec.SrvRecordType,
            timeoutMilliseconds);
    }

    /// <summary>Resolves IPv4 address records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    public DiameterDnsQueryResult ResolveDiameterIpv4Records(
        string name,
        int timeoutMilliseconds)
    {
        return Client.ResolveDnsQuery(
            name,
            DnsMessageCodec.Ipv4RecordType,
            timeoutMilliseconds);
    }

    /// <summary>Resolves IPv6 address records.</summary>
    /// <param name="name">The DNS owner name to query.</param>
    /// <param name="timeoutMilliseconds">The positive total query timeout.</param>
    /// <returns>The correlated DNS query outcome.</returns>
    public DiameterDnsQueryResult ResolveDiameterIpv6Records(
        string name,
        int timeoutMilliseconds)
    {
        return Client.ResolveDnsQuery(
            name,
            DnsMessageCodec.Ipv6RecordType,
            timeoutMilliseconds);
    }
}
