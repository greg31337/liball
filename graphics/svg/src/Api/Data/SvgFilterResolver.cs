namespace svg;

/// <summary>Configures bounded filter template and primitive-graph resolution.</summary>
public class SvgFilterResolveOptions
{
    /// <summary>The typed attribute accessor.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The URL reference options used by filter href values.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The explicit resource policy and callback used by feImage.</summary>
    public SvgImageResolveOptions ImageOptions;
    /// <summary>The reference depth and count limits.</summary>
    public SvgReferenceLimits ReferenceLimits;
    /// <summary>The filter primitive and surface limits.</summary>
    public SvgFilterLimits FilterLimits;
    /// <summary>The referencing element's viewport and font length context.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The referencing element's object bounding box, or <c>null</c>.</summary>
    public SvgRect? TargetBounds;
    /// <summary>The stylesheets used for filter and primitive styles.</summary>
    public System.Collections.Generic.List<SvgStyleSheet> StyleSheets;
    /// <summary>The cascade and computed-style options.</summary>
    public SvgStyleResolverOptions StyleOptions;
    /// <summary>An optional precomputed style for the requested filter element.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The maximum characters retained in one result or input name.</summary>
    public int MaxNameCharacters;

    /// <summary>Creates deterministic bounded filter-resolution defaults.</summary>
    public SvgFilterResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ReferenceOptions = new SvgReferenceResolveOptions();
        ImageOptions = new SvgImageResolveOptions();
        ReferenceLimits = new SvgReferenceLimits();
        FilterLimits = new SvgFilterLimits();
        LengthContext = new SvgLengthContext();
        TargetBounds = null;
        StyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleOptions = new SvgStyleResolverOptions();
        ComputedStyle = null;
        MaxNameCharacters = 4096;
    }
}

/// <summary>Records the source of one inherited or defaulted filter attribute.</summary>
public class SvgFilterAttribute
{
    /// <summary>The attribute local name.</summary>
    public string Name;
    /// <summary>The filter element that supplied the value, or <c>null</c> for a default.</summary>
    public SvgNode? SourceNode;
    /// <summary>The authored attribute, or <c>null</c> for a default.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The typed effective value.</summary>
    public object? Value;
    /// <summary>Whether a referenced template supplied the value.</summary>
    public bool Inherited;
    /// <summary>Whether the element catalog supplied the value after traversal.</summary>
    public bool Defaulted;

    internal SvgFilterAttribute(string name, SvgNode? sourceNode, SvgAttribute? attribute, object? value, bool inherited, bool defaulted)
    {
        Name = name;
        SourceNode = sourceNode;
        Attribute = attribute;
        Value = value;
        Inherited = inherited;
        Defaulted = defaulted;
    }
}

/// <summary>Contains one resolved filter primitive input edge.</summary>
public class SvgFilterInput
{
    /// <summary>The source attribute name, such as <c>in</c> or <c>in2</c>.</summary>
    public string AttributeName;
    /// <summary>The authored input text, or an empty string for an implicit input.</summary>
    public string Name;
    /// <summary>The resolved input kind.</summary>
    public SvgFilterInputKind Kind;
    /// <summary>The standard input kind when applicable.</summary>
    public SvgFilterStandardInput StandardInput;
    /// <summary>The preceding primitive supplying this input, or <c>null</c>.</summary>
    public SvgFilterPrimitive? Primitive;
    /// <summary>Whether the selected conceptual input is available.</summary>
    public bool Available;
    /// <summary>The authored input source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;

    internal SvgFilterInput(string attributeName)
    {
        AttributeName = attributeName;
        Name = "";
        Kind = SvgFilterInputKind.TransparentBlack;
        StandardInput = SvgFilterStandardInput.SourceGraphic;
        Primitive = null;
        Available = true;
        Source = null;
    }
}

/// <summary>Contains one parsed typed parameter of a filter primitive.</summary>
public class SvgFilterPrimitiveAttribute
{
    /// <summary>The attribute local name.</summary>
    public string Name;
    /// <summary>The complete typed access result.</summary>
    public SvgTypedAttributeValue TypedValue;

