namespace svg;

/// <summary>Contains group viewport and clipping data.</summary>
public class SvgSceneGroupData
{
    /// <summary>Whether this group establishes an SVG viewport.</summary>
    public bool EstablishesViewport;
    /// <summary>The established viewport in parent coordinates, or <c>null</c>.</summary>
    public SvgRect? ViewportBounds;
    /// <summary>The overflow clip in parent coordinates, or <c>null</c>.</summary>
    public SvgRect? OverflowClip;

    /// <summary>Creates empty group data.</summary>
    public SvgSceneGroupData()
    {
        EstablishesViewport = false;
        ViewportBounds = null;
        OverflowClip = null;
    }
}

/// <summary>Contains one normalized scene path and its fill rule.</summary>
public class SvgScenePathData
{
    /// <summary>The absolute normalized path, or <c>null</c> until assigned.</summary>
    public SvgPathData? Path;
    /// <summary>The path fill rule.</summary>
    public SvgFillRule FillRule;
    /// <summary>The computed stroke style in local user coordinates.</summary>
    public SvgStrokeStyle StrokeStyle;
    /// <summary>Whether stroke metrics scale with the path transform.</summary>
    public SvgStrokeVectorEffect VectorEffect;
    /// <summary>The effective shape-rendering hint.</summary>
    public SvgShapeRendering ShapeRendering;

    /// <summary>Creates empty nonzero-fill path data.</summary>
    public SvgScenePathData()
    {
        Path = null;
        FillRule = SvgFillRule.Nonzero;
        StrokeStyle = new SvgStrokeStyle();
        VectorEffect = SvgStrokeVectorEffect.None;
        ShapeRendering = SvgShapeRendering.Auto;
    }
}

/// <summary>Contains normalized text content and its local origin.</summary>
public class SvgSceneTextData
{
    /// <summary>The normalized text content.</summary>
    public string Text;
    /// <summary>The local horizontal text origin.</summary>
    public double X;
    /// <summary>The local vertical text origin.</summary>
    public double Y;
    /// <summary>The resolved local font size used by fallback text geometry.</summary>
    public double FontSize;

    /// <summary>Creates empty text at the local origin.</summary>
    public SvgSceneTextData()
    {
        Text = "";
        X = 0;
        Y = 0;
        FontSize = 16;
    }
}

/// <summary>Contains one positioned image resource and intrinsic mapping.</summary>
public class SvgSceneImageData
{
    /// <summary>The shared image resource, or <c>null</c> until assigned.</summary>
    public SvgSceneResource? Resource;
    /// <summary>The image positioning rectangle.</summary>
    public SvgRect? Bounds;
    /// <summary>The mapping from intrinsic image coordinates to local scene coordinates.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The effective image preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>The effective image interpolation hint.</summary>
    public SvgImageRendering Rendering;
    /// <summary>The resolved image host-content handoff record, or <c>null</c>.</summary>
    public SvgHostContentRecord? HostContent;

    /// <summary>Creates empty image data with identity mapping.</summary>
    public SvgSceneImageData()
    {
        Resource = null;
        Bounds = null;
        ContentTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        Rendering = SvgImageRendering.Auto;
        HostContent = null;
    }
}

/// <summary>Contains one normalized host-owned content record and optional raster output.</summary>
public class SvgSceneHostContentData
{
    /// <summary>The embedded-content kind.</summary>
    public SvgEmbeddedContentKind Kind;
    /// <summary>The element positioning rectangle.</summary>
    public SvgRect? Bounds;
    /// <summary>The host content bounds in local SVG user space.</summary>
    public SvgRect? ContentBounds;
    /// <summary>The mapping from callback raster coordinates into local SVG user space.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The optional independently retained callback raster.</summary>
    public SvgPixelBuffer? RasterPixels;
    /// <summary>The resolved host-content handoff record.</summary>
    public SvgHostContentRecord? Record;

    /// <summary>Creates empty host-content data with identity mapping.</summary>
    public SvgSceneHostContentData()
    {
        Kind = SvgEmbeddedContentKind.None;
        Bounds = null;
        ContentBounds = null;
        ContentTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        RasterPixels = null;
        Record = null;
    }
}

