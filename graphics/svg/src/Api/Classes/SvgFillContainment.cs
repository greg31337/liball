namespace svg;

/// <summary>Provides bounded nonzero and even-odd fill containment.</summary>
public class SvgFillContainment : ISvgFillContainment
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFillContainment()
    {
    }

    /// <summary>Classifies a point in path user coordinates using deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="fillRule">The fill rule.</param>
    /// <returns>The robust containment classification.</returns>
    public SvgFillContainmentResult ClassifyWithPathAndPointAndFillRule(SvgPathData path, SvgPoint point, SvgFillRule fillRule)
    {
        return new SvgFillContainment().ClassifyWithPathAndPointAndFillRuleAndOptions(path, point, fillRule, new SvgFillContainmentOptions());
    }

    /// <summary>Classifies a point using explicit tolerance, limits, and output-space transform.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The point in path or transformed output coordinates.</param>
    /// <param name="fillRule">The fill rule.</param>
    /// <param name="options">The bounded containment options.</param>
    /// <returns>The robust containment classification or a stable failure.</returns>
    public SvgFillContainmentResult ClassifyWithPathAndPointAndFillRuleAndOptions(SvgPathData path, SvgPoint point, SvgFillRule fillRule, SvgFillContainmentOptions options)
    {
        if (!new SvgFillContainment().Valid(path, point, fillRule, options)) return new SvgFillContainment().Failure(SvgFillContainmentError.InvalidArgument);
        SvgPathNormalizeResult normalized = path.Normalize();
        if (!normalized.Success) return new SvgFillContainment().Failure(new SvgFillContainment().Map(normalized.Error));
        SvgFillContainmentState state = new SvgFillContainmentState(normalized.PathData, point, fillRule, options);
        return state.Run();
    }

    internal SvgFillContainmentResult Failure(SvgFillContainmentError error)
    {
        return new SvgFillContainmentResult(false, error, SvgFillContainmentKind.Outside, false, 0, 0);
    }
bool Valid(SvgPathData? path, SvgPoint? point, SvgFillRule fillRule, SvgFillContainmentOptions? options)
    {
        if (path == null || point == null || options == null || !SvgFiniteMath.IsFinite(point.X) || !SvgFiniteMath.IsFinite(point.Y)) return false;
        if (fillRule != SvgFillRule.Nonzero && fillRule != SvgFillRule.EvenOdd) return false;
        if (!SvgFiniteMath.IsFinite(options.Tolerance) || options.Tolerance <= 0 || options.MaximumSubdivisionDepth < 0 || options.MaximumSegments < 1) return false;
        if (options.Transform == null) return true;
        SvgMatrix matrix = options.Transform;
        return SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
SvgFillContainmentError Map(SvgPathNormalizeError error)
    {
        if (error == SvgPathNormalizeError.InvalidPath) return SvgFillContainmentError.InvalidPath;
        if (error == SvgPathNormalizeError.NonFiniteResult) return SvgFillContainmentError.NonFiniteResult;
        if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgFillContainmentError.SegmentLimitExceeded;
        return SvgFillContainmentError.InvalidArgument;
    }
}
