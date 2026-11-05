namespace svg;

/// <summary>Converts current resolved SVG semantics into a tagged backend-neutral scene.</summary>
public class SvgSceneBuilder : ISvgSceneBuilder
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSceneBuilder()
    {
    }

    /// <summary>Builds and attaches a normalized scene using bounded defaults.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <returns>The complete scene construction outcome.</returns>
    public SvgSceneBuildResult BuildWithDocument(SvgResolvedDocument document)
    {
        return new SvgSceneBuilder().BuildWithDocumentAndOptions(document, new SvgSceneBuildOptions());
    }

    /// <summary>Builds a normalized scene using explicit bounds and attachment behavior.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="options">The scene construction options.</param>
    /// <returns>The complete or bounded partial construction outcome.</returns>
    public SvgSceneBuildResult BuildWithDocumentAndOptions(SvgResolvedDocument document, SvgSceneBuildOptions options)
    {
        SvgScene scene = document == null || document.SourceDocument == null ? new SvgScene() : SvgScene.Create(document.SourceDocument);
        SvgSceneBuildResult result = new SvgSceneBuildResult(scene);
        if (document == null || options == null || document.Root == null || document.SourceDocument == null || options.MaxNodes < 1 || options.MaxResources < 0)
        {
            new SvgSceneBuilder().AddError(result, SvgDiagnosticCode.InvalidArgument, "Scene construction arguments are invalid.", null);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        if (!document.IsCurrent())
        {
            new SvgSceneBuilder().AddError(result, SvgDiagnosticCode.InvalidArgument, "The resolved document is stale.", (document.Root.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        SvgSceneBuildState state = new SvgSceneBuildState(document, options, result);
        SvgSceneNode? root = new SvgSceneBuilder().ConvertNode(document.Root, false, false, state);
        if (root == null || root.Kind != SvgSceneNodeKind.Group || !scene.SetRoot(root))
        {
            if (!state.LimitReached) new SvgSceneBuilder().AddError(result, SvgDiagnosticCode.InternalInvariant, "The resolved root could not be converted to a scene group.", (document.Root.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        SvgSceneBoundsOptions boundsOptions = new SvgSceneBoundsOptions();
        boundsOptions.MaximumNodes = options.MaxNodes;
        boundsOptions.MaximumMarkerVertices = new SvgSceneBuilder().OriginalOptions(state).Limits.Geometry.MaxPoints;
        SvgSceneBoundsResult bounds = new SvgSceneBounds().ComputeWithSceneAndDocumentAndOptions(scene, document, boundsOptions);
        if (!bounds.Success)
        {
            SvgDiagnosticCode code = bounds.Error == SvgDrawingBoundsError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgSceneBuilder().AddError(result, code, "Scene transform or drawing-bounds precomputation failed.", (document.Root.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        SvgSceneLayerOptions layerOptions = new SvgSceneLayerOptions();
        layerOptions.MaximumLayers = options.MaxNodes;
        SvgSceneLayerPlan layers = new SvgSceneLayerPlanner().PlanWithSceneAndDocumentAndOptions(scene, document, layerOptions);
        if (!layers.Success)
        {
            SvgDiagnosticCode code = layers.Error == SvgDrawingBoundsError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgSceneBuilder().AddError(result, code, "Scene painting and layer planning failed.", (document.Root.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        SvgRenderCommandOptions commandOptions = new SvgRenderCommandOptions();
        commandOptions.MaximumCommands = options.MaxNodes > 134217727 ? 2147483647 : options.MaxNodes * 16;
        SvgRenderCommandList commands = new SvgRenderCommandEnumerator().EnumerateWithSceneAndOptions(scene, commandOptions);
        if (!commands.Success)
        {
            SvgDiagnosticCode code = commands.Error == SvgRenderCommandError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgSceneBuilder().AddError(result, code, "Scene render-command enumeration failed.", (document.Root.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            new SvgSceneBuilder().Complete(result, document, options, false);
            return result;
        }
        bool success = !state.LimitReached;
        scene.Success = success;
        new SvgSceneBuilder().Complete(result, document, options, success);
        return result;
    }
void Complete(SvgSceneBuildResult result, SvgResolvedDocument? document, SvgSceneBuildOptions? options, bool success)
    {
        result.Success = success;
        result.Scene.Success = success;
        result.Scene.Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(result.Diagnostics);
        result.ResourceCount = (result.Scene.Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count;
        if (success && document != null && options != null && options.AttachToDocument) document.Scene = result.Scene;
    }
SvgSceneNode? ConvertNode(SvgResolvedNode? source, bool resourceContent, bool clippingContent, SvgSceneBuildState state)
    {
        if (source == null || state.LimitReached) return null;
        if (!resourceContent && !source.Active) return null;
        SvgSceneNodeKind kind = new SvgSceneBuilder().SceneKind(source, resourceContent);
        if ((int)kind < 0) return null;
        if (!new SvgSceneBuilder().TakeNode(source.SourceNode == null ? null : source.SourceNode.SourceSpan, state)) return null;
        SvgSceneNode result = new SvgSceneNode(kind);
        result.SetIdentity(source);
        result.Transform = source.Transform;
        result.LocalBounds = source.Bounds;
        new SvgSceneBuilder().ApplyStyle(result, source.ComputedStyle);
        new SvgSceneBuilder().ApplyObjectBoundingBox(result, source);
        if (kind == SvgSceneNodeKind.Group) new SvgSceneBuilder().PopulateGroup(result, source);
        if (kind == SvgSceneNodeKind.Path && !new SvgSceneBuilder().PopulatePath(result, source, clippingContent, state)) return null;
        if (kind == SvgSceneNodeKind.Text) new SvgSceneBuilder().PopulateText(result, source);
        if (kind == SvgSceneNodeKind.Image && !new SvgSceneBuilder().PopulateImage(result, source, state)) return null;
        if (kind == SvgSceneNodeKind.HostContent && !new SvgSceneBuilder().PopulateHostContent(result, source, state)) return null;
        new SvgSceneBuilder().ApplyResources(result, source, state);
        if (kind == SvgSceneNodeKind.Group)
        {
            int index = 0;
            while (index < source.Children.Count && !state.LimitReached)
            {
                SvgSceneNode? child = new SvgSceneBuilder().ConvertNode(source.Children[index], resourceContent, clippingContent, state);
                if (child != null) result.AddChild(child);
                index = index + 1;
            }
        }
        return result;
    }
SvgSceneNodeKind SceneKind(SvgResolvedNode source, bool resourceContent)
    {
        if (source.Kind == SvgResolvedNodeKind.Viewport) return SvgSceneNodeKind.Group;
        if (source.Kind == SvgResolvedNodeKind.Container || source.Kind == SvgResolvedNodeKind.Switch || source.Kind == SvgResolvedNodeKind.Use || source.Kind == SvgResolvedNodeKind.Link)
        {
            if (source.SourceNode != null && (source.SourceNode.ElementKind == SvgElementKind.Defs || source.SourceNode.ElementKind == SvgElementKind.Symbol) && !resourceContent) return (SvgSceneNodeKind)(-1);
            return SvgSceneNodeKind.Group;
        }
        if (source.Kind == SvgResolvedNodeKind.Shape)
        {
            if (source.Geometry == null || !source.Geometry.HasGeometry) return (SvgSceneNodeKind)(-1);
            return SvgSceneNodeKind.Path;
        }
        if (source.Kind == SvgResolvedNodeKind.Text)
        {
            if (source.SourceNode == null || source.SourceNode.Kind != SvgNodeKind.Element) return (SvgSceneNodeKind)(-1);
            return SvgSceneNodeKind.Text;
        }
        if (source.Kind == SvgResolvedNodeKind.EmbeddedContent)
        {
            if (source.EmbeddedContent == null || !source.EmbeddedContent.Success || !source.EmbeddedContent.HasGeometry || !source.EmbeddedContent.HasContent) return (SvgSceneNodeKind)(-1);
            if (source.EmbeddedContent.Kind == SvgEmbeddedContentKind.Image) return SvgSceneNodeKind.Image;
            return SvgSceneNodeKind.HostContent;
        }
        return (SvgSceneNodeKind)(-1);
    }
void ApplyStyle(SvgSceneNode node, SvgComputedStyle? style)
    {
        if (style == null) return;
        SvgComputedNumberResult opacity = style.GetNumber(SvgCssPropertyNames.Opacity);
        if (opacity.Success) node.Opacity = new SvgSceneBuilder().Clamp(opacity.Value);
        SvgComputedNumberResult fillOpacity = style.GetNumber(SvgCssPropertyNames.FillOpacity);
        if (fillOpacity.Success) node.FillOpacity = new SvgSceneBuilder().Clamp(fillOpacity.Value);
        SvgComputedNumberResult strokeOpacity = style.GetNumber(SvgCssPropertyNames.StrokeOpacity);
        if (strokeOpacity.Success) node.StrokeOpacity = new SvgSceneBuilder().Clamp(strokeOpacity.Value);
        node.Displayed = style.GetKeyword(SvgCssPropertyNames.Display) != "none";
        node.Visible = style.GetKeyword(SvgCssPropertyNames.Visibility) == "visible";
        SvgComputedValue? order = style.Find(SvgCssPropertyNames.PaintOrder);
        if (order != null && order.Kind == SvgComputedValueKind.PaintOrder && order.PaintOrder.Count == 3)
        {
            node.PaintOrder.Clear();
            int index = 0;
            while (index < order.PaintOrder.Count)
            {
                node.PaintOrder.Add(order.PaintOrder[index]);
                index = index + 1;
            }
        }
        string blend = style.GetKeyword(SvgCssPropertyNames.MixBlendMode);
        if (blend.Length != 0) node.BlendMode = blend;
        if (style.GetKeyword(SvgCssPropertyNames.ColorInterpolation) == "linearrgb") node.ColorInterpolation = SvgColorSpace.SrgbLinear;
        node.Isolated = style.GetKeyword(SvgCssPropertyNames.Isolation) == "isolate";
    }
void PopulateGroup(SvgSceneNode node, SvgResolvedNode source)
    {
        if (source.Viewport == null) return;
        (node.Group ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).EstablishesViewport = true;
        node.Group.ViewportBounds = source.Viewport.Bounds;
        node.Group.OverflowClip = source.Viewport.ClipRect;
        node.OverflowClip = source.Viewport.ClipRect;
    }
void ApplyObjectBoundingBox(SvgSceneNode node, SvgResolvedNode source)
    {
        if (source.LengthContext == null || !source.LengthContext.HasObjectBoundingBox || source.Bounds == null || source.Bounds.IsEmpty) return;
        node.HasObjectBoundingBox = true;
        node.ObjectBoundingBox = source.Bounds;
        node.ObjectBoundingBoxTransform = new SvgMatrix(source.Bounds.Width, 0, 0, source.Bounds.Height, source.Bounds.X, source.Bounds.Y);
    }
bool PopulatePath(SvgSceneNode node, SvgResolvedNode source, bool clippingContent, SvgSceneBuildState state)
    {
        SvgShapeResult? geometry = source.Geometry;
        if ((geometry ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgShapeKind.Path) (node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path = geometry.PathData;
        else
        {
            int limit = new SvgSceneBuilder().OriginalOptions(state).Limits.Geometry.MaxPathSegments;
            SvgShapePathResult converted = geometry.ToPathWithLimit(limit);
            if (!converted.Success)
            {
                new SvgSceneBuilder().AddError(state.Result, SvgDiagnosticCode.InvalidValue, "A resolved shape could not be normalized to path data.", (source.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
                return false;
            }
            (node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path = converted.PathData;
        }
        string ruleName = clippingContent ? SvgCssPropertyNames.ClipRule : SvgCssPropertyNames.FillRule;
        node.Path.FillRule = source.ComputedStyle != null && source.ComputedStyle.GetKeyword(ruleName) == "evenodd" ? SvgFillRule.EvenOdd : SvgFillRule.Nonzero;
        SvgStrokeGeometryOptions? stroke = new SvgDrawingBounds().ComputedStroke(source, state.Document, false);
        if (stroke != null)
        {
            node.Path.StrokeStyle = stroke.Style;
            node.Path.VectorEffect = stroke.VectorEffect;
        }
        string rendering = source.ComputedStyle == null ? "" : source.ComputedStyle.GetKeyword(SvgCssPropertyNames.ShapeRendering);
        if (rendering == "optimizespeed") node.Path.ShapeRendering = SvgShapeRendering.OptimizeSpeed;
        else if (rendering == "crispedges") node.Path.ShapeRendering = SvgShapeRendering.CrispEdges;
        else if (rendering == "geometricprecision") node.Path.ShapeRendering = SvgShapeRendering.GeometricPrecision;
        return node.Path.Path != null;
    }
void PopulateText(SvgSceneNode node, SvgResolvedNode source)
    {
        (node.Text ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Text = source.Text == null ? "" : source.Text;
        if (source.LengthContext != null && source.LengthContext.HasFontSize && SvgFiniteMath.IsFinite(source.LengthContext.FontSize) && source.LengthContext.FontSize > 0) node.Text.FontSize = source.LengthContext.FontSize;
        node.Text.X = new SvgSceneBuilder().TextPosition(source, SvgAttributeNames.X, SvgLengthPercentageBasis.ViewportWidth);
        node.Text.Y = new SvgSceneBuilder().TextPosition(source, SvgAttributeNames.Y, SvgLengthPercentageBasis.ViewportHeight);
    }
double TextPosition(SvgResolvedNode source, string name, SvgLengthPercentageBasis basis)
    {
        if (source.SourceNode == null || source.SourceNode.Kind != SvgNodeKind.Element || source.LengthContext == null) return 0;
        string text = "";
        int index = 0;
        while (index < source.SourceNode.Attributes.Count)
        {
            SvgAttribute attribute = source.SourceNode.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name)
            {
                text = attribute.Value;
                break;
            }
            index = index + 1;
        }
        if (text.Length == 0) return 0;
        SvgLengthListParseResult parsed = new SvgLengthListParser().ParseTextPosition(text, 100000, 8192);
        if (!parsed.Success || parsed.Value == null || parsed.Value.Items.Count == 0) return 0;
        SvgLengthEvaluationResult evaluated = parsed.Value.Items[0].Length.Evaluate(source.LengthContext, basis);
        return evaluated.Success ? evaluated.Value : 0;
    }
bool PopulateImage(SvgSceneNode node, SvgResolvedNode source, SvgSceneBuildState state)
    {
        SvgEmbeddedContentResult? embedded = source.EmbeddedContent;
        (node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds = (embedded ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds;
        node.Image.ContentTransform = (embedded.ContentTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        node.Image.HostContent = embedded.HostContent;
        if (embedded.ClipsOverflow) node.OverflowClip = embedded.ClipBounds;
        if (embedded.AspectRatio != null) node.Image.AspectRatio = embedded.AspectRatio;
        string rendering = source.ComputedStyle == null ? "" : source.ComputedStyle.GetKeyword(SvgCssPropertyNames.ImageRendering);
        if (rendering == "smooth") node.Image.Rendering = SvgImageRendering.Smooth;
        else if (rendering == "high-quality") node.Image.Rendering = SvgImageRendering.HighQuality;
        else if (rendering == "crisp-edges") node.Image.Rendering = SvgImageRendering.CrispEdges;
        else if (rendering == "pixelated") node.Image.Rendering = SvgImageRendering.Pixelated;
        else if (rendering == "optimizespeed") node.Image.Rendering = SvgImageRendering.OptimizeSpeed;
        else if (rendering == "optimizequality") node.Image.Rendering = SvgImageRendering.OptimizeQuality;
        string key = new SvgSceneBuilder().ImageKey(embedded, source);
        SvgSceneResource? resource = state.Result.Scene.FindResource(key);
        if (resource == null)
        {
            if (!new SvgSceneBuilder().TakeResource((source.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan, state)) return false;
            resource = new SvgSceneResource(key, SvgSceneResourceKind.Image);
            resource.SetSource(source.SourceNode);
            SvgImageResolveResult? image = embedded.ImageResource;
            SvgReferenceResult? reference = embedded.Reference;
            if (reference != null) (resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Uri = reference.ResolvedUri;
            if (image != null)
            {
                (resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IntrinsicSize = image.IntrinsicSize;
                resource.Image.DecodedContentAvailable = image.DecodedContentAvailable;
                if (image.Acquisition != null)
                {
                    resource.Image.Uri = image.Acquisition.FinalUri;
                    resource.Image.MediaType = image.Acquisition.EffectiveMediaType;
                    resource.Image.Bytes = SvgResourceCacheEntry.CopyBytes(image.Acquisition.Bytes);
                    new SvgSceneBuilder().DecodePng(resource, state, source.SourceNode == null ? null : source.SourceNode.SourceSpan);
                }
            }
            SvgHostContentCallbackResult? callback = embedded.CallbackResult;
            if (callback != null && callback.RasterPixels != null)
            {
                (resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedPixels = new SvgSceneBuilder().CopyPixels(callback.RasterPixels, state);
                if (resource.Image.DecodedPixels == null) return false;
                resource.Image.DecodedContentAvailable = true;
                if (resource.Image.IntrinsicSize == null) resource.Image.IntrinsicSize = callback.IntrinsicSize == null ? SvgIntrinsicSize.Create(callback.RasterPixels.Width, callback.RasterPixels.Height) : callback.IntrinsicSize;
            }
            state.Result.Scene.AddResource(resource);
        }
        node.Image.Resource = resource;
        return true;
    }
bool PopulateHostContent(SvgSceneNode node, SvgResolvedNode source, SvgSceneBuildState state)
    {
        SvgEmbeddedContentResult embedded = (source.EmbeddedContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgSceneHostContentData host = (node.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        host.Kind = embedded.Kind;
        host.Bounds = embedded.Bounds;
        host.ContentBounds = embedded.ContentBounds;
        host.ContentTransform = (embedded.ContentTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        host.Record = embedded.HostContent;
        if (embedded.ClipsOverflow) node.OverflowClip = embedded.ClipBounds;
        if (embedded.CallbackResult != null && embedded.CallbackResult.RasterPixels != null)
        {
            host.RasterPixels = new SvgSceneBuilder().CopyPixels(embedded.CallbackResult.RasterPixels, state);
            if (host.RasterPixels == null) return false;
        }
        return true;
    }
SvgPixelBuffer? CopyPixels(SvgPixelBuffer source, SvgSceneBuildState state)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create(source.Width, source.Height, source.ColorSpace, new SvgSceneBuilder().OriginalOptions(state).Limits.Raster);
        if (!created.Success || created.Buffer == null) return null;
        SvgPixelOperationResult copied = created.Buffer.CopyFrom(source, 0, 0);
        return copied.Success ? created.Buffer : null;
    }
string ImageKey(SvgEmbeddedContentResult embedded, SvgResolvedNode source)
    {
        if (embedded.Reference != null && embedded.Reference.ResolvedUri.Length != 0) return "image:" + embedded.Reference.ResolvedUri;
        return "image:" + source.SourcePath + ":" + source.InstancePath;
    }
void ApplyResources(SvgSceneNode node, SvgResolvedNode source, SvgSceneBuildState state)
    {
        if (node.Kind == SvgSceneNodeKind.Path || node.Kind == SvgSceneNodeKind.Text)
        {
            node.FillUsesContext = source.Fill != null && source.Fill.Kind == SvgPaintKind.ContextFill;
            node.StrokeUsesContext = source.Stroke != null && source.Stroke.Kind == SvgPaintKind.ContextStroke;
            if (!node.FillUsesContext) node.Fill = new SvgSceneBuilder().ResolvePaint(source.Fill, source, state);
            if (!node.StrokeUsesContext) node.Stroke = new SvgSceneBuilder().ResolvePaint(source.Stroke, source, state);
        }
        if (node.Kind == SvgSceneNodeKind.Path)
        {
            node.MarkerStart = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.MarkerStart, SvgEffectKind.Marker, SvgMarkerPlacement.Start, source, state);
            node.MarkerMid = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.MarkerMid, SvgEffectKind.Marker, SvgMarkerPlacement.Mid, source, state);
            node.MarkerEnd = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.MarkerEnd, SvgEffectKind.Marker, SvgMarkerPlacement.End, source, state);
        }
        node.Clip = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.ClipPath, SvgEffectKind.ClipPath, SvgMarkerPlacement.Mid, source, state);
        node.Mask = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.Mask, SvgEffectKind.Mask, SvgMarkerPlacement.Mid, source, state);
        node.Filter = new SvgSceneBuilder().ResolveFilterProperty(source, state);
    }
SvgSceneResource? ResolvePaint(SvgPaint? paint, SvgResolvedNode target, SvgSceneBuildState state)
    {
        if (paint == null || paint.Kind == SvgPaintKind.None) return null;
        if (paint.Kind == SvgPaintKind.Color) return new SvgSceneBuilder().SolidPaint(paint.Color, paint.Color == null ? 0 : paint.Color.Alpha, state);
        if (paint.Kind != SvgPaintKind.Url || target.SourceNode == null) return null;
        SvgReferenceResult reference = new SvgReferenceResolver().ResolveValue(target.SourceNode, paint.Url, null, new SvgSceneBuilder().ReferenceOptions(state));
        if (!reference.Success || reference.Target == null) return new SvgSceneBuilder().PaintFallback(paint, target, state);
        if (new SvgSceneBuilder().Contains(state.ActiveResources, reference.Target)) return new SvgSceneBuilder().PaintFallback(paint, target, state);
        SvgPaintServerResolveOptions options = new SvgSceneBuilder().PaintOptions(target, reference.Target, state);
        SvgPaintServerResult resolved = new SvgPaintServerResolver().ResolveWithNodeAndOptions(reference.Target, options);
        if (!resolved.Success || !resolved.Renderable) return new SvgSceneBuilder().PaintFallback(paint, target, state);
        if (resolved.GradientPaintKind == SvgGradientPaintKind.Solid) return new SvgSceneBuilder().SolidPaint(resolved.SolidColor, resolved.SolidAlpha, state);
        string key = "paint:" + reference.ResolvedUri + new SvgSceneBuilder().ContextKey(target.Bounds);
        SvgSceneResource? existing = state.Result.Scene.FindResource(key);
        if (existing != null) return existing;
        SvgSceneResourceKind kind = resolved.Kind == SvgPaintServerKind.Pattern ? SvgSceneResourceKind.PatternPaint : SvgSceneResourceKind.GradientPaint;
        if (!new SvgSceneBuilder().TakeResource(reference.Target.SourceSpan, state)) return null;
        SvgSceneResource resource = new SvgSceneResource(key, kind);
        resource.SetSource(reference.Target);
        if (kind == SvgSceneResourceKind.PatternPaint) new SvgSceneBuilder().PopulatePattern(resource, resolved, state);
        else new SvgSceneBuilder().PopulateGradient(resource, resolved);
        state.Result.Scene.AddResource(resource);
        return resource;
    }
SvgSceneResource? PaintFallback(SvgPaint paint, SvgResolvedNode target, SvgSceneBuildState state)
    {
        int index = 0;
        while (index < paint.Fallbacks.Count)
        {
            SvgSceneResource? resource = new SvgSceneBuilder().ResolvePaint(paint.Fallbacks[index], target, state);
            if (resource != null || paint.Fallbacks[index].Kind == SvgPaintKind.None) return resource;
            index = index + 1;
        }
        return null;
    }
SvgSceneResource? SolidPaint(SvgColor? color, double opacity, SvgSceneBuildState state)
    {
        if (color == null) return null;
        string key = "solid:" + color.ToText() + ":" + new SvgSceneBuilder().Clamp(opacity).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        SvgSceneResource? existing = state.Result.Scene.FindResource(key);
        if (existing != null) return existing;
        if (!new SvgSceneBuilder().TakeResource(null, state)) return null;
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.SolidPaint);
        (resource.SolidPaint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Color = color;
        resource.SolidPaint.Opacity = new SvgSceneBuilder().Clamp(opacity);
        state.Result.Scene.AddResource(resource);
        return resource;
    }
void PopulateGradient(SvgSceneResource resource, SvgPaintServerResult source)
    {
        (resource.GradientPaint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind = source.Kind;
        resource.GradientPaint.SpreadMethod = source.SpreadMethod;
        string interpolation = source.ComputedStyle == null ? "" : source.ComputedStyle.GetKeyword(SvgCssPropertyNames.ColorInterpolation);
        resource.GradientPaint.InterpolationSpace = interpolation == "linearrgb" ? SvgColorSpace.SrgbLinear : SvgColorSpace.Srgb;
        resource.GradientPaint.Transform = source.Transform;
        resource.GradientPaint.StartOrCenter = (source.Kind == SvgPaintServerKind.LinearGradient ? source.Start : source.Center) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.");
        resource.GradientPaint.EndOrFocus = (source.Kind == SvgPaintServerKind.LinearGradient ? source.End : source.Focus) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.");
        resource.GradientPaint.Radius = source.Radius;
        resource.GradientPaint.FocalRadius = source.FocalRadius;
        int index = 0;
        while (index < source.Stops.Count)
        {
            SvgGradientStop stop = source.Stops[index];
            SvgSceneGradientStopData copy = new SvgSceneGradientStopData();
            copy.Offset = stop.Offset;
            copy.Color = stop.Color;
            copy.Alpha = stop.EffectiveAlpha;
            resource.GradientPaint.Stops.Add(copy);
            index = index + 1;
        }
    }
void PopulatePattern(SvgSceneResource resource, SvgPaintServerResult source, SvgSceneBuildState state)
    {
        (resource.PatternPaint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TileBounds = source.TileBounds;
        resource.PatternPaint.Transform = source.Transform;
        resource.PatternPaint.ContentTransform = source.ContentTransform;
        resource.PatternPaint.ViewBox = source.ViewBox;
        resource.PatternPaint.AspectRatio = source.AspectRatio;
        resource.PatternPaint.Content = new SvgSceneBuilder().ConvertContent(source.Node, source.Content, state);
    }
SvgSceneResource? ResolveEffectProperty(string propertyName, SvgEffectKind expected, SvgMarkerPlacement placement, SvgResolvedNode target, SvgSceneBuildState state)
    {
        SvgReferenceResult? reference = new SvgSceneBuilder().PropertyReference(target, propertyName, state);
        if (reference == null || !reference.Success || reference.Target == null) return null;
        if (new SvgSceneBuilder().Contains(state.ActiveResources, reference.Target)) return null;
        SvgEffectResolveOptions options = new SvgSceneBuilder().EffectOptions(target, reference.Target, placement, state);
        SvgEffectResult resolved = new SvgEffectResolver().ResolveWithNodeAndOptions(reference.Target, options);
        if (!resolved.Success || !resolved.Renderable || resolved.Kind != expected) return null;
        string key = "effect:" + ((int)expected).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + reference.ResolvedUri + new SvgSceneBuilder().ContextKey(target.Bounds);
        if (expected == SvgEffectKind.Marker) key = key + ":" + ((int)placement).ToString(System.Globalization.CultureInfo.InvariantCulture);
        SvgSceneResource? existing = state.Result.Scene.FindResource(key);
        if (existing != null) return existing;
        if (!new SvgSceneBuilder().TakeResource(reference.Target.SourceSpan, state)) return null;
        SvgSceneResourceKind kind = expected == SvgEffectKind.Marker ? SvgSceneResourceKind.Marker : expected == SvgEffectKind.ClipPath ? SvgSceneResourceKind.Clip : SvgSceneResourceKind.Mask;
        SvgSceneResource resource = new SvgSceneResource(key, kind);
        resource.SetSource(reference.Target);
        if (kind == SvgSceneResourceKind.Marker) new SvgSceneBuilder().PopulateMarker(resource, resolved, state);
        if (kind == SvgSceneResourceKind.Clip) new SvgSceneBuilder().PopulateClip(resource, resolved, state);
        if (kind == SvgSceneResourceKind.Mask) new SvgSceneBuilder().PopulateMask(resource, resolved, state);
        state.Result.Scene.AddResource(resource);
        return resource;
    }
void PopulateMarker(SvgSceneResource resource, SvgEffectResult source, SvgSceneBuildState state)
    {
        (resource.Marker ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Units = source.MarkerUnits;
        resource.Marker.OrientKind = source.OrientKind;
        resource.Marker.OrientAngle = source.AppliedAngle;
        resource.Marker.Viewport = source.Viewport;
        resource.Marker.ReferencePoint = new SvgPoint(source.RefX, source.RefY);
        resource.Marker.Transform = source.Transform;
        resource.Marker.Content = new SvgSceneBuilder().ConvertContent(source.Node, source.Children, state);
    }
void PopulateClip(SvgSceneResource resource, SvgEffectResult source, SvgSceneBuildState state)
    {
        (resource.Clip ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Units = source.Units;
        resource.Clip.Transform = source.Transform;
        resource.Clip.Content = new SvgSceneBuilder().ConvertContent(source.Node, source.Children, state);
    }
void PopulateMask(SvgSceneResource resource, SvgEffectResult source, SvgSceneBuildState state)
    {
        (resource.Mask ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Units = source.Units;
        resource.Mask.ContentUnits = source.ContentUnits;
        resource.Mask.MaskType = source.MaskType;
        string interpolation = source.ComputedStyle == null ? "" : source.ComputedStyle.GetKeyword(SvgCssPropertyNames.ColorInterpolation);
        resource.Mask.ColorInterpolation = interpolation == "linearrgb" ? SvgColorSpace.SrgbLinear : SvgColorSpace.Srgb;
        resource.Mask.Region = source.UserRegion;
        resource.Mask.ContentTransform = source.ContentTransform;
        resource.Mask.Content = new SvgSceneBuilder().ConvertContent(source.Node, source.Children, state);
    }
SvgSceneResource? ResolveFilterProperty(SvgResolvedNode target, SvgSceneBuildState state)
    {
        SvgReferenceResult? reference = new SvgSceneBuilder().PropertyReference(target, SvgCssPropertyNames.Filter, state);
        if (reference == null || !reference.Success || reference.Target == null || reference.Target.ElementKind != SvgElementKind.Filter) return null;
        if (new SvgSceneBuilder().Contains(state.ActiveResources, reference.Target)) return null;
        SvgFilterResolveOptions options = new SvgSceneBuilder().FilterOptions(target, reference.Target, state);
        SvgFilterResult resolved = new SvgFilterResolver().ResolveWithNodeAndOptions(reference.Target, options);
        if (!resolved.Success || !resolved.Renderable) return null;
        string key = "filter:" + reference.ResolvedUri + new SvgSceneBuilder().ContextKey(target.Bounds);
        SvgSceneResource? existing = state.Result.Scene.FindResource(key);
        if (existing != null) return existing;
        if (!new SvgSceneBuilder().TakeResource(reference.Target.SourceSpan, state)) return null;
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.Filter);
        resource.SetSource(reference.Target);
        (resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Units = resolved.FilterUnits;
        resource.Filter.PrimitiveUnits = resolved.PrimitiveUnits;
        resource.Filter.Region = resolved.UserRegion;
        resource.Filter.PrimitiveTransform = resolved.PrimitiveTransform;
        int index = 0;
        while (index < resolved.Primitives.Count)
        {
            SvgFilterPrimitive primitive = resolved.Primitives[index];
            SvgSceneFilterPrimitiveData copy = new SvgSceneFilterPrimitiveData();
            copy.Index = primitive.Index;
            copy.ElementKind = primitive.ElementKind;
            copy.ColorSpace = primitive.ColorSpace;
            copy.Inputs = new System.Collections.Generic.List<SvgFilterInput>(primitive.Inputs);
            copy.Attributes = new System.Collections.Generic.List<SvgFilterPrimitiveAttribute>(primitive.Attributes);
            copy.Children = new System.Collections.Generic.List<SvgFilterPrimitiveChild>(primitive.Children);
            copy.ResultName = primitive.ResultName;
            copy.Region = primitive.UserRegion;
            copy.PrimitiveRegion = primitive.Region;
            copy.OffsetX = new SvgSceneBuilder().NormalizedPrimitiveLength(primitive, SvgAttributeNames.Dx, resolved.PrimitiveUnits, target.LengthContext, SvgLengthPercentageBasis.ViewportWidth);
            copy.OffsetY = new SvgSceneBuilder().NormalizedPrimitiveLength(primitive, SvgAttributeNames.Dy, resolved.PrimitiveUnits, target.LengthContext, SvgLengthPercentageBasis.ViewportHeight);
            copy.FloodColor = primitive.ComputedStyle == null ? null : primitive.ComputedStyle.GetColor(SvgCssPropertyNames.FloodColor);
            copy.LightingColor = primitive.ComputedStyle == null ? null : primitive.ComputedStyle.GetColor(SvgCssPropertyNames.LightingColor);
            SvgComputedNumberResult? floodOpacity = primitive.ComputedStyle == null ? null : primitive.ComputedStyle.GetNumber(SvgCssPropertyNames.FloodOpacity);
            copy.FloodOpacity = floodOpacity == null || !floodOpacity.Success ? 1 : floodOpacity.Value;
            copy.ImageResource = new SvgSceneBuilder().FilterImageResource(primitive, state);
            SvgFilterPrimitiveAttribute? aspect = primitive.FindAttribute(SvgAttributeNames.PreserveAspectRatio);
            if (aspect != null && aspect.TypedValue != null && aspect.TypedValue.Success && aspect.TypedValue.ValueKind == SvgTypedAttributeKind.AspectRatio && aspect.TypedValue.Value != null) copy.ImageAspectRatio = (SvgAspectRatio)aspect.TypedValue.Value;
            resource.Filter.Primitives.Add(copy);
            index = index + 1;
        }
        state.Result.Scene.AddResource(resource);
        return resource;
    }
SvgSceneResource? FilterImageResource(SvgFilterPrimitive primitive, SvgSceneBuildState state)
    {
        if (primitive.ElementKind != SvgElementKind.FeImage || primitive.Reference == null || !primitive.Reference.Success) return null;
        if (primitive.Reference.Target != null)
        {
            SvgResolvedNode? target = state.Document.FindSource(primitive.Reference.Target);
            if (target == null || target.Kind != SvgResolvedNodeKind.EmbeddedContent || target.EmbeddedContent == null || target.EmbeddedContent.Kind != SvgEmbeddedContentKind.Image) return null;
            SvgSceneNode image = new SvgSceneNode(SvgSceneNodeKind.Image);
            return new SvgSceneBuilder().PopulateImage(image, target, state) ? (image.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource : null;
        }
        SvgImageResolveResult? resolved = primitive.Image;
        if (resolved == null || !resolved.Success) return null;
        string key = "image:" + primitive.Reference.ResolvedUri;
        SvgSceneResource? existing = state.Result.Scene.FindResource(key);
        if (existing != null) return existing;
        if (!new SvgSceneBuilder().TakeResource(primitive.SourceNode == null ? null : primitive.SourceNode.SourceSpan, state)) return null;
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.Image);
        (resource.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Uri = primitive.Reference.ResolvedUri;
        resource.Image.IntrinsicSize = resolved.IntrinsicSize;
        resource.Image.DecodedContentAvailable = resolved.DecodedContentAvailable;
        if (resolved.Acquisition != null)
        {
            resource.Image.Uri = resolved.Acquisition.FinalUri;
            resource.Image.MediaType = resolved.Acquisition.EffectiveMediaType;
            resource.Image.Bytes = SvgResourceCacheEntry.CopyBytes(resolved.Acquisition.Bytes);
            new SvgSceneBuilder().DecodePng(resource, state, primitive.SourceNode == null ? null : primitive.SourceNode.SourceSpan);
        }
        state.Result.Scene.AddResource(resource);
        return resource;
    }
void DecodePng(SvgSceneResource? resource, SvgSceneBuildState state, SvgSourceSpan? source)
    {
        if (resource == null || resource.Image == null || resource.Image.Bytes == null || resource.Image.Bytes.Length == 0 || !string.Equals(resource.Image.MediaType, SvgMimeTypes.Png, System.StringComparison.OrdinalIgnoreCase)) return;
        SvgPngDecodeResult decoded = new SvgPngCodec().DecodeWithBytesAndLimits(resource.Image.Bytes, new SvgSceneBuilder().OriginalOptions(state).Limits.Raster);
        if (decoded.Success)
        {
            resource.Image.DecodedPixels = decoded.Pixels;
            resource.Image.DecodedContentAvailable = true;
            return;
        }
        SvgDiagnosticCode code = decoded.Error == SvgPngDecodeError.ChunkLimitExceeded || decoded.Error == SvgPngDecodeError.DimensionLimitExceeded || decoded.Error == SvgPngDecodeError.DecodedSizeLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.ResourceFailed;
        new SvgSceneBuilder().AddError(state.Result, code, "The PNG image could not be decoded.", source);
    }
double NormalizedPrimitiveLength(SvgFilterPrimitive primitive, string name, SvgPaintServerUnits units, SvgLengthContext? context, SvgLengthPercentageBasis basis)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Length || attribute.TypedValue.Value == null) return 0;
        SvgLength length = (SvgLength)attribute.TypedValue.Value;
        if (units == SvgPaintServerUnits.ObjectBoundingBox)
        {
            if (length.Unit == SvgLengthUnit.Number) return length.Value;
            if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
            return 0;
        }
        SvgLengthEvaluationResult result = length.Evaluate((context ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), basis);
        return result.Success ? result.Value : 0;
    }
SvgSceneNode? ConvertContent(SvgNode? source, System.Collections.Generic.List<SvgNode>? children, SvgSceneBuildState state)
    {
        if (source == null || children == null || new SvgSceneBuilder().Contains(state.ActiveResources, source)) return null;
        if (!new SvgSceneBuilder().TakeNode(source.SourceSpan, state)) return null;
        state.ActiveResources.Add(source);
        SvgSceneNode root = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgResolvedNode? resolvedRoot = state.Document.FindSource(source);
        if (resolvedRoot != null)
        {
            root.SetIdentity(resolvedRoot);
            root.Clip = new SvgSceneBuilder().ResolveEffectProperty(SvgCssPropertyNames.ClipPath, SvgEffectKind.ClipPath, SvgMarkerPlacement.Mid, resolvedRoot, state);
        }
        int index = 0;
        while (index < children.Count && !state.LimitReached)
        {
            SvgResolvedNode? resolved = state.Document.FindSource(children[index]);
            bool clippingContent = source.ElementKind == SvgElementKind.ClipPath;
            SvgSceneNode? child = new SvgSceneBuilder().ConvertNode(resolved, true, clippingContent, state);
            if (child != null) root.AddChild(child);
            index = index + 1;
        }
        state.ActiveResources.RemoveAt(state.ActiveResources.Count - 1);
        return root;
    }
SvgReferenceResult? PropertyReference(SvgResolvedNode target, string propertyName, SvgSceneBuildState state)
    {
        if (target.ComputedStyle == null || target.SourceNode == null) return null;
        SvgComputedValue? value = target.ComputedStyle.Find(propertyName);
        if (value == null || value.Kind == SvgComputedValueKind.Keyword && value.Keyword == "none") return null;
        string iri = "";
        if (value.Kind == SvgComputedValueKind.Reference && value.Reference != null) iri = value.Reference.Value;
        else
        {
            SvgIriParseResult parsed = new SvgIriParser().ParseUrl(value.CssText, new SvgSceneBuilder().OriginalOptions(state).Limits.Css.MaxSelectorTokens);
            if (parsed.Success) iri = (parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;
        }
        if (iri.Length == 0) return null;
        return new SvgReferenceResolver().ResolveValue(target.SourceNode, iri, value.Source, new SvgSceneBuilder().ReferenceOptions(state));
    }
SvgPaintServerResolveOptions PaintOptions(SvgResolvedNode target, SvgNode resource, SvgSceneBuildState state)
    {
        SvgResolveOptions original = new SvgSceneBuilder().OriginalOptions(state);
        SvgPaintServerResolveOptions result = new SvgPaintServerResolveOptions();
        result.ReferenceOptions = new SvgSceneBuilder().ReferenceOptions(state);
        result.Limits = original.Limits.References;
        result.LengthContext = (target.LengthContext ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.TargetBounds = target.Bounds;
        result.StyleSheets = new SvgSceneBuilder().StyleSheets(state);
        result.StyleOptions = original.StyleOptions;
        SvgResolvedNode? resolved = state.Document.FindSource(resource);
        result.ComputedStyle = resolved == null ? null : resolved.ComputedStyle;
        result.MaxStops = original.Limits.Tree.MaxNodes;
        return result;
    }
SvgImageResolveOptions FilterImageOptions(SvgSceneBuildState state)
    {
        SvgResolveOptions original = new SvgSceneBuilder().OriginalOptions(state);
        SvgLoadOptions load = new SvgLoadOptions();
        load.Mode = (state.Document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ProcessingMode;
        load.BaseUri = state.Document.SourceDocument.BaseUri;
        load.ContentType = state.Document.SourceDocument.ContentType;
        load.Limits = original.Limits;
        load.ResourcePolicy = original.ResourcePolicy;
        load.ResourceProvider = original.ResourceProvider;
        load.CancellationToken = original.CancellationToken;
        load.DiagnosticBehavior = original.DiagnosticBehavior;
        SvgImageResolveOptions result = new SvgImageResolveOptions();
        result.LoadOptions = load;
        result.Context = new SvgNestedDocumentContext(original.Limits.Resources);
        return result;
    }
SvgEffectResolveOptions EffectOptions(SvgResolvedNode target, SvgNode resource, SvgMarkerPlacement placement, SvgSceneBuildState state)
    {
        SvgResolveOptions original = new SvgSceneBuilder().OriginalOptions(state);
        SvgEffectResolveOptions result = new SvgEffectResolveOptions();
        result.ReferenceOptions = new SvgSceneBuilder().ReferenceOptions(state);
        result.Limits = original.Limits.References;
        result.LengthContext = (target.LengthContext ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.TargetBounds = target.Bounds;
        result.MarkerPlacement = placement;
        result.StyleSheets = new SvgSceneBuilder().StyleSheets(state);
        result.StyleOptions = original.StyleOptions;
        SvgResolvedNode? resolved = state.Document.FindSource(resource);
        result.ComputedStyle = resolved == null ? null : resolved.ComputedStyle;
        result.MaxContentNodes = original.Limits.Tree.MaxNodes;
        return result;
    }
SvgFilterResolveOptions FilterOptions(SvgResolvedNode target, SvgNode resource, SvgSceneBuildState state)
    {
        SvgResolveOptions original = new SvgSceneBuilder().OriginalOptions(state);
        SvgFilterResolveOptions result = new SvgFilterResolveOptions();
        result.ReferenceOptions = new SvgSceneBuilder().ReferenceOptions(state);
        result.ImageOptions = new SvgSceneBuilder().FilterImageOptions(state);
        result.ReferenceLimits = original.Limits.References;
        result.FilterLimits = original.Limits.Filters;
        result.LengthContext = (target.LengthContext ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.TargetBounds = target.Bounds;
        result.StyleSheets = new SvgSceneBuilder().StyleSheets(state);
        result.StyleOptions = original.StyleOptions;
        SvgResolvedNode? resolved = state.Document.FindSource(resource);
        result.ComputedStyle = resolved == null ? null : resolved.ComputedStyle;
        result.MaxNameCharacters = original.Limits.Tree.MaxNameCharacters;
        return result;
    }
SvgReferenceResolveOptions ReferenceOptions(SvgSceneBuildState state)
    {
        SvgResolveOptions original = new SvgSceneBuilder().OriginalOptions(state);
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.ExternalDocument = original.ExternalDocument;
        result.MaxUriCharacters = original.Limits.Resources.MaxUriCharacters;
        result.MaxBaseDepth = original.Limits.Tree.MaxDepth;
        return result;
    }
SvgResolveOptions OriginalOptions(SvgSceneBuildState state)
    {
        return state.Document.CacheKey == null || state.Document.CacheKey.Options == null ? new SvgResolveOptions() : state.Document.CacheKey.Options;
    }
System.Collections.Generic.List<SvgStyleSheet> StyleSheets(SvgSceneBuildState state)
    {
        System.Collections.Generic.List<SvgStyleSheet> result = new System.Collections.Generic.List<SvgStyleSheet>();
        if (state.Document.StyleSheets == null) return result;
        int index = 0;
        while (index < state.Document.StyleSheets.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = state.Document.StyleSheets.StyleSheets[index];
            if (entry != null && entry.StyleSheet != null) result.Add(entry.StyleSheet);
            index = index + 1;
        }
        return result;
    }
bool TakeNode(SvgSourceSpan? source, SvgSceneBuildState state)
    {
        if (state.Result.NodeCount < state.Options.MaxNodes)
        {
            state.Result.NodeCount = state.Result.NodeCount + 1;
            return true;
        }
        if (!state.LimitReached) new SvgSceneBuilder().AddError(state.Result, SvgDiagnosticCode.InputLimitExceeded, "Scene construction exceeded MaxNodes.", source);
        state.LimitReached = true;
        return false;
    }
bool TakeResource(SvgSourceSpan? source, SvgSceneBuildState state)
    {
        if ((state.Result.Scene.Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count < state.Options.MaxResources) return true;
        if (!state.LimitReached) new SvgSceneBuilder().AddError(state.Result, SvgDiagnosticCode.InputLimitExceeded, "Scene construction exceeded MaxResources.", source);
        state.LimitReached = true;
        return false;
    }
bool Contains(System.Collections.Generic.List<SvgNode> nodes, SvgNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], target)) return true;
            index = index + 1;
        }
        return false;
    }
string ContextKey(SvgRect? bounds)
    {
        if (bounds == null) return ":none";
        return ":" + bounds.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + bounds.Y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + bounds.Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + bounds.Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }
double Clamp(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
void AddError(SvgSceneBuildResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
