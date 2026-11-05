namespace svg;

/// <summary>Configures bounded expansion of one SVG <c>use</c> element.</summary>
public class SvgUseResolveOptions
{
    /// <summary>The typed attribute accessor used for geometry and viewport attributes.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The containing viewport and font context.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The general URL-reference options.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The reference depth, count, and instance limits.</summary>
    public SvgReferenceLimits Limits;
    /// <summary>The stylesheets that apply to the host and referenced tree.</summary>
    public System.Collections.Generic.List<SvgStyleSheet> StyleSheets;
    /// <summary>The cascade and computed-style options.</summary>
    public SvgStyleResolverOptions StyleOptions;
    /// <summary>An optional precomputed style for the outer <c>use</c> element.</summary>
    public SvgComputedStyle? ComputedStyle;

    /// <summary>Creates deterministic secure expansion defaults.</summary>
    public SvgUseResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        LengthContext = new SvgLengthContext();
        ReferenceOptions = new SvgReferenceResolveOptions();
        Limits = new SvgReferenceLimits();
        StyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleOptions = new SvgStyleResolverOptions();
        ComputedStyle = null;
    }
}

/// <summary>Contains the paint supplied by the host of a use-element shadow tree.</summary>
public class SvgUsePaintContext
{
    /// <summary>The computed style of the shadow host.</summary>
    public SvgComputedStyle? HostStyle;
    /// <summary>The host fill after resolving an enclosing context paint.</summary>
    public SvgPaint? Fill;
    /// <summary>The host stroke after resolving an enclosing context paint.</summary>
    public SvgPaint? Stroke;

    internal SvgUsePaintContext(SvgComputedStyle? hostStyle, SvgPaint? fill, SvgPaint? stroke)
    {
        HostStyle = hostStyle;
        Fill = fill;
        Stroke = stroke;
    }

    /// <summary>Resolves a context paint against this shadow host.</summary>
    /// <param name="paint">The computed paint to resolve.</param>
    /// <returns>The host paint for a context keyword, otherwise the supplied paint.</returns>
    internal SvgPaint? Resolve(SvgPaint? paint)
    {
        if (paint == null) return null;
        if (paint.Kind == SvgPaintKind.ContextFill) return Fill;
        if (paint.Kind == SvgPaintKind.ContextStroke) return Stroke;
        return paint;
    }
}

/// <summary>Describes the viewport established by a referenced <c>symbol</c> or <c>svg</c>.</summary>
public class SvgUseViewport
{
    /// <summary>The referenced element that establishes the viewport.</summary>
    public SvgNode SourceNode;
    /// <summary>Whether the viewport is generated from a <c>symbol</c>.</summary>
    public bool Symbol;
    /// <summary>The viewport x coordinate before the host translation.</summary>
    public double X;
    /// <summary>The viewport y coordinate before the host translation.</summary>
    public double Y;
    /// <summary>The used viewport width.</summary>
    public double Width;
    /// <summary>The used viewport height.</summary>
    public double Height;
    /// <summary>Whether the host supplied a non-auto width.</summary>
    public bool WidthOverridden;
    /// <summary>Whether the host supplied a non-auto height.</summary>
    public bool HeightOverridden;
    /// <summary>The viewport bounds after the host translation.</summary>
    public SvgRect? Bounds;
    /// <summary>The referenced view box, or <c>null</c>.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The referenced preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>The mapping from referenced child user space to the containing use coordinate system.</summary>
    public SvgMatrix Transform;
    /// <summary>The child length context established by the used viewport.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>Whether the viewport clips overflowing graphics.</summary>
    public bool ClipsOverflow;
    /// <summary>The viewport clip after the host translation, or <c>null</c>.</summary>
    public SvgRect? ClipRect;
    /// <summary>Whether nonzero viewport and view-box dimensions permit rendering.</summary>
    public bool Renderable;

    internal SvgUseViewport(SvgNode source)
    {
        SourceNode = source;
        Symbol = source != null && source.ElementKind == SvgElementKind.Symbol;
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        WidthOverridden = false;
        HeightOverridden = false;
        Bounds = null;
        ViewBox = null;
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        LengthContext = new SvgLengthContext();
        ClipsOverflow = true;
        ClipRect = null;
        Renderable = false;
    }
}

/// <summary>Maps one generated instance identity to its corresponding authored source.</summary>
public class SvgUseSourceMapEntry
{
    /// <summary>The stable path within this expansion.</summary>
    public string InstancePath;
    /// <summary>The corresponding source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The corresponding source version captured during expansion.</summary>
    public long SourceVersion;
    /// <summary>The corresponding source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The source and nested-resource provenance from inner to outer source.</summary>
    public System.Collections.Generic.List<SvgSourceSpan> Provenance;
    /// <summary>The outer authored <c>use</c> element responsible for the instance.</summary>
    public SvgNode CorrespondingUseElement;
    /// <summary>The source <c>use</c> whose shadow root directly contains the instance.</summary>
    public SvgNode ShadowHostSource;

    internal SvgUseSourceMapEntry(string path, SvgNode? source, SvgNode correspondingUse, SvgNode shadowHost)
    {
        InstancePath = path;
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        Source = source == null ? null : source.SourceSpan;
        Provenance = new System.Collections.Generic.List<SvgSourceSpan>();
        if (Source != null)
        {
            System.Collections.Generic.List<SvgSourceSpan> spans = new SvgSourceMapping().GetProvenance(Source);
            int index = 0;
            while (index < spans.Count)
            {
                Provenance.Add(spans[index]);
                index = index + 1;
            }
        }
        CorrespondingUseElement = correspondingUse;
        ShadowHostSource = shadowHost;
    }
}

