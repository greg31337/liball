namespace svg;

/// <summary>Defines namespace URI constants used by SVG documents.</summary>
public static class SvgNamespaces
{
    /// <summary>The SVG namespace URI.</summary>
    public const string Svg = "http://www.w3.org/2000/svg";

    /// <summary>The legacy XLink namespace URI.</summary>
    public const string XLink = "http://www.w3.org/1999/xlink";

    /// <summary>The XML namespace URI.</summary>
    public const string Xml = "http://www.w3.org/XML/1998/namespace";

    /// <summary>The XML namespace-declaration namespace URI.</summary>
    public const string Xmlns = "http://www.w3.org/2000/xmlns/";

    /// <summary>The XHTML namespace URI used by common foreign content.</summary>
    public const string XHtml = "http://www.w3.org/1999/xhtml";

    /// <summary>The MathML namespace URI used by common foreign content.</summary>
    public const string MathMl = "http://www.w3.org/1998/Math/MathML";
}

/// <summary>Defines media type constants recognized by SVG loading and resources.</summary>
public static class SvgMimeTypes
{
    /// <summary>The SVG image media type.</summary>
    public const string Svg = "image/svg+xml";

    /// <summary>The PNG image media type.</summary>
    public const string Png = "image/png";

    /// <summary>The JPEG image media type.</summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>The GIF image media type.</summary>
    public const string Gif = "image/gif";

    /// <summary>The WebP image media type.</summary>
    public const string WebP = "image/webp";

    /// <summary>The AVIF image media type.</summary>
    public const string Avif = "image/avif";

    /// <summary>The XML application media type.</summary>
    public const string Xml = "application/xml";

    /// <summary>The XML text media type.</summary>
    public const string XmlText = "text/xml";

    /// <summary>The CSS media type.</summary>
    public const string Css = "text/css";

    /// <summary>The TrueType font media type.</summary>
    public const string TrueType = "font/ttf";

    /// <summary>The OpenType font media type.</summary>
    public const string OpenType = "font/otf";

    /// <summary>The WOFF font media type.</summary>
    public const string Woff = "font/woff";

    /// <summary>The WOFF2 font media type.</summary>
    public const string Woff2 = "font/woff2";

    /// <summary>The gzip container media type.</summary>
    public const string Gzip = "application/gzip";

    /// <summary>The generic binary media type.</summary>
    public const string Binary = "application/octet-stream";
}

/// <summary>Defines case-sensitive local names for recognized SVG elements.</summary>
public static class SvgElementNames
{
    /// <summary>The <c>a</c> element name.</summary>
    public const string A = "a";
    /// <summary>The <c>animate</c> element name.</summary>
    public const string Animate = "animate";
    /// <summary>The <c>animateMotion</c> element name.</summary>
    public const string AnimateMotion = "animateMotion";
    /// <summary>The <c>animateTransform</c> element name.</summary>
    public const string AnimateTransform = "animateTransform";
    /// <summary>The <c>audio</c> element name.</summary>
    public const string Audio = "audio";
    /// <summary>The <c>canvas</c> element name.</summary>
    public const string Canvas = "canvas";
    /// <summary>The <c>circle</c> element name.</summary>
    public const string Circle = "circle";
    /// <summary>The <c>clipPath</c> element name.</summary>
    public const string ClipPath = "clipPath";
    /// <summary>The <c>defs</c> element name.</summary>
    public const string Defs = "defs";
    /// <summary>The <c>desc</c> element name.</summary>
    public const string Desc = "desc";
    /// <summary>The <c>discard</c> element name.</summary>
    public const string Discard = "discard";
    /// <summary>The <c>ellipse</c> element name.</summary>
    public const string Ellipse = "ellipse";
    /// <summary>The <c>feBlend</c> element name.</summary>
    public const string FeBlend = "feBlend";
    /// <summary>The <c>feColorMatrix</c> element name.</summary>
    public const string FeColorMatrix = "feColorMatrix";
    /// <summary>The <c>feComponentTransfer</c> element name.</summary>
    public const string FeComponentTransfer = "feComponentTransfer";
    /// <summary>The <c>feComposite</c> element name.</summary>
    public const string FeComposite = "feComposite";
    /// <summary>The <c>feConvolveMatrix</c> element name.</summary>
    public const string FeConvolveMatrix = "feConvolveMatrix";
    /// <summary>The <c>feDiffuseLighting</c> element name.</summary>
    public const string FeDiffuseLighting = "feDiffuseLighting";
    /// <summary>The <c>feDisplacementMap</c> element name.</summary>
    public const string FeDisplacementMap = "feDisplacementMap";
    /// <summary>The <c>feDistantLight</c> element name.</summary>
    public const string FeDistantLight = "feDistantLight";
    /// <summary>The <c>feDropShadow</c> element name.</summary>
    public const string FeDropShadow = "feDropShadow";
    /// <summary>The <c>feFlood</c> element name.</summary>
    public const string FeFlood = "feFlood";
    /// <summary>The <c>feFuncA</c> element name.</summary>
    public const string FeFuncA = "feFuncA";
    /// <summary>The <c>feFuncB</c> element name.</summary>
    public const string FeFuncB = "feFuncB";
    /// <summary>The <c>feFuncG</c> element name.</summary>
    public const string FeFuncG = "feFuncG";
    /// <summary>The <c>feFuncR</c> element name.</summary>
    public const string FeFuncR = "feFuncR";
    /// <summary>The <c>feGaussianBlur</c> element name.</summary>
    public const string FeGaussianBlur = "feGaussianBlur";
    /// <summary>The <c>feImage</c> element name.</summary>
    public const string FeImage = "feImage";
    /// <summary>The <c>feMerge</c> element name.</summary>
    public const string FeMerge = "feMerge";
    /// <summary>The <c>feMergeNode</c> element name.</summary>
    public const string FeMergeNode = "feMergeNode";
    /// <summary>The <c>feMorphology</c> element name.</summary>
    public const string FeMorphology = "feMorphology";
    /// <summary>The <c>feOffset</c> element name.</summary>
    public const string FeOffset = "feOffset";
    /// <summary>The <c>fePointLight</c> element name.</summary>
    public const string FePointLight = "fePointLight";
    /// <summary>The <c>feSpecularLighting</c> element name.</summary>
    public const string FeSpecularLighting = "feSpecularLighting";
    /// <summary>The <c>feSpotLight</c> element name.</summary>
    public const string FeSpotLight = "feSpotLight";
    /// <summary>The <c>feTile</c> element name.</summary>
    public const string FeTile = "feTile";
    /// <summary>The <c>feTurbulence</c> element name.</summary>
    public const string FeTurbulence = "feTurbulence";
    /// <summary>The <c>filter</c> element name.</summary>
    public const string Filter = "filter";
    /// <summary>The <c>foreignObject</c> element name.</summary>
    public const string ForeignObject = "foreignObject";
    /// <summary>The <c>g</c> element name.</summary>
    public const string G = "g";
    /// <summary>The <c>iframe</c> element name.</summary>
    public const string Iframe = "iframe";
    /// <summary>The <c>image</c> element name.</summary>
    public const string Image = "image";
    /// <summary>The <c>line</c> element name.</summary>
    public const string Line = "line";
    /// <summary>The <c>linearGradient</c> element name.</summary>
    public const string LinearGradient = "linearGradient";
    /// <summary>The <c>marker</c> element name.</summary>
    public const string Marker = "marker";
    /// <summary>The <c>mask</c> element name.</summary>
    public const string Mask = "mask";
    /// <summary>The <c>metadata</c> element name.</summary>
    public const string Metadata = "metadata";
    /// <summary>The <c>mpath</c> element name.</summary>
    public const string MPath = "mpath";
    /// <summary>The <c>path</c> element name.</summary>
    public const string Path = "path";
    /// <summary>The <c>pattern</c> element name.</summary>
    public const string Pattern = "pattern";
    /// <summary>The <c>polygon</c> element name.</summary>
    public const string Polygon = "polygon";
    /// <summary>The <c>polyline</c> element name.</summary>
    public const string Polyline = "polyline";
    /// <summary>The <c>radialGradient</c> element name.</summary>
    public const string RadialGradient = "radialGradient";
    /// <summary>The <c>rect</c> element name.</summary>
    public const string Rect = "rect";
    /// <summary>The <c>script</c> element name.</summary>
    public const string Script = "script";
    /// <summary>The <c>set</c> element name.</summary>
    public const string Set = "set";
    /// <summary>The <c>stop</c> element name.</summary>
    public const string Stop = "stop";
    /// <summary>The <c>style</c> element name.</summary>
    public const string Style = "style";
    /// <summary>The <c>svg</c> element name.</summary>
    public const string Svg = "svg";
    /// <summary>The <c>switch</c> element name.</summary>
    public const string Switch = "switch";
    /// <summary>The <c>symbol</c> element name.</summary>
    public const string Symbol = "symbol";
    /// <summary>The <c>text</c> element name.</summary>
    public const string Text = "text";
    /// <summary>The <c>textPath</c> element name.</summary>
    public const string TextPath = "textPath";
    /// <summary>The <c>title</c> element name.</summary>
    public const string Title = "title";
    /// <summary>The <c>tspan</c> element name.</summary>
    public const string TSpan = "tspan";
    /// <summary>The <c>use</c> element name.</summary>
    public const string Use = "use";
    /// <summary>The <c>video</c> element name.</summary>
    public const string Video = "video";
    /// <summary>The <c>view</c> element name.</summary>
    public const string View = "view";

