namespace svg;

/// <summary>Configures bounded gradient and pattern template resolution.</summary>
public class SvgPaintServerResolveOptions
{
    /// <summary>The typed attribute accessor.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The URL reference options used for template href values.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The reference depth and count limits.</summary>
    public SvgReferenceLimits Limits;
    /// <summary>The viewport and font inputs for user-space lengths.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The target object bounding box, or <c>null</c> when unavailable.</summary>
    public SvgRect? TargetBounds;
    /// <summary>The stylesheets used for paint-server and stop styles.</summary>
    public System.Collections.Generic.List<SvgStyleSheet> StyleSheets;
    /// <summary>The cascade and computed-style options.</summary>
    public SvgStyleResolverOptions StyleOptions;
    /// <summary>An optional precomputed style for the requested paint-server element.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The maximum number of direct effective gradient stops.</summary>
    public int MaxStops;

    /// <summary>Creates secure deterministic paint-server defaults.</summary>
    public SvgPaintServerResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ReferenceOptions = new SvgReferenceResolveOptions();
        Limits = new SvgReferenceLimits();
        LengthContext = new SvgLengthContext();
        TargetBounds = null;
        StyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleOptions = new SvgStyleResolverOptions();
        ComputedStyle = null;
        MaxStops = 100000;
    }
}

/// <summary>Records the source of one inherited or defaulted paint-server attribute.</summary>
public class SvgPaintServerAttribute
{
    /// <summary>The attribute local name.</summary>
    public string Name;
    /// <summary>The template element that supplied the value, or <c>null</c> for an initial value.</summary>
    public SvgNode? SourceNode;
    /// <summary>The authored attribute, or <c>null</c> for an initial value.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The typed effective value.</summary>
    public object? Value;
    /// <summary>Whether a template supplied the value.</summary>
    public bool Inherited;
    /// <summary>Whether the value was synthesized after the template chain was exhausted.</summary>
    public bool Defaulted;

    internal SvgPaintServerAttribute(string name, SvgNode? sourceNode, SvgAttribute? attribute, object? value, bool inherited, bool defaulted)
    {
        Name = name;
        SourceNode = sourceNode;
        Attribute = attribute;
        Value = value;
        Inherited = inherited;
        Defaulted = defaulted;
    }
}

/// <summary>Contains one normalized gradient stop and its computed color.</summary>
public class SvgGradientStop
{
    /// <summary>The corresponding source stop element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The authored numeric or percentage offset before clamping and ordering.</summary>
    public double AuthoredOffset;
    /// <summary>The clamped nondecreasing offset from zero through one.</summary>
    public double Offset;
    /// <summary>The computed stop color.</summary>
    public SvgColor? Color;
    /// <summary>The clamped stop opacity.</summary>
    public double Opacity;
    /// <summary>The product of color alpha and stop opacity.</summary>
    public double EffectiveAlpha;
    /// <summary>The computed stop style.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The offset attribute source span, or the stop source span.</summary>
    public SvgSourceSpan? Source;

    internal SvgGradientStop(SvgNode? source)
    {
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        AuthoredOffset = 0;
        Offset = 0;
        Color = null;
        Opacity = 1;
        EffectiveAlpha = 1;
        ComputedStyle = null;
        Source = source == null ? null : source.SourceSpan;
    }
}

