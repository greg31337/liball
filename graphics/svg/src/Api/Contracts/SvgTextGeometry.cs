namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTextGeometry"/>.</summary>
public interface ISvgTextGeometry
{
    /// <summary>Converts planar positioned text into bounded glyph and decoration geometry.</summary>
    /// <param name="layout">The successful planar text layout.</param>
    /// <param name="options">The fonts, decorations, stroke, and security bounds.</param>
    /// <returns>The converted geometry, extraction, and bounds.</returns>
    SvgTextGeometryResult BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(SvgTextLayoutResult? layout, SvgTextGeometryOptions options);
    /// <summary>Converts projected textPath glyphs into bounded glyph and decoration geometry.</summary>
    /// <param name="layout">The successful textPath layout.</param>
    /// <param name="options">The fonts, decorations, stroke, and security bounds.</param>
    /// <returns>The converted geometry, extraction, and bounds.</returns>
    SvgTextGeometryResult BuildWithSvgTextPathResultAndSvgTextGeometryOptions(SvgTextPathResult? layout, SvgTextGeometryOptions options);
    /// <summary>Hit-tests converted text geometry.</summary>
    /// <param name="geometry">The successful converted text geometry.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The query mode and bounded fill options.</param>
    /// <returns>The topmost matching glyph or cluster.</returns>
    SvgTextHitTestResult HitTest(SvgTextGeometryResult? geometry, SvgPoint? point, SvgTextHitTestOptions? options);
}