    /// <summary>The legacy <c>altGlyph</c> element name.</summary>
    public const string AltGlyph = "altGlyph";
    /// <summary>The legacy <c>altGlyphDef</c> element name.</summary>
    public const string AltGlyphDef = "altGlyphDef";
    /// <summary>The legacy <c>altGlyphItem</c> element name.</summary>
    public const string AltGlyphItem = "altGlyphItem";
    /// <summary>The legacy <c>animateColor</c> element name.</summary>
    public const string AnimateColor = "animateColor";
    /// <summary>The legacy <c>color-profile</c> element name.</summary>
    public const string ColorProfile = "color-profile";
    /// <summary>The legacy <c>cursor</c> element name.</summary>
    public const string Cursor = "cursor";
    /// <summary>The legacy <c>font</c> element name.</summary>
    public const string Font = "font";
    /// <summary>The legacy <c>font-face</c> element name.</summary>
    public const string FontFace = "font-face";
    /// <summary>The legacy <c>font-face-format</c> element name.</summary>
    public const string FontFaceFormat = "font-face-format";
    /// <summary>The legacy <c>font-face-name</c> element name.</summary>
    public const string FontFaceName = "font-face-name";
    /// <summary>The legacy <c>font-face-src</c> element name.</summary>
    public const string FontFaceSrc = "font-face-src";
    /// <summary>The legacy <c>font-face-uri</c> element name.</summary>
    public const string FontFaceUri = "font-face-uri";
    /// <summary>The legacy <c>glyph</c> element name.</summary>
    public const string Glyph = "glyph";
    /// <summary>The legacy <c>glyphRef</c> element name.</summary>
    public const string GlyphRef = "glyphRef";
    /// <summary>The legacy <c>hkern</c> element name.</summary>
    public const string HKern = "hkern";
    /// <summary>The legacy <c>missing-glyph</c> element name.</summary>
    public const string MissingGlyph = "missing-glyph";
    /// <summary>The legacy <c>tref</c> element name.</summary>
    public const string TRef = "tref";
    /// <summary>The legacy <c>vkern</c> element name.</summary>
    public const string VKern = "vkern";
}

