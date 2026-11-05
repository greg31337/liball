namespace image;

/// <summary>Identifies the detected image file or container format.</summary>
public enum ImageFormatKind
{
    /// <summary>The format is unknown.</summary>
    Unknown = 0,
    /// <summary>Joint Photographic Experts Group image.</summary>
    Jpeg = 1,
    /// <summary>Classic Tagged Image File Format image.</summary>
    Tiff = 2,
    /// <summary>BigTIFF image with 64-bit offsets.</summary>
    BigTiff = 3,
    /// <summary>Portable Network Graphics image.</summary>
    Png = 4,
    /// <summary>Graphics Interchange Format image.</summary>
    Gif = 5,
    /// <summary>WebP image in a RIFF container.</summary>
    WebP = 6,
    /// <summary>Windows bitmap image.</summary>
    Bmp = 7,
    /// <summary>Windows icon file.</summary>
    Ico = 8,
    /// <summary>Windows cursor file.</summary>
    Cur = 9,
    /// <summary>Scalable Vector Graphics XML document.</summary>
    Svg = 10,
    /// <summary>Gzip-compressed Scalable Vector Graphics document.</summary>
    Svgz = 11,
    /// <summary>High Efficiency Image File container.</summary>
    Heif = 12,
    /// <summary>HEIF container carrying HEVC-coded images.</summary>
    Heic = 13,
    /// <summary>AV1 Image File Format container.</summary>
    Avif = 14,
    /// <summary>Canon CR3 image in an ISO base media container.</summary>
    Cr3 = 15,
    /// <summary>JPEG 2000 codestream or JP2-family container.</summary>
    Jpeg2000 = 16,
    /// <summary>JPEG XL codestream or container.</summary>
    JpegXl = 17,
    /// <summary>Adobe Photoshop document.</summary>
    Psd = 18,
    /// <summary>Adobe large Photoshop document.</summary>
    Psb = 19,
    /// <summary>Adobe Digital Negative raw image.</summary>
    Dng = 20,
    /// <summary>Nikon electronic raw image.</summary>
    Nef = 21,
    /// <summary>Sony Alpha raw image.</summary>
    Arw = 22,
    /// <summary>Olympus raw image.</summary>
    Orf = 23,
    /// <summary>Pentax electronic raw image.</summary>
    Pef = 24,
    /// <summary>Panasonic raw image.</summary>
    Rw2 = 25,
    /// <summary>Samsung raw image.</summary>
    Srw = 26,
    /// <summary>Hasselblad 3FR raw image.</summary>
    ThreeFr = 27,
    /// <summary>Kodak digital camera raw image.</summary>
    Kdc = 28,
    /// <summary>Minolta raw image.</summary>
    Mrw = 29,
    /// <summary>Leica raw image.</summary>
    Rwl = 30,
    /// <summary>GoPro raw image.</summary>
    Gpr = 31,
    /// <summary>Canon CR2 raw image.</summary>
    Cr2 = 32,
    /// <summary>Canon camera image file raw image.</summary>
    Crw = 33,
    /// <summary>Fujifilm raw image.</summary>
    Raf = 34,
    /// <summary>Netpbm-family bitmap, graymap, pixmap, or arbitrary map.</summary>
    Netpbm = 35,
    /// <summary>PC Paintbrush image.</summary>
    Pcx = 36,
    /// <summary>Truevision TGA image.</summary>
    Tga = 37,
    /// <summary>DirectDraw Surface texture.</summary>
    Dds = 38,
    /// <summary>Quite OK Image format image.</summary>
    Qoi = 39,
    /// <summary>Radiance RGBE high-dynamic-range image.</summary>
    RadianceHdr = 40,
    /// <summary>Encapsulated PostScript document.</summary>
    Eps = 41,
    /// <summary>Extensible Metadata Platform sidecar document.</summary>
    Xmp = 42
}
