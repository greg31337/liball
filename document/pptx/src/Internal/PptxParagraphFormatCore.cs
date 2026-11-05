using System.Collections.Generic;
using System.Xml;

namespace pptx;

internal static class PptxParagraphFormatCore
{
    internal static PptxParagraphFormat GetFormat(PptxParagraph paragraph)
    {
        PptxParagraphFormat format = new PptxParagraphFormat();
        XmlElement? properties = GetProperties(paragraph);
        if (properties == null)
        {
            return format;
        }

        format.Alignment = PptxXml.GetAttributeValue(properties, "", "algn");
        format.Level = PptxXml.GetIntAttribute(properties, "", "lvl", 0);
        format.MarginLeft = PptxTransform.GetLongAttribute(properties, "marL", 0L);
        format.MarginRight = PptxTransform.GetLongAttribute(properties, "marR", 0L);
        format.Indent = PptxTransform.GetLongAttribute(properties, "indent", 0L);
        format.DefaultTabSize = PptxTransform.GetLongAttribute(properties, "defTabSz", 0L);
        format.LineSpacing = ReadSpacing(properties, "lnSpc");
        format.SpaceBefore = ReadSpacing(properties, "spcBef");
        format.SpaceAfter = ReadSpacing(properties, "spcAft");
        format.Tabs = ReadTabs(properties);
        format.DefaultFont = ReadDefaultFont(properties);
        ReadBullet(format, properties);
        return format;
    }

    internal static void SetAlignment(PptxParagraph paragraph, string alignment)
    {
        string normalized = NormalizeAlignment(alignment);
        XmlElement properties = EnsureProperties(paragraph);
        if (normalized.Length == 0)
        {
            properties.RemoveAttribute("algn");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "algn", normalized);
        }

