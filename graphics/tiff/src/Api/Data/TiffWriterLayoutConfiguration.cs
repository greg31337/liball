namespace tiff;

/// <summary>Controls the physical strip or tile organization of a configured writer image.</summary>
public class TiffWriterLayoutConfiguration
{
    /// <summary>Selects strip or tile storage.</summary>
    public TiffWriterLayoutKind Kind;
    /// <summary>Selects contiguous or separate sample-plane organization.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the rows in each strip, or zero to use the complete image length.</summary>
    public int RowsPerStrip;
    /// <summary>Contains the stored tile width when tile storage is selected.</summary>
    public int TileWidth;
    /// <summary>Contains the stored tile length when tile storage is selected.</summary>
    public int TileLength;
    /// <summary>Contains the stored tile depth when tile storage is selected.</summary>
    public int TileDepth;
    /// <summary>Indicates whether unwritten segments are emitted as sparse zero-byte segments.</summary>
    public bool AllowSparseSegments;

    /// <summary>Creates a contiguous full-height strip layout.</summary>
    public TiffWriterLayoutConfiguration()
    {
        Kind = TiffWriterLayoutKind.Strips;
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        RowsPerStrip = 0;
        TileWidth = 0;
        TileLength = 0;
        TileDepth = 1;
        AllowSparseSegments = false;
    }
}
