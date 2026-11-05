namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgRasterizer"/>.</summary>
public interface ISvgRasterizer
{
    /// <summary>Rasterizes a fill with deterministic defaults and replacement coverage.</summary>
    /// <param name="path">The valid path in user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="color">The premultiplied fill color.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <returns>The completed scan conversion or a stable failure.</returns>
    SvgRasterResult FillWithPathAndSurfaceAndColorAndFillRule(SvgPathData path, SvgPixelBuffer surface, SvgPremultipliedRgba8 color, SvgFillRule fillRule);
    /// <summary>Rasterizes a fill with explicit transform, clipping, sampling, and work limits.</summary>
    /// <param name="path">The valid path in user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="color">The premultiplied fill color.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <param name="options">The bounded scan-conversion options.</param>
    /// <returns>The completed scan conversion or a stable failure.</returns>
    /// <remarks>Pixels with nonzero coverage are replaced by the coverage-scaled color. Pixels with zero coverage and bytes outside the active surface remain unchanged.</remarks>
    SvgRasterResult FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(SvgPathData path, SvgPixelBuffer surface, SvgPremultipliedRgba8 color, SvgFillRule fillRule, SvgRasterOptions options);
}
