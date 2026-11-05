using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxTextReplacerCore
{
    internal static int ReplacePresentationText(PptxPresentation presentation, string search, string replacement)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        int count = 0;
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            PptxSlide slide = presentation.GetSlide(index);
            count = count + ReplaceSlideText(slide, search, replacement);
            PptxPart? notesPart = presentation.FindNotesSlidePart(slide);
            if (notesPart != null)
            {
                count = count + ReplacePartText(presentation, notesPart, search, replacement);
            }

            count = count + ReplaceRelatedPartsText(presentation, slide, PptxRelationshipTypes.Comments, search, replacement);
            count = count + ReplaceRelatedPartsText(presentation, slide, PptxRelationshipTypes.Chart, search, replacement);
        }

        return count;
    }
    internal static int ReplaceSlideText(PptxSlide slide, string search, string replacement)
    {
        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return 0;
        }

        int count = ReplaceXmlText(root, search, replacement);
        if (count > 0)
        {
            slide.Part.SetXmlDocument(document);
            slide.ReloadPlaceholders();
        }

        return count;
    }

    internal static int ReplaceRelatedPartsText(PptxPresentation presentation, PptxSlide slide, string relationshipType, string search, string replacement)
    {
        int count = 0;
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type != relationshipType)
            {
                continue;
            }

            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                continue;
            }

            if (!presentation.Package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            count = count + ReplacePartText(presentation, presentation.Package.GetPart(relationship.TargetPartName), search, replacement);
        }

        return count;
    }

    internal static int ReplacePartText(PptxPresentation presentation, PptxPart part, string search, string replacement)
    {
        XmlDocument document = presentation.GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return 0;
        }

        int count = ReplaceXmlText(root, search, replacement);
        if (count > 0)
        {
            part.SetXmlDocument(document);
        }

        return count;
    }

    internal static int ReplaceXmlText(XmlNode node, string search, string replacement)
    {
        string cleanSearch = PptxRunFormatCore.NormalizeTextValue(search);
        string cleanReplacement = PptxRunFormatCore.NormalizeTextValue(replacement);
        if (cleanSearch.Length == 0)
        {
            return 0;
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "p")
            {
                return ReplaceParagraphText(element, cleanSearch, cleanReplacement);
            }

            if (IsPlainTextElement(element))
            {
                return ReplacePlainTextElement(element, cleanSearch, cleanReplacement);
            }
        }

        int count = 0;
        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                count = count + ReplaceXmlText(child, cleanSearch, cleanReplacement);
            }
        }

        return count;
    }

    internal static bool IsPlainTextElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "t")
        {
            return true;
        }

        if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "text")
        {
            return true;
        }

        if (element.NamespaceURI == PptxNamespaces.Chart && element.LocalName == "v")
        {
            return true;
        }

        return false;
    }

    internal static int ReplaceTextBody(XmlElement textBody, string search, string replacement)
    {
        return ReplaceXmlText(textBody, search, replacement);
    }

    internal static int ReplaceParagraphText(XmlElement paragraph, string search, string replacement)
    {
        string cleanSearch = PptxRunFormatCore.NormalizeTextValue(search);
        if (cleanSearch.Length == 0)
        {
            return 0;
        }

        StringBuilder builder = new StringBuilder();
        List<PptxTextSpan> spans = new List<PptxTextSpan>();
        BuildParagraphText(paragraph, builder, spans);
        string text = builder.ToString();
        List<PptxTextMatch> matches = FindReplaceableMatches(text, spans, cleanSearch);
        if (matches.Count == 0)
        {
            return 0;
        }

        ApplyMatches(spans, matches, PptxRunFormatCore.NormalizeTextValue(replacement));
        return matches.Count;
    }

    internal static void BuildParagraphText(XmlElement paragraph, StringBuilder builder, List<PptxTextSpan> spans)
    {
        for (int index = 0; index < paragraph.ChildNodes.Count; index++)
        {
            XmlNode? child = paragraph.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            if (element.LocalName == "r" || element.LocalName == "fld")
            {
                XmlElement? text = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "t");
                if (text != null)
                {
                    spans.Add(new PptxTextSpan(text, text.InnerText, builder.Length));
                    builder.Append(text.InnerText);
                }
            }

            if (element.LocalName == "tab")
            {
                builder.Append("\t");
            }

            if (element.LocalName == "br")
            {
                builder.Append("\n");
            }
        }
    }

    internal static List<PptxTextMatch> FindReplaceableMatches(string text, List<PptxTextSpan> spans, string search)
    {
        List<PptxTextMatch> matches = new List<PptxTextMatch>();
        int start = 0;
        while (start <= text.Length - search.Length)
        {
            int index = text.IndexOf(search, start, System.StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            if (CanReplaceRange(spans, index, search.Length))
            {
                matches.Add(new PptxTextMatch(index, search.Length));
                start = index + search.Length;
            }
            else
            {
                start = index + 1;
            }
        }

        return matches;
    }

    internal static bool CanReplaceRange(List<PptxTextSpan> spans, int start, int length)
    {
        int end = start + length;
        for (int index = start; index < end; index++)
        {
            if (FindSpanAt(spans, index) == null)
            {
                return false;
            }
        }

        return true;
    }

    internal static PptxTextSpan? FindSpanAt(List<PptxTextSpan> spans, int position)
    {
        for (int index = 0; index < spans.Count; index++)
        {
            PptxTextSpan span = spans[index];
            if (position >= span.Start && position < span.Start + span.Length)
            {
                return span;
            }
        }

        return null;
    }

    internal static void ApplyMatches(List<PptxTextSpan> spans, List<PptxTextMatch> matches, string replacement)
    {
        for (int index = 0; index < spans.Count; index++)
        {
            PptxTextSpan span = spans[index];
            span.TextElement.InnerText = BuildReplacementSpanText(span, matches, replacement);
        }
    }

    internal static string BuildReplacementSpanText(PptxTextSpan span, List<PptxTextMatch> matches, string replacement)
    {
        StringBuilder builder = new StringBuilder();
        int position = span.Start;
        int local = 0;
        int end = span.Start + span.Length;
        while (position < end)
        {
            PptxTextMatch? match = FindMatchCovering(matches, position);
            if (match != null)
            {
                if (match.Start == position)
                {
                    builder.Append(replacement);
                }

                int next = match.Start + match.Length;
                if (next > end)
                {
                    position = end;
                    local = span.Length;
                }
                else
                {
                    position = next;
                    local = next - span.Start;
                }
            }
            else
            {
                builder.Append(span.OriginalText[local]);
                position = position + 1;
                local = local + 1;
            }
        }

        return builder.ToString();
    }

    internal static PptxTextMatch? FindMatchCovering(List<PptxTextMatch> matches, int position)
    {
        for (int index = 0; index < matches.Count; index++)
        {
            PptxTextMatch match = matches[index];
            if (position >= match.Start && position < match.Start + match.Length)
            {
                return match;
            }
        }

        return null;
    }

    internal static int ReplacePlainTextElement(XmlElement element, string search, string replacement)
    {
        string cleanSearch = PptxRunFormatCore.NormalizeTextValue(search);
        string cleanReplacement = PptxRunFormatCore.NormalizeTextValue(replacement);
        if (cleanSearch.Length == 0)
        {
            return 0;
        }

        string text = element.InnerText;
        int count = 0;
        int start = 0;
        int index = text.IndexOf(cleanSearch, start, System.StringComparison.Ordinal);
        if (index < 0)
        {
            return 0;
        }

        StringBuilder builder = new StringBuilder();
        while (index >= 0)
        {
            builder.Append(text.Substring(start, index - start));
            builder.Append(cleanReplacement);
            count = count + 1;
            start = index + cleanSearch.Length;
            index = text.IndexOf(cleanSearch, start, System.StringComparison.Ordinal);
        }

        builder.Append(text.Substring(start));
        element.InnerText = builder.ToString();
        return count;
    }
}