/// <summary>Defines case-sensitive local names for recognized SVG attributes.</summary>
public static class SvgAttributeNames
{
    /// <summary>The <c>id</c> attribute name.</summary>
    public const string Id = "id";
    /// <summary>The <c>class</c> attribute name.</summary>
    public const string Class = "class";
    /// <summary>The <c>style</c> attribute name.</summary>
    public const string Style = "style";
    /// <summary>The <c>tabindex</c> attribute name.</summary>
    public const string TabIndex = "tabindex";
    /// <summary>The <c>lang</c> attribute local name.</summary>
    public const string Lang = "lang";
    /// <summary>The <c>space</c> attribute local name in the XML namespace.</summary>
    public const string Space = "space";
    /// <summary>The <c>base</c> attribute local name in the XML namespace.</summary>
    public const string Base = "base";
    /// <summary>The <c>href</c> attribute name.</summary>
    public const string Href = "href";
    /// <summary>The <c>transform</c> attribute name.</summary>
    public const string Transform = "transform";
    /// <summary>The <c>viewBox</c> attribute name.</summary>
    public const string ViewBox = "viewBox";
    /// <summary>The <c>preserveAspectRatio</c> attribute name.</summary>
    public const string PreserveAspectRatio = "preserveAspectRatio";
    /// <summary>The <c>x</c> attribute name.</summary>
    public const string X = "x";
    /// <summary>The <c>y</c> attribute name.</summary>
    public const string Y = "y";
    /// <summary>The <c>x1</c> attribute name.</summary>
    public const string X1 = "x1";
    /// <summary>The <c>y1</c> attribute name.</summary>
    public const string Y1 = "y1";
    /// <summary>The <c>x2</c> attribute name.</summary>
    public const string X2 = "x2";
    /// <summary>The <c>y2</c> attribute name.</summary>
    public const string Y2 = "y2";
    /// <summary>The <c>cx</c> attribute name.</summary>
    public const string Cx = "cx";
    /// <summary>The <c>cy</c> attribute name.</summary>
    public const string Cy = "cy";
    /// <summary>The <c>r</c> attribute name.</summary>
    public const string R = "r";
    /// <summary>The <c>rx</c> attribute name.</summary>
    public const string Rx = "rx";
    /// <summary>The <c>ry</c> attribute name.</summary>
    public const string Ry = "ry";
    /// <summary>The <c>width</c> attribute name.</summary>
    public const string Width = "width";
    /// <summary>The <c>height</c> attribute name.</summary>
    public const string Height = "height";
    /// <summary>The <c>d</c> attribute name.</summary>
    public const string D = "d";
    /// <summary>The <c>path</c> attribute name.</summary>
    public const string Path = "path";
    /// <summary>The <c>pathLength</c> attribute name.</summary>
    public const string PathLength = "pathLength";
    /// <summary>The <c>points</c> attribute name.</summary>
    public const string Points = "points";
    /// <summary>The <c>dx</c> attribute name.</summary>
    public const string Dx = "dx";
    /// <summary>The <c>dy</c> attribute name.</summary>
    public const string Dy = "dy";
    /// <summary>The <c>rotate</c> attribute name.</summary>
    public const string Rotate = "rotate";
    /// <summary>The <c>textLength</c> attribute name.</summary>
    public const string TextLength = "textLength";
    /// <summary>The <c>lengthAdjust</c> attribute name.</summary>
    public const string LengthAdjust = "lengthAdjust";
    /// <summary>The <c>startOffset</c> attribute name.</summary>
    public const string StartOffset = "startOffset";
    /// <summary>The <c>method</c> attribute name.</summary>
    public const string Method = "method";
    /// <summary>The <c>spacing</c> attribute name.</summary>
    public const string Spacing = "spacing";
    /// <summary>The <c>side</c> attribute name.</summary>
    public const string Side = "side";
    /// <summary>The <c>type</c> attribute name.</summary>
    public const string Type = "type";
    /// <summary>The <c>media</c> attribute name.</summary>
    public const string Media = "media";
    /// <summary>The <c>title</c> attribute name.</summary>
    public const string Title = "title";
    /// <summary>The <c>target</c> attribute name.</summary>
    public const string Target = "target";
    /// <summary>The <c>download</c> attribute name.</summary>
    public const string Download = "download";
    /// <summary>The <c>hreflang</c> attribute name.</summary>
    public const string HrefLanguage = "hreflang";
    /// <summary>The <c>rel</c> attribute name.</summary>
    public const string Rel = "rel";
    /// <summary>The <c>crossorigin</c> attribute name.</summary>
    public const string CrossOrigin = "crossorigin";
    /// <summary>The <c>requiredExtensions</c> attribute name.</summary>
    public const string RequiredExtensions = "requiredExtensions";
    /// <summary>The <c>requiredFeatures</c> attribute name.</summary>
    public const string RequiredFeatures = "requiredFeatures";
    /// <summary>The <c>systemLanguage</c> attribute name.</summary>
    public const string SystemLanguage = "systemLanguage";
    /// <summary>The <c>gradientUnits</c> attribute name.</summary>
    public const string GradientUnits = "gradientUnits";
    /// <summary>The <c>gradientTransform</c> attribute name.</summary>
    public const string GradientTransform = "gradientTransform";
    /// <summary>The <c>spreadMethod</c> attribute name.</summary>
    public const string SpreadMethod = "spreadMethod";
    /// <summary>The <c>fx</c> attribute name.</summary>
    public const string Fx = "fx";
    /// <summary>The <c>fy</c> attribute name.</summary>
    public const string Fy = "fy";
    /// <summary>The <c>fr</c> attribute name.</summary>
    public const string Fr = "fr";
    /// <summary>The <c>offset</c> attribute name.</summary>
    public const string Offset = "offset";
    /// <summary>The <c>patternUnits</c> attribute name.</summary>
    public const string PatternUnits = "patternUnits";
    /// <summary>The <c>patternContentUnits</c> attribute name.</summary>
    public const string PatternContentUnits = "patternContentUnits";
    /// <summary>The <c>patternTransform</c> attribute name.</summary>
    public const string PatternTransform = "patternTransform";
    /// <summary>The <c>markerUnits</c> attribute name.</summary>
    public const string MarkerUnits = "markerUnits";
    /// <summary>The <c>markerWidth</c> attribute name.</summary>
    public const string MarkerWidth = "markerWidth";
    /// <summary>The <c>markerHeight</c> attribute name.</summary>
    public const string MarkerHeight = "markerHeight";
    /// <summary>The <c>refX</c> attribute name.</summary>
    public const string RefX = "refX";
    /// <summary>The <c>refY</c> attribute name.</summary>
    public const string RefY = "refY";
    /// <summary>The <c>orient</c> attribute name.</summary>
    public const string Orient = "orient";
    /// <summary>The <c>clipPathUnits</c> attribute name.</summary>
    public const string ClipPathUnits = "clipPathUnits";
    /// <summary>The <c>maskUnits</c> attribute name.</summary>
    public const string MaskUnits = "maskUnits";
    /// <summary>The <c>maskContentUnits</c> attribute name.</summary>
    public const string MaskContentUnits = "maskContentUnits";
    /// <summary>The <c>filterUnits</c> attribute name.</summary>
    public const string FilterUnits = "filterUnits";
    /// <summary>The <c>primitiveUnits</c> attribute name.</summary>
    public const string PrimitiveUnits = "primitiveUnits";
    /// <summary>The <c>in</c> filter input attribute name.</summary>
    public const string In = "in";
    /// <summary>The <c>in2</c> filter input attribute name.</summary>
    public const string In2 = "in2";
    /// <summary>The <c>result</c> filter result attribute name.</summary>
    public const string Result = "result";
    /// <summary>The <c>mode</c> attribute name.</summary>
    public const string Mode = "mode";
    /// <summary>The <c>values</c> attribute name.</summary>
    public const string Values = "values";
    /// <summary>The <c>operator</c> attribute name.</summary>
    public const string Operator = "operator";
    /// <summary>The <c>k1</c> attribute name.</summary>
    public const string K1 = "k1";
    /// <summary>The <c>k2</c> attribute name.</summary>
    public const string K2 = "k2";
    /// <summary>The <c>k3</c> attribute name.</summary>
    public const string K3 = "k3";
    /// <summary>The <c>k4</c> attribute name.</summary>
    public const string K4 = "k4";
    /// <summary>The <c>order</c> attribute name.</summary>
    public const string Order = "order";
    /// <summary>The <c>kernelMatrix</c> attribute name.</summary>
    public const string KernelMatrix = "kernelMatrix";
    /// <summary>The <c>divisor</c> attribute name.</summary>
    public const string Divisor = "divisor";
    /// <summary>The <c>bias</c> attribute name.</summary>
    public const string Bias = "bias";
    /// <summary>The <c>targetX</c> attribute name.</summary>
    public const string TargetX = "targetX";
    /// <summary>The <c>targetY</c> attribute name.</summary>
    public const string TargetY = "targetY";
    /// <summary>The <c>edgeMode</c> attribute name.</summary>
    public const string EdgeMode = "edgeMode";
    /// <summary>The <c>kernelUnitLength</c> attribute name.</summary>
    public const string KernelUnitLength = "kernelUnitLength";
    /// <summary>The <c>preserveAlpha</c> attribute name.</summary>
    public const string PreserveAlpha = "preserveAlpha";
    /// <summary>The <c>surfaceScale</c> attribute name.</summary>
    public const string SurfaceScale = "surfaceScale";
    /// <summary>The <c>diffuseConstant</c> attribute name.</summary>
    public const string DiffuseConstant = "diffuseConstant";
    /// <summary>The <c>specularConstant</c> attribute name.</summary>
    public const string SpecularConstant = "specularConstant";
    /// <summary>The <c>specularExponent</c> attribute name.</summary>
    public const string SpecularExponent = "specularExponent";
    /// <summary>The <c>scale</c> attribute name.</summary>
    public const string Scale = "scale";
    /// <summary>The <c>xChannelSelector</c> attribute name.</summary>
    public const string XChannelSelector = "xChannelSelector";
    /// <summary>The <c>yChannelSelector</c> attribute name.</summary>
    public const string YChannelSelector = "yChannelSelector";
    /// <summary>The <c>azimuth</c> attribute name.</summary>
    public const string Azimuth = "azimuth";
    /// <summary>The <c>elevation</c> attribute name.</summary>
    public const string Elevation = "elevation";
    /// <summary>The <c>stdDeviation</c> attribute name.</summary>
    public const string StdDeviation = "stdDeviation";
    /// <summary>The <c>radius</c> attribute name.</summary>
    public const string Radius = "radius";
    /// <summary>The <c>baseFrequency</c> attribute name.</summary>
    public const string BaseFrequency = "baseFrequency";
    /// <summary>The <c>numOctaves</c> attribute name.</summary>
    public const string NumOctaves = "numOctaves";
    /// <summary>The <c>seed</c> attribute name.</summary>
    public const string Seed = "seed";
    /// <summary>The <c>stitchTiles</c> attribute name.</summary>
    public const string StitchTiles = "stitchTiles";
    /// <summary>The <c>tableValues</c> attribute name.</summary>
    public const string TableValues = "tableValues";
    /// <summary>The <c>slope</c> attribute name.</summary>
    public const string Slope = "slope";
    /// <summary>The <c>intercept</c> attribute name.</summary>
    public const string Intercept = "intercept";
    /// <summary>The <c>amplitude</c> attribute name.</summary>
    public const string Amplitude = "amplitude";
    /// <summary>The <c>exponent</c> attribute name.</summary>
    public const string Exponent = "exponent";
    /// <summary>The <c>begin</c> animation attribute name.</summary>
    public const string Begin = "begin";
    /// <summary>The <c>dur</c> animation attribute name.</summary>
    public const string Dur = "dur";
    /// <summary>The <c>end</c> animation attribute name.</summary>
    public const string End = "end";
    /// <summary>The <c>min</c> animation attribute name.</summary>
    public const string Min = "min";
    /// <summary>The <c>max</c> animation attribute name.</summary>
    public const string Max = "max";
    /// <summary>The <c>restart</c> animation attribute name.</summary>
    public const string Restart = "restart";
    /// <summary>The <c>repeatCount</c> animation attribute name.</summary>
    public const string RepeatCount = "repeatCount";
    /// <summary>The <c>repeatDur</c> animation attribute name.</summary>
    public const string RepeatDur = "repeatDur";
    /// <summary>The <c>fill</c> attribute name.</summary>
    public const string Fill = "fill";
    /// <summary>The <c>attributeName</c> animation attribute name.</summary>
    public const string AttributeName = "attributeName";
    /// <summary>The <c>attributeType</c> animation attribute name.</summary>
    public const string AttributeType = "attributeType";
    /// <summary>The <c>from</c> animation attribute name.</summary>
    public const string From = "from";
    /// <summary>The <c>to</c> animation attribute name.</summary>
    public const string To = "to";
    /// <summary>The <c>by</c> animation attribute name.</summary>
    public const string By = "by";
    /// <summary>The <c>calcMode</c> animation attribute name.</summary>
    public const string CalcMode = "calcMode";
    /// <summary>The <c>keyTimes</c> animation attribute name.</summary>
    public const string KeyTimes = "keyTimes";
    /// <summary>The <c>keySplines</c> animation attribute name.</summary>
    public const string KeySplines = "keySplines";
    /// <summary>The <c>keyPoints</c> animation attribute name.</summary>
    public const string KeyPoints = "keyPoints";
    /// <summary>The <c>additive</c> animation attribute name.</summary>
    public const string Additive = "additive";
    /// <summary>The <c>accumulate</c> animation attribute name.</summary>
    public const string Accumulate = "accumulate";
}

