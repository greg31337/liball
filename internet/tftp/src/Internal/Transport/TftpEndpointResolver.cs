using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tftp;

internal static class TftpEndpointResolver
{
    internal static TftpEndpointResolutionResult ResolveClientEndpoint(
        TftpEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (endpoint == null)
        {
            throw new TftpProtocolException(
                "The TFTP endpoint cannot be null.",
                false,
                0);
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new TftpProtocolException(
                "The remote host cannot be empty.",
                false,
                0);
        }

        if (endpoint.Port < 1 || endpoint.Port > 65535)
        {
            throw new TftpProtocolException(
                "The remote UDP port is outside the valid range.",
                false,
                0);
        }

        AddressFamily requestedFamily = endpoint.AddressFamily;
        if (requestedFamily != AddressFamily.Unspecified &&
            requestedFamily != AddressFamily.InterNetwork &&
            requestedFamily != AddressFamily.InterNetworkV6)
        {
            throw new TftpProtocolException(
                "The requested address family must be IPv4, IPv6, or unspecified.",
                false,
                0);
        }

        if (endpoint.LocalAddress != null)
        {
            AddressFamily localFamily = endpoint.LocalAddress.AddressFamily;
            if (localFamily != AddressFamily.InterNetwork &&
                localFamily != AddressFamily.InterNetworkV6)
            {
                throw new TftpProtocolException(
                    "The local address must use IPv4 or IPv6.",
                    false,
                    0);
            }

            if (requestedFamily != AddressFamily.Unspecified &&
                requestedFamily != localFamily)
            {
                throw new TftpProtocolException(
                    "The local address does not match the requested address family.",
                    false,
                    0);
            }

            requestedFamily = localFamily;
        }

        IPAddress[] addresses = Dns.GetHostAddressesAsync(
            endpoint.Host,
            requestedFamily,
            cancellationToken).GetAwaiter().GetResult();
        cancellationToken.ThrowIfCancellationRequested();

        IPAddress? selectedAddress = null;
        int index = 0;
        while (index < addresses.Length && selectedAddress == null)
        {
            IPAddress candidate = addresses[index];
            if (requestedFamily == AddressFamily.Unspecified ||
                candidate.AddressFamily == requestedFamily)
            {
                selectedAddress = candidate;
            }

            index = index + 1;
        }

        if (selectedAddress == null)
        {
            throw new TftpProtocolException(
                "The remote host has no address in the requested family.",
                false,
                0);
        }

        AddressFamily selectedFamily = selectedAddress.AddressFamily;
        IPAddress localAddress = IPAddress.Any;
        if (selectedFamily == AddressFamily.InterNetworkV6)
        {
            localAddress = IPAddress.IPv6Any;
        }

        if (endpoint.LocalAddress != null)
        {
            localAddress = endpoint.LocalAddress;
        }

        TftpEndpointResolutionResult result = new TftpEndpointResolutionResult();
        result.AddressFamily = selectedFamily;
        result.RemoteEndPoint = new IPEndPoint(selectedAddress, endpoint.Port);
        result.LocalEndPoint = new IPEndPoint(localAddress, 0);
        return result;
    }
}
