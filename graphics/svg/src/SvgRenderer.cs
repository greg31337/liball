namespace svg;

internal class SvgRenderCallbackResult
{
    internal bool Success;
    internal SvgPathData Path;

    internal SvgRenderCallbackResult(bool success, SvgPathData path)
    {
        Success = success;
        Path = path;
    }
}

internal class SvgRenderState
{
    SvgScene scene;
    SvgRenderResult result;
    SvgRenderOptions options;
    SvgMatrix pixelsFromWorld;
    long work;
    int nodes;
    int offscreenDepth;
    byte[] sampledPixel;

    internal SvgRenderState(SvgScene source, SvgRenderResult renderResult, SvgRenderOptions renderOptions, SvgMatrix pixelTransform)
    {
        scene = source;
        result = renderResult;
        options = renderOptions;
        pixelsFromWorld = pixelTransform;
        work = 0;
        nodes = 0;
        offscreenDepth = 0;
        sampledPixel = new byte[4];
    }

    internal bool Run()
    {
        if (!TakeWork((long)(result.Pixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * result.Pixels.Height)) return false;
        if (!RenderLayer((scene.LayerPlan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Root, result.Pixels)) return false;
        result.NodesVisited = nodes;
        result.PixelsProcessed = work;
        return true;
    }

    bool RenderLayer(SvgSceneLayer? layer, SvgPixelBuffer? destination)
    {
        if (layer == null || !layer.SubtreePaintable) return true;
        if (!Visit(layer.Node)) return false;
        bool local = layer.CreatesCompositingLayer || layer.Node.Clip != null || layer.OwnClipBounds != null;
        SvgPixelBuffer? target = destination;
        if (local)
        {
            if (offscreenDepth >= options.Limits.Filters.MaxOffscreenSurfaces) return Fail(SvgRenderError.AllocationLimitExceeded, "The active offscreen surface limit was exceeded.", layer.Node);
            SvgPixelBufferResult created = SvgPixelBuffer.Create((destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, destination.Height, SvgPixelColorSpace.Srgb, options.Limits.Raster);
            if (!created.Success) return Fail(SvgRenderError.AllocationLimitExceeded, "An offscreen render surface exceeded raster limits.", layer.Node);
            target = created.Buffer;
            offscreenDepth = offscreenDepth + 1;
        }
        if (layer.Paintable && !DrawOwn(layer.Node, target)) return false;
        int child = 0;
        while (child < layer.Children.Count)
        {
            if (!RenderLayer(layer.Children[child], target)) return false;
            child = child + 1;
        }
        if (local)
        {
            if (layer.Node.Filter != null)
            {
                target = Filter(layer.Node, target, destination);
                if (target == null) return false;
            }
            if (!ClipAndMask(layer, target)) return false;
            if (!Composite(target, destination, layer)) return false;
            offscreenDepth = offscreenDepth - 1;
        }
        return true;
    }

    bool DrawOwn(SvgSceneNode node, SvgPixelBuffer? destination)
    {
        if (!node.Visible) return true;
        if (node.Kind == SvgSceneNodeKind.Path) return DrawPath(node, (node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path, destination);
        if (node.Kind == SvgSceneNodeKind.Text)
        {
            SvgPathData? textPath = TextPath(node);
            if (textPath == null) return false;
            return DrawPath(node, textPath, destination);
        }
        if (node.Kind == SvgSceneNodeKind.Image) return DrawImage(node, destination);
        if (node.Kind == SvgSceneNodeKind.HostContent) return DrawHostContent(node, destination);
        return true;
    }

    bool DrawPath(SvgSceneNode node, SvgPathData? path, SvgPixelBuffer? destination)
    {
        if (path == null) return Fail(SvgRenderError.RasterizationFailed, "Renderable path geometry was missing.", node);
        int index = 0;
        while (index < node.PaintOrder.Count)
        {
            SvgPaintOrderComponent component = node.PaintOrder[index];
            if (component == SvgPaintOrderComponent.Fill && node.Fill != null && node.FillOpacity > 0)
            {
                if (!DrawPathComponent(node, path, node.Fill, node.FillOpacity, true, destination)) return false;
            }
            else if (component == SvgPaintOrderComponent.Stroke && node.Stroke != null && node.StrokeOpacity > 0)
            {
                if (!DrawPathComponent(node, path, node.Stroke, node.StrokeOpacity, false, destination)) return false;
            }
            index = index + 1;
        }
        return true;
    }

    bool DrawPathComponent(SvgSceneNode node, SvgPathData path, SvgSceneResource paint, double opacity, bool fill, SvgPixelBuffer? destination)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create((destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, destination.Height, SvgPixelColorSpace.Srgb, options.Limits.Raster);
        if (!created.Success) return Fail(SvgRenderError.AllocationLimitExceeded, "Path coverage allocation exceeded raster limits.", node);
        SvgPixelBuffer? coverage = created.Buffer;
        SvgMatrix? transform = NodeTransform(node);
        if (transform == null) return false;
        SvgPathRasterOptions raster = new SvgPathRasterOptions();
        if (fill) raster.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        else raster.StrokeColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        raster.FillRule = node.Path == null ? SvgFillRule.Nonzero : node.Path.FillRule;
        if (node.Path != null)
        {
            raster.StrokeStyle = node.Path.StrokeStyle;
            raster.VectorEffect = node.Path.VectorEffect;
            raster.ShapeRendering = node.Path.ShapeRendering;
        }
        raster.RasterOptions.Transform = transform;
        raster.RasterOptions.SamplesPerAxis = options.SamplesPerAxis;
        raster.RasterOptions.MaximumSubdivisionDepth = options.Limits.Geometry.MaxSubdivisionDepth;
        raster.RasterOptions.MaximumEdges = options.Limits.Geometry.MaxFlattenedSegments;
        raster.RasterOptions.MaximumWork = options.MaximumPixels - work;
        SvgPathRasterResult rasterized = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, (coverage ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), raster);
        if (!rasterized.Success)
        {
            SvgRenderError error = RasterLimit(rasterized) ? SvgRenderError.WorkLimitExceeded : SvgRenderError.RasterizationFailed;
            return Fail(error, "Path scan conversion failed.", node);
        }
        if (!TakeWork(rasterized.WorkUnits)) return false;
        SvgMatrixOperationResult inverse = transform.Invert();
        if (!inverse.Success) return Fail(SvgRenderError.RasterizationFailed, "The path-to-pixel transform was singular.", node);
        SvgPixelRect bounds = fill && rasterized.FillResult != null ? rasterized.FillResult.Bounds : rasterized.StrokeResult == null ? new SvgPixelRect(0, 0, 0, 0) : rasterized.StrokeResult.Bounds;
        SvgPaintSampleOptions paintOptions = new SvgPaintSampleOptions();
        paintOptions.Opacity = opacity;
        paintOptions.OutputColorSpace = destination.ColorSpace;
        paintOptions.RasterLimits = options.Limits.Raster;
        paintOptions.MaximumStops = options.Limits.Tree.MaxNodes;
        paintOptions.MaximumPatternDepth = options.Limits.References.MaxPaintServerDepth;
        paintOptions.MaximumPatternNodes = options.MaximumNodes;
        paintOptions.PatternSamplesPerAxis = options.SamplesPerAxis;
        int y = bounds.Y;
        while (y < bounds.Bottom)
        {
            int x = bounds.X;
            while (x < bounds.Right)
            {
                int coverageIndex = coverage.Offset + y * coverage.Stride + x * 4;
                byte amount = coverage.Data[coverageIndex + 3];
                if (amount > 0)
                {
                    SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(x + 0.5, y + 0.5));
                    if (!local.Success) return Fail(SvgRenderError.RasterizationFailed, "Paint sampling produced a non-finite point.", node);
                    SvgPaintSampleResult sampled = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(paint, (local.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), paintOptions);
                    if (!sampled.Success) return Fail(SvgRenderError.RasterizationFailed, "Paint sampling failed.", node);
                    if (sampled.HasPaint) CompositePixel(destination, x, y, sampled.Pixel, amount);
                }
                x = x + 1;
            }
            y = y + 1;
        }
        return TakeWork((long)bounds.Width * bounds.Height);
    }

    SvgPathData? TextPath(SvgSceneNode node)
    {
        if (options.TextPathCallback != null)
        {
            System.Threading.Tasks.Task<SvgRenderCallbackResult> task = System.Threading.Tasks.Task.Run(() => new SvgRenderCallbackResult(true, options.TextPathCallback(node)));
            System.IAsyncResult pending = task;
            pending.AsyncWaitHandle.WaitOne();
            if (task.IsFaulted)
            {
                System.AggregateException observed = task.Exception;
                if (observed != null) observed.GetBaseException();
                Fail(SvgRenderError.RasterizationFailed, "The text outline callback failed.", node);
                return null;
            }
            if (task.IsCanceled || task.Result.Path == null)
            {
                Fail(SvgRenderError.RasterizationFailed, "The text outline callback returned no geometry.", node);
                return null;
            }
            return task.Result.Path;
        }
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        double size = (node.Text ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontSize;
        double advance = size * 0.625;
        double width = size * 0.5;
        double x = node.Text.X;
        double top = node.Text.Y - size;
        int index = 0;
        while (index < node.Text.Text.Length)
        {
            char value = node.Text.Text[index];
            if (!char.IsWhiteSpace(value))
            {
                builder.MoveTo(x, top);
                builder.LineTo(x + width, top);
                builder.LineTo(x + width, node.Text.Y);
                builder.LineTo(x, node.Text.Y);
                builder.Close();
            }
            x = x + advance;
            index = index + 1;
        }
        return builder.Build();
    }

    bool DrawImage(SvgSceneNode node, SvgPixelBuffer? destination)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create((destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, destination.Height, SvgPixelColorSpace.Srgb, options.Limits.Raster);
        if (!created.Success) return Fail(SvgRenderError.AllocationLimitExceeded, "Image staging allocation exceeded raster limits.", node);
        SvgSceneNode imageNode = new SvgSceneNode(SvgSceneNodeKind.Image);
        imageNode.Image = node.Image;
        imageNode.AbsoluteTransform = (NodeTransform(node) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (imageNode.AbsoluteTransform == null) return false;
        SvgImageDrawOptions draw = new SvgImageDrawOptions();
        draw.MaximumPixels = options.MaximumPixels - work;
        SvgImageDrawResult drawn = new SvgImageRasterizer().DrawWithNodeAndDestinationAndOptions(imageNode, (created.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), draw);
        if (!drawn.Success)
        {
            SvgRenderError error = drawn.Error == SvgImageSampleError.WorkLimitExceeded ? SvgRenderError.WorkLimitExceeded : SvgRenderError.RasterizationFailed;
            return Fail(error, "Decoded image drawing failed.", node);
        }
        if (!TakeWork(drawn.PixelsTested)) return false;
        SvgCompositeOptions composite = CompositeOptions();
        SvgCompositeResult composited = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(created.Buffer, destination, composite);
        if (!composited.Success) return Fail(SvgRenderError.EffectFailed, "Image compositing failed.", node);
        return TakeWork(composited.PixelsProcessed);
    }

    bool DrawHostContent(SvgSceneNode node, SvgPixelBuffer? destination)
    {
        SvgSceneHostContentData host = (node.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (host.RasterPixels == null || host.Bounds == null) return true;
        SvgSceneResource resource = new SvgSceneResource("host-content", SvgSceneResourceKind.Image);
        (resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedContentAvailable = true;
        resource.Image.DecodedPixels = host.RasterPixels;
        resource.Image.IntrinsicSize = SvgIntrinsicSize.Create(host.RasterPixels.Width, host.RasterPixels.Height);
        SvgSceneNode image = new SvgSceneNode(SvgSceneNodeKind.Image);
        (image.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource = resource;
        image.Image.Bounds = host.Bounds;
        image.Image.ContentTransform = host.ContentTransform;
        image.AbsoluteTransform = node.AbsoluteTransform;
        image.Opacity = node.Opacity;
        return DrawImage(image, destination);
    }

    SvgPixelBuffer? Filter(SvgSceneNode node, SvgPixelBuffer? source, SvgPixelBuffer? background)
    {
        SvgFilterRenderOptions filterOptions = new SvgFilterRenderOptions();
        filterOptions.GraphOptions.Transform = (NodeTransform(node) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (filterOptions.GraphOptions.Transform == null) return null;
        filterOptions.GraphOptions.Clip = new SvgPixelRect(0, 0, (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width, source.Height);
        filterOptions.GraphOptions.FilterLimits = options.Limits.Filters;
        filterOptions.GraphOptions.RasterLimits = options.Limits.Raster;
        filterOptions.GraphOptions.SourceGraphic = source;
        filterOptions.GraphOptions.BackgroundImage = background;
        filterOptions.GraphOptions.DefaultColorSpace = node.ColorInterpolation == SvgColorSpace.SrgbLinear ? SvgPixelColorSpace.SrgbLinear : SvgPixelColorSpace.Srgb;
        filterOptions.MaximumPixels = options.MaximumPixels - work;
        SvgFilterRenderResult filtered = new SvgFilterRenderer().Render((node.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), filterOptions);
        if (!filtered.Success)
        {
            SvgRenderError error = filtered.Error == SvgFilterRenderError.WorkLimitExceeded ? SvgRenderError.WorkLimitExceeded : SvgRenderError.EffectFailed;
            Fail(error, "Filter evaluation failed.", node);
            return null;
        }
        if (!TakeWork(filtered.PixelsProcessed)) return null;
        SvgPixelBufferResult created = SvgPixelBuffer.Create(source.Width, source.Height, SvgPixelColorSpace.Srgb, options.Limits.Raster);
        if (!created.Success)
        {
            Fail(SvgRenderError.AllocationLimitExceeded, "Filtered output allocation exceeded raster limits.", node);
            return null;
        }
        SvgCompositeOptions composite = CompositeOptions();
        composite.DestinationX = filtered.Bounds.X;
        composite.DestinationY = filtered.Bounds.Y;
        SvgCompositeResult placed = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions((filtered.Output ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (created.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), composite);
        if (!placed.Success)
        {
            Fail(SvgRenderError.EffectFailed, "Filtered output placement failed.", node);
            return null;
        }
        if (!TakeWork(placed.PixelsProcessed)) return null;
        return created.Buffer;
    }

    bool ClipAndMask(SvgSceneLayer layer, SvgPixelBuffer? surface)
    {
        if (layer.OwnClipBounds == null && layer.Node.Clip == null && layer.Node.Mask == null) return true;
        SvgClipMaskOptions effect = new SvgClipMaskOptions();
        effect.RasterLimits = options.Limits.Raster;
        effect.RasterOptions.SamplesPerAxis = options.SamplesPerAxis;
        effect.RasterOptions.MaximumSubdivisionDepth = options.Limits.Geometry.MaxSubdivisionDepth;
        effect.RasterOptions.MaximumEdges = options.Limits.Geometry.MaxFlattenedSegments;
        effect.RasterOptions.MaximumWork = options.MaximumPixels - work;
        effect.PaintOptions.RasterLimits = options.Limits.Raster;
        effect.MaximumDepth = options.Limits.References.MaxEffectDepth;
        effect.MaximumNodes = options.MaximumNodes;
        effect.MaximumPixels = options.MaximumPixels - work;
        if (layer.OwnClipBounds != null)
        {
            SvgRectTransformResult transformed = pixelsFromWorld.TransformRect(layer.OwnClipBounds);
            if (!transformed.Success) return Fail(SvgRenderError.EffectFailed, "Overflow clipping produced non-finite bounds.", layer.Node);
            effect.Clips.Add(new SvgClipStackEntry(PixelBounds(transformed.Rectangle, surface)));
        }
        SvgMatrix? transform = NodeTransform(layer.Node);
        if (transform == null) return false;
        if (layer.Node.Clip != null) effect.Clips.Add(SvgClipStackEntry.CreateResource(layer.Node.Clip, transform));
        effect.Mask = layer.Node.Mask;
        effect.MaskTransform = transform;
        SvgClipMaskResult applied = new SvgClipMaskRenderer().Apply((surface ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), effect);
        if (!applied.Success)
        {
            SvgRenderError error = applied.Error == SvgClipMaskError.WorkLimitExceeded || applied.Error == SvgClipMaskError.NodeLimitExceeded || applied.Error == SvgClipMaskError.DepthLimitExceeded ? SvgRenderError.WorkLimitExceeded : SvgRenderError.EffectFailed;
            return Fail(error, "Clip or mask application failed.", layer.Node);
        }
        return TakeWork(applied.PixelsProcessed);
    }

    bool Composite(SvgPixelBuffer? source, SvgPixelBuffer? destination, SvgSceneLayer layer)
    {
        SvgCompositeOptions composite = CompositeOptions();
        composite.Opacity = layer.Opacity;
        composite.BlendMode = SvgBlendModeMath.Parse(layer.BlendMode);
        composite.BlendColorSpace = layer.ColorInterpolation == SvgColorSpace.SrgbLinear ? SvgBlendColorSpace.SrgbLinear : SvgBlendColorSpace.Srgb;
        composite.Isolated = layer.Isolated;
        SvgCompositeResult applied = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (destination ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), composite);
        if (!applied.Success)
        {
            SvgRenderError error = applied.Error == SvgCompositeError.WorkLimitExceeded ? SvgRenderError.WorkLimitExceeded : SvgRenderError.EffectFailed;
            return Fail(error, "Layer compositing failed.", layer.Node);
        }
        return TakeWork(applied.PixelsProcessed);
    }

    SvgCompositeOptions CompositeOptions()
    {
        SvgCompositeOptions result = new SvgCompositeOptions();
        result.MaximumPixels = options.MaximumPixels - work;
        result.RasterLimits = options.Limits.Raster;
        return result;
    }

    SvgMatrix? NodeTransform(SvgSceneNode node)
    {
        SvgMatrixOperationResult combined = pixelsFromWorld.Multiply(node.AbsoluteTransform);
        if (!combined.Success)
        {
            Fail(SvgRenderError.RasterizationFailed, "A scene transform produced a non-finite result.", node);
            return null;
        }
        return combined.Matrix;
    }

    SvgPixelRect PixelBounds(SvgRect? bounds, SvgPixelBuffer? surface)
    {
        int left = SvgRasterMath.FloorClamp((bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, 0, (surface ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width);
        int top = SvgRasterMath.FloorClamp(bounds.Y, 0, surface.Height);
        int right = SvgRasterMath.CeilingClamp(bounds.X + bounds.Width, 0, surface.Width);
        int bottom = SvgRasterMath.CeilingClamp(bounds.Y + bounds.Height, 0, surface.Height);
        if (right < left) right = left;
        if (bottom < top) bottom = top;
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    void CompositePixel(SvgPixelBuffer destination, int x, int y, SvgPremultipliedRgba8 pixel, byte coverage)
    {
        sampledPixel[0] = Multiply(pixel.Red, coverage);
        sampledPixel[1] = Multiply(pixel.Green, coverage);
        sampledPixel[2] = Multiply(pixel.Blue, coverage);
        sampledPixel[3] = Multiply(pixel.Alpha, coverage);
        int index = destination.Offset + y * destination.Stride + x * 4;
        int inverse = 255 - sampledPixel[3];
        destination.Data[index] = Add(sampledPixel[0], destination.Data[index], inverse);
        destination.Data[index + 1] = Add(sampledPixel[1], destination.Data[index + 1], inverse);
        destination.Data[index + 2] = Add(sampledPixel[2], destination.Data[index + 2], inverse);
        destination.Data[index + 3] = Add(sampledPixel[3], destination.Data[index + 3], inverse);
    }

    static byte Multiply(byte value, byte amount)
    {
        return (byte)((value * amount + 127) / 255);
    }

    static byte Add(byte source, byte destination, int inverse)
    {
        int value = source + (destination * inverse + 127) / 255;
        return (byte)(value > 255 ? 255 : value);
    }

    bool Visit(SvgSceneNode node)
    {
        if (options.CancellationToken.IsCancellationRequested) return Fail(SvgRenderError.Cancelled, "Rendering was cancelled.", node);
        if (nodes >= options.MaximumNodes) return Fail(SvgRenderError.WorkLimitExceeded, "The render node limit was exceeded.", node);
        nodes = nodes + 1;
        return true;
    }

    bool TakeWork(long amount)
    {
        if (amount < 0 || work > options.MaximumPixels - amount) return Fail(SvgRenderError.WorkLimitExceeded, "The cumulative render work limit was exceeded.", null);
        work = work + amount;
        result.PixelsProcessed = work;
        return true;
    }

    bool Fail(SvgRenderError error, string message, SvgSceneNode? node)
    {
        result.NodesVisited = nodes;
        result.PixelsProcessed = work;
        SvgSourceSpan? source = node == null || node.SourceNode == null ? null : node.SourceNode.SourceSpan;
        new SvgRenderer().SetFailure(result, error, message, source);
        return false;
    }

    static bool RasterLimit(SvgPathRasterResult result)
    {
        if (result.FillResult != null && (result.FillResult.Error == SvgRasterError.WorkLimitExceeded || result.FillResult.Error == SvgRasterError.EdgeLimitExceeded || result.FillResult.Error == SvgRasterError.SubdivisionLimitExceeded)) return true;
        if (result.StrokeResult != null && (result.StrokeResult.Error == SvgRasterError.WorkLimitExceeded || result.StrokeResult.Error == SvgRasterError.EdgeLimitExceeded || result.StrokeResult.Error == SvgRasterError.SubdivisionLimitExceeded)) return true;
        return result.StrokeError == SvgStrokeError.SubdivisionLimitExceeded || result.StrokeError == SvgStrokeError.OutputLimitExceeded || result.StrokeError == SvgStrokeError.DashLimitExceeded;
    }
}
