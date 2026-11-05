namespace tiff;

/// <summary>Describes the caller buffer required by a rectangular region read.</summary>
public class TiffRegionReadInfo
{
    /// <summary>Indicates whether the region and requested output layout are supported.</summary>
    public bool Success;
    /// <summary>Contains the validated source-image rectangle.</summary>
    public TiffRegion Region;
    /// <summary>Contains the requested output sample layout.</summary>
    public TiffRegionOutput Output;
    /// <summary>Contains the selected source sample index for source-layout plane reads.</summary>
    public int SampleIndex;
    /// <summary>Contains the number of samples written for each output pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>Contains the bits per output sample.</summary>
    public int[] BitsPerSample;
    /// <summary>Contains the output sample formats.</summary>
    public TiffSampleFormat[] SampleFormats;
    /// <summary>Contains the output planar configuration.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the number of bytes written for each output row.</summary>
    public ulong RowByteCount;
    /// <summary>Contains the total number of bytes required from the destination buffer.</summary>
    public ulong TotalByteCount;
    /// <summary>Contains validation diagnostics.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty unsuccessful region-read description.</summary>
    public TiffRegionReadInfo()
    {
        Success = false;
        Region = new TiffRegion();
        Output = TiffRegionOutput.Source;
        SampleIndex = 0;
        SamplesPerPixel = 0;
        BitsPerSample = new int[0];
        SampleFormats = new TiffSampleFormat[0];
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        RowByteCount = 0;
        TotalByteCount = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRegionReadInfo Create(TiffRegion region, TiffRegionOutput output, int sampleIndex, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats, TiffPlanarConfiguration planarConfiguration, ulong rowByteCount, ulong totalByteCount, TiffDiagnostics diagnostics)
    {
        TiffRegionReadInfo value = new TiffRegionReadInfo();
        value.Initialize(region, output, sampleIndex, samplesPerPixel, bitsPerSample, sampleFormats, planarConfiguration, rowByteCount, totalByteCount, diagnostics);
        return value;
    }

    internal void Initialize(TiffRegion region, TiffRegionOutput output, int sampleIndex, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats, TiffPlanarConfiguration planarConfiguration, ulong rowByteCount, ulong totalByteCount, TiffDiagnostics diagnostics)
    {
        Region = new TiffRegion();
        if (region != null)
        {
            Region = region.Copy();
        }

        Output = output;
        SampleIndex = sampleIndex;
        SamplesPerPixel = samplesPerPixel;
        BitsPerSample = TiffImageInfo.CopyIntArray(bitsPerSample);
        SampleFormats = TiffImageInfo.CopySampleFormats(sampleFormats);
        PlanarConfiguration = planarConfiguration;
        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffRegionReadInfo Copy()
    {
        TiffRegionReadInfo copy = TiffRegionReadInfo.Create(Region, Output, SampleIndex, SamplesPerPixel, BitsPerSample, SampleFormats, PlanarConfiguration, RowByteCount, TotalByteCount, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
