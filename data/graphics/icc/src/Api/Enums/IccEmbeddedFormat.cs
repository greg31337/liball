namespace icc;

/// <summary>
/// Identifies a supported profile carrier format.
/// </summary>
public enum IccEmbeddedFormat
{
    /// <summary>
    /// Indicates an unknown carrier format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies an unsegmented raw ICC profile.
    /// </summary>
    RawIcc = 1,

    /// <summary>
    /// Identifies a JPEG carrier.
    /// </summary>
    Jpeg = 2,

    /// <summary>
    /// Identifies a PNG carrier.
    /// </summary>
    Png = 3,

    /// <summary>
    /// Identifies a classic TIFF carrier.
    /// </summary>
    Tiff = 4,

    /// <summary>
    /// Identifies a BigTIFF carrier.
    /// </summary>
    BigTiff = 5,

    /// <summary>
    /// Identifies a DNG carrier.
    /// </summary>
    Dng = 6,

    /// <summary>
    /// Identifies a WebP carrier.
    /// </summary>
    WebP = 7,

    /// <summary>
    /// Identifies a JPEG 2000 JP2 or JPX carrier.
    /// </summary>
    Jpeg2000 = 8,

    /// <summary>
    /// Identifies a generic ISO Base Media File Format carrier.
    /// </summary>
    IsoBmff = 9,

    /// <summary>
    /// Identifies an AVIF carrier.
    /// </summary>
    Avif = 10,

    /// <summary>
    /// Identifies a HEIF carrier.
    /// </summary>
    Heif = 11,

    /// <summary>
    /// Identifies a recognized integration-only carrier such as PDF, OpenXPS,
    /// SVG, CSS, EPS, or JPEG XL.
    /// </summary>
    Unsupported = 12
}
