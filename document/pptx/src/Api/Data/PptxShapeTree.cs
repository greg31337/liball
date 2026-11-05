using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents shape tree data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxShapeTree
{
    internal PptxSlide Slide;
    internal XmlDocument Document;
    internal XmlElement ShapeTreeElement;
    /// <summary>
    /// Contains the shapes in package or presentation order.
    /// </summary>
    public List<PptxShape> Shapes;

    internal PptxShapeTree(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        Slide = slide;
        Document = slide.GetXmlDocument();
        ShapeTreeElement = GetOrCreateShapeTreeElement(Document);
        Shapes = new List<PptxShape>();
        Reload();
    }
    internal int Count()
    {
        return Shapes.Count;
    }
    internal PptxShape Get(int index)
    {
        ValidateIndex(index);
        return Shapes[index];
    }
    internal PptxShape? FindById(int id)
    {
        for (int index = 0; index < Shapes.Count; index++)
        {
            PptxShape? match = Shapes[index].FindDescendantById(id);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal PptxShape? FindByName(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        for (int index = 0; index < Shapes.Count; index++)
        {
            PptxShape? match = Shapes[index].FindDescendantByName(clean);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal int CountAll()
    {
        return GetAllShapes().Count;
    }
    internal List<PptxShape> GetAllShapes()
    {
        List<PptxShape> allShapes = new List<PptxShape>();
        for (int index = 0; index < Shapes.Count; index++)
        {
            AddShapeAndChildren(allShapes, Shapes[index]);
        }

        return allShapes;
    }

    internal void AddShapeAndChildren(List<PptxShape> allShapes, PptxShape shape)
    {
        allShapes.Add(shape);
        for (int index = 0; index < shape.Children.Count; index++)
        {
            AddShapeAndChildren(allShapes, shape.Children[index]);
        }
    }
    internal PptxShape AddShape(string shapeType, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        int id = AllocateShapeId();
        XmlElement shape = CreateShapeElement(Document, shapeType, id, CreateShapeName(shapeType, id), bounds);
        AppendShapeElement(shape);
        PptxShape result = new PptxShape(this, shape);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddAutoShape(string presetGeometry, PptxBounds bounds)
    {
        return AddShape(presetGeometry, bounds);
    }
    internal PptxShape AddConnector(string connectorType, PptxPoint start, PptxPoint end)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        int id = AllocateShapeId();
        XmlElement shape = CreateConnectorElement(Document, connectorType, id, CreateConnectorName(connectorType, id), start, end);
        AppendShapeElement(shape);
        PptxShape result = new PptxShape(this, shape);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddFreeform(PptxBounds bounds, List<PptxPoint> points)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        PptxGeometry.ValidateFreeformPoints(points);
        int id = AllocateShapeId();
        XmlElement shape = CreateFreeformElement(Document, id, CreateFreeformName(id), bounds, points);
        AppendShapeElement(shape);
        PptxShape result = new PptxShape(this, shape);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddPicture(string relationshipId, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        int id = AllocateShapeId();
        XmlElement picture = CreatePictureElement(Document, id, "Picture " + id.ToString(), PptxDiagnostic.CleanText(relationshipId), bounds);
        AppendShapeElement(picture);
        PptxShape result = new PptxShape(this, picture);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal void SetPictureRelationshipLinked(string relationshipId, bool linked)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        for (int index = 0; index < Shapes.Count; index++)
        {
            PptxShape shape = Shapes[index];
            if (shape.GetImageRelationshipId() == clean)
            {
                shape.SetImageRelationshipLinked(linked);
            }
        }
    }
    internal PptxShape AddMedia(string relationshipId, string mediaType, PptxBounds bounds)
    {
        int id = AllocateShapeId();
        XmlElement media = CreateMediaElement(Document, id, "Media " + id.ToString(), relationshipId, mediaType, bounds);
        AppendShapeElement(media);
        PptxShape result = new PptxShape(this, media);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddEmbeddedObject(string relationshipId, string programId, string objectName, PptxBounds bounds)
    {
        int id = AllocateShapeId();
        XmlElement embeddedObject = CreateEmbeddedObjectElement(Document, id, "Object " + id.ToString(), relationshipId, programId, objectName, bounds);
        AppendShapeElement(embeddedObject);
        PptxShape result = new PptxShape(this, embeddedObject);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddTable(int rowCount, int columnCount, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        int id = AllocateShapeId();
        XmlElement table = CreateTableElement(Document, id, "Table " + id.ToString(), rowCount, columnCount, bounds);
        AppendShapeElement(table);
        PptxShape result = new PptxShape(this, table);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddChart(string relationshipId, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        int id = AllocateShapeId();
        XmlElement chart = CreateChartElement(Document, id, "Chart " + id.ToString(), PptxDiagnostic.CleanText(relationshipId), bounds);
        AppendShapeElement(chart);
        PptxShape result = new PptxShape(this, chart);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddGroup(PptxBounds bounds, PptxBounds childBounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        if (childBounds == null)
        {
            throw new System.ArgumentNullException("childBounds");
        }

        int id = AllocateShapeId();
        XmlElement group = CreateGroupElement(Document, id, "Group " + id.ToString(), bounds, childBounds);
        AppendShapeElement(group);
        PptxShape result = new PptxShape(this, group);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddGraphicFrame(string dataUri, string dataXml, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        int id = AllocateShapeId();
        XmlElement frame = CreateGraphicFrameElement(Document, id, "Graphic Frame " + id.ToString(), dataUri, dataXml, bounds);
        AppendShapeElement(frame);
        PptxShape result = new PptxShape(this, frame);
        Shapes.Add(result);
        MarkDirty();
        return result;
    }
    internal PptxShape AddBackgroundShape(string shapeType, PptxBounds bounds)
    {
        PptxShape shape = AddShape(shapeType, bounds);
        shape.SetBackgroundShape(true);
        SendToBack(shape);
        return shape;
    }
    internal void RemoveAt(int index)
    {
        ValidateIndex(index);
        PptxShape shape = Shapes[index];
        ShapeTreeElement.RemoveChild(shape.Element);
        Shapes.RemoveAt(index);
        MarkDirty();
    }
    internal void Remove(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (shape.ParentGroup != null)
        {
            int childIndex = shape.ParentGroup.AsGroup().IndexOf(shape);
            if (childIndex >= 0)
            {
                shape.ParentGroup.AsGroup().RemoveAt(childIndex);
            }

            return;
        }

        for (int index = 0; index < Shapes.Count; index++)
        {
            if (Shapes[index].Element == shape.Element)
            {
                RemoveAt(index);
                return;
            }
        }
    }
    internal bool RemoveById(int id)
    {
        PptxShape? shape = FindById(id);
        if (shape == null)
        {
            return false;
        }

        Remove(shape);
        return true;
    }
    internal void Move(int fromIndex, int toIndex)
    {
        ValidateIndex(fromIndex);
        ValidateIndex(toIndex);
        if (fromIndex == toIndex)
        {
            return;
        }

        PptxShape shape = Shapes[fromIndex];
        Shapes.RemoveAt(fromIndex);
        ShapeTreeElement.RemoveChild(shape.Element);

        XmlElement? before = GetShapeElementAt(toIndex);
        if (before == null)
        {
            AppendShapeElement(shape.Element);
        }
        else
        {
            ShapeTreeElement.InsertBefore(shape.Element, before);
        }

        Shapes.Insert(toIndex, shape);
        MarkDirty();
    }
    internal PptxShape Clone(int index)
    {
        ValidateIndex(index);
        return CloneShape(Shapes[index]);
    }
    internal PptxShape CloneShape(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        XmlElement cloneElement = (XmlElement)shape.Element.CloneNode(true);
        int id = AllocateShapeId();
        AssignCloneIds(cloneElement, id);
        SetShapeName(cloneElement, shape.Name + " Copy");
        if (shape.ParentGroup == null)
        {
            ShapeTreeElement.InsertAfter(cloneElement, shape.Element);
            PptxShape clone = new PptxShape(this, cloneElement);
            int index = IndexOf(shape);
            if (index < 0)
            {
                Shapes.Add(clone);
            }
            else
            {
                Shapes.Insert(index + 1, clone);
            }

            MarkDirty();
            return clone;
        }

        shape.ParentGroup.Element.InsertAfter(cloneElement, shape.Element);
        PptxShape nestedClone = new PptxShape(this, cloneElement);
        nestedClone.ParentGroup = shape.ParentGroup;
        int childIndex = shape.ParentGroup.AsGroup().IndexOf(shape);
        if (childIndex < 0)
        {
            shape.ParentGroup.Children.Add(nestedClone);
        }
        else
        {
            shape.ParentGroup.Children.Insert(childIndex + 1, nestedClone);
        }

        nestedClone.RefreshEffectiveBounds();
        MarkDirty();
        return nestedClone;
    }
    internal PptxShape CloneInto(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        XmlNode imported = Document.ImportNode(shape.Element, true);
        XmlElement cloneElement = (XmlElement)imported;
        Dictionary<string, string> relationships = new Dictionary<string, string>();
        CopyCloneRelationships(cloneElement, shape, relationships);
        int id = AllocateShapeId();
        AssignCloneIds(cloneElement, id);
        SetShapeName(cloneElement, shape.Name + " Copy");
        AppendShapeElement(cloneElement);
        PptxShape clone = new PptxShape(this, cloneElement);
        Shapes.Add(clone);
        MarkDirty();
        return clone;
    }

    internal void CopyCloneRelationships(XmlNode node, PptxShape source, Dictionary<string, string> relationships)
    {
        if (source.Tree.Slide.Part != Slide.Part && source.Tree.Slide.Presentation != Slide.Presentation)
        {
            throw new System.ArgumentException("Cross-presentation shape cloning is not supported.");
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            for (int attributeIndex = 0; attributeIndex < element.Attributes.Count; attributeIndex++)
            {
                XmlAttribute? attribute = element.Attributes[attributeIndex];
                if (attribute == null || attribute.NamespaceURI != PptxNamespaces.Relationships)
                {
                    continue;
                }

                string relationshipId = attribute.Value;
                if (!relationships.ContainsKey(relationshipId))
                {
                    relationships[relationshipId] = CreateCloneRelationship(source, relationshipId);
                }

                if (relationships[relationshipId].Length > 0)
                {
                    attribute.Value = relationships[relationshipId];
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                CopyCloneRelationships(child, source, relationships);
            }
        }
    }

    internal string CreateCloneRelationship(PptxShape source, string relationshipId)
    {
        if (source.Tree.Slide.Part == Slide.Part)
        {
            return relationshipId;
        }

        PptxRelationship? relationship = source.Tree.FindRelationshipById(relationshipId);
        if (relationship == null)
        {
            return "";
        }

        string target = relationship.Target;
        if (relationship.TargetMode == PptxRelationshipTargetMode.Internal)
        {
            target = PptxSlideCloner.BuildRelativeTargetPath(Slide.PartName, relationship.TargetPartName);
            if (relationship.TargetFragment.Length > 0)
            {
                target = target + "#" + relationship.TargetFragment;
            }
        }

        if (Slide.Presentation == null)
        {
            throw new System.ArgumentException("Destination slide is not attached to a presentation.");
        }

        PptxRelationship clone = Slide.Presentation.Package.AddRelationship(Slide.PartName, relationship.Type, target, relationship.TargetMode);
        return clone.Id;
    }

    internal void BringToFront(PptxShape shape)
    {
        int index = IndexOf(shape);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a top-level member of this tree.");
        }

        Move(index, Shapes.Count - 1);
    }
    internal void SendToBack(PptxShape shape)
    {
        int index = IndexOf(shape);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a top-level member of this tree.");
        }

        Move(index, 0);
    }
    internal void MoveForward(PptxShape shape)
    {
        int index = IndexOf(shape);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a top-level member of this tree.");
        }

        if (index < Shapes.Count - 1)
        {
            Move(index, index + 1);
        }
    }
    internal void MoveBackward(PptxShape shape)
    {
        int index = IndexOf(shape);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a top-level member of this tree.");
        }

        if (index > 0)
        {
            Move(index, index - 1);
        }
    }

    internal void AppendShapeElement(XmlElement shape)
    {
        XmlElement? extensionList = PptxXml.FindFirstChild(ShapeTreeElement, PptxNamespaces.Presentation, "extLst");
        if (extensionList == null)
        {
            ShapeTreeElement.AppendChild(shape);
        }
        else
        {
            ShapeTreeElement.InsertBefore(shape, extensionList);
        }
    }

    internal static int AssignCloneIds(XmlNode node, int nextId)
    {
        Dictionary<int, int> identifiers = new Dictionary<int, int>();
        int result = AssignCloneIdentifiers(node, nextId, identifiers);
        UpdateCloneConnectionIds(node, identifiers);
        return result;
    }

    internal static int AssignCloneIdentifiers(XmlNode node, int nextId, Dictionary<int, int> identifiers)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cNvPr")
            {
                int previousId = PptxXml.GetIntAttribute(element, "", "id", 0);
                if (previousId > 0)
                {
                    identifiers[previousId] = nextId;
                }

                PptxXml.SetAttributeValue(element, "", "", "id", nextId.ToString());
                nextId = nextId + 1;
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                nextId = AssignCloneIdentifiers(child, nextId, identifiers);
            }
        }

        return nextId;
    }

    internal static void UpdateCloneConnectionIds(XmlNode node, Dictionary<int, int> identifiers)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Drawing && (element.LocalName == "stCxn" || element.LocalName == "endCxn"))
            {
                int previousId = PptxXml.GetIntAttribute(element, "", "id", 0);
                if (identifiers.ContainsKey(previousId))
                {
                    PptxXml.SetAttributeValue(element, "", "", "id", identifiers[previousId].ToString());
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                UpdateCloneConnectionIds(child, identifiers);
            }
        }
    }
    internal void Rename(int index, string name)
    {
        ValidateIndex(index);
        Shapes[index].SetName(name);
    }

    internal void ValidateIndex(int index)
    {
        if (index < 0 || index >= Shapes.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
    internal int IndexOf(PptxShape shape)
    {
        for (int index = 0; index < Shapes.Count; index++)
        {
            if (Shapes[index].Element == shape.Element)
            {
                return index;
            }
        }

        return -1;
    }

    internal void Reload()
    {
        Shapes.Clear();
        for (int index = 0; index < ShapeTreeElement.ChildNodes.Count; index++)
        {
            XmlNode? child = ShapeTreeElement.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (IsShapeElement(element))
            {
                Shapes.Add(new PptxShape(this, element));
            }
        }
    }

    internal void MarkDirty()
    {
        Slide.Part.SetXmlDocument(Document);
        Slide.ReloadPlaceholders();
    }

    internal PptxRelationship AddExternalRelationship(string relationshipType, string target)
    {
        string id = AllocateRelationshipId();
        PptxRelationship relationship = new PptxRelationship(
            Slide.PartName,
            id,
            relationshipType,
            target,
            PptxRelationshipTargetMode.External,
            "",
            "");
        Slide.Part.Relationships.Add(relationship);
        Slide.Part.MarkDirty();
        return relationship;
    }

    internal string AllocateRelationshipId()
    {
        int number = 1;
        while (Slide.Part.Relationships.ContainsId("rId" + number.ToString()))
        {
            number = number + 1;
        }

        return "rId" + number.ToString();
    }

    internal PptxRelationship? FindRelationshipById(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        for (int index = 0; index < Slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Slide.Part.Relationships.Relationships[index];
            if (relationship.Id == clean)
            {
                return relationship;
            }
        }

        return null;
    }

    internal void SetRelationshipTarget(string relationshipId, string target)
    {
        PptxRelationship? relationship = FindRelationshipById(relationshipId);
        if (relationship == null)
        {
            throw new System.ArgumentException("Relationship was not found.");
        }

        relationship.Target = PptxDiagnostic.CleanText(target);
        relationship.TargetMode = PptxRelationshipTargetMode.External;
        relationship.TargetPartName = "";
        relationship.TargetFragment = "";
        Slide.Part.MarkDirty();
    }

    internal void RemoveRelationship(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        int index = Slide.Part.Relationships.IndexOfId(clean);
        if (index >= 0)
        {
            Slide.Part.Relationships.Relationships.RemoveAt(index);
            Slide.Part.MarkDirty();
        }
    }
    internal void RemoveRelationshipIfUnused(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        if (clean.Length == 0)
        {
            return;
        }

        if (HasRelationshipReference(Document, clean))
        {
            return;
        }

        RemoveRelationship(clean);
    }
    internal bool HasRelationshipReference(XmlNode node, string relationshipId)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            for (int attributeIndex = 0; attributeIndex < element.Attributes.Count; attributeIndex++)
            {
                XmlAttribute? attribute = element.Attributes[attributeIndex];
                if (attribute != null && attribute.NamespaceURI == PptxNamespaces.Relationships && attribute.Value == relationshipId)
                {
                    return true;
                }
            }
        }

        for (int childIndex = 0; childIndex < node.ChildNodes.Count; childIndex++)
        {
            XmlNode? child = node.ChildNodes[childIndex];
            if (child != null && HasRelationshipReference(child, relationshipId))
            {
                return true;
            }
        }

        return false;
    }

    internal string BoolToXml(bool value)
    {
        if (value)
        {
            return "1";
        }

        return "0";
    }

    internal int AllocateShapeId()
    {
        return FindMaximumShapeId(Document.DocumentElement, 0) + 1;
    }

    internal int FindMaximumShapeId(XmlNode? node, int maximum)
    {
        if (node == null)
        {
            return maximum;
        }

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
            maximum = FindMaximumShapeId(child, maximum);
        }

        return maximum;
    }

    internal XmlElement? GetShapeElementAt(int ordinal)
    {
        int current = 0;
        for (int index = 0; index < ShapeTreeElement.ChildNodes.Count; index++)
        {
            XmlNode? child = ShapeTreeElement.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (!IsShapeElement(element))
            {
                continue;
            }

            if (current == ordinal)
            {
                return element;
            }

            current = current + 1;
        }

        return null;
    }

    internal XmlElement GetOrCreateShapeTreeElement(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Slide XML root is required.");
        }

        XmlElement commonSlideData = PptxXml.GetOrCreateChild(root, "p", "cSld", PptxNamespaces.Presentation);
        XmlElement shapeTree = PptxXml.GetOrCreateChild(commonSlideData, "p", "spTree", PptxNamespaces.Presentation);
        EnsureShapeTreeDefaults(shapeTree);
        return shapeTree;
    }

    internal void EnsureShapeTreeDefaults(XmlElement shapeTree)
    {
        if (PptxXml.FindFirstChild(shapeTree, PptxNamespaces.Presentation, "nvGrpSpPr") == null)
        {
            XmlElement nonVisual = Document.CreateElement("p", "nvGrpSpPr", PptxNamespaces.Presentation);
            XmlElement properties = Document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(properties, "", "", "id", "1");
            PptxXml.SetAttributeValue(properties, "", "", "name", "");
            nonVisual.AppendChild(properties);
            nonVisual.AppendChild(Document.CreateElement("p", "cNvGrpSpPr", PptxNamespaces.Presentation));
            nonVisual.AppendChild(Document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
            shapeTree.InsertBefore(nonVisual, shapeTree.FirstChild);
        }

        if (PptxXml.FindFirstChild(shapeTree, PptxNamespaces.Presentation, "grpSpPr") == null)
        {
            XmlElement groupProperties = Document.CreateElement("p", "grpSpPr", PptxNamespaces.Presentation);
            shapeTree.InsertBefore(groupProperties, GetFirstShapeElement(shapeTree));
        }
    }

    internal XmlElement? GetFirstShapeElement(XmlElement shapeTree)
    {
        for (int index = 0; index < shapeTree.ChildNodes.Count; index++)
        {
            XmlNode? child = shapeTree.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (IsShapeElement(element))
            {
                return element;
            }
        }

        return null;
    }

    internal static XmlElement CreateShapeElement(XmlDocument document, string shapeType, int id, string name, PptxBounds bounds)
    {
        string preset = NormalizeShapeType(shapeType);
        XmlElement shape = document.CreateElement("p", "sp", PptxNamespaces.Presentation);

        XmlElement nonVisual = document.CreateElement("p", "nvSpPr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvSpPr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        shape.AppendChild(nonVisual);

        XmlElement shapeProperties = document.CreateElement("p", "spPr", PptxNamespaces.Presentation);
        XmlElement transformElement = document.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        shapeProperties.AppendChild(transformElement);
        XmlElement geometry = document.CreateElement("a", "prstGeom", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(geometry, "", "", "prst", preset);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        shapeProperties.AppendChild(geometry);
        shape.AppendChild(shapeProperties);

        XmlElement textBody = document.CreateElement("p", "txBody", PptxNamespaces.Presentation);
        textBody.AppendChild(document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing));
        textBody.AppendChild(document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing));
        textBody.AppendChild(document.CreateElement("a", "p", PptxNamespaces.Drawing));
        shape.AppendChild(textBody);
        return shape;
    }

    internal static XmlElement CreateGroupElement(XmlDocument document, int id, string name, PptxBounds bounds, PptxBounds childBounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        if (childBounds == null)
        {
            throw new System.ArgumentNullException("childBounds");
        }

        XmlElement group = document.CreateElement("p", "grpSp", PptxNamespaces.Presentation);
        XmlElement nonVisual = document.CreateElement("p", "nvGrpSpPr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", PptxDiagnostic.CleanText(name));
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvGrpSpPr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        group.AppendChild(nonVisual);

        XmlElement groupProperties = document.CreateElement("p", "grpSpPr", PptxNamespaces.Presentation);
        XmlElement transformElement = document.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
        PptxTransform transform = new PptxTransform(bounds);
        transform.SetChildBounds(childBounds);
        transform.WriteToElement(transformElement);
        groupProperties.AppendChild(transformElement);
        group.AppendChild(groupProperties);
        return group;
    }

    internal static XmlElement CreateGraphicFrameElement(XmlDocument document, int id, string name, string dataUri, string dataXml, PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        string cleanUri = PptxDiagnostic.CleanText(dataUri);
        if (cleanUri.Length == 0)
        {
            throw new System.ArgumentException("Graphic data URI is required.");
        }

        XmlElement frame = document.CreateElement("p", "graphicFrame", PptxNamespaces.Presentation);
        XmlElement nonVisual = document.CreateElement("p", "nvGraphicFramePr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", PptxDiagnostic.CleanText(name));
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvGraphicFramePr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        frame.AppendChild(nonVisual);

        XmlElement transformElement = document.CreateElement("p", "xfrm", PptxNamespaces.Presentation);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        frame.AppendChild(transformElement);

        XmlElement graphic = document.CreateElement("a", "graphic", PptxNamespaces.Drawing);
        XmlElement graphicData = document.CreateElement("a", "graphicData", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(graphicData, "", "", "uri", cleanUri);
        string cleanXml = PptxDiagnostic.CleanText(dataXml);
        if (cleanXml.Length > 0)
        {
            XmlDocument contentDocument = PptxXml.LoadString(cleanXml);
            XmlElement? root = contentDocument.DocumentElement;
            if (root == null)
            {
                throw new System.ArgumentException("Graphic data XML root is required.");
            }

            graphicData.AppendChild(document.ImportNode(root, true));
        }

        graphic.AppendChild(graphicData);
        frame.AppendChild(graphic);
        return frame;
    }

    internal XmlElement CreateConnectorElement(XmlDocument document, string connectorType, int id, string name, PptxPoint start, PptxPoint end)
    {
        string preset = PptxConnectorCore.GetPresetGeometry(connectorType);
        XmlElement shape = document.CreateElement("p", "cxnSp", PptxNamespaces.Presentation);

        XmlElement nonVisual = document.CreateElement("p", "nvCxnSpPr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvCxnSpPr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        shape.AppendChild(nonVisual);

        XmlElement shapeProperties = document.CreateElement("p", "spPr", PptxNamespaces.Presentation);
        XmlElement transformElement = document.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
        long x = start.X;
        long y = start.Y;
        long width = end.X - start.X;
        long height = end.Y - start.Y;
        bool flipHorizontal = false;
        bool flipVertical = false;
        if (width < 0L)
        {
            x = end.X;
            width = start.X - end.X;
            flipHorizontal = true;
        }

        if (height < 0L)
        {
            y = end.Y;
            height = start.Y - end.Y;
            flipVertical = true;
        }

        PptxTransform transform = new PptxTransform(new PptxBounds(x, y, width, height));
        transform.FlipHorizontal = flipHorizontal;
        transform.FlipVertical = flipVertical;
        transform.WriteToElement(transformElement);
        shapeProperties.AppendChild(transformElement);
        XmlElement geometry = document.CreateElement("a", "prstGeom", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(geometry, "", "", "prst", preset);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        shapeProperties.AppendChild(geometry);
        shape.AppendChild(shapeProperties);
        return shape;
    }

    internal XmlElement CreatePictureElement(XmlDocument document, int id, string name, string relationshipId, PptxBounds bounds)
    {
        XmlElement picture = document.CreateElement("p", "pic", PptxNamespaces.Presentation);

        XmlElement nonVisual = document.CreateElement("p", "nvPicPr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvPicPr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        picture.AppendChild(nonVisual);

        XmlElement blipFill = document.CreateElement("p", "blipFill", PptxNamespaces.Presentation);
        XmlElement blip = document.CreateElement("a", "blip", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "embed", relationshipId);
        blipFill.AppendChild(blip);
        XmlElement stretch = document.CreateElement("a", "stretch", PptxNamespaces.Drawing);
        stretch.AppendChild(document.CreateElement("a", "fillRect", PptxNamespaces.Drawing));
        blipFill.AppendChild(stretch);
        picture.AppendChild(blipFill);

        XmlElement shapeProperties = document.CreateElement("p", "spPr", PptxNamespaces.Presentation);
        XmlElement transformElement = document.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        shapeProperties.AppendChild(transformElement);
        XmlElement geometry = document.CreateElement("a", "prstGeom", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(geometry, "", "", "prst", PptxShapeType.Rectangle);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        shapeProperties.AppendChild(geometry);
        picture.AppendChild(shapeProperties);
        return picture;
    }

    internal XmlElement CreateMediaElement(XmlDocument document, int id, string name, string relationshipId, string mediaType, PptxBounds bounds)
    {
        XmlElement picture = document.CreateElement("p", "pic", PptxNamespaces.Presentation);
        XmlElement nonVisual = document.CreateElement("p", "nvPicPr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvPicPr", PptxNamespaces.Presentation));
        XmlElement applicationProperties = document.CreateElement("p", "nvPr", PptxNamespaces.Presentation);
        string elementName = "videoFile";
        if (mediaType == PptxMediaType.Audio)
        {
            elementName = "audioFile";
        }

        XmlElement media = document.CreateElement("a", elementName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(media, "r", PptxNamespaces.Relationships, "link", relationshipId);
        applicationProperties.AppendChild(media);
        nonVisual.AppendChild(applicationProperties);
        picture.AppendChild(nonVisual);
        XmlElement blipFill = document.CreateElement("p", "blipFill", PptxNamespaces.Presentation);
        blipFill.AppendChild(document.CreateElement("a", "blip", PptxNamespaces.Drawing));
        XmlElement stretch = document.CreateElement("a", "stretch", PptxNamespaces.Drawing);
        stretch.AppendChild(document.CreateElement("a", "fillRect", PptxNamespaces.Drawing));
        blipFill.AppendChild(stretch);
        picture.AppendChild(blipFill);
        XmlElement shapeProperties = document.CreateElement("p", "spPr", PptxNamespaces.Presentation);
        XmlElement transformElement = document.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        shapeProperties.AppendChild(transformElement);
        XmlElement geometry = document.CreateElement("a", "prstGeom", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(geometry, "", "", "prst", PptxShapeType.Rectangle);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        shapeProperties.AppendChild(geometry);
        picture.AppendChild(shapeProperties);
        return picture;
    }

    internal XmlElement CreateEmbeddedObjectElement(XmlDocument document, int id, string frameName, string relationshipId, string programId, string objectName, PptxBounds bounds)
    {
        XmlElement frame = document.CreateElement("p", "graphicFrame", PptxNamespaces.Presentation);
        XmlElement nonVisual = document.CreateElement("p", "nvGraphicFramePr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", frameName);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvGraphicFramePr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        frame.AppendChild(nonVisual);
        XmlElement transformElement = document.CreateElement("p", "xfrm", PptxNamespaces.Presentation);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        frame.AppendChild(transformElement);
        XmlElement graphic = document.CreateElement("a", "graphic", PptxNamespaces.Drawing);
        XmlElement graphicData = document.CreateElement("a", "graphicData", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(graphicData, "", "", "uri", "http://schemas.openxmlformats.org/presentationml/2006/ole");
        XmlElement embeddedObject = document.CreateElement("p", "oleObj", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(embeddedObject, "r", PptxNamespaces.Relationships, "id", relationshipId);
        PptxXml.SetAttributeValue(embeddedObject, "", "", "progId", PptxDiagnostic.CleanText(programId));
        PptxXml.SetAttributeValue(embeddedObject, "", "", "name", PptxDiagnostic.CleanText(objectName));
        PptxXml.SetAttributeValue(embeddedObject, "", "", "showAsIcon", "0");
        PptxXml.SetAttributeValue(embeddedObject, "", "", "imgW", bounds.Width.ToString());
        PptxXml.SetAttributeValue(embeddedObject, "", "", "imgH", bounds.Height.ToString());
        embeddedObject.AppendChild(document.CreateElement("p", "embed", PptxNamespaces.Presentation));
        graphicData.AppendChild(embeddedObject);
        graphic.AppendChild(graphicData);
        frame.AppendChild(graphic);
        return frame;
    }

    internal XmlElement CreateFreeformElement(XmlDocument document, int id, string name, PptxBounds bounds, List<PptxPoint> points)
    {
        XmlElement shape = CreateShapeElement(document, PptxShapeType.Rectangle, id, name, bounds);
        XmlElement? shapeProperties = PptxTransform.FindShapeProperties(shape);
        if (shapeProperties == null)
        {
            throw new System.ArgumentException("Shape properties are required.");
        }

        RemoveDirectChild(shapeProperties, PptxNamespaces.Drawing, "prstGeom");
        shapeProperties.AppendChild(PptxGeometry.CreateFreeformGeometryElement(document, points, bounds.Width, bounds.Height));
        return shape;
    }

    internal XmlElement CreateTableElement(XmlDocument document, int id, string name, int rowCount, int columnCount, PptxBounds bounds)
    {
        XmlElement frame = document.CreateElement("p", "graphicFrame", PptxNamespaces.Presentation);

        XmlElement nonVisual = document.CreateElement("p", "nvGraphicFramePr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvGraphicFramePr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        frame.AppendChild(nonVisual);

        XmlElement transformElement = document.CreateElement("p", "xfrm", PptxNamespaces.Presentation);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        frame.AppendChild(transformElement);

        XmlElement graphic = document.CreateElement("a", "graphic", PptxNamespaces.Drawing);
        XmlElement graphicData = document.CreateElement("a", "graphicData", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(graphicData, "", "", "uri", "http://schemas.openxmlformats.org/drawingml/2006/table");
        graphicData.AppendChild(CreateTableBodyElement(document, rowCount, columnCount, bounds));
        graphic.AppendChild(graphicData);
        frame.AppendChild(graphic);
        return frame;
    }

    internal XmlElement CreateTableBodyElement(XmlDocument document, int rowCount, int columnCount, PptxBounds bounds)
    {
        XmlElement table = document.CreateElement("a", "tbl", PptxNamespaces.Drawing);
        table.AppendChild(document.CreateElement("a", "tblPr", PptxNamespaces.Drawing));
        XmlElement grid = document.CreateElement("a", "tblGrid", PptxNamespaces.Drawing);
        long columnWidth = bounds.Width / (long)columnCount;
        if (columnWidth <= 0L)
        {
            columnWidth = 1L;
        }

        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            XmlElement column = document.CreateElement("a", "gridCol", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(column, "", "", "w", columnWidth.ToString());
            grid.AppendChild(column);
        }

        table.AppendChild(grid);

        long rowHeight = bounds.Height / (long)rowCount;
        if (rowHeight <= 0L)
        {
            rowHeight = 1L;
        }

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            table.AppendChild(CreateTableRowElement(document, columnCount, rowHeight));
        }

        return table;
    }

    internal XmlElement CreateTableRowElement(XmlDocument document, int columnCount, long rowHeight)
    {
        XmlElement row = document.CreateElement("a", "tr", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(row, "", "", "h", rowHeight.ToString());
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            row.AppendChild(CreateTableCellElement(document));
        }

        return row;
    }

    internal XmlElement CreateTableCellElement(XmlDocument document)
    {
        XmlElement cell = document.CreateElement("a", "tc", PptxNamespaces.Drawing);
        XmlElement textBody = document.CreateElement("a", "txBody", PptxNamespaces.Drawing);
        textBody.AppendChild(document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing));
        textBody.AppendChild(document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing));
        textBody.AppendChild(document.CreateElement("a", "p", PptxNamespaces.Drawing));
        cell.AppendChild(textBody);
        cell.AppendChild(document.CreateElement("a", "tcPr", PptxNamespaces.Drawing));
        return cell;
    }

    internal XmlElement CreateChartElement(XmlDocument document, int id, string name, string relationshipId, PptxBounds bounds)
    {
        XmlElement frame = document.CreateElement("p", "graphicFrame", PptxNamespaces.Presentation);

        XmlElement nonVisual = document.CreateElement("p", "nvGraphicFramePr", PptxNamespaces.Presentation);
        XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "name", name);
        nonVisual.AppendChild(properties);
        nonVisual.AppendChild(document.CreateElement("p", "cNvGraphicFramePr", PptxNamespaces.Presentation));
        nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
        frame.AppendChild(nonVisual);

        XmlElement transformElement = document.CreateElement("p", "xfrm", PptxNamespaces.Presentation);
        PptxTransform transform = new PptxTransform(bounds);
        transform.WriteToElement(transformElement);
        frame.AppendChild(transformElement);

        XmlElement graphic = document.CreateElement("a", "graphic", PptxNamespaces.Drawing);
        XmlElement graphicData = document.CreateElement("a", "graphicData", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(graphicData, "", "", "uri", "http://schemas.openxmlformats.org/drawingml/2006/chart");
        XmlElement chart = document.CreateElement("c", "chart", PptxNamespaces.Chart);
        PptxXml.SetAttributeValue(chart, "r", PptxNamespaces.Relationships, "id", relationshipId);
        graphicData.AppendChild(chart);
        graphic.AppendChild(graphicData);
        frame.AppendChild(graphic);
        return frame;
    }

    internal static string NormalizeShapeType(string shapeType)
    {
        return PptxShapeTypeCore.NormalizePresetGeometry(shapeType);
    }

    internal static string CreateShapeName(string shapeType, int id)
    {
        string clean = PptxDiagnostic.CleanText(shapeType);
        if (clean.Length == 0)
        {
            clean = "Shape";
        }

        return clean + " " + id.ToString();
    }

    internal static string CreateConnectorName(string connectorType, int id)
    {
        string clean = PptxConnectorCore.NormalizeConnectorType(connectorType);
        return clean + " Connector " + id.ToString();
    }

    internal static string CreateFreeformName(int id)
    {
        return "Freeform " + id.ToString();
    }

    internal void RemoveDirectChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                root.RemoveChild(child);
            }
        }
    }

    internal static bool IsShapeElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Presentation)
        {
            return false;
        }

        if (element.LocalName == "sp")
        {
            return true;
        }

        if (element.LocalName == "pic")
        {
            return true;
        }

        if (element.LocalName == "graphicFrame")
        {
            return true;
        }

        if (element.LocalName == "cxnSp")
        {
            return true;
        }

        if (element.LocalName == "grpSp")
        {
            return true;
        }

        return false;
    }

    internal void SetShapeId(XmlElement shape, int id)
    {
        XmlElement? properties = FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties != null)
        {
            PptxXml.SetAttributeValue(properties, "", "", "id", id.ToString());
        }
    }

    internal static void SetShapeName(XmlElement shape, string name)
    {
        XmlElement? properties = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties != null)
        {
            PptxXml.SetAttributeValue(properties, "", "", "name", PptxDiagnostic.CleanText(name));
        }
    }

    internal XmlElement? FindFirstDescendant(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }

            XmlElement? descendant = FindFirstDescendant(element, namespaceUri, localName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
