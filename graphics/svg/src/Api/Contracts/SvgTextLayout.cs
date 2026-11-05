namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTextLayout"/>.</summary>
public interface ISvgTextLayout
{
    /// <summary>Positions shaped glyphs with horizontal alphabetic defaults.</summary>
    /// <param name="shaping">A successful baseline or callback shaping result.</param>
    /// <returns>The positioned glyphs and chunks.</returns>
    SvgTextLayoutResult LayoutWithShaping(SvgShapingResult shaping);
    /// <summary>Positions shaped glyphs with explicit SVG lists, spacing, baselines, and text length.</summary>
    /// <param name="shaping">A successful baseline or callback shaping result.</param>
    /// <param name="options">The bounded SVG text-positioning options.</param>
    /// <returns>The positioned glyphs and chunks.</returns>
    SvgTextLayoutResult LayoutWithShapingAndOptions(SvgShapingResult shaping, SvgTextLayoutOptions options);
}
