namespace svg;

/// <summary>Identifies recognized SVG 2 and preserved SVG 1.1 element names.</summary>
public enum SvgElementKind
{
    /// <summary>An element outside the recognized SVG name tables.</summary>
    Unknown = 0,
    /// <summary>The <c>a</c> element.</summary>
    A = 1,
    /// <summary>The <c>animate</c> element.</summary>
    Animate = 2,
    /// <summary>The <c>animateMotion</c> element.</summary>
    AnimateMotion = 3,
    /// <summary>The <c>animateTransform</c> element.</summary>
    AnimateTransform = 4,
    /// <summary>The <c>audio</c> element.</summary>
    Audio = 5,
    /// <summary>The <c>canvas</c> element.</summary>
    Canvas = 6,
    /// <summary>The <c>circle</c> element.</summary>
    Circle = 7,
    /// <summary>The <c>clipPath</c> element.</summary>
    ClipPath = 8,
    /// <summary>The <c>defs</c> element.</summary>
    Defs = 9,
    /// <summary>The <c>desc</c> element.</summary>
    Desc = 10,
    /// <summary>The <c>discard</c> element.</summary>
    Discard = 11,
    /// <summary>The <c>ellipse</c> element.</summary>
    Ellipse = 12,
    /// <summary>The <c>feBlend</c> element.</summary>
    FeBlend = 13,
    /// <summary>The <c>feColorMatrix</c> element.</summary>
    FeColorMatrix = 14,
    /// <summary>The <c>feComponentTransfer</c> element.</summary>
    FeComponentTransfer = 15,
    /// <summary>The <c>feComposite</c> element.</summary>
    FeComposite = 16,
    /// <summary>The <c>feConvolveMatrix</c> element.</summary>
    FeConvolveMatrix = 17,
    /// <summary>The <c>feDiffuseLighting</c> element.</summary>
    FeDiffuseLighting = 18,
    /// <summary>The <c>feDisplacementMap</c> element.</summary>
    FeDisplacementMap = 19,
    /// <summary>The <c>feDistantLight</c> element.</summary>
    FeDistantLight = 20,
    /// <summary>The <c>feDropShadow</c> element.</summary>
    FeDropShadow = 21,
    /// <summary>The <c>feFlood</c> element.</summary>
    FeFlood = 22,
    /// <summary>The <c>feFuncA</c> element.</summary>
    FeFuncA = 23,
    /// <summary>The <c>feFuncB</c> element.</summary>
    FeFuncB = 24,
    /// <summary>The <c>feFuncG</c> element.</summary>
    FeFuncG = 25,
    /// <summary>The <c>feFuncR</c> element.</summary>
    FeFuncR = 26,
    /// <summary>The <c>feGaussianBlur</c> element.</summary>
    FeGaussianBlur = 27,
    /// <summary>The <c>feImage</c> element.</summary>
    FeImage = 28,
    /// <summary>The <c>feMerge</c> element.</summary>
    FeMerge = 29,
    /// <summary>The <c>feMergeNode</c> element.</summary>
    FeMergeNode = 30,
    /// <summary>The <c>feMorphology</c> element.</summary>
    FeMorphology = 31,
    /// <summary>The <c>feOffset</c> element.</summary>
    FeOffset = 32,
    /// <summary>The <c>fePointLight</c> element.</summary>
    FePointLight = 33,
    /// <summary>The <c>feSpecularLighting</c> element.</summary>
    FeSpecularLighting = 34,
    /// <summary>The <c>feSpotLight</c> element.</summary>
    FeSpotLight = 35,
    /// <summary>The <c>feTile</c> element.</summary>
    FeTile = 36,
    /// <summary>The <c>feTurbulence</c> element.</summary>
    FeTurbulence = 37,
    /// <summary>The <c>filter</c> element.</summary>
    Filter = 38,
    /// <summary>The <c>foreignObject</c> element.</summary>
    ForeignObject = 39,
    /// <summary>The <c>g</c> element.</summary>
    G = 40,
    /// <summary>The <c>iframe</c> element.</summary>
    Iframe = 41,
    /// <summary>The <c>image</c> element.</summary>
    Image = 42,
    /// <summary>The <c>line</c> element.</summary>
    Line = 43,
    /// <summary>The <c>linearGradient</c> element.</summary>
    LinearGradient = 44,
    /// <summary>The <c>marker</c> element.</summary>
    Marker = 45,
    /// <summary>The <c>mask</c> element.</summary>
    Mask = 46,
    /// <summary>The <c>metadata</c> element.</summary>
    Metadata = 47,
    /// <summary>The <c>mpath</c> element.</summary>
    MPath = 48,
    /// <summary>The <c>path</c> element.</summary>
    Path = 49,
    /// <summary>The <c>pattern</c> element.</summary>
    Pattern = 50,
    /// <summary>The <c>polygon</c> element.</summary>
    Polygon = 51,
    /// <summary>The <c>polyline</c> element.</summary>
    Polyline = 52,
    /// <summary>The <c>radialGradient</c> element.</summary>
    RadialGradient = 53,
    /// <summary>The <c>rect</c> element.</summary>
    Rect = 54,
    /// <summary>The <c>script</c> element.</summary>
    Script = 55,
    /// <summary>The <c>set</c> element.</summary>
    Set = 56,
    /// <summary>The <c>stop</c> element.</summary>
    Stop = 57,
    /// <summary>The <c>style</c> element.</summary>
    Style = 58,
    /// <summary>The <c>svg</c> element.</summary>
    Svg = 59,
    /// <summary>The <c>switch</c> element.</summary>
    Switch = 60,
    /// <summary>The <c>symbol</c> element.</summary>
    Symbol = 61,
    /// <summary>The <c>text</c> element.</summary>
    Text = 62,
    /// <summary>The <c>textPath</c> element.</summary>
    TextPath = 63,
    /// <summary>The <c>title</c> element.</summary>
    Title = 64,
    /// <summary>The <c>tspan</c> element.</summary>
    TSpan = 65,
    /// <summary>The <c>use</c> element.</summary>
    Use = 66,
    /// <summary>The <c>video</c> element.</summary>
    Video = 67,
    /// <summary>The <c>view</c> element.</summary>
    View = 68,
    /// <summary>The legacy SVG 1.1 <c>altGlyph</c> element.</summary>
    AltGlyph = 69,
    /// <summary>The legacy SVG 1.1 <c>altGlyphDef</c> element.</summary>
    AltGlyphDef = 70,
    /// <summary>The legacy SVG 1.1 <c>altGlyphItem</c> element.</summary>
    AltGlyphItem = 71,
    /// <summary>The legacy SVG 1.1 <c>animateColor</c> element.</summary>
    AnimateColor = 72,
    /// <summary>The legacy SVG 1.1 <c>color-profile</c> element.</summary>
    ColorProfile = 73,
    /// <summary>The legacy SVG 1.1 <c>cursor</c> element.</summary>
    Cursor = 74,
    /// <summary>The legacy SVG 1.1 <c>font</c> element.</summary>
    Font = 75,
    /// <summary>The legacy SVG 1.1 <c>font-face</c> element.</summary>
    FontFace = 76,
    /// <summary>The legacy SVG 1.1 <c>font-face-format</c> element.</summary>
    FontFaceFormat = 77,
    /// <summary>The legacy SVG 1.1 <c>font-face-name</c> element.</summary>
    FontFaceName = 78,
    /// <summary>The legacy SVG 1.1 <c>font-face-src</c> element.</summary>
    FontFaceSrc = 79,
    /// <summary>The legacy SVG 1.1 <c>font-face-uri</c> element.</summary>
    FontFaceUri = 80,
    /// <summary>The legacy SVG 1.1 <c>glyph</c> element.</summary>
    Glyph = 81,
    /// <summary>The legacy SVG 1.1 <c>glyphRef</c> element.</summary>
    GlyphRef = 82,
    /// <summary>The legacy SVG 1.1 <c>hkern</c> element.</summary>
    HKern = 83,
    /// <summary>The legacy SVG 1.1 <c>missing-glyph</c> element.</summary>
    MissingGlyph = 84,
    /// <summary>The legacy SVG 1.1 <c>tref</c> element.</summary>
    TRef = 85,
    /// <summary>The legacy SVG 1.1 <c>vkern</c> element.</summary>
    VKern = 86
}

/// <summary>Classifies recognized SVG and related namespace-aware attributes.</summary>
public enum SvgAttributeKind
{
    /// <summary>An attribute outside the recognized name and namespace tables.</summary>
    Unknown = 0,
    /// <summary>An SVG 2 regular, presentation, geometry, or event attribute.</summary>
    Svg2 = 1,
    /// <summary>An SVG 1.1 compatibility attribute removed from SVG 2.</summary>
    Svg11Legacy = 2,
    /// <summary>An attribute in the XLink namespace.</summary>
    XLink = 3,
    /// <summary>An attribute in the XML namespace.</summary>
    Xml = 4,
    /// <summary>A WAI-ARIA attribute or the unnamespaced <c>role</c> attribute.</summary>
    Aria = 5,
    /// <summary>An unnamespaced custom <c>data-*</c> attribute.</summary>
    Data = 6,
    /// <summary>An XML namespace declaration.</summary>
    NamespaceDeclaration = 7
}
