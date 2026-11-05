using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxTextExtractorCore
{
    internal static string ExtractPresentationText(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            AppendChunk(builder, ExtractSlideText(presentation.GetSlide(index)));
            AppendChunk(builder, ExtractNotesText(presentation, presentation.GetSlide(index)));
            AppendChunk(builder, ExtractRelatedPartsText(presentation, presentation.GetSlide(index), PptxRelationshipTypes.Comments));
            AppendChunk(builder, ExtractRelatedPartsText(presentation, presentation.GetSlide(index), PptxRelationshipTypes.ModernComments));
            AppendChunk(builder, ExtractRelatedPartsText(presentation, presentation.GetSlide(index), PptxRelationshipTypes.Chart));
            AppendChunk(builder, ExtractRelatedPartsText(presentation, presentation.GetSlide(index), PptxRelationshipTypes.DiagramData));
        }

        return builder.ToString();
    }
    internal static string ExtractSlideText(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return "";
        }

        return ExtractXmlText(root);
    }
    internal static string ExtractNotesText(PptxPresentation presentation, PptxSlide slide)
    {
        PptxPart? notesPart = presentation.FindNotesSlidePart(slide);
        if (notesPart == null)
        {
            return "";
        }

        return ExtractPartText(presentation, notesPart);
    }
    internal static string ExtractRelatedPartsText(PptxPresentation presentation, PptxSlide slide, string relationshipType)
    {
        StringBuilder builder = new StringBuilder();
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

            AppendChunk(builder, ExtractPartText(presentation, presentation.Package.GetPart(relationship.TargetPartName)));
        }

        return builder.ToString();
    }

    internal static string ExtractPartText(PptxPresentation presentation, PptxPart part)
    {
        XmlDocument document = GetPartXmlDocument(presentation, part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return "";
        }

        return ExtractXmlText(root);
    }

    internal static XmlDocument GetPartXmlDocument(PptxPresentation presentation, PptxPart part)
    {
        if (part.HasXmlDocument)
        {
            return part.XmlDocument;
        }

        return PptxXml.LoadBytes(presentation.Package.GetPartBytesForSave(part));
    }

    internal static string ExtractXmlText(XmlNode node)
    {
        StringBuilder builder = new StringBuilder();
        AppendXmlText(node, builder);
        return builder.ToString();
    }

    internal static void AppendXmlText(XmlNode node, StringBuilder builder)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (IsTextBodyElement(element))
            {
                AppendChunk(builder, PptxTextFrame.GetTextBodyText(element));
                return;
            }

            if (IsChartRichTextElement(element))
            {
                AppendChunk(builder, GetRichText(element));
                return;
            }

            if (IsCommentTextElement(element))
            {
                AppendChunk(builder, element.InnerText);
                return;
            }

            if (IsChartValueElement(element))
            {
                AppendChunk(builder, element.InnerText);
                return;
            }

            if (IsDiagramTextElement(element))
            {
                AppendChunk(builder, element.InnerText);
                return;
            }

            if (IsDrawingTextElement(element))
            {
                AppendChunk(builder, element.InnerText);
                return;
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                AppendXmlText(child, builder);
            }
        }
    }

    internal static bool IsTextBodyElement(XmlElement element)
    {
        if (element.LocalName != "txBody")
        {
            return false;
        }

        if (element.NamespaceURI == PptxNamespaces.Presentation)
        {
            return true;
        }

        if (element.NamespaceURI == PptxNamespaces.Drawing)
        {
            return true;
        }

        return false;
    }

    internal static bool IsChartRichTextElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Chart && element.LocalName == "rich")
        {
            return true;
        }

        return false;
    }

    internal static bool IsCommentTextElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "text")
        {
            return true;
        }

        return false;
    }

    internal static bool IsChartValueElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Chart && element.LocalName == "v")
        {
            return true;
        }

        return false;
    }

    internal static bool IsDrawingTextElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "t")
        {
            return true;
        }

        return false;
    }

    internal static bool IsDiagramTextElement(XmlElement element)
    {
        if (element.NamespaceURI == PptxNamespaces.Diagram && element.LocalName == "t")
        {
            return true;
        }

        return false;
    }

    internal static string GetRichText(XmlElement rich)
    {
        StringBuilder builder = new StringBuilder();
        int paragraphCount = 0;
        for (int index = 0; index < rich.ChildNodes.Count; index++)
        {
            XmlNode? child = rich.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement paragraph = (XmlElement)child;
            if (paragraph.NamespaceURI != PptxNamespaces.Drawing || paragraph.LocalName != "p")
            {
                continue;
            }

            if (paragraphCount > 0)
            {
                builder.Append("\n");
            }

            PptxTextFrame.AppendParagraphText(paragraph, builder);
            paragraphCount = paragraphCount + 1;
        }

        return builder.ToString();
    }

    internal static void AppendChunk(StringBuilder builder, string text)
    {
        string clean = PptxDiagnostic.CleanText(text);
        if (clean.Length == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("\n");
        }

        builder.Append(clean);
    }
}

