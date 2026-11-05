namespace tiff;

/// <summary>Specifies the supported byte order values used by TIFF data.</summary>
public enum TiffByteOrder
{
    /// <summary>Specifies unknown.</summary>
    Unknown = 0,
    /// <summary>Specifies little endian.</summary>
    LittleEndian = 18761,
    /// <summary>Specifies big endian.</summary>
    BigEndian = 19789
}
