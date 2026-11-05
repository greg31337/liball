using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxXmlHelperTests
{
    [Test]
    public void FindFirstChild_IsNamespaceAwareAndUsesDirectChildrenOnly()
    {
        string xml =
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">" +
            "<a:sldIdLst />" +
            "<p:wrapper><p:sldIdLst id=\"nested\" /></p:wrapper>" +
            "<p:sldIdLst id=\"direct\" />" +
            "</p:presentation>";

        XmlDocument document = PptxXml.LoadString(xml);
        XmlElement root = document.DocumentElement!;

        XmlElement? child = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "sldIdLst");

        Assert.That(child, Is.Not.Null);
        Assert.That(child!.GetAttribute("id"), Is.EqualTo("direct"));
    }

    [Test]
    public void FindFirstChildByAttribute_MatchesNamespacedAttributes()
    {
        string xml =
            "<p:sld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<p:pic r:id=\"rId1\" />" +
            "<p:pic r:id=\"rId2\" />" +
            "</p:sld>";

        XmlDocument document = PptxXml.LoadString(xml);
        XmlElement root = document.DocumentElement!;

        XmlElement? child = PptxXml.FindFirstChildByAttribute(root, PptxNamespaces.Presentation, "pic", PptxNamespaces.Relationships, "id", "rId2");

        Assert.That(child, Is.Not.Null);
        Assert.That(child!.GetAttribute("id", PptxNamespaces.Relationships), Is.EqualTo("rId2"));
    }

    [Test]
    public void GetOrCreateChild_ReusesExistingElementAndCreatesMissingElement()
    {
        XmlDocument document = PptxXml.LoadString(
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">" +
            "<p:sldIdLst />" +
            "</p:presentation>");
        XmlElement root = document.DocumentElement!;

        XmlElement first = PptxXml.GetOrCreateChild(root, "p", "sldIdLst", PptxNamespaces.Presentation);
        XmlElement second = PptxXml.GetOrCreateChild(root, "p", "sldIdLst", PptxNamespaces.Presentation);
        XmlElement created = PptxXml.GetOrCreateChild(root, "p", "notesSz", PptxNamespaces.Presentation);

        Assert.That(second, Is.SameAs(first));
        Assert.That(created.LocalName, Is.EqualTo("notesSz"));
        Assert.That(created.Prefix, Is.EqualTo("p"));
        Assert.That(root.ChildNodes.Count, Is.EqualTo(2));
    }

    [Test]
    public void AttributeHelpers_SetReadAndParseValues()
    {
        XmlDocument document = PptxXml.LoadString(
            "<p:pic xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" />");
        XmlElement element = document.DocumentElement!;

        PptxXml.SetAttributeValue(element, "", "", "id", "42");
        PptxXml.SetAttributeValue(element, "r", PptxNamespaces.Relationships, "embed", "rId5");
        PptxXml.SetAttributeValue(element, "", "", "scale", "1.25");
        PptxXml.SetAttributeValue(element, "", "", "hidden", "1");
        PptxXml.SetAttributeValue(element, "", "", "badInt", "abc");

        Assert.That(PptxXml.HasAttributeValue(element, "", "id"), Is.True);
        Assert.That(PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "embed"), Is.EqualTo("rId5"));
        Assert.That(PptxXml.GetIntAttribute(element, "", "id", -1), Is.EqualTo(42));
        Assert.That(PptxXml.GetIntAttribute(element, "", "badInt", -1), Is.EqualTo(-1));
        Assert.That(PptxXml.GetDoubleAttribute(element, "", "scale", -1), Is.EqualTo(1.25d));
        Assert.That(PptxXml.GetBoolAttribute(element, "", "hidden", false), Is.True);
        Assert.That(PptxXml.GetBoolAttribute(element, "", "missing", true), Is.True);
        Assert.That(element.GetAttributeNode("embed", PptxNamespaces.Relationships)!.Prefix, Is.EqualTo("r"));
    }

    [Test]
    public void InsertChildInOrder_PlacesKnownChildrenBeforeLaterKnownSiblings()
    {
        XmlDocument document = PptxXml.LoadString(
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:x=\"urn:extension\">" +
            "<x:custom />" +
            "<p:extLst />" +
            "</p:presentation>");
        XmlElement root = document.DocumentElement!;
        string[] order =
        [
            "sldMasterIdLst",
            "notesMasterIdLst",
            "handoutMasterIdLst",
            "sldIdLst",
            "sldSz",
            "notesSz",
            "extLst",
        ];

        PptxXml.InsertChildInOrder(root, PptxXml.CreateElement(document, "p", "notesSz", PptxNamespaces.Presentation), PptxNamespaces.Presentation, order);
        PptxXml.InsertChildInOrder(root, PptxXml.CreateElement(document, "p", "sldIdLst", PptxNamespaces.Presentation), PptxNamespaces.Presentation, order);
        PptxXml.InsertChildInOrder(root, PptxXml.CreateElement(document, "p", "sldMasterIdLst", PptxNamespaces.Presentation), PptxNamespaces.Presentation, order);

        Assert.That(root.ChildNodes[0]!.LocalName, Is.EqualTo("custom"));
        Assert.That(root.ChildNodes[1]!.LocalName, Is.EqualTo("sldMasterIdLst"));
        Assert.That(root.ChildNodes[2]!.LocalName, Is.EqualTo("sldIdLst"));
        Assert.That(root.ChildNodes[3]!.LocalName, Is.EqualTo("notesSz"));
        Assert.That(root.ChildNodes[4]!.LocalName, Is.EqualTo("extLst"));
    }

    [Test]
    public void InsertChildInOrder_AppendsUnknownOrderedChildren()
    {
        XmlDocument document = PptxXml.LoadString(
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">" +
            "<p:sldIdLst />" +
            "</p:presentation>");
        XmlElement root = document.DocumentElement!;
        string[] order = ["sldIdLst"];

        PptxXml.InsertChildInOrder(root, PptxXml.CreateElement(document, "p", "unknown", PptxNamespaces.Presentation), PptxNamespaces.Presentation, order);

        Assert.That(root.ChildNodes[0]!.LocalName, Is.EqualTo("sldIdLst"));
        Assert.That(root.ChildNodes[1]!.LocalName, Is.EqualTo("unknown"));
    }
}
