namespace tiff;

/// <summary>Describes the normalized tile organization of a TIFF image.</summary>
public class TiffTileLayout
{
    /// <summary>Identifies the zero-based image directory.</summary>
    public int ImageIndex;
    /// <summary>Contains the planar configuration.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the number of tiles across the image.</summary>
    public ulong TilesAcross;
    /// <summary>Contains the number of tiles down the image.</summary>
    public ulong TilesDown;
    /// <summary>Contains the number of tiles through the image depth.</summary>
    public ulong TilesDeep;
    /// <summary>Contains the number of tiles in each sample plane.</summary>
    public ulong TilesPerPlane;
    /// <summary>Contains the total tile count.</summary>
    public ulong TileCount;
    /// <summary>Indicates whether every stored segment occupies one adjacent file range in layout order.</summary>
    public bool IsDataContiguous;
    /// <summary>Contains the tile descriptions in layout order.</summary>
    public TiffTileSegment[] Segments;

    /// <summary>Creates an empty tile layout.</summary>
    public TiffTileLayout()
    {
        ImageIndex = TiffDiagnostic.NoImageIndex;
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        TilesAcross = 0;
        TilesDown = 0;
        TilesDeep = 0;
        TilesPerPlane = 0;
        TileCount = 0;
        IsDataContiguous = false;
        Segments = new TiffTileSegment[0];
    }

    internal TiffTileLayout Copy()
    {
        TiffTileLayout copy = new TiffTileLayout();
        copy.ImageIndex = ImageIndex;
        copy.PlanarConfiguration = PlanarConfiguration;
        copy.TilesAcross = TilesAcross;
        copy.TilesDown = TilesDown;
        copy.TilesDeep = TilesDeep;
        copy.TilesPerPlane = TilesPerPlane;
        copy.TileCount = TileCount;
        copy.IsDataContiguous = IsDataContiguous;
        copy.Segments = CopySegments(Segments);
        return copy;
    }

    internal static TiffTileSegment[] CopySegments(TiffTileSegment[] values)
    {
        if (values == null)
        {
            return new TiffTileSegment[0];
        }

        TiffTileSegment[] copy = new TiffTileSegment[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
