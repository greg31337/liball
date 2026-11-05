namespace icc;

/// <summary>Represents an indexed gamut-boundary mesh.</summary>
public class IccGamutBoundary
{
    /// <summary>Gets or sets the PCS channel count.</summary>
    public int PcsChannels;
    /// <summary>Gets or sets the device channel count.</summary>
    public int DeviceChannels;
    /// <summary>Gets or sets mesh vertices.</summary>
    public IccGamutVertex[] Vertices;
    /// <summary>Gets or sets mesh triangles.</summary>
    public IccGamutTriangle[] Triangles;

    /// <summary>Initializes an empty mesh.</summary>
    public IccGamutBoundary()
    {
        PcsChannels = 0;
        DeviceChannels = 0;
        Vertices = new IccGamutVertex[0];
        Triangles = new IccGamutTriangle[0];
    }
}
