namespace tiff;

/// <summary>Contains the values and diagnostics produced by a source raster write operation.</summary>
public class TiffSourceRasterWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the segment count.</summary>
    public ulong SegmentCount;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the compression.</summary>
    public TiffCompression Compression;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Source Raster Write Result class.</summary>
    public TiffSourceRasterWriteResult()
    {
        Success = false;
        SegmentCount = 0;
        BytesWritten = 0;
        Compression = TiffCompression.Unknown;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Source Raster Write Result class.</summary>
    /// <param name="segmentCount">The segment count.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="compression">The compression.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffSourceRasterWriteResult Create(ulong segmentCount, ulong bytesWritten, TiffCompression compression, TiffDiagnostics diagnostics)
    {
        TiffSourceRasterWriteResult value = new TiffSourceRasterWriteResult();
        value.Initialize(segmentCount, bytesWritten, compression, diagnostics);
        return value;
    }

    internal void Initialize(ulong segmentCount, ulong bytesWritten, TiffCompression compression, TiffDiagnostics diagnostics)
    {
        SegmentCount = segmentCount;
        BytesWritten = bytesWritten;
        Compression = compression;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffSourceRasterWriteResult Copy()
    {
        TiffSourceRasterWriteResult copy = TiffSourceRasterWriteResult.Create(SegmentCount, BytesWritten, Compression, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
