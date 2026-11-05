using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents slide collection data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxSlideCollection
{
    internal PptxPresentation Presentation;
    /// <summary>
    /// Contains the slides in package or presentation order.
    /// </summary>
    public List<PptxSlide> Slides;

    internal PptxSlideCollection(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        Presentation = presentation;
        Slides = presentation.Slides;
    }
    internal int Count()
    {
        return Slides.Count;
    }
    internal PptxSlide Get(int index)
    {
        ValidateIndex(index);
        return Slides[index];
    }
    internal PptxSlide? FindById(int slideId)
    {
        for (int index = 0; index < Slides.Count; index++)
        {
            if (Slides[index].SlideId == slideId)
            {
                return Slides[index];
            }
        }

        return null;
    }
    internal PptxSlide Add()
    {
        return Insert(Slides.Count);
    }
    internal PptxSlide AddWithLayout(string layoutName)
    {
        return InsertWithLayoutName(Slides.Count, layoutName);
    }
    internal PptxSlide Insert(int index)
    {
        PptxSlideLayout? layout = null;
        if (Presentation.SlideLayouts.Count > 0)
        {
            layout = Presentation.SlideLayouts[0];
        }

        return InsertWithLayout(index, layout);
    }
    internal PptxSlide InsertWithLayoutName(int index, string layoutName)
    {
        PptxSlideLayout? layout = Presentation.FindSlideLayoutByName(layoutName);
        if (layout == null)
        {
            throw new System.ArgumentException("Slide layout was not found.");
        }

        return InsertWithLayout(index, layout);
    }
    internal PptxSlide InsertWithLayout(int index, PptxSlideLayout? layout)
    {
        ValidateInsertIndex(index);
        if (Presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        string partName = PptxSlideAllocator.AllocateSlidePartName(Presentation.Package);
        int slideId = PptxSlideAllocator.AllocateSlideId(Presentation);
        string relationshipId = PptxSlideAllocator.AllocateSlideRelationshipId(Presentation);
        PptxPart part = Presentation.Package.AddPart(partName, PptxContentTypes.Slide, CreateSlideBytes(layout));
        PptxRelationship relationship = Presentation.Package.AddRelationshipWithId(Presentation.Part.PartName, relationshipId, PptxRelationshipTypes.Slide, PptxSlideAllocator.GetPresentationTargetForSlidePartName(partName), PptxRelationshipTargetMode.Internal);

        PptxSlide slide = new PptxSlide(Presentation, part, relationship.Id);
        slide.SlideId = slideId;
        ConnectSlideToLayout(slide, layout);
        slide.ReloadPlaceholders();
        Presentation.ApplySlidePlaceholderInheritance(slide);
        Slides.Insert(index, slide);
        RewritePresentationSlideIdList();
        return slide;
    }

    internal byte[] CreateSlideBytes(PptxSlideLayout? layout)
    {
        if (layout == null)
        {
            return Encoding.UTF8.GetBytes(PptxMinimumPresentation.SlideXml());
        }

        XmlDocument slideDocument = PptxXml.LoadString(PptxMinimumPresentation.SlideXml());
        XmlElement? root = slideDocument.DocumentElement;
        if (root == null)
        {
            return PptxXml.WriteBytes(slideDocument);
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData == null)
        {
            return PptxXml.WriteBytes(slideDocument);
        }

        XmlElement? shapeTree = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "spTree");
        if (shapeTree == null)
        {
            return PptxXml.WriteBytes(slideDocument);
        }

        int nextShapeId = GetNextShapeId(shapeTree);
        nextShapeId = AppendPlaceholderShapesFromPart(layout.Part, slideDocument, shapeTree, layout.Placeholders, nextShapeId);

        if (layout.Master != null)
        {
            for (int index = 0; index < layout.Master.Placeholders.Count; index++)
            {
                PptxPlaceholder masterPlaceholder = layout.Master.Placeholders[index];
                PptxPlaceholder? layoutPlaceholder = Presentation.FindMatchingLayoutPlaceholder(layout, masterPlaceholder);
                if (layoutPlaceholder == null)
                {
                    List<PptxPlaceholder> placeholders = new List<PptxPlaceholder>();
                    placeholders.Add(masterPlaceholder);
                    nextShapeId = AppendPlaceholderShapesFromPart(layout.Master.Part, slideDocument, shapeTree, placeholders, nextShapeId);
                }
            }
        }

        return PptxXml.WriteBytes(slideDocument);
    }

    internal int AppendPlaceholderShapesFromPart(PptxPart sourcePart, XmlDocument slideDocument, XmlElement shapeTree, List<PptxPlaceholder> placeholders, int nextShapeId)
    {
        if (placeholders.Count == 0)
        {
            return nextShapeId;
        }

        XmlDocument sourceDocument = PptxXml.LoadBytes(Presentation.Package.GetPartBytesForSave(sourcePart));
        XmlElement? root = sourceDocument.DocumentElement;
        if (root == null)
        {
            return nextShapeId;
        }

        for (int index = 0; index < placeholders.Count; index++)
        {
            XmlElement? shape = FindShapeForPlaceholder(root, placeholders[index]);
            if (shape != null)
            {
                XmlNode imported = slideDocument.ImportNode(shape, true);
                if (imported.NodeType == XmlNodeType.Element)
                {
                    SetShapeId((XmlElement)imported, nextShapeId);
                    nextShapeId = nextShapeId + 1;
                }

                shapeTree.AppendChild(imported);
            }
        }

        return nextShapeId;
    }

    internal XmlElement? FindShapeForPlaceholder(XmlNode node, PptxPlaceholder placeholder)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "ph")
            {
                PptxPlaceholder candidate = Presentation.CreatePlaceholder(element, "");
                if (PptxPresentation.PlaceholderMatches(candidate, placeholder))
                {
                    return Presentation.FindPlaceholderShape(element);
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                XmlElement? result = FindShapeForPlaceholder(child, placeholder);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    internal int GetNextShapeId(XmlElement shapeTree)
    {
        int maximum = FindMaximumShapeId(shapeTree, 0);
        return maximum + 1;
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

    internal void SetShapeId(XmlElement shape, int shapeId)
    {
        XmlElement? properties = Presentation.FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties != null)
        {
            PptxXml.SetAttributeValue(properties, "", "", "id", shapeId.ToString());
        }
    }
    internal PptxSlide Clone(int index)
    {
        return CloneToIndex(index, index + 1);
    }
    internal PptxSlide CloneToIndex(int index, int insertIndex)
    {
        return PptxSlideCloner.Clone(this, index, insertIndex);
    }
    internal PptxSlide Import(PptxPresentation sourcePresentation, int sourceIndex)
    {
        return ImportToIndex(sourcePresentation, sourceIndex, Slides.Count);
    }
    internal PptxSlide ImportToIndex(PptxPresentation sourcePresentation, int sourceIndex, int insertIndex)
    {
        return PptxSlideImporter.Import(this, sourcePresentation, sourceIndex, insertIndex);
    }
    internal void RemoveAt(int index)
    {
        ValidateIndex(index);
        if (Presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        PptxSlide slide = Slides[index];
        Presentation.RemoveNotesSlide(slide);
        Presentation.Package.RemoveRelationship(Presentation.Part.PartName, slide.RelationshipId);
        Presentation.Package.RemovePart(slide.PartName);
        Slides.RemoveAt(index);
        RewritePresentationSlideIdList();
        Presentation.RemoveSlideIdReferences(slide.SlideId);
    }
    internal bool RemoveById(int slideId)
    {
        for (int index = 0; index < Slides.Count; index++)
        {
            if (Slides[index].SlideId == slideId)
            {
                RemoveAt(index);
                return true;
            }
        }

        return false;
    }
    internal void Move(int fromIndex, int toIndex)
    {
        ValidateIndex(fromIndex);
        ValidateIndex(toIndex);
        if (fromIndex == toIndex)
        {
            return;
        }

        PptxSlide slide = Slides[fromIndex];
        Slides.RemoveAt(fromIndex);
        Slides.Insert(toIndex, slide);
        RewritePresentationSlideIdList();
    }

    internal void ValidateIndex(int index)
    {
        if (index < 0 || index >= Slides.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ValidateInsertIndex(int index)
    {
        if (index < 0 || index > Slides.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void ConnectSlideToDefaultLayout(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (Presentation.SlideLayouts.Count == 0)
        {
            return;
        }

        PptxSlideLayout layout = Presentation.SlideLayouts[0];
        ConnectSlideToLayout(slide, layout);
    }

    internal void ConnectSlideToLayout(PptxSlide slide, PptxSlideLayout? layout)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (layout == null)
        {
            return;
        }

        Presentation.Package.AddRelationship(slide.PartName, PptxRelationshipTypes.SlideLayout, PptxSlideAllocator.GetSlideLayoutTargetForSlidePartName(layout.PartName), PptxRelationshipTargetMode.Internal);
        slide.Layout = layout;
    }

    internal void RewritePresentationSlideIdList()
    {
        if (Presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        XmlDocument document = GetPresentationXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Presentation XML root is required.");
        }

        XmlElement list = PptxXml.GetOrCreateChild(root, "p", "sldIdLst", PptxNamespaces.Presentation);
        RemoveSlideIdElements(list);

        for (int index = 0; index < Slides.Count; index++)
        {
            XmlElement item = PptxXml.AppendChildElement(list, "p", "sldId", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(item, "", "", "id", Slides[index].SlideId.ToString());
            PptxXml.SetAttributeValue(item, "r", PptxNamespaces.Relationships, "id", Slides[index].RelationshipId);
            if (Slides[index].Hidden)
            {
                PptxXml.SetAttributeValue(item, "", "", "show", "0");
            }
        }

        Presentation.Part.SetXmlDocument(document);
    }

    internal XmlDocument GetPresentationXmlDocument()
    {
        if (Presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        if (Presentation.Part.HasXmlDocument)
        {
            return Presentation.Part.XmlDocument;
        }

        return PptxXml.LoadBytes(Presentation.Part.GetBytes());
    }

    internal void RemoveSlideIdElements(XmlElement list)
    {
        for (int index = list.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "sldId")
            {
                list.RemoveChild(child);
            }
        }
    }
}
