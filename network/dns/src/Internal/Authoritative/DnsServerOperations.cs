using System.Collections.Generic;
using System.Threading.Tasks;

namespace dns;

internal static class DnsServerOperations
{
    internal static DnsServerRunResult Run(
        DnsServer server,
        DnsServerOptions options,
        IDnsZoneStore zoneStore)
    {
        if (options.EnableUdp && options.EnableTcp)
        {
            Task<DnsServerRunResult> udpTask = Task.Run(
                () => UdpServerOperations.Run(
                    server,
                    options,
                    new DnsServerRequestServices(zoneStore)));
            DnsServerRunResult tcp = TcpServerOperations.Run(
                server,
                options,
                new DnsServerRequestServices(zoneStore));
            DnsServerRunResult udp = udpTask.GetAwaiter().GetResult();
            return MergeResults(udp, tcp);
        }

        if (options.EnableUdp)
        {
            return UdpServerOperations.Run(
                server,
                options,
                new DnsServerRequestServices(zoneStore));
        }

        if (options.EnableTcp)
        {
            return TcpServerOperations.Run(
                server,
                options,
                new DnsServerRequestServices(zoneStore));
        }

        DnsServerRunResult result = new DnsServerRunResult();
        result.Status = DnsServerStatus.Stopped;
        result.StartedAt = TransportTime.ReadClock();
        result.CompletedAt = TransportTime.ReadClock();
        return result;
    }

    static DnsServerRunResult MergeResults(
        DnsServerRunResult udp,
        DnsServerRunResult tcp)
    {
        DnsServerRunResult result = new DnsServerRunResult();
        result.StartedAt = udp.StartedAt;
        if (tcp.StartedAt.MonotonicMilliseconds <
            result.StartedAt.MonotonicMilliseconds)
        {
            result.StartedAt = tcp.StartedAt;
        }

        result.CompletedAt = TransportTime.ReadClock();
        result.RequestCount = udp.RequestCount + tcp.RequestCount;
        result.UdpRequestCount = udp.UdpRequestCount;
        result.TcpRequestCount = tcp.TcpRequestCount;
        result.FailedRequestCount =
            udp.FailedRequestCount + tcp.FailedRequestCount;
        if (udp.Status == DnsServerStatus.Failed ||
            tcp.Status == DnsServerStatus.Failed)
        {
            result.Status = DnsServerStatus.Failed;
        }
        else if (udp.Cancelled || tcp.Cancelled)
        {
            result.Status = DnsServerStatus.Cancelled;
            result.Cancelled = true;
        }
        else
        {
            result.Status = DnsServerStatus.Stopped;
        }

        CopyEndpoints(udp.BoundEndpoints, result.BoundEndpoints);
        CopyEndpoints(tcp.BoundEndpoints, result.BoundEndpoints);
        CopyDiagnostics(udp.Diagnostics, result.Diagnostics);
        CopyDiagnostics(tcp.Diagnostics, result.Diagnostics);
        return result;
    }

    static void CopyEndpoints(
        List<DnsEndpoint> source,
        List<DnsEndpoint> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!ContainsEndpoint(destination, source[index]))
            {
                destination.Add(source[index]);
            }

            index = index + 1;
        }
    }

    static bool ContainsEndpoint(
        List<DnsEndpoint> endpoints,
        DnsEndpoint candidate)
    {
        int endpointIndex = 0;
        while (endpointIndex < endpoints.Count)
        {
            DnsEndpoint endpoint = endpoints[endpointIndex];
            if (endpoint.Port == candidate.Port &&
                endpoint.AddressFamily == candidate.AddressFamily &&
                AddressesEqual(endpoint.Address, candidate.Address))
            {
                return true;
            }

            endpointIndex = endpointIndex + 1;
        }

        return false;
    }

    static bool AddressesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
