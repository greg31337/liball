namespace tiff;

/// <summary>Contains the values and diagnostics produced by a EXIF write operation.</summary>
public class TiffExifWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Contains the EXIF offset.</summary>
    public ulong ExifOffset;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF EXIF Write Result class.</summary>
    public TiffExifWriteResult()
    {
        Success = false;
        ParentOffset = 0;
        ExifOffset = 0;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF EXIF Write Result class.</summary>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="exifOffset">The exif offset.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffExifWriteResult Create(ulong parentOffset, ulong exifOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffExifWriteResult value = new TiffExifWriteResult();
        value.Initialize(parentOffset, exifOffset, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong parentOffset, ulong exifOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        ParentOffset = parentOffset;
        ExifOffset = exifOffset;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffExifWriteResult Copy()
    {
        TiffExifWriteResult copy = TiffExifWriteResult.Create(ParentOffset, ExifOffset, BytesWritten, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
