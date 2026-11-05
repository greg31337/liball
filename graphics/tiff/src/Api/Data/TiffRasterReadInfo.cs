namespace tiff;

/// <summary>Describes the exact sample and caller-buffer layout of a source raster or sample-plane read.</summary>
public class TiffRasterReadInfo
{
    /// <summary>Indicates whether the source layout can be represented by the requested read.</summary>
    public bool Success;
    /// <summary>Contains the source raster width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the source raster height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the selected sample index, or negative one for a complete raster.</summary>
    public int SampleIndex;
    /// <summary>Contains the number of samples written for each output pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>Contains the exact source bit width of each output sample.</summary>
    public int[] BitsPerSample;
    /// <summary>Contains the format of each output sample.</summary>
    public TiffSampleFormat[] SampleFormats;
    /// <summary>Contains the source photometric interpretation without applying a color conversion.</summary>
    public TiffPhotometric Photometric;
    /// <summary>Contains the planar configuration used by the stored source segments.</summary>
    public TiffPlanarConfiguration SourcePlanarConfiguration;
    /// <summary>Contains the contiguous planar configuration used by the output buffer.</summary>
    public TiffPlanarConfiguration PlanarConfiguration;
    /// <summary>Contains the source orientation metadata; source raster reads do not apply this transform.</summary>
    public TiffOrientation Orientation;
    /// <summary>Indicates whether one or more output samples are packed at a non-byte-aligned bit width.</summary>
    public bool IsPacked;
    /// <summary>Contains the meaningful output bits for each pixel.</summary>
    public ulong BitsPerPixel;
    /// <summary>Contains the meaningful output bits for each row before byte padding.</summary>
    public ulong RowBitCount;
    /// <summary>Contains the byte-padded output size of each row.</summary>
    public ulong RowByteCount;
    /// <summary>Contains the total caller-buffer byte count.</summary>
    public ulong TotalByteCount;
    /// <summary>Contains the byte order of byte-aligned multibyte output samples, or <c>Unknown</c> for packed output.</summary>
    public TiffByteOrder ByteOrder;
    /// <summary>Contains validation diagnostics.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty unsuccessful raster-read description.</summary>
    public TiffRasterReadInfo()
    {
        Success = false;
        Width = 0;
        Length = 0;
        SampleIndex = -1;
        SamplesPerPixel = 0;
        BitsPerSample = new int[0];
        SampleFormats = new TiffSampleFormat[0];
        Photometric = TiffPhotometric.WhiteIsZero;
        SourcePlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        Orientation = TiffOrientation.TopLeft;
        IsPacked = false;
        BitsPerPixel = 0;
        RowBitCount = 0;
        RowByteCount = 0;
        TotalByteCount = 0;
        ByteOrder = TiffByteOrder.Unknown;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRasterReadInfo Create(TiffImageInfo info, int sampleIndex, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats, bool isPacked, ulong bitsPerPixel, ulong rowBitCount, ulong rowByteCount, ulong totalByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        TiffRasterReadInfo value = new TiffRasterReadInfo();
        value.Initialize(info, sampleIndex, samplesPerPixel, bitsPerSample, sampleFormats, isPacked, bitsPerPixel, rowBitCount, rowByteCount, totalByteCount, byteOrder, diagnostics);
        return value;
    }

    internal void Initialize(TiffImageInfo info, int sampleIndex, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats, bool isPacked, ulong bitsPerPixel, ulong rowBitCount, ulong rowByteCount, ulong totalByteCount, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (info != null)
        {
            Width = info.Width;
            Length = info.Length;
            Photometric = info.Photometric;
            SourcePlanarConfiguration = info.PlanarConfiguration;
            Orientation = info.Orientation;
        }

        SampleIndex = sampleIndex;
        SamplesPerPixel = samplesPerPixel;
        BitsPerSample = TiffImageInfo.CopyIntArray(bitsPerSample);
        SampleFormats = TiffImageInfo.CopySampleFormats(sampleFormats);
        PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        IsPacked = isPacked;
        BitsPerPixel = bitsPerPixel;
        RowBitCount = rowBitCount;
        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        ByteOrder = byteOrder;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffRasterReadInfo Copy()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = Width;
        info.Length = Length;
        info.Photometric = Photometric;
        info.PlanarConfiguration = SourcePlanarConfiguration;
        info.Orientation = Orientation;
        TiffRasterReadInfo copy = TiffRasterReadInfo.Create(info, SampleIndex, SamplesPerPixel, BitsPerSample, SampleFormats, IsPacked, BitsPerPixel, RowBitCount, RowByteCount, TotalByteCount, ByteOrder, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
