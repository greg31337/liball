using System.Net;
using System.Net.Sockets;

namespace tftp;

internal static class TftpListenerDatagramFilter
{
    internal static bool ShouldSuppressDestination(IPAddress destinationAddress)
    {
        if (destinationAddress == null)
        {
            return true;
        }

        if (destinationAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return destinationAddress.IsIPv6Multicast;
        }

        if (destinationAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            return true;
        }

        if (destinationAddress.Equals(IPAddress.Broadcast))
        {
            return true;
        }

        byte[] addressBytes = destinationAddress.GetAddressBytes();
        return addressBytes[0] >= 224 && addressBytes[0] <= 239;
    }
}
