using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxRunFormatCore
{
    internal static PptxFont CloneFont(PptxFont font)
    {
        PptxFont clone = new PptxFont();
        clone.Family = font.Family;
        clone.EastAsianFamily = font.EastAsianFamily;
        clone.ComplexScriptFamily = font.ComplexScriptFamily;
        clone.Language = font.Language;
        clone.AlternativeLanguage = font.AlternativeLanguage;
        clone.SizePoints = font.SizePoints;
        clone.Bold = font.Bold;
        clone.Italic = font.Italic;
        clone.Underline = font.Underline;
        clone.Strike = font.Strike;
        clone.Baseline = font.Baseline;
        clone.Capitalization = font.Capitalization;
        clone.Kerning = font.Kerning;
        clone.Spacing = font.Spacing;
        clone.UnderlineStyle = font.UnderlineStyle;
        clone.StrikeStyle = font.StrikeStyle;
        clone.SymbolFamily = font.SymbolFamily;
        clone.FillColor = PptxStyleResolverCore.CloneColor(font.FillColor);
        clone.Fill = CloneFill(font.Fill);
        clone.HighlightColor = PptxStyleResolverCore.CloneColor(font.HighlightColor);
        return clone;
    }

    internal static PptxFill CloneFill(PptxFill fill)
    {
        if (!fill.Present)
        {
            return new PptxFill();
        }

        XmlDocument document = new XmlDocument();
        return PptxFill.FromElement(fill.ToElement(document));
    }

    internal static void SetFont(PptxRun run, PptxFont font)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        PptxFont normalized = NormalizeFont(font);
        XmlElement properties = EnsureProperties(run);
        PptxParagraphFormatCore.WriteDefaultFont(properties, normalized);
        MarkChanged(run);
    }

    internal static void SetFamilies(PptxRun run, string latin, string eastAsian, string complexScript)
    {
        PptxFont font = CloneFont(run.Font);
        font.Family = PptxDiagnostic.CleanText(latin);
        font.EastAsianFamily = PptxDiagnostic.CleanText(eastAsian);
        font.ComplexScriptFamily = PptxDiagnostic.CleanText(complexScript);
        SetFont(run, font);
    }

    internal static void SetSize(PptxRun run, double points)
    {
        if (points < 0.0 || double.IsNaN(points) || double.IsInfinity(points))
        {
            throw new System.ArgumentOutOfRangeException("points");
        }

        PptxFont font = CloneFont(run.Font);
        font.SizePoints = points;
        SetFont(run, font);
    }

    internal static void SetEmphasis(PptxRun run, bool bold, bool italic)
    {
        PptxFont font = CloneFont(run.Font);
        font.Bold = bold;
        font.Italic = italic;
        SetFont(run, font);
    }

    internal static void SetDecorations(PptxRun run, string underline, string strike)
    {
        PptxFont font = CloneFont(run.Font);
        font.UnderlineStyle = NormalizeUnderline(underline);
        font.StrikeStyle = NormalizeStrike(strike);
        font.Underline = font.UnderlineStyle.Length > 0 && font.UnderlineStyle != "none";
        font.Strike = font.StrikeStyle.Length > 0 && font.StrikeStyle != "noStrike";
        SetFont(run, font);
    }

    internal static void SetBaseline(PptxRun run, int baseline)
    {
        if (baseline < -100000 || baseline > 100000)
        {
            throw new System.ArgumentOutOfRangeException("baseline");
        }

        PptxFont font = CloneFont(run.Font);
        font.Baseline = baseline;
        SetFont(run, font);
    }

    internal static void SetCapitalization(PptxRun run, string capitalization)
    {
        PptxFont font = CloneFont(run.Font);
        font.Capitalization = NormalizeCapitalization(capitalization);
        SetFont(run, font);
    }

    internal static void SetKerning(PptxRun run, int kerning)
    {
        if (kerning < 0)
        {
            throw new System.ArgumentOutOfRangeException("kerning");
        }

        PptxFont font = CloneFont(run.Font);
        font.Kerning = kerning;
        SetFont(run, font);
    }

    internal static void SetCharacterSpacing(PptxRun run, int spacing)
    {
        PptxFont font = CloneFont(run.Font);
        font.Spacing = spacing;
        SetFont(run, font);
    }

    internal static void SetLanguage(PptxRun run, string language, string alternativeLanguage)
    {
        PptxFont font = CloneFont(run.Font);
        font.Language = PptxDiagnostic.CleanText(language);
        font.AlternativeLanguage = PptxDiagnostic.CleanText(alternativeLanguage);
        SetFont(run, font);
    }

    internal static void SetFill(PptxRun run, PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        PptxFont font = CloneFont(run.Font);
        font.Fill = CloneFill(fill);
        font.FillColor = new PptxColor();
        if (font.Fill.Type == PptxFill.Solid)
        {
            font.FillColor = PptxStyleResolverCore.CloneColor(font.Fill.Color);
        }

        SetFont(run, font);
    }

    internal static void SetHighlight(PptxRun run, PptxColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        XmlElement properties = EnsureProperties(run);
        PptxParagraphFormatCore.RemoveDirectRunChild(properties, "highlight");
        if (color.Type != PptxColor.Empty)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            XmlElement highlight = document.CreateElement("a", "highlight", PptxNamespaces.Drawing);
            highlight.AppendChild(color.ToElement(document));
            PptxParagraphFormatCore.InsertOrderedRunChild(properties, highlight);
        }

        MarkChanged(run);
    }

    internal static void SetSymbol(PptxRun run, string typeface)
    {
        PptxFont font = CloneFont(run.Font);
        font.SymbolFamily = PptxDiagnostic.CleanText(typeface);
        SetFont(run, font);
    }

    internal static void SetField(PptxRun run, string fieldId, string fieldType)
    {
        if (!run.IsField())
        {
            throw new System.ArgumentException("The run is not a field.", "run");
        }

        string cleanId = PptxDiagnostic.CleanText(fieldId);
        string cleanType = PptxDiagnostic.CleanText(fieldType);
        if (cleanId.Length == 0)
        {
            throw new System.ArgumentException("A field identifier is required.", "fieldId");
        }

        if (cleanType.Length == 0)
        {
            throw new System.ArgumentException("A field type is required.", "fieldType");
        }

        PptxXml.SetAttributeValue(run.Element, "", "", "id", cleanId);
        PptxXml.SetAttributeValue(run.Element, "", "", "type", cleanType);
        MarkChanged(run);
    }

    internal static void SetExternalHyperlink(PptxRun run, string url)
    {
        string clean = PptxDiagnostic.CleanText(url);
        if (clean.Length == 0)
        {
            RemoveHyperlink(run);
            return;
        }

        if (!PptxUriCore.IsExternalTarget(clean))
        {
            throw new System.ArgumentException("Hyperlink target must be external.", "url");
        }

        XmlElement properties = EnsureProperties(run);
        XmlElement? existingHyperlink = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
        XmlElement hyperlink;
        if (existingHyperlink == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
            hyperlink = document.CreateElement("a", "hlinkClick", PptxNamespaces.Drawing);
            PptxParagraphFormatCore.InsertOrderedRunChild(properties, hyperlink);
        }
        else
        {
            hyperlink = existingHyperlink;
        }
        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        PptxRelationship? relationship = run.Paragraph.TextFrame.Shape.Tree.FindRelationshipById(relationshipId);
        if (relationshipId.Length == 0 || relationship == null || relationship.Type != PptxRelationshipTypes.Hyperlink)
        {
            if (relationshipId.Length > 0)
            {
                hyperlink.RemoveAttribute("id", PptxNamespaces.Relationships);
                run.Paragraph.TextFrame.Shape.Tree.RemoveRelationshipIfUnused(relationshipId);
            }

            relationship = run.Paragraph.TextFrame.Shape.Tree.AddExternalRelationship(PptxRelationshipTypes.Hyperlink, clean);
            relationshipId = relationship.Id;
            PptxXml.SetAttributeValue(hyperlink, "r", PptxNamespaces.Relationships, "id", relationshipId);
        }
        else
        {
            run.Paragraph.TextFrame.Shape.Tree.SetRelationshipTarget(relationshipId, clean);
        }

        hyperlink.RemoveAttribute("action");
        MarkChanged(run);
    }

    internal static void SetFileHyperlink(PptxRun run, string fileUri)
    {
        SetExternalHyperlink(run, fileUri);
    }

    internal static void SetSlideHyperlink(PptxRun run, int slideIndex)
    {
        PptxShapeTree tree = run.Paragraph.TextFrame.Shape.Tree;
        if (tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        PptxSlide targetSlide = tree.Slide.Presentation.GetSlide(slideIndex);
        XmlElement properties = EnsureProperties(run);
        XmlElement hyperlink = GetOrCreateHyperlink(properties);
        RemoveHyperlinkRelationshipOnly(run, hyperlink);
        string target = PptxSlideCloner.BuildRelativeTargetPath(tree.Slide.PartName, targetSlide.PartName);
        PptxRelationship relationship = tree.Slide.Presentation.Package.AddRelationship(tree.Slide.PartName, PptxRelationshipTypes.Slide, target, PptxRelationshipTargetMode.Internal);
        PptxXml.SetAttributeValue(hyperlink, "r", PptxNamespaces.Relationships, "id", relationship.Id);
        PptxXml.SetAttributeValue(hyperlink, "", "", "action", "ppaction://hlinksldjump");
        MarkChanged(run);
    }

    internal static void SetNavigationAction(PptxRun run, string navigation)
    {
        SetAction(run, PptxHyperlink.NavigationAction(navigation));
    }

    internal static void SetCustomShowAction(PptxRun run, string customShowName)
    {
        string clean = PptxDiagnostic.CleanText(customShowName);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Custom show name is required.");
        }

        SetAction(run, "ppaction://customshow?id=" + clean);
    }

    internal static void SetAction(PptxRun run, string action)
    {
        string clean = PptxDiagnostic.CleanText(action);
        if (clean.Length == 0)
        {
            RemoveHyperlink(run);
            return;
        }

        XmlElement properties = EnsureProperties(run);
        XmlElement hyperlink = GetOrCreateHyperlink(properties);
        RemoveHyperlinkRelationshipOnly(run, hyperlink);
        PptxXml.SetAttributeValue(hyperlink, "", "", "action", clean);
        MarkChanged(run);
    }

    internal static void RemoveHyperlink(PptxRun run)
    {
        XmlElement? properties = GetProperties(run);
        if (properties == null)
        {
            return;
        }

        XmlElement? hyperlink = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
        if (hyperlink == null)
        {
            return;
        }

        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        properties.RemoveChild(hyperlink);
        if (relationshipId.Length > 0)
        {
            run.Paragraph.TextFrame.Shape.Tree.RemoveRelationshipIfUnused(relationshipId);
        }

        MarkChanged(run);
    }

    internal static XmlElement GetOrCreateHyperlink(XmlElement properties)
    {
        XmlElement? existing = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
        if (existing != null)
        {
            return existing;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement hyperlink = document.CreateElement("a", "hlinkClick", PptxNamespaces.Drawing);
        PptxParagraphFormatCore.InsertOrderedRunChild(properties, hyperlink);
        return hyperlink;
    }

    internal static void RemoveHyperlinkRelationshipOnly(PptxRun run, XmlElement hyperlink)
    {
        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        if (relationshipId.Length > 0)
        {
            hyperlink.RemoveAttribute("id", PptxNamespaces.Relationships);
            run.Paragraph.TextFrame.Shape.Tree.RemoveRelationshipIfUnused(relationshipId);
        }
    }

    internal static string NormalizeTextValue(string text)
    {
        string clean = PptxDiagnostic.CleanText(text).Replace("\r\n", "\n").Replace("\r", "\n");
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < clean.Length; index++)
        {
            char value = clean[index];
            if (char.IsHighSurrogate(value) && index + 1 < clean.Length && char.IsLowSurrogate(clean[index + 1]))
            {
                builder.Append(value);
                builder.Append(clean[index + 1]);
                index = index + 1;
            }
            else if (XmlConvert.IsXmlChar(value))
            {
                builder.Append(value);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    internal static void NormalizeRun(PptxRun run)
    {
        if (run.IsLineBreak() || run.IsTab())
        {
            throw new System.ArgumentException("Line breaks and tabs do not contain editable text.", "run");
        }

        XmlElement value = run.GetOrCreateTextElement();
        value.InnerText = NormalizeTextValue(value.InnerText);
        MarkChanged(run);
    }

    internal static XmlElement? GetProperties(PptxRun run)
    {
        return PptxXml.FindFirstChild(run.Element, PptxNamespaces.Drawing, "rPr");
    }

    internal static XmlElement EnsureProperties(PptxRun run)
    {
        if (run.IsTab())
        {
            throw new System.ArgumentException("Tab runs do not support run properties.", "run");
        }

        XmlElement? properties = GetProperties(run);
        if (properties != null)
        {
            return properties;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(run.Element);
        XmlElement created = document.CreateElement("a", "rPr", PptxNamespaces.Drawing);
        run.Element.InsertBefore(created, run.Element.FirstChild);
        return created;
    }

    internal static void MarkChanged(PptxRun run)
    {
        run.Paragraph.TextFrame.Shape.Tree.MarkDirty();
        run.Load();
        run.Paragraph.TextFrame.Load();
    }

    internal static void ValidateFont(PptxFont font)
    {
        if (font.SizePoints < 0.0 || double.IsNaN(font.SizePoints) || double.IsInfinity(font.SizePoints))
        {
            throw new System.ArgumentOutOfRangeException("font");
        }

        if (font.Kerning < 0)
        {
            throw new System.ArgumentOutOfRangeException("font");
        }

        if (font.Baseline < -100000 || font.Baseline > 100000)
        {
            throw new System.ArgumentOutOfRangeException("font");
        }

        NormalizeCapitalization(font.Capitalization);
        if (font.UnderlineStyle.Length > 0)
        {
            NormalizeUnderline(font.UnderlineStyle);
        }

        if (font.StrikeStyle.Length > 0)
        {
            NormalizeStrike(font.StrikeStyle);
        }
    }

    internal static PptxFont NormalizeFont(PptxFont font)
    {
        ValidateFont(font);
        PptxFont normalized = CloneFont(font);
        normalized.Capitalization = NormalizeCapitalization(normalized.Capitalization);
        if (normalized.UnderlineStyle.Length > 0)
        {
            normalized.UnderlineStyle = NormalizeUnderline(normalized.UnderlineStyle);
        }

        if (normalized.StrikeStyle.Length > 0)
        {
            normalized.StrikeStyle = NormalizeStrike(normalized.StrikeStyle);
        }

        return normalized;
    }

    internal static string NormalizeCapitalization(string capitalization)
    {
        string clean = PptxDiagnostic.CleanText(capitalization).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "none" || clean == "small" || clean == "all")
        {
            return clean;
        }

        if (clean == "smallcaps")
        {
            return "small";
        }

        if (clean == "allcaps" || clean == "uppercase")
        {
            return "all";
        }

        throw new System.ArgumentException("Unknown capitalization mode.", "capitalization");
    }

    internal static string NormalizeUnderline(string underline)
    {
        string clean = PptxDiagnostic.CleanText(underline).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "none" || clean == "words" || clean == "heavy" || clean == "dotted" || clean == "dottedheavy" || clean == "dash" || clean == "dashheavy" || clean == "dashlong" || clean == "dashlongheavy" || clean == "dotdash" || clean == "dotdashheavy" || clean == "dotdotdash" || clean == "dotdotdashheavy" || clean == "wavy" || clean == "wavyheavy")
        {
            return CanonicalUnderline(clean);
        }

        if (clean == "single" || clean == "sng")
        {
            return "sng";
        }

        if (clean == "double" || clean == "dbl")
        {
            return "dbl";
        }

        if (clean == "wavydouble" || clean == "wavydbl")
        {
            return "wavyDbl";
        }

        throw new System.ArgumentException("Unknown underline style.", "underline");
    }

    internal static string CanonicalUnderline(string clean)
    {
        if (clean == "dottedheavy")
        {
            return "dottedHeavy";
        }

        if (clean == "dashheavy")
        {
            return "dashHeavy";
        }

        if (clean == "dashlong")
        {
            return "dashLong";
        }

        if (clean == "dashlongheavy")
        {
            return "dashLongHeavy";
        }

        if (clean == "dotdash")
        {
            return "dotDash";
        }

        if (clean == "dotdashheavy")
        {
            return "dotDashHeavy";
        }

        if (clean == "dotdotdash")
        {
            return "dotDotDash";
        }

        if (clean == "dotdotdashheavy")
        {
            return "dotDotDashHeavy";
        }

        if (clean == "wavyheavy")
        {
            return "wavyHeavy";
        }

        return clean;
    }

    internal static string NormalizeStrike(string strike)
    {
        string clean = PptxDiagnostic.CleanText(strike).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "none" || clean == "nostrike")
        {
            return "noStrike";
        }

        if (clean == "single" || clean == "sng" || clean == "sngstrike")
        {
            return "sngStrike";
        }

        if (clean == "double" || clean == "dbl" || clean == "dblstrike")
        {
            return "dblStrike";
        }

        throw new System.ArgumentException("Unknown strike style.", "strike");
    }
}