/// <summary>Defines canonical lowercase names for supported CSS properties.</summary>
public static class SvgCssPropertyNames
{
    /// <summary>The <c>animation</c> shorthand property name.</summary>
    public const string Animation = "animation";
    /// <summary>The <c>animation-name</c> property name.</summary>
    public const string AnimationName = "animation-name";
    /// <summary>The <c>animation-duration</c> property name.</summary>
    public const string AnimationDuration = "animation-duration";
    /// <summary>The <c>animation-timing-function</c> property name.</summary>
    public const string AnimationTimingFunction = "animation-timing-function";
    /// <summary>The <c>animation-delay</c> property name.</summary>
    public const string AnimationDelay = "animation-delay";
    /// <summary>The <c>animation-iteration-count</c> property name.</summary>
    public const string AnimationIterationCount = "animation-iteration-count";
    /// <summary>The <c>animation-direction</c> property name.</summary>
    public const string AnimationDirection = "animation-direction";
    /// <summary>The <c>animation-fill-mode</c> property name.</summary>
    public const string AnimationFillMode = "animation-fill-mode";
    /// <summary>The <c>animation-play-state</c> property name.</summary>
    public const string AnimationPlayState = "animation-play-state";
    /// <summary>The <c>alignment-baseline</c> property name.</summary>
    public const string AlignmentBaseline = "alignment-baseline";
    /// <summary>The <c>baseline-shift</c> property name.</summary>
    public const string BaselineShift = "baseline-shift";
    /// <summary>The legacy <c>clip</c> property name.</summary>
    public const string Clip = "clip";
    /// <summary>The <c>clip-path</c> property name.</summary>
    public const string ClipPath = "clip-path";
    /// <summary>The <c>clip-rule</c> property name.</summary>
    public const string ClipRule = "clip-rule";
    /// <summary>The <c>color</c> property name.</summary>
    public const string Color = "color";
    /// <summary>The legacy <c>color-profile</c> property name.</summary>
    public const string ColorProfile = "color-profile";
    /// <summary>The <c>color-interpolation</c> property name.</summary>
    public const string ColorInterpolation = "color-interpolation";
    /// <summary>The <c>color-interpolation-filters</c> property name.</summary>
    public const string ColorInterpolationFilters = "color-interpolation-filters";
    /// <summary>The <c>color-rendering</c> property name.</summary>
    public const string ColorRendering = "color-rendering";
    /// <summary>The <c>cursor</c> property name.</summary>
    public const string Cursor = "cursor";
    /// <summary>The <c>direction</c> property name.</summary>
    public const string Direction = "direction";
    /// <summary>The <c>display</c> property name.</summary>
    public const string Display = "display";
    /// <summary>The <c>dominant-baseline</c> property name.</summary>
    public const string DominantBaseline = "dominant-baseline";
    /// <summary>The legacy <c>enable-background</c> property name.</summary>
    public const string EnableBackground = "enable-background";
    /// <summary>The <c>fill</c> property name.</summary>
    public const string Fill = "fill";
    /// <summary>The <c>fill-opacity</c> property name.</summary>
    public const string FillOpacity = "fill-opacity";
    /// <summary>The <c>fill-rule</c> property name.</summary>
    public const string FillRule = "fill-rule";
    /// <summary>The <c>filter</c> property name.</summary>
    public const string Filter = "filter";
    /// <summary>The <c>flood-color</c> property name.</summary>
    public const string FloodColor = "flood-color";
    /// <summary>The <c>flood-opacity</c> property name.</summary>
    public const string FloodOpacity = "flood-opacity";
    /// <summary>The <c>font</c> shorthand property name.</summary>
    public const string Font = "font";
    /// <summary>The <c>font-family</c> property name.</summary>
    public const string FontFamily = "font-family";
    /// <summary>The <c>font-feature-settings</c> property name.</summary>
    public const string FontFeatureSettings = "font-feature-settings";
    /// <summary>The <c>font-kerning</c> property name.</summary>
    public const string FontKerning = "font-kerning";
    /// <summary>The <c>font-size</c> property name.</summary>
    public const string FontSize = "font-size";
    /// <summary>The <c>font-size-adjust</c> property name.</summary>
    public const string FontSizeAdjust = "font-size-adjust";
    /// <summary>The <c>font-stretch</c> property name.</summary>
    public const string FontStretch = "font-stretch";
    /// <summary>The <c>font-style</c> property name.</summary>
    public const string FontStyle = "font-style";
    /// <summary>The <c>font-variant</c> property name.</summary>
    public const string FontVariant = "font-variant";
    /// <summary>The <c>font-variation-settings</c> property name.</summary>
    public const string FontVariationSettings = "font-variation-settings";
    /// <summary>The <c>font-weight</c> property name.</summary>
    public const string FontWeight = "font-weight";
    /// <summary>The legacy <c>glyph-orientation-horizontal</c> property name.</summary>
    public const string GlyphOrientationHorizontal = "glyph-orientation-horizontal";
    /// <summary>The legacy <c>glyph-orientation-vertical</c> property name.</summary>
    public const string GlyphOrientationVertical = "glyph-orientation-vertical";
    /// <summary>The <c>image-rendering</c> property name.</summary>
    public const string ImageRendering = "image-rendering";
    /// <summary>The <c>inline-size</c> property name.</summary>
    public const string InlineSize = "inline-size";
    /// <summary>The <c>isolation</c> property name.</summary>
    public const string Isolation = "isolation";
    /// <summary>The legacy <c>kerning</c> property name.</summary>
    public const string Kerning = "kerning";
    /// <summary>The <c>letter-spacing</c> property name.</summary>
    public const string LetterSpacing = "letter-spacing";
    /// <summary>The <c>lighting-color</c> property name.</summary>
    public const string LightingColor = "lighting-color";
    /// <summary>The <c>marker</c> shorthand property name.</summary>
    public const string Marker = "marker";
    /// <summary>The <c>marker-end</c> property name.</summary>
    public const string MarkerEnd = "marker-end";
    /// <summary>The <c>marker-mid</c> property name.</summary>
    public const string MarkerMid = "marker-mid";
    /// <summary>The <c>marker-start</c> property name.</summary>
    public const string MarkerStart = "marker-start";
    /// <summary>The <c>mask</c> property name.</summary>
    public const string Mask = "mask";
    /// <summary>The <c>mask-type</c> property name.</summary>
    public const string MaskType = "mask-type";
    /// <summary>The <c>mix-blend-mode</c> property name.</summary>
    public const string MixBlendMode = "mix-blend-mode";
    /// <summary>The <c>opacity</c> property name.</summary>
    public const string Opacity = "opacity";
    /// <summary>The <c>overflow</c> property name.</summary>
    public const string Overflow = "overflow";
    /// <summary>The <c>overflow-x</c> property name.</summary>
    public const string OverflowX = "overflow-x";
    /// <summary>The <c>overflow-y</c> property name.</summary>
    public const string OverflowY = "overflow-y";
    /// <summary>The <c>paint-order</c> property name.</summary>
    public const string PaintOrder = "paint-order";
    /// <summary>The <c>pointer-events</c> property name.</summary>
    public const string PointerEvents = "pointer-events";
    /// <summary>The <c>shape-rendering</c> property name.</summary>
    public const string ShapeRendering = "shape-rendering";
    /// <summary>The <c>stop-color</c> property name.</summary>
    public const string StopColor = "stop-color";
    /// <summary>The <c>stop-opacity</c> property name.</summary>
    public const string StopOpacity = "stop-opacity";
    /// <summary>The <c>stroke</c> property name.</summary>
    public const string Stroke = "stroke";
    /// <summary>The <c>stroke-dasharray</c> property name.</summary>
    public const string StrokeDashArray = "stroke-dasharray";
    /// <summary>The <c>stroke-dashoffset</c> property name.</summary>
    public const string StrokeDashOffset = "stroke-dashoffset";
    /// <summary>The <c>stroke-linecap</c> property name.</summary>
    public const string StrokeLineCap = "stroke-linecap";
    /// <summary>The <c>stroke-linejoin</c> property name.</summary>
    public const string StrokeLineJoin = "stroke-linejoin";
    /// <summary>The <c>stroke-miterlimit</c> property name.</summary>
    public const string StrokeMiterLimit = "stroke-miterlimit";
    /// <summary>The <c>stroke-opacity</c> property name.</summary>
    public const string StrokeOpacity = "stroke-opacity";
    /// <summary>The <c>stroke-width</c> property name.</summary>
    public const string StrokeWidth = "stroke-width";
    /// <summary>The <c>text-anchor</c> property name.</summary>
    public const string TextAnchor = "text-anchor";
    /// <summary>The <c>text-decoration</c> shorthand property name.</summary>
    public const string TextDecoration = "text-decoration";
    /// <summary>The <c>text-decoration-color</c> property name.</summary>
    public const string TextDecorationColor = "text-decoration-color";
    /// <summary>The <c>text-decoration-line</c> property name.</summary>
    public const string TextDecorationLine = "text-decoration-line";
    /// <summary>The <c>text-decoration-style</c> property name.</summary>
    public const string TextDecorationStyle = "text-decoration-style";
    /// <summary>The <c>text-rendering</c> property name.</summary>
    public const string TextRendering = "text-rendering";
    /// <summary>The <c>transform</c> property name.</summary>
    public const string Transform = "transform";
    /// <summary>The <c>transform-box</c> property name.</summary>
    public const string TransformBox = "transform-box";
    /// <summary>The <c>transform-origin</c> property name.</summary>
    public const string TransformOrigin = "transform-origin";
    /// <summary>The <c>unicode-bidi</c> property name.</summary>
    public const string UnicodeBidi = "unicode-bidi";
    /// <summary>The <c>vector-effect</c> property name.</summary>
    public const string VectorEffect = "vector-effect";
    /// <summary>The <c>visibility</c> property name.</summary>
    public const string Visibility = "visibility";
    /// <summary>The <c>white-space</c> property name.</summary>
    public const string WhiteSpace = "white-space";
    /// <summary>The <c>word-spacing</c> property name.</summary>
    public const string WordSpacing = "word-spacing";
    /// <summary>The <c>writing-mode</c> property name.</summary>
    public const string WritingMode = "writing-mode";
    /// <summary>The SVG geometry <c>cx</c> property name.</summary>
    public const string Cx = "cx";
    /// <summary>The SVG geometry <c>cy</c> property name.</summary>
    public const string Cy = "cy";
    /// <summary>The SVG geometry <c>d</c> property name.</summary>
    public const string D = "d";
    /// <summary>The SVG geometry <c>height</c> property name.</summary>
    public const string Height = "height";
    /// <summary>The SVG geometry <c>r</c> property name.</summary>
    public const string R = "r";
    /// <summary>The SVG geometry <c>rx</c> property name.</summary>
    public const string Rx = "rx";
    /// <summary>The SVG geometry <c>ry</c> property name.</summary>
    public const string Ry = "ry";
    /// <summary>The SVG geometry <c>width</c> property name.</summary>
    public const string Width = "width";
    /// <summary>The SVG geometry <c>x</c> property name.</summary>
    public const string X = "x";
    /// <summary>The SVG geometry <c>y</c> property name.</summary>
    public const string Y = "y";
}