/// <summary>Contains one immutable-by-convention normalized graphics node.</summary>
public class SvgSceneNode
{
    /// <summary>The tagged node payload kind.</summary>
    public SvgSceneNodeKind Kind;
    /// <summary>The source node identity, or <c>null</c> for generated content.</summary>
    public SvgNode? SourceNode;
    /// <summary>The captured source version.</summary>
    public long SourceVersion;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>The parent scene node, or <c>null</c> for a root.</summary>
    public SvgSceneNode? Parent;
    /// <summary>The ordered child scene nodes.</summary>
    public System.Collections.Generic.List<SvgSceneNode> Children;
    /// <summary>The local transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The complete local-to-world transform.</summary>
    public SvgMatrix AbsoluteTransform;
    /// <summary>The clamped local opacity.</summary>
    public double Opacity;
    /// <summary>The clamped fill-paint opacity.</summary>
    public double FillOpacity;
    /// <summary>The clamped stroke-paint opacity.</summary>
    public double StrokeOpacity;
    /// <summary>Whether computed display permits this node and its subtree.</summary>
    public bool Displayed;
    /// <summary>Whether computed visibility permits this node's own painting.</summary>
    public bool Visible;
    /// <summary>The complete fill, stroke, and marker painting order.</summary>
    public System.Collections.Generic.List<SvgPaintOrderComponent> PaintOrder;
    /// <summary>The normalized blend-mode keyword.</summary>
    public string BlendMode;
    /// <summary>The sRGB or linear-sRGB interpolation space selected for rendering.</summary>
    public SvgColorSpace ColorInterpolation;
    /// <summary>Whether this node starts an isolated compositing group.</summary>
    public bool Isolated;
    /// <summary>The node-local overflow clip, or <c>null</c> when overflow is visible.</summary>
    public SvgRect? OverflowClip;
    /// <summary>Whether a finite local object bounding box is available.</summary>
    public bool HasObjectBoundingBox;
    /// <summary>The local object bounding box, or <c>null</c>.</summary>
    public SvgRect? ObjectBoundingBox;
    /// <summary>The normalized object-box to local-user-space transform.</summary>
    public SvgMatrix ObjectBoundingBoxTransform;
    /// <summary>The local geometry bounds, or <c>null</c>.</summary>
    public SvgRect? LocalBounds;
    /// <summary>The transformed fill or embedded-content bounds.</summary>
    public SvgRect FillBounds;
    /// <summary>The transformed stroke bounds.</summary>
    public SvgRect StrokeBounds;
    /// <summary>The transformed marker bounds.</summary>
    public SvgRect MarkerBounds;
    /// <summary>The transformed filter output region, or an explicitly empty rectangle.</summary>
    public SvgRect FilterBounds;
    /// <summary>The precomputed absolute world bounds, or <c>null</c>.</summary>
    public SvgRect? WorldBounds;
    /// <summary>The matching group payload, or <c>null</c>.</summary>
    public SvgSceneGroupData? Group;
    /// <summary>The matching path payload, or <c>null</c>.</summary>
    public SvgScenePathData? Path;
    /// <summary>The matching text payload, or <c>null</c>.</summary>
    public SvgSceneTextData? Text;
    /// <summary>The matching image payload, or <c>null</c>.</summary>
    public SvgSceneImageData? Image;
    /// <summary>The matching host-content payload, or <c>null</c>.</summary>
    public SvgSceneHostContentData? HostContent;
    /// <summary>The shared fill-paint resource, or <c>null</c>.</summary>
    public SvgSceneResource? Fill;
    /// <summary>The shared stroke-paint resource, or <c>null</c>.</summary>
    public SvgSceneResource? Stroke;
    /// <summary>Whether the fill is supplied by the referencing element's context fill.</summary>
    public bool FillUsesContext;
    /// <summary>Whether the stroke is supplied by the referencing element's context stroke.</summary>
    public bool StrokeUsesContext;
    /// <summary>The shared start-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerStart;
    /// <summary>The shared middle-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerMid;
    /// <summary>The shared end-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerEnd;
    /// <summary>The shared clipping resource, or <c>null</c>.</summary>
    public SvgSceneResource? Clip;
    /// <summary>The shared mask resource, or <c>null</c>.</summary>
    public SvgSceneResource? Mask;
    /// <summary>The shared filter resource, or <c>null</c>.</summary>
    public SvgSceneResource? Filter;

