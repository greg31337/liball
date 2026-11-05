namespace svg;

/// <summary>Represents a finite color value or the current-color placeholder.</summary>
public class SvgColor
{
    /// <summary>The color space of the numeric channels.</summary>
    public SvgColorSpace Space;

    /// <summary>The finite first channel in the selected color space.</summary>
    public double Red;

    /// <summary>The finite second channel in the selected color space.</summary>
    public double Green;

    /// <summary>The finite third channel in the selected color space.</summary>
    public double Blue;

    /// <summary>The alpha channel from zero through one.</summary>
    public double Alpha;

    /// <summary>Whether this value defers to the computed current color.</summary>
    public bool IsCurrentColor;

    /// <summary>Whether the first authored functional component was missing.</summary>
    public bool FirstMissing;

    /// <summary>Whether the second authored functional component was missing.</summary>
    public bool SecondMissing;

    /// <summary>Whether the third authored functional component was missing.</summary>
    public bool ThirdMissing;

    /// <summary>Whether the authored alpha component was missing.</summary>
    public bool AlphaMissing;

    /// <summary>The preserved authored text, or an empty string for a programmatic color.</summary>
    public string Source;

    /// <summary>Whether the mutable color state is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a finite numeric color.</summary>
    /// <param name="space">The channel color space.</param>
    /// <param name="red">The finite red channel.</param>
    /// <param name="green">The finite green channel.</param>
    /// <param name="blue">The finite blue channel.</param>
    /// <param name="alpha">The alpha channel from zero through one.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an unknown space, non-finite channel, or alpha outside zero through one.</exception>
    public SvgColor(SvgColorSpace space, double red, double green, double blue, double alpha)
    {
        if (!SvgColorMath.IsKnownSpace(space) || !SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue) || !SvgFiniteMath.IsFinite(alpha) || alpha < 0 || alpha > 1)
        {
            throw new System.ArgumentOutOfRangeException("alpha");
        }
        Space = space;
        Red = SvgFiniteMath.NormalizeZero(red);
        Green = SvgFiniteMath.NormalizeZero(green);
        Blue = SvgFiniteMath.NormalizeZero(blue);
        Alpha = SvgFiniteMath.NormalizeZero(alpha);
        IsCurrentColor = false;
        FirstMissing = false;
        SecondMissing = false;
        ThirdMissing = false;
        AlphaMissing = false;
        Source = "";
        IsValid = true;
    }

    /// <summary>Creates the current-color placeholder.</summary>
    /// <returns>A valid current-color value.</returns>
    internal static SvgColor CurrentColor()
    {
        SvgColor color = new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 1);
        color.IsCurrentColor = true;
        return color;
    }

    /// <summary>Converts this color to another supported color space.</summary>
    /// <param name="targetSpace">The destination color space.</param>
    /// <returns>The converted color or a stable failure.</returns>
    internal SvgColorOperationResult ConvertTo(SvgColorSpace targetSpace)
    {
        return SvgColorMath.Convert(this, targetSpace);
    }

    /// <summary>Interpolates this color with another color in a selected color space.</summary>
    /// <param name="target">The ending color.</param>
    /// <param name="amount">The finite interpolation amount, normally from zero through one.</param>
    /// <param name="interpolationSpace">The color space in which interpolation occurs.</param>
    /// <returns>The interpolated color or a stable failure.</returns>
    internal SvgColorOperationResult Interpolate(SvgColor target, double amount, SvgColorSpace interpolationSpace)
    {
        return SvgColorMath.Interpolate(this, target, amount, interpolationSpace);
    }

    /// <summary>Determines whether this color lies within the sRGB gamut.</summary>
    /// <returns><c>true</c> when all converted sRGB channels are from zero through one.</returns>
    internal bool IsInSrgbGamut()
    {
        return SvgColorMath.IsInSrgbGamut(this);
    }

    /// <summary>Converts this color to sRGB and clips its channels to the sRGB gamut.</summary>
    /// <returns>The clipped sRGB color or a stable failure.</returns>
    internal SvgColorOperationResult ClipToSrgbGamut()
    {
        return SvgColorMath.ClipToSrgbGamut(this);
    }

    /// <summary>Formats this value in its canonical functional color form.</summary>
    /// <returns>The canonical text, or an empty string for invalid mutable state.</returns>
    internal string ToText()
    {
        return SvgColorMath.Format(this);
    }
}

/// <summary>Contains a non-throwing CSS color parse result.</summary>
public class SvgColorParseResult
{
    /// <summary>Whether a complete color parsed successfully.</summary>
    public bool Success;

    /// <summary>The stable parsing failure kind.</summary>
    public SvgColorParseError Error;

    /// <summary>The parsed color, or <c>null</c> after failure.</summary>
    public SvgColor? Color;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a CSS color parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="color">The parsed color, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgColorParseResult(bool success, SvgColorParseError error, SvgColor? color, int errorOffset)
    {
        Success = success;
        Error = error;
        Color = color;
        ErrorOffset = errorOffset;
    }
}
