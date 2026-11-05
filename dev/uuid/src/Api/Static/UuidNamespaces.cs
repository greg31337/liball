namespace uuid;

/// <summary>Provides the standard RFC namespace UUIDs used by name-based UUID versions 3 and 5.</summary>
/// <remarks>UUID values are immutable and therefore safe to share.</remarks>
public static class UuidNamespaces
{
    /// <summary>The namespace UUID for fully qualified domain names.</summary>
    public static readonly UuidValue Dns = new UuidValue(new byte[]
        {
            0x6B,
            0xA7,
            0xB8,
            0x10,
            0x9D,
            0xAD,
            0x11,
            0xD1,
            0x80,
            0xB4,
            0x00,
            0xC0,
            0x4F,
            0xD4,
            0x30,
            0xC8
        });

    /// <summary>The namespace UUID for URLs.</summary>
    public static readonly UuidValue Url = new UuidValue(new byte[]
        {
            0x6B,
            0xA7,
            0xB8,
            0x11,
            0x9D,
            0xAD,
            0x11,
            0xD1,
            0x80,
            0xB4,
            0x00,
            0xC0,
            0x4F,
            0xD4,
            0x30,
            0xC8
        });

    /// <summary>The namespace UUID for ISO object identifiers.</summary>
    public static readonly UuidValue Oid = new UuidValue(new byte[]
        {
            0x6B,
            0xA7,
            0xB8,
            0x12,
            0x9D,
            0xAD,
            0x11,
            0xD1,
            0x80,
            0xB4,
            0x00,
            0xC0,
            0x4F,
            0xD4,
            0x30,
            0xC8
        });

    /// <summary>The namespace UUID for X.500 distinguished names.</summary>
    public static readonly UuidValue X500 = new UuidValue(new byte[]
        {
            0x6B,
            0xA7,
            0xB8,
            0x14,
            0x9D,
            0xAD,
            0x11,
            0xD1,
            0x80,
            0xB4,
            0x00,
            0xC0,
            0x4F,
            0xD4,
            0x30,
            0xC8
        });
}
