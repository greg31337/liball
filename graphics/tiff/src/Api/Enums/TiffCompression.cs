namespace tiff;

/// <summary>Specifies the supported compression values used by TIFF data.</summary>
public enum TiffCompression
{
    /// <summary>Specifies unknown.</summary>
    Unknown = 0,
    /// <summary>Specifies none.</summary>
    None = 1,
    /// <summary>Specifies CCITT RLE.</summary>
    CcittRle = 2,
    /// <summary>Specifies CCITT group 3.</summary>
    CcittGroup3 = 3,
    /// <summary>Specifies CCITT group 4.</summary>
    CcittGroup4 = 4,
    /// <summary>Specifies LZW.</summary>
    Lzw = 5,
    /// <summary>Specifies old JPEG.</summary>
    OldJpeg = 6,
    /// <summary>Specifies JPEG.</summary>
    Jpeg = 7,
    /// <summary>Specifies deflate.</summary>
    Deflate = 8,
    /// <summary>Specifies T85.</summary>
    T85 = 9,
    /// <summary>Specifies T43.</summary>
    T43 = 10,
    /// <summary>Specifies NeXT 2-bit.</summary>
    NeXt = 32766,
    /// <summary>Specifies word-aligned CCITT RLE.</summary>
    CcittRleW = 32771,
    /// <summary>Specifies pack bits.</summary>
    PackBits = 32773,
    /// <summary>Specifies thunder scan.</summary>
    ThunderScan = 32809,
    /// <summary>Specifies IT8 ctpad.</summary>
    It8Ctpad = 32895,
    /// <summary>Specifies IT8 lw.</summary>
    It8Lw = 32896,
    /// <summary>Specifies IT8 mp.</summary>
    It8Mp = 32897,
    /// <summary>Specifies IT8 bl.</summary>
    It8Bl = 32898,
    /// <summary>Specifies pixar film.</summary>
    PixarFilm = 32908,
    /// <summary>Specifies pixar log.</summary>
    PixarLog = 32909,
    /// <summary>Specifies adobe deflate.</summary>
    AdobeDeflate = 32946,
    /// <summary>Specifies DCS.</summary>
    Dcs = 32947,
    /// <summary>Specifies JBIG.</summary>
    Jbig = 34661,
    /// <summary>Specifies SGI log.</summary>
    Sgilog = 34676,
    /// <summary>Specifies 24-bit SGILog.</summary>
    Sgilog24 = 34677,
    /// <summary>Specifies JPEG 2000.</summary>
    Jpeg2000 = 34712,
    /// <summary>Specifies LZMA.</summary>
    Lzma = 34925,
    /// <summary>Specifies zstandard.</summary>
    Zstd = 50000,
    /// <summary>Specifies WebP.</summary>
    WebP = 50001,
    /// <summary>Specifies JPEG XL.</summary>
    Jxl = 50002
}
