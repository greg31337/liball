namespace tiff;

/// <summary>Describes one TIFF tile and its image-space coordinates.</summary>
public class TiffTileSegment
{
    /// <summary>Contains the tile index.</summary>
    public ulong TileIndex;
    /// <summary>Contains the tile index within its sample plane.</summary>
    public ulong TileIndexInPlane;
    /// <summary>Contains the sample plane index for planar-separate data.</summary>
    public int SampleIndex;
    /// <summary>Contains the tile column index.</summary>
    public ulong TileColumn;
    /// <summary>Contains the tile row index.</summary>
    public ulong TileRow;
    /// <summary>Contains the tile depth index.</summary>
    public ulong TileDepthIndex;
    /// <summary>Contains the first represented image column.</summary>
    public ulong FirstColumn;
    /// <summary>Contains the first represented image row.</summary>
    public ulong FirstRow;
    /// <summary>Contains the first represented image depth coordinate.</summary>
    public ulong FirstDepth;
    /// <summary>Contains the represented image width.</summary>
    public ulong Width;
    /// <summary>Contains the represented image length.</summary>
    public ulong Length;
    /// <summary>Contains the represented image depth.</summary>
    public ulong Depth;
    /// <summary>Contains the expected decoded padded-tile byte count.</summary>
    public ulong DecodedByteCount;
    /// <summary>Contains the useful image byte count excluding padded edges.</summary>
    public ulong ImageByteCount;
    /// <summary>Contains the encoded byte count stored in the file.</summary>
    public ulong StoredByteCount;
    /// <summary>Contains the encoded byte range in the file.</summary>
    public TiffByteRange ByteRange;
    /// <summary>Indicates whether the tile intersects an image edge.</summary>
    public bool IsPartial;
    /// <summary>Indicates whether the tile has no stored payload.</summary>
    public bool IsSparse;

    /// <summary>Creates an empty tile-segment description.</summary>
    public TiffTileSegment()
    {
        TileIndex = 0;
        TileIndexInPlane = 0;
        SampleIndex = 0;
        TileColumn = 0;
        TileRow = 0;
        TileDepthIndex = 0;
        FirstColumn = 0;
        FirstRow = 0;
        FirstDepth = 0;
        Width = 0;
        Length = 0;
        Depth = 0;
        DecodedByteCount = 0;
        ImageByteCount = 0;
        StoredByteCount = 0;
        ByteRange = new TiffByteRange();
        IsPartial = false;
        IsSparse = true;
    }

    internal TiffTileSegment Copy()
    {
        TiffTileSegment copy = new TiffTileSegment();
        copy.TileIndex = TileIndex;
        copy.TileIndexInPlane = TileIndexInPlane;
        copy.SampleIndex = SampleIndex;
        copy.TileColumn = TileColumn;
        copy.TileRow = TileRow;
        copy.TileDepthIndex = TileDepthIndex;
        copy.FirstColumn = FirstColumn;
        copy.FirstRow = FirstRow;
        copy.FirstDepth = FirstDepth;
        copy.Width = Width;
        copy.Length = Length;
        copy.Depth = Depth;
        copy.DecodedByteCount = DecodedByteCount;
        copy.ImageByteCount = ImageByteCount;
        copy.StoredByteCount = StoredByteCount;
        copy.ByteRange = ByteRange.Copy();
        copy.IsPartial = IsPartial;
        copy.IsSparse = IsSparse;
        return copy;
    }
}
