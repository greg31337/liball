namespace image;

/// <summary>Identifies the compression scheme reported for an image or frame.</summary>
public enum ImageCompressionKind
{
    /// <summary>The compression scheme was not declared or could not be determined.</summary>
    Unknown = 0,
    /// <summary>The sample data is stored without compression.</summary>
    None = 1,
    /// <summary>The sample data uses JPEG compression.</summary>
    Jpeg = 2,
    /// <summary>The sample data uses the Deflate algorithm.</summary>
    Deflate = 3,
    /// <summary>The sample data uses Lempel-Ziv-Welch compression.</summary>
    Lzw = 4,
    /// <summary>The sample data uses TIFF PackBits compression.</summary>
    PackBits = 5,
    /// <summary>The sample data uses a run-length encoding scheme.</summary>
    Rle = 6,
    /// <summary>The sample data uses WebP compression.</summary>
    WebP = 7,
    /// <summary>The sample data uses JPEG 2000 compression.</summary>
    Jpeg2000 = 8,
    /// <summary>The sample data uses JPEG XL compression.</summary>
    JpegXl = 9,
    /// <summary>The sample data uses bzip2 compression.</summary>
    Bzip2 = 10,
    /// <summary>The sample data uses Zstandard compression.</summary>
    Zstd = 11,
    /// <summary>The sample data uses LZMA compression.</summary>
    Lzma = 12
}