    internal SvgFilterPrimitiveAttribute(string name, SvgTypedAttributeValue value)
    {
        Name = name;
        TypedValue = value;
    }
}

/// <summary>Contains one parsed merge-node, transfer-function, or lighting-source child.</summary>
public class SvgFilterPrimitiveChild
{
    /// <summary>The authored child element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The child source version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The child element kind.</summary>
    public SvgElementKind ElementKind;
    /// <summary>The parsed child attributes and their defaults.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitiveAttribute> Attributes;

    internal SvgFilterPrimitiveChild(SvgNode? source)
    {
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        ElementKind = source == null ? SvgElementKind.Unknown : source.ElementKind;
        Attributes = new System.Collections.Generic.List<SvgFilterPrimitiveAttribute>();
    }

    /// <summary>Finds a parsed child attribute by local name.</summary>
    /// <param name="name">The attribute local name.</param>
    /// <returns>The matching attribute, or <c>null</c>.</returns>
    internal SvgFilterPrimitiveAttribute? FindAttribute(string? name)
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
}

/// <summary>Contains one normalized filter primitive and its input graph edges.</summary>
public class SvgFilterPrimitive
{
    /// <summary>The effective source primitive element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The zero-based primitive order.</summary>
    public int Index;
    /// <summary>The primitive element kind.</summary>
    public SvgElementKind ElementKind;
    /// <summary>The computed primitive style.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The effective filter-operation color space.</summary>
    public SvgFilterColorSpace ColorSpace;
    /// <summary>The resolved input edges in attribute or merge-node order.</summary>
    public System.Collections.Generic.List<SvgFilterInput> Inputs;
    /// <summary>The parsed primitive parameters and common attributes.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitiveAttribute> Attributes;
    /// <summary>The parsed merge-node, transfer-function, or light-source children.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitiveChild> Children;
    /// <summary>The feImage href resolution result, or <c>null</c>.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The policy-mediated external feImage acquisition, or <c>null</c>.</summary>
    public SvgImageResolveResult? Image;
    /// <summary>The authored result name, or an empty string.</summary>
    public string ResultName;
    /// <summary>The primitive subregion in primitiveUnits coordinates.</summary>
    public SvgRect? Region;
    /// <summary>The primitive subregion in referencing user space.</summary>
    public SvgRect? UserRegion;
    /// <summary>Whether positive subregion dimensions allow a nonempty output.</summary>
    public bool Enabled;

    internal SvgFilterPrimitive(SvgNode? source, int index)
    {
        SourceNode = source;
        SourceVersion = source == null ? 0 : source.Version;
        Index = index;
        ElementKind = source == null ? SvgElementKind.Unknown : source.ElementKind;
        ComputedStyle = null;
        ColorSpace = SvgFilterColorSpace.LinearRgb;
        Inputs = new System.Collections.Generic.List<SvgFilterInput>();
        Attributes = new System.Collections.Generic.List<SvgFilterPrimitiveAttribute>();
        Children = new System.Collections.Generic.List<SvgFilterPrimitiveChild>();
        Reference = null;
        Image = null;
        ResultName = "";
        Region = null;
        UserRegion = null;
        Enabled = false;
    }

    /// <summary>Finds a parsed primitive attribute by local name.</summary>
    /// <param name="name">The attribute local name.</param>
    /// <returns>The matching attribute, or <c>null</c>.</returns>
    internal SvgFilterPrimitiveAttribute? FindAttribute(string? name)
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
}

