using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SlideCreationFromLayoutTests
{
    [Test]
    public void AddSlide_WithLayoutNameClonesLayoutAndInheritedPlaceholders()
    {
        PptxPresentation presentation = LoadPresentationWithTitleLayout();

        PptxSlide slide = presentation.AddSlideWithLayout("Title");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.SlideId, Is.EqualTo(257));
        Assert.That(slide.Layout, Is.Not.Null);
        Assert.That(slide.Layout!.Name, Is.EqualTo("Title"));
        Assert.That(GetRelationshipTarget(presentation.Package, slide.PartName), Is.EqualTo(PptxPartNames.FirstSlideLayout));

        PptxSlide reopenedSlide = reopened.GetSlide(1);
        Assert.That(reopenedSlide.Layout, Is.Not.Null);
        Assert.That(reopenedSlide.Layout!.Name, Is.EqualTo("Title"));
        Assert.That(GetPlaceholderCount(reopened.Package, reopenedSlide.PartName), Is.EqualTo(2));
        Assert.That(GetShapeNameForPlaceholder(reopened.Package, reopenedSlide.PartName, "title", 1), Is.EqualTo("Layout Title"));
        Assert.That(GetShapeNameForPlaceholder(reopened.Package, reopenedSlide.PartName, "body", 2), Is.EqualTo("Master Body"));
        Assert.That(GetShapeIdForPlaceholder(reopened.Package, reopenedSlide.PartName, "title", 1), Is.EqualTo("2"));
        Assert.That(GetShapeIdForPlaceholder(reopened.Package, reopenedSlide.PartName, "body", 2), Is.EqualTo("3"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void InsertSlide_WithLayoutTypeUsesRequestedPositionAndLayout()
    {
        PptxPresentation presentation = LoadPresentationWithTitleLayout();

        PptxSlide slide = presentation.InsertSlideWithLayout(0, "title");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.SlideId, Is.EqualTo(257));
        Assert.That(presentation.GetSlide(0), Is.SameAs(slide));
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(257));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(0).Layout, Is.Not.Null);
        Assert.That(reopened.GetSlide(0).Layout!.Type, Is.EqualTo("title"));
        Assert.That(GetPlaceholderCount(reopened.Package, reopened.GetSlide(0).PartName), Is.EqualTo(2));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AddSlide_WithMissingLayoutNameThrows()
    {
        PptxPresentation presentation = LoadPresentationWithTitleLayout();

        Assert.Throws<System.ArgumentException>(() => presentation.AddSlideWithLayout("Missing Layout"));
    }

    static PptxPresentation LoadPresentationWithTitleLayout()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string GetRelationshipTarget(PptxPackage package, string slidePartName)
    {
        PptxRelationshipCollection relationships = package.GetRelationships(slidePartName);
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.SlideLayout)
            {
                return relationship.TargetPartName;
            }
        }

        return "";
    }

    static int GetPlaceholderCount(PptxPackage package, string slidePartName)
    {
        XmlDocument document = PptxXml.LoadBytes(package.GetPartBytesForSave(package.GetPart(slidePartName)));
        return document.GetElementsByTagName("ph", PptxNamespaces.Presentation).Count;
    }

    static string GetShapeNameForPlaceholder(PptxPackage package, string slidePartName, string type, int index)
    {
        XmlElement? shape = GetShapeForPlaceholder(package, slidePartName, type, index);
        if (shape == null)
        {
            return "";
        }

        XmlElement? properties = FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(properties, "", "name");
    }

    static string GetShapeIdForPlaceholder(PptxPackage package, string slidePartName, string type, int index)
    {
        XmlElement? shape = GetShapeForPlaceholder(package, slidePartName, type, index);
        if (shape == null)
        {
            return "";
        }

        XmlElement? properties = FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(properties, "", "id");
    }

    static XmlElement? GetShapeForPlaceholder(PptxPackage package, string slidePartName, string type, int index)
    {
        XmlDocument document = PptxXml.LoadBytes(package.GetPartBytesForSave(package.GetPart(slidePartName)));
        XmlNodeList nodes = document.GetElementsByTagName("ph", PptxNamespaces.Presentation);
        for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
        {
            XmlElement placeholder = (XmlElement)nodes[nodeIndex]!;
            if (PptxXml.GetAttributeValue(placeholder, "", "type") == type && PptxXml.GetIntAttribute(placeholder, "", "idx", -1) == index)
            {
                return FindShape(placeholder);
            }
        }

        return null;
    }

    static XmlElement? FindShape(XmlElement placeholder)
    {
        XmlNode? node = placeholder.ParentNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "sp")
                {
                    return element;
                }
            }

            node = node.ParentNode;
        }

        return null;
    }

    static XmlElement? FindFirstDescendant(XmlElement root, string namespaceUri, string localName)
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

    static string CreateMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateShapeXml("2", "Master Title", "title", "1") +
            CreateShapeXml("3", "Master Body", "body", "2") +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";
    }

    static string CreateLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"title\" preserve=\"1\">" +
            "<p:cSld name=\"Title\"><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateShapeXml("10", "Layout Title", "title", "1") +
            "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    static string CreateShapeXml(string id, string name, string type, string index)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"" + type + "\" idx=\"" + index + "\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
