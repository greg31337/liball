namespace svg;

/// <summary>Precomputes absolute scene transforms and component, effect, and aggregate bounds.</summary>
public class SvgSceneBounds : ISvgSceneBounds
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSceneBounds()
    {
    }

    /// <summary>Precomputes a scene using deterministic bounded defaults.</summary>
    /// <param name="scene">The normalized scene to update.</param>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <returns>The completed bounds outcome.</returns>
    public SvgSceneBoundsResult ComputeWithSceneAndDocument(SvgScene scene, SvgResolvedDocument document)
    {
        return new SvgSceneBounds().ComputeWithSceneAndDocumentAndOptions(scene, document, new SvgSceneBoundsOptions());
    }

    /// <summary>Precomputes a scene using explicit component and traversal options.</summary>
    /// <param name="scene">The normalized scene to update.</param>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <param name="options">The component and traversal options.</param>
    /// <returns>The completed or failed bounds outcome.</returns>
    public SvgSceneBoundsResult ComputeWithSceneAndDocumentAndOptions(SvgScene scene, SvgResolvedDocument document, SvgSceneBoundsOptions options)
    {
        if (!new SvgSceneBounds().Valid(scene, document, options)) return new SvgSceneBounds().Failure(SvgDrawingBoundsError.InvalidArgument, 0);
        if ((scene.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count > options.MaximumNodes) return new SvgSceneBounds().Failure(SvgDrawingBoundsError.LimitExceeded, 0);
        SvgSceneBoundsState state = new SvgSceneBoundsState(scene, document, options);
        int index = 0;
        while (index < scene.Nodes.Count)
        {
            SvgSceneNode node = scene.Nodes[index];
            SvgResolvedNode? resolved = new SvgSceneBounds().Resolved(node, document);
            if (resolved == null) return new SvgSceneBounds().Failure(SvgDrawingBoundsError.InvalidArgument, index);
            SvgMatrixOperationResult world = new SvgDrawingBounds().World(resolved);
            if (!world.Success) return new SvgSceneBounds().Failure(new SvgSceneBounds().Map(world.Error), index);
            node.AbsoluteTransform = (world.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            index = index + 1;
        }
        index = scene.Nodes.Count - 1;
        while (index >= 0)
        {
            SvgSceneNode node = scene.Nodes[index];
            SvgResolvedNode? resolved = new SvgSceneBounds().Resolved(node, document);
            if (!new SvgSceneBounds().ComputeNode(node, resolved, state)) return new SvgSceneBounds().Failure(state.Error, scene.Nodes.Count - 1 - index);
            index = index - 1;
        }
        scene.Bounds = (scene.Root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).WorldBounds;
        return new SvgSceneBoundsResult(true, SvgDrawingBoundsError.None, scene.Bounds, scene.Nodes.Count);
    }
bool ComputeNode(SvgSceneNode node, SvgResolvedNode? resolved, SvgSceneBoundsState state)
    {
        SvgRect empty = new SvgSceneBounds().Empty();
        node.FillBounds = empty;
        node.StrokeBounds = empty;
        node.MarkerBounds = empty;
        node.FilterBounds = empty;
        if (node.Kind == SvgSceneNodeKind.Path)
        {
            SvgDrawingBoundsOptions drawing = new SvgDrawingBoundsOptions();
            drawing.IncludeFill = node.Fill != null;
            drawing.Transform = node.AbsoluteTransform;
            if (state.Options.IncludeStroke && node.Stroke != null) drawing.Stroke = new SvgDrawingBounds().ComputedStroke(resolved, state.Document, true);
            if (state.Options.IncludeMarkers) drawing.Markers = new SvgSceneBounds().MarkerOptions(node, resolved, state);
            if (state.Error != SvgDrawingBoundsError.None) return false;
            if (state.Options.ApplyEffects) new SvgSceneBounds().ApplyEffects(node, drawing, state);
            if (state.Error != SvgDrawingBoundsError.None) return false;
            SvgDrawingBoundsResult bounds = new SvgDrawingBounds().Path((node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path, drawing);
            if (!bounds.Success)
            {
                state.Error = bounds.Error;
                return false;
            }
            node.FillBounds = (bounds.FillBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            node.StrokeBounds = (bounds.StrokeBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            node.MarkerBounds = (bounds.MarkerBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            node.WorldBounds = bounds.Bounds;
            return true;
        }
        if (node.Kind == SvgSceneNodeKind.Image || node.Kind == SvgSceneNodeKind.HostContent)
        {
            SvgRect? sourceBounds = node.Kind == SvgSceneNodeKind.Image ? (node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds : (node.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds;
            SvgRect local = sourceBounds == null ? empty : sourceBounds;
            SvgRect? world = new SvgSceneBounds().Transform(local, node.AbsoluteTransform, state);
            if (state.Error != SvgDrawingBoundsError.None) return false;
            node.FillBounds = (world ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            SvgDrawingBoundsOptions effects = new SvgDrawingBoundsOptions();
            if (state.Options.ApplyEffects) new SvgSceneBounds().ApplyEffects(node, effects, state);
            if (state.Error != SvgDrawingBoundsError.None) return false;
            SvgDrawingBoundsResult bounds = new SvgDrawingBounds().Effects(world, empty, empty, world, effects);
            if (!bounds.Success)
            {
                state.Error = bounds.Error;
                return false;
            }
            node.WorldBounds = bounds.Bounds;
            return true;
        }
        if (node.Kind == SvgSceneNodeKind.Text)
        {
            node.WorldBounds = empty;
            return true;
        }
        SvgRect fill = empty;
        SvgRect stroke = empty;
        SvgRect markers = empty;
        SvgRect worldBounds = empty;
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgSceneNode child = node.Children[index];
            fill = (new SvgSceneBounds().Union(fill, child.FillBounds, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            stroke = (new SvgSceneBounds().Union(stroke, child.StrokeBounds, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            markers = (new SvgSceneBounds().Union(markers, child.MarkerBounds, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            worldBounds = (new SvgSceneBounds().Union(worldBounds, child.WorldBounds, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            if (state.Error != SvgDrawingBoundsError.None) return false;
            index = index + 1;
        }
        node.FillBounds = (fill ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        node.StrokeBounds = (stroke ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        node.MarkerBounds = (markers ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgDrawingBoundsOptions groupEffects = new SvgDrawingBoundsOptions();
        if (state.Options.ApplyEffects) new SvgSceneBounds().ApplyEffects(node, groupEffects, state);
        if (state.Error != SvgDrawingBoundsError.None) return false;
        SvgDrawingBoundsResult group = new SvgDrawingBounds().Effects(fill, stroke, markers, worldBounds, groupEffects);
        if (!group.Success)
        {
            state.Error = group.Error;
            return false;
        }
        node.WorldBounds = group.Bounds;
        return true;
    }
void ApplyEffects(SvgSceneNode node, SvgDrawingBoundsOptions drawing, SvgSceneBoundsState state)
    {
        if (node.Filter != null && node.Filter.Filter != null && node.Filter.Filter.Region != null)
        {
            node.FilterBounds = (new SvgSceneBounds().Transform(node.Filter.Filter.Region, node.AbsoluteTransform, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            drawing.FilterBounds = node.FilterBounds;
        }
        if (node.Clip != null && node.Clip.Clip != null && node.Clip.Clip.Content != null)
        {
            SvgMatrixOperationResult first = node.AbsoluteTransform.Multiply(node.Clip.Clip.Transform);
            if (!first.Success)
            {
                state.Error = new SvgSceneBounds().Map(first.Error);
                return;
            }
            SvgSceneRectResult clip = new SvgSceneBounds().ContentBounds(node.Clip.Clip.Content, first.Matrix, state);
            if (!clip.Success) return;
            drawing.ClipBounds = clip.Bounds;
        }
        if (node.Mask != null && node.Mask.Mask != null && node.Mask.Mask.Region != null) drawing.MaskBounds = new SvgSceneBounds().Transform(node.Mask.Mask.Region, node.AbsoluteTransform, state);
    }
SvgMarkerGeometryOptions? MarkerOptions(SvgSceneNode node, SvgResolvedNode? resolved, SvgSceneBoundsState state)
    {
        if (node.MarkerStart == null && node.MarkerMid == null && node.MarkerEnd == null) return null;
        SvgMarkerGeometryOptions result = new SvgMarkerGeometryOptions();
        result.MaximumVertices = state.Options.MaximumMarkerVertices;
        SvgStrokeGeometryOptions? stroke = new SvgDrawingBounds().ComputedStroke(resolved, state.Document, false);
        result.StrokeWidth = stroke == null ? 1 : stroke.Style.Width;
        result.StartMarker = new SvgSceneBounds().MarkerDefinition(node.MarkerStart, state);
        result.MidMarker = new SvgSceneBounds().MarkerDefinition(node.MarkerMid, state);
        result.EndMarker = new SvgSceneBounds().MarkerDefinition(node.MarkerEnd, state);
        return result;
    }
SvgMarkerDefinition? MarkerDefinition(SvgSceneResource? resource, SvgSceneBoundsState state)
    {
        if (resource == null || resource.Marker == null || resource.Marker.Content == null) return null;
        SvgSceneRectResult content = new SvgSceneBounds().ContentBounds(resource.Marker.Content, new SvgSceneBounds().Identity(), state);
        if (!content.Success) return null;
        SvgMarkerDefinition result = new SvgMarkerDefinition();
        result.Bounds = (content.Bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.Units = resource.Marker.Units;
        result.OrientKind = resource.Marker.OrientKind;
        result.Angle = 0;
        result.RefX = 0;
        result.RefY = 0;
        result.ContentTransform = resource.Marker.Transform;
        return result;
    }
SvgSceneRectResult ContentBounds(SvgSceneNode node, SvgMatrix? parent, SvgSceneBoundsState state)
    {
        if (node == null || parent == null)
        {
            state.Error = SvgDrawingBoundsError.InvalidArgument;
            return new SvgSceneRectResult(false, null);
        }
        SvgMatrix? local = node.SourceNode == null ? node.Transform : new SvgSceneBounds().Local(node, state);
        if (state.Error != SvgDrawingBoundsError.None) return new SvgSceneRectResult(false, null);
        SvgMatrixOperationResult multiplied = parent.Multiply((local ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!multiplied.Success)
        {
            state.Error = new SvgSceneBounds().Map(multiplied.Error);
            return new SvgSceneRectResult(false, null);
        }
        SvgRect bounds = new SvgSceneBounds().Empty();
        if (node.Kind == SvgSceneNodeKind.Path)
        {
            SvgDrawingBoundsOptions drawing = new SvgDrawingBoundsOptions();
            drawing.IncludeFill = node.Fill != null;
            drawing.Transform = multiplied.Matrix;
            SvgResolvedNode? resolved = new SvgSceneBounds().Resolved(node, state.Document);
            if (state.Options.IncludeStroke && node.Stroke != null && resolved != null) drawing.Stroke = new SvgDrawingBounds().ComputedStroke(resolved, state.Document, true);
            SvgDrawingBoundsResult path = new SvgDrawingBounds().Path((node.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path, drawing);
            if (!path.Success)
            {
                state.Error = path.Error;
                return new SvgSceneRectResult(false, null);
            }
            bounds = (path.Bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        else if (node.Kind == SvgSceneNodeKind.Image) bounds = (new SvgSceneBounds().Transform((node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds, multiplied.Matrix, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        else if (node.Kind == SvgSceneNodeKind.HostContent) bounds = (new SvgSceneBounds().Transform((node.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bounds, multiplied.Matrix, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int index = 0;
        while (index < node.Children.Count && state.Error == SvgDrawingBoundsError.None)
        {
            SvgSceneRectResult child = new SvgSceneBounds().ContentBounds(node.Children[index], multiplied.Matrix, state);
            if (!child.Success) return child;
            bounds = (new SvgSceneBounds().Union(bounds, child.Bounds, state) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            index = index + 1;
        }
        return new SvgSceneRectResult(state.Error == SvgDrawingBoundsError.None, bounds);
    }
SvgMatrix? Local(SvgSceneNode node, SvgSceneBoundsState state)
    {
        SvgResolvedNode? resolved = new SvgSceneBounds().Resolved(node, state.Document);
        if (resolved == null) return node.Transform;
        SvgMatrixOperationResult local = new SvgDrawingBounds().Local(resolved);
        if (!local.Success)
        {
            state.Error = new SvgSceneBounds().Map(local.Error);
            return null;
        }
        return local.Matrix;
    }
SvgResolvedNode? Resolved(SvgSceneNode? node, SvgResolvedDocument document)
    {
        if (node == null || node.SourceNode == null) return null;
        if (node.InstancePath.Length != 0) return document.FindInstance(node.InstancePath);
        return document.FindSource(node.SourceNode);
    }
SvgRect? Transform(SvgRect? bounds, SvgMatrix? transform, SvgSceneBoundsState state)
    {
        if (bounds == null || bounds.IsEmpty) return new SvgSceneBounds().Empty();
        SvgRectTransformResult result = (transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(bounds);
        if (!result.Success)
        {
            state.Error = new SvgSceneBounds().Map(result.Error);
            return null;
        }
        return result.Rectangle;
    }
SvgRect? Union(SvgRect? left, SvgRect? right, SvgSceneBoundsState state)
    {
        if (left == null || right == null)
        {
            state.Error = SvgDrawingBoundsError.InvalidArgument;
            return null;
        }
        SvgRectOperationResult result = left.Union(right);
        if (!result.Success)
        {
            state.Error = new SvgSceneBounds().Map(result.Error);
            return null;
        }
        return result.Rectangle;
    }
bool Valid(SvgScene? scene, SvgResolvedDocument? document, SvgSceneBoundsOptions? options)
    {
        if (scene == null || document == null || options == null || scene.Root == null || scene.Nodes == null || scene.SourceDocument == null || document.SourceDocument == null) return false;
        if (!object.ReferenceEquals(scene.SourceDocument, document.SourceDocument) || scene.SourceVersion != document.SourceVersion || !document.IsCurrent()) return false;
        return options.MaximumNodes > 0 && options.MaximumMarkerVertices > 0;
    }
SvgSceneBoundsResult Failure(SvgDrawingBoundsError error, int processed)
    {
        return new SvgSceneBoundsResult(false, error, null, processed);
    }
SvgDrawingBoundsError Map(SvgGeometryError error)
    {
        return error == SvgGeometryError.InvalidArgument ? SvgDrawingBoundsError.InvalidArgument : SvgDrawingBoundsError.NonFiniteResult;
    }
SvgRect Empty()
    {
        return SvgRect.Create(0, 0, 0, 0, true);
    }
SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}
