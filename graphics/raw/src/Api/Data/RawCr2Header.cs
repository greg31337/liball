namespace raw;

/// <summary>Describes the Canon CR2 extension to a classic TIFF header.</summary>
public class RawCr2Header
{
    /// <summary>The byte order shared with the enclosing TIFF container.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>The CR2 major version.</summary>
    public int MajorVersion;
    /// <summary>The CR2 minor version.</summary>
    public int MinorVersion;
    /// <summary>The first TIFF IFD offset.</summary>
    public long FirstIfdOffset;
    /// <summary>The CR2 header's raw IFD offset.</summary>
    public long RawIfdOffset;
    /// <summary>The validated stable path of the raw IFD.</summary>
    public string RawIfdPath;

    /// <summary>Creates parsed CR2 header information.</summary>
    /// <param name="byteOrder">The TIFF byte order.</param>
    /// <param name="majorVersion">The CR2 major version.</param>
    /// <param name="minorVersion">The CR2 minor version.</param>
    /// <param name="firstIfdOffset">The first TIFF IFD offset.</param>
    /// <param name="rawIfdOffset">The raw IFD offset.</param>
    /// <param name="rawIfdPath">The validated raw IFD path.</param>
    public RawCr2Header(RawByteOrder byteOrder, int majorVersion, int minorVersion, long firstIfdOffset, long rawIfdOffset, string rawIfdPath)
    {
        ByteOrder = byteOrder;
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        FirstIfdOffset = firstIfdOffset;
        RawIfdOffset = rawIfdOffset;
        RawIfdPath = rawIfdPath == null ? string.Empty : rawIfdPath;
    }
}
