namespace svg;

/// <summary>Orchestrates current SVG semantic stages with version-owned bounded caching.</summary>
public class SvgDocumentResolver : ISvgDocumentResolver
{
    System.Collections.Generic.List<SvgResolvedCacheEntry> cache;
    SvgStyleResolver styleResolver;
    long cacheHits;
    long cacheMisses;
    long invalidatedEntries;
    long evictedEntries;
    long accessClock;

    /// <summary>Creates an empty document and computed-style cache.</summary>
    public SvgDocumentResolver()
    {
        cache = new System.Collections.Generic.List<SvgResolvedCacheEntry>();
        styleResolver = new SvgStyleResolver();
        cacheHits = 0;
        cacheMisses = 0;
        invalidatedEntries = 0;
        evictedEntries = 0;
        accessClock = 0;
    }

    /// <summary>Resolves an editable document using secure deterministic defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The current resolved snapshot.</returns>
    public SvgResolvedDocument Resolve(SvgDocument document)
    {
        return ResolveWithOptions(document, new SvgResolveOptions());
    }

    /// <summary>Resolves an editable document through current style, conditional, viewport, structure, geometry, reference, and resource stages.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The host environment, resource policy, security limits, and cache bounds.</param>
    /// <returns>A cached current snapshot or a newly resolved document.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgResolvedDocument ResolveWithOptions(SvgDocument document, SvgResolveOptions options)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        if (options == null) throw new System.ArgumentNullException("options");
        if (!ValidOptions(options) || document.DocumentElement == null) return Failure(document, "Document resolution arguments are invalid.");
        if (options.CancellationToken.IsCancellationRequested)
        {
            cacheMisses = cacheMisses + 1;
            return Build(document, options);
        }
        int found = Find(document, options);
        if (found >= 0)
        {
            cacheHits = cacheHits + 1;
            cache[found].AccessOrder = NextAccess();
            return cache[found].Result;
        }
        cacheMisses = cacheMisses + 1;
        SvgResolvedDocument result = Build(document, options);
        if (!result.Diagnostics.Exists(diagnostic => diagnostic.Code == SvgDiagnosticCode.Cancelled)) Add(result, options.MaxCacheEntries);
        return result;
    }

    /// <summary>Invalidates every cached snapshot owned by one editable document.</summary>
    /// <param name="document">The editable source document.</param>
    /// <returns>The removed cache-entry count.</returns>
    public int Invalidate(SvgDocument? document)
    {
        if (document == null) return 0;
        int removed = 0;
        int index = cache.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(cache[index].Result.SourceDocument, document))
            {
                cache.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        invalidatedEntries = invalidatedEntries + removed;
        styleResolver.InvalidateTree(document.DocumentNode);
        return removed;
    }

    /// <summary>Clears every resolved-document and computed-style cache entry.</summary>
    /// <returns>The removed resolved-document entry count.</returns>
    public int ClearCache()
    {
        int removed = cache.Count;
        cache.Clear();
        invalidatedEntries = invalidatedEntries + removed;
        styleResolver.InvalidateAll();
        return removed;
    }

    /// <summary>Returns current resolved-document cache counters.</summary>
    /// <returns>An independent statistics snapshot.</returns>
    public SvgResolvedDocumentCacheStatistics GetStatistics()
    {
        return new SvgResolvedDocumentCacheStatistics(cache.Count, cacheHits, cacheMisses, invalidatedEntries, evictedEntries);
    }

    SvgResolvedDocument Build(SvgDocument document, SvgResolveOptions options)
    {
        SvgResolvedDocument result = new SvgResolvedDocument(document);
        result.CacheKey = new SvgResolvedCacheKey(document, options);
        SvgDiagnosticBag bag = new SvgDiagnosticBag(options.MaxDiagnostics, options.DiagnosticBehavior);
        SvgResolvedBuildState state = new SvgResolvedBuildState(result, options, bag);
        AppendDiagnostics(null, document.Diagnostics, "", state);
        if (!CheckCancellation(document.SourceSpan, state)) return Complete(result, state);

        state.StyleOptions = CopyStyleOptions(options);
        SvgLoadOptions loadOptions = CollectionOptions(document, options);
        state.StyleSheets = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, options.CallerStyleSheets, loadOptions, value => state.StyleOptions.StyleEnvironment.Matches(value, options.Limits.Css));
        result.StyleSheets = state.StyleSheets;
        CopyResources(state.StyleSheets.Resources, result.Resources);
        state.NestedResources = new SvgNestedDocumentContext(options.Limits.Resources);
        state.NestedResources.AccountedCount = result.Resources.RequestCount;
        state.NestedResources.InheritedEncodedBytes = result.Resources.EncodedBytes;
        AppendDiagnostics(null, state.StyleSheets.Diagnostics, "", state);
        if (!bag.ShouldContinue()) return Complete(result, state);

        state.Styles = styleResolver.ResolveDocument(document, state.StyleSheets, state.StyleOptions);
        result.ComputedStyles = state.Styles;
        state.ActiveStyleSheets = ActiveSheets(state.StyleSheets, state.StyleOptions.StyleEnvironment, options.Limits.Css);
        state.Attributes = CreateAttributes(options);
        state.ConditionalOptions = CreateConditionalOptions(options);
        SvgLengthContext initialContext = InitialContext(options);
        result.Root = BuildNode(document.DocumentElement, null, "0", "", null, true, initialContext, null, null, 0, state);
        return Complete(result, state);
    }

    static SvgResolvedDocument Complete(SvgResolvedDocument result, SvgResolvedBuildState state)
    {
        if (state.NestedResources != null)
        {
            result.Resources.RequestCount = result.Resources.RequestCount + state.NestedResources.Resources.RequestCount;
            result.Resources.LoadedCount = result.Resources.LoadedCount + state.NestedResources.Resources.LoadedCount;
            result.Resources.EncodedBytes = result.Resources.EncodedBytes + state.NestedResources.Resources.EncodedBytes;
        }
        result.Diagnostics = state.Diagnostics.Snapshot();
        result.Success = result.Root != null && !state.Diagnostics.HasErrors && !state.Cancelled && !state.LimitReached;
        return result;
    }

    static SvgResolvedDocument Failure(SvgDocument? document, string message)
    {
        SvgResolvedDocument result = new SvgResolvedDocument(document);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, message, document == null ? null : document.SourceSpan));
        return result;
    }

    static SvgResolvedNode? BuildNode(SvgNode? source, SvgResolvedNode? parent, string sourcePath, string instancePath, SvgUseInstance? instance, bool parentActive, SvgLengthContext lengthContext, SvgViewportResult? parentViewport, SvgConditionalResult? precomputed, int depth, SvgResolvedBuildState state)
    {
        if (!CanBuild(source, depth, state)) return null;
        SvgResolvedNode result = new SvgResolvedNode(state.Document, parent, source, sourcePath, instancePath, instance);
        state.NodeCount = state.NodeCount + 1;
        state.Document.Nodes.Add(result);
        if (parent != null) parent.Children.Add(result);
        if ((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind != SvgNodeKind.Element)
        {
            result.LengthContext = new SvgUnitContextResolver().Inherit(lengthContext);
            result.Kind = source.Kind == SvgNodeKind.Text || source.Kind == SvgNodeKind.CData ? SvgResolvedNodeKind.Text : SvgResolvedNodeKind.Other;
            result.Text = source.Value;
            result.Active = parentActive;
            result.Renderable = parentActive && result.Kind == SvgResolvedNodeKind.Text;
            result.Success = true;
            return result;
        }

        result.Kind = Kind(source);
        result.ComputedStyle = instance == null ? (state.Styles ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Find(source) : instance.ComputedStyle;
        if (result.ComputedStyle != null)
        {
            result.ComputedTransform = result.ComputedStyle.GetTransformList();
            result.Fill = result.ComputedStyle.GetPaint(SvgCssPropertyNames.Fill);
            result.Stroke = result.ComputedStyle.GetPaint(SvgCssPropertyNames.Stroke);
            AppendDiagnostics(result, result.ComputedStyle.Diagnostics, result.InstancePath, state);
        }
        bool rootElement = parent == null || result.Instance != null && result.Instance.IsInstanceRoot && source.OwnerDocument != null && object.ReferenceEquals(source.OwnerDocument.DocumentElement, source);
        result.LengthContext = new SvgUnitContextResolver().Resolve(result.ComputedStyle, lengthContext, rootElement);
        result.Conditional = precomputed == null ? new SvgConditionalProcessing().EvaluateWithNodeAndOptions(source, (state.ConditionalOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))) : precomputed;
        AppendDiagnostics(result, result.Conditional.Diagnostics, result.InstancePath, state);
        result.Active = parentActive && result.Conditional.Success && result.Conditional.Matches;
        if (!CheckCancellation(source.SourceSpan, state)) return FinishNode(result);

        if (source.ElementKind == SvgElementKind.Switch)
        {
            result.Switch = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(source, (state.ConditionalOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            if (result.Switch.Candidates.Count == 0) AppendDiagnostics(result, result.Switch.Diagnostics, result.InstancePath, state);
        }

        ResolvePayload(result, result.LengthContext, parentViewport, state);
        SvgLengthContext payloadContext = result.Viewport == null || result.Viewport.LengthContext == null ? result.LengthContext : result.Viewport.LengthContext;
        result.LengthContext = new SvgUnitContextResolver().WithObjectBoundingBox(payloadContext, result.Bounds);
        SvgLengthContext childContext = ChildContext(result, result.LengthContext);
        SvgViewportResult? childViewport = result.Viewport == null ? parentViewport : result.Viewport;
        if (result.Use != null && result.Use.Root != null && state.Diagnostics.ShouldContinue())
        {
            string prefix = result.SourcePath + "/instance";
            BuildInstance(result.Use.Root, result, prefix, result.Active && result.Use.Root.Renderable, null, depth + 1, state);
        }

        int childIndex = 0;
        while (instance == null && childIndex < source.Children.Count && state.Diagnostics.ShouldContinue() && !state.LimitReached && !state.Cancelled)
        {
            SvgNode child = source.Children[childIndex];
            bool childActive = ChildActive(result, child);
            SvgConditionalResult? childConditional = Candidate(result.Switch, child);
            string childPath = sourcePath + "/" + childIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            BuildNode(child, result, childPath, instancePath, null, childActive, childContext, childViewport, childConditional, depth + 1, state);
            childIndex = childIndex + 1;
        }
        return FinishNode(result);
    }

    static void ResolvePayload(SvgResolvedNode result, SvgLengthContext lengthContext, SvgViewportResult? parentViewport, SvgResolvedBuildState state)
    {
        SvgNode? source = result.SourceNode;
        if ((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind == SvgElementKind.Svg)
        {
            SvgViewportResolveOptions options = new SvgViewportResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.ParentViewport = parentViewport;
            options.LengthContext = lengthContext;
            options.DefaultRootWidth = state.Options.ViewportWidth;
            options.DefaultRootHeight = state.Options.ViewportHeight;
            options.FragmentIdentifier = parentViewport == null ? state.Options.FragmentIdentifier : "";
            result.Viewport = new SvgViewportResolver().Resolve(source, options);
            result.Transform = result.Viewport.Transform;
            result.Bounds = result.Viewport.Bounds;
            result.Renderable = result.Active && result.Viewport.Success && result.Viewport.Renderable && StyleAllowsRendering(result.ComputedStyle);
            AppendDiagnostics(result, result.Viewport.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.Shape)
        {
            SvgShapeResolveOptions options = new SvgShapeResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.LengthContext = lengthContext;
            options.GeometryLimits = state.Options.Limits.Geometry;
            options.MaxTokenCharacters = state.Options.Limits.Tree.MaxNameCharacters;
            options.MaxCssPathCharacters = state.Options.Limits.Css.MaxCharacters;
            options.MaxCssPathTokens = state.Options.Limits.Css.MaxTokens;
            options.AncestorRendered = result.Active;
            result.Geometry = new SvgShapeResolver().ResolveWithNodeAndOptions(source, options);
            result.Bounds = ShapeBounds(result.Geometry);
            result.Renderable = result.Geometry.Renderable;
            AppendDiagnostics(result, result.Geometry.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.EmbeddedContent)
        {
            SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.InstancePath = result.InstancePath;
            options.LengthContext = lengthContext;
            options.IntrinsicSize = state.Options.ImageIntrinsicSize;
            options.ReferenceOptions = ReferenceOptions(state, true);
            options.ReferenceOptions.NestedDocumentOptions = null;
            options.ImageOptions = ImageOptions(state);
            options.ImageContentAvailable = state.Options.ImageContentAvailable;
            options.ForeignContentCallbackAvailable = state.Options.ForeignContentCallbackAvailable;
            options.HostContentCallback = state.Options.HostContentCallback;
            options.ConditionsSatisfied = (result.Conditional ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Matches;
            options.AncestorRendered = result.Active;
            options.MaxHostContentNodes = state.Options.Limits.Tree.MaxNodes;
            options.MaxHostRasterWidth = state.Options.Limits.Raster.MaxWidth;
            options.MaxHostRasterHeight = state.Options.Limits.Raster.MaxHeight;
            options.MaxHostRasterPixels = state.Options.Limits.Raster.MaxSurfacePixels;
            options.MaxHostRasterBytes = state.Options.Limits.Raster.MaxPixelBytes;
            options.MaxHostDiagnostics = state.Options.MaxDiagnostics;
            options.MaxHostCallbackMilliseconds = state.Options.Limits.Resources.MaxCallbackMilliseconds;
            options.CancellationToken = state.Options.CancellationToken;
            result.EmbeddedContent = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(source, options);
            result.Bounds = result.EmbeddedContent.Bounds;
            result.Renderable = result.EmbeddedContent.Renderable;
            AppendDiagnostics(result, result.EmbeddedContent.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.Use && result.Instance == null && result.Active)
        {
            SvgUseResolveOptions options = new SvgUseResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.LengthContext = lengthContext;
            options.ReferenceOptions = ReferenceOptions(state, true);
            options.Limits = state.Options.Limits.References;
            options.StyleSheets = state.ActiveStyleSheets;
            options.StyleOptions = (state.StyleOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            result.Use = new SvgUseResolver().ResolveWithNodeAndOptions(source, options);
            result.Transform = result.Use.Transform;
            result.Bounds = result.Use.Viewport == null ? null : result.Use.Viewport.Bounds;
            result.Renderable = result.Active && result.Use.Success && result.Use.Root != null && result.Use.Root.Renderable;
            AppendDiagnostics(result, result.Use.Diagnostics, result.SourcePath + "/instance", state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.PaintServer)
        {
            SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ReferenceOptions = ReferenceOptions(state, false);
            options.Limits = state.Options.Limits.References;
            options.LengthContext = lengthContext;
            options.StyleSheets = state.ActiveStyleSheets;
            options.StyleOptions = (state.StyleOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.MaxStops = state.Options.Limits.Tree.MaxNodes;
            result.PaintServer = new SvgPaintServerResolver().ResolveWithNodeAndOptions(source, options);
            result.Renderable = false;
            AppendDiagnostics(result, result.PaintServer.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.Effect)
        {
            SvgEffectResolveOptions options = new SvgEffectResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ReferenceOptions = ReferenceOptions(state, false);
            options.Limits = state.Options.Limits.References;
            options.LengthContext = lengthContext;
            options.StyleSheets = state.ActiveStyleSheets;
            options.StyleOptions = (state.StyleOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.MaxContentNodes = state.Options.Limits.Tree.MaxNodes;
            result.Effect = new SvgEffectResolver().ResolveWithNodeAndOptions(source, options);
            result.Renderable = false;
            AppendDiagnostics(result, result.Effect.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.Filter)
        {
            SvgFilterResolveOptions options = new SvgFilterResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ReferenceOptions = ReferenceOptions(state, false);
            options.ReferenceLimits = state.Options.Limits.References;
            options.FilterLimits = state.Options.Limits.Filters;
            options.LengthContext = lengthContext;
            options.StyleSheets = state.ActiveStyleSheets;
            options.StyleOptions = (state.StyleOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.MaxNameCharacters = state.Options.Limits.Tree.MaxNameCharacters;
            result.Filter = new SvgFilterResolver().ResolveWithNodeAndOptions(source, options);
            result.Renderable = false;
            AppendDiagnostics(result, result.Filter.Diagnostics, result.InstancePath, state);
            return;
        }
        if (IsStructural(source))
        {
            SvgStructuralResolveOptions options = new SvgStructuralResolveOptions();
            options.Attributes = (state.Attributes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            options.ComputedStyle = result.ComputedStyle;
            options.TreeLimits = state.Options.Limits.Tree;
            options.Referenced = result.Instance != null && result.Instance.IsInstanceRoot;
            options.AncestorRendered = result.Active;
            options.MaxRelationTokens = state.Options.Limits.Tree.MaxAttributesPerElement;
            options.MaxTokenCharacters = state.Options.Limits.Tree.MaxNameCharacters;
            result.Structural = new SvgStructuralResolver().ResolveWithNodeAndOptions(source, options);
            result.Renderable = result.Structural.Rendered;
            if (result.Kind == SvgResolvedNodeKind.Descriptive) result.Text = result.Structural.TextContent;
            AppendDiagnostics(result, result.Structural.Diagnostics, result.InstancePath, state);
            return;
        }
        if (result.Kind == SvgResolvedNodeKind.Text) result.Text = new SvgNodeApi().GetTextContent(source);
        result.Renderable = result.Active && (result.Kind == SvgResolvedNodeKind.Text || result.Kind == SvgResolvedNodeKind.Switch || result.Kind == SvgResolvedNodeKind.Container) && StyleAllowsRendering(result.ComputedStyle);
    }

    static SvgResolvedNode? BuildInstance(SvgUseInstance? instance, SvgResolvedNode parent, string prefix, bool parentActive, SvgLengthContext? inheritedContext, int depth, SvgResolvedBuildState state)
    {
        if (instance == null) return null;
        string path = prefix + "/" + instance.InstancePath;
        string sourcePath = PathForNode(instance.SourceNode);
        SvgLengthContext context = inheritedContext == null ? instance.LengthContext == null ? InitialContext(state.Options) : instance.LengthContext : inheritedContext;
        SvgResolvedNode? result = BuildNode(instance.SourceNode, parent, sourcePath, path, instance, parentActive, context, null, null, depth, state);
        if (result == null) return null;
        result.Transform = instance.Transform;
        result.Bounds = instance.Viewport == null ? result.Bounds : instance.Viewport.Bounds;
        result.Renderable = result.Renderable && instance.Renderable;
        int index = 0;
        while (index < instance.Children.Count && state.Diagnostics.ShouldContinue() && !state.LimitReached && !state.Cancelled)
        {
            bool childActive = ChildActive(result, (instance.Children[index].SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            BuildInstance(instance.Children[index], result, prefix, childActive && instance.Children[index].Renderable, result.LengthContext, depth + 1, state);
            index = index + 1;
        }
        if (instance.ShadowRoot != null && state.Diagnostics.ShouldContinue() && !state.LimitReached && !state.Cancelled) BuildInstance(instance.ShadowRoot, result, prefix, result.Active && instance.ShadowRoot.Renderable, result.LengthContext, depth + 1, state);
        return result;
    }

    static SvgResolvedNode FinishNode(SvgResolvedNode result)
    {
        result.Success = !HasError(result.Diagnostics);
        return result;
    }

    static bool CanBuild(SvgNode? source, int depth, SvgResolvedBuildState state)
    {
        if (source == null || state.LimitReached || state.Cancelled) return false;
        if (depth > state.Options.Limits.Tree.MaxDepth || state.NodeCount >= state.Options.Limits.Tree.MaxNodes)
        {
            state.LimitReached = true;
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "Resolved tree exceeded the configured depth or node limit.", source.SourceSpan);
            state.Diagnostics.Add(diagnostic);
            return false;
        }
        return CheckCancellation(source.SourceSpan, state);
    }

    static bool CheckCancellation(SvgSourceSpan? source, SvgResolvedBuildState state)
    {
        if (!state.Options.CancellationToken.IsCancellationRequested) return true;
        if (!state.Cancelled)
        {
            state.Cancelled = true;
            state.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.Cancelled, "Document resolution was cancelled.", source));
        }
        return false;
    }

    static bool ChildActive(SvgResolvedNode parent, SvgNode child)
    {
        if (!parent.Active || !parent.Renderable) return false;
        if (parent.Structural != null && !parent.Structural.RendersChildren) return false;
        if (parent.Switch == null) return true;
        if (child.Kind != SvgNodeKind.Element) return false;
        if (child.ElementKind == SvgElementKind.Style || child.ElementKind == SvgElementKind.Script) return true;
        return object.ReferenceEquals(parent.Switch.SelectedChild, child);
    }

    static SvgConditionalResult? Candidate(SvgSwitchResult? result, SvgNode? child)
    {
        if (result == null || child == null || child.Kind != SvgNodeKind.Element) return null;
        int index = 0;
        while (index < result.Candidates.Count)
        {
            if (object.ReferenceEquals(result.Candidates[index].Node, child)) return result.Candidates[index];
            index = index + 1;
        }
        return null;
    }

    static SvgLengthContext ChildContext(SvgResolvedNode node, SvgLengthContext fallback)
    {
        if (node.LengthContext != null) return node.LengthContext;
        return fallback;
    }

    static SvgRect? ShapeBounds(SvgShapeResult? shape)
    {
        if (shape == null || !shape.HasGeometry) return null;
        if (shape.Kind == SvgShapeKind.Rectangle) return new SvgRect(shape.X, shape.Y, shape.Width, shape.Height);
        if (shape.Kind == SvgShapeKind.Circle) return new SvgRect(shape.Cx - shape.R, shape.Cy - shape.R, shape.R * 2, shape.R * 2);
        if (shape.Kind == SvgShapeKind.Ellipse) return new SvgRect(shape.Cx - shape.Rx, shape.Cy - shape.Ry, shape.Rx * 2, shape.Ry * 2);
        if (shape.Kind == SvgShapeKind.Line)
        {
            double x = shape.X1 < shape.X2 ? shape.X1 : shape.X2;
            double y = shape.Y1 < shape.Y2 ? shape.Y1 : shape.Y2;
            double width = System.Math.Abs(shape.X2 - shape.X1);
            double height = System.Math.Abs(shape.Y2 - shape.Y1);
            return new SvgRect(x, y, width, height);
        }
        SvgDrawingBoundsOptions options = new SvgDrawingBoundsOptions();
        options.IncludeFill = true;
        SvgDrawingBoundsResult bounds = new SvgDrawingBounds().Shape(shape, options);
        if (bounds.Success && bounds.FillBounds != null && !bounds.FillBounds.IsEmpty) return bounds.FillBounds;
        return null;
    }

    static bool StyleAllowsRendering(SvgComputedStyle? style)
    {
        if (style == null) return true;
        string display = style.GetKeyword(SvgCssPropertyNames.Display);
        string visibility = style.GetKeyword(SvgCssPropertyNames.Visibility);
        return !string.Equals(display, "none", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(visibility, "hidden", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(visibility, "collapse", System.StringComparison.OrdinalIgnoreCase);
    }

    static SvgResolvedNodeKind Kind(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return SvgResolvedNodeKind.Unknown;
        if (node.NamespaceUri != SvgNamespaces.Svg || node.ElementKind == SvgElementKind.Unknown) return SvgResolvedNodeKind.Unknown;
        if (node.ElementKind == SvgElementKind.Svg) return SvgResolvedNodeKind.Viewport;
        if (node.ElementKind == SvgElementKind.Switch) return SvgResolvedNodeKind.Switch;
        if (node.ElementKind == SvgElementKind.Use) return SvgResolvedNodeKind.Use;
        if (node.ElementKind == SvgElementKind.A) return SvgResolvedNodeKind.Link;
        if (node.ElementKind == SvgElementKind.Image || node.ElementKind == SvgElementKind.ForeignObject || node.ElementKind == SvgElementKind.Audio || node.ElementKind == SvgElementKind.Video || node.ElementKind == SvgElementKind.Iframe || node.ElementKind == SvgElementKind.Canvas) return SvgResolvedNodeKind.EmbeddedContent;
        if (node.ElementKind == SvgElementKind.LinearGradient || node.ElementKind == SvgElementKind.RadialGradient || node.ElementKind == SvgElementKind.Pattern) return SvgResolvedNodeKind.PaintServer;
        if (node.ElementKind == SvgElementKind.Marker || node.ElementKind == SvgElementKind.ClipPath || node.ElementKind == SvgElementKind.Mask) return SvgResolvedNodeKind.Effect;
        if (node.ElementKind == SvgElementKind.Filter) return SvgResolvedNodeKind.Filter;
        if (node.ElementKind == SvgElementKind.Circle || node.ElementKind == SvgElementKind.Ellipse || node.ElementKind == SvgElementKind.Line || node.ElementKind == SvgElementKind.Path || node.ElementKind == SvgElementKind.Polygon || node.ElementKind == SvgElementKind.Polyline || node.ElementKind == SvgElementKind.Rect) return SvgResolvedNodeKind.Shape;
        if (node.ElementKind == SvgElementKind.Text || node.ElementKind == SvgElementKind.TextPath || node.ElementKind == SvgElementKind.TSpan) return SvgResolvedNodeKind.Text;
        if (node.ElementKind == SvgElementKind.Title || node.ElementKind == SvgElementKind.Desc || node.ElementKind == SvgElementKind.Metadata) return SvgResolvedNodeKind.Descriptive;
        if (node.ElementKind == SvgElementKind.G || node.ElementKind == SvgElementKind.Defs || node.ElementKind == SvgElementKind.Symbol) return SvgResolvedNodeKind.Container;
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgElementDefinition? definition = catalog.Find(node.ElementKind);
        if (definition != null && (definition.RenderMode == SvgElementRenderMode.Resource || definition.RenderMode == SvgElementRenderMode.Never || definition.RenderMode == SvgElementRenderMode.Referenced)) return SvgResolvedNodeKind.Resource;
        return SvgResolvedNodeKind.Other;
    }

    static bool IsStructural(SvgNode? node)
    {
        if (node == null) return false;
        return node.ElementKind == SvgElementKind.G || node.ElementKind == SvgElementKind.Defs || node.ElementKind == SvgElementKind.Symbol || node.ElementKind == SvgElementKind.View || node.ElementKind == SvgElementKind.A || node.ElementKind == SvgElementKind.Metadata || node.ElementKind == SvgElementKind.Title || node.ElementKind == SvgElementKind.Desc;
    }

    static SvgConditionalProcessingOptions CreateConditionalOptions(SvgResolveOptions options)
    {
        SvgConditionalProcessingOptions result = new SvgConditionalProcessingOptions();
        result.SupportedFeatures = options.Capabilities;
        result.SupportedExtensions = options.Extensions;
        result.LanguagePreferences = options.LanguagePreferences;
        result.Limits = options.Limits.References;
        result.MaxListItems = options.Limits.Tree.MaxNodes;
        result.MaxTokenCharacters = options.Limits.Tree.MaxNameCharacters;
        return result;
    }

    static SvgTypedAttributeAccessor CreateAttributes(SvgResolveOptions options)
    {
        SvgTypedAttributeOptions values = new SvgTypedAttributeOptions();
        values.GeometryLimits = options.Limits.Geometry;
        values.MaxTokenCharacters = options.Limits.Tree.MaxNameCharacters;
        values.MaxListItems = options.Limits.Tree.MaxNodes;
        values.MaxTransforms = options.Limits.Geometry.MaxPathSegments;
        return new SvgTypedAttributeAccessor().Create(values);
    }

    static SvgStyleResolverOptions CopyStyleOptions(SvgResolveOptions options)
    {
        SvgStyleResolverOptions result = new SvgStyleResolverOptions();
        result.Properties = options.StyleOptions.Properties;
        result.Limits = options.Limits.Css;
        result.SelectorOptions = options.StyleOptions.SelectorOptions;
        result.StyleEnvironment = CopyEnvironment(options.StyleOptions.StyleEnvironment, options.ViewportWidth, options.ViewportHeight);
        result.IncludeUserAgentDefaults = options.StyleOptions.IncludeUserAgentDefaults;
        result.MaxCacheEntries = options.StyleOptions.MaxCacheEntries;
        result.MaxPaintFallbacks = options.StyleOptions.MaxPaintFallbacks;
        result.FontVersion = options.StyleOptions.FontVersion;
        return result;
    }

    static SvgStyleEnvironment CopyEnvironment(SvgStyleEnvironment source, double width, double height)
    {
        SvgStyleEnvironment result = new SvgStyleEnvironment();
        result.Version = source.Version;
        result.MediaType = source.MediaType;
        result.ViewportWidth = width;
        result.ViewportHeight = height;
        result.ResolutionDpi = source.ResolutionDpi;
        result.ColorBits = source.ColorBits;
        result.MonochromeBits = source.MonochromeBits;
        result.ReducedMotion = source.ReducedMotion;
        result.RootFontSize = source.RootFontSize;
        return result;
    }

    static SvgLengthContext InitialContext(SvgResolveOptions options)
    {
        SvgLengthContext result = new SvgLengthContext();
        result.HasViewport = true;
        result.ViewportWidth = options.ViewportWidth;
        result.ViewportHeight = options.ViewportHeight;
        result.HasFontSize = true;
        result.FontSize = options.StyleOptions.StyleEnvironment.RootFontSize;
        result.HasXHeight = true;
        result.XHeight = result.FontSize * 0.5;
        result.HasZeroAdvance = true;
        result.ZeroAdvance = result.FontSize * 0.5;
        result.HasRootFontSize = true;
        result.RootFontSize = options.StyleOptions.StyleEnvironment.RootFontSize;
        return result;
    }

    static SvgReferenceResolveOptions ReferenceOptions(SvgResolvedBuildState state, bool required)
    {
        SvgResolveOptions options = state.Options;
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.RequireReference = required;
        result.ExternalDocument = options.ExternalDocument;
        result.MaxUriCharacters = options.Limits.Resources.MaxUriCharacters;
        result.MaxBaseDepth = options.Limits.Tree.MaxDepth;
        if (options.ResourceProvider != null)
        {
            SvgNestedDocumentOptions nested = new SvgNestedDocumentOptions();
            nested.LoadOptions = CollectionOptions((state.Document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), options);
            nested.Context = (state.NestedResources ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            result.NestedDocumentOptions = nested;
        }
        return result;
    }

    static SvgImageResolveOptions ImageOptions(SvgResolvedBuildState state)
    {
        SvgImageResolveOptions result = new SvgImageResolveOptions();
        result.LoadOptions = CollectionOptions((state.Document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), state.Options);
        result.Context = (state.NestedResources ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return result;
    }

    static SvgLoadOptions CollectionOptions(SvgDocument document, SvgResolveOptions options)
    {
        SvgLoadOptions result = new SvgLoadOptions();
        result.Mode = document.ProcessingMode;
        result.BaseUri = document.BaseUri;
        result.ContentType = document.ContentType;
        result.Limits = options.Limits;
        result.ResourcePolicy = options.ResourcePolicy;
        result.ResourceProvider = options.ResourceProvider;
        result.CancellationToken = options.CancellationToken;
        result.DiagnosticBehavior = options.DiagnosticBehavior;
        return result;
    }

    static System.Collections.Generic.List<SvgStyleSheet> ActiveSheets(SvgStyleSheetCollection collection, SvgStyleEnvironment environment, SvgCssLimits limits)
    {
        System.Collections.Generic.List<SvgStyleSheet> result = new System.Collections.Generic.List<SvgStyleSheet>();
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry != null && entry.MatchesMedia(environment, limits)) result.Add(entry.StyleSheet);
            index = index + 1;
        }
        return result;
    }

    static void CopyResources(SvgResourceSummary? source, SvgResourceSummary? destination)
    {
        if (source == null || destination == null) return;
        destination.RequestCount = source.RequestCount;
        destination.LoadedCount = source.LoadedCount;
        destination.EncodedBytes = source.EncodedBytes;
    }

    static void AppendDiagnostics(SvgResolvedNode? node, System.Collections.Generic.List<SvgDiagnostic>? diagnostics, string prefix, SvgResolvedBuildState state)
    {
        if (diagnostics == null) return;
        int index = 0;
        while (index < diagnostics.Count)
        {
            SvgDiagnostic source = diagnostics[index];
            if (source != null)
            {
                SvgDiagnostic copy = CopyDiagnostic(source, prefix);
                if (node != null) node.Diagnostics.Add(copy);
                state.Diagnostics.Add(copy);
                if (!state.Diagnostics.ShouldContinue()) return;
            }
            index = index + 1;
        }
    }

    static SvgDiagnostic CopyDiagnostic(SvgDiagnostic source, string prefix)
    {
        SvgDiagnostic result = new SvgDiagnostic(source.Severity, source.Code, source.Message, source.Source);
        result.ResourcePath = source.ResourcePath;
        if (prefix == null || prefix.Length == 0) result.InstancePath = source.InstancePath;
        else if (source.InstancePath == null || source.InstancePath.Length == 0) result.InstancePath = prefix;
        else result.InstancePath = prefix + "/" + source.InstancePath;
        int index = 0;
        while (index < source.RelatedSpans.Count)
        {
            result.RelatedSpans.Add(source.RelatedSpans[index]);
            index = index + 1;
        }
        return result;
    }

    static bool HasError(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }

    static string PathForNode(SvgNode? node)
    {
        if (node == null) return "";
        System.Collections.Generic.List<int> parts = new System.Collections.Generic.List<int>();
        SvgNode current = node;
        while (current.Parent != null && current.Parent.Kind != SvgNodeKind.Document)
        {
            parts.Add(IndexOf(current.Parent, current));
            current = current.Parent;
        }
        string result = "0";
        int index = parts.Count - 1;
        while (index >= 0)
        {
            result = result + "/" + parts[index].ToString(System.Globalization.CultureInfo.InvariantCulture);
            index = index - 1;
        }
        return result;
    }

    static int IndexOf(SvgNode parent, SvgNode child)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }

    static bool ValidOptions(SvgResolveOptions options)
    {
        if (options.FragmentIdentifier == null || options.LanguagePreferences == null || options.Capabilities == null || options.Extensions == null || options.CallerStyleSheets == null) return false;
        if (options.StyleOptions == null || options.StyleOptions.Properties == null || options.StyleOptions.SelectorOptions == null || options.StyleOptions.SelectorOptions.Namespaces == null || options.StyleOptions.SelectorOptions.DynamicState == null || options.StyleOptions.StyleEnvironment == null) return false;
        if (options.StyleOptions.MaxCacheEntries < 1 || options.StyleOptions.MaxPaintFallbacks < 0 || options.StyleOptions.FontVersion < 0 || options.StyleOptions.SelectorOptions.MaxWork < 1) return false;
        if (options.ResourcePolicy == null || options.Limits == null || !options.Limits.Validate().Success) return false;
        if (!SvgFiniteMath.IsFinite(options.ViewportWidth) || !SvgFiniteMath.IsFinite(options.ViewportHeight) || options.ViewportWidth < 0 || options.ViewportHeight < 0) return false;
        if (!SvgFiniteMath.IsFinite(options.AnimationTime) || options.MaxDiagnostics < 0 || options.MaxCacheEntries < 1) return false;
        if (options.DiagnosticBehavior != SvgDiagnosticBehavior.Strict && options.DiagnosticBehavior != SvgDiagnosticBehavior.Compatible) return false;
        int index = 0;
        while (index < options.CallerStyleSheets.Count)
        {
            if (options.CallerStyleSheets[index] == null) return false;
            index = index + 1;
        }
        index = 0;
        while (index < options.LanguagePreferences.Count)
        {
            SvgTextTokenListParseResult language = new SvgLanguageListParser().Parse(options.LanguagePreferences[index], 1, options.Limits.Tree.MaxNameCharacters);
            if (!language.Success || language.Items.Count != 1) return false;
            index = index + 1;
        }
        return true;
    }

    int Find(SvgDocument document, SvgResolveOptions options)
    {
        int index = cache.Count - 1;
        while (index >= 0)
        {
            SvgResolvedDocument result = cache[index].Result;
            if (!result.IsCurrent())
            {
                cache.RemoveAt(index);
                invalidatedEntries = invalidatedEntries + 1;
            }
            else if ((result.CacheKey ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Matches(document, options)) return index;
            index = index - 1;
        }
        return -1;
    }

    void Add(SvgResolvedDocument result, int maximum)
    {
        while (cache.Count >= maximum)
        {
            int oldest = 0;
            int index = 1;
            while (index < cache.Count)
            {
                if (cache[index].AccessOrder < cache[oldest].AccessOrder) oldest = index;
                index = index + 1;
            }
            cache.RemoveAt(oldest);
            evictedEntries = evictedEntries + 1;
        }
        cache.Add(new SvgResolvedCacheEntry(result, NextAccess()));
    }

    long NextAccess()
    {
        if (accessClock == System.Int64.MaxValue) accessClock = 0;
        accessClock = accessClock + 1;
        return accessClock;
    }
}
