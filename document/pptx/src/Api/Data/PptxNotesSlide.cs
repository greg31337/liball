using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents notes slide data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxNotesSlide
{
    internal PptxPresentation Presentation;
    internal PptxSlide Slide;
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;

    internal PptxNotesSlide(PptxPresentation presentation, PptxSlide slide, PptxPart part, string relationshipId)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Presentation = presentation;
        Slide = slide;
        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
    }

    internal XmlDocument GetXmlDocument()
    {
        return Presentation.GetPartXmlDocument(Part);
    }
    internal string GetText()
    {
        XmlElement? textBody = GetTextBodyElement();
        if (textBody == null)
        {
            return "";
        }

        return PptxTextFrame.GetTextBodyText(textBody);
    }
    internal void SetText(string text)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement textBody = Presentation.GetOrCreateNotesTextBody(document);
        PptxTextFrame.SetTextBodyText(textBody, PptxDiagnostic.CleanText(text));
        Part.SetXmlDocument(document);
    }
    internal PptxPartMetadata GetMetadata()
    {
        return PptxPartInspectorCore.GetMetadata(Part, RelationshipId, 0);
    }
    internal PptxPartShapeTree GetShapeTree()
    {
        return PptxPartInspectorCore.GetShapeTree(Part);
    }
    internal bool Remove()
    {
        return Presentation.RemoveNotesSlide(Slide);
    }

    internal XmlElement? GetTextBodyElement()
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        return Presentation.FindNotesTextBody(root);
    }
    internal int ShapeCount()
    {
        return GetShapeElements().Count;
    }
    internal List<string> GetShapeNames()
    {
        List<XmlElement> shapes = GetShapeElements();
        List<string> names = new List<string>();
        for (int index = 0; index < shapes.Count; index++)
        {
            names.Add(GetShapeName(index));
        }

        return names;
    }
    internal string GetShapeName(int index)
    {
        XmlElement shape = GetShapeElement(index);
        XmlElement? properties = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(properties, "", "name");
    }

    internal string GetShapeXml(int index)
    {
        return GetShapeElement(index).OuterXml;
    }

    internal XmlElement GetShapeElement(int index)
    {
        List<XmlElement> shapes = GetShapeElements();
        if (index < 0 || index >= shapes.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return shapes[index];
    }

    internal List<XmlElement> GetShapeElements()
    {
        List<XmlElement> shapes = new List<XmlElement>();
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return shapes;
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData == null)
        {
            return shapes;
        }

        XmlElement? shapeTree = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "spTree");
        if (shapeTree == null)
        {
            return shapes;
        }

        for (int index = 0; index < shapeTree.ChildNodes.Count; index++)
        {
            XmlNode? child = shapeTree.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (PptxShapeTree.IsShapeElement(element))
            {
                shapes.Add(element);
            }
        }

        return shapes;
    }
}
