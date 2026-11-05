namespace svg;

internal class SvgLegacyCatalogBuilder
{
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> elements;
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> attributes;

    internal SvgLegacyCatalogBuilder()
    {
        elements = new System.Collections.Generic.List<SvgLegacyFeatureDefinition>();
        attributes = new System.Collections.Generic.List<SvgLegacyFeatureDefinition>();
    }

    internal SvgLegacyCatalog Build()
    {
        AddElement(SvgElementKind.AltGlyph, SvgElementNames.AltGlyph, SvgLegacyHandling.Unsupported, "text", "Glyph substitution through altGlyph is not computed.");
        AddElement(SvgElementKind.AltGlyphDef, SvgElementNames.AltGlyphDef, SvgLegacyHandling.PreserveOnly, "", "The alternate-glyph definition remains available for round-tripping.");
        AddElement(SvgElementKind.AltGlyphItem, SvgElementNames.AltGlyphItem, SvgLegacyHandling.PreserveOnly, "", "The alternate-glyph item remains available for round-tripping.");
        AddElement(SvgElementKind.AnimateColor, SvgElementNames.AnimateColor, SvgLegacyHandling.Deprecated, SvgElementNames.Animate, "Color properties use the general animation element.");
        AddElement(SvgElementKind.ColorProfile, SvgElementNames.ColorProfile, SvgLegacyHandling.Deprecated, "CSS color profiles", "The SVG color-profile element was removed from SVG 2.");
        AddElement(SvgElementKind.Cursor, SvgElementNames.Cursor, SvgLegacyHandling.Deprecated, "cursor property", "CSS cursor values replace the SVG cursor element.");
        AddElement(SvgElementKind.Font, SvgElementNames.Font, SvgLegacyHandling.Implemented, "OpenType font", "Embedded SVG font metrics, glyphs, outlines, and kerning are available through the legacy font reader.");
        AddElement(SvgElementKind.FontFace, SvgElementNames.FontFace, SvgLegacyHandling.Implemented, "@font-face", "SVG font-face family and metric metadata is read for legacy font lookup.");
        AddElement(SvgElementKind.FontFaceFormat, SvgElementNames.FontFaceFormat, SvgLegacyHandling.PreserveOnly, "", "Legacy font source metadata is retained.");
        AddElement(SvgElementKind.FontFaceName, SvgElementNames.FontFaceName, SvgLegacyHandling.PreserveOnly, "", "Legacy font source metadata is retained.");
        AddElement(SvgElementKind.FontFaceSrc, SvgElementNames.FontFaceSrc, SvgLegacyHandling.PreserveOnly, "", "Legacy font source metadata is retained.");
        AddElement(SvgElementKind.FontFaceUri, SvgElementNames.FontFaceUri, SvgLegacyHandling.PreserveOnly, "", "Legacy font source metadata is retained.");
        AddElement(SvgElementKind.Glyph, SvgElementNames.Glyph, SvgLegacyHandling.Implemented, "OpenType glyph", "SVG glyph sequences, selectors, metrics, outlines, and child-content identity are available for lookup.");
        AddElement(SvgElementKind.GlyphRef, SvgElementNames.GlyphRef, SvgLegacyHandling.Unsupported, "text", "Direct SVG glyph references are not resolved.");
        AddElement(SvgElementKind.HKern, SvgElementNames.HKern, SvgLegacyHandling.Implemented, "OpenType kerning", "Horizontal SVG font kerning selectors and adjustments are available for lookup.");
        AddElement(SvgElementKind.MissingGlyph, SvgElementNames.MissingGlyph, SvgLegacyHandling.Implemented, "OpenType .notdef glyph", "The SVG missing-glyph metrics and outline are available for fallback.");
        AddElement(SvgElementKind.TRef, SvgElementNames.TRef, SvgLegacyHandling.Deprecated, SvgElementNames.TSpan, "Referenced text content should be copied into the text tree.");
        AddElement(SvgElementKind.VKern, SvgElementNames.VKern, SvgLegacyHandling.Implemented, "OpenType kerning", "Vertical SVG font kerning selectors and adjustments are available for lookup.");

        AddAttribute("enable-background", SvgLegacyHandling.Implemented, "isolation", "The legacy compositing value is retained in computed style.");
        AddAttribute("glyph-orientation-horizontal", SvgLegacyHandling.Implemented, "text-orientation", "The legacy text angle is parsed and retained in computed style.");
        AddAttribute("glyph-orientation-vertical", SvgLegacyHandling.Implemented, "text-orientation", "The legacy text angle or auto value is parsed and retained in computed style.");
        AddAttribute("kerning", SvgLegacyHandling.Implemented, "font-kerning", "The legacy kerning value is parsed and retained in computed style.");

        AddAttributes("attributeType,baseProfile,contentScriptType,contentStyleType,externalResourcesRequired,filterRes,version,viewTarget,zoomAndPan", SvgLegacyHandling.Deprecated, "", "The attribute has no current SVG 2 processing effect.");
        AddAttribute("requiredFeatures", SvgLegacyHandling.Deprecated, "requiredExtensions", "Feature-string conditional processing was removed from SVG 2.");
        AddAttributes("onactivate,onzoom", SvgLegacyHandling.Unsupported, "", "Script execution and legacy event dispatch are never performed.");
        AddAttributes("arabic-form,ascent,descent,g1,g2,glyph-name,horiz-adv-x,horiz-origin-x,horiz-origin-y,k,orientation,u1,u2,unicode,units-per-em,vert-adv-y,vert-origin-x,vert-origin-y", SvgLegacyHandling.Implemented, "", "Legacy SVG font metadata is parsed by the legacy font reader.");
        AddAttributes("accent-height,alphabetic,bbox,cap-height,format,glyphRef,hanging,ideographic,local,mathematical,name,overline-position,overline-thickness,panose-1,rendering-intent,stemh,stemv,strikethrough-position,strikethrough-thickness,string,underline-position,underline-thickness,unicode-range,v-alphabetic,v-hanging,v-ideographic,v-mathematical,widths,x-height", SvgLegacyHandling.PreserveOnly, "", "Legacy SVG font or profile metadata is retained for round-tripping.");

        AddXLink("actuate");
        AddXLink("arcrole");
        AddXLink("href");
        AddXLink("role");
        AddXLink("show");
        AddXLink("title");
        AddXLink("type");
        ValidateComplete();
        return new SvgLegacyCatalog(elements, attributes);
    }