/// <summary>Defines stable profile and feature capability identifiers.</summary>
public static class SvgFeatureIdentifiers
{
    /// <summary>The prefix for individual SVG element capabilities.</summary>
    public const string ElementPrefix = "svg.feature.element.";
    /// <summary>The prefix for individual CSS property capabilities.</summary>
    public const string CssPropertyPrefix = "svg.feature.css.property.";
    /// <summary>The prefix for individual CSS at-rule capabilities.</summary>
    public const string CssAtRulePrefix = "svg.feature.css.at-rule.";
    /// <summary>The prefix for individual CSS selector capabilities.</summary>
    public const string CssSelectorPrefix = "svg.feature.css.selector.";
    /// <summary>The prefix for individual filter primitive capabilities.</summary>
    public const string FilterPrimitivePrefix = "svg.feature.filter.primitive.";
    /// <summary>The prefix for individual animation value capabilities.</summary>
    public const string AnimationValuePrefix = "svg.feature.animation.value.";
    /// <summary>The prefix for individual image format capabilities.</summary>
    public const string ImageFormatPrefix = "svg.feature.image.format.";
    /// <summary>The prefix for individual font format capabilities.</summary>
    public const string FontFormatPrefix = "svg.feature.font.format.";
    /// <summary>The prefix for individual processor job capabilities.</summary>
    public const string ProcessorJobPrefix = "svg.feature.processor.job.";
    /// <summary>The prefix for individual host callback boundaries.</summary>
    public const string HostCallbackPrefix = "svg.feature.host.callback.";