        MarkChanged(paragraph);
    }

    internal static void SetIndentation(PptxParagraph paragraph, int level, long marginLeft, long marginRight, long indent)
    {
        if (level < 0 || level > 8)
        {
            throw new System.ArgumentOutOfRangeException("level");
        }

        if (marginLeft < 0L)
        {
            throw new System.ArgumentOutOfRangeException("marginLeft");
        }

        if (marginRight < 0L)
        {
            throw new System.ArgumentOutOfRangeException("marginRight");
        }

        XmlElement properties = EnsureProperties(paragraph);
        PptxXml.SetAttributeValue(properties, "", "", "lvl", level.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "marL", marginLeft.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "marR", marginRight.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "indent", indent.ToString());
        MarkChanged(paragraph);
    }

    internal static void SetSpacing(PptxParagraph paragraph, PptxTextSpacing line, PptxTextSpacing before, PptxTextSpacing after)
    {
        ValidateSpacing(line, "line");
        ValidateSpacing(before, "before");
        ValidateSpacing(after, "after");
        XmlElement properties = EnsureProperties(paragraph);
        WriteSpacing(properties, "lnSpc", line);
        WriteSpacing(properties, "spcBef", before);
        WriteSpacing(properties, "spcAft", after);
        MarkChanged(paragraph);
    }

    internal static void SetTabs(PptxParagraph paragraph, List<PptxTextTab> tabs)
    {
        if (tabs == null)
        {
            throw new System.ArgumentNullException("tabs");
        }

        for (int index = 0; index < tabs.Count; index++)
        {
            if (tabs[index] == null)
            {
                throw new System.ArgumentException("Tab stops cannot contain null values.", "tabs");
            }

            NormalizeTabAlignment(tabs[index].Alignment);
        }

        XmlElement properties = EnsureProperties(paragraph);
        RemoveDirectChild(properties, "tabLst");
        if (tabs.Count > 0)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            XmlElement list = document.CreateElement("a", "tabLst", PptxNamespaces.Drawing);
            for (int index = 0; index < tabs.Count; index++)
            {
                PptxTextTab item = tabs[index];
                XmlElement tab = document.CreateElement("a", "tab", PptxNamespaces.Drawing);
                PptxXml.SetAttributeValue(tab, "", "", "pos", item.Position.ToString());
                PptxXml.SetAttributeValue(tab, "", "", "algn", NormalizeTabAlignment(item.Alignment));
                list.AppendChild(tab);
            }

            InsertOrderedChild(properties, list);
        }

        MarkChanged(paragraph);
    }

    internal static void SetDefaultTabSize(PptxParagraph paragraph, long size)
    {
        if (size < 0L)
        {
            throw new System.ArgumentOutOfRangeException("size");
        }

        XmlElement properties = EnsureProperties(paragraph);
        PptxXml.SetAttributeValue(properties, "", "", "defTabSz", size.ToString());
        MarkChanged(paragraph);
    }

    internal static void SetDefaultRunProperties(PptxParagraph paragraph, PptxFont font)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        PptxFont normalized = PptxRunFormatCore.NormalizeFont(font);
        XmlElement properties = EnsureProperties(paragraph);
        XmlElement? defaultProperties = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "defRPr");
        if (defaultProperties == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            defaultProperties = document.CreateElement("a", "defRPr", PptxNamespaces.Drawing);
            InsertOrderedChild(properties, defaultProperties);
        }

        WriteDefaultFont(defaultProperties, normalized);
        MarkChanged(paragraph);
    }

    internal static void SetCharacterBullet(PptxParagraph paragraph, string character)
    {
        string clean = PptxDiagnostic.CleanText(character);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("A bullet character is required.", "character");
        }

        XmlElement properties = EnsureProperties(paragraph);
        RemoveBulletKind(properties);
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement bullet = document.CreateElement("a", "buChar", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(bullet, "", "", "char", clean);
        InsertOrderedChild(properties, bullet);
        MarkChanged(paragraph);
    }

    internal static void SetAutomaticNumbering(PptxParagraph paragraph, string numberingType, int startAt)
    {
        string clean = PptxDiagnostic.CleanText(numberingType);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("A numbering type is required.", "numberingType");
        }

        if (startAt < 1 || startAt > 32767)
        {
            throw new System.ArgumentOutOfRangeException("startAt");
        }

        XmlElement properties = EnsureProperties(paragraph);
        RemoveBulletKind(properties);
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement bullet = document.CreateElement("a", "buAutoNum", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(bullet, "", "", "type", clean);
        PptxXml.SetAttributeValue(bullet, "", "", "startAt", startAt.ToString());
        InsertOrderedChild(properties, bullet);
        MarkChanged(paragraph);
    }

    internal static void ClearBullet(PptxParagraph paragraph)
    {
        XmlElement properties = EnsureProperties(paragraph);
        RemoveBulletKind(properties);
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        InsertOrderedChild(properties, document.CreateElement("a", "buNone", PptxNamespaces.Drawing));
        MarkChanged(paragraph);
    }

    internal static XmlElement? GetProperties(PptxParagraph paragraph)
    {
        return PptxXml.FindFirstChild(paragraph.Element, PptxNamespaces.Drawing, "pPr");
    }

    internal static XmlElement EnsureProperties(PptxParagraph paragraph)
    {
        XmlElement? properties = GetProperties(paragraph);
        if (properties != null)
        {
            return properties;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(paragraph.Element);
        XmlElement created = document.CreateElement("a", "pPr", PptxNamespaces.Drawing);
        paragraph.Element.InsertBefore(created, paragraph.Element.FirstChild);
        return created;
    }

    internal static void MarkChanged(PptxParagraph paragraph)
    {
        paragraph.TextFrame.Shape.Tree.MarkDirty();
        paragraph.Load();
        paragraph.TextFrame.Load();
    }

    internal static PptxTextSpacing ReadSpacing(XmlElement properties, string localName)
    {
        PptxTextSpacing spacing = new PptxTextSpacing();
        XmlElement? container = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, localName);
        if (container == null)
        {
            return spacing;
        }

        XmlElement? percent = PptxXml.FindFirstChild(container, PptxNamespaces.Drawing, "spcPct");
        if (percent != null)
        {
            spacing.Unit = PptxTextSpacing.Percent;
            spacing.Value = PptxXml.GetIntAttribute(percent, "", "val", 0);
            return spacing;
        }

        XmlElement? points = PptxXml.FindFirstChild(container, PptxNamespaces.Drawing, "spcPts");
        if (points != null)
        {
            spacing.Unit = PptxTextSpacing.Points;
            spacing.Value = PptxXml.GetIntAttribute(points, "", "val", 0);
        }

        return spacing;
    }

    internal static void ValidateSpacing(PptxTextSpacing spacing, string parameterName)
    {
        if (spacing == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        NormalizeSpacingUnit(spacing.Unit);
        if (spacing.Value < 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static void WriteSpacing(XmlElement properties, string localName, PptxTextSpacing spacing)
    {
        RemoveDirectChild(properties, localName);
        string unit = NormalizeSpacingUnit(spacing.Unit);
        if (unit.Length == 0)
        {
            return;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement container = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        string valueName = "spcPct";
        if (unit == PptxTextSpacing.Points)
        {
            valueName = "spcPts";
        }

        XmlElement value = document.CreateElement("a", valueName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(value, "", "", "val", spacing.Value.ToString());
        container.AppendChild(value);
        InsertOrderedChild(properties, container);
    }

    internal static List<PptxTextTab> ReadTabs(XmlElement properties)
    {
        List<PptxTextTab> tabs = new List<PptxTextTab>();
        XmlElement? list = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "tabLst");
        if (list == null)
        {
            return tabs;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlElement? element = list.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "tab")
            {
                PptxTextTab tab = new PptxTextTab();
                tab.Position = PptxTransform.GetLongAttribute(element, "pos", 0L);
                tab.Alignment = PptxXml.GetAttributeValue(element, "", "algn");
                tabs.Add(tab);
            }
        }

        return tabs;
    }

    internal static PptxFont ReadDefaultFont(XmlElement properties)
    {
        PptxFont font = new PptxFont();
        XmlElement? defaultProperties = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "defRPr");
        if (defaultProperties == null)
        {
            return font;
        }

        font.Language = PptxXml.GetAttributeValue(defaultProperties, "", "lang");
        font.AlternativeLanguage = PptxXml.GetAttributeValue(defaultProperties, "", "altLang");
        int size = PptxXml.GetIntAttribute(defaultProperties, "", "sz", 0);
        if (size > 0)
        {
            font.SizePoints = (double)size / 100.0;
        }

        font.Bold = PptxXml.GetBoolAttribute(defaultProperties, "", "b", false);
        font.Italic = PptxXml.GetBoolAttribute(defaultProperties, "", "i", false);
        font.UnderlineStyle = PptxXml.GetAttributeValue(defaultProperties, "", "u");
        font.Underline = font.UnderlineStyle.Length > 0 && font.UnderlineStyle != "none";
        font.StrikeStyle = PptxXml.GetAttributeValue(defaultProperties, "", "strike");
        font.Strike = font.StrikeStyle.Length > 0 && font.StrikeStyle != "noStrike";
        font.Baseline = PptxXml.GetIntAttribute(defaultProperties, "", "baseline", 0);
        font.Capitalization = PptxXml.GetAttributeValue(defaultProperties, "", "cap");
        font.Kerning = PptxXml.GetIntAttribute(defaultProperties, "", "kern", 0);
        font.Spacing = PptxXml.GetIntAttribute(defaultProperties, "", "spc", 0);
        font.Family = ReadTypeface(defaultProperties, "latin");
        font.EastAsianFamily = ReadTypeface(defaultProperties, "ea");
        font.ComplexScriptFamily = ReadTypeface(defaultProperties, "cs");
        font.SymbolFamily = ReadTypeface(defaultProperties, "sym");
        font.Fill = PptxFill.FromShapeProperties(defaultProperties);
        if (font.Fill.Type == PptxFill.Solid)
        {
            font.FillColor = font.Fill.Color;
        }

        XmlElement? highlight = PptxXml.FindFirstChild(defaultProperties, PptxNamespaces.Drawing, "highlight");
        if (highlight != null)
        {
            font.HighlightColor = PptxColor.FromParent(highlight);
        }

        return font;
    }

    internal static string ReadTypeface(XmlElement properties, string localName)
    {
        XmlElement? typeface = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, localName);
        if (typeface == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(typeface, "", "typeface");
    }

    internal static void ReadBullet(PptxParagraphFormat format, XmlElement properties)
    {
        if (PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buNone") != null)
        {
            format.BulletType = PptxBullet.None;
            return;
        }

        XmlElement? character = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buChar");
        if (character != null)
        {
            format.BulletType = PptxBullet.Character;
            format.BulletCharacter = PptxXml.GetAttributeValue(character, "", "char");
            return;
        }

        XmlElement? automatic = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buAutoNum");
        if (automatic != null)
        {
            format.BulletType = PptxBullet.Number;
            format.NumberingType = PptxXml.GetAttributeValue(automatic, "", "type");
            format.NumberingStartAt = PptxXml.GetIntAttribute(automatic, "", "startAt", 1);
            return;
        }

        XmlElement? picture = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buBlip");
        if (picture != null)
        {
            format.BulletType = PptxBullet.Picture;
            XmlElement? blip = PptxXml.FindFirstDescendant(picture, PptxNamespaces.Drawing, "blip");
            if (blip != null)
            {
                format.PictureBulletRelationshipId = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "embed");
                if (format.PictureBulletRelationshipId.Length == 0)
                {
                    format.PictureBulletRelationshipId = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "link");
                }
            }
        }
    }

    internal static void WriteDefaultFont(XmlElement properties, PptxFont font)
    {
        SetOptionalAttribute(properties, "lang", font.Language);
        SetOptionalAttribute(properties, "altLang", font.AlternativeLanguage);
        if (font.SizePoints > 0.0)
        {
            int size = (int)(font.SizePoints * 100.0);
            PptxXml.SetAttributeValue(properties, "", "", "sz", size.ToString());
        }
        else
        {
            properties.RemoveAttribute("sz");
        }

        PptxXml.SetAttributeValue(properties, "", "", "b", PptxTransform.BoolToXml(font.Bold));
        PptxXml.SetAttributeValue(properties, "", "", "i", PptxTransform.BoolToXml(font.Italic));
        string underline = font.UnderlineStyle;
        if (underline.Length == 0)
        {
            underline = font.Underline ? "sng" : "none";
        }

        string strike = font.StrikeStyle;
        if (strike.Length == 0)
        {
            strike = font.Strike ? "sngStrike" : "noStrike";
        }

        PptxXml.SetAttributeValue(properties, "", "", "u", underline);
        PptxXml.SetAttributeValue(properties, "", "", "strike", strike);
        PptxXml.SetAttributeValue(properties, "", "", "baseline", font.Baseline.ToString());
        SetOptionalAttribute(properties, "cap", font.Capitalization);
        if (font.Kerning > 0)
        {
            PptxXml.SetAttributeValue(properties, "", "", "kern", font.Kerning.ToString());
        }
        else
        {
            properties.RemoveAttribute("kern");
        }

        if (font.Spacing != 0)
        {
            PptxXml.SetAttributeValue(properties, "", "", "spc", font.Spacing.ToString());
        }
        else
        {
            properties.RemoveAttribute("spc");
        }

        SetTypeface(properties, "latin", font.Family);
        SetTypeface(properties, "ea", font.EastAsianFamily);
        SetTypeface(properties, "cs", font.ComplexScriptFamily);
        SetTypeface(properties, "sym", font.SymbolFamily);
        RemoveFillChildren(properties);
        if (font.Fill.Present)
        {
            InsertOrderedRunChild(properties, font.Fill.ToElement(PptxTextFrame.GetOwnerDocument(properties)));
        }
        else if (font.FillColor.Type != PptxColor.Empty)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            XmlElement fill = document.CreateElement("a", "solidFill", PptxNamespaces.Drawing);
            fill.AppendChild(font.FillColor.ToElement(document));
            InsertOrderedRunChild(properties, fill);
        }

        if (font.HighlightColor.Type != PptxColor.Empty)
        {
            RemoveDirectRunChild(properties, "highlight");
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            XmlElement highlight = document.CreateElement("a", "highlight", PptxNamespaces.Drawing);
            highlight.AppendChild(font.HighlightColor.ToElement(document));
            InsertOrderedRunChild(properties, highlight);
        }
    }

    internal static void SetOptionalAttribute(XmlElement element, string name, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            element.RemoveAttribute(name);
        }
        else
        {
            PptxXml.SetAttributeValue(element, "", "", name, clean);
        }
    }

    internal static void SetTypeface(XmlElement properties, string localName, string family)
    {
        XmlElement? existing = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, localName);
        string clean = PptxDiagnostic.CleanText(family);
        if (clean.Length == 0)
        {
            if (existing != null)
            {
                properties.RemoveChild(existing);
            }

            return;
        }

        if (existing == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            existing = document.CreateElement("a", localName, PptxNamespaces.Drawing);
            InsertOrderedRunChild(properties, existing);
        }

        PptxXml.SetAttributeValue(existing, "", "", "typeface", clean);
    }

    internal static void RemoveFillChildren(XmlElement properties)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlElement? child = properties.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(child.LocalName))
            {
                properties.RemoveChild(child);
            }
        }
    }

    internal static void RemoveDirectRunChild(XmlElement properties, string localName)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlElement? child = properties.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI == PptxNamespaces.Drawing && child.LocalName == localName)
            {
                properties.RemoveChild(child);
            }
        }
    }

    internal static void RemoveBulletKind(XmlElement properties)
    {
        string[] names = new string[] { "buNone", "buAutoNum", "buChar", "buBlip" };
        for (int index = 0; index < names.Length; index++)
        {
            RemoveDirectChild(properties, names[index]);
        }
    }

    internal static void RemoveDirectChild(XmlElement properties, string localName)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlElement? child = properties.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI == PptxNamespaces.Drawing && child.LocalName == localName)
            {
                properties.RemoveChild(child);
            }
        }
    }

    internal static void InsertOrderedChild(XmlElement properties, XmlElement child)
    {
        int childOrder = GetParagraphChildOrder(child.LocalName);
        for (int index = 0; index < properties.ChildNodes.Count; index++)
        {
            XmlElement? existing = properties.ChildNodes[index] as XmlElement;
            if (existing != null && GetParagraphChildOrder(existing.LocalName) > childOrder)
            {
                properties.InsertBefore(child, existing);
                return;
            }
        }

        properties.AppendChild(child);
    }

    internal static int GetParagraphChildOrder(string localName)
    {
        if (localName == "lnSpc")
        {
            return 10;
        }

        if (localName == "spcBef")
        {
            return 20;
        }

        if (localName == "spcAft")
        {
            return 30;
        }

        if (localName == "buClrTx" || localName == "buClr")
        {
            return 40;
        }

        if (localName == "buSzTx" || localName == "buSzPct" || localName == "buSzPts")
        {
            return 41;
        }

        if (localName == "buFontTx" || localName == "buFont")
        {
            return 42;
        }

        if (localName == "buNone" || localName == "buAutoNum" || localName == "buChar" || localName == "buBlip")
        {
            return 43;
        }

        if (localName == "tabLst")
        {
            return 50;
        }

        if (localName == "defRPr")
        {
            return 60;
        }

        if (localName == "extLst")
        {
            return 100;
        }

        return 90;
    }

    internal static void InsertOrderedRunChild(XmlElement properties, XmlElement child)
    {
        int childOrder = GetRunChildOrder(child.LocalName);
        for (int index = 0; index < properties.ChildNodes.Count; index++)
        {
            XmlElement? existing = properties.ChildNodes[index] as XmlElement;
            if (existing != null && GetRunChildOrder(existing.LocalName) > childOrder)
            {
                properties.InsertBefore(child, existing);
                return;
            }
        }

        properties.AppendChild(child);
    }

    internal static int GetRunChildOrder(string localName)
    {
        if (PptxFill.IsFillElement(localName))
        {
            return 10;
        }

        if (localName == "effectLst" || localName == "effectDag")
        {
            return 20;
        }

        if (localName == "highlight")
        {
            return 30;
        }

        if (localName == "latin")
        {
            return 50;
        }

        if (localName == "ea")
        {
            return 51;
        }

        if (localName == "cs")
        {
            return 52;
        }

        if (localName == "sym")
        {
            return 53;
        }

        if (localName == "hlinkClick")
        {
            return 60;
        }

        if (localName == "hlinkMouseOver")
        {
            return 61;
        }

        if (localName == "rtl")
        {
            return 62;
        }

        if (localName == "extLst")
        {
            return 100;
        }

        return 40;
    }

    internal static string NormalizeAlignment(string alignment)
    {
        string clean = PptxDiagnostic.CleanText(alignment).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "left" || clean == "l")
        {
            return "l";
        }

        if (clean == "center" || clean == "centre" || clean == "ctr")
        {
            return "ctr";
        }

        if (clean == "right" || clean == "r")
        {
            return "r";
        }

        if (clean == "justified" || clean == "justify" || clean == "just")
        {
            return "just";
        }

        if (clean == "justlow")
        {
            return "justLow";
        }

        if (clean == "distributed" || clean == "dist")
        {
            return "dist";
        }

        if (clean == "thaidistributed" || clean == "thaidist")
        {
            return "thaiDist";
        }

        throw new System.ArgumentException("Unknown paragraph alignment.", "alignment");
    }

    internal static string NormalizeSpacingUnit(string unit)
    {
        string clean = PptxDiagnostic.CleanText(unit).ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "point" || clean == "points" || clean == "pt" || clean == "pts")
        {
            return PptxTextSpacing.Points;
        }

        if (clean == "percent" || clean == "percentage" || clean == "pct")
        {
            return PptxTextSpacing.Percent;
        }

        throw new System.ArgumentException("Unknown paragraph spacing unit.", "unit");
    }

    internal static string NormalizeTabAlignment(string alignment)
    {
        string clean = PptxDiagnostic.CleanText(alignment).ToLowerInvariant();
        if (clean == "left" || clean == "l")
        {
            return "l";
        }

        if (clean == "center" || clean == "centre" || clean == "ctr")
        {
            return "ctr";
        }

        if (clean == "right" || clean == "r")
        {
            return "r";
        }

        if (clean == "decimal" || clean == "dec")
        {
            return "dec";
        }

        throw new System.ArgumentException("Unknown tab alignment.", "alignment");
    }
}
