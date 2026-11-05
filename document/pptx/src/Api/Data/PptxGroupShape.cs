using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents group shape data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxGroupShape
{
    internal PptxShape Shape;
    /// <summary>
    /// Contains the children in package or presentation order.
    /// </summary>
    public List<PptxShape> Children;

    internal PptxGroupShape(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (!shape.IsGroup())
        {
            throw new System.ArgumentException("Shape is not a group.");
        }

        Shape = shape;
        Children = shape.Children;
    }
    internal int Count()
    {
        return Children.Count;
    }
    internal PptxShape Get(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Children[index];
    }
    internal PptxShape? FindById(int id)
    {
        for (int index = 0; index < Children.Count; index++)
        {
            PptxShape? match = Children[index].FindDescendantById(id);
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
        for (int index = 0; index < Children.Count; index++)
        {
            PptxShape? match = Children[index].FindDescendantByName(clean);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal PptxBounds MapChildBounds(PptxShape child)
    {
        if (child == null)
        {
            throw new System.ArgumentNullException("child");
        }

        return Shape.GetTransform().MapChildBounds(child.GetBounds());
    }
    internal PptxPoint MapChildPoint(PptxPoint point)
    {
        return Shape.GetTransform().MapChildPoint(point);
    }
    internal PptxBounds MapChildAnchorBounds(PptxShape child)
    {
        if (child == null)
        {
            throw new System.ArgumentNullException("child");
        }

        return Shape.GetTransform().MapChildAnchorBounds(child.GetAnchorBounds());
    }
    internal int IndexOf(PptxShape child)
    {
        for (int index = 0; index < Children.Count; index++)
        {
            if (Children[index].Element == child.Element)
            {
                return index;
            }
        }

        return -1;
    }
    internal PptxShape AddAutoShape(string presetGeometry, PptxBounds bounds)
    {
        int id = Shape.Tree.AllocateShapeId();
        XmlElement element = PptxShapeTree.CreateShapeElement(Shape.Tree.Document, presetGeometry, id, PptxShapeTree.CreateShapeName(presetGeometry, id), bounds);
        AppendChildElement(element);
        return AddChildModel(element);
    }
    internal PptxShape AddGroup(PptxBounds bounds, PptxBounds childBounds)
    {
        int id = Shape.Tree.AllocateShapeId();
        XmlElement element = PptxShapeTree.CreateGroupElement(Shape.Tree.Document, id, "Group " + id.ToString(), bounds, childBounds);
        AppendChildElement(element);
        return AddChildModel(element);
    }
    internal PptxShape AddGraphicFrame(string dataUri, string dataXml, PptxBounds bounds)
    {
        int id = Shape.Tree.AllocateShapeId();
        XmlElement element = PptxShapeTree.CreateGraphicFrameElement(Shape.Tree.Document, id, "Graphic Frame " + id.ToString(), dataUri, dataXml, bounds);
        AppendChildElement(element);
        return AddChildModel(element);
    }
    internal PptxShape CloneShape(PptxShape source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        XmlNode imported = Shape.Tree.Document.ImportNode(source.Element, true);
        XmlElement element = (XmlElement)imported;
        PptxShapeTree.AssignCloneIds(element, Shape.Tree.AllocateShapeId());
        PptxShapeTree.SetShapeName(element, source.Name + " Copy");
        AppendChildElement(element);
        return AddChildModel(element);
    }
    internal PptxShape Clone(int index)
    {
        return CloneShape(Get(index));
    }
    internal void RemoveAt(int index)
    {
        PptxShape child = Get(index);
        Shape.Element.RemoveChild(child.Element);
        Children.RemoveAt(index);
        Shape.Tree.MarkDirty();
    }
    internal void Move(int fromIndex, int toIndex)
    {
        PptxShape child = Get(fromIndex);
        Get(toIndex);
        if (fromIndex == toIndex)
        {
            return;
        }

        Children.RemoveAt(fromIndex);
        Shape.Element.RemoveChild(child.Element);
        XmlElement? before = null;
        if (toIndex < Children.Count)
        {
            before = Children[toIndex].Element;
        }

        if (before == null)
        {
            AppendChildElement(child.Element);
        }
        else
        {
            Shape.Element.InsertBefore(child.Element, before);
        }

        Children.Insert(toIndex, child);
        Shape.Tree.MarkDirty();
    }
    internal void BringToFront(PptxShape child)
    {
        int index = IndexOf(child);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a direct child of this group.");
        }

        Move(index, Children.Count - 1);
    }
    internal void SendToBack(PptxShape child)
    {
        int index = IndexOf(child);
        if (index < 0)
        {
            throw new System.ArgumentException("Shape is not a direct child of this group.");
        }

        Move(index, 0);
    }
    internal void AppendChildElement(XmlElement element)
    {
        XmlElement? extensionList = PptxXml.FindFirstChild(Shape.Element, PptxNamespaces.Presentation, "extLst");
        if (extensionList == null)
        {
            Shape.Element.AppendChild(element);
        }
        else
        {
            Shape.Element.InsertBefore(element, extensionList);
        }
    }
    internal PptxShape AddChildModel(XmlElement element)
    {
        PptxShape child = new PptxShape(Shape.Tree, element);
        child.ParentGroup = Shape;
        child.RefreshEffectiveBounds();
        Children.Add(child);
        Shape.Tree.MarkDirty();
        return child;
    }
}
