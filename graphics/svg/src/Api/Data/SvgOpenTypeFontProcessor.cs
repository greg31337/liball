namespace svg;

/// <summary>Describes one axis from an OpenType <c>fvar</c> table.</summary>
public class SvgOpenTypeVariationAxis
{
    /// <summary>The exact four-character axis tag.</summary>
    public string Tag;
    /// <summary>The minimum user coordinate.</summary>
    public double Minimum;
    /// <summary>The default user coordinate.</summary>
    public double Default;
    /// <summary>The maximum user coordinate.</summary>
    public double Maximum;
    /// <summary>The axis flags.</summary>
    public int Flags;
    /// <summary>The axis name identifier.</summary>
    public int NameIdentifier;
    /// <summary>Whether the axis is hidden from ordinary user interfaces.</summary>
    public bool Hidden;

    internal SvgOpenTypeVariationAxis()
    {
        Tag = "";
        Minimum = 0;
        Default = 0;
        Maximum = 0;
        Flags = 0;
        NameIdentifier = 0;
        Hidden = false;
    }
}

/// <summary>Supplies one requested variable-font user coordinate.</summary>
public class SvgOpenTypeVariationCoordinate
{
    /// <summary>The exact four-character axis tag.</summary>
    public string Tag;
    /// <summary>The finite user coordinate.</summary>
    public double Value;

    /// <summary>Creates one axis coordinate.</summary>
    /// <param name="tag">The exact four-character axis tag.</param>
    /// <param name="value">The finite user coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the tag or value is invalid.</exception>
    public SvgOpenTypeVariationCoordinate(string tag, double value)
    {
        if (tag == null || tag.Length != 4 || !SvgFiniteMath.IsFinite(value)) throw new System.ArgumentOutOfRangeException("tag");
        Tag = tag;
        Value = value;
    }
}

/// <summary>Contains one clamped and normalized variable-font coordinate.</summary>
public class SvgOpenTypeNormalizedCoordinate
{
    /// <summary>The exact four-character axis tag.</summary>
    public string Tag;
    /// <summary>The clamped user coordinate.</summary>
    public double UserValue;
    /// <summary>The normalized coordinate from negative one through positive one after <c>avar</c>.</summary>
    public double NormalizedValue;

    internal SvgOpenTypeNormalizedCoordinate()
    {
        Tag = "";
        UserValue = 0;
        NormalizedValue = 0;
    }
}

/// <summary>Contains one CPAL color in straight RGBA8 form.</summary>
public class SvgOpenTypePaletteColor
{
    /// <summary>The red channel.</summary>
    public byte Red;
    /// <summary>The green channel.</summary>
    public byte Green;
    /// <summary>The blue channel.</summary>
    public byte Blue;
    /// <summary>The alpha channel.</summary>
    public byte Alpha;

    internal SvgOpenTypePaletteColor(byte red, byte green, byte blue, byte alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}

/// <summary>Contains one bounded CPAL palette.</summary>
public class SvgOpenTypeColorPalette
{
    /// <summary>The zero-based palette index.</summary>
    public int Index;
    /// <summary>The palette colors in entry order.</summary>
    public System.Collections.Generic.List<SvgOpenTypePaletteColor> Colors;

    internal SvgOpenTypeColorPalette(int index)
    {
        Index = index;
        Colors = new System.Collections.Generic.List<SvgOpenTypePaletteColor>();
    }
}

/// <summary>Contains one normalized COLR glyph layer.</summary>
public class SvgOpenTypeColorGlyphLayer
{
    /// <summary>The supported paint operation.</summary>
    public SvgOpenTypeColorPaintKind PaintKind;
    /// <summary>The glyph outline painted by this layer.</summary>
    public int GlyphIndex;
    /// <summary>The CPAL entry index, or <c>65535</c> for the current text color.</summary>
    public int PaletteEntryIndex;
    /// <summary>The resolved palette color, or <c>null</c> for the current text color.</summary>
    public SvgOpenTypePaletteColor? Color;

