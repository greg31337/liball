namespace tiff;

/// <summary>Controls the bounds, source sample, and output layout of a rectangular region read.</summary>
public class TiffRegionReadOptions
{
    /// <summary>Contains the source-image rectangle to read.</summary>
    public TiffRegion Region;
    /// <summary>Specifies the requested output sample layout.</summary>
    public TiffRegionOutput Output;
    /// <summary>Selects the source sample plane when <c>Output</c> is <c>Source</c> and the image is planar-separate.</summary>
    public int SampleIndex;

    /// <summary>Creates region read options with an empty region and source-layout output.</summary>
    public TiffRegionReadOptions()
    {
        Region = new TiffRegion();
        Output = TiffRegionOutput.Source;
        SampleIndex = 0;
    }

    internal TiffRegionReadOptions Copy()
    {
        TiffRegionReadOptions copy = new TiffRegionReadOptions();
        if (Region != null)
        {
            copy.Region = Region.Copy();
        }

        copy.Output = Output;
        copy.SampleIndex = SampleIndex;
        return copy;
    }
}
