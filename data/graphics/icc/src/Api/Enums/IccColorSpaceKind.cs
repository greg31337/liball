namespace icc;

/// <summary>
/// Identifies a decoded ICC data colour-space family.
/// </summary>
public enum IccColorSpaceKind
{
    /// <summary>
    /// Indicates an unknown colour space.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies CIE XYZ data.
    /// </summary>
    Xyz = 1,

    /// <summary>
    /// Identifies CIE Lab data.
    /// </summary>
    Lab = 2,

    /// <summary>
    /// Identifies CIE Luv data.
    /// </summary>
    Luv = 3,

    /// <summary>
    /// Identifies YCbCr data.
    /// </summary>
    YCbCr = 4,

    /// <summary>
    /// Identifies CIE Yxy data.
    /// </summary>
    Yxy = 5,

    /// <summary>
    /// Identifies RGB data.
    /// </summary>
    Rgb = 6,

    /// <summary>
    /// Identifies grayscale data.
    /// </summary>
    Gray = 7,

    /// <summary>
    /// Identifies HSV data.
    /// </summary>
    Hsv = 8,

    /// <summary>
    /// Identifies HLS data.
    /// </summary>
    Hls = 9,

    /// <summary>
    /// Identifies CMYK data.
    /// </summary>
    Cmyk = 10,

    /// <summary>
    /// Identifies CMY data.
    /// </summary>
    Cmy = 11,

    /// <summary>
    /// Identifies named-colour data.
    /// </summary>
    NamedColor = 12,

    /// <summary>
    /// Identifies one-through-fifteen-channel or legacy MCH device data.
    /// </summary>
    DeviceChannels = 13,

    /// <summary>
    /// Identifies ICC.2 extended device-channel data.
    /// </summary>
    ExtendedDevice = 14,

    /// <summary>
    /// Identifies spectral data.
    /// </summary>
    Spectral = 15,

    /// <summary>
    /// Identifies bi-spectral data.
    /// </summary>
    BiSpectral = 16,

    /// <summary>
    /// Identifies ICC.2 multiplex-channel data.
    /// </summary>
    Multiplex = 17
}
