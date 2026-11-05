using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents an editable shape tree for a master, layout, notes, or handout part.
/// </summary>
public class PptxPartShapeTree
{
    internal PptxPart? Part;
    internal XmlDocument? Document;
    internal XmlElement? ShapeTreeElement;
    /// <summary>
    /// Contains top-level shapes in document order.
    /// </summary>
    public List<PptxShapeMetadata> Shapes;

    /// <summary>
    /// Initializes an empty shape-tree snapshot.
    /// </summary>
    public PptxPartShapeTree()
    {
        Shapes = new List<PptxShapeMetadata>();
        Part = null;
        Document = null;
        ShapeTreeElement = null;
    }
    internal void Attach(PptxPart part, XmlDocument document, XmlElement? shapeTree)
    {
        Part = part;
        Document = document;
        ShapeTreeElement = shapeTree;
    }
    internal int Count()
    {
        return Shapes.Count;
    }
    internal PptxShapeMetadata Get(int index)
    {
        if (index < 0 || index >= Shapes.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Shapes[index];
    }
    internal PptxShapeMetadata? FindById(int id)
    {
        for (int index = 0; index < Shapes.Count; index++)
        {
            PptxShapeMetadata? match = FindMetadataById(Shapes[index], id);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal PptxShapeMetadata AddAutoShape(string presetGeometry, PptxBounds bounds)
    {
        XmlDocument document = GetRequiredDocument();
        XmlElement tree = GetRequiredShapeTree();
        int id = AllocateShapeId();
        XmlElement shape = PptxShapeTree.CreateShapeElement(document, presetGeometry, id, PptxShapeTree.CreateShapeName(presetGeometry, id), bounds);
        AppendShapeElement(tree, shape);
        MarkDirty();
        return GetMetadataById(id);
    }
    internal PptxShapeMetadata AddGroup(PptxBounds bounds, PptxBounds childBounds)
    {
        XmlDocument document = GetRequiredDocument();
        XmlElement tree = GetRequiredShapeTree();
        int id = AllocateShapeId();
        XmlElement group = PptxShapeTree.CreateGroupElement(document, id, "Group " + id.ToString(), bounds, childBounds);
        AppendShapeElement(tree, group);
        MarkDirty();
        return GetMetadataById(id);
    }
    internal PptxShapeMetadata AddGraphicFrame(string dataUri, string dataXml, PptxBounds bounds)
    {
        XmlDocument document = GetRequiredDocument();
        XmlElement tree = GetRequiredShapeTree();
        int id = AllocateShapeId();
        XmlElement frame = PptxShapeTree.CreateGraphicFrameElement(document, id, "Graphic Frame " + id.ToString(), dataUri, dataXml, bounds);
        AppendShapeElement(tree, frame);
        MarkDirty();
        return GetMetadataById(id);
    }
    internal PptxShapeMetadata AddBackgroundShape(string presetGeometry, PptxBounds bounds)
    {
        PptxShapeMetadata shape = AddAutoShape(presetGeometry, bounds);
        SetCustomData(shape.Id, PptxShape.BackgroundDataName, "1");
        int index = Shapes.Count - 1;
        if (index > 0)
        {
            Move(index, 0);
        }

        return GetMetadataById(shape.Id);
    }
    internal PptxShapeMetadata AddAutoShapeToGroup(int groupId, string presetGeometry, PptxBounds bounds)
    {
        XmlDocument document = GetRequiredDocument();
        XmlElement group = GetGroupElement(groupId);
        int id = AllocateShapeId();
        XmlElement shape = PptxShapeTree.CreateShapeElement(document, presetGeometry, id, PptxShapeTree.CreateShapeName(presetGeometry, id), bounds);
        AppendShapeElement(group, shape);
        MarkDirty();
        return GetMetadataById(id);
    }
    internal PptxShapeMetadata AddNestedGroup(int groupId, PptxBounds bounds, PptxBounds childBounds)
    {
        XmlDocument document = GetRequiredDocument();
        XmlElement parent = GetGroupElement(groupId);
        int id = AllocateShapeId();
        XmlElement group = PptxShapeTree.CreateGroupElement(document, id, "Group " + id.ToString(), bounds, childBounds);
        AppendShapeElement(parent, group);
        MarkDirty();
        return GetMetadataById(id);
    }
    internal PptxShapeMetadata Clone(int id)
    {
        XmlElement shape = GetShapeElement(id);
        XmlNode? parent = shape.ParentNode;
        if (parent == null)
        {
            throw new System.ArgumentException("Shape parent is required.");
        }

        XmlElement clone = (XmlElement)shape.CloneNode(true);
        int cloneId = AllocateShapeId();
        PptxShapeTree.AssignCloneIds(clone, cloneId);
        string name = GetShapeName(shape);
        PptxShapeTree.SetShapeName(clone, name + " Copy");
        parent.InsertAfter(clone, shape);
        MarkDirty();
        return GetMetadataById(cloneId);
    }
    internal bool RemoveById(int id)
    {
        XmlElement? shape = FindShapeElement(GetRequiredShapeTree(), id);
        if (shape == null || shape.ParentNode == null)
        {
            return false;
        }

        shape.ParentNode.RemoveChild(shape);
        MarkDirty();
        return true;
    }
    internal void Move(int fromIndex, int toIndex)
    {
        MoveWithin(GetRequiredShapeTree(), fromIndex, toIndex);
        MarkDirty();
    }
    internal void MoveInGroup(int groupId, int fromIndex, int toIndex)
    {
        MoveWithin(GetGroupElement(groupId), fromIndex, toIndex);
        MarkDirty();
    }
    internal void Rename(int id, string name)
    {
        PptxShapeTree.SetShapeName(GetShapeElement(id), name);
        MarkDirty();
    }
    internal string GetCustomData(int id, string name)
    {
        XmlElement? data = FindCustomDataElement(GetShapeElement(id), PptxDiagnostic.CleanText(name));
        if (data == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(data, "", "value");
    }
    internal void SetCustomData(int id, string name, string value)
    {
        string cleanName = PptxDiagnostic.CleanText(name);
        if (cleanName.Length == 0)
        {
            throw new System.ArgumentException("Custom data name is required.");
        }

        XmlElement shape = GetShapeElement(id);
        XmlElement properties = GetNonVisualProperties(shape);
        XmlElement list = PptxXml.GetOrCreateChild(properties, "a", "extLst", PptxNamespaces.Drawing);
        XmlElement? extension = FindCustomDataExtension(shape);
        if (extension == null)
        {
            extension = GetRequiredDocument().CreateElement("a", "ext", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(extension, "", "", "uri", PptxShape.CustomDataExtensionUri);
            list.AppendChild(extension);
        }

        XmlElement? data = FindCustomDataElement(shape, cleanName);
        if (data == null)
        {
            data = GetRequiredDocument().CreateElement("pptxlib", "data", PptxShape.CustomDataNamespace);
            PptxXml.SetAttributeValue(data, "", "", "name", cleanName);
            extension.AppendChild(data);
        }

        PptxXml.SetAttributeValue(data, "", "", "value", PptxDiagnostic.CleanText(value));
        MarkDirty();
    }
    internal bool RemoveCustomData(int id, string name)
    {
        XmlElement shape = GetShapeElement(id);
        XmlElement? data = FindCustomDataElement(shape, PptxDiagnostic.CleanText(name));
        if (data == null || data.ParentNode == null)
        {
            return false;
        }

        XmlElement extension = (XmlElement)data.ParentNode;
        extension.RemoveChild(data);
        XmlNode? listNode = extension.ParentNode;
        if (!PptxShape.HasElementChildren(extension) && listNode != null && listNode.NodeType == XmlNodeType.Element)
        {
            XmlElement list = (XmlElement)listNode;
            list.RemoveChild(extension);
            if (!PptxShape.HasElementChildren(list) && list.ParentNode != null)
            {
                list.ParentNode.RemoveChild(list);
            }
        }

        MarkDirty();
        return true;
    }
    internal List<string> GetExtensionXml(int id)
    {
        List<string> extensions = new List<string>();
        XmlElement properties = GetNonVisualProperties(GetShapeElement(id));
        XmlElement? list = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "extLst");
        if (list == null)
        {
            return extensions;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                extensions.Add(((XmlElement)child).OuterXml);
            }
        }

        return extensions;
    }
    internal XmlElement GetNonVisualProperties(XmlElement shape)
    {
        XmlElement? properties = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            throw new System.ArgumentException("Shape non-visual properties are required.");
        }

        return properties;
    }
    internal XmlElement? FindCustomDataExtension(XmlElement shape)
    {
        XmlElement properties = GetNonVisualProperties(shape);
        XmlElement? list = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "extLst");
        if (list == null)
        {
            return null;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement extension = (XmlElement)child;
            if (extension.NamespaceURI == PptxNamespaces.Drawing && extension.LocalName == "ext" && PptxXml.GetAttributeValue(extension, "", "uri") == PptxShape.CustomDataExtensionUri)
            {
                return extension;
            }
        }

        return null;
    }
    internal XmlElement? FindCustomDataElement(XmlElement shape, string name)
    {
        XmlElement? extension = FindCustomDataExtension(shape);
        if (extension == null)
        {
            return null;
        }

        for (int index = 0; index < extension.ChildNodes.Count; index++)
        {
            XmlNode? child = extension.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement data = (XmlElement)child;
            if (data.NamespaceURI == PptxShape.CustomDataNamespace && data.LocalName == "data" && PptxXml.GetAttributeValue(data, "", "name") == name)
            {
                return data;
            }
        }

        return null;
    }
    internal void MoveWithin(XmlElement parent, int fromIndex, int toIndex)
    {
        List<XmlElement> children = GetDirectShapes(parent);
        if (fromIndex < 0 || fromIndex >= children.Count)
        {
            throw new System.ArgumentOutOfRangeException("fromIndex");
        }

        if (toIndex < 0 || toIndex >= children.Count)
        {
            throw new System.ArgumentOutOfRangeException("toIndex");
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        XmlElement shape = children[fromIndex];
        parent.RemoveChild(shape);
        children.RemoveAt(fromIndex);
        if (toIndex >= children.Count)
        {
            AppendShapeElement(parent, shape);
        }
        else
        {
            parent.InsertBefore(shape, children[toIndex]);
        }
    }
    internal void AppendShapeElement(XmlElement parent, XmlElement shape)
    {
        XmlElement? extensionList = PptxXml.FindFirstChild(parent, PptxNamespaces.Presentation, "extLst");
        if (extensionList == null)
        {
            parent.AppendChild(shape);
        }
        else
        {
            parent.InsertBefore(shape, extensionList);
        }
    }
    internal List<XmlElement> GetDirectShapes(XmlElement parent)
    {
        List<XmlElement> shapes = new List<XmlElement>();
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element && PptxShapeTree.IsShapeElement((XmlElement)child))
            {
                shapes.Add((XmlElement)child);
            }
        }

        return shapes;
    }
    internal XmlElement GetGroupElement(int id)
    {
        XmlElement group = GetShapeElement(id);
        if (group.LocalName != "grpSp")
        {
            throw new System.ArgumentException("Requested shape is not a group.");
        }

        return group;
    }
    internal XmlElement GetShapeElement(int id)
    {
        XmlElement? shape = FindShapeElement(GetRequiredShapeTree(), id);
        if (shape == null)
        {
            throw new System.ArgumentException("Shape was not found.");
        }

        return shape;
    }
    internal XmlElement? FindShapeElement(XmlElement parent, int id)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (!PptxShapeTree.IsShapeElement(element))
            {
                continue;
            }

