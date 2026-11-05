namespace svg;

/// <summary>Configures bounded deterministic scene-layer planning.</summary>
public class SvgSceneLayerOptions
{
    /// <summary>The maximum scene nodes and layer records processed.</summary>
    public int MaximumLayers;

    /// <summary>Creates deterministic bounded layer-planning defaults.</summary>
    public SvgSceneLayerOptions()
    {
        MaximumLayers = 100000;
    }
}

/// <summary>Contains one scene node's retained painting and compositing plan.</summary>
public class SvgSceneLayer
{
    /// <summary>The corresponding normalized scene node.</summary>
    public SvgSceneNode Node;
    /// <summary>The containing layer record, or <c>null</c> for the root.</summary>
    public SvgSceneLayer? Parent;
    /// <summary>The ordered child layer records.</summary>
    public System.Collections.Generic.List<SvgSceneLayer> Children;
    /// <summary>The complete fill, stroke, and marker order.</summary>
    public System.Collections.Generic.List<SvgPaintOrderComponent> PaintOrder;
    /// <summary>The semantic features affecting planning.</summary>
    public System.Collections.Generic.List<SvgSceneLayerFeature> Features;
    /// <summary>Whether this record requires a distinct compositing layer.</summary>
    public bool CreatesCompositingLayer;
    /// <summary>Whether this node has an enabled own paint contribution.</summary>
    public bool Paintable;
    /// <summary>Whether this node or an ordered descendant has an enabled contribution.</summary>
    public bool SubtreePaintable;
    /// <summary>The inherited and local world-space overflow clip, or <c>null</c>.</summary>
    public SvgRect? EffectiveClipBounds;
    /// <summary>The node's own world-space overflow clip, or <c>null</c>.</summary>
    public SvgRect? OwnClipBounds;
    /// <summary>The final visible world-space bounds after display, visibility, opacity, effects, and overflow clips.</summary>
    public SvgRect PaintBounds;
    /// <summary>The local opacity applied to the composited node result.</summary>
    public double Opacity;
    /// <summary>The normalized backdrop blend mode.</summary>
    public string BlendMode;
    /// <summary>The sRGB or linear-sRGB rendering interpolation space.</summary>
    public SvgColorSpace ColorInterpolation;
    /// <summary>Whether an isolated stacking context is required.</summary>
    public bool Isolated;
    /// <summary>Whether a local object bounding box is available for object-box resources.</summary>
    public bool HasObjectBoundingBox;
    /// <summary>The local object bounding box, or <c>null</c>.</summary>
    public SvgRect? ObjectBoundingBox;
    /// <summary>The normalized object-box to local-user-space transform.</summary>
    public SvgMatrix ObjectBoundingBoxTransform;

    internal SvgSceneLayer(SvgSceneNode node, SvgSceneLayer? parent)
    {
        Node = node;
        Parent = parent;
        Children = new System.Collections.Generic.List<SvgSceneLayer>();
        PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>(node.PaintOrder);
        Features = new System.Collections.Generic.List<SvgSceneLayerFeature>();
        CreatesCompositingLayer = false;
        Paintable = false;
        SubtreePaintable = false;
        EffectiveClipBounds = null;
        OwnClipBounds = null;
        PaintBounds = Empty();
        Opacity = node.Opacity;
        BlendMode = node.BlendMode;
        ColorInterpolation = node.ColorInterpolation;
        Isolated = node.Isolated;
        HasObjectBoundingBox = node.HasObjectBoundingBox;
        ObjectBoundingBox = node.ObjectBoundingBox;
        ObjectBoundingBoxTransform = node.ObjectBoundingBoxTransform;
    }

    internal static SvgRect Empty()
    {
        return SvgRect.Create(0, 0, 0, 0, true);
    }
}

/// <summary>Contains a deterministic scene layer plan in node preorder.</summary>
public class SvgSceneLayerPlan
{
    /// <summary>The planned scene.</summary>
    public SvgScene Scene;
    /// <summary>The root layer record, or <c>null</c> after failure.</summary>
    public SvgSceneLayer? Root;
    /// <summary>Every layer record in deterministic scene preorder.</summary>
    public System.Collections.Generic.List<SvgSceneLayer> Layers;
    /// <summary>The final visible scene bounds.</summary>
    public SvgRect PaintBounds;
    /// <summary>The number of records requiring distinct compositing surfaces.</summary>
    public int CompositingLayerCount;
    /// <summary>Whether planning completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgDrawingBoundsError Error;

    internal SvgSceneLayerPlan(SvgScene scene)
    {
        Scene = scene;
        Root = null;
        Layers = new System.Collections.Generic.List<SvgSceneLayer>();
        PaintBounds = SvgRect.Create(0, 0, 0, 0, true);
        CompositingLayerCount = 0;
        Success = false;
        Error = SvgDrawingBoundsError.None;
    }

    /// <summary>Finds the layer record for one scene node identity.</summary>
    /// <param name="node">The exact scene node.</param>
    /// <returns>The matching layer record, or <c>null</c>.</returns>
    internal SvgSceneLayer? Find(SvgSceneNode node)
    {
        int index = 0;
        while (index < Layers.Count)
        {
            if (object.ReferenceEquals(Layers[index].Node, node)) return Layers[index];
            index = index + 1;
        }
        return null;
    }
}
