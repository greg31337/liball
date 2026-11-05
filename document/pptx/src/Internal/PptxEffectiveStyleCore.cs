using System.Collections.Generic;
using System.Xml;

namespace pptx;

internal static class PptxEffectiveStyleCore
{
    internal static PptxEffectiveStyle ResolveShape(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        PptxTheme theme = GetThemeForShape(shape);
        XmlElement? layoutShape = null;
        XmlElement? masterShape = null;
        PptxSlide slide = shape.Tree.Slide;
        if (slide.Layout != null)
        {
            layoutShape = FindMatchingPlaceholderShape(slide.Layout.Part, shape.Element);
            if (slide.Layout.Master != null)
            {
                masterShape = FindMatchingPlaceholderShape(slide.Layout.Master.Part, shape.Element);
            }
        }

        PptxEffectiveStyle result = new PptxEffectiveStyle();
        ResolveShapeFill(result, theme, shape.Element, layoutShape, masterShape);
        ResolveShapeLine(result, theme, shape.Element, layoutShape, masterShape);
        ResolveShapeEffect(result, theme, shape.Element, layoutShape, masterShape);
        ResolveShapeText(result, theme, shape, layoutShape, masterShape);
        PptxEffectiveStyle background = ResolveBackground(slide);
        result.Background = background.Background;
        result.BackgroundSource = background.BackgroundSource;
        return result;
    }

    internal static PptxEffectiveStyle ResolveBackground(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        PptxEffectiveStyle result = new PptxEffectiveStyle();
        PptxInheritedBackground inherited = slide.Presentation.GetEffectiveBackground(slide);
        if (inherited.Fill.Present)
        {
            result.Background = PptxStyleResolverCore.ResolveFill(GetThemeForSlide(slide), inherited.Fill);
            result.BackgroundSource = inherited.SourceLevel;
        }

        return result;
    }

    internal static PptxTheme GetThemeForShape(PptxShape shape)
    {
        return GetThemeForSlide(shape.Tree.Slide);
    }

    internal static PptxTheme GetThemeForSlide(PptxSlide slide)
    {
        if (slide.Layout != null && slide.Layout.Master != null && slide.Layout.Master.Themes.Count > 0)
        {
            return slide.Layout.Master.Themes[0];
        }

        if (slide.Presentation != null)
        {
            if (slide.Presentation.Theme != null)
            {
                return slide.Presentation.Theme;
            }

            if (slide.Presentation.Themes.Count > 0)
            {
                return slide.Presentation.Themes[0];
            }
        }

        throw new System.InvalidOperationException("A theme is required to resolve effective styles.");
    }

    internal static void ResolveShapeFill(PptxEffectiveStyle result, PptxTheme theme, XmlElement shape, XmlElement? layoutShape, XmlElement? masterShape)
    {
        PptxFill fill = ReadDirectFill(shape);
        if (fill.Present)
        {
            result.Fill = PptxStyleResolverCore.ResolveFill(theme, fill);
            result.FillSource = "shape";
            return;
        }

        XmlElement? reference = FindStyleReference(shape, "fillRef");
        if (reference != null)
        {
            result.Fill = ResolveFillReference(theme, reference);
            result.FillSource = "shape-style";
            return;
        }

        if (layoutShape != null)
        {
            fill = ReadDirectFill(layoutShape);
            if (fill.Present)
            {
                result.Fill = PptxStyleResolverCore.ResolveFill(theme, fill);
                result.FillSource = "layout";
                return;
            }

            reference = FindStyleReference(layoutShape, "fillRef");
            if (reference != null)
            {
                result.Fill = ResolveFillReference(theme, reference);
                result.FillSource = "layout-style";
                return;
            }
        }

        if (masterShape != null)
        {
            fill = ReadDirectFill(masterShape);
            if (fill.Present)
            {
                result.Fill = PptxStyleResolverCore.ResolveFill(theme, fill);
                result.FillSource = "master";
                return;
            }

            reference = FindStyleReference(masterShape, "fillRef");
            if (reference != null)
            {
                result.Fill = ResolveFillReference(theme, reference);
                result.FillSource = "master-style";
                return;
            }
        }

        if (theme.FillStyleCount() > 0)
        {
            result.Fill = PptxStyleResolverCore.ResolveFill(theme, theme.GetFillStyle(0));
            result.FillSource = "theme";
        }
    }

