namespace svg;

/// <summary>Builds deterministic nonzero-fill stroke outlines.</summary>
public class SvgStrokeOutliner : ISvgStrokeOutliner
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStrokeOutliner()
    {
    }

    /// <summary>Constructs a closed outline for a path and explicit stroke style.</summary>
    /// <param name="path">The valid source path.</param><param name="style">The stroke geometry and bounds.</param>
    /// <returns>The outline and dash placements or a stable failure.</returns>
    public SvgStrokeOutlineResult Create(SvgPathData path, SvgStrokeStyle style)
    {
        if (!new SvgStrokeOutliner().Valid(path, style)) return new SvgStrokeOutliner().Failure(SvgStrokeError.InvalidArgument);
        SvgStrokeDashPatternResult pattern = new SvgStrokeDashes().Normalize(style.DashArray, style.DashOffset);
        if (!pattern.Success) return new SvgStrokeOutliner().Failure(pattern.Error);
        SvgPathNormalizeResult normalized = path.Normalize();
        if (!normalized.Success) return new SvgStrokeOutliner().Failure(new SvgStrokeOutliner().Map(normalized.Error));
        SvgStrokeFlattenState flatten = new SvgStrokeFlattenState(normalized.PathData, style);
        SvgStrokeError flattenError = flatten.Run();
        if (flattenError != SvgStrokeError.None) return new SvgStrokeOutliner().Failure(flattenError);
        SvgStrokeDashState dash = new SvgStrokeDashState(flatten.Polylines, pattern, style.MaximumDashFragments);
        SvgStrokeError dashError = dash.Run();
        if (dashError != SvgStrokeError.None) return new SvgStrokeOutliner().Failure(dashError);
        SvgStrokeOutlineState outline = new SvgStrokeOutlineState(dash.Pieces, style);
        SvgStrokeError outlineError = outline.Run();
        if (outlineError != SvgStrokeError.None) return new SvgStrokeOutliner().Failure(outlineError);
        return new SvgStrokeOutlineResult(true, SvgStrokeError.None, outline.Builder.Build(), pattern, dash.Placements);
    }

    internal SvgStrokeOutlineResult Failure(SvgStrokeError error) { return new SvgStrokeOutlineResult(false, error, null, null, null); }
bool Valid(SvgPathData? path, SvgStrokeStyle? style)
    {
        if (path == null || style == null || !SvgFiniteMath.IsFinite(style.Width) || style.Width < 0 || !SvgFiniteMath.IsFinite(style.MiterLimit) || style.MiterLimit < 1) return false;
        if (style.LineCap != SvgStrokeLineCap.Butt && style.LineCap != SvgStrokeLineCap.Round && style.LineCap != SvgStrokeLineCap.Square) return false;
        if (style.LineJoin != SvgStrokeLineJoin.Miter && style.LineJoin != SvgStrokeLineJoin.MiterClip && style.LineJoin != SvgStrokeLineJoin.Round && style.LineJoin != SvgStrokeLineJoin.Bevel && style.LineJoin != SvgStrokeLineJoin.Arcs) return false;
        return style.DashArray != null && style.DashArray.Count <= style.MaximumDashFragments && SvgFiniteMath.IsFinite(style.DashOffset) && SvgFiniteMath.IsFinite(style.Tolerance) && style.Tolerance > 0 && style.MaximumSubdivisionDepth >= 0 && style.MaximumFlattenedSegments > 0 && style.MaximumDashFragments > 0 && style.MaximumOutputCommands > 0;
    }
SvgStrokeError Map(SvgPathNormalizeError error) { if (error == SvgPathNormalizeError.InvalidPath) return SvgStrokeError.InvalidPath; if (error == SvgPathNormalizeError.NonFiniteResult) return SvgStrokeError.NonFiniteResult; if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgStrokeError.SegmentLimitExceeded; return SvgStrokeError.InvalidArgument; }
}
