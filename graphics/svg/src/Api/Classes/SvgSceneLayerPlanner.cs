namespace svg;

/// <summary>Plans display, visibility, paint order, object boxes, clips, and compositing layers.</summary>
public class SvgSceneLayerPlanner : ISvgSceneLayerPlanner
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSceneLayerPlanner()
    {
    }

    /// <summary>Plans a normalized scene using bounded defaults.</summary>
    /// <param name="scene">The current normalized scene.</param>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <returns>The deterministic layer plan.</returns>
    public SvgSceneLayerPlan PlanWithSceneAndDocument(SvgScene scene, SvgResolvedDocument document)
    {
        return new SvgSceneLayerPlanner().PlanWithSceneAndDocumentAndOptions(scene, document, new SvgSceneLayerOptions());
    }

    /// <summary>Plans a normalized scene under an explicit layer bound.</summary>
    /// <param name="scene">The current normalized scene.</param>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="options">The layer traversal options.</param>
    /// <returns>The complete or failed deterministic layer plan.</returns>
    public SvgSceneLayerPlan PlanWithSceneAndDocumentAndOptions(SvgScene scene, SvgResolvedDocument document, SvgSceneLayerOptions options)
    {
        SvgSceneLayerPlan plan = new SvgSceneLayerPlan(scene);
        if (!new SvgSceneLayerPlanner().Valid(scene, document, options))
        {
            plan.Error = SvgDrawingBoundsError.InvalidArgument;
            return plan;
        }
        if ((scene.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count > options.MaximumLayers)
        {
            plan.Error = SvgDrawingBoundsError.LimitExceeded;
            return plan;
        }
        SvgSceneLayerState state = new SvgSceneLayerState(plan, document, options);
        plan.Root = new SvgSceneLayerPlanner().Build(scene.Root, null, null, state);
        if (plan.Root == null || state.Error != SvgDrawingBoundsError.None)
        {
            plan.Error = state.Error == SvgDrawingBoundsError.None ? SvgDrawingBoundsError.InvalidArgument : state.Error;
            return plan;
        }
        plan.PaintBounds = plan.Root.PaintBounds;
        plan.Success = true;
        scene.LayerPlan = plan;
        scene.Bounds = plan.PaintBounds;
        return plan;
    }
SvgSceneLayer? Build(SvgSceneNode? node, SvgSceneLayer? parent, SvgRect? inheritedClip, SvgSceneLayerState state)
    {
        if (node == null || state.Plan.Layers.Count >= state.Options.MaximumLayers)
        {
            state.Error = node == null ? SvgDrawingBoundsError.InvalidArgument : SvgDrawingBoundsError.LimitExceeded;
            return null;
        }
        SvgSceneLayer layer = new SvgSceneLayer(node, parent);
        state.Plan.Layers.Add(layer);
        if (parent != null) parent.Children.Add(layer);
        if (parent == null) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Root);
        if (node.Opacity < 1) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Opacity);
        if (node.BlendMode != "normal") new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Blend);
        if (node.Isolated) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Isolation);
        if (node.Clip != null || node.OverflowClip != null) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Clip);
        if (node.Mask != null) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Mask);
        if (node.Filter != null) new SvgSceneLayerPlanner().AddFeature(layer, SvgSceneLayerFeature.Filter);
        layer.CreatesCompositingLayer = parent == null || node.Opacity < 1 || node.BlendMode != "normal" || node.Isolated || node.Mask != null || node.Filter != null;
        if (layer.CreatesCompositingLayer) state.Plan.CompositingLayerCount = state.Plan.CompositingLayerCount + 1;
        SvgRect? ownClip = new SvgSceneLayerPlanner().OverflowClip(node, state);
        if (state.Error != SvgDrawingBoundsError.None) return null;
        layer.OwnClipBounds = ownClip;
        layer.EffectiveClipBounds = new SvgSceneLayerPlanner().IntersectClips(inheritedClip, ownClip, state);
        if (state.Error != SvgDrawingBoundsError.None) return null;
        layer.Paintable = new SvgSceneLayerPlanner().OwnPaintable(node);
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgSceneLayer? child = new SvgSceneLayerPlanner().Build(node.Children[index], layer, layer.EffectiveClipBounds, state);
            if (child == null) return null;
            index = index + 1;
        }
        bool childPaintable = false;
        SvgRect? bounds = layer.Paintable ? node.WorldBounds : new SvgSceneLayerPlanner().Empty();
        index = 0;
        while (index < layer.Children.Count)
        {
            SvgSceneLayer child = layer.Children[index];
            if (child.SubtreePaintable) childPaintable = true;
            bounds = new SvgSceneLayerPlanner().Union(bounds, child.PaintBounds, state);
            if (state.Error != SvgDrawingBoundsError.None) return null;
            index = index + 1;
        }
        layer.SubtreePaintable = node.Displayed && node.Opacity > 0 && (layer.Paintable || childPaintable);
        if (!layer.SubtreePaintable) bounds = new SvgSceneLayerPlanner().Empty();
        else if (node.Kind == SvgSceneNodeKind.Group && (node.Filter != null || node.Mask != null || node.Clip != null) && node.WorldBounds != null) bounds = node.WorldBounds;
        if (layer.EffectiveClipBounds != null) bounds = new SvgSceneLayerPlanner().Intersect(bounds, layer.EffectiveClipBounds, state);
        layer.PaintBounds = (bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return layer;
    }
