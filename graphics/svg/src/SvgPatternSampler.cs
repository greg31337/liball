namespace svg;

internal class SvgPatternSampleContext
{
    internal System.Collections.Generic.List<SvgSceneResource> ActivePatterns;
    internal System.Collections.Generic.List<SvgSceneNode> ActiveNodes;
    internal int NodesVisited;

    internal SvgPatternSampleContext()
    {
        ActivePatterns = new System.Collections.Generic.List<SvgSceneResource>();
        ActiveNodes = new System.Collections.Generic.List<SvgSceneNode>();
        NodesVisited = 0;
    }
}

internal class SvgPatternTileResult
{
    internal bool Success;
    internal SvgPaintSampleError Error;
    internal SvgPixelBuffer? Buffer;
    internal SvgMatrix? PatternToPixels;
    internal SvgMatrix? PixelsToPattern;

    internal SvgPatternTileResult(bool success, SvgPaintSampleError error, SvgPixelBuffer? buffer, SvgMatrix? patternToPixels, SvgMatrix? pixelsToPattern)
    {
        Success = success;
        Error = error;
        Buffer = buffer;
        PatternToPixels = patternToPixels;
        PixelsToPattern = pixelsToPattern;
    }
}

internal static class SvgPatternPaintSampler
{
    internal static SvgPaintSampleResult Sample(SvgSceneResource resource, SvgPoint? point, SvgPaintSampleOptions options, SvgPatternSampleContext? context)
    {
        if (!ValidOptions(options) || context == null) return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        if (Contains(context.ActivePatterns, resource)) return SvgPaintSampleMath.Failure(SvgPaintSampleError.PatternRecursionDetected);
        if (context.ActivePatterns.Count >= options.MaximumPatternDepth) return SvgPaintSampleMath.Failure(SvgPaintSampleError.PatternDepthLimitExceeded);
        context.ActivePatterns.Add(resource);
        SvgPatternTileRenderer renderer = new SvgPatternTileRenderer(resource.PatternPaint, options, context);
        SvgPatternTileResult tile = renderer.Render();
        context.ActivePatterns.RemoveAt(context.ActivePatterns.Count - 1);
        if (!tile.Success) return SvgPaintSampleMath.Failure(tile.Error);
        return SampleTile(resource.PatternPaint, tile, (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), options);
    }

