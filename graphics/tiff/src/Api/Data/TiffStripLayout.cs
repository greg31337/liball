namespace tiff;

/// <summary>Describes the normalized strip organization of a TIFF image.</summary>
public class TiffStripLayout
{
    /// <summary>Identifies the zero-based image directory.</summary>
    public int ImageIndex;
    /// <summary>Contains the planar configuration.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the number of strips in each sample plane.</summary>
    public ulong StripsPerPlane;
    /// <summary>Contains the number of physical source strips.</summary>
    public ulong SourceStripCount;
    /// <summary>Contains the number of strips in this layout.</summary>
    public ulong StripCount;
    /// <summary>Indicates whether every stored segment occupies one adjacent file range in layout order.</summary>
    public bool IsDataContiguous;
    /// <summary>Indicates whether this is a virtual chopped-strip view.</summary>
    public bool IsChopped;
    /// <summary>Contains the strip descriptions in layout order.</summary>
    public TiffStripSegment[] Segments;

    /// <summary>Creates an empty strip layout.</summary>
    public TiffStripLayout()
    {
        ImageIndex = TiffDiagnostic.NoImageIndex;
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        StripsPerPlane = 0;
        SourceStripCount = 0;
        StripCount = 0;
        IsDataContiguous = false;
        IsChopped = false;
        Segments = new TiffStripSegment[0];
    }

    internal TiffStripLayout Copy()
    {
        TiffStripLayout copy = new TiffStripLayout();
        copy.ImageIndex = ImageIndex;
        copy.PlanarConfiguration = PlanarConfiguration;
        copy.StripsPerPlane = StripsPerPlane;
        copy.SourceStripCount = SourceStripCount;
        copy.StripCount = StripCount;
        copy.IsDataContiguous = IsDataContiguous;
        copy.IsChopped = IsChopped;
        copy.Segments = CopySegments(Segments);
        return copy;
    }

    internal static TiffStripSegment[] CopySegments(TiffStripSegment[] values)
    {
        if (values == null)
        {
            return new TiffStripSegment[0];
        }

        TiffStripSegment[] copy = new TiffStripSegment[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
