namespace image;

/// <summary>Identifies a recognized metadata byte range in the source.</summary>
/// <remarks>The range can be reported without copying bytes. <see cref="Data"/> is populated only when raw payload capture is enabled and allowed by limits.</remarks>
public class MetadataRawBlock
{
    /// <summary>The metadata family associated with the block.</summary>
    public MetadataFamily Family;
    /// <summary>The container-specific block, marker, or chunk type.</summary>
    public string BlockType;
    /// <summary>The human-readable block name.</summary>
    public string Name;
    /// <summary>The zero-based byte offset from the beginning of the reader's source range.</summary>
    public long Offset;
    /// <summary>The source block length in bytes.</summary>
    public long Length;
    /// <summary>The copied block payload, or an empty array when payload capture is disabled or limited.</summary>
    public byte[] Data;

    /// <summary>Creates an unknown block with no source location and an empty payload.</summary>
    public MetadataRawBlock()
    {
        Family = MetadataFamily.Unknown;
        BlockType = "";
        Name = "";
        Offset = -1;
        Length = 0;
        Data = new byte[0];
    }
}
