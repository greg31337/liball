namespace svg;

/// <summary>Configures marker, clipping-path, and mask resolution.</summary>
public class SvgEffectResolveOptions
{
    /// <summary>The typed attribute accessor.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The URL reference options used by nested references.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The nested reference and expansion limits.</summary>
    public SvgReferenceLimits Limits;
    /// <summary>The referencing element's viewport and font length context.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The referencing element's object bounding box, or <c>null</c>.</summary>
    public SvgRect? TargetBounds;
    /// <summary>The referencing element's used stroke width.</summary>
    public double StrokeWidth;
    /// <summary>The marker placement point in referencing user space.</summary>
    public SvgPoint MarkerPosition;
    /// <summary>The path-derived marker angle in degrees.</summary>
    public double MarkerAutoAngle;
    /// <summary>The marker vertex role.</summary>
    public SvgMarkerPlacement MarkerPlacement;
    /// <summary>The stylesheets used for resource content and nested effects.</summary>
    public System.Collections.Generic.List<SvgStyleSheet> StyleSheets;
    /// <summary>The cascade and computed-style options.</summary>
    public SvgStyleResolverOptions StyleOptions;
    /// <summary>An optional precomputed style for the requested resource.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The maximum number of resource descendants inspected.</summary>
    public int MaxContentNodes;

    /// <summary>Creates deterministic bounded effect-resolution defaults.</summary>
    public SvgEffectResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ReferenceOptions = new SvgReferenceResolveOptions();
        Limits = new SvgReferenceLimits();
        LengthContext = new SvgLengthContext();
        TargetBounds = null;
        StrokeWidth = 1;
        MarkerPosition = new SvgPoint(0, 0);
        MarkerAutoAngle = 0;
        MarkerPlacement = SvgMarkerPlacement.Mid;
        StyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleOptions = new SvgStyleResolverOptions();
        ComputedStyle = null;
        MaxContentNodes = 100000;
    }
}

/// <summary>Contains one inspected source node and its computed resource style.</summary>
public class SvgEffectContent
{
    /// <summary>The authored resource-content node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The computed style, or <c>null</c> for a non-element node.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>A nested use-element expansion, or <c>null</c>.</summary>
    public SvgUseResult? Use;

    internal SvgEffectContent(SvgNode? node)
    {
        SourceNode = node;
        SourceVersion = node == null ? 0 : node.Version;
        ComputedStyle = null;
        Use = null;
    }
}

/// <summary>Records a nested use, clipping-path, or mask reference.</summary>
public class SvgEffectReference
{
    /// <summary>The source element containing the computed or href reference.</summary>
    public SvgNode SourceNode;
    /// <summary>The property or attribute name that supplied the reference.</summary>
    public string Name;
    /// <summary>The general URL resolution result.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The recursively resolved clipping path or mask, or <c>null</c>.</summary>
    public SvgEffectResult? Effect;
    /// <summary>The expanded use reference, or <c>null</c>.</summary>
    public SvgUseResult? Use;
    /// <summary>Whether this edge would re-enter an active effect resource.</summary>
    public bool Cycle;
    /// <summary>Whether the nested reference is valid and usable.</summary>
    public bool Success;

    internal SvgEffectReference(SvgNode node, string name)
    {
        SourceNode = node;
        Name = name;
        Reference = null;
        Effect = null;
        Use = null;
        Cycle = false;
        Success = false;
    }
}

