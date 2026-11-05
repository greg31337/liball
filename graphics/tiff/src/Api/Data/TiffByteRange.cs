namespace tiff;

/// <summary>Represents byte range used by the public TIFF API.</summary>
public class TiffByteRange
{
    /// <summary>Contains the byte offset measured from the start of the TIFF file.</summary>
    public ulong Offset;
    /// <summary>Contains the byte count.</summary>
    public ulong ByteCount;
    /// <summary>Contains the end offset.</summary>
    public ulong EndOffset;
    /// <summary>Indicates whether sparse applies.</summary>
    public bool IsSparse;

    /// <summary>Creates a new instance of the TIFF Byte Range class.</summary>
    public TiffByteRange()
    {
        Offset = 0;
        ByteCount = 0;
        EndOffset = 0;
        IsSparse = true;
    }

    /// <summary>Creates a new instance of the TIFF Byte Range class.</summary>
    /// <param name="offset">The offset.</param>
    /// <param name="byteCount">The byte count.</param>
    /// <param name="endOffset">The end offset.</param>
    /// <param name="isSparse">Whether sparse applies.</param>
    internal static TiffByteRange Create(ulong offset, ulong byteCount, ulong endOffset, bool isSparse)
    {
        TiffByteRange value = new TiffByteRange();
        value.Initialize(offset, byteCount, endOffset, isSparse);
        return value;
    }

    internal void Initialize(ulong offset, ulong byteCount, ulong endOffset, bool isSparse)
    {
        Offset = offset;
        ByteCount = byteCount;
        EndOffset = endOffset;
        IsSparse = isSparse;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF byte range.</returns>
    internal TiffByteRange Copy()
    {
        return TiffByteRange.Create(Offset, ByteCount, EndOffset, IsSparse);
    }
}
