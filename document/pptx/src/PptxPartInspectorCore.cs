using System.Xml;

namespace pptx;

internal static class PptxPartInspectorCore
{
    internal static PptxPartMetadata GetMetadata(PptxPart part, string relationshipId, int themeCount)
    {
        XmlDocument document = GetDocument(part);
        PptxPartShapeTree tree = GetShapeTree(part);
        PptxPartMetadata metadata = new PptxPartMetadata();
        metadata.PartName = part.PartName;
        metadata.RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        metadata.ShapeCount = tree.Shapes.Count;
        metadata.PlaceholderCount = CountPlaceholders(tree);
        metadata.ThemeCount = themeCount;
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return metadata;
        }

        metadata.RootElementName = root.LocalName;
        metadata.ShowMasterShapes = PptxXml.GetBoolAttribute(root, "", "showMasterSp", true);
        metadata.ShowMasterPlaceholderAnimations = PptxXml.GetBoolAttribute(root, "", "showMasterPhAnim", true);
        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData != null)
        {
            metadata.Name = PptxXml.GetAttributeValue(commonSlideData, "", "name");
        }

        return metadata;
    }

    internal static PptxPartShapeTree GetShapeTree(PptxPart part)
    {
        PptxPartShapeTree result = new PptxPartShapeTree();
        XmlDocument document = GetDocument(part);
        result.Attach(part, document, null);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return result;
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData == null)
        {
            return result;
        }

        XmlElement? shapeTree = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "spTree");
        if (shapeTree == null)
        {
            return result;
        }

        result.Attach(part, document, shapeTree);
        AppendShapes(shapeTree, result.Shapes);
        return result;
    }

    internal static XmlDocument GetDocument(PptxPart part)
    {
        if (part.HasXmlDocument)
        {
            return part.XmlDocument;
        }

        return PptxXml.LoadBytes(part.GetBytes());
    }

    internal static void AppendShapes(XmlElement parent, System.Collections.Generic.List<PptxShapeMetadata> shapes)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (PptxShapeTree.IsShapeElement(element))
            {
                shapes.Add(CreateShapeMetadata(element));
            }
        }
    }

    internal static PptxShapeMetadata CreateShapeMetadata(XmlElement element)
    {
        PptxShapeMetadata metadata = new PptxShapeMetadata();
        metadata.ElementType = element.LocalName;
        metadata.RawXml = element.OuterXml;
        XmlElement? properties = PptxXml.FindFirstDescendant(element, PptxNamespaces.Presentation, "cNvPr");
        if (properties != null)
        {
            metadata.Id = PptxXml.GetIntAttribute(properties, "", "id", 0);
            metadata.Name = PptxXml.GetAttributeValue(properties, "", "name");
        }

        XmlElement? placeholder = PptxXml.FindFirstDescendant(element, PptxNamespaces.Presentation, "ph");
        if (placeholder != null)
        {
            metadata.PlaceholderType = PptxPresentation.NormalizePlaceholderType(PptxXml.GetAttributeValue(placeholder, "", "type"));
            metadata.PlaceholderIndex = PptxXml.GetIntAttribute(placeholder, "", "idx", -1);
        }

        XmlElement? textBody = PptxXml.FindFirstDescendant(element, PptxNamespaces.Presentation, "txBody");
        if (textBody != null)
        {
            metadata.Text = PptxTextFrame.GetTextBodyText(textBody);
        }

        AppendShapes(element, metadata.Children);
        return metadata;
    }

    internal static int CountPlaceholders(PptxPartShapeTree tree)
    {
        int count = 0;
        for (int index = 0; index < tree.Shapes.Count; index++)
        {
            count = count + CountShapePlaceholders(tree.Shapes[index]);
        }

        return count;
    }

    internal static int CountShapePlaceholders(PptxShapeMetadata shape)
    {
        int count = 0;
        if (shape.PlaceholderType.Length > 0)
        {
            count = 1;
        }

        for (int index = 0; index < shape.Children.Count; index++)
        {
            count = count + CountShapePlaceholders(shape.Children[index]);
        }

        return count;
    }
}