    internal static void ResolveShapeLine(PptxEffectiveStyle result, PptxTheme theme, XmlElement shape, XmlElement? layoutShape, XmlElement? masterShape)
    {
        PptxLine line = ReadDirectLine(shape);
        if (line.Present)
        {
            result.Line = PptxStyleResolverCore.ResolveLine(theme, line);
            result.LineSource = "shape";
            return;
        }

        XmlElement? reference = FindStyleReference(shape, "lnRef");
        if (reference != null)
        {
            result.Line = ResolveLineReference(theme, reference);
            result.LineSource = "shape-style";
            return;
        }

        if (layoutShape != null)
        {
            line = ReadDirectLine(layoutShape);
            if (line.Present)
            {
                result.Line = PptxStyleResolverCore.ResolveLine(theme, line);
                result.LineSource = "layout";
                return;
            }

            reference = FindStyleReference(layoutShape, "lnRef");
            if (reference != null)
            {
                result.Line = ResolveLineReference(theme, reference);
                result.LineSource = "layout-style";
                return;
            }
        }

        if (masterShape != null)
        {
            line = ReadDirectLine(masterShape);
            if (line.Present)
            {
                result.Line = PptxStyleResolverCore.ResolveLine(theme, line);
                result.LineSource = "master";
                return;
            }

            reference = FindStyleReference(masterShape, "lnRef");
            if (reference != null)
            {
                result.Line = ResolveLineReference(theme, reference);
                result.LineSource = "master-style";
                return;
            }
        }

        if (theme.LineStyleCount() > 0)
        {
            result.Line = PptxStyleResolverCore.ResolveLine(theme, theme.GetLineStyle(0));
            result.LineSource = "theme";
        }
    }

    internal static void ResolveShapeEffect(PptxEffectiveStyle result, PptxTheme theme, XmlElement shape, XmlElement? layoutShape, XmlElement? masterShape)
    {
        XmlElement? properties = GetShapeProperties(shape);
        if (HasEffectElement(properties))
        {
            result.Effect = PptxStyleResolverCore.ResolveEffect(theme, PptxEffect.FromShapeProperties(properties));
            result.EffectSource = "shape";
            return;
        }

        XmlElement? reference = FindStyleReference(shape, "effectRef");
        if (reference != null)
        {
            result.Effect = ResolveEffectReference(theme, reference);
            result.EffectSource = "shape-style";
            return;
        }

        if (layoutShape != null)
        {
            properties = GetShapeProperties(layoutShape);
            if (HasEffectElement(properties))
            {
                result.Effect = PptxStyleResolverCore.ResolveEffect(theme, PptxEffect.FromShapeProperties(properties));
                result.EffectSource = "layout";
                return;
            }

            reference = FindStyleReference(layoutShape, "effectRef");
            if (reference != null)
            {
                result.Effect = ResolveEffectReference(theme, reference);
                result.EffectSource = "layout-style";
                return;
            }
        }

        if (masterShape != null)
        {
            properties = GetShapeProperties(masterShape);
            if (HasEffectElement(properties))
            {
                result.Effect = PptxStyleResolverCore.ResolveEffect(theme, PptxEffect.FromShapeProperties(properties));
                result.EffectSource = "master";
                return;
            }

            reference = FindStyleReference(masterShape, "effectRef");
            if (reference != null)
            {
                result.Effect = ResolveEffectReference(theme, reference);
                result.EffectSource = "master-style";
                return;
            }
        }

        if (theme.EffectStyleCount() > 0)
        {
            result.Effect = PptxStyleResolverCore.ResolveEffect(theme, theme.GetEffectStyle(0));
            result.EffectSource = "theme";
        }
    }

    internal static PptxFill ReadDirectFill(XmlElement shape)
    {
        return PptxFill.FromShapeProperties(GetShapeProperties(shape));
    }

    internal static PptxLine ReadDirectLine(XmlElement shape)
    {
        return PptxLine.FromShapeProperties(GetShapeProperties(shape));
    }

    internal static XmlElement? GetShapeProperties(XmlElement shape)
    {
        XmlElement? properties = PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, "spPr");
        if (properties != null)
        {
            return properties;
        }

