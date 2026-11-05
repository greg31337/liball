namespace chart;
/// <summary>Defines supported PNG color encodings.</summary>
public enum ChartPngColorType
{
    /// <summary>Selects the lossless color type that matches the source bitmap format.</summary>
    Automatic = -1,
    /// <summary>Eight-bit grayscale samples.</summary>
    Grayscale = 0,
    /// <summary>Eight-bit RGB samples.</summary>
    Truecolor = 2,
    /// <summary>Eight-bit palette indices with palette and optional transparency chunks.</summary>
    Indexed = 3,
    /// <summary>Eight-bit grayscale and alpha samples.</summary>
    GrayscaleAlpha = 4,
    /// <summary>Eight-bit RGBA samples.</summary>
    TruecolorAlpha = 6
}
