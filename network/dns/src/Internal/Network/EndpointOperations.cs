using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace dns;

internal static class EndpointOperations
{
    internal static List<IPEndPoint> Resolve(DnsEndpoint endpoint)
    {
        ValidateEndpoint(endpoint);
        List<IPEndPoint> result = new List<IPEndPoint>();
        if (endpoint.HasAddress)
        {
            result.Add(CreateNumeric(endpoint));
            return result;
        }

        string host = endpoint.Host == null ? string.Empty : endpoint.Host;
        IPAddress[] addresses = global::System.Net.Dns.GetHostAddresses(host);
        int index = 0;
        while (index < addresses.Length)
        {
            IPAddress address = addresses[index];
            if (address.AddressFamily == AddressFamily.InterNetwork ||
                address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                result.Add(new IPEndPoint(address, endpoint.Port));
            }

            index = index + 1;
        }

        if (result.Count == 0)
        {
            throw new ArgumentException(
                "The DNS endpoint host did not resolve to an IP address.");
        }

        return result;
    }

    internal static IPEndPoint CreateNumeric(DnsEndpoint endpoint)
    {
        ValidateEndpoint(endpoint);
        if (!endpoint.HasAddress)
        {
            throw new ArgumentException(
                "A numeric DNS endpoint address is required.");
        }

        byte[] addressBytes = CopyAddress(endpoint.Address);
        IPAddress address = new IPAddress(addressBytes);
        return new IPEndPoint(address, endpoint.Port);
    }

    internal static bool EndpointsEqual(
        IPEndPoint? left,
        IPEndPoint? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.Port == right.Port && left.Address.Equals(right.Address);
    }

    internal static DnsEndpoint ToDnsEndpoint(IPEndPoint endpoint)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }

        DnsEndpoint result = new DnsEndpoint();
        result.HasAddress = true;
        result.Address = endpoint.Address.GetAddressBytes();
        result.Port = endpoint.Port;
        if (endpoint.AddressFamily == AddressFamily.InterNetwork)
        {
            result.AddressFamily = 4;
        }
        else if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            result.AddressFamily = 6;
        }
        else
        {
            throw new ArgumentException(
                "Only IPv4 and IPv6 DNS endpoints are supported.");
        }

        return result;
    }

    static void ValidateEndpoint(DnsEndpoint endpoint)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }

        if (endpoint.Port < 1 || endpoint.Port > 65535)
        {
            throw new ArgumentOutOfRangeException("endpoint.Port");
        }

        if (!endpoint.HasAddress)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Host))
            {
                throw new ArgumentException(
                    "A DNS endpoint host or numeric address is required.");
            }

            return;
        }

        if (endpoint.Address == null)
        {
            throw new ArgumentException(
                "A numeric DNS endpoint address is required.");
        }

        if (endpoint.AddressFamily == 4 && endpoint.Address.Length != 4)
        {
            throw new ArgumentException(
                "An IPv4 DNS endpoint requires four address octets.");
        }

        if (endpoint.AddressFamily == 6 && endpoint.Address.Length != 16)
        {
            throw new ArgumentException(
                "An IPv6 DNS endpoint requires sixteen address octets.");
        }

        if (endpoint.AddressFamily != 4 && endpoint.AddressFamily != 6)
        {
            throw new ArgumentException(
                "A numeric DNS endpoint address family must be four or six.");
        }
    }

    static byte[] CopyAddress(byte[] address)
    {
        byte[] result = new byte[address.Length];
        Array.Copy(address, result, address.Length);
        return result;
    }
}
