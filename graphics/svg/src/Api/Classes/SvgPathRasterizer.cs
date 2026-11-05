namespace svg;

/// <summary>Connects transformed SVG fill and stroke geometry to deterministic scan conversion.</summary>
public class SvgPathRasterizer : ISvgPathRasterizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathRasterizer()
    {
    }

    /// <summary>Paints one path using explicit colors, opacity, order, stroke geometry, and sampling hints.</summary>
    /// <param name="path">The valid path in local user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="options">The complete path-painting options.</param>
    /// <returns>The ordered fill and stroke results.</returns>
    public SvgPathRasterResult DrawWithPathAndSurfaceAndOptions(SvgPathData path, SvgPixelBuffer surface, SvgPathRasterOptions options)
    {
        SvgPathRasterResult result = new SvgPathRasterResult();
        if (!SvgPathRasterMath.Valid(path, surface, options))
        {
            result.Error = SvgPathRasterError.InvalidArgument;
            return result;
        }
        SvgPathNormalizeResult pathValidation = path.Normalize();
        if (!pathValidation.Success)
        {
            result.Error = SvgPathRasterError.InvalidPath;
            return result;
        }
        SvgRasterOptions? raster = SvgPathRasterMath.EffectiveRaster(options.RasterOptions, options.ShapeRendering);
        SvgStrokeRasterPreparation? preparedStroke = null;
        if (options.StrokeColor != null && options.StrokeOpacity > 0)
        {
            SvgStrokeStyle strokeStyle = SvgPathRasterMath.EffectiveStroke(options.StrokeStyle, options.ShapeRendering);
            preparedStroke = SvgPathRasterMath.PrepareStroke(path, strokeStyle, options.VectorEffect, (raster ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform);
            result.StrokeError = preparedStroke.Error;
            if (preparedStroke.Error != SvgStrokeError.None)
            {
                result.Error = SvgPathRasterError.StrokeFailed;
                return result;
            }
        }
        int index = 0;
        while (index < options.PaintOrder.Count)
        {
            SvgPaintOrderComponent component = options.PaintOrder[index];
            if (component == SvgPaintOrderComponent.Fill && options.FillColor != null && options.FillOpacity > 0)
            {
                SvgPremultipliedRgba8 color = SvgPathRasterMath.ApplyOpacity(options.FillColor, options.FillOpacity);
                SvgRasterResult fill = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(path, surface, color, options.FillRule, (SvgPathRasterMath.CopyRaster(raster) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                result.FillResult = fill;
                if (!fill.Success)
                {
                    result.Error = SvgPathRasterError.FillFailed;
                    return result;
                }
                SvgPathRasterMath.Add(fill, result);
            }
            else if (component == SvgPaintOrderComponent.Stroke && options.StrokeColor != null && options.StrokeOpacity > 0)
            {
                SvgRasterOptions? strokeRaster = SvgPathRasterMath.CopyRaster(raster);
                (strokeRaster ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform = (preparedStroke ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform;
                SvgPremultipliedRgba8 color = SvgPathRasterMath.ApplyOpacity(options.StrokeColor, options.StrokeOpacity);
                SvgRasterResult stroke = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions((preparedStroke.Outline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), surface, color, SvgFillRule.Nonzero, strokeRaster);
                result.StrokeResult = stroke;
                if (!stroke.Success)
                {
                    result.Error = SvgPathRasterError.StrokeFailed;
                    return result;
                }
                SvgPathRasterMath.Add(stroke, result);
            }
            index = index + 1;
        }
        result.Success = true;
        return result;
    }

    /// <summary>Paints one normalized scene path using caller-supplied premultiplied colors.</summary>
    /// <param name="node">The normalized scene path carrying transform and computed painting semantics.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="fillColor">The fill color used when the node has a fill resource.</param>
    /// <param name="strokeColor">The stroke color used when the node has a stroke resource.</param>
    /// <param name="rasterOptions">The clip and scan-conversion limits; the node supplies the transform and rendering hint.</param>
    /// <returns>The ordered fill and stroke results.</returns>
    public SvgPathRasterResult DrawWithNodeAndSurfaceAndFillColorAndStrokeColorAndRasterOptions(SvgSceneNode node, SvgPixelBuffer surface, SvgPremultipliedRgba8 fillColor, SvgPremultipliedRgba8 strokeColor, SvgRasterOptions rasterOptions)
    {
        if (node == null || node.Kind != SvgSceneNodeKind.Path || node.Path == null)
        {
            SvgPathRasterResult failure = new SvgPathRasterResult();
            failure.Error = SvgPathRasterError.InvalidArgument;
            return failure;
        }
        SvgPathRasterOptions options = new SvgPathRasterOptions();
        options.FillColor = node.Fill == null ? null : fillColor;
        options.StrokeColor = node.Stroke == null ? null : strokeColor;
        options.FillRule = node.Path.FillRule;
        options.FillOpacity = node.FillOpacity;
        options.StrokeOpacity = node.StrokeOpacity;
        options.PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>(node.PaintOrder);
        options.StrokeStyle = node.Path.StrokeStyle;
        options.VectorEffect = node.Path.VectorEffect;
        options.ShapeRendering = node.Path.ShapeRendering;
        options.RasterOptions = (SvgPathRasterMath.CopyRaster(rasterOptions) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (options.RasterOptions != null) options.RasterOptions.Transform = node.AbsoluteTransform;
        return new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions((node.Path.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), surface, options);
    }
}
