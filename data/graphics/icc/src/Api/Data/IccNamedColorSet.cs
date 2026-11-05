namespace icc;

/// <summary>Represents an ordered named-colour collection.</summary>
public class IccNamedColorSet
{
    /// <summary>Gets or sets the vendor-specific flag field.</summary>
    public uint VendorFlags;
    /// <summary>Gets or sets the common name prefix.</summary>
    public string Prefix;
    /// <summary>Gets or sets the common name suffix.</summary>
    public string Suffix;
    /// <summary>Gets or sets the number of device coordinates per colour.</summary>
    public int DeviceChannelCount;
    /// <summary>Gets or sets the optional ICC.2 zero-tint endpoint shared by the named colours.</summary>
    public IccNamedColor? TintZero;
    /// <summary>Gets or sets the ordered colours.</summary>
    public IccNamedColor[] Colors;

    /// <summary>Initializes an empty named-colour collection.</summary>
    public IccNamedColorSet()
    {
        VendorFlags = 0;
        Prefix = string.Empty;
        Suffix = string.Empty;
        DeviceChannelCount = 0;
        TintZero = null;
        Colors = new IccNamedColor[0];
    }
}
