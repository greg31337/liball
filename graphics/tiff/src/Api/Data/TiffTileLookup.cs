namespace tiff;

/// <summary>Represents tile lookup used by the public TIFF API.</summary>
public class TiffTileLookup
{
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Contains the pyramid level index.</summary>
    public int PyramidLevelIndex;
    /// <summary>Contains the tile index.</summary>
    public ulong TileIndex;
    /// <summary>Contains the tile index in plane.</summary>
    public ulong TileIndexInPlane;
    /// <summary>Contains the tile column.</summary>
    public ulong TileColumn;
    /// <summary>Contains the tile row.</summary>
    public ulong TileRow;
    /// <summary>Contains the tile depth index.</summary>
    public ulong TileDepthIndex;
    /// <summary>Contains the sample index.</summary>
    public int SampleIndex;
    /// <summary>Contains the first column.</summary>
    public ulong FirstColumn;
    /// <summary>Contains the first row.</summary>
    public ulong FirstRow;
    /// <summary>Contains the first depth coordinate.</summary>
    public ulong FirstDepth;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the image or region depth.</summary>
    public ulong Depth;
    /// <summary>Contains the storage byte count.</summary>
    public ulong StorageByteCount;
    /// <summary>Contains the image byte count.</summary>
    public ulong ImageByteCount;
    /// <summary>Contains the byte range.</summary>
    public TiffByteRange ByteRange;
    /// <summary>Indicates whether sparse applies.</summary>
    public bool IsSparse;

    /// <summary>Creates a new instance of the TIFF Tile Lookup class.</summary>
    public TiffTileLookup()
    {
        ImageIndex = TiffDiagnostic.NoImageIndex;
        PyramidLevelIndex = 0;
        TileIndex = 0;
        TileIndexInPlane = 0;
        TileColumn = 0;
        TileRow = 0;
        TileDepthIndex = 0;
        SampleIndex = 0;
        FirstColumn = 0;
        FirstRow = 0;
        FirstDepth = 0;
        Width = 0;
        Length = 0;
        Depth = 0;
        StorageByteCount = 0;
        ImageByteCount = 0;
        ByteRange = new TiffByteRange();
        IsSparse = true;
    }

    /// <summary>Creates a new instance of the TIFF Tile Lookup class.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="pyramidLevelIndex">The pyramid level index.</param>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="tileIndexInPlane">The tile index in plane.</param>
    /// <param name="tileColumn">The tile column.</param>
    /// <param name="tileRow">The tile row.</param>
    /// <param name="tileDepthIndex">The tile depth index.</param>
    /// <param name="sampleIndex">The zero-based sample plane or channel index.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="firstRow">The first row.</param>
    /// <param name="firstDepth">The first depth coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="storageByteCount">The storage byte count.</param>
    /// <param name="imageByteCount">The image byte count.</param>
    /// <param name="byteRange">The byte range.</param>
    internal static TiffTileLookup Create(int imageIndex, int pyramidLevelIndex, ulong tileIndex, ulong tileIndexInPlane, ulong tileColumn, ulong tileRow, ulong tileDepthIndex, int sampleIndex, ulong firstColumn, ulong firstRow, ulong firstDepth, ulong width, ulong length, ulong depth, ulong storageByteCount, ulong imageByteCount, TiffByteRange byteRange)
    {
        TiffTileLookup value = new TiffTileLookup();
        value.Initialize(imageIndex, pyramidLevelIndex, tileIndex, tileIndexInPlane, tileColumn, tileRow, tileDepthIndex, sampleIndex, firstColumn, firstRow, firstDepth, width, length, depth, storageByteCount, imageByteCount, byteRange);
        return value;
    }

    internal void Initialize(int imageIndex, int pyramidLevelIndex, ulong tileIndex, ulong tileIndexInPlane, ulong tileColumn, ulong tileRow, ulong tileDepthIndex, int sampleIndex, ulong firstColumn, ulong firstRow, ulong firstDepth, ulong width, ulong length, ulong depth, ulong storageByteCount, ulong imageByteCount, TiffByteRange byteRange)
    {
        ImageIndex = imageIndex;
        PyramidLevelIndex = pyramidLevelIndex;
        TileIndex = tileIndex;
        TileIndexInPlane = tileIndexInPlane;
        TileColumn = tileColumn;
        TileRow = tileRow;
        TileDepthIndex = tileDepthIndex;
        SampleIndex = sampleIndex;
        FirstColumn = firstColumn;
        FirstRow = firstRow;
        FirstDepth = firstDepth;
        Width = width;
        Length = length;
        Depth = depth;
        StorageByteCount = storageByteCount;
        ImageByteCount = imageByteCount;
        if (byteRange == null)
        {
            ByteRange = new TiffByteRange();
        }
        else
        {
            ByteRange = byteRange.Copy();
        }

        IsSparse = ByteRange.IsSparse;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF tile lookup.</returns>
    internal TiffTileLookup Copy()
    {
        return TiffTileLookup.Create(ImageIndex, PyramidLevelIndex, TileIndex, TileIndexInPlane, TileColumn, TileRow, TileDepthIndex, SampleIndex, FirstColumn, FirstRow, FirstDepth, Width, Length, Depth, StorageByteCount, ImageByteCount, ByteRange);
    }
}
