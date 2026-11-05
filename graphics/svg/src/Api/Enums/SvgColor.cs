namespace svg;

/// <summary>Defines color spaces stored by color values.</summary>
public enum SvgColorSpace
{
    /// <summary>The gamma-encoded standard RGB color space.</summary>
    Srgb = 0,

    /// <summary>The linear-light standard RGB color space.</summary>
    SrgbLinear = 1,

    /// <summary>The polar sRGB hue-whiteness-blackness color space.</summary>
    Hwb = 2,

    /// <summary>The CIE Lab color space with a D50 white point.</summary>
    Lab = 3,

    /// <summary>The polar CIE LCH color space with a D50 white point.</summary>
    Lch = 4,

    /// <summary>The OKLab perceptual color space.</summary>
    Oklab = 5,

    /// <summary>The polar OKLCH perceptual color space.</summary>
    Oklch = 6
}

/// <summary>Defines stable CSS color parsing failures.</summary>
public enum SvgColorParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,

    /// <summary>An input or parser bound was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>A color value was required.</summary>
    ExpectedColor = 2,

    /// <summary>The color keyword was not recognized.</summary>
    UnknownColor = 3,

    /// <summary>The hexadecimal color had an invalid digit count or character.</summary>
    InvalidHex = 4,

    /// <summary>The color function name was not recognized.</summary>
    UnknownFunction = 5,

    /// <summary>A required component was missing or malformed.</summary>
    ExpectedComponent = 6,

    /// <summary>A component number was malformed, too long, or non-finite.</summary>
    InvalidNumber = 7,

    /// <summary>A hue unit was not recognized.</summary>
    InvalidUnit = 8,

    /// <summary>Legacy RGB components mixed numbers and percentages.</summary>
    MixedLegacyUnits = 9,

    /// <summary>Component separators mixed legacy and modern syntax or were missing.</summary>
    InvalidSeparator = 10,

    /// <summary>A closing parenthesis was required.</summary>
    ExpectedCloseParenthesis = 11,

    /// <summary>Characters followed a complete color value.</summary>
    TrailingCharacters = 12,

    /// <summary>A keyword, function name, or hexadecimal token exceeded its configured bound.</summary>
    TokenTooLong = 13,

    /// <summary>Color conversion produced a non-finite channel.</summary>
    NonFiniteResult = 14
}
