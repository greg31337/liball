namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTextPathLayout"/>.</summary>
public interface ISvgTextPathLayout
{
    /// <summary>Projects positioned text onto a caller-resolved referenced path.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="path">The referenced SVG path.</param>
    /// <returns>The projected text path glyphs.</returns>
    SvgTextPathResult LayoutWithTextAndPath(SvgTextLayoutResult text, SvgPathData path);
    /// <summary>Projects positioned text onto a caller-resolved referenced path.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="path">The referenced SVG path.</param>
    /// <param name="options">The start, calibration, method, side, overflow, and bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    SvgTextPathResult LayoutWithTextAndPathAndOptions(SvgTextLayoutResult text, SvgPathData path, SvgTextPathOptions options);
    /// <summary>Converts a referenced SVG shape and projects positioned text onto it.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="shape">The resolved referenced basic shape or path.</param>
    /// <param name="options">The start, calibration, method, side, overflow, and bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    SvgTextPathResult LayoutShape(SvgTextLayoutResult text, SvgShapeResult? shape, SvgTextPathOptions? options);
    /// <summary>Parses inline SVG path data and projects positioned text onto it.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="pathData">The complete inline SVG path data.</param>
    /// <param name="options">The start, calibration, method, side, overflow, parsing, and measurement bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    SvgTextPathResult LayoutInline(SvgTextLayoutResult text, string? pathData, SvgTextPathOptions? options);
}
