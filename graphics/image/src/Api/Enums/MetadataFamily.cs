namespace image;

/// <summary>Groups metadata by its source container or governing standard.</summary>
public enum MetadataFamily
{
    /// <summary>The metadata family is unknown.</summary>
    Unknown = 0,
    /// <summary>Facts about the source file as a whole.</summary>
    File = 1,
    /// <summary>Facts that identify the image format.</summary>
    Format = 2,
    /// <summary>General decoded image characteristics.</summary>
    Image = 3,
    /// <summary>Facts from an outer file container.</summary>
    Container = 4,
    /// <summary>JPEG marker and frame metadata.</summary>
    Jpeg = 5,
    /// <summary>JPEG File Interchange Format metadata.</summary>
    Jfif = 6,
    /// <summary>Tagged Image File Format metadata.</summary>
    Tiff = 7,
    /// <summary>Exchangeable Image File Format metadata.</summary>
    Exif = 8,
    /// <summary>Global Positioning System metadata.</summary>
    Gps = 9,
    /// <summary>International Press Telecommunications Council metadata.</summary>
    Iptc = 10,
    /// <summary>Extensible Metadata Platform metadata.</summary>
    Xmp = 11,
    /// <summary>International Color Consortium profile metadata.</summary>
    Icc = 12,
    /// <summary>Adobe Photoshop resource metadata.</summary>
    Photoshop = 13,
    /// <summary>Portable Network Graphics chunk metadata.</summary>
    Png = 14,
    /// <summary>Graphics Interchange Format block metadata.</summary>
    Gif = 15,
    /// <summary>WebP chunk metadata.</summary>
    WebP = 16,
    /// <summary>Windows bitmap metadata.</summary>
    Bmp = 17,
    /// <summary>Windows icon or cursor metadata.</summary>
    Ico = 18,
    /// <summary>Scalable Vector Graphics metadata.</summary>
    Svg = 19,
    /// <summary>ISO base media file format box metadata.</summary>
    Bmff = 20,
    /// <summary>JPEG 2000 box or codestream metadata.</summary>
    Jpeg2000 = 21,
    /// <summary>JPEG XL box or codestream metadata.</summary>
    JpegXl = 22,
    /// <summary>Photoshop document metadata.</summary>
    Psd = 23,
    /// <summary>Camera raw format metadata.</summary>
    Raw = 24,
    /// <summary>Vendor-specific camera maker-note metadata.</summary>
    MakerNote = 25,
    /// <summary>Embedded thumbnail or preview metadata.</summary>
    Thumbnail = 26,
    /// <summary>Parser diagnostic information represented as metadata.</summary>
    Diagnostic = 27
}
