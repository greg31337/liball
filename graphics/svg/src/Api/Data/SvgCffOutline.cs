namespace svg;

/// <summary>Configures bounded CFF and CFF2 CharString expansion.</summary>
public class SvgCffOutlineOptions
{
    /// <summary>The shared font limits, including glyph and expanded-point bounds.</summary>
    public SvgFontLimits Limits;
    /// <summary>The maximum number of operands retained on the CharString stack.</summary>
    public int MaximumOperands;
    /// <summary>The maximum nested local and global subroutine depth.</summary>
    public int MaximumSubroutineDepth;
    /// <summary>The maximum total subroutine calls for one glyph.</summary>
    public int MaximumSubroutineCalls;
    /// <summary>The maximum entries accepted in either subroutine INDEX.</summary>
    public int MaximumSubroutines;
    /// <summary>The normalized variation coordinates in <c>fvar</c> axis order.</summary>
    public System.Collections.Generic.List<double> VariationCoordinates;

    /// <summary>Creates bounded defaults matching the Type 2 stack and recursion limits.</summary>
    public SvgCffOutlineOptions()
    {
        Limits = new SvgFontLimits();
        MaximumOperands = 513;
        MaximumSubroutineDepth = 10;
        MaximumSubroutineCalls = 10000;
        MaximumSubroutines = 65535;
        VariationCoordinates = new System.Collections.Generic.List<double>();
    }
}

/// <summary>Contains one decoded cubic CFF or CFF2 glyph outline.</summary>
public class SvgCffGlyphOutline
{
    /// <summary>The zero-based glyph index.</summary>
    public int GlyphIndex;
    /// <summary>Whether the source table uses CFF2.</summary>
    public bool Cff2;
    /// <summary>Whether the glyph contains no drawing commands.</summary>
    public bool Empty;
    /// <summary>The number of contours emitted into the path.</summary>
    public int ContourCount;
    /// <summary>The number of local and global subroutine calls executed.</summary>
    public int SubroutineCalls;
    /// <summary>Whether hint operators were validated and ignored.</summary>
    public bool HasHints;
    /// <summary>The decoded cubic SVG path in font design coordinates.</summary>
    public SvgPathData? Path;

    internal SvgCffGlyphOutline(int glyphIndex, bool cff2)
    {
        GlyphIndex = glyphIndex;
        Cff2 = cff2;
        Empty = true;
        ContourCount = 0;
        SubroutineCalls = 0;
        HasHints = false;
        Path = null;
    }
}

/// <summary>Contains a completed, failed, or explicitly delegated CFF glyph decode.</summary>
public class SvgCffGlyphResult
{
    /// <summary>Whether the outline was decoded completely.</summary>
    public bool Success;
    /// <summary>The stable decoding outcome.</summary>
    public SvgCffError Error;
    /// <summary>The decoded outline, or <c>null</c>.</summary>
    public SvgCffGlyphOutline? Outline;
    /// <summary>Whether a capable caller should decode this glyph as a fallback.</summary>
    public bool RequiresFallback;
    /// <summary>The stable fallback classification.</summary>
    public SvgCffFallbackReason FallbackReason;
    /// <summary>The unsupported one-byte or escaped operator code, or zero.</summary>
    public int UnsupportedOperator;

    internal SvgCffGlyphResult(bool success, SvgCffError error, SvgCffGlyphOutline? outline, bool requiresFallback, SvgCffFallbackReason fallbackReason, int unsupportedOperator)
    {
        Success = success;
        Error = error;
        Outline = outline;
        RequiresFallback = requiresFallback;
        FallbackReason = fallbackReason;
        UnsupportedOperator = unsupportedOperator;
    }
}
