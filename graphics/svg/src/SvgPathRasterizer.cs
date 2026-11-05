namespace svg;

internal class SvgStrokeRasterPreparation
{
    internal SvgStrokeError Error;
    internal SvgPathData? Outline;
    internal SvgMatrix? Transform;

    internal SvgStrokeRasterPreparation(SvgStrokeError error, SvgPathData? outline, SvgMatrix? transform)
    {
        Error = error;
        Outline = outline;
        Transform = transform;
    }
}

internal static class SvgPathRasterMath
{
    internal static bool Valid(SvgPathData? path, SvgPixelBuffer? surface, SvgPathRasterOptions? options)
    {
        if (path == null || surface == null || options == null || options.RasterOptions == null || options.StrokeStyle == null || options.PaintOrder == null) return false;
        if (options.FillRule != SvgFillRule.Nonzero && options.FillRule != SvgFillRule.EvenOdd) return false;
        if (!SvgFiniteMath.IsFinite(options.FillOpacity) || options.FillOpacity < 0 || options.FillOpacity > 1) return false;
        if (!SvgFiniteMath.IsFinite(options.StrokeOpacity) || options.StrokeOpacity < 0 || options.StrokeOpacity > 1) return false;
        if (options.VectorEffect != SvgStrokeVectorEffect.None && options.VectorEffect != SvgStrokeVectorEffect.NonScalingStroke) return false;
        if (options.ShapeRendering != SvgShapeRendering.Auto && options.ShapeRendering != SvgShapeRendering.OptimizeSpeed && options.ShapeRendering != SvgShapeRendering.CrispEdges && options.ShapeRendering != SvgShapeRendering.GeometricPrecision) return false;
        if (!ValidColor(options.FillColor) || !ValidColor(options.StrokeColor) || !ValidOrder(options.PaintOrder)) return false;
        SvgStrokeOutlineResult strokeValidation = EmptyPath().StrokeOutline(options.StrokeStyle);
        if (!strokeValidation.Success) return false;
        SvgPremultipliedRgba8 validationColor = new SvgPremultipliedRgba8(0, 0, 0, 0);
        SvgRasterResult validation = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(EmptyPath(), surface, validationColor, SvgFillRule.Nonzero, (CopyRaster(options.RasterOptions) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        return validation.Success;
    }

    internal static SvgRasterOptions? EffectiveRaster(SvgRasterOptions source, SvgShapeRendering hint)
    {
        SvgRasterOptions? result = CopyRaster(source);
        if (hint == SvgShapeRendering.OptimizeSpeed)
        {
            (result ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis = 1;
            if (result.Tolerance < 1) result.Tolerance = 1;
        }
        else if (hint == SvgShapeRendering.CrispEdges) (result ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis = 1;
        else if (hint == SvgShapeRendering.GeometricPrecision)
        {
            (result ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SamplesPerAxis = 8;
            if (result.Tolerance > 0.05) result.Tolerance = 0.05;
        }
        return result;
    }

    internal static SvgRasterOptions? CopyRaster(SvgRasterOptions? source)
    {
        if (source == null) return null;
        SvgRasterOptions result = new SvgRasterOptions();
        result.Tolerance = source.Tolerance;
        result.Transform = source.Transform;
        result.Clip = source.Clip;
        result.SamplesPerAxis = source.SamplesPerAxis;
        result.MaximumSubdivisionDepth = source.MaximumSubdivisionDepth;
        result.MaximumEdges = source.MaximumEdges;
        result.MaximumWork = source.MaximumWork;
        return result;
    }

    internal static SvgStrokeStyle EffectiveStroke(SvgStrokeStyle source, SvgShapeRendering hint)
    {
        SvgStrokeStyle result = new SvgStrokeStyle();
        result.Width = source.Width;
        result.LineCap = source.LineCap;
        result.LineJoin = source.LineJoin;
        result.MiterLimit = source.MiterLimit;
        result.DashArray = new System.Collections.Generic.List<double>(source.DashArray);
        result.DashOffset = source.DashOffset;
        result.Tolerance = source.Tolerance;
        result.MaximumSubdivisionDepth = source.MaximumSubdivisionDepth;
        result.MaximumFlattenedSegments = source.MaximumFlattenedSegments;
        result.MaximumDashFragments = source.MaximumDashFragments;
        result.MaximumOutputCommands = source.MaximumOutputCommands;
        if (hint == SvgShapeRendering.OptimizeSpeed && result.Tolerance < 1) result.Tolerance = 1;
        else if (hint == SvgShapeRendering.GeometricPrecision && result.Tolerance > 0.05) result.Tolerance = 0.05;
        return result;
    }

    internal static SvgPremultipliedRgba8 ApplyOpacity(SvgPremultipliedRgba8 color, double opacity)
    {
        byte red = Scale(color.Red, opacity);
        byte green = Scale(color.Green, opacity);
        byte blue = Scale(color.Blue, opacity);
        byte alpha = Scale(color.Alpha, opacity);
        return new SvgPremultipliedRgba8(red, green, blue, alpha);
    }

    internal static SvgStrokeRasterPreparation PrepareStroke(SvgPathData path, SvgStrokeStyle style, SvgStrokeVectorEffect vectorEffect, SvgMatrix? transform)
    {
        SvgPathData centerline = path;
        SvgMatrix? outlineTransform = transform;
        if (vectorEffect == SvgStrokeVectorEffect.NonScalingStroke && transform != null)
        {
            SvgPathTransformOptions transformOptions = new SvgPathTransformOptions();
            transformOptions.MaximumCommands = style.MaximumOutputCommands;
            SvgPathTransformResult transformed = path.TransformWithOptions(transform, transformOptions);
            if (!transformed.Success) return new SvgStrokeRasterPreparation(Map(transformed.Error), null, null);
            centerline = (transformed.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            outlineTransform = null;
        }
        SvgStrokeOutlineResult outline = (centerline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StrokeOutline(style);
        if (!outline.Success) return new SvgStrokeRasterPreparation(outline.Error, null, null);
        return new SvgStrokeRasterPreparation(SvgStrokeError.None, outline.PathData, outlineTransform);
    }

    internal static void Add(SvgRasterResult operation, SvgPathRasterResult result)
    {
        result.OperationsCompleted = result.OperationsCompleted + 1;
        if (operation.WorkUnits > long.MaxValue - result.WorkUnits) result.WorkUnits = long.MaxValue;
        else result.WorkUnits = result.WorkUnits + operation.WorkUnits;
        result.PixelsWritten = result.PixelsWritten + operation.PixelsWritten;
    }

    static bool ValidColor(SvgPremultipliedRgba8? color)
    {
        return color == null || color.Red <= color.Alpha && color.Green <= color.Alpha && color.Blue <= color.Alpha;
    }

    static bool ValidOrder(System.Collections.Generic.List<SvgPaintOrderComponent> order)
    {
        if (order.Count != 3) return false;
        bool fill = false;
        bool stroke = false;
        bool markers = false;
        int index = 0;
        while (index < order.Count)
        {
            if (order[index] == SvgPaintOrderComponent.Fill && !fill) fill = true;
            else if (order[index] == SvgPaintOrderComponent.Stroke && !stroke) stroke = true;
            else if (order[index] == SvgPaintOrderComponent.Markers && !markers) markers = true;
            else return false;
            index = index + 1;
        }
        return fill && stroke && markers;
    }

    static SvgPathData EmptyPath()
    {
        return new SvgPathData(new System.Collections.Generic.List<SvgPathCommand>(), "", new System.Collections.Generic.List<SvgPathParseIssue>());
    }

    static byte Scale(byte channel, double opacity)
    {
        return (byte)System.Math.Floor(channel * opacity + 0.5);
    }

    static SvgStrokeError Map(SvgPathTransformError error)
    {
        if (error == SvgPathTransformError.InvalidPath) return SvgStrokeError.InvalidPath;
        if (error == SvgPathTransformError.NonFiniteResult) return SvgStrokeError.NonFiniteResult;
        if (error == SvgPathTransformError.CommandLimitExceeded) return SvgStrokeError.OutputLimitExceeded;
        return SvgStrokeError.InvalidArgument;
    }
}
