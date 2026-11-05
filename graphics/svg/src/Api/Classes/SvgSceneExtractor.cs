namespace svg;

/// <summary>Extracts fragment, source, and use-instance scenes without mutating the full scene.</summary>
public class SvgSceneExtractor : ISvgSceneExtractor
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSceneExtractor()
    {
    }

    /// <summary>Extracts the scene node identified by a fragment ID using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractFragmentWithSceneAndDocumentAndFragment(SvgScene scene, SvgResolvedDocument document, string fragment)
    {
        return new SvgSceneExtractor().ExtractFragmentWithSceneAndDocumentAndFragmentAndOptions(scene, document, fragment, new SvgSceneExtractionOptions());
    }

    /// <summary>Extracts a fragment under explicit node, resource, and command bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractFragmentWithSceneAndDocumentAndFragmentAndOptions(SvgScene scene, SvgResolvedDocument document, string? fragment, SvgSceneExtractionOptions options)
    {
        if (!new SvgSceneExtractor().Valid(scene, document, options) || fragment == null) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.InvalidArgument);
        string identifier = new SvgUri().DecodeFragment(fragment);
        if (identifier.Length == 0) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.InvalidArgument);
        SvgNode? source = (document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Index.GetById(identifier);
        if (source == null) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.TargetNotFound);
        SvgSceneNode? target = scene.FindNode(source, "");
        if (target == null) target = new SvgSceneExtractor().FirstSource(scene, source);
        return new SvgSceneExtractor().Extract(scene, document, target, options);
    }

    /// <summary>Extracts one authored source node using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractSourceWithSceneAndDocumentAndSource(SvgScene scene, SvgResolvedDocument document, SvgNode source)
    {
        return new SvgSceneExtractor().ExtractSourceWithSceneAndDocumentAndSourceAndOptions(scene, document, source, new SvgSceneExtractionOptions());
    }

    /// <summary>Extracts one authored source node under explicit bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractSourceWithSceneAndDocumentAndSourceAndOptions(SvgScene scene, SvgResolvedDocument document, SvgNode? source, SvgSceneExtractionOptions options)
    {
        if (!new SvgSceneExtractor().Valid(scene, document, options) || source == null) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.InvalidArgument);
        return new SvgSceneExtractor().Extract(scene, document, scene.FindNode(source, ""), options);
    }

    /// <summary>Extracts one exact use-instance path using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractInstanceWithSceneAndDocumentAndInstancePath(SvgScene scene, SvgResolvedDocument document, string instancePath)
    {
        return new SvgSceneExtractor().ExtractInstanceWithSceneAndDocumentAndInstancePathAndOptions(scene, document, instancePath, new SvgSceneExtractionOptions());
    }

    /// <summary>Extracts one exact use-instance path under explicit bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    public SvgSceneExtractionResult ExtractInstanceWithSceneAndDocumentAndInstancePathAndOptions(SvgScene scene, SvgResolvedDocument document, string? instancePath, SvgSceneExtractionOptions options)
    {
        if (!new SvgSceneExtractor().Valid(scene, document, options) || instancePath == null || instancePath.Length == 0) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.InvalidArgument);
        return new SvgSceneExtractor().Extract(scene, document, new SvgSceneExtractor().FindInstance(scene, instancePath), options);
    }
