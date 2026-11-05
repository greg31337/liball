namespace fake;

/// <summary>
/// Represents a synthetic network interface and its correlated MAC address metadata.
/// </summary>
/// <remarks>
/// Universal administration describes only the encoded U/L bit. It does not claim that
/// an organizational prefix was assigned by a registry or that the address belongs to a vendor.
/// </remarks>
public class NetworkInterfaceRecord
{
    /// <summary>A synthetic interface name correlated with the interface type.</summary>
    public string Name = string.Empty;

    /// <summary>The broad wired or wireless interface type.</summary>
    public System.Net.NetworkInformation.NetworkInterfaceType InterfaceType;

    /// <summary>The lowercase, colon-separated 48-bit MAC address.</summary>
    public string MacAddress = string.Empty;

    /// <summary>The same MAC address as a platform physical-address object.</summary>
    public System.Net.NetworkInformation.PhysicalAddress PhysicalAddress =
        new System.Net.NetworkInformation.PhysicalAddress(new byte[0]);

    /// <summary>Whether the individual/group bit identifies an individual unicast address.</summary>
    public bool IsUnicast;

    /// <summary>Whether the individual/group bit identifies a multicast group address.</summary>
    public bool IsMulticast;

    /// <summary>Whether the universal/local bit is set for local administration.</summary>
    public bool IsLocallyAdministered;

    /// <summary>Whether the universal/local bit is clear.</summary>
    public bool IsUniversallyAdministered;

    internal NetworkInterfaceRecord()
    {
    }
}
