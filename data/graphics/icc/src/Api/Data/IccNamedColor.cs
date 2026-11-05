namespace icc;

/// <summary>Represents one named colour and optional tint/device coordinates.</summary>
public class IccNamedColor
{
    /// <summary>Gets or sets the root colour name.</summary>
    public string Name;
    /// <summary>Gets or sets PCS coordinates.</summary>
    public IccColorVector PcsColor;
    /// <summary>Gets or sets device coordinates.</summary>
    public IccColorVector DeviceColor;
    /// <summary>Gets or sets tint values associated with the colour.</summary>
    public double[] Tints;
    /// <summary>Gets or sets a human-oriented colour description.</summary>
    public IccColorDescription Description;

    /// <summary>Initializes an unnamed colour.</summary>
    public IccNamedColor()
    {
        Name = string.Empty;
        PcsColor = new IccColorVector();
        DeviceColor = new IccColorVector();
        Tints = new double[0];
        Description = new IccColorDescription();
    }
}
