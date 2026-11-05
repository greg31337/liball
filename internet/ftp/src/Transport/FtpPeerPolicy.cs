using System.Net;
using System.Net.Sockets;

namespace ftp;

internal static class FtpPeerPolicy
{
    internal static IPAddress NormalizeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 &&
            address.ScopeId != 0 && !RequiresScopeIdentifier(address))
        {
            return new IPAddress(address.GetAddressBytes(), 0);
        }

        return address;
    }

    internal static FtpAddressFamily GetAddressFamily(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            return FtpAddressFamily.IPv4;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return FtpAddressFamily.IPv6;
        }

        throw new FtpConfigurationException(
            "A peer address must use IPv4 or IPv6.", null);
    }

    internal static bool IsAddressFamilyMatch(IPAddress first, IPAddress second)
    {
        return GetAddressFamily(first) == GetAddressFamily(second);
    }

    internal static bool IsSameAddress(IPAddress first, IPAddress second)
    {
        IPAddress normalizedFirst = NormalizeAddress(first);
        IPAddress normalizedSecond = NormalizeAddress(second);
        return normalizedFirst.AddressFamily == normalizedSecond.AddressFamily &&
            normalizedFirst.Equals(normalizedSecond);
    }

    internal static bool IsUnspecifiedAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        return normalized.Equals(IPAddress.Any) ||
            normalized.Equals(IPAddress.IPv6Any);
    }

    internal static bool IsLoopbackAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return bytes[0] == 127;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return normalized.Equals(IPAddress.IPv6Loopback);
        }

        return false;
    }

    internal static bool IsPrivateAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            if (bytes[0] == 10)
            {
                return true;
            }

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return (bytes[0] & 254) == 252 || normalized.IsIPv6SiteLocal;
        }

        return false;
    }

    internal static bool IsLinkLocalAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return bytes[0] == 169 && bytes[1] == 254;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return normalized.IsIPv6LinkLocal;
        }

        return false;
    }

    internal static bool IsMulticastAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return bytes[0] >= 224 && bytes[0] <= 239;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return normalized.IsIPv6Multicast;
        }

        return false;
    }

    internal static bool IsPrivateOrLocalAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily != AddressFamily.InterNetwork &&
            normalized.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return true;
        }

        return IsPrivateAddress(normalized) ||
            IsLoopbackAddress(normalized) ||
            IsLinkLocalAddress(normalized);
    }

    internal static bool IsWildcardAddress(IPAddress address)
    {
        return IsUnspecifiedAddress(address);
    }

    internal static bool IsProhibitedPassiveTarget(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (normalized.AddressFamily != AddressFamily.InterNetwork)
        {
            return true;
        }

        byte[] bytes = normalized.GetAddressBytes();
        return IsUnspecifiedAddress(normalized) ||
            IsMulticastAddress(normalized) ||
            bytes[0] == 0 || bytes[0] >= 240;
    }

    internal static bool IsProhibitedActiveTarget(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (IsUnspecifiedAddress(normalized) || IsMulticastAddress(normalized))
        {
            return true;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return bytes[0] == 0 || bytes[0] >= 240;
        }

        return normalized.AddressFamily != AddressFamily.InterNetworkV6;
    }

    internal static bool IsProhibitedAdvertisedAddress(IPAddress address)
    {
        IPAddress normalized = NormalizeAddress(address);
        if (IsUnspecifiedAddress(normalized) || IsMulticastAddress(normalized))
        {
            return true;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalized.GetAddressBytes();
            return bytes[0] == 0 || bytes[0] >= 240;
        }

        return normalized.AddressFamily != AddressFamily.InterNetworkV6;
    }

    internal static bool RequiresScopeIdentifier(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
            (address.IsIPv6LinkLocal ||
                address.IsIPv6SiteLocal ||
                address.IsIPv6Multicast);
    }
}
