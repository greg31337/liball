namespace tiff;

/// <summary>Represents photoshop metadata extracted from or prepared for a TIFF file.</summary>
public class TiffPhotoshopMetadata
{
    /// <summary>Indicates whether the photoshop is present.</summary>
    public bool HasPhotoshop;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;
    /// <summary>Contains the blocks.</summary>
    public TiffPhotoshopImageResourceBlock[] Blocks;

    /// <summary>Creates a new instance of the TIFF Photoshop Metadata class.</summary>
    public TiffPhotoshopMetadata()
    {
        HasPhotoshop = false;
        RawBytes = new byte[0];
        Blocks = new TiffPhotoshopImageResourceBlock[0];
    }

    /// <summary>Creates a new instance of the TIFF Photoshop Metadata class.</summary>
    /// <param name="hasPhotoshop">The has photoshop.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <param name="blocks">The blocks.</param>
    internal static TiffPhotoshopMetadata Create(bool hasPhotoshop, byte[] rawBytes, TiffPhotoshopImageResourceBlock[] blocks)
    {
        TiffPhotoshopMetadata value = new TiffPhotoshopMetadata();
        value.Initialize(hasPhotoshop, rawBytes, blocks);
        return value;
    }

    internal void Initialize(bool hasPhotoshop, byte[] rawBytes, TiffPhotoshopImageResourceBlock[] blocks)
    {
        HasPhotoshop = hasPhotoshop;
        RawBytes = TiffTagValue.CopyByteArray(rawBytes);
        Blocks = CopyBlocks(blocks);
    }

    /// <summary>Returns the number of available block entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int BlockCount()
    {
        return Blocks.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF photoshop metadata.</returns>
    internal TiffPhotoshopMetadata Copy()
    {
        return TiffPhotoshopMetadata.Create(HasPhotoshop, RawBytes, Blocks);
    }

    internal static TiffPhotoshopImageResourceBlock[] CopyBlocks(TiffPhotoshopImageResourceBlock[] blocks)
    {
        if (blocks == null)
        {
            return new TiffPhotoshopImageResourceBlock[0];
        }

        TiffPhotoshopImageResourceBlock[] copy = new TiffPhotoshopImageResourceBlock[blocks.Length];
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index] == null)
            {
                copy[index] = new TiffPhotoshopImageResourceBlock();
            }
            else
            {
                copy[index] = blocks[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