/// <summary>Contains normalized marker, clipping-path, or mask resource semantics.</summary>
public class SvgEffectResult
{
    /// <summary>The requested resource element.</summary>
    public SvgNode? Node;
    /// <summary>The requested source version captured during resolution.</summary>
    public long NodeVersion;
    /// <summary>The resource kind.</summary>
    public SvgEffectKind Kind;
    /// <summary>The computed style of the requested resource.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The relevant direct content children in source order.</summary>
    public System.Collections.Generic.List<SvgNode> Children;
    /// <summary>All inspected resource descendants in deterministic pre-order.</summary>
    public System.Collections.Generic.List<SvgEffectContent> Content;
    /// <summary>Nested use, clipping-path, and mask reference edges.</summary>
    public System.Collections.Generic.List<SvgEffectReference> References;
    /// <summary>The marker coordinate-system mode.</summary>
    public SvgMarkerUnits MarkerUnits;
    /// <summary>The clipping-path or mask region coordinate-system mode.</summary>
    public SvgPaintServerUnits Units;
    /// <summary>The mask content coordinate-system mode.</summary>
    public SvgPaintServerUnits ContentUnits;
    /// <summary>The marker orientation mode.</summary>
    public SvgMarkerOrientKind OrientKind;
    /// <summary>The authored fixed marker angle in degrees.</summary>
    public double OrientAngle;
    /// <summary>The placement-specific marker angle in degrees.</summary>
    public double AppliedAngle;
    /// <summary>The marker viewport before stroke-width and placement transforms.</summary>
    public SvgRect? Viewport;
    /// <summary>Whether the marker viewport clips overflowing child graphics.</summary>
    public bool ClipsOverflow;
    /// <summary>The local marker viewport clip, or <c>null</c> when overflow is visible.</summary>
    public SvgRect? ClipRect;
    /// <summary>The marker view box, or <c>null</c>.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The effective marker preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>The effective marker reference x coordinate in marker content space.</summary>
    public double RefX;
    /// <summary>The effective marker reference y coordinate in marker content space.</summary>
    public double RefY;
    /// <summary>The marker child length context after viewport establishment.</summary>
    public SvgLengthContext? ContentLengthContext;
    /// <summary>The authored clip-path transform before implicit unit conversion.</summary>
    public SvgMatrix ExplicitTransform;
    /// <summary>The resource-content to referencing-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The mask-content to referencing-user-space transform.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The mask painting region in maskUnits coordinates.</summary>
    public SvgRect? Region;
    /// <summary>The mask painting region in referencing user space.</summary>
    public SvgRect? UserRegion;
    /// <summary>The mask source interpretation mode.</summary>
    public SvgMaskType MaskType;
    /// <summary>Whether the resource has no effective graphical content.</summary>
    public bool Empty;
    /// <summary>Whether the resolved resource can contribute to rendering.</summary>
    public bool Renderable;
    /// <summary>Located parsing, geometry, style, reference, and cycle diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether resolution completed without error diagnostics.</summary>
    public bool Success;

    internal SvgEffectResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        Kind = SvgEffectKind.None;
        ComputedStyle = null;
        Children = new System.Collections.Generic.List<SvgNode>();
        Content = new System.Collections.Generic.List<SvgEffectContent>();
        References = new System.Collections.Generic.List<SvgEffectReference>();
        MarkerUnits = SvgMarkerUnits.StrokeWidth;
        Units = SvgPaintServerUnits.UserSpaceOnUse;
        ContentUnits = SvgPaintServerUnits.UserSpaceOnUse;
        OrientKind = SvgMarkerOrientKind.Angle;
        OrientAngle = 0;
        AppliedAngle = 0;
        Viewport = null;
        ClipsOverflow = false;
        ClipRect = null;
        ViewBox = null;
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        RefX = 0;
        RefY = 0;
        ContentLengthContext = null;
        ExplicitTransform = Identity();
        Transform = Identity();
        ContentTransform = Identity();
        Region = null;
        UserRegion = null;
        MaskType = SvgMaskType.Luminance;
        Empty = true;
        Renderable = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks whether all captured source and nested-resource versions remain current.</summary>
    /// <returns><c>true</c> when the result can still be reused.</returns>
    internal bool IsCurrent()
    {
        if (Node == null || Node.Version != NodeVersion) return false;
        int index = 0;
        while (index < Content.Count)
        {
            SvgEffectContent content = Content[index];
            if (content.SourceNode == null || content.SourceNode.Version != content.SourceVersion) return false;
            if (content.Use != null && !content.Use.IsCurrent()) return false;
            index = index + 1;
        }
        index = 0;
        while (index < References.Count)
        {
            SvgEffectReference reference = References[index];
            if (reference.Reference != null && reference.Reference.Node != null && reference.Reference.Node.Version != reference.Reference.NodeVersion) return false;
            if (reference.Effect != null && !reference.Effect.IsCurrent()) return false;
            if (reference.Use != null && !reference.Use.IsCurrent()) return false;
            index = index + 1;
        }
        return true;
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}
