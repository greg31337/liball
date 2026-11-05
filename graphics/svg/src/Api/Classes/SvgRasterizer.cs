namespace svg;

/// <summary>Rasterizes flattened SVG fills into premultiplied RGBA8 surfaces.</summary>
public class SvgRasterizer : ISvgRasterizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgRasterizer()
    {
    }

    /// <summary>Rasterizes a fill with deterministic defaults and replacement coverage.</summary>
    /// <param name="path">The valid path in user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="color">The premultiplied fill color.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <returns>The completed scan conversion or a stable failure.</returns>
    public SvgRasterResult FillWithPathAndSurfaceAndColorAndFillRule(SvgPathData path, SvgPixelBuffer surface, SvgPremultipliedRgba8 color, SvgFillRule fillRule)
    {
        return new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(path, surface, color, fillRule, new SvgRasterOptions());
    }

    /// <summary>Rasterizes a fill with explicit transform, clipping, sampling, and work limits.</summary>
    /// <param name="path">The valid path in user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="color">The premultiplied fill color.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <param name="options">The bounded scan-conversion options.</param>
    /// <returns>The completed scan conversion or a stable failure.</returns>
    /// <remarks>Pixels with nonzero coverage are replaced by the coverage-scaled color. Pixels with zero coverage and bytes outside the active surface remain unchanged.</remarks>
    public SvgRasterResult FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(SvgPathData path, SvgPixelBuffer surface, SvgPremultipliedRgba8 color, SvgFillRule fillRule, SvgRasterOptions options)
    {
        if (!SvgRasterMath.Valid(path, surface, color, fillRule, options)) return SvgRasterMath.Failure(SvgRasterError.InvalidArgument);
        SvgPathNormalizeResult normalized = path.Normalize();
        if (!normalized.Success) return SvgRasterMath.Failure(SvgRasterMath.Map(normalized.Error));
        SvgRasterState state = new SvgRasterState(normalized.PathData, surface, color, fillRule, options);
        return state.Run();
    }
}
