namespace svg;

/// <summary>Identifies supported separable, non-separable, and additive blend modes.</summary>
public enum SvgBlendMode
{
    /// <summary>Uses ordinary source-over colors.</summary>
    Normal = 0,
    /// <summary>Multiplies backdrop and source channels.</summary>
    Multiply = 1,
    /// <summary>Applies the inverse of multiplied inverse channels.</summary>
    Screen = 2,
    /// <summary>Multiplies or screens according to the backdrop.</summary>
    Overlay = 3,
    /// <summary>Selects the darker channel.</summary>
    Darken = 4,
    /// <summary>Selects the lighter channel.</summary>
    Lighten = 5,
    /// <summary>Brightens the backdrop toward white.</summary>
    ColorDodge = 6,
    /// <summary>Darkens the backdrop toward black.</summary>
    ColorBurn = 7,
    /// <summary>Multiplies or screens according to the source.</summary>
    HardLight = 8,
    /// <summary>Applies a softened light adjustment.</summary>
    SoftLight = 9,
    /// <summary>Uses the absolute channel difference.</summary>
    Difference = 10,
    /// <summary>Uses a lower-contrast difference.</summary>
    Exclusion = 11,
    /// <summary>Uses source hue with backdrop saturation and luminosity.</summary>
    Hue = 12,
    /// <summary>Uses source saturation with backdrop hue and luminosity.</summary>
    Saturation = 13,
    /// <summary>Uses source hue and saturation with backdrop luminosity.</summary>
    Color = 14,
    /// <summary>Uses source luminosity with backdrop hue and saturation.</summary>
    Luminosity = 15,
    /// <summary>Adds premultiplied colors with black-side saturation.</summary>
    PlusDarker = 16,
    /// <summary>Adds premultiplied colors with white-side saturation.</summary>
    PlusLighter = 17
}

/// <summary>Selects the color space used by blending and compositing arithmetic.</summary>
public enum SvgBlendColorSpace
{
    /// <summary>Uses the destination buffer's concrete color space.</summary>
    Auto = 0,
    /// <summary>Uses gamma-encoded sRGB channels.</summary>
    Srgb = 1,
    /// <summary>Uses linear-light sRGB channels.</summary>
    SrgbLinear = 2
}
