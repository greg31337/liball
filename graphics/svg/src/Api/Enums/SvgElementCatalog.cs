namespace svg;

/// <summary>Classifies the semantic role of a current SVG element.</summary>
public enum SvgElementCategory
{
    /// <summary>Document structure or grouping content.</summary>
    Structural = 0,
    /// <summary>An element that can contribute graphics.</summary>
    Graphics = 1,
    /// <summary>A geometric shape element.</summary>
    Shape = 2,
    /// <summary>A text layout or text-content element.</summary>
    Text = 3,
    /// <summary>Descriptive or metadata content.</summary>
    Descriptive = 4,
    /// <summary>Declarative animation content.</summary>
    Animation = 5,
    /// <summary>A paint server or paint-server child.</summary>
    PaintServer = 6,
    /// <summary>A filter container, primitive, function, or light source.</summary>
    Filter = 7,
    /// <summary>Embedded or host-provided content.</summary>
    Embedded = 8,
    /// <summary>Style or script content.</summary>
    Scripting = 9
}

/// <summary>Defines how an SVG element can contribute rendered output.</summary>
public enum SvgElementRenderMode
{
    /// <summary>The element never directly contributes rendered output.</summary>
    Never = 0,
    /// <summary>The element directly paints geometry or an image.</summary>
    Direct = 1,
    /// <summary>The element renders eligible descendants.</summary>
    Container = 2,
    /// <summary>The element participates in an SVG text layout tree.</summary>
    Text = 3,
    /// <summary>The element renders only in a referencing context.</summary>
    Referenced = 4,
    /// <summary>The element supplies a paint, filter, or other non-graphics resource.</summary>
    Resource = 5,
    /// <summary>The element requires a host-content callback to render.</summary>
    HostCallback = 6
}

/// <summary>Identifies how an absent element attribute obtains its default.</summary>
public enum SvgAttributeDefaultKind
{
    /// <summary>The absent attribute has no declared default.</summary>
    None = 0,
    /// <summary>The absent attribute uses a literal default value.</summary>
    Literal = 1,
    /// <summary>The absent attribute copies the named sibling attribute.</summary>
    OtherAttribute = 2,
    /// <summary>The default depends on document or rendering context.</summary>
    ContextDependent = 3
}