    /// <summary>Creates a node with exactly one payload matching the supplied tag.</summary>
    /// <param name="kind">The normalized node kind.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the tag is not recognized.</exception>
    public SvgSceneNode(SvgSceneNodeKind kind)
    {
        if ((int)kind < 0 || (int)kind > 4) throw new System.ArgumentOutOfRangeException("kind");
        Kind = kind;
        SourceNode = null;
        SourceVersion = 0;
        SourcePath = "";
        InstancePath = "";
        Parent = null;
        Children = new System.Collections.Generic.List<SvgSceneNode>();
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        AbsoluteTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Opacity = 1;
        FillOpacity = 1;
        StrokeOpacity = 1;
        Displayed = true;
        Visible = true;
        PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>();
        PaintOrder.Add(SvgPaintOrderComponent.Fill);
        PaintOrder.Add(SvgPaintOrderComponent.Stroke);
        PaintOrder.Add(SvgPaintOrderComponent.Markers);
        BlendMode = "normal";
        ColorInterpolation = SvgColorSpace.Srgb;
        Isolated = false;
        OverflowClip = null;
        HasObjectBoundingBox = false;
        ObjectBoundingBox = null;
        ObjectBoundingBoxTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        LocalBounds = null;
        FillBounds = SvgRect.Create(0, 0, 0, 0, true);
        StrokeBounds = SvgRect.Create(0, 0, 0, 0, true);
        MarkerBounds = SvgRect.Create(0, 0, 0, 0, true);
        FilterBounds = SvgRect.Create(0, 0, 0, 0, true);
        WorldBounds = null;
        Group = kind == SvgSceneNodeKind.Group ? new SvgSceneGroupData() : null;
        Path = kind == SvgSceneNodeKind.Path ? new SvgScenePathData() : null;
        Text = kind == SvgSceneNodeKind.Text ? new SvgSceneTextData() : null;
        Image = kind == SvgSceneNodeKind.Image ? new SvgSceneImageData() : null;
        HostContent = kind == SvgSceneNodeKind.HostContent ? new SvgSceneHostContentData() : null;
        Fill = null;
        Stroke = null;
        FillUsesContext = false;
        StrokeUsesContext = false;
        MarkerStart = null;
        MarkerMid = null;
        MarkerEnd = null;
        Clip = null;
        Mask = null;
        Filter = null;
    }

    /// <summary>Tests whether exactly one payload matches this node's tag.</summary>
    /// <returns><c>true</c> when the tagged payload is structurally valid.</returns>
    internal bool HasValidTag()
    {
        int count = 0;
        if (Group != null) count = count + 1;
        if (Path != null) count = count + 1;
        if (Text != null) count = count + 1;
        if (Image != null) count = count + 1;
        if (HostContent != null) count = count + 1;
        if (count != 1) return false;
        if (Kind == SvgSceneNodeKind.Group) return Group != null;
        if (Kind == SvgSceneNodeKind.Path) return Path != null;
        if (Kind == SvgSceneNodeKind.Text) return Text != null;
        if (Kind == SvgSceneNodeKind.Image) return Image != null;
        if (Kind == SvgSceneNodeKind.HostContent) return HostContent != null;
        return false;
    }

    /// <summary>Adds an unattached child to a group while preventing cycles.</summary>
    /// <param name="child">The child scene node.</param>
    /// <returns><c>true</c> when the child was attached.</returns>
    internal bool AddChild(SvgSceneNode? child)
    {
        if (Kind != SvgSceneNodeKind.Group || child == null || child.Parent != null || object.ReferenceEquals(child, this)) return false;
        SvgSceneNode? ancestor = this;
        while (ancestor != null)
        {
            if (object.ReferenceEquals(ancestor, child)) return false;
            ancestor = ancestor.Parent;
        }
        child.Parent = this;
        Children.Add(child);
        return true;
    }

    /// <summary>Removes one direct child and clears its parent link.</summary>
    /// <param name="child">The child to detach.</param>
    /// <returns><c>true</c> when the child was present.</returns>
    internal bool RemoveChild(SvgSceneNode child)
    {
        int index = IndexOfChild(child);
        if (index < 0) return false;
        Children.RemoveAt(index);
        child.Parent = null;
        return true;
    }

    /// <summary>Captures source and instance identity from a resolved node.</summary>
    /// <param name="source">The source resolved node.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when the source is <c>null</c>.</exception>
    internal void SetIdentity(SvgResolvedNode source)
    {
        if (source == null) throw new System.ArgumentNullException("source");
        SourceNode = source.SourceNode;
        SourceVersion = source.SourceVersion;
        SourcePath = source.SourcePath;
        InstancePath = source.InstancePath;
    }

    internal int IndexOfChild(SvgSceneNode child)
    {
        int index = 0;
        while (index < Children.Count)
        {
            if (object.ReferenceEquals(Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }
}
