namespace icc;

/// <summary>Collects equivalent human-oriented descriptions of one colour.</summary>
public class IccColorDescription
{
    /// <summary>Gets or sets the display name.</summary>
    public string Name;
    /// <summary>Gets or sets the XYZ representation when available.</summary>
    public IccXyzColor? Xyz;
    /// <summary>Gets or sets the Lab representation when available.</summary>
    public IccLabColor? Lab;
    /// <summary>Gets or sets the xyY representation when available.</summary>
    public IccXyYColor? XyY;
    /// <summary>Gets or sets the LCH representation when available.</summary>
    public IccLchColor? Lch;
    /// <summary>Gets or sets device-space channel values when available.</summary>
    public IccColorVector? DeviceColor;

    /// <summary>Initializes an unnamed colour description.</summary>
    public IccColorDescription()
    {
        Name = string.Empty;
        Xyz = null;
        Lab = null;
        XyY = null;
        Lch = null;
        DeviceColor = null;
    }
}
