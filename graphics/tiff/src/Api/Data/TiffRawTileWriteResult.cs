namespace tiff;

/// <summary>Contains the values and diagnostics produced by a raw tile write operation.</summary>
public class TiffRawTileWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the tile index.</summary>
    public ulong TileIndex;
    /// <summary>Contains the range.</summary>
    public TiffByteRange Range;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Raw Tile Write Result class.</summary>
    public TiffRawTileWriteResult()
    {
        Success = false;
        TileIndex = 0;
        Range = new TiffByteRange();
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Raw Tile Write Result class.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="range">The range.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffRawTileWriteResult Create(ulong tileIndex, TiffByteRange range, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffRawTileWriteResult value = new TiffRawTileWriteResult();
        value.Initialize(tileIndex, range, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong tileIndex, TiffByteRange range, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TileIndex = tileIndex;
        Range = range.Copy();
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffRawTileWriteResult Copy()
    {
        TiffRawTileWriteResult copy = TiffRawTileWriteResult.Create(TileIndex, Range, BytesWritten, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