    void AddElement(SvgElementKind kind, string name, SvgLegacyHandling handling, string replacement, string detail)
    {
        elements.Add(new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Element, kind, SvgNamespaces.Svg, name, handling, replacement, detail));
    }

    void AddAttribute(string name, SvgLegacyHandling handling, string replacement, string detail)
    {
        attributes.Add(new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Unknown, "", name, handling, replacement, detail));
    }

    void AddAttributes(string names, SvgLegacyHandling handling, string replacement, string detail)
    {
        System.Collections.Generic.List<string> parts = Split(names);
        int index = 0;
        while (index < parts.Count)
        {
            AddAttribute(parts[index], handling, replacement, detail);
            index = index + 1;
        }
    }

    void AddXLink(string name)
    {
        string replacement = name == "href" || name == "title" ? name : "";
        string detail = replacement.Length == 0 ? "The extended XLink behavior has no SVG 2 processing effect." : "The unqualified SVG 2 attribute takes precedence when both forms are present.";
        attributes.Add(new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Unknown, SvgNamespaces.XLink, name, SvgLegacyHandling.Deprecated, replacement, detail));
    }

    void ValidateComplete()
    {
        int kind = (int)SvgElementKind.AltGlyph;
        while (kind <= (int)SvgElementKind.VKern)
        {
            if (FindElement((SvgElementKind)kind) == null) throw new System.InvalidOperationException("The legacy SVG element catalog is incomplete.");
            kind = kind + 1;
        }
        if (elements.Count != (int)SvgElementKind.VKern - (int)SvgElementKind.AltGlyph + 1) throw new System.InvalidOperationException("The legacy SVG element catalog contains duplicates.");
        if (attributes.Count != 70) throw new System.InvalidOperationException("The legacy SVG attribute catalog is incomplete or contains duplicates.");
        int index = 0;
        while (index < attributes.Count)
        {
            SvgLegacyFeatureDefinition definition = attributes[index];
            SvgAttributeKind attributeKind = new SvgNodeNames().GetAttributeKind(definition.NamespaceUri, definition.LocalName);
            if (definition.NamespaceUri.Length == 0 && attributeKind != SvgAttributeKind.Svg11Legacy) throw new System.InvalidOperationException("A legacy SVG attribute definition is invalid.");
            if (definition.NamespaceUri == SvgNamespaces.XLink && attributeKind != SvgAttributeKind.XLink) throw new System.InvalidOperationException("A legacy XLink attribute definition is invalid.");
            int other = index + 1;
            while (other < attributes.Count)
            {
                if (attributes[other].NamespaceUri == definition.NamespaceUri && attributes[other].LocalName == definition.LocalName) throw new System.InvalidOperationException("The legacy SVG attribute catalog contains duplicates.");
                other = other + 1;
            }
            index = index + 1;
        }
    }

    SvgLegacyFeatureDefinition? FindElement(SvgElementKind kind)
    {
        int index = 0;
        while (index < elements.Count)
        {
            if (elements[index].ElementKind == kind) return elements[index];
            index = index + 1;
        }
        return null;
    }

    static System.Collections.Generic.List<string> Split(string text)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == ',')
            {
                result.Add(text.Substring(start, index - start));
                start = index + 1;
            }
            index = index + 1;
        }
        return result;
    }
}
