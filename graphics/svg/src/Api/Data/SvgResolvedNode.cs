namespace svg;

/// <summary>Contains the current computed semantic state for one source node or use instance.</summary>
public class SvgResolvedNode
{
    /// <summary>The owning resolved document.</summary>
    public SvgResolvedDocument Document;
    /// <summary>The parent resolved node, or <c>null</c> for the resolved root.</summary>
    public SvgResolvedNode? Parent;
    /// <summary>The semantic payload kind.</summary>
    public SvgResolvedNodeKind Kind;
    /// <summary>The corresponding authored source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source node version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The stable path to the authored source node.</summary>
    public string SourcePath;
    /// <summary>The globally stable use-instance path, or an empty string for authored nodes.</summary>
    public string InstancePath;
    /// <summary>The generated use instance, or <c>null</c> for authored nodes.</summary>
    public SvgUseInstance? Instance;
    /// <summary>The resolved children in processing order.</summary>
    public System.Collections.Generic.List<SvgResolvedNode> Children;
    /// <summary>The computed style for an element, or <c>null</c>.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The independent computed viewport, font, root-font, and local object-bounding-box length context.</summary>
    public SvgLengthContext? LengthContext;
    /// <summary>The conditional-processing evaluation, or <c>null</c> for non-elements.</summary>
    public SvgConditionalResult? Conditional;
    /// <summary>The ordered switch selection, or <c>null</c>.</summary>
    public SvgSwitchResult? Switch;
    /// <summary>The established SVG viewport, or <c>null</c>.</summary>
    public SvgViewportResult? Viewport;
    /// <summary>The structural element result, or <c>null</c>.</summary>
    public SvgStructuralResult? Structural;
    /// <summary>The basic shape geometry, or <c>null</c>.</summary>
    public SvgShapeResult? Geometry;
    /// <summary>The use expansion, or <c>null</c>.</summary>
    public SvgUseResult? Use;
    /// <summary>The embedded-content result, or <c>null</c>.</summary>
    public SvgEmbeddedContentResult? EmbeddedContent;
    /// <summary>The paint-server resource result, or <c>null</c>.</summary>
    public SvgPaintServerResult? PaintServer;
    /// <summary>The marker, clipping-path, or mask result, or <c>null</c>.</summary>
    public SvgEffectResult? Effect;
    /// <summary>The filter resource result, or <c>null</c>.</summary>
    public SvgFilterResult? Filter;
    /// <summary>The local transform currently established by viewport or use resolution.</summary>
    public SvgMatrix Transform;
    /// <summary>The computed CSS transform list, or <c>null</c>.</summary>
    public SvgCssTransformList? ComputedTransform;
    /// <summary>The currently available local geometry bounds, or <c>null</c>.</summary>
    public SvgRect? Bounds;
    /// <summary>The computed fill paint, or <c>null</c>.</summary>
    public SvgPaint? Fill;
    /// <summary>The computed stroke paint, or <c>null</c>.</summary>
    public SvgPaint? Stroke;
    /// <summary>The bounded text payload for text or descriptive content.</summary>
    public string Text;
    /// <summary>Whether ancestor, switch, and conditional processing permit this node.</summary>
    public bool Active;
    /// <summary>Whether the currently implemented semantic stages permit a rendered contribution.</summary>
    public bool Renderable;
    /// <summary>Located diagnostics attributed to this source or instance.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether every stage attached to this node completed without an error.</summary>
    public bool Success;

    internal SvgResolvedNode(SvgResolvedDocument document, SvgResolvedNode? parent, SvgNode? source, string? sourcePath, string? instancePath, SvgUseInstance? instance)
    {
        Document = document;
        Parent = parent;
        Kind = SvgResolvedNodeKind.Unknown;
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        SourcePath = sourcePath == null ? "" : sourcePath;
        InstancePath = instancePath == null ? "" : instancePath;
        Instance = instance;
        Children = new System.Collections.Generic.List<SvgResolvedNode>();
        ComputedStyle = null;
        LengthContext = null;
        Conditional = null;
        Switch = null;
        Viewport = null;
        Structural = null;
        Geometry = null;
        Use = null;
        EmbeddedContent = null;
        PaintServer = null;
        Effect = null;
        Filter = null;
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        ComputedTransform = null;
        Bounds = null;
        Fill = null;
        Stroke = null;
        Text = "";
        Active = false;
        Renderable = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks every captured source and nested resolver version.</summary>
    /// <returns><c>true</c> when this resolved subtree can still be reused.</returns>
    internal bool IsCurrent()
    {
        if (SourceNode == null || SourceNode.Version != SourceVersion) return false;
        if (Instance != null && (Instance.SourceNode == null || Instance.SourceNode.Version != Instance.SourceVersion)) return false;
        if (ComputedStyle != null && (ComputedStyle.Node == null || ComputedStyle.Node.Version != ComputedStyle.SourceVersion)) return false;
        if (Conditional != null && !Conditional.IsCurrent()) return false;
        if (Switch != null && !Switch.IsCurrent()) return false;
        if (Viewport != null && (Viewport.Node == null || Viewport.Node.Version != Viewport.NodeVersion)) return false;
        if (Structural != null && (Structural.Node == null || Structural.Node.Version != Structural.NodeVersion)) return false;
        if (Geometry != null && (Geometry.Node == null || Geometry.Node.Version != Geometry.NodeVersion)) return false;
        if (Use != null && !Use.IsCurrent()) return false;
        if (EmbeddedContent != null && (EmbeddedContent.Node == null || EmbeddedContent.Node.Version != EmbeddedContent.NodeVersion)) return false;
        if (PaintServer != null && !PaintServer.IsCurrent()) return false;
        if (Effect != null && !Effect.IsCurrent()) return false;
        if (Filter != null && !Filter.IsCurrent()) return false;
        int index = 0;
        while (index < Children.Count)
        {
            if (Children[index] == null || !Children[index].IsCurrent()) return false;
            index = index + 1;
        }
        return true;
    }

    /// <summary>Finds the first descendant, including this node, with a source identity and optional instance path.</summary>
    /// <param name="source">The authored source node.</param>
    /// <param name="instancePath">The exact instance path, or an empty string for authored content.</param>
    /// <returns>The matching resolved node, or <c>null</c>.</returns>
    internal SvgResolvedNode? Find(SvgNode? source, string? instancePath)
    {
        if (source == null || instancePath == null) return null;
        if (object.ReferenceEquals(SourceNode, source) && InstancePath == instancePath) return this;
        int index = 0;
        while (index < Children.Count)
        {
            SvgResolvedNode? found = Children[index].Find(source, instancePath);
            if (found != null) return found;
            index = index + 1;
        }
        return null;
    }
}