    static SvgPaintSampleResult SampleTile(SvgScenePatternPaintData? pattern, SvgPatternTileResult tile, SvgPoint point, SvgPaintSampleOptions options)
    {
        SvgMatrixOperationResult inverse = (pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform.Invert();
        if (!inverse.Success) return SvgPaintSampleMath.Failure(inverse.Error == SvgGeometryError.SingularMatrix ? SvgPaintSampleError.SingularTransform : SvgPaintSampleError.NonFiniteResult);
        SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(point);
        if (!local.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        double x = Repeat((local.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (pattern.TileBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, pattern.TileBounds.Width);
        double y = Repeat(local.Point.Y, pattern.TileBounds.Y, pattern.TileBounds.Height);
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        SvgPointTransformResult pixelPoint = (tile.PatternToPixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(x, y));
        if (!pixelPoint.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        int pixelX = PixelIndex((pixelPoint.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (tile.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width);
        int pixelY = PixelIndex(pixelPoint.Point.Y, tile.Buffer.Height);
        SvgPixelReadResult read = tile.Buffer.Read(pixelX, pixelY);
        if (!read.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.PatternRenderFailed);
        SvgPaintSampleResult result = new SvgPaintSampleResult();
        result.Success = true;
        result.HasPaint = true;
        result.Pixel = Scale(read.Pixel, options.Opacity);
        return result;
    }

    static double Repeat(double value, double origin, double size)
    {
        double offset = value - origin;
        double quotient = System.Math.Floor(offset / size);
        double repeated = offset - quotient * size;
        if (repeated < 0) repeated = repeated + size;
        if (repeated >= size) repeated = 0;
        return origin + repeated;
    }

    static int PixelIndex(double coordinate, int length)
    {
        if (coordinate <= 0) return 0;
        if (coordinate >= length) return length - 1;
        return (int)System.Math.Floor(coordinate);
    }

    static SvgPremultipliedRgba8 Scale(SvgPremultipliedRgba8? pixel, double opacity)
    {
        return new SvgPremultipliedRgba8(Byte((pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * opacity), Byte(pixel.Green * opacity), Byte(pixel.Blue * opacity), Byte(pixel.Alpha * opacity));
    }

    static byte Byte(double value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    static bool ValidOptions(SvgPaintSampleOptions options)
    {
        return options.RasterLimits != null && options.MaximumPatternDepth > 0 && options.MaximumPatternNodes > 0 && options.PatternSamplesPerAxis > 0 && options.PatternSamplesPerAxis <= 16;
    }

    static bool Contains(System.Collections.Generic.List<SvgSceneResource> resources, SvgSceneResource resource)
    {
        int index = 0;
        while (index < resources.Count)
        {
            if (object.ReferenceEquals(resources[index], resource)) return true;
            index = index + 1;
        }
        return false;
    }
}

internal class SvgPatternTileRenderer
{
    SvgScenePatternPaintData? pattern;
    SvgPaintSampleOptions options;
    SvgPatternSampleContext context;
    SvgPixelBuffer? tile;
    SvgPixelBuffer? mask;
    SvgMatrix? patternToPixels;
    SvgMatrix? pixelsToPattern;
    SvgMatrix? pixelsToOuter;
    SvgPaintSampleError error;

    internal SvgPatternTileRenderer(SvgScenePatternPaintData? source, SvgPaintSampleOptions sampleOptions, SvgPatternSampleContext sampleContext)
    {
        pattern = source;
        options = sampleOptions;
        context = sampleContext;
        tile = null;
        mask = null;
        patternToPixels = null;
        pixelsToPattern = null;
        pixelsToOuter = null;
        error = SvgPaintSampleError.None;
    }

    internal SvgPatternTileResult Render()
    {
        if (!ValidPattern()) return Failure(SvgPaintSampleError.InvalidArgument);
        SvgMatrixOperationResult patternInverse = (pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform.Invert();
        if (!patternInverse.Success) return Failure(MapMatrix(patternInverse.Error));
        int width = PixelLength(pattern.Transform.A, pattern.Transform.B, (pattern.TileBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width);
        int height = PixelLength(pattern.Transform.C, pattern.Transform.D, pattern.TileBounds.Height);
        if (width < 1 || height < 1) return Failure(SvgPaintSampleError.PatternTileLimitExceeded);
        SvgPixelBufferResult tileResult = SvgPixelBuffer.Create(width, height, options.OutputColorSpace, options.RasterLimits);
        if (!tileResult.Success) return Failure(MapAllocation(tileResult.Error));
        SvgPixelBufferResult maskResult = SvgPixelBuffer.Create(width, height, options.OutputColorSpace, options.RasterLimits);
        if (!maskResult.Success) return Failure(MapAllocation(maskResult.Error));
        tile = tileResult.Buffer;
        mask = maskResult.Buffer;
        if (!BuildMatrices(width, height)) return Failure(error);
        if (pattern.Content != null)
        {
            SvgMatrixOperationResult first = (patternToPixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply((patternInverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            if (!first.Success) return Failure(MapMatrix(first.Error));
            SvgMatrixOperationResult baseTransform = (first.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(pattern.ContentTransform);
            if (!baseTransform.Success) return Failure(MapMatrix(baseTransform.Error));
            RenderNode(pattern.Content, baseTransform.Matrix, 1);
            if (error != SvgPaintSampleError.None) return Failure(error);
        }
        return new SvgPatternTileResult(true, SvgPaintSampleError.None, tile, patternToPixels, pixelsToPattern);
    }

    void RenderNode(SvgSceneNode node, SvgMatrix? parentTransform, double inheritedOpacity)
    {
        if (error != SvgPaintSampleError.None) return;
        if (node == null || !node.HasValidTag() || node.Children == null || !ValidMatrix(node.Transform))
        {
            error = SvgPaintSampleError.InvalidArgument;
            return;
        }
        if (ContainsNode(node))
        {
            error = SvgPaintSampleError.PatternRecursionDetected;
            return;
        }
        if (context.NodesVisited >= options.MaximumPatternNodes)
        {
            error = SvgPaintSampleError.PatternNodeLimitExceeded;
            return;
        }
        context.NodesVisited = context.NodesVisited + 1;
        if (!node.Displayed) return;
        if (!SvgFiniteMath.IsFinite(node.Opacity) || node.Opacity < 0 || node.Opacity > 1)
        {
            error = SvgPaintSampleError.InvalidArgument;
            return;
        }
        SvgMatrixOperationResult combinedResult = (parentTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(node.Transform);
        if (!combinedResult.Success)
        {
            error = MapMatrix(combinedResult.Error);
            return;
        }
        double opacity = inheritedOpacity * node.Opacity;
        context.ActiveNodes.Add(node);
        if (node.Kind == SvgSceneNodeKind.Path && node.Visible && opacity > 0) RenderPath(node, combinedResult.Matrix, opacity);
        if (error == SvgPaintSampleError.None && node.Kind == SvgSceneNodeKind.Group)
        {
            int index = 0;
            while (index < node.Children.Count && error == SvgPaintSampleError.None)
            {
                RenderNode(node.Children[index], combinedResult.Matrix, opacity);
                index = index + 1;
            }
        }
        context.ActiveNodes.RemoveAt(context.ActiveNodes.Count - 1);
    }

    void RenderPath(SvgSceneNode node, SvgMatrix? transform, double opacity)
    {
        if ((node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path == null || node.PaintOrder == null || !SvgFiniteMath.IsFinite(node.FillOpacity) || !SvgFiniteMath.IsFinite(node.StrokeOpacity) || node.FillOpacity < 0 || node.FillOpacity > 1 || node.StrokeOpacity < 0 || node.StrokeOpacity > 1)
        {
            error = SvgPaintSampleError.InvalidArgument;
            return;
        }
        int index = 0;
        while (index < node.PaintOrder.Count && error == SvgPaintSampleError.None)
        {
            SvgPaintOrderComponent component = node.PaintOrder[index];
            if (component == SvgPaintOrderComponent.Fill && (node.Fill != null || node.FillUsesContext) && node.FillOpacity > 0) RenderOperation(node, transform, opacity * node.FillOpacity, true);
            else if (component == SvgPaintOrderComponent.Stroke && (node.Stroke != null || node.StrokeUsesContext) && node.StrokeOpacity > 0) RenderOperation(node, transform, opacity * node.StrokeOpacity, false);
            else if (component != SvgPaintOrderComponent.Markers && component != SvgPaintOrderComponent.Fill && component != SvgPaintOrderComponent.Stroke) error = SvgPaintSampleError.InvalidArgument;
            index = index + 1;
        }
    }

    void RenderOperation(SvgSceneNode node, SvgMatrix? transform, double opacity, bool fill)
    {
        SvgPixelOperationResult cleared = (mask ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Clear();
        if (!cleared.Success)
        {
            error = SvgPaintSampleError.PatternRenderFailed;
            return;
        }
        SvgPathRasterOptions raster = new SvgPathRasterOptions();
        if (fill) raster.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        else raster.StrokeColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        raster.FillRule = (node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FillRule;
        raster.StrokeStyle = node.Path.StrokeStyle;
        raster.VectorEffect = node.Path.VectorEffect;
        raster.ShapeRendering = node.Path.ShapeRendering;
        raster.RasterOptions.Transform = transform;
        raster.RasterOptions.SamplesPerAxis = options.PatternSamplesPerAxis;
        SvgPathRasterResult rasterized = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions((node.Path.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), mask, raster);
        if (!rasterized.Success)
        {
            error = SvgPaintSampleError.PatternRenderFailed;
            return;
        }
        SvgMatrixOperationResult inverse = (transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Invert();
        if (!inverse.Success)
        {
            error = MapMatrix(inverse.Error);
            return;
        }
        SvgSceneResource? paint = fill ? node.FillUsesContext ? options.ContextFill : node.Fill : node.StrokeUsesContext ? options.ContextStroke : node.Stroke;
        if (paint == null) return;
        bool contextPaint = fill ? node.FillUsesContext : node.StrokeUsesContext;
        CompositeMask(paint, inverse.Matrix, opacity, contextPaint);
    }

    void CompositeMask(SvgSceneResource paint, SvgMatrix? pixelsToLocal, double opacity, bool contextPaint)
    {
        SvgPaintSampleOptions nested = NestedOptions();
        int y = 0;
        while (y < (tile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Height && error == SvgPaintSampleError.None)
        {
            int x = 0;
            while (x < tile.Width && error == SvgPaintSampleError.None)
            {
                int maskIndex = (mask ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * mask.Stride + x * 4;
                byte coverage = mask.Data[maskIndex + 3];
                if (coverage > 0)
                {
                    SvgPoint pixel = new SvgPoint(x + 0.5, y + 0.5);
                    SvgPointTransformResult mapped = contextPaint ? (pixelsToOuter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(pixel) : (pixelsToLocal ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(pixel);
                    if (!mapped.Success)
                    {
                        error = SvgPaintSampleError.NonFiniteResult;
                        return;
                    }
                    SvgPaintSampleResult sampled = new SvgPaintSampler().SampleWithResourceAndPointAndOptionsAndContext(paint, mapped.Point, nested, context);
                    if (!sampled.Success)
                    {
                        error = sampled.Error;
                        return;
                    }
                    if (sampled.HasPaint) Composite(x, y, sampled.Pixel, coverage / 255.0 * opacity);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Composite(int x, int y, SvgPremultipliedRgba8 source, double factor)
    {
        byte sourceAlpha = Byte(source.Alpha * factor);
        byte sourceRed = Byte(source.Red * factor);
        byte sourceGreen = Byte(source.Green * factor);
        byte sourceBlue = Byte(source.Blue * factor);
        int index = (tile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * tile.Stride + x * 4;
        int inverseAlpha = 255 - sourceAlpha;
        tile.Data[index] = Over(sourceRed, tile.Data[index], inverseAlpha);
        tile.Data[index + 1] = Over(sourceGreen, tile.Data[index + 1], inverseAlpha);
        tile.Data[index + 2] = Over(sourceBlue, tile.Data[index + 2], inverseAlpha);
        tile.Data[index + 3] = Over(sourceAlpha, tile.Data[index + 3], inverseAlpha);
    }

    SvgPaintSampleOptions NestedOptions()
    {
        SvgPaintSampleOptions nested = new SvgPaintSampleOptions();
        nested.Opacity = 1;
        nested.OutputColorSpace = options.OutputColorSpace;
        nested.MaximumStops = options.MaximumStops;
        nested.RasterLimits = options.RasterLimits;
        nested.MaximumPatternDepth = options.MaximumPatternDepth;
        nested.MaximumPatternNodes = options.MaximumPatternNodes;
        nested.PatternSamplesPerAxis = options.PatternSamplesPerAxis;
        nested.ContextFill = options.ContextFill;
        nested.ContextStroke = options.ContextStroke;
        return nested;
    }

    bool BuildMatrices(int width, int height)
    {
        double scaleX = width / ((pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TileBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width;
        double scaleY = height / (pattern.TileBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Height;
        double translateX = -pattern.TileBounds.X * scaleX;
        double translateY = -pattern.TileBounds.Y * scaleY;
        if (!SvgFiniteMath.IsFinite(scaleX) || !SvgFiniteMath.IsFinite(scaleY) || !SvgFiniteMath.IsFinite(translateX) || !SvgFiniteMath.IsFinite(translateY))
        {
            error = SvgPaintSampleError.NonFiniteResult;
            return false;
        }
        patternToPixels = new SvgMatrix(scaleX, 0, 0, scaleY, translateX, translateY);
        SvgMatrixOperationResult inverse = patternToPixels.Invert();
        if (!inverse.Success)
        {
            error = MapMatrix(inverse.Error);
            return false;
        }
        pixelsToPattern = inverse.Matrix;
        SvgMatrixOperationResult outer = pattern.Transform.Multiply((pixelsToPattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!outer.Success)
        {
            error = MapMatrix(outer.Error);
            return false;
        }
        pixelsToOuter = outer.Matrix;
        return true;
    }

    bool ValidPattern()
    {
        if (pattern == null || pattern.TileBounds == null || pattern.Transform == null || pattern.ContentTransform == null) return false;
        SvgRect bounds = pattern.TileBounds;
        if (!SvgFiniteMath.IsFinite(bounds.X) || !SvgFiniteMath.IsFinite(bounds.Y) || !SvgFiniteMath.IsFinite(bounds.Width) || !SvgFiniteMath.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0 || bounds.IsEmpty) return false;
        return ValidMatrix(pattern.Transform) && ValidMatrix(pattern.ContentTransform);
    }

    static bool ValidMatrix(SvgMatrix matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }

    static int PixelLength(double first, double second, double extent)
    {
        double scale = System.Math.Max(System.Math.Abs(first), System.Math.Abs(second));
        if (!SvgFiniteMath.IsFinite(scale) || scale == 0) return 0;
        double normalizedFirst = first / scale;
        double normalizedSecond = second / scale;
        double length = scale * System.Math.Sqrt(normalizedFirst * normalizedFirst + normalizedSecond * normalizedSecond) * extent;
        if (!SvgFiniteMath.IsFinite(length) || length > int.MaxValue) return -1;
        int pixels = (int)System.Math.Ceiling(length);
        return pixels < 1 ? 1 : pixels;
    }

    bool ContainsNode(SvgSceneNode node)
    {
        int index = 0;
        while (index < context.ActiveNodes.Count)
        {
            if (object.ReferenceEquals(context.ActiveNodes[index], node)) return true;
            index = index + 1;
        }
        return false;
    }

    static byte Over(byte source, byte destination, int inverseAlpha)
    {
        int result = source + (destination * inverseAlpha + 127) / 255;
        return result > 255 ? (byte)255 : (byte)result;
    }

    static byte Byte(double value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    static SvgPaintSampleError MapAllocation(SvgPixelBufferError allocation)
    {
        if (allocation == SvgPixelBufferError.DimensionLimitExceeded || allocation == SvgPixelBufferError.ByteLimitExceeded) return SvgPaintSampleError.PatternTileLimitExceeded;
        return SvgPaintSampleError.InvalidArgument;
    }

    static SvgPaintSampleError MapMatrix(SvgGeometryError matrixError)
    {
        if (matrixError == SvgGeometryError.SingularMatrix) return SvgPaintSampleError.SingularTransform;
        if (matrixError == SvgGeometryError.InvalidArgument) return SvgPaintSampleError.InvalidArgument;
        return SvgPaintSampleError.NonFiniteResult;
    }

    static SvgPatternTileResult Failure(SvgPaintSampleError failure)
    {
        return new SvgPatternTileResult(false, failure, null, null, null);
    }
}
