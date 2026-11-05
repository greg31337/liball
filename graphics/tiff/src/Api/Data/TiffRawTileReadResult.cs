namespace tiff;

/// <summary>Contains the values and diagnostics produced by a raw tile read operation.</summary>
public class TiffRawTileReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the tile index.</summary>
    public ulong TileIndex;
    /// <summary>Contains the range.</summary>
    public TiffByteRange Range;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;
    /// <summary>Indicates whether sparse applies.</summary>
    public bool IsSparse;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Raw Tile Read Result class.</summary>
    public TiffRawTileReadResult()
    {
        Success = false;
        TileIndex = 0;
        Range = new TiffByteRange();
        Bytes = new byte[0];
        IsSparse = true;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Raw Tile Read Result class.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="range">The range.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffRawTileReadResult Create(ulong tileIndex, TiffByteRange range, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffRawTileReadResult value = new TiffRawTileReadResult();
        value.Initialize(tileIndex, range, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong tileIndex, TiffByteRange range, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TileIndex = tileIndex;
        Range = range.Copy();
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        IsSparse = range.IsSparse;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffRawTileReadResult Copy()
    {
        TiffRawTileReadResult copy = TiffRawTileReadResult.Create(TileIndex, Range, Bytes, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
