namespace fake;

/// <summary>
/// Describes an inclusive IPv4 network range.
/// </summary>
public class Ipv4Range
{
    /// <summary>The address category used to select the range.</summary>
    public NetworkAddressKind Kind;

    /// <summary>The first address and network address of the range.</summary>
    public System.Net.IPAddress NetworkAddress = new System.Net.IPAddress(new byte[4]);

    /// <summary>The final inclusive address of the range.</summary>
    public System.Net.IPAddress LastAddress = new System.Net.IPAddress(new byte[4]);

    /// <summary>The contiguous IPv4 network mask.</summary>
    public System.Net.IPAddress Netmask = new System.Net.IPAddress(new byte[4]);

    /// <summary>The network prefix length from zero through 32.</summary>
    public int PrefixLength;

    /// <summary>The number of addresses in the inclusive range.</summary>
    public ulong AddressCount;

    /// <summary>The network address and prefix in CIDR notation.</summary>
    public string Cidr = string.Empty;

    internal uint NetworkNumber;
    internal uint LastNumber;

    internal Ipv4Range()
    {
    }

    /// <summary>
    /// Reports whether an IPv4 address is inside the inclusive range.
    /// </summary>
    /// <param name="address">The address to test.</param>
    /// <returns><c>true</c> for an IPv4 address in the range; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the address is null.</exception>
    internal bool Contains(System.Net.IPAddress address)
    {
        Guard.NotNull(address, "address");
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }
        uint number = Ipv4Generator.Number(address);
        return number >= NetworkNumber && number <= LastNumber;
    }
}
