namespace fake;

/// <summary>
/// Identifies an append-style checksum engine available from <see cref="ChecksumProvider"/>.
/// </summary>
public enum ChecksumKind
{
    /// <summary>The Luhn decimal checksum.</summary>
    Luhn = 0,

    /// <summary>The Verhoeff decimal checksum.</summary>
    Verhoeff = 1,

    /// <summary>The Damm decimal checksum.</summary>
    Damm = 2,

    /// <summary>The default right-anchored weighted modulo-10 checksum.</summary>
    Modulo10 = 3,

    /// <summary>The default right-anchored weighted modulo-11 checksum.</summary>
    Modulo11 = 4,

    /// <summary>The ISO 7064 MOD 97-10 decimal checksum.</summary>
    Mod97 = 5,

    /// <summary>The compact ISBN-10 checksum and structural contract.</summary>
    Isbn10 = 6,

    /// <summary>The compact ISBN-13 checksum and structural contract.</summary>
    Isbn13 = 7,

    /// <summary>The compact ISSN checksum and structural contract.</summary>
    Issn = 8,

    /// <summary>The compact EAN-8 checksum and structural contract.</summary>
    Ean8 = 9,

    /// <summary>The compact UPC-A checksum and structural contract.</summary>
    UpcA = 10,

    /// <summary>The compact GTIN checksum and structural contract.</summary>
    Gtin = 11
}