        properties = PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, "grpSpPr");
        if (properties != null)
        {
            return properties;
        }

        if (shape.NamespaceURI == PptxNamespaces.Drawing || shape.NamespaceURI == PptxNamespaces.Chart)
        {
            return shape;
        }

        return null;
    }

    internal static bool HasEffectElement(XmlElement? properties)
    {
        if (properties == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "effectLst") != null || PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "effectDag") != null || PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "scene3d") != null || PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "sp3d") != null;
    }

    internal static XmlElement? FindStyleReference(XmlElement shape, string localName)
    {
        XmlElement? style = PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, "style");
        if (style == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(style, PptxNamespaces.Drawing, localName);
    }

    internal static PptxFill ResolveFillReference(PptxTheme theme, XmlElement reference)
    {
        int index = PptxXml.GetIntAttribute(reference, "", "idx", 0);
        PptxFill fill = PptxFill.CreateNone();
        if (index >= 1001 && index - 1001 < theme.BackgroundFillStyleCount())
        {
            fill = theme.GetBackgroundFillStyle(index - 1001);
        }
        else if (index > 0 && index - 1 < theme.FillStyleCount())
        {
            fill = theme.GetFillStyle(index - 1);
        }

        PptxColor placeholder = ReadReferenceColor(reference);
        PptxFill clone = CloneFill(fill);
        ResolveFillColors(theme, clone, placeholder);
        return clone;
    }

    internal static PptxLine ResolveLineReference(PptxTheme theme, XmlElement reference)
    {
        int index = PptxXml.GetIntAttribute(reference, "", "idx", 0);
        if (index <= 0 || index - 1 >= theme.LineStyleCount())
        {
            return PptxLine.CreateNone();
        }

        PptxLine line = CloneLine(theme.GetLineStyle(index - 1));
        line.Color = ResolveReferenceColor(theme, line.Color, ReadReferenceColor(reference));
        return line;
    }

    internal static PptxEffect ResolveEffectReference(PptxTheme theme, XmlElement reference)
    {
        int index = PptxXml.GetIntAttribute(reference, "", "idx", 0);
        if (index <= 0 || index - 1 >= theme.EffectStyleCount())
        {
            return PptxEffect.CreateNone();
        }

        PptxEffect effect = CloneEffect(theme.GetEffectStyle(index - 1));
        PptxColor placeholder = ReadReferenceColor(reference);
        if (effect.HasShadow)
        {
            effect.ShadowColor = ResolveReferenceColor(theme, effect.ShadowColor, placeholder);
        }

        if (effect.HasGlow)
        {
            effect.GlowColor = ResolveReferenceColor(theme, effect.GlowColor, placeholder);
        }

        if (effect.ThreeD.ExtrusionColor.Type != PptxColor.Empty)
        {
            effect.ThreeD.ExtrusionColor = ResolveReferenceColor(theme, effect.ThreeD.ExtrusionColor, placeholder);
        }

        if (effect.ThreeD.ContourColor.Type != PptxColor.Empty)
        {
            effect.ThreeD.ContourColor = ResolveReferenceColor(theme, effect.ThreeD.ContourColor, placeholder);
        }

        return effect;
    }

    internal static PptxColor ReadReferenceColor(XmlElement reference)
    {
        PptxColor color = PptxColor.FromParent(reference);
        if (color.Type == PptxColor.Empty)
        {
            color = PptxColor.CreateScheme("dk1");
        }

        return color;
    }

    internal static PptxColor ResolveReferenceColor(PptxTheme theme, PptxColor color, PptxColor placeholder)
    {
        if (color.Type != PptxColor.Scheme || color.Value != "phClr")
        {
            return PptxStyleResolverCore.ResolveColor(theme, color);
        }

        PptxColor baseColor = PptxStyleResolverCore.ResolveColor(theme, placeholder);
        PptxColor transforms = PptxStyleResolverCore.CloneColor(color);
        transforms.Type = PptxColor.Rgb;
        transforms.Value = baseColor.Value;
        transforms.Alpha = (int)((long)baseColor.Alpha * PptxColor.ClampPercentage(color.Alpha) / 100000L);
        return PptxStyleResolverCore.ResolveDirectColor(transforms);
    }

    internal static void ResolveFillColors(PptxTheme theme, PptxFill fill, PptxColor placeholder)
    {
        if (fill.Type == PptxFill.Solid)
        {
            fill.Color = ResolveReferenceColor(theme, fill.Color, placeholder);
        }

        if (fill.Type == PptxFill.Gradient)
        {
            foreach (PptxGradientStop stop in fill.GradientStops)
            {
                stop.Color = ResolveReferenceColor(theme, stop.Color, placeholder);
            }

            if (fill.GradientStops.Count > 0)
            {
                fill.Color = fill.GradientStops[0].Color;
                fill.SecondaryColor = fill.GradientStops[fill.GradientStops.Count - 1].Color;
            }
            else
            {
                fill.Color = ResolveReferenceColor(theme, fill.Color, placeholder);
                fill.SecondaryColor = ResolveReferenceColor(theme, fill.SecondaryColor, placeholder);
            }
        }

        if (fill.Type == PptxFill.Pattern)
        {
            fill.Color = ResolveReferenceColor(theme, fill.Color, placeholder);
            fill.SecondaryColor = ResolveReferenceColor(theme, fill.SecondaryColor, placeholder);
        }
    }

    internal static PptxFill CloneFill(PptxFill fill)
    {
        XmlDocument document = new XmlDocument();
        XmlElement element = fill.ToElement(document);
        return PptxFill.FromElement(element);
    }

    internal static PptxLine CloneLine(PptxLine line)
    {
        XmlDocument document = new XmlDocument();
        XmlElement element = line.ToElement(document);
        return PptxLine.FromElement(element);
    }

    internal static PptxEffect CloneEffect(PptxEffect effect)
    {
        XmlDocument document = new XmlDocument();
        XmlElement properties = document.CreateElement("a", "spPr", PptxNamespaces.Drawing);
        document.AppendChild(properties);
        properties.AppendChild(effect.ToElement(document));
        effect.AppendThreeD(document, properties);
        return PptxEffect.FromShapeProperties(properties);
    }

    internal static XmlElement? FindMatchingPlaceholderShape(PptxPart part, XmlElement sourceShape)
    {
        XmlElement? source = PptxXml.FindFirstDescendant(sourceShape, PptxNamespaces.Presentation, "ph");
        if (source == null)
        {
            return null;
        }

        string type = PptxXml.GetAttributeValue(source, "", "type");
        int index = PptxXml.GetIntAttribute(source, "", "idx", -1);
        XmlDocument document = GetPartDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        XmlElement? shapeTree = PptxXml.FindFirstDescendant(root, PptxNamespaces.Presentation, "spTree");
        if (shapeTree == null)
        {
            return null;
        }

        XmlElement? match = FindPlaceholderShapeExact(shapeTree, type, index);
        if (match != null)
        {
            return match;
        }

        return FindPlaceholderShapeType(shapeTree, type);
    }

    internal static XmlElement? FindPlaceholderShapeExact(XmlElement parent, string type, int index)
    {
        for (int childIndex = 0; childIndex < parent.ChildNodes.Count; childIndex++)
        {
            XmlElement? shape = parent.ChildNodes[childIndex] as XmlElement;
            if (shape == null)
            {
                continue;
            }

            if (PptxShapeTree.IsShapeElement(shape))
            {
                XmlElement? placeholder = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "ph");
                if (placeholder != null)
                {
                    int candidateIndex = PptxXml.GetIntAttribute(placeholder, "", "idx", -1);
                    string candidateType = PptxXml.GetAttributeValue(placeholder, "", "type");
                    if (index >= 0 && candidateIndex == index && PlaceholderTypesMatch(type, candidateType))
                    {
                        return shape;
                    }
                }

                XmlElement? nested = FindPlaceholderShapeExact(shape, type, index);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    internal static XmlElement? FindPlaceholderShapeType(XmlElement parent, string type)
    {
        for (int childIndex = 0; childIndex < parent.ChildNodes.Count; childIndex++)
        {
            XmlElement? shape = parent.ChildNodes[childIndex] as XmlElement;
            if (shape == null)
            {
                continue;
            }

            if (PptxShapeTree.IsShapeElement(shape))
            {
                XmlElement? placeholder = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "ph");
                if (placeholder != null && PlaceholderTypesMatch(type, PptxXml.GetAttributeValue(placeholder, "", "type")))
                {
                    return shape;
                }

                XmlElement? nested = FindPlaceholderShapeType(shape, type);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    internal static bool PlaceholderTypesMatch(string left, string right)
    {
        string cleanLeft = left;
        string cleanRight = right;
        if (cleanLeft.Length == 0)
        {
            cleanLeft = "obj";
        }

        if (cleanRight.Length == 0)
        {
            cleanRight = "obj";
        }

        if (cleanLeft == cleanRight)
        {
            return true;
        }

        if ((cleanLeft == "title" || cleanLeft == "ctrTitle") && (cleanRight == "title" || cleanRight == "ctrTitle"))
        {
            return true;
        }

        return false;
    }

    internal static XmlDocument GetPartDocument(PptxPart part)
    {
        if (part.HasXmlDocument)
        {
            return part.XmlDocument;
        }

        return PptxXml.LoadBytes(part.GetBytes());
    }

    internal static void ResolveShapeText(PptxEffectiveStyle result, PptxTheme theme, PptxShape shape, XmlElement? layoutShape, XmlElement? masterShape)
    {
        PptxFont font = BuildTextFont(theme, shape, layoutShape, masterShape, 0, null, null);
        result.Font = font;
        result.TextColor = font.FillColor;
        result.TextSource = "inherited";
    }

    internal static PptxEffectiveStyle ResolveRun(PptxRun run)
    {
        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        PptxShape shape = run.Paragraph.TextFrame.Shape;
        PptxEffectiveStyle result = ResolveShape(shape);
        XmlElement? layoutShape = null;
        XmlElement? masterShape = null;
        PptxSlide slide = shape.Tree.Slide;
        if (slide.Layout != null)
        {
            layoutShape = FindMatchingPlaceholderShape(slide.Layout.Part, shape.Element);
            if (slide.Layout.Master != null)
            {
                masterShape = FindMatchingPlaceholderShape(slide.Layout.Master.Part, shape.Element);
            }
        }

        PptxTheme theme = GetThemeForShape(shape);
        result.Font = BuildTextFont(theme, shape, layoutShape, masterShape, run.Paragraph.Level, run.Paragraph.Element, run.Element);
        result.TextColor = result.Font.FillColor;
        if (run.PropertiesXml.Length > 0)
        {
            result.TextSource = "run";
        }
        else
        {
            result.TextSource = "inherited";
        }

        return result;
    }

    internal static PptxFont BuildTextFont(PptxTheme theme, PptxShape shape, XmlElement? layoutShape, XmlElement? masterShape, int level, XmlElement? paragraph, XmlElement? run)
    {
        PptxFont font = new PptxFont();
        font.Family = theme.MinorLatinFont;
        font.FillColor = PptxStyleResolverCore.ResolveTextColor(theme);
        ApplyPresentationTextStyle(font, shape, level, theme);
        ApplyMasterTextStyle(font, shape, level, theme);
        if (masterShape != null)
        {
            ApplyShapeTextLayer(font, masterShape, level, theme);
        }

        if (layoutShape != null)
        {
            ApplyShapeTextLayer(font, layoutShape, level, theme);
        }

        ApplyShapeTextLayer(font, shape.Element, level, theme);
        if (paragraph != null)
        {
            XmlElement? paragraphProperties = PptxXml.FindFirstChild(paragraph, PptxNamespaces.Drawing, "pPr");
            if (paragraphProperties != null)
            {
                ApplyRunProperties(font, PptxXml.FindFirstChild(paragraphProperties, PptxNamespaces.Drawing, "defRPr"), theme);
            }

            ApplyRunProperties(font, PptxXml.FindFirstChild(paragraph, PptxNamespaces.Drawing, "endParaRPr"), theme);
        }

        if (run != null)
        {
            ApplyRunProperties(font, PptxXml.FindFirstChild(run, PptxNamespaces.Drawing, "rPr"), theme);
        }

        ResolveFontFamily(font, theme);
        if (font.FillColor.Type != PptxColor.Rgb)
        {
            font.FillColor = PptxStyleResolverCore.ResolveColor(theme, font.FillColor);
        }

        return font;
    }

    internal static void ApplyPresentationTextStyle(PptxFont font, PptxShape shape, int level, PptxTheme theme)
    {
        PptxPresentation? presentation = shape.Tree.Slide.Presentation;
        if (presentation == null || presentation.Part == null)
        {
            return;
        }

        XmlDocument document = GetPartDocument(presentation.Part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? style = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "defaultTextStyle");
        ApplyParagraphStyle(font, style, level, theme);
    }

    internal static void ApplyMasterTextStyle(PptxFont font, PptxShape shape, int level, PptxTheme theme)
    {
        PptxSlideLayout? layout = shape.Tree.Slide.Layout;
        if (layout == null || layout.Master == null)
        {
            return;
        }

        XmlDocument document = GetPartDocument(layout.Master.Part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? styles = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "txStyles");
        if (styles == null)
        {
            return;
        }

        string placeholderType = GetPlaceholderType(shape.Element);
        string styleName = "otherStyle";
        if (placeholderType == "title" || placeholderType == "ctrTitle")
        {
            styleName = "titleStyle";
        }
        else if (placeholderType == "body" || placeholderType == "subTitle")
        {
            styleName = "bodyStyle";
        }

        ApplyParagraphStyle(font, PptxXml.FindFirstChild(styles, PptxNamespaces.Presentation, styleName), level, theme);
    }

    internal static void ApplyShapeTextLayer(PptxFont font, XmlElement shape, int level, PptxTheme theme)
    {
        XmlElement? fontReference = FindStyleReference(shape, "fontRef");
        if (fontReference != null)
        {
            ApplyFontReference(font, fontReference, theme);
        }

        XmlElement? textBody = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "txBody");
        if (textBody == null)
        {
            return;
        }

        XmlElement? listStyle = PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "lstStyle");
        ApplyParagraphStyle(font, listStyle, level, theme);
        XmlElement? firstParagraph = PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "p");
        if (firstParagraph != null)
        {
            XmlElement? paragraphProperties = PptxXml.FindFirstChild(firstParagraph, PptxNamespaces.Drawing, "pPr");
            if (paragraphProperties != null)
            {
                ApplyRunProperties(font, PptxXml.FindFirstChild(paragraphProperties, PptxNamespaces.Drawing, "defRPr"), theme);
            }
        }
    }

    internal static void ApplyParagraphStyle(PptxFont font, XmlElement? style, int level, PptxTheme theme)
    {
        if (style == null)
        {
            return;
        }

        XmlElement? defaultParagraph = PptxXml.FindFirstChild(style, PptxNamespaces.Drawing, "defPPr");
        if (defaultParagraph != null)
        {
            ApplyRunProperties(font, PptxXml.FindFirstChild(defaultParagraph, PptxNamespaces.Drawing, "defRPr"), theme);
        }

        string levelName = "lvl" + (level + 1).ToString() + "pPr";
        XmlElement? levelProperties = PptxXml.FindFirstChild(style, PptxNamespaces.Drawing, levelName);
        if (levelProperties != null)
        {
            ApplyRunProperties(font, PptxXml.FindFirstChild(levelProperties, PptxNamespaces.Drawing, "defRPr"), theme);
        }
    }

    internal static void ApplyRunProperties(PptxFont font, XmlElement? properties, PptxTheme theme)
    {
        if (properties == null)
        {
            return;
        }

        if (properties.HasAttribute("lang"))
        {
            font.Language = PptxXml.GetAttributeValue(properties, "", "lang");
        }

        if (properties.HasAttribute("sz"))
        {
            font.SizePoints = PptxXml.GetIntAttribute(properties, "", "sz", 0) / 100.0;
        }

        if (properties.HasAttribute("b"))
        {
            font.Bold = PptxXml.GetBoolAttribute(properties, "", "b", false);
        }

        if (properties.HasAttribute("i"))
        {
            font.Italic = PptxXml.GetBoolAttribute(properties, "", "i", false);
        }

        if (properties.HasAttribute("u"))
        {
            string underline = PptxXml.GetAttributeValue(properties, "", "u");
            font.Underline = underline.Length > 0 && underline != "none";
        }

        if (properties.HasAttribute("strike"))
        {
            string strike = PptxXml.GetAttributeValue(properties, "", "strike");
            font.Strike = strike.Length > 0 && strike != "noStrike";
        }

        if (properties.HasAttribute("baseline"))
        {
            font.Baseline = PptxXml.GetIntAttribute(properties, "", "baseline", 0);
        }

        XmlElement? latin = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "latin");
        if (latin != null)
        {
            font.Family = PptxXml.GetAttributeValue(latin, "", "typeface");
        }

        XmlElement? eastAsian = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "ea");
        if (eastAsian != null)
        {
            font.EastAsianFamily = PptxXml.GetAttributeValue(eastAsian, "", "typeface");
        }

        XmlElement? complexScript = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "cs");
        if (complexScript != null)
        {
            font.ComplexScriptFamily = PptxXml.GetAttributeValue(complexScript, "", "typeface");
        }

        XmlElement? solidFill = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "solidFill");
        if (solidFill != null)
        {
            font.FillColor = PptxStyleResolverCore.ResolveColor(theme, PptxColor.FromParent(solidFill));
        }
    }

    internal static void ApplyFontReference(PptxFont font, XmlElement reference, PptxTheme theme)
    {
        string role = PptxXml.GetAttributeValue(reference, "", "idx");
        if (role == "major")
        {
            font.Family = theme.MajorLatinFont;
        }
        else if (role == "minor")
        {
            font.Family = theme.MinorLatinFont;
        }

        PptxColor color = PptxColor.FromParent(reference);
        if (color.Type != PptxColor.Empty)
        {
            font.FillColor = PptxStyleResolverCore.ResolveColor(theme, color);
        }
    }

    internal static void ResolveFontFamily(PptxFont font, PptxTheme theme)
    {
        if (font.Family == "+mj-lt")
        {
            font.Family = theme.MajorLatinFont;
        }
        else if (font.Family == "+mn-lt" || font.Family.Length == 0)
        {
            font.Family = theme.MinorLatinFont;
        }
    }

    internal static string GetPlaceholderType(XmlElement shape)
    {
        XmlElement? placeholder = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "ph");
        if (placeholder == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(placeholder, "", "type");
    }

    internal static PptxEffectiveStyle ResolveTableCell(PptxTableCell cell)
    {
        if (cell == null)
        {
            throw new System.ArgumentNullException("cell");
        }

        PptxTable table = cell.Row.Table;
        PptxEffectiveStyle result = ResolveShape(table.Shape);
        PptxTheme theme = GetThemeForShape(table.Shape);
        XmlElement? tableStyle = FindTableStyle(table);
        if (tableStyle != null)
        {
            ApplyTableRegion(result, theme, tableStyle, "wholeTbl");
            if (table.GetBandedRows())
            {
                if (cell.RowIndex % 2 == 0)
                {
                    ApplyTableRegion(result, theme, tableStyle, "band1H");
                }
                else
                {
                    ApplyTableRegion(result, theme, tableStyle, "band2H");
                }
            }

            if (table.GetBandedColumns())
            {
                if (cell.ColumnIndex % 2 == 0)
                {
                    ApplyTableRegion(result, theme, tableStyle, "band1V");
                }
                else
                {
                    ApplyTableRegion(result, theme, tableStyle, "band2V");
                }
            }

            if (table.GetFirstRow() && cell.RowIndex == 0)
            {
                ApplyTableRegion(result, theme, tableStyle, "firstRow");
            }

            if (table.GetLastRow() && cell.RowIndex == table.RowCount() - 1)
            {
                ApplyTableRegion(result, theme, tableStyle, "lastRow");
            }

            if (table.GetFirstColumn() && cell.ColumnIndex == 0)
            {
                ApplyTableRegion(result, theme, tableStyle, "firstCol");
            }

            if (table.GetLastColumn() && cell.ColumnIndex == table.ColumnCount() - 1)
            {
                ApplyTableRegion(result, theme, tableStyle, "lastCol");
            }

            ApplyTableCornerRegion(result, theme, tableStyle, table, cell);
        }

        PptxFill directFill = cell.GetFill();
        if (directFill.Present)
        {
            result.Fill = PptxStyleResolverCore.ResolveFill(theme, directFill);
            result.FillSource = "cell";
        }

        PptxLine directLine = FindCellBorder(cell);
        if (directLine.Present)
        {
            result.Line = PptxStyleResolverCore.ResolveLine(theme, directLine);
            result.LineSource = "cell";
        }

        XmlElement? directRunProperties = PptxXml.FindFirstDescendant(cell.Element, PptxNamespaces.Drawing, "rPr");
        ApplyRunProperties(result.Font, directRunProperties, theme);
        ResolveFontFamily(result.Font, theme);
        result.TextColor = result.Font.FillColor;
        return result;
    }

    internal static XmlElement? FindTableStyle(PptxTable table)
    {
        PptxPresentation? presentation = table.Shape.Tree.Slide.Presentation;
        if (presentation == null || presentation.TableStylesPart == null)
        {
            return null;
        }

        string styleId = table.GetStyleId();
        XmlDocument document = GetPartDocument(presentation.TableStylesPart);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlElement? style = root.ChildNodes[index] as XmlElement;
            if (style != null && style.NamespaceURI == PptxNamespaces.Drawing && style.LocalName == "tblStyle" && PptxXml.GetAttributeValue(style, "", "styleId") == styleId)
            {
                return style;
            }
        }

        return null;
    }

    internal static void ApplyTableRegion(PptxEffectiveStyle result, PptxTheme theme, XmlElement tableStyle, string regionName)
    {
        XmlElement? region = PptxXml.FindFirstChild(tableStyle, PptxNamespaces.Drawing, regionName);
        if (region == null)
        {
            return;
        }

        XmlElement? cellStyle = PptxXml.FindFirstChild(region, PptxNamespaces.Drawing, "tcStyle");
        if (cellStyle != null)
        {
            XmlElement? fillContainer = PptxXml.FindFirstChild(cellStyle, PptxNamespaces.Drawing, "fill");
            if (fillContainer == null)
            {
                fillContainer = cellStyle;
            }

            PptxFill fill = PptxFill.FromShapeProperties(fillContainer);
            if (fill.Present)
            {
                result.Fill = PptxStyleResolverCore.ResolveFill(theme, fill);
                result.FillSource = "table-style";
            }

            XmlElement? borders = PptxXml.FindFirstChild(cellStyle, PptxNamespaces.Drawing, "tcBdr");
            PptxLine line = ReadFirstBorder(borders);
            if (line.Present)
            {
                result.Line = PptxStyleResolverCore.ResolveLine(theme, line);
                result.LineSource = "table-style";
            }
        }

        XmlElement? textStyle = PptxXml.FindFirstChild(region, PptxNamespaces.Drawing, "tcTxStyle");
        if (textStyle != null)
        {
            if (textStyle.HasAttribute("b"))
            {
                result.Font.Bold = PptxXml.GetBoolAttribute(textStyle, "", "b", false);
            }

            if (textStyle.HasAttribute("i"))
            {
                result.Font.Italic = PptxXml.GetBoolAttribute(textStyle, "", "i", false);
            }

            XmlElement? fontReference = PptxXml.FindFirstChild(textStyle, PptxNamespaces.Drawing, "fontRef");
            if (fontReference != null)
            {
                ApplyFontReference(result.Font, fontReference, theme);
            }

            PptxColor textColor = PptxColor.FromParent(textStyle);
            if (textColor.Type != PptxColor.Empty)
            {
                result.Font.FillColor = PptxStyleResolverCore.ResolveColor(theme, textColor);
            }

            result.TextColor = result.Font.FillColor;
            result.TextSource = "table-style";
        }
    }

    internal static void ApplyTableCornerRegion(PptxEffectiveStyle result, PptxTheme theme, XmlElement tableStyle, PptxTable table, PptxTableCell cell)
    {
        bool firstRow = cell.RowIndex == 0;
        bool lastRow = cell.RowIndex == table.RowCount() - 1;
        bool firstColumn = cell.ColumnIndex == 0;
        bool lastColumn = cell.ColumnIndex == table.ColumnCount() - 1;
        if (firstRow && firstColumn)
        {
            ApplyTableRegion(result, theme, tableStyle, "nwCell");
        }

        if (firstRow && lastColumn)
        {
            ApplyTableRegion(result, theme, tableStyle, "neCell");
        }

        if (lastRow && firstColumn)
        {
            ApplyTableRegion(result, theme, tableStyle, "swCell");
        }

        if (lastRow && lastColumn)
        {
            ApplyTableRegion(result, theme, tableStyle, "seCell");
        }
    }

    internal static PptxLine FindCellBorder(PptxTableCell cell)
    {
        PptxLine line = cell.GetBorder(PptxTableCell.BorderLeft);
        if (line.Present)
        {
            return line;
        }

        line = cell.GetBorder(PptxTableCell.BorderTop);
        if (line.Present)
        {
            return line;
        }

        line = cell.GetBorder(PptxTableCell.BorderRight);
        if (line.Present)
        {
            return line;
        }

        return cell.GetBorder(PptxTableCell.BorderBottom);
    }

    internal static PptxLine ReadFirstBorder(XmlElement? borders)
    {
        if (borders == null)
        {
            return new PptxLine();
        }

        string[] names = new string[] { "lnL", "lnT", "lnR", "lnB", "insideH", "insideV" };
        foreach (string name in names)
        {
            XmlElement? line = PptxXml.FindFirstChild(borders, PptxNamespaces.Drawing, name);
            if (line != null)
            {
                return PptxLine.FromElement(line);
            }
        }

        return new PptxLine();
    }

    internal static PptxEffectiveStyle ResolveChart(PptxChart chart)
    {
        if (chart == null)
        {
            throw new System.ArgumentNullException("chart");
        }

        PptxEffectiveStyle result = ResolveShape(chart.Shape);
        PptxTheme theme = GetThemeForShape(chart.Shape);
        if (!chart.HasPart())
        {
            return result;
        }

        XmlDocument document = chart.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return result;
        }

        XmlElement? properties = PptxXml.FindFirstChild(root, PptxNamespaces.Chart, "spPr");
        if (properties != null)
        {
            PptxFill fill = PptxFill.FromShapeProperties(properties);
            if (fill.Present)
            {
                result.Fill = PptxStyleResolverCore.ResolveFill(theme, fill);
                result.FillSource = "chart";
            }

            PptxLine line = PptxLine.FromShapeProperties(properties);
            if (line.Present)
            {
                result.Line = PptxStyleResolverCore.ResolveLine(theme, line);
                result.LineSource = "chart";
            }

            if (HasEffectElement(properties))
            {
                result.Effect = PptxStyleResolverCore.ResolveEffect(theme, PptxEffect.FromShapeProperties(properties));
                result.EffectSource = "chart";
            }
        }

        XmlElement? textProperties = PptxXml.FindFirstDescendant(root, PptxNamespaces.Drawing, "defRPr");
        if (textProperties != null)
        {
            ApplyRunProperties(result.Font, textProperties, theme);
            ResolveFontFamily(result.Font, theme);
            result.TextColor = result.Font.FillColor;
            result.TextSource = "chart";
        }

        PptxPart? colorPart = chart.GetColorStylePart();
        if (colorPart != null)
        {
            XmlDocument colorDocument = GetPartDocument(colorPart);
            XmlElement? colorRoot = colorDocument.DocumentElement;
            if (colorRoot != null)
            {
                XmlElement? colorElement = FindFirstColorElement(colorRoot);
                if (colorElement != null)
                {
                    result.TextColor = PptxStyleResolverCore.ResolveColor(theme, PptxColor.FromElement(colorElement));
                    result.Font.FillColor = result.TextColor;
                    result.TextSource = "chart-color-style";
                }
            }
        }

        return result;
    }

    internal static XmlElement? FindFirstColorElement(XmlElement parent)
    {
        if (PptxColor.IsColorElement(parent))
        {
            return parent;
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null)
            {
                XmlElement? color = FindFirstColorElement(child);
                if (color != null)
                {
                    return color;
                }
            }
        }

        return null;
    }
}
