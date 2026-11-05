namespace svg;

/// <summary>Provides exact namespace-aware SVG element and attribute recognition tables.</summary>
public class SvgNodeNames : ISvgNodeNames
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgNodeNames()
    {
    }

    /// <summary>Returns the recognized element kind for an expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The recognized SVG kind, or <c>Unknown</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgElementKind GetElementKind(string namespaceUri, string localName)
    {
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (localName == null) throw new System.ArgumentNullException("localName");
        if (namespaceUri.Length != 0 && namespaceUri != SvgNamespaces.Svg) return SvgElementKind.Unknown;
        switch (localName)
        {
            case SvgElementNames.A:
                return SvgElementKind.A;
            case SvgElementNames.Animate:
                return SvgElementKind.Animate;
            case SvgElementNames.AnimateMotion:
                return SvgElementKind.AnimateMotion;
            case SvgElementNames.AnimateTransform:
                return SvgElementKind.AnimateTransform;
            case SvgElementNames.Audio:
                return SvgElementKind.Audio;
            case SvgElementNames.Canvas:
                return SvgElementKind.Canvas;
            case SvgElementNames.Circle:
                return SvgElementKind.Circle;
            case SvgElementNames.ClipPath:
                return SvgElementKind.ClipPath;
            case SvgElementNames.Defs:
                return SvgElementKind.Defs;
            case SvgElementNames.Desc:
                return SvgElementKind.Desc;
            case SvgElementNames.Discard:
                return SvgElementKind.Discard;
            case SvgElementNames.Ellipse:
                return SvgElementKind.Ellipse;
            case SvgElementNames.FeBlend:
                return SvgElementKind.FeBlend;
            case SvgElementNames.FeColorMatrix:
                return SvgElementKind.FeColorMatrix;
            case SvgElementNames.FeComponentTransfer:
                return SvgElementKind.FeComponentTransfer;
            case SvgElementNames.FeComposite:
                return SvgElementKind.FeComposite;
            case SvgElementNames.FeConvolveMatrix:
                return SvgElementKind.FeConvolveMatrix;
            case SvgElementNames.FeDiffuseLighting:
                return SvgElementKind.FeDiffuseLighting;
            case SvgElementNames.FeDisplacementMap:
                return SvgElementKind.FeDisplacementMap;
            case SvgElementNames.FeDistantLight:
                return SvgElementKind.FeDistantLight;
            case SvgElementNames.FeDropShadow:
                return SvgElementKind.FeDropShadow;
            case SvgElementNames.FeFlood:
                return SvgElementKind.FeFlood;
            case SvgElementNames.FeFuncA:
                return SvgElementKind.FeFuncA;
            case SvgElementNames.FeFuncB:
                return SvgElementKind.FeFuncB;
            case SvgElementNames.FeFuncG:
                return SvgElementKind.FeFuncG;
            case SvgElementNames.FeFuncR:
                return SvgElementKind.FeFuncR;
            case SvgElementNames.FeGaussianBlur:
                return SvgElementKind.FeGaussianBlur;
            case SvgElementNames.FeImage:
                return SvgElementKind.FeImage;
            case SvgElementNames.FeMerge:
                return SvgElementKind.FeMerge;
            case SvgElementNames.FeMergeNode:
                return SvgElementKind.FeMergeNode;
            case SvgElementNames.FeMorphology:
                return SvgElementKind.FeMorphology;
            case SvgElementNames.FeOffset:
                return SvgElementKind.FeOffset;
            case SvgElementNames.FePointLight:
                return SvgElementKind.FePointLight;
            case SvgElementNames.FeSpecularLighting:
                return SvgElementKind.FeSpecularLighting;
            case SvgElementNames.FeSpotLight:
                return SvgElementKind.FeSpotLight;
            case SvgElementNames.FeTile:
                return SvgElementKind.FeTile;
            case SvgElementNames.FeTurbulence:
                return SvgElementKind.FeTurbulence;
            case SvgElementNames.Filter:
                return SvgElementKind.Filter;
            case SvgElementNames.ForeignObject:
                return SvgElementKind.ForeignObject;
            case SvgElementNames.G:
                return SvgElementKind.G;
            case SvgElementNames.Iframe:
                return SvgElementKind.Iframe;
            case SvgElementNames.Image:
                return SvgElementKind.Image;
            case SvgElementNames.Line:
                return SvgElementKind.Line;
            case SvgElementNames.LinearGradient:
                return SvgElementKind.LinearGradient;
            case SvgElementNames.Marker:
                return SvgElementKind.Marker;
            case SvgElementNames.Mask:
                return SvgElementKind.Mask;
            case SvgElementNames.Metadata:
                return SvgElementKind.Metadata;
            case SvgElementNames.MPath:
                return SvgElementKind.MPath;
            case SvgElementNames.Path:
                return SvgElementKind.Path;
            case SvgElementNames.Pattern:
                return SvgElementKind.Pattern;
            case SvgElementNames.Polygon:
                return SvgElementKind.Polygon;
            case SvgElementNames.Polyline:
                return SvgElementKind.Polyline;
            case SvgElementNames.RadialGradient:
                return SvgElementKind.RadialGradient;
            case SvgElementNames.Rect:
                return SvgElementKind.Rect;
            case SvgElementNames.Script:
                return SvgElementKind.Script;
            case SvgElementNames.Set:
                return SvgElementKind.Set;
            case SvgElementNames.Stop:
                return SvgElementKind.Stop;
            case SvgElementNames.Style:
                return SvgElementKind.Style;
            case SvgElementNames.Svg:
                return SvgElementKind.Svg;
            case SvgElementNames.Switch:
                return SvgElementKind.Switch;
            case SvgElementNames.Symbol:
                return SvgElementKind.Symbol;
            case SvgElementNames.Text:
                return SvgElementKind.Text;
            case SvgElementNames.TextPath:
                return SvgElementKind.TextPath;
            case SvgElementNames.Title:
                return SvgElementKind.Title;
            case SvgElementNames.TSpan:
                return SvgElementKind.TSpan;
            case SvgElementNames.Use:
                return SvgElementKind.Use;
            case SvgElementNames.Video:
                return SvgElementKind.Video;
            case SvgElementNames.View:
                return SvgElementKind.View;
            case SvgElementNames.AltGlyph:
                return SvgElementKind.AltGlyph;
            case SvgElementNames.AltGlyphDef:
                return SvgElementKind.AltGlyphDef;
            case SvgElementNames.AltGlyphItem:
                return SvgElementKind.AltGlyphItem;
            case SvgElementNames.AnimateColor:
                return SvgElementKind.AnimateColor;
            case SvgElementNames.ColorProfile:
                return SvgElementKind.ColorProfile;
            case SvgElementNames.Cursor:
                return SvgElementKind.Cursor;
            case SvgElementNames.Font:
                return SvgElementKind.Font;
            case SvgElementNames.FontFace:
                return SvgElementKind.FontFace;
            case SvgElementNames.FontFaceFormat:
                return SvgElementKind.FontFaceFormat;
            case SvgElementNames.FontFaceName:
                return SvgElementKind.FontFaceName;
            case SvgElementNames.FontFaceSrc:
                return SvgElementKind.FontFaceSrc;
            case SvgElementNames.FontFaceUri:
                return SvgElementKind.FontFaceUri;
            case SvgElementNames.Glyph:
                return SvgElementKind.Glyph;
            case SvgElementNames.GlyphRef:
                return SvgElementKind.GlyphRef;
            case SvgElementNames.HKern:
                return SvgElementKind.HKern;
            case SvgElementNames.MissingGlyph:
                return SvgElementKind.MissingGlyph;
            case SvgElementNames.TRef:
                return SvgElementKind.TRef;
            case SvgElementNames.VKern:
                return SvgElementKind.VKern;
            default:
                return SvgElementKind.Unknown;
        }
    }

    /// <summary>Tests whether a recognized element belongs to the SVG 2 table.</summary>
    /// <param name="kind">The element kind.</param>
    /// <returns><c>true</c> for SVG 2 element kinds.</returns>
    public bool IsSvg2Element(SvgElementKind kind)
    {
        return kind >= SvgElementKind.A && kind <= SvgElementKind.View;
    }

    /// <summary>Tests whether a recognized element is preserved for SVG 1.1 compatibility.</summary>
    /// <param name="kind">The element kind.</param>
    /// <returns><c>true</c> for legacy SVG 1.1 element kinds.</returns>
    public bool IsSvg11LegacyElement(SvgElementKind kind)
    {
        switch (kind)
        {
            case SvgElementKind.AltGlyph:
            case SvgElementKind.AltGlyphDef:
            case SvgElementKind.AltGlyphItem:
            case SvgElementKind.AnimateColor:
            case SvgElementKind.ColorProfile:
            case SvgElementKind.Cursor:
            case SvgElementKind.Font:
            case SvgElementKind.FontFace:
            case SvgElementKind.FontFaceFormat:
            case SvgElementKind.FontFaceName:
            case SvgElementKind.FontFaceSrc:
            case SvgElementKind.FontFaceUri:
            case SvgElementKind.Glyph:
            case SvgElementKind.GlyphRef:
            case SvgElementKind.HKern:
            case SvgElementKind.MissingGlyph:
            case SvgElementKind.TRef:
            case SvgElementKind.VKern:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Returns the recognized attribute category for an expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The recognized attribute category, or <c>Unknown</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgAttributeKind GetAttributeKind(string namespaceUri, string localName)
    {
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (localName == null) throw new System.ArgumentNullException("localName");
        if (namespaceUri == SvgNamespaces.XLink) return new SvgNodeNames().IsXLinkAttribute(localName) ? SvgAttributeKind.XLink : SvgAttributeKind.Unknown;
        if (namespaceUri == SvgNamespaces.Xml) return new SvgNodeNames().IsXmlAttribute(localName) ? SvgAttributeKind.Xml : SvgAttributeKind.Unknown;
        if (namespaceUri == SvgNamespaces.Xmlns) return SvgAttributeKind.NamespaceDeclaration;
        if (namespaceUri.Length != 0) return SvgAttributeKind.Unknown;
        if (localName == "role" || new SvgNodeNames().IsAriaAttribute(localName)) return SvgAttributeKind.Aria;
        if (localName.Length > 5 && localName.StartsWith("data-", System.StringComparison.Ordinal)) return SvgAttributeKind.Data;
        if (new SvgNodeNames().IsSvg11LegacyAttribute(localName)) return SvgAttributeKind.Svg11Legacy;
        if (new SvgNodeNames().IsSvg2Attribute(localName)) return SvgAttributeKind.Svg2;
        return SvgAttributeKind.Unknown;
    }
bool IsXLinkAttribute(string localName)
    {
        switch (localName)
        {
            case "actuate":
            case "arcrole":
            case "href":
            case "role":
            case "show":
            case "title":
            case "type":
                return true;
            default:
                return false;
        }
    }
bool IsXmlAttribute(string localName)
    {
        switch (localName)
        {
            case "base":
            case "lang":
            case "space":
                return true;
            default:
                return false;
        }
    }
bool IsAriaAttribute(string localName)
    {
        switch (localName)
        {
            case "aria-activedescendant":
            case "aria-atomic":
            case "aria-autocomplete":
            case "aria-busy":
            case "aria-checked":
            case "aria-colcount":
            case "aria-colindex":
            case "aria-colspan":
            case "aria-controls":
            case "aria-current":
            case "aria-describedby":
            case "aria-details":
            case "aria-disabled":
            case "aria-dropeffect":
            case "aria-errormessage":
            case "aria-expanded":
            case "aria-flowto":
            case "aria-grabbed":
            case "aria-haspopup":
            case "aria-hidden":
            case "aria-invalid":
            case "aria-keyshortcuts":
            case "aria-label":
            case "aria-labelledby":
            case "aria-level":
            case "aria-live":
            case "aria-modal":
            case "aria-multiline":
            case "aria-multiselectable":
            case "aria-orientation":
            case "aria-owns":
            case "aria-placeholder":
            case "aria-posinset":
            case "aria-pressed":
            case "aria-readonly":
            case "aria-relevant":
            case "aria-required":
            case "aria-roledescription":
            case "aria-rowcount":
            case "aria-rowindex":
            case "aria-rowspan":
            case "aria-selected":
            case "aria-setsize":
            case "aria-sort":
            case "aria-valuemax":
            case "aria-valuemin":
            case "aria-valuenow":
            case "aria-valuetext":
                return true;
            default:
                return false;
        }
    }
bool IsSvg11LegacyAttribute(string localName)
    {
        switch (localName)
        {
            case "accent-height":
            case "alphabetic":
            case "arabic-form":
            case "ascent":
            case "attributeType":
            case "baseProfile":
            case "bbox":
            case "cap-height":
            case "contentScriptType":
            case "contentStyleType":
            case "descent":
            case "enable-background":
            case "externalResourcesRequired":
            case "filterRes":
            case "format":
            case "g1":
            case "g2":
            case "glyph-name":
            case "glyph-orientation-horizontal":
            case "glyph-orientation-vertical":
            case "glyphRef":
            case "hanging":
            case "horiz-adv-x":
            case "horiz-origin-x":
            case "horiz-origin-y":
            case "ideographic":
            case "k":
            case "kerning":
            case "local":
            case "mathematical":
            case "name":
            case "onactivate":
            case "onzoom":
            case "orientation":
            case "overline-position":
            case "overline-thickness":
            case "panose-1":
            case "rendering-intent":
            case "requiredFeatures":
            case "stemh":
            case "stemv":
            case "strikethrough-position":
            case "strikethrough-thickness":
            case "string":
            case "u1":
            case "u2":
            case "underline-position":
            case "underline-thickness":
            case "unicode":
            case "unicode-range":
            case "units-per-em":
            case "v-alphabetic":
            case "v-hanging":
            case "v-ideographic":
            case "v-mathematical":
            case "version":
            case "vert-adv-y":
            case "vert-origin-x":
            case "vert-origin-y":
            case "viewTarget":
            case "widths":
            case "x-height":
            case "zoomAndPan":
                return true;
            default:
                return false;
        }
    }
bool IsSvg2Attribute(string localName)
    {
        switch (localName)
        {
            case "accumulate":
            case "additive":
            case "alignment-baseline":
            case "amplitude":
            case "attributeName":
            case "azimuth":
            case "baseFrequency":
            case "baseline-shift":
            case "begin":
            case "bias":
            case "by":
            case "calcMode":
            case "class":
            case "clip":
            case "clip-path":
            case "clip-rule":
            case "clipPathUnits":
            case "color":
            case "color-interpolation":
            case "color-interpolation-filters":
            case "color-rendering":
            case "crossorigin":
            case "cursor":
            case "cx":
            case "cy":
            case "d":
            case "diffuseConstant":
            case "direction":
            case "display":
            case "divisor":
            case "dominant-baseline":
            case "download":
            case "dur":
            case "dx":
            case "dy":
            case "edgeMode":
            case "elevation":
            case "enable-background":
            case "end":
            case "exponent":
            case "fill":
            case "fill-opacity":
            case "fill-rule":
            case "filter":
            case "filterUnits":
            case "flood-color":
            case "flood-opacity":
            case "font":
            case "font-family":
            case "font-feature-settings":
            case "font-kerning":
            case "font-size":
            case "font-size-adjust":
            case "font-stretch":
            case "font-style":
            case "font-variant":
            case "font-variation-settings":
            case "font-weight":
            case "fr":
            case "from":
            case "fx":
            case "fy":
            case "glyph-orientation-horizontal":
            case "glyph-orientation-vertical":
            case "gradientTransform":
            case "gradientUnits":
            case "height":
            case "href":
            case "hreflang":
            case "id":
            case "image-rendering":
            case "in":
            case "in2":
            case "inline-size":
            case "intercept":
            case "isolation":
            case "k1":
            case "k2":
            case "k3":
            case "k4":
            case "kernelMatrix":
            case "kernelUnitLength":
            case "kerning":
            case "keyPoints":
            case "keySplines":
            case "keyTimes":
            case "lang":
            case "lengthAdjust":
            case "letter-spacing":
            case "lighting-color":
            case "limitingConeAngle":
            case "marker":
            case "marker-end":
            case "marker-mid":
            case "marker-start":
            case "markerHeight":
            case "markerUnits":
            case "markerWidth":
            case "mask":
            case "mask-type":
            case "maskContentUnits":
            case "maskUnits":
            case "max":
            case "media":
            case "method":
            case "min":
            case "mix-blend-mode":
            case "mode":
            case "numOctaves":
            case "offset":
            case "onabort":
            case "onafterprint":
            case "onbeforeprint":
            case "onbegin":
            case "oncancel":
            case "oncanplay":
            case "oncanplaythrough":
            case "onchange":
            case "onclick":
            case "onclose":
            case "oncopy":
            case "oncuechange":
            case "oncut":
            case "ondblclick":
            case "ondrag":
            case "ondragend":
            case "ondragenter":
            case "ondragexit":
            case "ondragleave":
            case "ondragover":
            case "ondragstart":
            case "ondrop":
            case "ondurationchange":
            case "onemptied":
            case "onend":
            case "onended":
            case "onerror":
            case "onfocus":
            case "onfocusin":
            case "onfocusout":
            case "onhashchange":
            case "oninput":
            case "oninvalid":
            case "onkeydown":
            case "onkeypress":
            case "onkeyup":
            case "onload":
            case "onloadeddata":
            case "onloadedmetadata":
            case "onloadstart":
            case "onmessage":
            case "onmousedown":
            case "onmouseenter":
            case "onmouseleave":
            case "onmousemove":
            case "onmouseout":
            case "onmouseover":
            case "onmouseup":
            case "onmousewheel":
            case "onoffline":
            case "ononline":
            case "onpagehide":
            case "onpageshow":
            case "onpaste":
            case "onpause":
            case "onplay":
            case "onplaying":
            case "onpopstate":
            case "onprogress":
            case "onratechange":
            case "onrepeat":
            case "onreset":
            case "onresize":
            case "onscroll":
            case "onseeked":
            case "onseeking":
            case "onselect":
            case "onshow":
            case "onstalled":
            case "onstorage":
            case "onsubmit":
            case "onsuspend":
            case "ontimeupdate":
            case "ontoggle":
            case "onunload":
            case "onvolumechange":
            case "onwaiting":
            case "opacity":
            case "operator":
            case "order":
            case "orient":
            case "origin":
            case "overflow":
            case "paint-order":
            case "path":
            case "pathLength":
            case "patternContentUnits":
            case "patternTransform":
            case "patternUnits":
            case "ping":
            case "playbackorder":
            case "pointer-events":
            case "points":
            case "pointsAtX":
            case "pointsAtY":
            case "pointsAtZ":
            case "preserveAlpha":
            case "preserveAspectRatio":
            case "primitiveUnits":
            case "r":
            case "radius":
            case "referrerpolicy":
            case "refX":
            case "refY":
            case "rel":
            case "repeatCount":
            case "repeatDur":
            case "requiredExtensions":
            case "restart":
            case "result":
            case "role":
            case "rotate":
            case "rx":
            case "ry":
            case "scale":
            case "seed":
            case "shape-rendering":
            case "side":
            case "slope":
            case "spacing":
            case "specularConstant":
            case "specularExponent":
            case "spreadMethod":
            case "startOffset":
            case "stdDeviation":
            case "stitchTiles":
            case "stop-color":
            case "stop-opacity":
            case "stroke":
            case "stroke-dasharray":
            case "stroke-dashoffset":
            case "stroke-linecap":
            case "stroke-linejoin":
            case "stroke-miterlimit":
            case "stroke-opacity":
            case "stroke-width":
            case "style":
            case "surfaceScale":
            case "systemLanguage":
            case "tabindex":
            case "tableValues":
            case "target":
            case "targetX":
            case "targetY":
            case "text-anchor":
            case "text-decoration":
            case "text-decoration-color":
            case "text-decoration-line":
            case "text-decoration-style":
            case "text-rendering":
            case "textLength":
            case "timelinebegin":
            case "title":
            case "to":
            case "transform":
            case "transform-box":
            case "transform-origin":
            case "type":
            case "unicode-bidi":
            case "values":
            case "vector-effect":
            case "viewBox":
            case "visibility":
            case "white-space":
            case "width":
            case "word-spacing":
            case "writing-mode":
            case "x":
            case "x1":
            case "x2":
            case "xChannelSelector":
            case "y":
            case "y1":
            case "y2":
            case "yChannelSelector":
            case "z":
            case "zoomAndPan":
                return true;
            default:
                return false;
        }
    }
}