    /// <summary>The primary SVG 2 profile identifier.</summary>
    public const string ProfileSvg2 = "svg.profile.svg2";
    /// <summary>The SVG 1.1 compatibility profile identifier.</summary>
    public const string ProfileSvg11Compatibility = "svg.profile.svg11.compat";
    /// <summary>The preserve-only legacy profile identifier.</summary>
    public const string ProfileLegacyPreservation = "svg.profile.legacy.preserve";
    /// <summary>The explicit host-callback profile identifier.</summary>
    public const string ProfileHostCallback = "svg.profile.host.callback";

    /// <summary>The source-provenance foundation feature identifier.</summary>
    public const string SourceMapping = "svg.feature.foundation.source-mapping";
    /// <summary>The bounded diagnostics foundation feature identifier.</summary>
    public const string Diagnostics = "svg.feature.foundation.diagnostics";
    /// <summary>The security-limits foundation feature identifier.</summary>
    public const string SecurityLimits = "svg.feature.foundation.security-limits";
    /// <summary>The capability-reporting foundation feature identifier.</summary>
    public const string CapabilityReporting = "svg.feature.foundation.capabilities";

    /// <summary>The secure SVG and SVGZ loading feature identifier.</summary>
    public const string Loading = "svg.feature.loading";
    /// <summary>The source-preserving XML document feature identifier.</summary>
    public const string EditableDocument = "svg.feature.document.editable";
    /// <summary>The document mutation feature identifier.</summary>
    public const string DocumentMutation = "svg.feature.document.mutation";
    /// <summary>The CSS parsing feature identifier.</summary>
    public const string CssParsing = "svg.feature.css.parsing";
    /// <summary>The CSS selector feature identifier.</summary>
    public const string CssSelectors = "svg.feature.css.selectors";
    /// <summary>The CSS cascade feature identifier.</summary>
    public const string CssCascade = "svg.feature.css.cascade";
    /// <summary>The SVG shape feature identifier.</summary>
    public const string Shapes = "svg.feature.geometry.shapes";
    /// <summary>The SVG path feature identifier.</summary>
    public const string Paths = "svg.feature.geometry.paths";
    /// <summary>The path-processing feature identifier.</summary>
    public const string PathProcessing = "svg.feature.geometry.processing";
    /// <summary>The gradient and pattern paint feature identifier.</summary>
    public const string PaintServers = "svg.feature.paint.servers";
    /// <summary>The marker feature identifier.</summary>
    public const string Markers = "svg.feature.effects.markers";
    /// <summary>The clipping feature identifier.</summary>
    public const string Clipping = "svg.feature.effects.clipping";
    /// <summary>The masking feature identifier.</summary>
    public const string Masking = "svg.feature.effects.masking";
    /// <summary>The filter-effects feature identifier.</summary>
    public const string Filters = "svg.feature.effects.filters";
    /// <summary>The image feature identifier.</summary>
    public const string Images = "svg.feature.images";
    /// <summary>The built-in PNG codec feature identifier.</summary>
    public const string PngCodec = "svg.feature.images.png";
    /// <summary>The host image-codec feature identifier.</summary>
    public const string HostImageCodec = "svg.feature.images.host-codec";
    /// <summary>The basic text layout feature identifier.</summary>
    public const string TextLayout = "svg.feature.text.layout";
    /// <summary>The managed OpenType feature identifier.</summary>
    public const string OpenType = "svg.feature.text.opentype";
    /// <summary>The host text-shaping feature identifier.</summary>
    public const string HostTextShaping = "svg.feature.text.host-shaping";
    /// <summary>The declarative SMIL animation feature identifier.</summary>
    public const string SmilAnimation = "svg.feature.animation.smil";
    /// <summary>The CSS animation feature identifier.</summary>
    public const string CssAnimation = "svg.feature.animation.css";
    /// <summary>The interaction and hit-testing feature identifier.</summary>
    public const string Interaction = "svg.feature.interaction";
    /// <summary>The accessibility-tree feature identifier.</summary>
    public const string Accessibility = "svg.feature.accessibility";
    /// <summary>The backend-neutral scene feature identifier.</summary>
    public const string Scene = "svg.feature.scene";
    /// <summary>The software rasterizer feature identifier.</summary>
    public const string Rasterization = "svg.feature.rendering.raster";
    /// <summary>The host foreign-content feature identifier.</summary>
    public const string ForeignContent = "svg.feature.host.foreign-content";
    /// <summary>The caller-mediated external-resource feature identifier.</summary>
    public const string ExternalResources = "svg.feature.resources.external";
    /// <summary>The declarative validation feature identifier.</summary>
    public const string Validation = "svg.feature.validation";
    /// <summary>The sanitization feature identifier.</summary>
    public const string Sanitization = "svg.feature.sanitization";
    /// <summary>The document processing feature identifier.</summary>
    public const string Processing = "svg.feature.processing";
    /// <summary>The SVG generation feature identifier.</summary>
    public const string Generation = "svg.feature.generation";
    /// <summary>The SVG and SVGZ writing feature identifier.</summary>
    public const string Writing = "svg.feature.writing";
    /// <summary>The preserve-only script feature identifier.</summary>
    public const string ScriptPreservation = "svg.feature.script.preservation";
    /// <summary>The intentionally unsupported script-execution feature identifier.</summary>
    public const string ScriptExecution = "svg.feature.script.execution";
    /// <summary>The legacy SVG content preservation feature identifier.</summary>
    public const string LegacyPreservation = "svg.feature.legacy.preservation";
}