/// <summary>Represents one source-linked node in a use-element shadow expansion.</summary>
public class SvgUseInstance
{
    /// <summary>The stable path within this expansion.</summary>
    public string InstancePath;
    /// <summary>The corresponding source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The corresponding source version captured during expansion.</summary>
    public long SourceVersion;
    /// <summary>The parent instance, or <c>null</c> for the outer instance root.</summary>
    public SvgUseInstance? Parent;
    /// <summary>The ordinary cloned child instances in source order.</summary>
    public System.Collections.Generic.List<SvgUseInstance> Children;
    /// <summary>The nested shadow root generated for a corresponding <c>use</c>, or <c>null</c>.</summary>
    public SvgUseInstance? ShadowRoot;
    /// <summary>Whether this instance is the root of a use-element shadow tree.</summary>
    public bool IsInstanceRoot;
    /// <summary>The outer authored <c>use</c> responsible for this instance.</summary>
    public SvgNode CorrespondingUseElement;
    /// <summary>The source <c>use</c> whose shadow root directly contains this instance.</summary>
    public SvgNode ShadowHostSource;
    /// <summary>The computed style using the instance parent for inheritance.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The host context used by <c>context-fill</c> and <c>context-stroke</c>.</summary>
    public SvgUsePaintContext? PaintContext;
    /// <summary>The containing or newly established length context.</summary>
    public SvgLengthContext? LengthContext;
    /// <summary>The local translation and optional viewport mapping for an instance root.</summary>
    public SvgMatrix Transform;
    /// <summary>The viewport established by an instance root, or <c>null</c>.</summary>
    public SvgUseViewport? Viewport;
    /// <summary>The reference resolved for a source <c>use</c>, or <c>null</c>.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>Whether this instance participates in rendering.</summary>
    public bool Renderable;

    internal SvgUseInstance(string path, SvgNode? source, SvgUseInstance? parent, bool root, SvgNode correspondingUse, SvgNode shadowHost)
    {
        InstancePath = path;
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        Parent = parent;
        Children = new System.Collections.Generic.List<SvgUseInstance>();
        ShadowRoot = null;
        IsInstanceRoot = root;
        CorrespondingUseElement = correspondingUse;
        ShadowHostSource = shadowHost;
        ComputedStyle = null;
        PaintContext = null;
        LengthContext = null;
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Viewport = null;
        Reference = null;
        Renderable = true;
    }
}

/// <summary>Contains one bounded source-linked use-element shadow expansion.</summary>
public class SvgUseResult
{
    /// <summary>The outer authored <c>use</c> element.</summary>
    public SvgNode? UseElement;
    /// <summary>The outer element version captured during expansion.</summary>
    public long UseVersion;
    /// <summary>The selected outer reference.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The outer shadow-tree instance root, or <c>null</c>.</summary>
    public SvgUseInstance? Root;
    /// <summary>All generated instances in deterministic discovery order.</summary>
    public System.Collections.Generic.List<SvgUseInstance> Instances;
    /// <summary>The source map in the same order as the instance list.</summary>
    public System.Collections.Generic.List<SvgUseSourceMapEntry> SourceMap;
    /// <summary>The outer host computed style.</summary>
    public SvgComputedStyle? UseStyle;
    /// <summary>The outer host context paint.</summary>
    public SvgUsePaintContext? PaintContext;
    /// <summary>The outer host x translation.</summary>
    public double X;
    /// <summary>The outer host y translation.</summary>
    public double Y;
    /// <summary>The outer instance transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The outer symbol or svg viewport, or <c>null</c>.</summary>
    public SvgUseViewport? Viewport;
    /// <summary>Located expansion, reference, style, and sizing diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether expansion completed without an error diagnostic.</summary>
    public bool Success;

    internal SvgUseResult(SvgNode? use)
    {
        UseElement = use;
        UseVersion = use == null ? 0 : use.Version;
        Reference = null;
        Root = null;
        Instances = new System.Collections.Generic.List<SvgUseInstance>();
        SourceMap = new System.Collections.Generic.List<SvgUseSourceMapEntry>();
        UseStyle = null;
        PaintContext = null;
        X = 0;
        Y = 0;
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Viewport = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds an instance by its stable path.</summary>
    /// <param name="instancePath">The path to locate.</param>
    /// <returns>The matching instance, or <c>null</c>.</returns>
    internal SvgUseInstance? FindInstance(string? instancePath)
    {
        if (instancePath == null) return null;
        int index = 0;
        while (index < Instances.Count)
        {
            if (Instances[index].InstancePath == instancePath) return Instances[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds a source-map entry by its stable instance path.</summary>
    /// <param name="instancePath">The path to locate.</param>
    /// <returns>The matching source entry, or <c>null</c>.</returns>
    internal SvgUseSourceMapEntry? FindSource(string? instancePath)
    {
        if (instancePath == null) return null;
        int index = 0;
        while (index < SourceMap.Count)
        {
            if (SourceMap[index].InstancePath == instancePath) return SourceMap[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Checks whether every source identity still has its captured version.</summary>
    /// <returns><c>true</c> when the expansion is current.</returns>
    internal bool IsCurrent()
    {
        if (UseElement == null || UseElement.Version != UseVersion) return false;
        int index = 0;
        while (index < SourceMap.Count)
        {
            SvgUseSourceMapEntry entry = SourceMap[index];
            if (entry.SourceNode == null || entry.SourceNode.Version != entry.SourceVersion) return false;
            index = index + 1;
        }
        return true;
    }
}
