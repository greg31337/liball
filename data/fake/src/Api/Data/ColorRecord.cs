namespace fake;

/// <summary>Represents one correlated named color in multiple color models.</summary>
public class ColorRecord
{
    /// <summary>The reviewed invariant CSS color name.</summary>
    public string Name = string.Empty;

    /// <summary>The uppercase six-digit hexadecimal sRGB representation.</summary>
    public string Hex = string.Empty;

    /// <summary>The sRGB channel representation.</summary>
    public RgbColor Rgb = new RgbColor();

    /// <summary>The HSL conversion of <c>Rgb</c>.</summary>
    public HslColor Hsl = new HslColor();

    /// <summary>The HWB conversion of <c>Rgb</c>.</summary>
    public HwbColor Hwb = new HwbColor();

    /// <summary>The arithmetic CMYK conversion of <c>Rgb</c>.</summary>
    public CmykColor Cmyk = new CmykColor();

    /// <summary>The D65 CIE L*a*b* conversion of <c>Rgb</c>.</summary>
    public LabColor Lab = new LabColor();

    /// <summary>The cylindrical D65 CIE L*C*h conversion of <c>Lab</c>.</summary>
    public LchColor Lch = new LchColor();

    /// <summary>The CSS Color 4 <c>rgb()</c> function for <c>Rgb</c>.</summary>
    public string Css = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    internal ColorRecord()
    {
    }
}
