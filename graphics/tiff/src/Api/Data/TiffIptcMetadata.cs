namespace tiff;

/// <summary>Represents IPTC metadata extracted from or prepared for a TIFF file.</summary>
public class TiffIptcMetadata
{
    /// <summary>Indicates whether the IPTC is present.</summary>
    public bool HasIptc;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;
    /// <summary>Contains the blocks.</summary>
    public TiffIptcBlock[] Blocks;

    /// <summary>Creates a new instance of the TIFF IPTC Metadata class.</summary>
    public TiffIptcMetadata()
    {
        HasIptc = false;
        RawBytes = new byte[0];
        Blocks = new TiffIptcBlock[0];
    }

    /// <summary>Creates a new instance of the TIFF IPTC Metadata class.</summary>
    /// <param name="hasIptc">The has IPTC.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <param name="blocks">The blocks.</param>
    internal static TiffIptcMetadata Create(bool hasIptc, byte[] rawBytes, TiffIptcBlock[] blocks)
    {
        TiffIptcMetadata value = new TiffIptcMetadata();
        value.Initialize(hasIptc, rawBytes, blocks);
        return value;
    }

    internal void Initialize(bool hasIptc, byte[] rawBytes, TiffIptcBlock[] blocks)
    {
        HasIptc = hasIptc;
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
    /// <returns>The resulting TIFF IPTC metadata.</returns>
    internal TiffIptcMetadata Copy()
    {
        return TiffIptcMetadata.Create(HasIptc, RawBytes, Blocks);
    }

    internal static TiffIptcBlock[] CopyBlocks(TiffIptcBlock[] blocks)
    {
        if (blocks == null)
        {
            return new TiffIptcBlock[0];
        }

        TiffIptcBlock[] copy = new TiffIptcBlock[blocks.Length];
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index] == null)
            {
                copy[index] = new TiffIptcBlock();
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