    internal SvgOpenTypeColorGlyphLayer()
    {
        PaintKind = SvgOpenTypeColorPaintKind.SolidGlyph;
        GlyphIndex = 0;
        PaletteEntryIndex = 0;
        Color = null;
    }
}

/// <summary>Contains one COLR base glyph and its ordered paint layers.</summary>
public class SvgOpenTypeColorGlyph
{
    /// <summary>The base glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The ordered normalized paint layers.</summary>
    public System.Collections.Generic.List<SvgOpenTypeColorGlyphLayer> Layers;

    internal SvgOpenTypeColorGlyph(int glyphIndex)
    {
        GlyphIndex = glyphIndex;
        Layers = new System.Collections.Generic.List<SvgOpenTypeColorGlyphLayer>();
    }
}

/// <summary>Configures bounded variable and color-font profile inspection.</summary>
public class SvgOpenTypeProfileOptions
{
    /// <summary>The shared font and variation-axis limits.</summary>
    public SvgFontLimits Limits;
    /// <summary>The maximum CPAL palette count.</summary>
    public int MaximumPalettes;
    /// <summary>The maximum colors retained across all palettes.</summary>
    public int MaximumPaletteColors;
    /// <summary>The maximum COLR base glyph count.</summary>
    public int MaximumColorGlyphs;
    /// <summary>The maximum COLR layer count.</summary>
    public int MaximumColorLayers;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure bounded profile defaults.</summary>
    public SvgOpenTypeProfileOptions()
    {
        Limits = new SvgFontLimits();
        MaximumPalettes = 4096;
        MaximumPaletteColors = 1000000;
        MaximumColorGlyphs = 65535;
        MaximumColorLayers = 1000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains exposed variable-axis, palette, and color-glyph metadata.</summary>
public class SvgOpenTypeFontProfile
{
    /// <summary>The stable processing outcome.</summary>
    public SvgOpenTypeInstanceStatus Status;
    /// <summary>Whether profile inspection completed.</summary>
    public bool Success;
    /// <summary>The ordered variation axes.</summary>
    public System.Collections.Generic.List<SvgOpenTypeVariationAxis> Axes;
    /// <summary>The ordered CPAL palettes.</summary>
    public System.Collections.Generic.List<SvgOpenTypeColorPalette> Palettes;
    /// <summary>The COLR version, or negative one when absent.</summary>
    public int ColorVersion;
    /// <summary>The supported COLR v0 base glyphs.</summary>
    public System.Collections.Generic.List<SvgOpenTypeColorGlyph> ColorGlyphs;
    /// <summary>Whether valid but unsupported COLR v1 paint data is present.</summary>
    public bool RequiresColorCallback;
    /// <summary>The exact table requiring a caller, or an empty string.</summary>
    public string RequiredTableTag;
    /// <summary>The unsupported table operator or paint format, or zero.</summary>
    public int UnsupportedOperator;
    /// <summary>Deterministic diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgOpenTypeFontProfile()
    {
        Status = SvgOpenTypeInstanceStatus.InvalidArgument;
        Success = false;
        Axes = new System.Collections.Generic.List<SvgOpenTypeVariationAxis>();
        Palettes = new System.Collections.Generic.List<SvgOpenTypeColorPalette>();
        ColorVersion = -1;
        ColorGlyphs = new System.Collections.Generic.List<SvgOpenTypeColorGlyph>();
        RequiresColorCallback = false;
        RequiredTableTag = "";
        UnsupportedOperator = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}

/// <summary>Configures one bounded variable glyph instantiation.</summary>
public class SvgOpenTypeInstanceOptions
{
    /// <summary>The requested user coordinates; unspecified axes use their defaults.</summary>
    public System.Collections.Generic.List<SvgOpenTypeVariationCoordinate> Coordinates;
    /// <summary>The finite nonnegative output em size.</summary>
    public double EmSize;
    /// <summary>The selected zero-based CPAL palette.</summary>
    public int PaletteIndex;
    /// <summary>The bounded TrueType outline options.</summary>
    public SvgTrueTypeGlyphOptions TrueTypeOptions;
    /// <summary>The bounded CFF outline options.</summary>
    public SvgCffOutlineOptions CffOptions;
    /// <summary>The bounded profile options.</summary>
    public SvgOpenTypeProfileOptions ProfileOptions;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates default-coordinate, palette-zero, one-em options.</summary>
    public SvgOpenTypeInstanceOptions()
    {
        Coordinates = new System.Collections.Generic.List<SvgOpenTypeVariationCoordinate>();
        EmSize = 1;
        PaletteIndex = 0;
        TrueTypeOptions = new SvgTrueTypeGlyphOptions();
        CffOptions = new SvgCffOutlineOptions();
        ProfileOptions = new SvgOpenTypeProfileOptions();
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains variable design-unit and scaled glyph metrics.</summary>
public class SvgOpenTypeInstantiatedMetrics
{
    /// <summary>The horizontal advance after variation deltas.</summary>
    public double AdvanceWidth;
    /// <summary>The left side bearing after variation deltas.</summary>
    public double LeftSideBearing;
    /// <summary>Whether vertical metrics are available.</summary>
    public bool HasVerticalMetrics;
    /// <summary>The vertical advance after variation deltas.</summary>
    public double AdvanceHeight;
    /// <summary>The top side bearing after variation deltas.</summary>
    public double TopSideBearing;
    /// <summary>The scaled horizontal advance.</summary>
    public double ScaledAdvanceWidth;
    /// <summary>The scaled left side bearing.</summary>
    public double ScaledLeftSideBearing;
    /// <summary>The scaled vertical advance.</summary>
    public double ScaledAdvanceHeight;
    /// <summary>The scaled top side bearing.</summary>
    public double ScaledTopSideBearing;

    internal SvgOpenTypeInstantiatedMetrics()
    {
        AdvanceWidth = 0;
        LeftSideBearing = 0;
        HasVerticalMetrics = false;
        AdvanceHeight = 0;
        TopSideBearing = 0;
        ScaledAdvanceWidth = 0;
        ScaledLeftSideBearing = 0;
        ScaledAdvanceHeight = 0;
        ScaledTopSideBearing = 0;
    }
}

/// <summary>Contains one instantiated variable glyph and optional color paints.</summary>
public class SvgOpenTypeGlyphInstance
{
    /// <summary>The stable processing outcome.</summary>
    public SvgOpenTypeInstanceStatus Status;
    /// <summary>Whether metrics and outline instantiation completed.</summary>
    public bool Success;
    /// <summary>The requested glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The clamped normalized coordinates in axis order.</summary>
    public System.Collections.Generic.List<SvgOpenTypeNormalizedCoordinate> Coordinates;
    /// <summary>The instantiated metrics.</summary>
    public SvgOpenTypeInstantiatedMetrics? Metrics;
    /// <summary>The instantiated TrueType outline, or <c>null</c>.</summary>
    public SvgTrueTypeGlyphOutline? TrueTypeOutline;
    /// <summary>The instantiated CFF or CFF2 outline, or <c>null</c>.</summary>
    public SvgCffGlyphOutline? CffOutline;
    /// <summary>The selected color glyph layers, or <c>null</c>.</summary>
    public SvgOpenTypeColorGlyph? ColorGlyph;
    /// <summary>The exact table requiring a caller, or an empty string.</summary>
    public string RequiredTableTag;
    /// <summary>The unsupported operator or paint format, or zero.</summary>
    public int UnsupportedOperator;
    /// <summary>Deterministic diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgOpenTypeGlyphInstance(int glyphIndex)
    {
        Status = SvgOpenTypeInstanceStatus.InvalidArgument;
        Success = false;
        GlyphIndex = glyphIndex;
        Coordinates = new System.Collections.Generic.List<SvgOpenTypeNormalizedCoordinate>();
        Metrics = null;
        TrueTypeOutline = null;
        CffOutline = null;
        ColorGlyph = null;
        RequiredTableTag = "";
        UnsupportedOperator = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}