SvgSceneExtractionResult Extract(SvgScene? source, SvgResolvedDocument document, SvgSceneNode? target, SvgSceneExtractionOptions options)
    {
        if (target == null) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.TargetNotFound);
        SvgSceneExtractionState state = new SvgSceneExtractionState((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), document, options);
        System.Collections.Generic.List<SvgSceneNode> chain = new SvgSceneExtractor().Ancestors(target);
        SvgSceneNode? root = null;
        SvgSceneNode? parent = null;
        SvgSceneNode? targetClone = null;
        int index = chain.Count - 1;
        while (index >= 0 && state.Error == SvgSceneExtractionError.None)
        {
            bool deep = index == 0;
            SvgSceneNode? clone = new SvgSceneExtractor().CloneNode(chain[index], deep, state);
            if (clone == null) break;
            if (root == null) root = clone;
            if (parent != null && !parent.AddChild(clone))
            {
                state.Error = SvgSceneExtractionError.ProcessingFailed;
                break;
            }
            parent = clone;
            if (deep) targetClone = clone;
            index = index - 1;
        }
        if (state.Error != SvgSceneExtractionError.None) return new SvgSceneExtractor().Failure(state.Error);
        if (root == null || root.Kind != SvgSceneNodeKind.Group || targetClone == null) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.ProcessingFailed);
        SvgScene extracted = SvgScene.Create(source.SourceDocument);
        if (!extracted.SetRoot(root)) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.ProcessingFailed);
        if (!new SvgSceneExtractor().RetainResources(source, extracted, options.MaximumResources)) return new SvgSceneExtractor().Failure(SvgSceneExtractionError.LimitExceeded);
        SvgSceneBoundsOptions boundsOptions = new SvgSceneBoundsOptions();
        boundsOptions.MaximumNodes = options.MaximumNodes;
        SvgSceneBoundsResult bounds = new SvgSceneBounds().ComputeWithSceneAndDocumentAndOptions(extracted, document, boundsOptions);
        if (!bounds.Success) return new SvgSceneExtractor().Failure(bounds.Error == SvgDrawingBoundsError.LimitExceeded ? SvgSceneExtractionError.LimitExceeded : SvgSceneExtractionError.ProcessingFailed);
        SvgSceneLayerOptions layerOptions = new SvgSceneLayerOptions();
        layerOptions.MaximumLayers = options.MaximumNodes;
        SvgSceneLayerPlan layers = new SvgSceneLayerPlanner().PlanWithSceneAndDocumentAndOptions(extracted, document, layerOptions);
        if (!layers.Success) return new SvgSceneExtractor().Failure(layers.Error == SvgDrawingBoundsError.LimitExceeded ? SvgSceneExtractionError.LimitExceeded : SvgSceneExtractionError.ProcessingFailed);
        SvgRenderCommandOptions commandOptions = new SvgRenderCommandOptions();
        commandOptions.MaximumCommands = options.MaximumCommands;
        SvgRenderCommandList commands = new SvgRenderCommandEnumerator().EnumerateWithSceneAndOptions(extracted, commandOptions);
        if (!commands.Success) return new SvgSceneExtractor().Failure(commands.Error == SvgRenderCommandError.LimitExceeded ? SvgSceneExtractionError.LimitExceeded : SvgSceneExtractionError.ProcessingFailed);
        extracted.Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>((source.Diagnostics ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        extracted.Success = true;
        SvgSceneExtractionResult result = new SvgSceneExtractionResult();
        result.Scene = extracted;
        result.Target = targetClone;
        result.SourceTarget = target;
        result.AncestorCount = chain.Count - 1;
        result.Success = true;
        return result;
    }
System.Collections.Generic.List<SvgSceneNode> Ancestors(SvgSceneNode target)
    {
        System.Collections.Generic.List<SvgSceneNode> result = new System.Collections.Generic.List<SvgSceneNode>();
        SvgSceneNode? current = target;
        while (current != null)
        {
            result.Add(current);
            current = current.Parent;
        }
        return result;
    }
SvgSceneNode? CloneNode(SvgSceneNode source, bool deep, SvgSceneExtractionState state)
    {
        if (state.NodeCount >= state.Options.MaximumNodes)
        {
            state.Error = SvgSceneExtractionError.LimitExceeded;
            return null;
        }
        state.NodeCount = state.NodeCount + 1;
        SvgSceneNode result = new SvgSceneNode(source.Kind);
        new SvgSceneExtractor().CopyCommon(source, result);
        new SvgSceneExtractor().CopyPayload(source, result);
        if (deep && source.Kind == SvgSceneNodeKind.Group)
        {
            int index = 0;
            while (index < source.Children.Count && state.Error == SvgSceneExtractionError.None)
            {
                SvgSceneNode? child = new SvgSceneExtractor().CloneNode(source.Children[index], true, state);
                if (child == null || !result.AddChild(child))
                {
                    if (state.Error == SvgSceneExtractionError.None) state.Error = SvgSceneExtractionError.ProcessingFailed;
                    return null;
                }
                index = index + 1;
            }
        }
        return result;
    }
void CopyCommon(SvgSceneNode source, SvgSceneNode target)
    {
        target.SourceNode = source.SourceNode;
        target.SourceVersion = source.SourceVersion;
        target.SourcePath = source.SourcePath;
        target.InstancePath = source.InstancePath;
        target.Transform = (new SvgSceneExtractor().Matrix(source.Transform) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.AbsoluteTransform = (new SvgSceneExtractor().Matrix(source.AbsoluteTransform) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.Opacity = source.Opacity;
        target.FillOpacity = source.FillOpacity;
        target.StrokeOpacity = source.StrokeOpacity;
        target.Displayed = source.Displayed;
        target.Visible = source.Visible;
        target.PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>(source.PaintOrder);
        target.BlendMode = source.BlendMode;
        target.ColorInterpolation = source.ColorInterpolation;
        target.Isolated = source.Isolated;
        target.OverflowClip = new SvgSceneExtractor().Rect(source.OverflowClip);
        target.HasObjectBoundingBox = source.HasObjectBoundingBox;
        target.ObjectBoundingBox = new SvgSceneExtractor().Rect(source.ObjectBoundingBox);
        target.ObjectBoundingBoxTransform = (new SvgSceneExtractor().Matrix(source.ObjectBoundingBoxTransform) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.LocalBounds = new SvgSceneExtractor().Rect(source.LocalBounds);
        target.FillBounds = (new SvgSceneExtractor().Rect(source.FillBounds) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.StrokeBounds = (new SvgSceneExtractor().Rect(source.StrokeBounds) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.MarkerBounds = (new SvgSceneExtractor().Rect(source.MarkerBounds) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.FilterBounds = (new SvgSceneExtractor().Rect(source.FilterBounds) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        target.WorldBounds = new SvgSceneExtractor().Rect(source.WorldBounds);
        target.Fill = source.Fill;
        target.Stroke = source.Stroke;
        target.FillUsesContext = source.FillUsesContext;
        target.StrokeUsesContext = source.StrokeUsesContext;
        target.MarkerStart = source.MarkerStart;
        target.MarkerMid = source.MarkerMid;
        target.MarkerEnd = source.MarkerEnd;
        target.Clip = source.Clip;
        target.Mask = source.Mask;
        target.Filter = source.Filter;
    }
void CopyPayload(SvgSceneNode source, SvgSceneNode target)
    {
        if (source.Group != null)
        {
            (target.Group ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).EstablishesViewport = source.Group.EstablishesViewport;
            target.Group.ViewportBounds = new SvgSceneExtractor().Rect(source.Group.ViewportBounds);
            target.Group.OverflowClip = new SvgSceneExtractor().Rect(source.Group.OverflowClip);
        }
        if (source.Path != null)
        {
            (target.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FillRule = source.Path.FillRule;
            target.Path.Path = new SvgSceneExtractor().ClonePath(source.Path.Path);
            target.Path.StrokeStyle = (new SvgSceneExtractor().CopyStrokeStyle(source.Path.StrokeStyle) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            target.Path.VectorEffect = source.Path.VectorEffect;
            target.Path.ShapeRendering = source.Path.ShapeRendering;
        }
        if (source.Text != null)
        {
            (target.Text ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Text = source.Text.Text;
            target.Text.X = source.Text.X;
            target.Text.Y = source.Text.Y;
            target.Text.FontSize = source.Text.FontSize;
        }
        if (source.Image != null)
        {
            (target.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource = source.Image.Resource;
            target.Image.Bounds = new SvgSceneExtractor().Rect(source.Image.Bounds);
            target.Image.ContentTransform = (new SvgSceneExtractor().Matrix(source.Image.ContentTransform) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            target.Image.AspectRatio = source.Image.AspectRatio;
            target.Image.Rendering = source.Image.Rendering;
            target.Image.HostContent = source.Image.HostContent;
        }
        if (source.HostContent != null)
        {
            SvgSceneHostContentData host = (target.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            host.Kind = source.HostContent.Kind;
            host.Bounds = new SvgSceneExtractor().Rect(source.HostContent.Bounds);
            host.ContentBounds = new SvgSceneExtractor().Rect(source.HostContent.ContentBounds);
            host.ContentTransform = (new SvgSceneExtractor().Matrix(source.HostContent.ContentTransform) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            host.RasterPixels = source.HostContent.RasterPixels;
            host.Record = source.HostContent.Record;
        }
    }
SvgPathData? ClonePath(SvgPathData? source)
    {
        if (source == null) return null;
        System.Collections.Generic.List<SvgPathCommand> commands = new System.Collections.Generic.List<SvgPathCommand>();
        int index = 0;
        while (index < source.Commands.Count)
        {
            SvgPathCommand command = source.Commands[index];
            commands.Add(new SvgPathCommand(command.Kind, command.Relative, command.Values, command.SourceOffset, command.EndOffset, command.Explicit, command.StartX, command.StartY, command.EndX, command.EndY, command.SubpathStartX, command.SubpathStartY, command.SubpathIndex));
            index = index + 1;
        }
        System.Collections.Generic.List<SvgPathParseIssue> issues = new System.Collections.Generic.List<SvgPathParseIssue>();
        index = 0;
        while (index < source.Issues.Count)
        {
            SvgPathParseIssue issue = source.Issues[index];
            issues.Add(new SvgPathParseIssue(issue.Error, issue.ErrorOffset, issue.CommandOffset));
            index = index + 1;
        }
        return new SvgPathData(commands, source.Source, issues);
    }
SvgStrokeStyle? CopyStrokeStyle(SvgStrokeStyle? source)
    {
        if (source == null) return null;
        SvgStrokeStyle result = new SvgStrokeStyle();
        result.Width = source.Width;
        result.LineCap = source.LineCap;
        result.LineJoin = source.LineJoin;
        result.MiterLimit = source.MiterLimit;
        result.DashArray = source.DashArray == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : new System.Collections.Generic.List<double>(source.DashArray);
        result.DashOffset = source.DashOffset;
        result.Tolerance = source.Tolerance;
        result.MaximumSubdivisionDepth = source.MaximumSubdivisionDepth;
        result.MaximumFlattenedSegments = source.MaximumFlattenedSegments;
        result.MaximumDashFragments = source.MaximumDashFragments;
        result.MaximumOutputCommands = source.MaximumOutputCommands;
        return result;
    }
bool RetainResources(SvgScene source, SvgScene target, int maximum)
    {
        System.Collections.Generic.List<SvgSceneResource> needed = new System.Collections.Generic.List<SvgSceneResource>();
        int index = 0;
        while (index < (target.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            new SvgSceneExtractor().CollectNodeResources(target.Nodes[index], needed);
            index = index + 1;
        }
        index = 0;
        while (index < (source.Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgSceneResource resource = source.Resources[index];
            if (new SvgSceneExtractor().Contains(needed, resource))
            {
                if ((target.Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count >= maximum || !target.AddResource(resource)) return false;
            }
            index = index + 1;
        }
        return needed.Count == (target.Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count;
    }
void CollectNodeResources(SvgSceneNode? node, System.Collections.Generic.List<SvgSceneResource> needed)
    {
        if (node == null) return;
        new SvgSceneExtractor().AddResource(node.Fill, needed);
        new SvgSceneExtractor().AddResource(node.Stroke, needed);
        new SvgSceneExtractor().AddResource(node.MarkerStart, needed);
        new SvgSceneExtractor().AddResource(node.MarkerMid, needed);
        new SvgSceneExtractor().AddResource(node.MarkerEnd, needed);
        new SvgSceneExtractor().AddResource(node.Clip, needed);
        new SvgSceneExtractor().AddResource(node.Mask, needed);
        new SvgSceneExtractor().AddResource(node.Filter, needed);
        if (node.Image != null) new SvgSceneExtractor().AddResource(node.Image.Resource, needed);
    }
void AddResource(SvgSceneResource? resource, System.Collections.Generic.List<SvgSceneResource> needed)
    {
        if (resource == null || new SvgSceneExtractor().Contains(needed, resource)) return;
        needed.Add(resource);
        if (resource.PatternPaint != null) new SvgSceneExtractor().CollectContent(resource.PatternPaint.Content, needed);
        if (resource.Marker != null) new SvgSceneExtractor().CollectContent(resource.Marker.Content, needed);
        if (resource.Clip != null) new SvgSceneExtractor().CollectContent(resource.Clip.Content, needed);
        if (resource.Mask != null) new SvgSceneExtractor().CollectContent(resource.Mask.Content, needed);
        if (resource.Filter != null)
        {
            int index = 0;
            while (index < resource.Filter.Primitives.Count)
            {
                new SvgSceneExtractor().AddResource(resource.Filter.Primitives[index].ImageResource, needed);
                index = index + 1;
            }
        }
    }
void CollectContent(SvgSceneNode? node, System.Collections.Generic.List<SvgSceneResource> needed)
    {
        if (node == null) return;
        new SvgSceneExtractor().CollectNodeResources(node, needed);
        int index = 0;
        while (index < node.Children.Count)
        {
            new SvgSceneExtractor().CollectContent(node.Children[index], needed);
            index = index + 1;
        }
    }
bool Contains(System.Collections.Generic.List<SvgSceneResource> resources, SvgSceneResource target)
    {
        int index = 0;
        while (index < resources.Count)
        {
            if (object.ReferenceEquals(resources[index], target)) return true;
            index = index + 1;
        }
        return false;
    }
SvgSceneNode? FirstSource(SvgScene scene, SvgNode source)
    {
        int index = 0;
        while (index < (scene.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (object.ReferenceEquals(scene.Nodes[index].SourceNode, source)) return scene.Nodes[index];
            index = index + 1;
        }
        return null;
    }
SvgSceneNode? FindInstance(SvgScene? scene, string instancePath)
    {
        int index = 0;
        while (index < ((scene ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if ((scene.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index].InstancePath == instancePath) return scene.Nodes[index];
            index = index + 1;
        }
        return null;
    }
SvgMatrix? Matrix(SvgMatrix? source)
    {
        return source == null ? null : new SvgMatrix(source.A, source.B, source.C, source.D, source.E, source.F);
    }
SvgRect? Rect(SvgRect? source)
    {
        return source == null ? null : SvgRect.Create(source.X, source.Y, source.Width, source.Height, source.IsEmpty);
    }
bool Valid(SvgScene? scene, SvgResolvedDocument? document, SvgSceneExtractionOptions? options)
    {
        if (scene == null || document == null || options == null || options.MaximumNodes < 1 || options.MaximumResources < 0 || options.MaximumCommands < 1) return false;
        if (!scene.Success || !scene.IsCurrent() || scene.Root == null || scene.LayerPlan == null || scene.RenderCommands == null) return false;
        return document.IsCurrent() && object.ReferenceEquals(scene.SourceDocument, document.SourceDocument) && scene.SourceVersion == document.SourceVersion;
    }
SvgSceneExtractionResult Failure(SvgSceneExtractionError error)
    {
        SvgSceneExtractionResult result = new SvgSceneExtractionResult();
        result.Error = error;
        return result;
    }
}