/// <summary>Contains a resolved gradient or pattern with template provenance and target-space geometry.</summary>
public class SvgPaintServerResult
{
    /// <summary>The requested paint-server element.</summary>
    public SvgNode? Node;
    /// <summary>The source version captured during resolution.</summary>
    public long NodeVersion;
    /// <summary>The resolved paint-server kind.</summary>
    public SvgPaintServerKind Kind;
    /// <summary>The computed style of the requested paint-server element.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The traversed template elements starting with the requested element.</summary>
    public System.Collections.Generic.List<SvgNode> TemplateChain;
    /// <summary>The source versions corresponding to the template chain.</summary>
    public System.Collections.Generic.List<long> TemplateVersions;
    /// <summary>The resolved references in traversal order.</summary>
    public System.Collections.Generic.List<SvgReferenceResult> References;
    /// <summary>The effective inherited and defaulted attributes.</summary>
    public System.Collections.Generic.List<SvgPaintServerAttribute> Attributes;
    /// <summary>The template whose relevant children are used, or <c>null</c>.</summary>
    public SvgNode? ContentSource;
    /// <summary>The effective direct source children used for stops or pattern content.</summary>
    public System.Collections.Generic.List<SvgNode> Content;
    /// <summary>The gradient geometry units.</summary>
    public SvgPaintServerUnits GradientUnits;
    /// <summary>The pattern tile geometry units.</summary>
    public SvgPaintServerUnits PatternUnits;
    /// <summary>The pattern content geometry units.</summary>
    public SvgPaintServerUnits PatternContentUnits;
    /// <summary>The gradient spread method.</summary>
    public SvgGradientSpreadMethod SpreadMethod;
    /// <summary>The authored gradient or pattern transform before implicit unit conversion.</summary>
    public SvgMatrix ExplicitTransform;
    /// <summary>The complete paint-coordinate to target-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The complete pattern-content to target-user-space transform.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The local linear-gradient start point.</summary>
    public SvgPoint? Start;
    /// <summary>The local linear-gradient end point.</summary>
    public SvgPoint? End;
    /// <summary>The transformed linear-gradient start point.</summary>
    public SvgPoint? UserStart;
    /// <summary>The transformed linear-gradient end point.</summary>
    public SvgPoint? UserEnd;
    /// <summary>The local radial-gradient end-circle center.</summary>
    public SvgPoint? Center;
    /// <summary>The local radial-gradient focal-circle center.</summary>
    public SvgPoint? Focus;
    /// <summary>The local radial-gradient end radius.</summary>
    public double Radius;
    /// <summary>The local radial-gradient focal radius.</summary>
    public double FocalRadius;
    /// <summary>The transformed radial-gradient end-circle center.</summary>
    public SvgPoint? UserCenter;
    /// <summary>The transformed radial-gradient focal-circle center.</summary>
    public SvgPoint? UserFocus;
    /// <summary>The effective normalized gradient stops.</summary>
    public System.Collections.Generic.List<SvgGradientStop> Stops;
    /// <summary>The effective gradient paint behavior.</summary>
    public SvgGradientPaintKind GradientPaintKind;
    /// <summary>The effective solid color for a single-stop or degenerate gradient.</summary>
    public SvgColor? SolidColor;
    /// <summary>The effective alpha for a single-stop or degenerate gradient.</summary>
    public double SolidAlpha;
    /// <summary>The local pattern tile rectangle.</summary>
    public SvgRect? TileBounds;
    /// <summary>The axis-aligned target-user-space bounds of one transformed pattern tile.</summary>
    public SvgRect? UserTileBounds;
    /// <summary>The effective pattern view box, or <c>null</c>.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The effective pattern preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>Whether the paint server can produce paint for the supplied target geometry.</summary>
    public bool Renderable;
    /// <summary>Located parsing, reference, cycle, style, and geometry diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether resolution completed without error diagnostics.</summary>
    public bool Success;

    internal SvgPaintServerResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        Kind = SvgPaintServerKind.None;
        ComputedStyle = null;
        TemplateChain = new System.Collections.Generic.List<SvgNode>();
        TemplateVersions = new System.Collections.Generic.List<long>();
        References = new System.Collections.Generic.List<SvgReferenceResult>();
        Attributes = new System.Collections.Generic.List<SvgPaintServerAttribute>();
        ContentSource = null;
        Content = new System.Collections.Generic.List<SvgNode>();
        GradientUnits = SvgPaintServerUnits.ObjectBoundingBox;
        PatternUnits = SvgPaintServerUnits.ObjectBoundingBox;
        PatternContentUnits = SvgPaintServerUnits.UserSpaceOnUse;
        SpreadMethod = SvgGradientSpreadMethod.Pad;
        ExplicitTransform = Identity();
        Transform = Identity();
        ContentTransform = Identity();
        Start = null;
        End = null;
        UserStart = null;
        UserEnd = null;
        Center = null;
        Focus = null;
        Radius = 0;
        FocalRadius = 0;
        UserCenter = null;
        UserFocus = null;
        Stops = new System.Collections.Generic.List<SvgGradientStop>();
        GradientPaintKind = SvgGradientPaintKind.None;
        SolidColor = null;
        SolidAlpha = 0;
        TileBounds = null;
        UserTileBounds = null;
        ViewBox = null;
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        Renderable = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds effective attribute provenance by local name.</summary>
    /// <param name="name">The attribute local name.</param>
    /// <returns>The effective attribute, or <c>null</c>.</returns>
    internal SvgPaintServerAttribute? FindAttribute(string? name)
    {
        if (name == null) return null;
        int index = 0;
        while (index < Attributes.Count)
        {
            if (Attributes[index].Name == name) return Attributes[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Checks whether every traversed source node retains its captured version.</summary>
    /// <returns><c>true</c> when the resolved snapshot is current.</returns>
    internal bool IsCurrent()
    {
        if (Node == null || Node.Version != NodeVersion) return false;
        int index = 0;
        while (index < TemplateChain.Count)
        {
            SvgNode source = TemplateChain[index];
            if (source == null || index >= TemplateVersions.Count || source.Version != TemplateVersions[index]) return false;
            index = index + 1;
        }
        index = 0;
        while (index < Stops.Count)
        {
            if (Stops[index].SourceNode == null || (Stops[index].SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Version != Stops[index].SourceVersion) return false;
            index = index + 1;
        }
        return true;
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}