            if (GetShapeId(element) == id)
            {
                return element;
            }

            XmlElement? nested = FindShapeElement(element, id);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
    internal int AllocateShapeId()
    {
        return FindMaximumShapeId(GetRequiredShapeTree(), 0) + 1;
    }
    internal int FindMaximumShapeId(XmlNode node, int maximum)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cNvPr")
            {
                int id = PptxXml.GetIntAttribute(element, "", "id", 0);
                if (id > maximum)
                {
                    maximum = id;
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                maximum = FindMaximumShapeId(child, maximum);
            }
        }

        return maximum;
    }
    internal int GetShapeId(XmlElement shape)
    {
        XmlElement? properties = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return 0;
        }

        return PptxXml.GetIntAttribute(properties, "", "id", 0);
    }
    internal string GetShapeName(XmlElement shape)
    {
        XmlElement? properties = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(properties, "", "name");
    }
    internal PptxShapeMetadata GetMetadataById(int id)
    {
        Reload();
        PptxShapeMetadata? metadata = FindById(id);
        if (metadata == null)
        {
            throw new System.ArgumentException("Shape metadata was not found.");
        }

        return metadata;
    }
    internal PptxShapeMetadata? FindMetadataById(PptxShapeMetadata shape, int id)
    {
        if (shape.Id == id)
        {
            return shape;
        }

        for (int index = 0; index < shape.Children.Count; index++)
        {
            PptxShapeMetadata? match = FindMetadataById(shape.Children[index], id);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal XmlDocument GetRequiredDocument()
    {
        if (Document == null)
        {
            throw new System.ArgumentException("Shape tree is not attached to a presentation part.");
        }

        return Document;
    }
    internal XmlElement GetRequiredShapeTree()
    {
        if (ShapeTreeElement != null)
        {
            return ShapeTreeElement;
        }

        XmlDocument document = GetRequiredDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Part XML root is required.");
        }

        XmlElement commonSlideData = PptxXml.GetOrCreateChild(root, "p", "cSld", PptxNamespaces.Presentation);
        ShapeTreeElement = PptxXml.GetOrCreateChild(commonSlideData, "p", "spTree", PptxNamespaces.Presentation);
        EnsureDefaults(ShapeTreeElement);
        return ShapeTreeElement;
    }
    internal void EnsureDefaults(XmlElement tree)
    {
        XmlDocument document = GetRequiredDocument();
        if (PptxXml.FindFirstChild(tree, PptxNamespaces.Presentation, "nvGrpSpPr") == null)
        {
            XmlElement nonVisual = document.CreateElement("p", "nvGrpSpPr", PptxNamespaces.Presentation);
            XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(properties, "", "", "id", "1");
            PptxXml.SetAttributeValue(properties, "", "", "name", "");
            nonVisual.AppendChild(properties);
            nonVisual.AppendChild(document.CreateElement("p", "cNvGrpSpPr", PptxNamespaces.Presentation));
            nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
            tree.InsertBefore(nonVisual, tree.FirstChild);
        }

        if (PptxXml.FindFirstChild(tree, PptxNamespaces.Presentation, "grpSpPr") == null)
        {
            XmlElement properties = document.CreateElement("p", "grpSpPr", PptxNamespaces.Presentation);
            XmlElement? firstShape = null;
            List<XmlElement> shapes = GetDirectShapes(tree);
            if (shapes.Count > 0)
            {
                firstShape = shapes[0];
            }

            tree.InsertBefore(properties, firstShape);
        }
    }
    internal void Reload()
    {
        Shapes.Clear();
        XmlElement tree = GetRequiredShapeTree();
        PptxPartInspectorCore.AppendShapes(tree, Shapes);
    }
    internal void MarkDirty()
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Shape tree is not attached to a presentation part.");
        }

        Part.SetXmlDocument(GetRequiredDocument());
        Reload();
    }
}
