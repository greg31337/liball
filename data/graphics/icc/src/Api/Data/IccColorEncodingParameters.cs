namespace icc;

/// <summary>Represents ICC.2 colour-encoding-space parameters.</summary>
public class IccColorEncodingParameters
{
    /// <summary>Gets or sets the colour-encoding name.</summary>
    public string Name;
    /// <summary>Gets or sets channel encoding ranges.</summary>
    public IccEncodingRange[] ChannelRanges;
    /// <summary>Gets or sets the reference white.</summary>
    public IccXyzColor ReferenceWhite;
    /// <summary>Gets or sets encoding curves.</summary>
    public IccCurve[] Curves;
    /// <summary>Gets or sets an optional conversion matrix.</summary>
    public IccMatrix? Matrix;
    /// <summary>Gets or sets metadata entries.</summary>
    public IccMetadataEntry[] Metadata;

    /// <summary>Initializes unspecified colour-encoding parameters.</summary>
    public IccColorEncodingParameters()
    {
        Name = string.Empty;
        ChannelRanges = new IccEncodingRange[0];
        ReferenceWhite = new IccXyzColor();
        Curves = new IccCurve[0];
        Matrix = null;
        Metadata = new IccMetadataEntry[0];
    }
}
