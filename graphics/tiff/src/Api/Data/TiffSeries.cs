namespace tiff;

/// <summary>Represents series used by the public TIFF API.</summary>
public class TiffSeries
{
    /// <summary>Contains the series index.</summary>
    public int SeriesIndex;
    /// <summary>Indicates whether the multidimensional metadata is present.</summary>
    public bool HasMultidimensionalMetadata;
    /// <summary>Contains the source metadata.</summary>
    public string SourceMetadata;
    /// <summary>Contains the size X.</summary>
    public ulong SizeX;
    /// <summary>Contains the size Y.</summary>
    public ulong SizeY;
    /// <summary>Contains the size C.</summary>
    public int SizeC;
    /// <summary>Contains the size Z.</summary>
    public int SizeZ;
    /// <summary>Contains the size T.</summary>
    public int SizeT;
    /// <summary>Contains the planes.</summary>
    public TiffSeriesPlane[] Planes;

    /// <summary>Creates a new instance of the TIFF Series class.</summary>
    public TiffSeries()
    {
        SeriesIndex = 0;
        HasMultidimensionalMetadata = false;
        SourceMetadata = "";
        SizeX = 0;
        SizeY = 0;
        SizeC = 0;
        SizeZ = 0;
        SizeT = 0;
        Planes = new TiffSeriesPlane[0];
    }

    /// <summary>Creates a new instance of the TIFF Series class.</summary>
    /// <param name="seriesIndex">The series index.</param>
    /// <param name="hasMultidimensionalMetadata">The has multidimensional metadata.</param>
    /// <param name="sourceMetadata">The source metadata.</param>
    /// <param name="sizeX">The size X.</param>
    /// <param name="sizeY">The size Y.</param>
    /// <param name="sizeC">The size C.</param>
    /// <param name="sizeZ">The size Z.</param>
    /// <param name="sizeT">The size T.</param>
    /// <param name="planes">The planes.</param>
    internal static TiffSeries Create(int seriesIndex, bool hasMultidimensionalMetadata, string sourceMetadata, ulong sizeX, ulong sizeY, int sizeC, int sizeZ, int sizeT, TiffSeriesPlane[] planes)
    {
        TiffSeries value = new TiffSeries();
        value.Initialize(seriesIndex, hasMultidimensionalMetadata, sourceMetadata, sizeX, sizeY, sizeC, sizeZ, sizeT, planes);
        return value;
    }

    internal void Initialize(int seriesIndex, bool hasMultidimensionalMetadata, string sourceMetadata, ulong sizeX, ulong sizeY, int sizeC, int sizeZ, int sizeT, TiffSeriesPlane[] planes)
    {
        SeriesIndex = seriesIndex;
        HasMultidimensionalMetadata = hasMultidimensionalMetadata;
        SourceMetadata = NormalizeString(sourceMetadata);
        SizeX = sizeX;
        SizeY = sizeY;
        SizeC = sizeC;
        SizeZ = sizeZ;
        SizeT = sizeT;
        Planes = CopyPlanes(planes);
    }

    /// <summary>Returns the number of available plane entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int PlaneCount()
    {
        return Planes.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF series.</returns>
    internal TiffSeries Copy()
    {
        return TiffSeries.Create(SeriesIndex, HasMultidimensionalMetadata, SourceMetadata, SizeX, SizeY, SizeC, SizeZ, SizeT, Planes);
    }

    internal static TiffSeriesPlane[] CopyPlanes(TiffSeriesPlane[] planes)
    {
        if (planes == null)
        {
            return new TiffSeriesPlane[0];
        }

        TiffSeriesPlane[] copy = new TiffSeriesPlane[planes.Length];
        int index = 0;
        while (index < planes.Length)
        {
            if (planes[index] == null)
            {
                copy[index] = new TiffSeriesPlane();
            }
            else
            {
                copy[index] = planes[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static string NormalizeString(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
