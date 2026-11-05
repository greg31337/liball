namespace tiff;

/// <summary>Contains the values and diagnostics produced by a raw strip write operation.</summary>
public class TiffRawStripWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the strip index.</summary>
    public ulong StripIndex;
    /// <summary>Contains the range.</summary>
    public TiffByteRange Range;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Raw Strip Write Result class.</summary>
    public TiffRawStripWriteResult()
    {
        Success = false;
        StripIndex = 0;
        Range = new TiffByteRange();
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Raw Strip Write Result class.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="range">The range.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffRawStripWriteResult Create(ulong stripIndex, TiffByteRange range, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffRawStripWriteResult value = new TiffRawStripWriteResult();
        value.Initialize(stripIndex, range, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong stripIndex, TiffByteRange range, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        StripIndex = stripIndex;
        Range = range.Copy();
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffRawStripWriteResult Copy()
    {
        TiffRawStripWriteResult copy = TiffRawStripWriteResult.Create(StripIndex, Range, BytesWritten, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
