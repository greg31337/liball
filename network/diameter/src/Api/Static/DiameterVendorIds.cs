namespace diameter;

/// <summary>
/// Defines vendor identifiers used by built-in and common extension dictionaries.
/// </summary>
public static class DiameterVendorIds
{
    /// <summary>The value representing IETF-defined AVPs without a vendor header.</summary>
    public const uint Ietf = 0;

    /// <summary>The Cable Television Laboratories private enterprise number.</summary>
    public const uint CableLabs = 4491;

    /// <summary>The 3GPP2 private enterprise number.</summary>
    public const uint ThreeGpp2 = 5535;

    /// <summary>The 3GPP private enterprise number.</summary>
    public const uint ThreeGpp = 10415;

    /// <summary>The ETSI private enterprise number.</summary>
    public const uint Etsi = 13019;
}
