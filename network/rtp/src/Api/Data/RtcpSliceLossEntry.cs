namespace rtp;

/// <summary>
/// Contains one RFC 4585 slice loss indication entry.
/// </summary>
public class RtcpSliceLossEntry
{
    /// <summary>The first lost macroblock in scan order.</summary>
    public int FirstMacroblock;

    /// <summary>The number of lost macroblocks.</summary>
    public int MacroblockCount;

    /// <summary>The six-bit picture identifier.</summary>
    public byte PictureIdentifier;

    internal RtcpSliceLossEntry Copy()
    {
        RtcpSliceLossEntry copy = new RtcpSliceLossEntry();
        copy.FirstMacroblock = FirstMacroblock;
        copy.MacroblockCount = MacroblockCount;
        copy.PictureIdentifier = PictureIdentifier;
        return copy;
    }
}
