using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="ColorProvider"/>.</summary>
public interface IColorProvider
{
    /// <summary>Selects a reviewed invariant CSS color name.</summary>
    /// <returns>A lowercase named color.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Name();
    /// <summary>Generates an uppercase six-digit hexadecimal sRGB color.</summary>
    /// <returns>A value in <c>#RRGGBB</c> form.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string Hex();
    /// <summary>Formats an sRGB color as uppercase hexadecimal text.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A value in <c>#RRGGBB</c> form.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    string HexFromValue(RgbColor value);
    /// <summary>Generates an sRGB color.</summary>
    /// <returns>Three integer channels from 0 through 255.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    RgbColor Rgb();
    /// <summary>Creates a validated sRGB color from explicit channels.</summary>
    /// <param name="red">The red channel from 0 through 255.</param>
    /// <param name="green">The green channel from 0 through 255.</param>
    /// <param name="blue">The blue channel from 0 through 255.</param>
    /// <returns>An independent sRGB color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    RgbColor RgbFromRedAndGreenAndBlue(int red, int green, int blue);
    /// <summary>Parses a six-digit hexadecimal sRGB color.</summary>
    /// <param name="hex">Six hexadecimal digits with an optional leading <c>#</c>.</param>
    /// <returns>The parsed sRGB channels.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when hex is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when hex is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    RgbColor RgbFromHex(string hex);
    /// <summary>Generates an HSL color converted from random sRGB channels.</summary>
    /// <returns>Hue in degrees and saturation and lightness percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    HslColor Hsl();
    /// <summary>Converts an sRGB color to HSL.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Hue in degrees and saturation and lightness percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    HslColor HslFromValue(RgbColor value);
    /// <summary>Generates an HWB color converted from random sRGB channels.</summary>
    /// <returns>Hue in degrees and whiteness and blackness percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    HwbColor Hwb();
    /// <summary>Converts an sRGB color to HWB.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Hue in degrees and whiteness and blackness percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    HwbColor HwbFromValue(RgbColor value);
    /// <summary>Generates an arithmetic CMYK color converted from random sRGB channels.</summary>
    /// <remarks>The result is synthetic numeric data and does not model a printer profile.</remarks>
    /// <returns>Cyan, magenta, yellow, and black key percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    CmykColor Cmyk();
    /// <summary>Converts an sRGB color to arithmetic CMYK.</summary>
    /// <remarks>The result is synthetic numeric data and does not model a printer profile.</remarks>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Cyan, magenta, yellow, and black key percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    CmykColor CmykFromValue(RgbColor value);
    /// <summary>Generates a D65 CIE L*a*b* color converted from random sRGB channels.</summary>
    /// <returns>A concrete Lab color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    LabColor Lab();
    /// <summary>Converts an sRGB color to D65 CIE L*a*b*.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A concrete Lab color value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    LabColor LabFromValue(RgbColor value);
    /// <summary>Generates a cylindrical D65 CIE L*C*h color converted from random sRGB channels.</summary>
    /// <returns>A concrete LCH color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    LchColor Lch();
    /// <summary>Converts an sRGB color to cylindrical D65 CIE L*C*h.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A concrete LCH color value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    LchColor LchFromValue(RgbColor value);
    /// <summary>Generates a CSS Color 4 <c>rgb()</c> function.</summary>
    /// <returns>A space-separated CSS sRGB function.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string Css();
    /// <summary>Formats an sRGB color as a CSS Color 4 <c>rgb()</c> function.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A space-separated CSS sRGB function.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    string CssFromValue(RgbColor value);
    /// <summary>Generates one correlated reviewed named-color record.</summary>
    /// <returns>A named color represented in every supported model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    ColorRecord Record();
    /// <summary>Generates a five-color palette without duplicate names.</summary>
    /// <returns>Five independent correlated color records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    ColorRecord[] Palette();
    /// <summary>Generates a palette without duplicate names.</summary>
    /// <param name="count">The number of colors, from zero through the reviewed named-color count.</param>
    /// <returns>Independent correlated color records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when count or a configured limit is invalid.</exception>
    ColorRecord[] PaletteFromCount(int count);
}
