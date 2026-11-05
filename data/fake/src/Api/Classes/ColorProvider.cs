using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides color generation operations.
/// </summary>
public class ColorProvider : IColorProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal ColorProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("color");
    }

    /// <summary>Selects a reviewed invariant CSS color name.</summary>
    /// <returns>A lowercase named color.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Name()
    {
        return ColorGenerator.Name(Context, RandomValue);
    }

    /// <summary>Generates an uppercase six-digit hexadecimal sRGB color.</summary>
    /// <returns>A value in <c>#RRGGBB</c> form.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string Hex()
    {
        return ColorGenerator.Hex(Context, RandomValue);
    }

    /// <summary>Formats an sRGB color as uppercase hexadecimal text.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A value in <c>#RRGGBB</c> form.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public string HexFromValue(RgbColor value)
    {
        return ColorGenerator.Hex(Context, value);
    }

    /// <summary>Generates an sRGB color.</summary>
    /// <returns>Three integer channels from 0 through 255.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public RgbColor Rgb()
    {
        return ColorGenerator.Rgb(Context, RandomValue);
    }

    /// <summary>Creates a validated sRGB color from explicit channels.</summary>
    /// <param name="red">The red channel from 0 through 255.</param>
    /// <param name="green">The green channel from 0 through 255.</param>
    /// <param name="blue">The blue channel from 0 through 255.</param>
    /// <returns>An independent sRGB color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public RgbColor RgbFromRedAndGreenAndBlue(int red, int green, int blue)
    {
        return ColorGenerator.Rgb(Context, red, green, blue);
    }

    /// <summary>Parses a six-digit hexadecimal sRGB color.</summary>
    /// <param name="hex">Six hexadecimal digits with an optional leading <c>#</c>.</param>
    /// <returns>The parsed sRGB channels.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when hex is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when hex is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public RgbColor RgbFromHex(string hex)
    {
        return ColorGenerator.Rgb(Context, hex);
    }

    /// <summary>Generates an HSL color converted from random sRGB channels.</summary>
    /// <returns>Hue in degrees and saturation and lightness percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public HslColor Hsl()
    {
        return ColorGenerator.Hsl(Context, RandomValue);
    }

    /// <summary>Converts an sRGB color to HSL.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Hue in degrees and saturation and lightness percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public HslColor HslFromValue(RgbColor value)
    {
        return ColorGenerator.Hsl(Context, value);
    }

    /// <summary>Generates an HWB color converted from random sRGB channels.</summary>
    /// <returns>Hue in degrees and whiteness and blackness percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public HwbColor Hwb()
    {
        return ColorGenerator.Hwb(Context, RandomValue);
    }

    /// <summary>Converts an sRGB color to HWB.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Hue in degrees and whiteness and blackness percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public HwbColor HwbFromValue(RgbColor value)
    {
        return ColorGenerator.Hwb(Context, value);
    }

    /// <summary>Generates an arithmetic CMYK color converted from random sRGB channels.</summary>
    /// <remarks>The result is synthetic numeric data and does not model a printer profile.</remarks>
    /// <returns>Cyan, magenta, yellow, and black key percentages.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public CmykColor Cmyk()
    {
        return ColorGenerator.Cmyk(Context, RandomValue);
    }

    /// <summary>Converts an sRGB color to arithmetic CMYK.</summary>
    /// <remarks>The result is synthetic numeric data and does not model a printer profile.</remarks>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>Cyan, magenta, yellow, and black key percentages.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public CmykColor CmykFromValue(RgbColor value)
    {
        return ColorGenerator.Cmyk(Context, value);
    }

    /// <summary>Generates a D65 CIE L*a*b* color converted from random sRGB channels.</summary>
    /// <returns>A concrete Lab color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public LabColor Lab()
    {
        return ColorGenerator.Lab(Context, RandomValue);
    }

    /// <summary>Converts an sRGB color to D65 CIE L*a*b*.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A concrete Lab color value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public LabColor LabFromValue(RgbColor value)
    {
        return ColorGenerator.Lab(Context, value);
    }

    /// <summary>Generates a cylindrical D65 CIE L*C*h color converted from random sRGB channels.</summary>
    /// <returns>A concrete LCH color value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public LchColor Lch()
    {
        return ColorGenerator.Lch(Context, RandomValue);
    }

    /// <summary>Converts an sRGB color to cylindrical D65 CIE L*C*h.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A concrete LCH color value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public LchColor LchFromValue(RgbColor value)
    {
        return ColorGenerator.Lch(Context, value);
    }

    /// <summary>Generates a CSS Color 4 <c>rgb()</c> function.</summary>
    /// <returns>A space-separated CSS sRGB function.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string Css()
    {
        return ColorGenerator.Css(Context, RandomValue);
    }

    /// <summary>Formats an sRGB color as a CSS Color 4 <c>rgb()</c> function.</summary>
    /// <param name="value">The validated sRGB color.</param>
    /// <returns>A space-separated CSS sRGB function.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a channel or configured limit is invalid.</exception>
    public string CssFromValue(RgbColor value)
    {
        return ColorGenerator.Css(Context, value);
    }

    /// <summary>Generates one correlated reviewed named-color record.</summary>
    /// <returns>A named color represented in every supported model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public ColorRecord Record()
    {
        return ColorGenerator.Record(Context, RandomValue);
    }

    /// <summary>Generates a five-color palette without duplicate names.</summary>
    /// <returns>Five independent correlated color records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public ColorRecord[] Palette()
    {
        return ColorGenerator.Palette(Context, RandomValue);
    }

    /// <summary>Generates a palette without duplicate names.</summary>
    /// <param name="count">The number of colors, from zero through the reviewed named-color count.</param>
    /// <returns>Independent correlated color records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when count or a configured limit is invalid.</exception>
    public ColorRecord[] PaletteFromCount(int count)
    {
        return ColorGenerator.Palette(Context, RandomValue, count);
    }
}
