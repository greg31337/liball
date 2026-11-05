using System.Collections.Generic;

namespace odf;

internal static class GraphicPropertyModel
{
    internal static void Resolve(
        OdfDocument document,
        OdfDrawingObject[] drawings)
    {
        int index = 0;
        while (index < drawings.Length)
        {
            OdfDrawingObject drawing = drawings[index];
            OdfStyle? style = FindStyle(document, drawing);
            drawing.GraphicProperties = CreateProperties(
                document,
                drawing,
                style);
            index = index + 1;
        }
    }

    internal static void Apply(OdfDrawingObject drawing)
    {
        OdfGraphicProperties graphic = drawing.GraphicProperties;
        if (graphic == null)
        {
            return;
        }
        if (drawing.Kind == OdfDrawingKind.Image
            && drawing.Node != null
            && graphic.Image.FilterName != graphic.OriginalFilterName)
        {
            WriteFilter(drawing.Node, graphic.Image.FilterName);
        }
        OdfStyle? style = graphic.Style;
        if (style == null || style.Node == null)
        {
            return;
        }
        bool changed = ApplyRawProperties(graphic, style);
        changed = ApplyTypedProperties(graphic, style) || changed;
        if (!changed)
        {
            return;
        }
        style.GraphicPropertiesNode =
            StylePropertyDocumentModel.SynchronizePropertySet(
                style.Node,
                "graphic-properties",
                style.GraphicProperties,
                style.GraphicPropertyChildren,
                style.OriginalGraphicPropertiesNode,
                style.OriginalGraphicPropertyAttributes,
                style.OriginalGraphicPropertyChildren);
        style.OriginalGraphicPropertiesNode = style.GraphicPropertiesNode;
        style.OriginalGraphicPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                style.GraphicProperties);
        style.OriginalGraphicPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                style.GraphicPropertyChildren);
    }

    static OdfGraphicProperties CreateProperties(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfStyle? style)
    {
        OdfGraphicProperties result = new OdfGraphicProperties();
        result.Style = style;
        OdfNode? node = style == null
            ? null
            : EffectiveGraphicProperties(document, style);
        result.Properties = ReadProperties(node);
        PopulateTypedProperties(result);
        if (drawing.Kind == OdfDrawingKind.Image && drawing.Node != null)
        {
            result.Image.FilterName = StyleDocumentModel.ReadAttribute(
                drawing.Node,
                OdfNamespaces.Drawing,
                "filter-name");
        }
        result.OriginalProperties = CopyProperties(result.Properties);
        result.OriginalFilterName = result.Image.FilterName;
        return result;
    }

    static OdfStyle? FindStyle(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        if (drawing.StyleName.Length == 0)
        {
            return null;
        }
        OdfStyleFamily family = drawing.StyleNamespace
                == OdfNamespaces.Presentation
            ? OdfStyleFamily.Presentation
            : OdfStyleFamily.Graphic;
        OdfStyle? fallback = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == drawing.StyleName && style.Family == family)
            {
                if (style.PartPath == drawing.PartPath)
                {
                    return style;
                }
                if (fallback == null)
                {
                    fallback = style;
                }
            }
            index = index + 1;
        }
        return fallback;
    }

    static OdfNode? EffectiveGraphicProperties(
        OdfDocument document,
        OdfStyle style)
    {
        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, style);
        int index = 0;
        while (index < resolution.EffectiveProperties.Length)
        {
            OdfNode node = resolution.EffectiveProperties[index];
            if (node.Name != null
                && node.Name.NamespaceName == OdfNamespaces.Style
                && node.Name.LocalName == "graphic-properties")
            {
                return node;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfStyleProperty[] ReadProperties(OdfNode? node)
    {
        if (node == null)
        {
            return [];
        }
        OdfStyleProperty[] result = new OdfStyleProperty[
            node.Attributes.Length];
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            OdfStyleProperty property = new OdfStyleProperty();
            property.Name.NamespaceName = attribute.Name.NamespaceName;
            property.Name.LocalName = attribute.Name.LocalName;
            property.Name.Prefix = attribute.Name.Prefix;
            property.LexicalValue = attribute.Value;
            property.Attribute = attribute;
            property.OriginalAttribute = attribute;
            property.OriginalNamespaceName = property.Name.NamespaceName;
            property.OriginalLocalName = property.Name.LocalName;
            property.OriginalPrefix = property.Name.Prefix;
            property.OriginalLexicalValue = property.LexicalValue;
            result[index] = property;
            index = index + 1;
        }
        return result;
    }

    static void PopulateTypedProperties(OdfGraphicProperties graphic)
    {
        OdfStyleProperty[] values = graphic.Properties;
        graphic.Fill.Mode = Value(values, OdfNamespaces.Drawing, "fill");
        graphic.Fill.Color = Value(values, OdfNamespaces.Drawing, "fill-color");
        graphic.Fill.SecondaryColor = Value(values, OdfNamespaces.Drawing, "secondary-fill-color");
        graphic.Fill.GradientName = Value(values, OdfNamespaces.Drawing, "fill-gradient-name");
        graphic.Fill.GradientStepCount = Value(values, OdfNamespaces.Drawing, "gradient-step-count");
        graphic.Fill.HatchName = Value(values, OdfNamespaces.Drawing, "fill-hatch-name");
        graphic.Fill.HatchSolidValue = Value(values, OdfNamespaces.Drawing, "fill-hatch-solid");
        graphic.Fill.ImageName = Value(values, OdfNamespaces.Drawing, "fill-image-name");
        graphic.Fill.ImageRepeat = Value(values, OdfNamespaces.Style, "repeat");
        graphic.Fill.ImageWidth = Value(values, OdfNamespaces.Drawing, "fill-image-width");
        graphic.Fill.ImageHeight = Value(values, OdfNamespaces.Drawing, "fill-image-height");
        graphic.Fill.ImageReferencePoint = Value(values, OdfNamespaces.Drawing, "fill-image-ref-point");
        graphic.Fill.ImageReferencePointX = Value(values, OdfNamespaces.Drawing, "fill-image-ref-point-x");
        graphic.Fill.ImageReferencePointY = Value(values, OdfNamespaces.Drawing, "fill-image-ref-point-y");
        graphic.Fill.TileRepeatOffset = Value(values, OdfNamespaces.Drawing, "tile-repeat-offset");
        graphic.Fill.Opacity = Value(values, OdfNamespaces.Drawing, "opacity");
        graphic.Fill.OpacityName = Value(values, OdfNamespaces.Drawing, "opacity-name");
        graphic.Fill.Rule = Value(values, OdfNamespaces.Svg, "fill-rule");
        graphic.Stroke.Mode = Value(values, OdfNamespaces.Drawing, "stroke");
        graphic.Stroke.DashName = Value(values, OdfNamespaces.Drawing, "stroke-dash");
        graphic.Stroke.DashNames = Value(values, OdfNamespaces.Drawing, "stroke-dash-names");
        graphic.Stroke.Width = Value(values, OdfNamespaces.Svg, "stroke-width");
        graphic.Stroke.Color = Value(values, OdfNamespaces.Svg, "stroke-color");
        graphic.Stroke.Opacity = Value(values, OdfNamespaces.Svg, "stroke-opacity");
        graphic.Stroke.LineJoin = Value(values, OdfNamespaces.Drawing, "stroke-linejoin");
        graphic.Stroke.LineCap = Value(values, OdfNamespaces.Svg, "stroke-linecap");
        graphic.Markers.StartName = Value(values, OdfNamespaces.Drawing, "marker-start");
        graphic.Markers.EndName = Value(values, OdfNamespaces.Drawing, "marker-end");
        graphic.Markers.StartWidth = Value(values, OdfNamespaces.Drawing, "marker-start-width");
        graphic.Markers.EndWidth = Value(values, OdfNamespaces.Drawing, "marker-end-width");
        graphic.Markers.StartCenterValue = Value(values, OdfNamespaces.Drawing, "marker-start-center");
        graphic.Markers.EndCenterValue = Value(values, OdfNamespaces.Drawing, "marker-end-center");
        graphic.Shadow.Visibility = Value(values, OdfNamespaces.Drawing, "shadow");
        graphic.Shadow.OffsetX = Value(values, OdfNamespaces.Drawing, "shadow-offset-x");
        graphic.Shadow.OffsetY = Value(values, OdfNamespaces.Drawing, "shadow-offset-y");
        graphic.Shadow.Color = Value(values, OdfNamespaces.Drawing, "shadow-color");
        graphic.Shadow.Opacity = Value(values, OdfNamespaces.Drawing, "shadow-opacity");
        graphic.Wrap.Mode = Value(values, OdfNamespaces.Style, "wrap");
        graphic.Wrap.DynamicThreshold = Value(values, OdfNamespaces.Style, "wrap-dynamic-threshold");
        graphic.Wrap.ParagraphCount = Value(values, OdfNamespaces.Style, "number-wrapped-paragraphs");
        graphic.Wrap.ContourValue = Value(values, OdfNamespaces.Style, "wrap-contour");
        graphic.Wrap.ContourMode = Value(values, OdfNamespaces.Style, "wrap-contour-mode");
        graphic.Wrap.RunThrough = Value(values, OdfNamespaces.Style, "run-through");
        graphic.Wrap.InfluenceOnPosition = Value(values, OdfNamespaces.Drawing, "wrap-influence-on-position");
        graphic.Position.Horizontal = Value(values, OdfNamespaces.Style, "horizontal-pos");
        graphic.Position.HorizontalRelation = Value(values, OdfNamespaces.Style, "horizontal-rel");
        graphic.Position.Vertical = Value(values, OdfNamespaces.Style, "vertical-pos");
        graphic.Position.VerticalRelation = Value(values, OdfNamespaces.Style, "vertical-rel");
        graphic.Position.X = Value(values, OdfNamespaces.Svg, "x");
        graphic.Position.Y = Value(values, OdfNamespaces.Svg, "y");
        graphic.Position.TextAreaHorizontalAlignment = Value(values, OdfNamespaces.Drawing, "textarea-horizontal-align");
        graphic.Position.TextAreaVerticalAlignment = Value(values, OdfNamespaces.Drawing, "textarea-vertical-align");
        graphic.Position.TextWrapOption = Value(values, OdfNamespaces.Fo, "wrap-option");
        graphic.Position.FlowWithTextValue = Value(values, OdfNamespaces.Style, "flow-with-text");
        graphic.Position.Mirror = Value(values, OdfNamespaces.Style, "mirror");
        graphic.Position.Clip = Value(values, OdfNamespaces.Fo, "clip");
        graphic.Image.ColorMode = Value(values, OdfNamespaces.Drawing, "color-mode");
        graphic.Image.ColorInversionValue = Value(values, OdfNamespaces.Drawing, "color-inversion");
        graphic.Image.Luminance = Value(values, OdfNamespaces.Drawing, "luminance");
        graphic.Image.Contrast = Value(values, OdfNamespaces.Drawing, "contrast");
        graphic.Image.Gamma = Value(values, OdfNamespaces.Drawing, "gamma");
        graphic.Image.Red = Value(values, OdfNamespaces.Drawing, "red");
        graphic.Image.Green = Value(values, OdfNamespaces.Drawing, "green");
        graphic.Image.Blue = Value(values, OdfNamespaces.Drawing, "blue");
        graphic.Image.Opacity = Value(values, OdfNamespaces.Drawing, "image-opacity");
    }

    static bool ApplyRawProperties(
        OdfGraphicProperties graphic,
        OdfStyle style)
    {
        if (graphic.Properties == null)
        {
            throw new OdfDocumentException(
                "A graphic-property collection is required for writing.");
        }
        ValidateProperties(graphic.Properties);
        bool changed = false;
        int index = 0;
        while (index < graphic.Properties.Length)
        {
            OdfStyleProperty property = graphic.Properties[index];
            OdfStyleProperty? original = FindProperty(
                graphic.OriginalProperties,
                property.Name.NamespaceName,
                property.Name.LocalName);
            if (original == null
                || property.Name.Prefix != original.Name.Prefix
                || property.LexicalValue != original.LexicalValue)
            {
                SetRawProperty(style, property);
                changed = true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < graphic.OriginalProperties.Length)
        {
            OdfStyleProperty original = graphic.OriginalProperties[index];
            if (FindProperty(
                    graphic.Properties,
                    original.Name.NamespaceName,
                    original.Name.LocalName) == null)
            {
                RemoveProperty(
                    style,
                    original.Name.NamespaceName,
                    original.Name.LocalName);
                changed = true;
            }
            index = index + 1;
        }
        return changed;
    }

    static bool ApplyTypedProperties(
        OdfGraphicProperties graphic,
        OdfStyle style)
    {
        bool changed = false;
        OdfStyleProperty[] originals = graphic.OriginalProperties;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill", graphic.Fill.Mode) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-color", graphic.Fill.Color) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "secondary-fill-color", graphic.Fill.SecondaryColor) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-gradient-name", graphic.Fill.GradientName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "gradient-step-count", graphic.Fill.GradientStepCount) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-hatch-name", graphic.Fill.HatchName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-hatch-solid", graphic.Fill.HatchSolidValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-name", graphic.Fill.ImageName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "repeat", graphic.Fill.ImageRepeat) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-width", graphic.Fill.ImageWidth) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-height", graphic.Fill.ImageHeight) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-ref-point", graphic.Fill.ImageReferencePoint) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-ref-point-x", graphic.Fill.ImageReferencePointX) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "fill-image-ref-point-y", graphic.Fill.ImageReferencePointY) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "tile-repeat-offset", graphic.Fill.TileRepeatOffset) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "opacity", graphic.Fill.Opacity) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "opacity-name", graphic.Fill.OpacityName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "fill-rule", graphic.Fill.Rule) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "stroke", graphic.Stroke.Mode) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "stroke-dash", graphic.Stroke.DashName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "stroke-dash-names", graphic.Stroke.DashNames) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "stroke-width", graphic.Stroke.Width) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "stroke-color", graphic.Stroke.Color) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "stroke-opacity", graphic.Stroke.Opacity) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "stroke-linejoin", graphic.Stroke.LineJoin) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "stroke-linecap", graphic.Stroke.LineCap) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-start", graphic.Markers.StartName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-end", graphic.Markers.EndName) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-start-width", graphic.Markers.StartWidth) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-end-width", graphic.Markers.EndWidth) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-start-center", graphic.Markers.StartCenterValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "marker-end-center", graphic.Markers.EndCenterValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "shadow", graphic.Shadow.Visibility) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "shadow-offset-x", graphic.Shadow.OffsetX) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "shadow-offset-y", graphic.Shadow.OffsetY) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "shadow-color", graphic.Shadow.Color) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "shadow-opacity", graphic.Shadow.Opacity) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "wrap", graphic.Wrap.Mode) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "wrap-dynamic-threshold", graphic.Wrap.DynamicThreshold) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "number-wrapped-paragraphs", graphic.Wrap.ParagraphCount) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "wrap-contour", graphic.Wrap.ContourValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "wrap-contour-mode", graphic.Wrap.ContourMode) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "run-through", graphic.Wrap.RunThrough) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "wrap-influence-on-position", graphic.Wrap.InfluenceOnPosition) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "horizontal-pos", graphic.Position.Horizontal) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "horizontal-rel", graphic.Position.HorizontalRelation) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "vertical-pos", graphic.Position.Vertical) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "vertical-rel", graphic.Position.VerticalRelation) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "x", graphic.Position.X) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Svg, "y", graphic.Position.Y) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "textarea-horizontal-align", graphic.Position.TextAreaHorizontalAlignment) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "textarea-vertical-align", graphic.Position.TextAreaVerticalAlignment) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Fo, "wrap-option", graphic.Position.TextWrapOption) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "flow-with-text", graphic.Position.FlowWithTextValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Style, "mirror", graphic.Position.Mirror) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Fo, "clip", graphic.Position.Clip) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "color-mode", graphic.Image.ColorMode) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "color-inversion", graphic.Image.ColorInversionValue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "luminance", graphic.Image.Luminance) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "contrast", graphic.Image.Contrast) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "gamma", graphic.Image.Gamma) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "red", graphic.Image.Red) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "green", graphic.Image.Green) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "blue", graphic.Image.Blue) || changed;
        changed = SetTyped(style, originals, OdfNamespaces.Drawing, "image-opacity", graphic.Image.Opacity) || changed;
        return changed;
    }

    static bool SetTyped(
        OdfStyle style,
        OdfStyleProperty[] originals,
        string namespaceName,
        string localName,
        string value)
    {
        if (value == Value(originals, namespaceName, localName))
        {
            return false;
        }
        SetProperty(style, namespaceName, localName, value);
        return true;
    }

    static void SetRawProperty(
        OdfStyle style,
        OdfStyleProperty source)
    {
        int index = PropertyIndex(
            style.GraphicProperties,
            source.Name.NamespaceName,
            source.Name.LocalName);
        if (index >= 0)
        {
            OdfStyleProperty target = style.GraphicProperties[index];
            target.Name.Prefix = source.Name.Prefix;
            target.LexicalValue = source.LexicalValue;
            return;
        }
        OdfStyleProperty added = NewProperty(
            source.Name.NamespaceName,
            source.Name.LocalName,
            source.Name.Prefix,
            source.LexicalValue);
        style.GraphicProperties = AppendProperty(
            style.GraphicProperties,
            added);
    }

    static void SetProperty(
        OdfStyle style,
        string namespaceName,
        string localName,
        string value)
    {
        int index = PropertyIndex(
            style.GraphicProperties,
            namespaceName,
            localName);
        if (value.Length == 0)
        {
            if (index >= 0)
            {
                style.GraphicProperties = RemovePropertyAt(
                    style.GraphicProperties,
                    index);
            }
            return;
        }
        if (index >= 0)
        {
            style.GraphicProperties[index].LexicalValue = value;
            return;
        }
        OdfStyleProperty added = NewProperty(
            namespaceName,
            localName,
            "",
            value);
        style.GraphicProperties = AppendProperty(
            style.GraphicProperties,
            added);
    }

    static void RemoveProperty(
        OdfStyle style,
        string namespaceName,
        string localName)
    {
        int index = PropertyIndex(
            style.GraphicProperties,
            namespaceName,
            localName);
        if (index >= 0)
        {
            style.GraphicProperties = RemovePropertyAt(
                style.GraphicProperties,
                index);
        }
    }

    static OdfStyleProperty NewProperty(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        OdfStyleProperty result = new OdfStyleProperty();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Name.Prefix = prefix;
        result.LexicalValue = value;
        return result;
    }

    static OdfStyleProperty[] AppendProperty(
        OdfStyleProperty[] properties,
        OdfStyleProperty value)
    {
        OdfStyleProperty[] result = new OdfStyleProperty[
            properties.Length + 1];
        global::System.Array.Copy(properties, result, properties.Length);
        result[result.Length - 1] = value;
        return result;
    }

    static OdfStyleProperty[] RemovePropertyAt(
        OdfStyleProperty[] properties,
        int removal)
    {
        List<OdfStyleProperty> result = new List<OdfStyleProperty>();
        int index = 0;
        while (index < properties.Length)
        {
            if (index != removal)
            {
                result.Add(properties[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateProperties(OdfStyleProperty[] properties)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null || property.Name == null
                || property.Name.LocalName.Length == 0)
            {
                throw new OdfDocumentException(
                    "Each effective graphic property requires a qualified name.");
            }
            int earlier = 0;
            while (earlier < index)
            {
                OdfStyleProperty previous = properties[earlier];
                if (previous.Name.NamespaceName
                        == property.Name.NamespaceName
                    && previous.Name.LocalName == property.Name.LocalName)
                {
                    throw new OdfDocumentException(
                        "Effective graphic properties cannot contain duplicate qualified names.");
                }
                earlier = earlier + 1;
            }
            index = index + 1;
        }
    }

    static OdfStyleProperty? FindProperty(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        int index = PropertyIndex(properties, namespaceName, localName);
        return index < 0 ? null : properties[index];
    }

    static int PropertyIndex(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property != null && property.Name != null
                && property.Name.NamespaceName == namespaceName
                && property.Name.LocalName == localName)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string Value(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        OdfStyleProperty? property = FindProperty(
            properties,
            namespaceName,
            localName);
        return property == null ? "" : property.LexicalValue;
    }

    static OdfStyleProperty[] CopyProperties(
        OdfStyleProperty[] properties)
    {
        OdfStyleProperty[] result = new OdfStyleProperty[
            properties.Length];
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty source = properties[index];
            result[index] = NewProperty(
                source.Name.NamespaceName,
                source.Name.LocalName,
                source.Name.Prefix,
                source.LexicalValue);
            index = index + 1;
        }
        return result;
    }

    static void WriteFilter(OdfNode node, string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Drawing,
                "filter-name");
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Drawing,
                "filter-name",
                OdfNamespaces.DrawingPrefix,
                value);
        }
    }
}
