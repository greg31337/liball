namespace geo;

/// <summary>Identifies the byte order used by a binary geometry encoding.</summary>
public enum ByteOrder
{
    /// <summary>Most-significant byte first, represented by zero in WKB.</summary>
    BigEndian = 0,

    /// <summary>Least-significant byte first, represented by one in WKB.</summary>
    LittleEndian = 1
}
