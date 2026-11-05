namespace image;

/// <summary>Identifies the color model or named RGB space used by image samples.</summary>
public enum ImageColorSpaceKind
{
    /// <summary>The color space was not declared or could not be determined.</summary>
    Unknown = 0,
    /// <summary>Samples represent grayscale intensity.</summary>
    Gray = 1,
    /// <summary>Samples use red, green, and blue channels without a more specific named space.</summary>
    Rgb = 2,
    /// <summary>Samples use cyan, magenta, yellow, and black channels.</summary>
    Cmyk = 3,
    /// <summary>Samples use luma and blue/red chroma components.</summary>
    YCbCr = 4,
    /// <summary>Samples use the CIE L*a*b* color model.</summary>
    Lab = 5,
    /// <summary>Pixel values select colors from a palette.</summary>
    Indexed = 6,
    /// <summary>Samples use hue, saturation, and value components.</summary>
    Hsv = 7,
    /// <summary>Samples use hue, saturation, and lightness components.</summary>
    Hsl = 8,
    /// <summary>Samples use CIE XYZ tristimulus components.</summary>
    Xyz = 9,
    /// <summary>Samples use an unspecified wide-gamut RGB space.</summary>
    WideGamutRgb = 10,
    /// <summary>Samples use the Display P3 RGB color space.</summary>
    DisplayP3 = 11,
    /// <summary>Samples use the Adobe RGB color space.</summary>
    AdobeRgb = 12,
    /// <summary>Samples use the ProPhoto RGB color space.</summary>
    ProPhotoRgb = 13
}
