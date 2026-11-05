namespace svg;

internal class SvgClipMaskState
{
    SvgPixelBuffer? target;
    SvgClipMaskOptions options;
    SvgPixelRect? bounds;
    SvgPixelBuffer? combined;
    SvgClipMaskError error;
    long work;
    int surfaces;
    int nodes;
    int clipsApplied;
    System.Collections.Generic.List<SvgSceneResource> active;

    internal SvgClipMaskState(SvgPixelBuffer? surface, SvgClipMaskOptions clipMaskOptions)
    {
        target = surface;
        options = clipMaskOptions;
        bounds = null;
        combined = null;
        error = SvgClipMaskError.None;
        work = 0;
        surfaces = 0;
        nodes = 0;
        clipsApplied = 0;
        active = new System.Collections.Generic.List<SvgSceneResource>();
    }

    internal SvgClipMaskResult Run()
    {
        if (!Validate()) return Failure(error);
        bounds = options.Subregion == null ? new SvgPixelRect(0, 0, (target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, target.Height) : options.Subregion;
        if (!SvgCompositeMath.Premultiplied(target, bounds)) return Failure(SvgClipMaskError.InvalidArgument);
        if (bounds.IsEmpty || options.Clips.Count == 0 && options.Mask == null) return Success();
        combined = Create(bounds.Width, bounds.Height, SvgPixelColorSpace.Srgb);
        if (combined == null) return Failure(error);
        SvgPixelOperationResult cleared = combined.ClearWithPixel(new SvgPremultipliedRgba8(255, 255, 255, 255));
        if (!cleared.Success) return Failure(SvgClipMaskError.InvalidArgument);
        int index = 0;
        while (index < options.Clips.Count && error == SvgClipMaskError.None)
        {
            ApplyClip(options.Clips[index]);
            if (error == SvgClipMaskError.None) clipsApplied = clipsApplied + 1;
            index = index + 1;
        }
        if (error == SvgClipMaskError.None && options.Mask != null) ApplyMask();
        if (error != SvgClipMaskError.None) return Failure(error);
        if (!TakeWork((long)bounds.Width * bounds.Height)) return Failure(error);
        int y = 0;
        while (y < bounds.Height)
        {
            int x = 0;
            while (x < bounds.Width)
            {
                int coverageIndex = combined.Offset + y * combined.Stride + x * 4;
                int targetIndex = (target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + (bounds.Y + y) * target.Stride + (bounds.X + x) * 4;
                int coverage = combined.Data[coverageIndex + 3];
                target.Data[targetIndex] = Multiply(target.Data[targetIndex], coverage);
                target.Data[targetIndex + 1] = Multiply(target.Data[targetIndex + 1], coverage);
                target.Data[targetIndex + 2] = Multiply(target.Data[targetIndex + 2], coverage);
                target.Data[targetIndex + 3] = Multiply(target.Data[targetIndex + 3], coverage);
                x = x + 1;
            }
            y = y + 1;
        }
        return Success();
    }

    void ApplyClip(SvgClipStackEntry entry)
    {
        if (entry.Kind == SvgClipStackEntryKind.Rectangle)
        {
            if (!TakeWork((long)(bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * bounds.Height)) return;
            int y = 0;
            while (y < bounds.Height)
            {
                int x = 0;
                while (x < bounds.Width)
                {
                    int surfaceX = bounds.X + x;
                    int surfaceY = bounds.Y + y;
                    if (surfaceX < (entry.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X || surfaceY < entry.Rectangle.Y || surfaceX >= entry.Rectangle.Right || surfaceY >= entry.Rectangle.Bottom) SetCoverage(combined, x, y, 0);
                    x = x + 1;
                }
                y = y + 1;
            }
            return;
        }
        SvgPixelBuffer? clip = Create((bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, bounds.Height, SvgPixelColorSpace.Srgb);
        if (clip == null) return;
        SvgMatrix? shifted = Shifted(entry.Transform, bounds.X, bounds.Y);
        if (shifted == null) return;
        RenderClipResource(entry.Resource, clip, shifted, 0);
        if (error != SvgClipMaskError.None) return;
        MultiplyCoverage(combined, clip);
    }

    void ApplyMask()
    {
        SvgSceneMaskData? mask = (options.Mask ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Mask;
        SvgRectTransformResult transformed = options.MaskTransform.TransformRect(((mask ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Region ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!transformed.Success)
        {
            error = Map(transformed.Error);
            return;
        }
        SvgPixelRect? region = PixelIntersection(transformed.Rectangle, bounds);
        if (region == null)
        {
            error = SvgClipMaskError.NonFiniteResult;
            return;
        }
        if (region.IsEmpty)
        {
            ZeroCoverage(combined);
            return;
        }
        SvgPixelColorSpace colorSpace = mask.ColorInterpolation == SvgColorSpace.SrgbLinear ? SvgPixelColorSpace.SrgbLinear : SvgPixelColorSpace.Srgb;
        SvgPixelBuffer? rendered = Create(region.Width, region.Height, colorSpace);
        if (rendered == null) return;
        SvgMatrix? shifted = Shifted(options.MaskTransform, region.X, region.Y);
        if (shifted == null) return;
        SvgMatrix? content = MultiplyMatrix(shifted, mask.ContentTransform);
        if (content == null) return;
        RenderMaskNode(mask.Content, rendered, content, 1, 0);
        if (error != SvgClipMaskError.None) return;
        SvgMatrixOperationResult inverse = options.MaskTransform.Invert();
        if (!inverse.Success)
        {
            error = Map(inverse.Error);
            return;
        }
        if (!TakeWork((long)(bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * bounds.Height)) return;
        int y = 0;
        while (y < bounds.Height)
        {
            int x = 0;
            while (x < bounds.Width)
            {
                int surfaceX = bounds.X + x;
                int surfaceY = bounds.Y + y;
                byte value = 0;
                if (surfaceX >= region.X && surfaceY >= region.Y && surfaceX < region.Right && surfaceY < region.Bottom)
                {
                    SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(surfaceX + 0.5, surfaceY + 0.5));
                    if (!local.Success)
                    {
                        error = SvgClipMaskError.NonFiniteResult;
                        return;
                    }
                    if (Contains(mask.Region, local.Point))
                    {
                        int maskIndex = rendered.Offset + (surfaceY - region.Y) * rendered.Stride + (surfaceX - region.X) * 4;
                        value = MaskValue(rendered.Data, maskIndex, mask.MaskType);
                    }
                }
                int combinedIndex = (combined ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * combined.Stride + x * 4;
                SetCoverage(combined, x, y, Multiply(combined.Data[combinedIndex + 3], value));
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void RenderClipResource(SvgSceneResource? resource, SvgPixelBuffer output, SvgMatrix targetTransform, int depth)
    {
        if (depth >= options.MaximumDepth)
        {
            error = SvgClipMaskError.DepthLimitExceeded;
            return;
        }
        if (Contains(active, resource))
        {
            error = SvgClipMaskError.RecursionDetected;
            return;
        }
        active.Add((resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        SvgMatrix? transform = MultiplyMatrix(targetTransform, (resource.Clip ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform);
        if (transform != null && resource.Clip.Content != null) RenderClipNode(resource.Clip.Content, output, transform, depth);
        active.RemoveAt(active.Count - 1);
    }

    void RenderClipNode(SvgSceneNode node, SvgPixelBuffer output, SvgMatrix parentTransform, int depth)
    {
        if (error != SvgClipMaskError.None || node == null) return;
        if (!TakeNode()) return;
        if (!node.Displayed || !node.Visible) return;
        SvgMatrix? transform = MultiplyMatrix(parentTransform, node.Transform);
        if (transform == null) return;
        if (node.Clip != null)
        {
            SvgPixelBuffer? content = Create(output.Width, output.Height, SvgPixelColorSpace.Srgb);
            SvgPixelBuffer? nested = Create(output.Width, output.Height, SvgPixelColorSpace.Srgb);
            if (content == null || nested == null) return;
            RenderClipNodeBody(node, content, transform, depth);
            RenderClipResource(node.Clip, nested, transform, depth + 1);
            if (error != SvgClipMaskError.None) return;
            MultiplyCoverage(content, nested);
            MaximumCoverage(output, content);
            return;
        }
        RenderClipNodeBody(node, output, transform, depth);
    }

    void RenderClipNodeBody(SvgSceneNode node, SvgPixelBuffer output, SvgMatrix transform, int depth)
    {
        if (node.Kind == SvgSceneNodeKind.Path && node.Path != null && node.Path.Path != null)
        {
            SvgPixelBuffer? shape = Create(output.Width, output.Height, SvgPixelColorSpace.Srgb);
            if (shape == null) return;
            SvgRasterOptions? raster = SvgPathRasterMath.CopyRaster(options.RasterOptions);
            (raster ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform = transform;
            raster.Clip = null;
            SvgRasterResult result = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(node.Path.Path, shape, new SvgPremultipliedRgba8(255, 255, 255, 255), node.Path.FillRule, raster);
            if (!result.Success)
            {
                error = SvgClipMaskError.RasterizationFailed;
                return;
            }
            MaximumCoverage(output, shape);
            return;
        }
        int index = 0;
        while (index < node.Children.Count && error == SvgClipMaskError.None)
        {
            RenderClipNode(node.Children[index], output, transform, depth);
            index = index + 1;
        }
    }

    void RenderMaskNode(SvgSceneNode? node, SvgPixelBuffer output, SvgMatrix parentTransform, double inheritedOpacity, int depth)
    {
        if (error != SvgClipMaskError.None || node == null) return;
        if (!TakeNode()) return;
        if (!node.Displayed || !node.Visible) return;
        if (!SvgFiniteMath.IsFinite(node.Opacity) || node.Opacity < 0 || node.Opacity > 1)
        {
            error = SvgClipMaskError.InvalidArgument;
            return;
        }
        SvgMatrix? transform = MultiplyMatrix(parentTransform, node.Transform);
        if (transform == null) return;
        double opacity = inheritedOpacity * node.Opacity;
        if (node.Clip != null || node.Opacity < 1)
        {
            SvgPixelBuffer? content = Create(output.Width, output.Height, output.ColorSpace);
            if (content == null) return;
            RenderMaskNodeBody(node, content, transform, 1, depth);
            if (node.Clip != null)
            {
                SvgPixelBuffer? clip = Create(output.Width, output.Height, SvgPixelColorSpace.Srgb);
                if (clip == null) return;
                RenderClipResource(node.Clip, clip, transform, depth + 1);
                if (error != SvgClipMaskError.None) return;
                ScaleByCoverage(content, clip);
            }
            CompositeBuffer(content, output, opacity);
            return;
        }
        RenderMaskNodeBody(node, output, transform, opacity, depth);
    }

    void RenderMaskNodeBody(SvgSceneNode node, SvgPixelBuffer output, SvgMatrix transform, double opacity, int depth)
    {
        if (opacity <= 0) return;
        if (node.Kind == SvgSceneNodeKind.Path && node.Path != null && node.Path.Path != null)
        {
            int index = 0;
            while (index < node.PaintOrder.Count && error == SvgClipMaskError.None)
            {
                SvgPaintOrderComponent component = node.PaintOrder[index];
                if (component == SvgPaintOrderComponent.Fill && node.Fill != null && node.FillOpacity > 0) RenderMaskPath(node, output, transform, node.Fill, opacity * node.FillOpacity, true);
                else if (component == SvgPaintOrderComponent.Stroke && node.Stroke != null && node.StrokeOpacity > 0) RenderMaskPath(node, output, transform, node.Stroke, opacity * node.StrokeOpacity, false);
                index = index + 1;
            }
            return;
        }
        if (node.Kind == SvgSceneNodeKind.Image)
        {
            RenderMaskImage(node, output, transform, opacity);
            return;
        }
        int child = 0;
        while (child < node.Children.Count && error == SvgClipMaskError.None)
        {
            RenderMaskNode(node.Children[child], output, transform, opacity, depth);
            child = child + 1;
        }
    }

    void RenderMaskPath(SvgSceneNode node, SvgPixelBuffer output, SvgMatrix transform, SvgSceneResource paint, double opacity, bool fill)
    {
        if (!SvgFiniteMath.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            error = SvgClipMaskError.InvalidArgument;
            return;
        }
        SvgPixelBuffer? coverage = Create(output.Width, output.Height, SvgPixelColorSpace.Srgb);
        if (coverage == null) return;
        SvgPathRasterOptions raster = new SvgPathRasterOptions();
        if (fill) raster.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        else raster.StrokeColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        raster.FillRule = (node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FillRule;
        raster.StrokeStyle = node.Path.StrokeStyle;
        raster.VectorEffect = node.Path.VectorEffect;
        raster.ShapeRendering = node.Path.ShapeRendering;
        raster.RasterOptions = (SvgPathRasterMath.CopyRaster(options.RasterOptions) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        (raster.RasterOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform = transform;
        raster.RasterOptions.Clip = null;
        SvgPathRasterResult rasterized = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions((node.Path.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), coverage, raster);
        if (!rasterized.Success)
        {
            error = SvgClipMaskError.RasterizationFailed;
            return;
        }
        SvgMatrixOperationResult inverse = transform.Invert();
        if (!inverse.Success)
        {
            error = Map(inverse.Error);
            return;
        }
        SvgPaintSampleOptions paintOptions = CopyPaintOptions(output.ColorSpace);
        if (!TakeWork((long)output.Width * output.Height)) return;
        int y = 0;
        while (y < output.Height && error == SvgClipMaskError.None)
        {
            int x = 0;
            while (x < output.Width && error == SvgClipMaskError.None)
            {
                int coverageIndex = coverage.Offset + y * coverage.Stride + x * 4;
                byte amount = coverage.Data[coverageIndex + 3];
                if (amount > 0)
                {
                    SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(x + 0.5, y + 0.5));
                    if (!local.Success)
                    {
                        error = SvgClipMaskError.NonFiniteResult;
                        return;
                    }
                    SvgPaintSampleResult sampled = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(paint, (local.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), paintOptions);
                    if (!sampled.Success)
                    {
                        error = SvgClipMaskError.PaintSamplingFailed;
                        return;
                    }
                    if (sampled.HasPaint) CompositePixel(output, x, y, sampled.Pixel, amount / 255.0 * opacity);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void RenderMaskImage(SvgSceneNode node, SvgPixelBuffer output, SvgMatrix transform, double opacity)
    {
        SvgMatrixOperationResult inverse = transform.Invert();
        if (!inverse.Success)
        {
            error = Map(inverse.Error);
            return;
        }
        SvgImageSampleOptions imageOptions = new SvgImageSampleOptions();
        imageOptions.Opacity = 1;
        if (!TakeWork((long)output.Width * output.Height)) return;
        int y = 0;
        while (y < output.Height && error == SvgClipMaskError.None)
        {
            int x = 0;
            while (x < output.Width && error == SvgClipMaskError.None)
            {
                SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(x + 0.5, y + 0.5));
                if (!local.Success)
                {
                    error = SvgClipMaskError.NonFiniteResult;
                    return;
                }
                SvgImageSampleResult sampled = new SvgImageSampler().SampleWithNodeAndPointAndOptions(node, (local.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), imageOptions);
                if (!sampled.Success)
                {
                    error = SvgClipMaskError.PaintSamplingFailed;
                    return;
                }
                if (sampled.HasPixel)
                {
                    SvgPremultipliedRgba8 pixel = sampled.Pixel;
                    if (sampled.ColorSpace != output.ColorSpace)
                    {
                        pixel = (ConvertPixel(sampled.Pixel, sampled.ColorSpace, output.ColorSpace) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                        if (pixel == null)
                        {
                            error = SvgClipMaskError.PaintSamplingFailed;
                            return;
                        }
                    }
                    CompositePixel(output, x, y, pixel, opacity);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void MultiplyCoverage(SvgPixelBuffer? first, SvgPixelBuffer second)
    {
        if (!TakeWork((long)(first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * first.Height)) return;
        int y = 0;
        while (y < first.Height)
        {
            int x = 0;
            while (x < first.Width)
            {
                int firstIndex = first.Offset + y * first.Stride + x * 4;
                int secondIndex = second.Offset + y * second.Stride + x * 4;
                SetCoverage(first, x, y, Multiply(first.Data[firstIndex + 3], second.Data[secondIndex + 3]));
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void MaximumCoverage(SvgPixelBuffer destination, SvgPixelBuffer source)
    {
        if (!TakeWork((long)destination.Width * destination.Height)) return;
        int y = 0;
        while (y < destination.Height)
        {
            int x = 0;
            while (x < destination.Width)
            {
                int destinationIndex = destination.Offset + y * destination.Stride + x * 4;
                int sourceIndex = source.Offset + y * source.Stride + x * 4;
                byte value = destination.Data[destinationIndex + 3] >= source.Data[sourceIndex + 3] ? destination.Data[destinationIndex + 3] : source.Data[sourceIndex + 3];
                SetCoverage(destination, x, y, value);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void ScaleByCoverage(SvgPixelBuffer image, SvgPixelBuffer coverage)
    {
        if (!TakeWork((long)image.Width * image.Height)) return;
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                int imageIndex = image.Offset + y * image.Stride + x * 4;
                int coverageIndex = coverage.Offset + y * coverage.Stride + x * 4;
                int amount = coverage.Data[coverageIndex + 3];
                image.Data[imageIndex] = Multiply(image.Data[imageIndex], amount);
                image.Data[imageIndex + 1] = Multiply(image.Data[imageIndex + 1], amount);
                image.Data[imageIndex + 2] = Multiply(image.Data[imageIndex + 2], amount);
                image.Data[imageIndex + 3] = Multiply(image.Data[imageIndex + 3], amount);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void CompositeBuffer(SvgPixelBuffer source, SvgPixelBuffer destination, double opacity)
    {
        if (!TakeWork((long)source.Width * source.Height)) return;
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                int index = source.Offset + y * source.Stride + x * 4;
                SvgPremultipliedRgba8 pixel = new SvgPremultipliedRgba8(source.Data[index], source.Data[index + 1], source.Data[index + 2], source.Data[index + 3]);
                CompositePixel(destination, x, y, pixel, opacity);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void CompositePixel(SvgPixelBuffer destination, int x, int y, SvgPremultipliedRgba8 source, double factor)
    {
        byte alpha = Byte(source.Alpha * factor);
        byte red = Byte(source.Red * factor);
        byte green = Byte(source.Green * factor);
        byte blue = Byte(source.Blue * factor);
        int index = destination.Offset + y * destination.Stride + x * 4;
        int inverse = 255 - alpha;
        destination.Data[index] = Over(red, destination.Data[index], inverse);
        destination.Data[index + 1] = Over(green, destination.Data[index + 1], inverse);
        destination.Data[index + 2] = Over(blue, destination.Data[index + 2], inverse);
        destination.Data[index + 3] = Over(alpha, destination.Data[index + 3], inverse);
    }

    void ZeroCoverage(SvgPixelBuffer? surface)
    {
        if (!TakeWork((long)(surface ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * surface.Height)) return;
        SvgPixelOperationResult cleared = surface.Clear();
        if (!cleared.Success) error = SvgClipMaskError.InvalidArgument;
    }

    SvgPixelBuffer? Create(int width, int height, SvgPixelColorSpace colorSpace)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create(width, height, colorSpace, options.RasterLimits);
        if (!created.Success)
        {
            error = created.Error == SvgPixelBufferError.DimensionLimitExceeded || created.Error == SvgPixelBufferError.ByteLimitExceeded ? SvgClipMaskError.AllocationLimitExceeded : SvgClipMaskError.InvalidArgument;
            return null;
        }
        surfaces = surfaces + 1;
        return created.Buffer;
    }

    SvgMatrix? Shifted(SvgMatrix transform, int x, int y)
    {
        return MultiplyMatrix(new SvgMatrix(1, 0, 0, 1, -x, -y), transform);
    }

    SvgMatrix? MultiplyMatrix(SvgMatrix first, SvgMatrix second)
    {
        if (!ValidMatrix(first) || !ValidMatrix(second))
        {
            error = SvgClipMaskError.InvalidArgument;
            return null;
        }
        SvgMatrixOperationResult result = first.Multiply(second);
        if (!result.Success)
        {
            error = Map(result.Error);
            return null;
        }
        return result.Matrix;
    }

    SvgPixelRect? PixelIntersection(SvgRect? rectangle, SvgPixelRect? limit)
    {
        if (rectangle == null || !SvgFiniteMath.IsFinite(rectangle.X) || !SvgFiniteMath.IsFinite(rectangle.Y) || !SvgFiniteMath.IsFinite(rectangle.Width) || !SvgFiniteMath.IsFinite(rectangle.Height)) return null;
        int left = SvgRasterMath.FloorClamp(rectangle.X, (limit ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, limit.Right);
        int top = SvgRasterMath.FloorClamp(rectangle.Y, limit.Y, limit.Bottom);
        int right = SvgRasterMath.CeilingClamp(rectangle.X + rectangle.Width, limit.X, limit.Right);
        int bottom = SvgRasterMath.CeilingClamp(rectangle.Y + rectangle.Height, limit.Y, limit.Bottom);
        if (right < left) right = left;
        if (bottom < top) bottom = top;
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    SvgPaintSampleOptions CopyPaintOptions(SvgPixelColorSpace colorSpace)
    {
        SvgPaintSampleOptions copy = new SvgPaintSampleOptions();
        copy.Opacity = 1;
        copy.OutputColorSpace = colorSpace;
        copy.MaximumStops = options.PaintOptions.MaximumStops;
        copy.RasterLimits = options.RasterLimits;
        copy.MaximumPatternDepth = options.PaintOptions.MaximumPatternDepth;
        copy.MaximumPatternNodes = options.PaintOptions.MaximumPatternNodes;
        copy.PatternSamplesPerAxis = options.PaintOptions.PatternSamplesPerAxis;
        copy.ContextFill = options.PaintOptions.ContextFill;
        copy.ContextStroke = options.PaintOptions.ContextStroke;
        return copy;
    }

    bool Validate()
    {
        if (!SvgCompositeMath.ValidBuffer(target) || options == null || options.Clips == null || options.MaskTransform == null || options.RasterLimits == null || options.RasterOptions == null || options.PaintOptions == null || options.MaximumDepth < 1 || options.MaximumNodes < 1 || options.MaximumPixels < 1)
        {
            error = SvgClipMaskError.InvalidArgument;
            return false;
        }
        if (!SvgCompositeMath.ValidLimits(options.RasterLimits) || !ValidMatrix(options.MaskTransform) || !ValidSubregion(options.Subregion))
        {
            error = SvgClipMaskError.InvalidArgument;
            return false;
        }
        if (options.Clips.Count > options.MaximumDepth)
        {
            error = SvgClipMaskError.DepthLimitExceeded;
            return false;
        }
        if (!SvgFiniteMath.IsFinite(options.RasterOptions.Tolerance) || options.RasterOptions.Tolerance <= 0 || options.RasterOptions.SamplesPerAxis < 1 || options.RasterOptions.SamplesPerAxis > 16 || options.RasterOptions.MaximumSubdivisionDepth < 0 || options.RasterOptions.MaximumEdges < 1 || options.RasterOptions.MaximumWork < 1)
        {
            error = SvgClipMaskError.InvalidArgument;
            return false;
        }
        if (!SvgFiniteMath.IsFinite(options.PaintOptions.Opacity) || options.PaintOptions.MaximumStops < 1 || options.PaintOptions.MaximumPatternDepth < 1 || options.PaintOptions.MaximumPatternNodes < 1 || options.PaintOptions.PatternSamplesPerAxis < 1 || options.PaintOptions.PatternSamplesPerAxis > 16)
        {
            error = SvgClipMaskError.InvalidArgument;
            return false;
        }
        int index = 0;
        while (index < options.Clips.Count)
        {
            SvgClipStackEntry entry = options.Clips[index];
            if (entry == null || entry.Kind != SvgClipStackEntryKind.Rectangle && entry.Kind != SvgClipStackEntryKind.Resource)
            {
                error = SvgClipMaskError.InvalidArgument;
                return false;
            }
            if (entry.Kind == SvgClipStackEntryKind.Rectangle && !ValidRectangle(entry.Rectangle) || entry.Kind == SvgClipStackEntryKind.Resource && (!ValidClip(entry.Resource) || !ValidMatrix(entry.Transform)))
            {
                error = SvgClipMaskError.InvalidArgument;
                return false;
            }
            index = index + 1;
        }
        if (options.Mask != null && !ValidMask(options.Mask))
        {
            error = SvgClipMaskError.InvalidArgument;
            return false;
        }
        return true;
    }

    bool ValidSubregion(SvgPixelRect? rectangle)
    {
        if (rectangle == null) return true;
        return ValidRectangle(rectangle) && rectangle.X >= 0 && rectangle.Y >= 0 && rectangle.Right <= (target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width && rectangle.Bottom <= target.Height;
    }

    bool TakeNode()
    {
        if (nodes >= options.MaximumNodes)
        {
            error = SvgClipMaskError.NodeLimitExceeded;
            return false;
        }
        nodes = nodes + 1;
        return true;
    }

    bool TakeWork(long amount)
    {
        if (amount < 0 || work > options.MaximumPixels - amount)
        {
            error = SvgClipMaskError.WorkLimitExceeded;
            return false;
        }
        work = work + amount;
        return true;
    }

    SvgClipMaskResult Success()
    {
        SvgClipMaskResult result = new SvgClipMaskResult();
        result.Success = true;
        result.Bounds = bounds == null ? new SvgPixelRect(0, 0, 0, 0) : bounds;
        result.ClipsApplied = clipsApplied;
        result.MaskApplied = options != null && options.Mask != null;
        result.PixelsProcessed = work;
        result.OffscreenSurfaces = surfaces;
        return result;
    }

    SvgClipMaskResult Failure(SvgClipMaskError failure)
    {
        SvgClipMaskResult result = new SvgClipMaskResult();
        result.Error = failure;
        result.Bounds = bounds == null ? new SvgPixelRect(0, 0, 0, 0) : bounds;
        result.ClipsApplied = clipsApplied;
        result.PixelsProcessed = work;
        result.OffscreenSurfaces = surfaces;
        return result;
    }

    static bool ValidClip(SvgSceneResource? resource)
    {
        if (resource == null || resource.Kind != SvgSceneResourceKind.Clip || resource.Clip == null || !ValidMatrix(resource.Clip.Transform)) return false;
        return resource.Clip.Units == SvgPaintServerUnits.UserSpaceOnUse || resource.Clip.Units == SvgPaintServerUnits.ObjectBoundingBox;
    }

    static bool ValidMask(SvgSceneResource? resource)
    {
        if (resource == null || resource.Kind != SvgSceneResourceKind.Mask || resource.Mask == null || !ValidRect(resource.Mask.Region) || !ValidMatrix(resource.Mask.ContentTransform)) return false;
        if (resource.Mask.MaskType != SvgMaskType.Alpha && resource.Mask.MaskType != SvgMaskType.Luminance) return false;
        if (resource.Mask.ColorInterpolation != SvgColorSpace.Srgb && resource.Mask.ColorInterpolation != SvgColorSpace.SrgbLinear) return false;
        if (resource.Mask.Units != SvgPaintServerUnits.UserSpaceOnUse && resource.Mask.Units != SvgPaintServerUnits.ObjectBoundingBox) return false;
        return resource.Mask.ContentUnits == SvgPaintServerUnits.UserSpaceOnUse || resource.Mask.ContentUnits == SvgPaintServerUnits.ObjectBoundingBox;
    }

    static bool ValidRect(SvgRect? rectangle)
    {
        if (rectangle == null || !SvgFiniteMath.IsFinite(rectangle.X) || !SvgFiniteMath.IsFinite(rectangle.Y) || !SvgFiniteMath.IsFinite(rectangle.Width) || !SvgFiniteMath.IsFinite(rectangle.Height) || rectangle.Width < 0 || rectangle.Height < 0) return false;
        return SvgFiniteMath.IsFinite(rectangle.X + rectangle.Width) && SvgFiniteMath.IsFinite(rectangle.Y + rectangle.Height);
    }

    static bool ValidRectangle(SvgPixelRect? rectangle)
    {
        if (rectangle == null || rectangle.Width < 0 || rectangle.Height < 0) return false;
        long right = (long)rectangle.X + rectangle.Width;
        long bottom = (long)rectangle.Y + rectangle.Height;
        return right == rectangle.Right && bottom == rectangle.Bottom && rectangle.IsEmpty == (rectangle.Width == 0 || rectangle.Height == 0);
    }

    static bool ValidMatrix(SvgMatrix matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }

    static bool Contains(System.Collections.Generic.List<SvgSceneResource> resources, SvgSceneResource? resource)
    {
        int index = 0;
        while (index < resources.Count)
        {
            if (object.ReferenceEquals(resources[index], resource)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool Contains(SvgRect? rectangle, SvgPoint? point)
    {
        return (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X >= (rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X && point.Y >= rectangle.Y && point.X < rectangle.X + rectangle.Width && point.Y < rectangle.Y + rectangle.Height;
    }

    static SvgClipMaskError Map(SvgGeometryError matrixError)
    {
        if (matrixError == SvgGeometryError.SingularMatrix) return SvgClipMaskError.SingularTransform;
        if (matrixError == SvgGeometryError.InvalidArgument) return SvgClipMaskError.InvalidArgument;
        return SvgClipMaskError.NonFiniteResult;
    }

    static byte MaskValue(byte[] data, int index, SvgMaskType type)
    {
        if (type == SvgMaskType.Alpha) return data[index + 3];
        return Byte(0.2126 * data[index] + 0.7152 * data[index + 1] + 0.0722 * data[index + 2]);
    }

    static SvgPremultipliedRgba8? ConvertPixel(SvgPremultipliedRgba8 pixel, SvgPixelColorSpace source, SvgPixelColorSpace destination)
    {
        if (pixel.Alpha == 0) return new SvgPremultipliedRgba8(0, 0, 0, 0);
        double alpha = pixel.Alpha / 255.0;
        SvgColorSpace sourceSpace = source == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColorSpace destinationSpace = destination == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColor color = new SvgColor(sourceSpace, pixel.Red / 255.0 / alpha, pixel.Green / 255.0 / alpha, pixel.Blue / 255.0 / alpha, alpha);
        SvgColorOperationResult converted = color.ConvertTo(destinationSpace);
        if (!converted.Success) return null;
        return new SvgPremultipliedRgba8(Byte((converted.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * alpha * 255), Byte(converted.Color.Green * alpha * 255), Byte(converted.Color.Blue * alpha * 255), pixel.Alpha);
    }

    static void SetCoverage(SvgPixelBuffer? surface, int x, int y, byte value)
    {
        int index = (surface ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * surface.Stride + x * 4;
        surface.Data[index] = value;
        surface.Data[index + 1] = value;
        surface.Data[index + 2] = value;
        surface.Data[index + 3] = value;
    }

    static byte Multiply(int value, int factor)
    {
        return (byte)((value * factor + 127) / 255);
    }

    static byte Over(byte source, byte destination, int inverseAlpha)
    {
        int value = source + (destination * inverseAlpha + 127) / 255;
        return value > 255 ? (byte)255 : (byte)value;
    }

    static byte Byte(double value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }
}