/// <summary>Defines canonical CSS text for supported properties' initial values.</summary>
/// <remarks>Values are tokens, not computed-value objects, and are parsed by the shared value system.</remarks>
public static class SvgInitialValues
{
    /// <summary>The initial value of the <c>animation</c> shorthand.</summary>
    public const string Animation = "none 0s ease 0s 1 normal none running";
    /// <summary>The initial value of <c>animation-name</c>.</summary>
    public const string AnimationName = "none";
    /// <summary>The initial value of <c>animation-duration</c>.</summary>
    public const string AnimationDuration = "0s";
    /// <summary>The initial value of <c>animation-timing-function</c>.</summary>
    public const string AnimationTimingFunction = "ease";
    /// <summary>The initial value of <c>animation-delay</c>.</summary>
    public const string AnimationDelay = "0s";
    /// <summary>The initial value of <c>animation-iteration-count</c>.</summary>
    public const string AnimationIterationCount = "1";
    /// <summary>The initial value of <c>animation-direction</c>.</summary>
    public const string AnimationDirection = "normal";
    /// <summary>The initial value of <c>animation-fill-mode</c>.</summary>
    public const string AnimationFillMode = "none";
    /// <summary>The initial value of <c>animation-play-state</c>.</summary>
    public const string AnimationPlayState = "running";
    /// <summary>The initial value of <c>alignment-baseline</c>.</summary>
    public const string AlignmentBaseline = "baseline";
    /// <summary>The initial value of <c>baseline-shift</c>.</summary>
    public const string BaselineShift = "0";
    /// <summary>The initial value of legacy <c>clip</c>.</summary>
    public const string Clip = "auto";
    /// <summary>The initial value of <c>clip-path</c>.</summary>
    public const string ClipPath = "none";
    /// <summary>The initial value of <c>clip-rule</c>.</summary>
    public const string ClipRule = "nonzero";
    /// <summary>The initial value of <c>color</c>.</summary>
    public const string Color = "black";
    /// <summary>The initial value of legacy <c>color-profile</c>.</summary>
    public const string ColorProfile = "auto";
    /// <summary>The initial value of <c>color-interpolation</c>.</summary>
    public const string ColorInterpolation = "sRGB";
    /// <summary>The initial value of <c>color-interpolation-filters</c>.</summary>
    public const string ColorInterpolationFilters = "linearRGB";
    /// <summary>The initial value of <c>color-rendering</c>.</summary>
    public const string ColorRendering = "auto";
    /// <summary>The initial value of <c>cursor</c>.</summary>
    public const string Cursor = "auto";
    /// <summary>The initial value of <c>direction</c>.</summary>
    public const string Direction = "ltr";
    /// <summary>The initial value of <c>display</c>.</summary>
    public const string Display = "inline";
    /// <summary>The initial value of <c>dominant-baseline</c>.</summary>
    public const string DominantBaseline = "auto";
    /// <summary>The initial value of legacy <c>enable-background</c>.</summary>
    public const string EnableBackground = "accumulate";
    /// <summary>The initial value of <c>fill</c>.</summary>
    public const string Fill = "black";
    /// <summary>The initial value of <c>fill-opacity</c>.</summary>
    public const string FillOpacity = "1";
    /// <summary>The initial value of <c>fill-rule</c>.</summary>
    public const string FillRule = "nonzero";
    /// <summary>The initial value of <c>filter</c>.</summary>
    public const string Filter = "none";
    /// <summary>The initial value of <c>flood-color</c>.</summary>
    public const string FloodColor = "black";
    /// <summary>The initial value of <c>flood-opacity</c>.</summary>
    public const string FloodOpacity = "1";
    /// <summary>A canonical value that resets the <c>font</c> shorthand to its longhand initials.</summary>
    public const string Font = "medium sans-serif";
    /// <summary>The initial value of <c>font-family</c>.</summary>
    public const string FontFamily = "sans-serif";
    /// <summary>The initial value of <c>font-feature-settings</c>.</summary>
    public const string FontFeatureSettings = "normal";
    /// <summary>The initial value of <c>font-kerning</c>.</summary>
    public const string FontKerning = "auto";
    /// <summary>The initial value of <c>font-size</c>.</summary>
    public const string FontSize = "medium";
    /// <summary>The initial value of <c>font-size-adjust</c>.</summary>
    public const string FontSizeAdjust = "none";
    /// <summary>The initial value of <c>font-stretch</c>.</summary>
    public const string FontStretch = "normal";
    /// <summary>The initial value of <c>font-style</c>.</summary>
    public const string FontStyle = "normal";
    /// <summary>The initial value of <c>font-variant</c>.</summary>
    public const string FontVariant = "normal";
    /// <summary>The initial value of <c>font-variation-settings</c>.</summary>
    public const string FontVariationSettings = "normal";
    /// <summary>The initial value of <c>font-weight</c>.</summary>
    public const string FontWeight = "normal";
    /// <summary>The initial value of legacy <c>glyph-orientation-horizontal</c>.</summary>
    public const string GlyphOrientationHorizontal = "0deg";
    /// <summary>The initial value of legacy <c>glyph-orientation-vertical</c>.</summary>
    public const string GlyphOrientationVertical = "auto";
    /// <summary>The initial value of <c>image-rendering</c>.</summary>
    public const string ImageRendering = "auto";
    /// <summary>The initial value of <c>inline-size</c>.</summary>
    public const string InlineSize = "auto";
    /// <summary>The initial value of <c>isolation</c>.</summary>
    public const string Isolation = "auto";
    /// <summary>The initial value of legacy <c>kerning</c>.</summary>
    public const string Kerning = "auto";
    /// <summary>The initial value of <c>letter-spacing</c>.</summary>
    public const string LetterSpacing = "normal";
    /// <summary>The initial value of <c>lighting-color</c>.</summary>
    public const string LightingColor = "white";
    /// <summary>A canonical value that resets the <c>marker</c> shorthand to its longhand initials.</summary>
    public const string Marker = "none";
    /// <summary>The initial value of <c>marker-end</c>.</summary>
    public const string MarkerEnd = "none";
    /// <summary>The initial value of <c>marker-mid</c>.</summary>
    public const string MarkerMid = "none";
    /// <summary>The initial value of <c>marker-start</c>.</summary>
    public const string MarkerStart = "none";
    /// <summary>The initial value of <c>mask</c>.</summary>
    public const string Mask = "none";
    /// <summary>The initial value of <c>mask-type</c>.</summary>
    public const string MaskType = "luminance";
    /// <summary>The initial value of <c>mix-blend-mode</c>.</summary>
    public const string MixBlendMode = "normal";
    /// <summary>The initial value of <c>opacity</c>.</summary>
    public const string Opacity = "1";
    /// <summary>The initial value of <c>overflow</c>.</summary>
    public const string Overflow = "visible";
    /// <summary>The initial value of <c>overflow-x</c>.</summary>
    public const string OverflowX = "visible";
    /// <summary>The initial value of <c>overflow-y</c>.</summary>
    public const string OverflowY = "visible";
    /// <summary>The initial value of <c>paint-order</c>.</summary>
    public const string PaintOrder = "normal";
    /// <summary>The initial value of <c>pointer-events</c>.</summary>
    public const string PointerEvents = "auto";
    /// <summary>The initial value of <c>shape-rendering</c>.</summary>
    public const string ShapeRendering = "auto";
    /// <summary>The initial value of <c>stop-color</c>.</summary>
    public const string StopColor = "black";
    /// <summary>The initial value of <c>stop-opacity</c>.</summary>
    public const string StopOpacity = "1";
    /// <summary>The initial value of <c>stroke</c>.</summary>
    public const string Stroke = "none";
    /// <summary>The initial value of <c>stroke-dasharray</c>.</summary>
    public const string StrokeDashArray = "none";
    /// <summary>The initial value of <c>stroke-dashoffset</c>.</summary>
    public const string StrokeDashOffset = "0";
    /// <summary>The initial value of <c>stroke-linecap</c>.</summary>
    public const string StrokeLineCap = "butt";
    /// <summary>The initial value of <c>stroke-linejoin</c>.</summary>
    public const string StrokeLineJoin = "miter";
    /// <summary>The initial value of <c>stroke-miterlimit</c>.</summary>
    public const string StrokeMiterLimit = "4";
    /// <summary>The initial value of <c>stroke-opacity</c>.</summary>
    public const string StrokeOpacity = "1";
    /// <summary>The initial value of <c>stroke-width</c>.</summary>
    public const string StrokeWidth = "1px";
    /// <summary>The initial value of <c>text-anchor</c>.</summary>
    public const string TextAnchor = "start";
    /// <summary>The initial value of <c>text-decoration</c>.</summary>
    public const string TextDecoration = "none";
    /// <summary>The initial value of <c>text-decoration-color</c>.</summary>
    public const string TextDecorationColor = "currentColor";
    /// <summary>The initial value of <c>text-decoration-line</c>.</summary>
    public const string TextDecorationLine = "none";
    /// <summary>The initial value of <c>text-decoration-style</c>.</summary>
    public const string TextDecorationStyle = "solid";
    /// <summary>The initial value of <c>text-rendering</c>.</summary>
    public const string TextRendering = "auto";
    /// <summary>The initial value of <c>transform</c>.</summary>
    public const string Transform = "none";
    /// <summary>The initial value of <c>transform-box</c>.</summary>
    public const string TransformBox = "view-box";
    /// <summary>The initial value of <c>transform-origin</c>.</summary>
    public const string TransformOrigin = "50% 50%";
    /// <summary>The initial value of <c>unicode-bidi</c>.</summary>
    public const string UnicodeBidi = "normal";
    /// <summary>The initial value of <c>vector-effect</c>.</summary>
    public const string VectorEffect = "none";
    /// <summary>The initial value of <c>visibility</c>.</summary>
    public const string Visibility = "visible";
    /// <summary>The initial value of <c>white-space</c>.</summary>
    public const string WhiteSpace = "normal";
    /// <summary>The initial value of <c>word-spacing</c>.</summary>
    public const string WordSpacing = "normal";
    /// <summary>The initial value of <c>writing-mode</c>.</summary>
    public const string WritingMode = "horizontal-tb";
    /// <summary>The initial value of SVG geometry <c>cx</c>.</summary>
    public const string Cx = "0";
    /// <summary>The initial value of SVG geometry <c>cy</c>.</summary>
    public const string Cy = "0";
    /// <summary>The initial value of SVG geometry <c>d</c>.</summary>
    public const string D = "none";
    /// <summary>The initial value of SVG geometry <c>height</c>.</summary>
    public const string Height = "auto";
    /// <summary>The initial value of SVG geometry <c>r</c>.</summary>
    public const string R = "0";
    /// <summary>The initial value of SVG geometry <c>rx</c>.</summary>
    public const string Rx = "auto";
    /// <summary>The initial value of SVG geometry <c>ry</c>.</summary>
    public const string Ry = "auto";
    /// <summary>The initial value of SVG geometry <c>width</c>.</summary>
    public const string Width = "auto";
    /// <summary>The initial value of SVG geometry <c>x</c>.</summary>
    public const string X = "0";
    /// <summary>The initial value of SVG geometry <c>y</c>.</summary>
    public const string Y = "0";
}
