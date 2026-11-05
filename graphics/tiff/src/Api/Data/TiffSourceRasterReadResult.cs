namespace tiff;

/// <summary>Contains the values and diagnostics produced by a source raster read operation.</summary>
public class TiffSourceRasterReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the samples per pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>Contains the bits per sample.</summary>
    public int[] BitsPerSample;
    /// <summary>Contains the sample formats.</summary>
    public TiffSampleFormat[] SampleFormats;
    /// <summary>Contains the photometric.</summary>
    public TiffPhotometric Photometric;
    /// <summary>Contains the row byte count.</summary>
    public ulong RowByteCount;
    /// <summary>Contains the total byte count.</summary>
    public ulong TotalByteCount;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Source Raster Read Result class.</summary>
    public TiffSourceRasterReadResult()
    {
        Success = false;
        Width = 0;
        Length = 0;
        SamplesPerPixel = 0;
        BitsPerSample = new int[0];
        SampleFormats = new TiffSampleFormat[0];
        Photometric = TiffPhotometric.WhiteIsZero;
        RowByteCount = 0;
        TotalByteCount = 0;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Source Raster Read Result class.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <param name="rowByteCount">The row byte count.</param>
    /// <param name="totalByteCount">The total byte count.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffSourceRasterReadResult Create(TiffImageInfo info, ulong width, ulong length, ulong rowByteCount, ulong totalByteCount, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffSourceRasterReadResult value = new TiffSourceRasterReadResult();
        value.Initialize(info, width, length, rowByteCount, totalByteCount, bytes, diagnostics);
        return value;
    }

    internal void Initialize(TiffImageInfo info, ulong width, ulong length, ulong rowByteCount, ulong totalByteCount, byte[] bytes, TiffDiagnostics diagnostics)
    {
        Width = width;
        Length = length;
        SamplesPerPixel = 0;
        BitsPerSample = new int[0];
        SampleFormats = new TiffSampleFormat[0];
        Photometric = TiffPhotometric.WhiteIsZero;
        if (info != null)
        {
            SamplesPerPixel = info.SamplesPerPixel;
            BitsPerSample = TiffImageInfo.CopyIntArray(info.BitsPerSample);
            SampleFormats = TiffImageInfo.CopySampleFormats(info.SampleFormats);
            Photometric = info.Photometric;
        }

        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffSourceRasterReadResult Copy()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.SamplesPerPixel = SamplesPerPixel;
        info.BitsPerSample = TiffImageInfo.CopyIntArray(BitsPerSample);
        info.SampleFormats = TiffImageInfo.CopySampleFormats(SampleFormats);
        info.Photometric = Photometric;
        TiffSourceRasterReadResult copy = TiffSourceRasterReadResult.Create(info, Width, Length, RowByteCount, TotalByteCount, Bytes, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
