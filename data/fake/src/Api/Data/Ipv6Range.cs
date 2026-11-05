namespace fake;

/// <summary>Describes an inclusive IPv6 network range.</summary>
public class Ipv6Range
{
    /// <summary>The address category used to select the range.</summary>
    public NetworkAddressKind Kind;

    /// <summary>The first address and network address of the range.</summary>
    public System.Net.IPAddress NetworkAddress = new System.Net.IPAddress(new byte[16]);

    /// <summary>The final inclusive address of the range.</summary>
    public System.Net.IPAddress LastAddress = new System.Net.IPAddress(new byte[16]);

    /// <summary>The contiguous IPv6 network mask.</summary>
    public System.Net.IPAddress Netmask = new System.Net.IPAddress(new byte[16]);

    /// <summary>The network prefix length from zero through 128.</summary>
    public int PrefixLength;

    /// <summary>The number of addresses in the inclusive range.</summary>
    public System.Numerics.BigInteger AddressCount;

    /// <summary>The network address and prefix in CIDR notation.</summary>
    public string Cidr = string.Empty;

    internal byte[] NetworkBytes = new byte[16];
    internal byte[] LastBytes = new byte[16];

    internal Ipv6Range()
    {
    }

    internal bool Contains(System.Net.IPAddress address)
    {
        Guard.NotNull(address, "address");
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return false;
        }
        byte[] bytes = address.GetAddressBytes();
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] < NetworkBytes[index] ||
                bytes[index] > LastBytes[index])
            {
                return false;
            }
            if (bytes[index] > NetworkBytes[index] &&
                bytes[index] < LastBytes[index])
            {
                return true;
            }
            index++;
        }
        return true;
    }
}
