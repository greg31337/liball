namespace avif;

/// <summary>Describes one contiguous source-byte range.</summary>
public class AvifByteRange : IAvifByteRange
{
    ulong offset;
    ulong length;

    internal AvifByteRange(ulong offset, ulong length)
    {
        this.offset = offset;
        this.length = length;
    }

    /// <summary>Returns the absolute source offset.</summary>
    /// <returns>The byte offset.</returns>
    public ulong GetOffset()
    {
        return offset;
    }

    /// <summary>Returns the range length.</summary>
    /// <returns>The byte count.</returns>
    public ulong GetLength()
    {
        return length;
    }

    /// <summary>Returns the exclusive end offset.</summary>
    /// <returns>The offset immediately following the range.</returns>
    public ulong GetEndOffset()
    {
        return offset + length;
    }
}
