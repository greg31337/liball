namespace whois;

internal static class AddressClassifier
{
    internal static bool IsPublicUnicast(System.Net.IPAddress? address)
    {
        if (address == null)
        {
            throw new System.ArgumentNullException("address");
        }

        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return IsPublicIpv4(bytes);
        }
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return IsPublicIpv6(bytes);
        }
        return false;
    }

    internal static bool IsPublicIpv4(byte[] bytes)
    {
        int first = bytes[0];
        int second = bytes[1];
        int third = bytes[2];

        if (first == 0 || first == 10 || first == 127)
        {
            return false;
        }
        if (first == 100 && second >= 64 && second <= 127)
        {
            return false;
        }
        if (first == 169 && second == 254)
        {
            return false;
        }
        if (first == 172 && second >= 16 && second <= 31)
        {
            return false;
        }
        if (first == 192 && second == 0 && third == 0)
        {
            return false;
        }
        if (first == 192 && second == 0 && third == 2)
        {
            return false;
        }
        if (first == 192 && second == 31 && third == 196)
        {
            return false;
        }
        if (first == 192 && second == 52 && third == 193)
        {
            return false;
        }
        if (first == 192 && second == 88 && third == 99)
        {
            return false;
        }
        if (first == 192 && second == 168)
        {
            return false;
        }
        if (first == 192 && second == 175 && third == 48)
        {
            return false;
        }
        if (first == 198 && (second == 18 || second == 19))
        {
            return false;
        }
        if (first == 198 && second == 51 && third == 100)
        {
            return false;
        }
        if (first == 203 && second == 0 && third == 113)
        {
            return false;
        }
        if (first >= 224)
        {
            return false;
        }
        return true;
    }

    internal static bool IsPublicIpv6(byte[] bytes)
    {
        if ((bytes[0] & 224) != 32)
        {
            return false;
        }
        if (bytes[0] == 32 && bytes[1] == 1 && (bytes[2] & 254) == 0)
        {
            return false;
        }
        if (bytes[0] == 32 && bytes[1] == 1 && bytes[2] == 13 && bytes[3] == 184)
        {
            return false;
        }
        if (bytes[0] == 32 && bytes[1] == 2)
        {
            return false;
        }
        if (bytes[0] == 38 && bytes[1] == 32 && bytes[2] == 0 && bytes[3] == 79 && bytes[4] == 128 && bytes[5] == 0)
        {
            return false;
        }
        if (bytes[0] == 63 && bytes[1] == 254)
        {
            return false;
        }
        if (bytes[0] == 63 && bytes[1] == 255 && (bytes[2] & 240) == 0)
        {
            return false;
        }
        return true;
    }
}