bool OwnPaintable(SvgSceneNode node)
    {
        if (!node.Displayed || !node.Visible || node.Opacity <= 0) return false;
        if (node.Kind == SvgSceneNodeKind.Path)
        {
            bool fill = node.Fill != null && node.FillOpacity > 0;
            bool stroke = node.Stroke != null && node.StrokeOpacity > 0;
            bool markers = node.MarkerStart != null || node.MarkerMid != null || node.MarkerEnd != null;
            return fill || stroke || markers;
        }
        if (node.Kind == SvgSceneNodeKind.Image) return node.Image != null && node.Image.Resource != null;
        if (node.Kind == SvgSceneNodeKind.HostContent) return node.HostContent != null && node.HostContent.RasterPixels != null;
        return node.Kind == SvgSceneNodeKind.Text && node.Text != null && node.Text.Text.Length != 0;
    }
SvgRect? OverflowClip(SvgSceneNode node, SvgSceneLayerState state)
    {
        if (node.OverflowClip == null) return null;
        SvgMatrix matrix = node.AbsoluteTransform;
        if (node.Group != null && node.Group.EstablishesViewport)
        {
            SvgResolvedNode? resolved = new SvgSceneLayerPlanner().Resolved(node, state.Document);
            if (resolved == null)
            {
                state.Error = SvgDrawingBoundsError.InvalidArgument;
                return null;
            }
            SvgMatrix parent = new SvgSceneLayerPlanner().Identity();
            if (resolved.Parent != null)
            {
                SvgMatrixOperationResult parentWorld = new SvgDrawingBounds().World(resolved.Parent);
                if (!parentWorld.Success)
                {
                    state.Error = new SvgSceneLayerPlanner().Map(parentWorld.Error);
                    return null;
                }
                parent = (parentWorld.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            }
            SvgMatrixOperationResult computed = new SvgDrawingBounds().ComputedTransform(resolved);
            if (!computed.Success)
            {
                state.Error = new SvgSceneLayerPlanner().Map(computed.Error);
                return null;
            }
            SvgMatrixOperationResult combined = (parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply((computed.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            if (!combined.Success)
            {
                state.Error = new SvgSceneLayerPlanner().Map(combined.Error);
                return null;
            }
            matrix = (combined.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        SvgRectTransformResult transformed = (matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(node.OverflowClip);
        if (!transformed.Success)
        {
            state.Error = new SvgSceneLayerPlanner().Map(transformed.Error);
            return null;
        }
        return transformed.Rectangle;
    }
SvgResolvedNode? Resolved(SvgSceneNode? node, SvgResolvedDocument document)
    {
        if (node == null || node.SourceNode == null) return null;
        if (node.InstancePath.Length != 0) return document.FindInstance(node.InstancePath);
        return document.FindSource(node.SourceNode);
    }
SvgRect? IntersectClips(SvgRect? inherited, SvgRect? local, SvgSceneLayerState state)
    {
        if (inherited == null) return local;
        if (local == null) return inherited;
        return new SvgSceneLayerPlanner().Intersect(inherited, local, state);
    }
SvgRect? Intersect(SvgRect? left, SvgRect right, SvgSceneLayerState state)
    {
        SvgRectOperationResult result = (left ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Intersect(right);
        if (!result.Success)
        {
            state.Error = new SvgSceneLayerPlanner().Map(result.Error);
            return null;
        }
        return result.Rectangle;
    }
SvgRect? Union(SvgRect? left, SvgRect right, SvgSceneLayerState state)
    {
        if (left == null || right == null)
        {
            state.Error = SvgDrawingBoundsError.InvalidArgument;
            return null;
        }
        SvgRectOperationResult result = left.Union(right);
        if (!result.Success)
        {
            state.Error = new SvgSceneLayerPlanner().Map(result.Error);
            return null;
        }
        return result.Rectangle;
    }
void AddFeature(SvgSceneLayer layer, SvgSceneLayerFeature feature)
    {
        layer.Features.Add(feature);
    }
bool Valid(SvgScene? scene, SvgResolvedDocument? document, SvgSceneLayerOptions? options)
    {
        if (scene == null || document == null || options == null || options.MaximumLayers < 1 || scene.Root == null || scene.Nodes == null || scene.Bounds == null) return false;
        if (scene.SourceDocument == null || !object.ReferenceEquals(scene.SourceDocument, document.SourceDocument) || scene.SourceVersion != document.SourceVersion) return false;
        return document.IsCurrent();
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
