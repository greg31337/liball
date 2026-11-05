namespace icc;

/// <summary>Represents one gamut-boundary mesh vertex.</summary>
public class IccGamutVertex
{
    /// <summary>Gets or sets PCS coordinates.</summary>
    public IccColorVector PcsColor;
    /// <summary>Gets or sets optional device coordinates.</summary>
    public IccColorVector DeviceColor;

    /// <summary>Initializes a vertex with empty coordinates.</summary>
    public IccGamutVertex()
    {
        PcsColor = new IccColorVector();
        DeviceColor = new IccColorVector();
    }
}
