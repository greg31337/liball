namespace tiff;

/// <summary>Represents series plane used by the public TIFF API.</summary>
public class TiffSeriesPlane
{
    /// <summary>Contains the series index.</summary>
    public int SeriesIndex;
    /// <summary>Contains the plane index.</summary>
    public int PlaneIndex;
    /// <summary>Contains the zero-based channel coordinate.</summary>
    public int ChannelIndex;
    /// <summary>Contains the zero-based depth coordinate.</summary>
    public int ZIndex;
    /// <summary>Contains the zero-based time coordinate.</summary>
    public int TimeIndex;
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Contains the directory.</summary>
    public TiffDirectory Directory;

    /// <summary>Creates a new instance of the TIFF Series Plane class.</summary>
    public TiffSeriesPlane()
    {
        SeriesIndex = 0;
        PlaneIndex = 0;
        ChannelIndex = 0;
        ZIndex = 0;
        TimeIndex = 0;
        ImageIndex = 0;
        Directory = new TiffDirectory();
    }

    /// <summary>Creates a new instance of the TIFF Series Plane class.</summary>
    /// <param name="seriesIndex">The series index.</param>
    /// <param name="planeIndex">The plane index.</param>
    /// <param name="channelIndex">The zero-based channel coordinate.</param>
    /// <param name="zIndex">The zero-based depth coordinate.</param>
    /// <param name="timeIndex">The zero-based time coordinate.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    internal static TiffSeriesPlane Create(int seriesIndex, int planeIndex, int channelIndex, int zIndex, int timeIndex, int imageIndex, TiffDirectory directory)
    {
        TiffSeriesPlane value = new TiffSeriesPlane();
        value.Initialize(seriesIndex, planeIndex, channelIndex, zIndex, timeIndex, imageIndex, directory);
        return value;
    }

    internal void Initialize(int seriesIndex, int planeIndex, int channelIndex, int zIndex, int timeIndex, int imageIndex, TiffDirectory directory)
    {
        SeriesIndex = seriesIndex;
        PlaneIndex = planeIndex;
        ChannelIndex = channelIndex;
        ZIndex = zIndex;
        TimeIndex = timeIndex;
        ImageIndex = imageIndex;
        if (directory == null)
        {
            Directory = new TiffDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF series plane.</returns>
    internal TiffSeriesPlane Copy()
    {
        return TiffSeriesPlane.Create(SeriesIndex, PlaneIndex, ChannelIndex, ZIndex, TimeIndex, ImageIndex, Directory);
    }
}