/// <summary>Contains an inherited filter definition and its normalized primitive graph.</summary>
public class SvgFilterResult
{
    /// <summary>The requested filter element.</summary>
    public SvgNode? Node;
    /// <summary>The requested source version captured during resolution.</summary>
    public long NodeVersion;
    /// <summary>The computed style of the requested filter element.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The traversed filters beginning with the requested element.</summary>
    public System.Collections.Generic.List<SvgNode> TemplateChain;
    /// <summary>The source versions corresponding to the template chain.</summary>
    public System.Collections.Generic.List<long> TemplateVersions;
    /// <summary>The resolved filter-template and feImage href edges in discovery order.</summary>
    public System.Collections.Generic.List<SvgReferenceResult> References;
    /// <summary>The effective inherited and defaulted filter attributes.</summary>
    public System.Collections.Generic.List<SvgFilterAttribute> Attributes;
    /// <summary>The filter whose primitive children are effective, or <c>null</c>.</summary>
    public SvgNode? ContentSource;
    /// <summary>The filter region coordinate-system mode.</summary>
    public SvgPaintServerUnits FilterUnits;
    /// <summary>The primitive geometry coordinate-system mode.</summary>
    public SvgPaintServerUnits PrimitiveUnits;
    /// <summary>The filterUnits-to-user-space matrix.</summary>
    public SvgMatrix FilterTransform;
    /// <summary>The primitiveUnits-to-user-space matrix.</summary>
    public SvgMatrix PrimitiveTransform;
    /// <summary>The filter region in filterUnits coordinates.</summary>
    public SvgRect? Region;
    /// <summary>The filter region in referencing user space.</summary>
    public SvgRect? UserRegion;
    /// <summary>The ordered normalized primitive graph.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitive> Primitives;
    /// <summary>Whether the effective filter contains no primitives.</summary>
    public bool Empty;
    /// <summary>Whether the filter region and primitive graph can produce an effect.</summary>
    public bool Renderable;
    /// <summary>Located template, parsing, graph, style, geometry, and limit diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether resolution completed without error diagnostics.</summary>
    public bool Success;

    internal SvgFilterResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        ComputedStyle = null;
        TemplateChain = new System.Collections.Generic.List<SvgNode>();
        TemplateVersions = new System.Collections.Generic.List<long>();
        References = new System.Collections.Generic.List<SvgReferenceResult>();
        Attributes = new System.Collections.Generic.List<SvgFilterAttribute>();
        ContentSource = null;
        FilterUnits = SvgPaintServerUnits.ObjectBoundingBox;
        PrimitiveUnits = SvgPaintServerUnits.UserSpaceOnUse;
        FilterTransform = Identity();
        PrimitiveTransform = Identity();
        Region = null;
        UserRegion = null;
        Primitives = new System.Collections.Generic.List<SvgFilterPrimitive>();
        Empty = true;
        Renderable = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds an effective filter attribute by local name.</summary>
    /// <param name="name">The attribute local name.</param>
    /// <returns>The matching effective attribute, or <c>null</c>.</returns>
    internal SvgFilterAttribute? FindAttribute(string? name)
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

    /// <summary>Finds the closest primitive at or before an index with a result name.</summary>
    /// <param name="name">The exact result name.</param>
    /// <param name="beforeIndex">The exclusive primitive index bound.</param>
    /// <returns>The closest preceding named primitive, or <c>null</c>.</returns>
    internal SvgFilterPrimitive? FindResult(string? name, int beforeIndex)
    {
        if (name == null) return null;
        int index = beforeIndex - 1;
        if (index >= Primitives.Count) index = Primitives.Count - 1;
        while (index >= 0)
        {
            if (Primitives[index].ResultName == name) return Primitives[index];
            index = index - 1;
        }
        return null;
    }

    /// <summary>Checks whether every captured template and primitive version remains current.</summary>
    /// <returns><c>true</c> when the result can still be reused.</returns>
    internal bool IsCurrent()
    {
        if (Node == null || Node.Version != NodeVersion) return false;
        int index = 0;
        while (index < TemplateChain.Count)
        {
            if (TemplateChain[index] == null || TemplateChain[index].Version != TemplateVersions[index]) return false;
            index = index + 1;
        }
        index = 0;
        while (index < Primitives.Count)
        {
            if (Primitives[index].SourceNode == null || (Primitives[index].SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Version != Primitives[index].SourceVersion) return false;
            int childIndex = 0;
            while (childIndex < Primitives[index].Children.Count)
            {
                SvgFilterPrimitiveChild child = Primitives[index].Children[childIndex];
                if (child.SourceNode == null || child.SourceNode.Version != child.SourceVersion) return false;
                childIndex = childIndex + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}
