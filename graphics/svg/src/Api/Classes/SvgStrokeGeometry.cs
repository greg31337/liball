namespace svg;

/// <summary>Provides dashed transformed stroke containment and bounds.</summary>
public class SvgStrokeGeometry : ISvgStrokeGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStrokeGeometry()
    {
    }

    /// <summary>Tests containment in transformed output coordinates.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="point">The finite output-space query point.</param>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The stroke-containment result or a stable failure.</returns>
    public SvgStrokeContainmentResult Contains(SvgPathData path, SvgPoint? point, SvgStrokeGeometryOptions options)
    {
        if (point == null || !new SvgStrokeGeometry().Valid(path, point, options)) return new SvgStrokeGeometry().ContainmentFailure(SvgStrokeError.InvalidArgument);
        SvgStrokePrepared prepared = new SvgStrokeGeometry().Prepare(path, options);
        if (prepared.Error != SvgStrokeError.None) return new SvgStrokeGeometry().ContainmentFailure(prepared.Error);
        SvgFillContainmentOptions fillOptions = new SvgStrokeGeometry().FillOptions(options.Style);
        fillOptions.Transform = prepared.OutlineTransform;
        SvgFillContainmentResult contained = (prepared.Outline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ClassifyFillWithOptions(point, SvgFillRule.Nonzero, fillOptions);
        if (!contained.Success) return new SvgStrokeGeometry().ContainmentFailure(new SvgStrokeGeometry().MapWithSvgFillContainmentError(contained.Error));
        return new SvgStrokeContainmentResult(true, SvgStrokeError.None, contained.Contains);
    }

    /// <summary>Computes bounds in transformed output coordinates.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The finite stroke bounds or a stable failure.</returns>
    public SvgStrokeBoundsResult GetBounds(SvgPathData path, SvgStrokeGeometryOptions options)
    {
        if (!new SvgStrokeGeometry().Valid(path, null, options)) return new SvgStrokeGeometry().BoundsFailure(SvgStrokeError.InvalidArgument);
        SvgStrokePrepared prepared = new SvgStrokeGeometry().Prepare(path, options);
        if (prepared.Error != SvgStrokeError.None) return new SvgStrokeGeometry().BoundsFailure(prepared.Error);
        SvgBoundsAccumulator accumulator = new SvgBoundsAccumulator();
        int index = 0;
        while (index < (prepared.Outline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPoint point = new SvgPoint(prepared.Outline.Commands[index].EndX, prepared.Outline.Commands[index].EndY);
            if (prepared.OutlineTransform != null)
            {
                SvgPointTransformResult transformed = prepared.OutlineTransform.TransformPoint(point);
                if (!transformed.Success) return new SvgStrokeGeometry().BoundsFailure(new SvgStrokeGeometry().MapWithSvgGeometryError(transformed.Error));
                point = (transformed.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            }
            accumulator.Add(point);
            index = index + 1;
        }
        SvgRectOperationResult bounds = accumulator.Finish();
        if (!bounds.Success) return new SvgStrokeGeometry().BoundsFailure(new SvgStrokeGeometry().MapWithSvgGeometryError(bounds.Error));
        return new SvgStrokeBoundsResult(true, SvgStrokeError.None, bounds.Rectangle);
    }

    internal SvgStrokeContainmentResult ContainmentFailure(SvgStrokeError error)
    {
        return new SvgStrokeContainmentResult(false, error, false);
    }

    internal SvgStrokeBoundsResult BoundsFailure(SvgStrokeError error)
    {
        return new SvgStrokeBoundsResult(false, error, null);
    }
SvgStrokePrepared Prepare(SvgPathData path, SvgStrokeGeometryOptions options)
    {
        SvgPathData centerline = path;
        SvgMatrix? outlineTransform = options.Transform;
        if (options.VectorEffect == SvgStrokeVectorEffect.NonScalingStroke && options.Transform != null)
        {
            SvgPathTransformOptions transformOptions = new SvgPathTransformOptions();
            transformOptions.MaximumCommands = options.Style.MaximumOutputCommands;
            SvgPathTransformResult transformed = path.TransformWithOptions(options.Transform, transformOptions);
            if (!transformed.Success) return new SvgStrokePrepared(new SvgStrokeGeometry().MapWithSvgPathTransformError(transformed.Error), null, null);
            centerline = (transformed.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            outlineTransform = null;
        }
        SvgStrokeOutlineResult outline = (centerline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StrokeOutline(options.Style);
        if (!outline.Success) return new SvgStrokePrepared(outline.Error, null, null);
        return new SvgStrokePrepared(SvgStrokeError.None, outline.PathData, outlineTransform);
    }
SvgFillContainmentOptions FillOptions(SvgStrokeStyle style)
    {
        SvgFillContainmentOptions options = new SvgFillContainmentOptions();
        options.Tolerance = style.Tolerance;
        options.MaximumSubdivisionDepth = style.MaximumSubdivisionDepth;
        options.MaximumSegments = style.MaximumOutputCommands;
        return options;
    }
bool Valid(SvgPathData? path, SvgPoint? point, SvgStrokeGeometryOptions? options)
    {
        if (path == null || options == null || options.Style == null) return false;
        if (point != null && (!SvgFiniteMath.IsFinite(point.X) || !SvgFiniteMath.IsFinite(point.Y))) return false;
        if (options.VectorEffect != SvgStrokeVectorEffect.None && options.VectorEffect != SvgStrokeVectorEffect.NonScalingStroke) return false;
        if (options.Transform == null) return true;
        SvgMatrix matrix = options.Transform;
        return SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
SvgStrokeError MapWithSvgFillContainmentError(SvgFillContainmentError error)
    {
        if (error == SvgFillContainmentError.InvalidPath) return SvgStrokeError.InvalidPath;
        if (error == SvgFillContainmentError.NonFiniteResult) return SvgStrokeError.NonFiniteResult;
        if (error == SvgFillContainmentError.SegmentLimitExceeded) return SvgStrokeError.SegmentLimitExceeded;
        if (error == SvgFillContainmentError.SubdivisionLimitExceeded) return SvgStrokeError.SubdivisionLimitExceeded;
        return SvgStrokeError.InvalidArgument;
    }
SvgStrokeError MapWithSvgPathTransformError(SvgPathTransformError error)
    {
        if (error == SvgPathTransformError.InvalidPath) return SvgStrokeError.InvalidPath;
        if (error == SvgPathTransformError.NonFiniteResult) return SvgStrokeError.NonFiniteResult;
        if (error == SvgPathTransformError.CommandLimitExceeded) return SvgStrokeError.OutputLimitExceeded;
        return SvgStrokeError.InvalidArgument;
    }
SvgStrokeError MapWithSvgGeometryError(SvgGeometryError error)
    {
        return error == SvgGeometryError.InvalidArgument ? SvgStrokeError.InvalidArgument : SvgStrokeError.NonFiniteResult;
    }
}
