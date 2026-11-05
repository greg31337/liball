namespace uuid;

/// <summary>Provides the special Nil and Max UUID values defined by RFC 9562.</summary>
/// <remarks>The values are immutable and byte-returning operations provide defensive copies.</remarks>
public static class UuidValues
{
    /// <summary>The Nil UUID whose 128 bits are all zero.</summary>
    public static readonly UuidValue Nil = new UuidValue(new byte[16]);

    /// <summary>The Max UUID whose 128 bits are all one.</summary>
    public static readonly UuidValue Max = new UuidValue(new byte[]
        {
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF,
            0xFF
        });
}
