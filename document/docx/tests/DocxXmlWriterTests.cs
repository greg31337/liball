using System.Xml;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxXmlWriterTests
{
    [Test]
    public void Writer_WritesNamespaceAwareElementsAndText()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElement("w", "document", global::docx.DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "body", global::docx.DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "p", global::docx.DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "r", global::docx.DocxConstants.NamespaceWordprocessingMl);
        writer.WriteElementText("w", "t", global::docx.DocxConstants.NamespaceWordprocessingMl, "Hello <docx>");
        writer.EndElement();
        writer.EndElement();
        writer.EndElement();
        writer.EndElement();

        string xml = writer.GetXml();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(xml, Does.Not.StartWith("<?xml"));
        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadToElement("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadElementText("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("Hello <docx>"));
    }

    [Test]
    public void Writer_WritesNamespaceDeclarationsAndAttributes()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElement("w", "drawing", global::docx.DocxConstants.NamespaceWordprocessingMl);
        writer.WriteNamespace("r", global::docx.DocxConstants.NamespaceRelationships);
        writer.WriteAttributeWithoutPrefix("id", "local-id");
        writer.WriteAttribute("r", "embed", global::docx.DocxConstants.NamespaceRelationships, "rId5");
        writer.EndElement();

        string xml = writer.GetXml();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("drawing", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("id", ""), Is.EqualTo("local-id"));
        Assert.That(reader.GetAttribute("embed", global::docx.DocxConstants.NamespaceRelationships), Is.EqualTo("rId5"));
    }

    [Test]
    public void Writer_WritesDefaultNamespaceAndEscapedTextNode()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElementWithoutPrefix("root", "urn:root");
        writer.WriteNamespace("", "urn:root");
        writer.WriteText("A < B & C");
        writer.EndElement();

        string xml = writer.GetXml();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(xml, Is.EqualTo("<root xmlns=\"urn:root\">A &lt; B &amp; C</root>"));
        Assert.That(reader.ReadToElement("root", "urn:root"), Is.True);
    }

    [Test]
    public void Writer_CanWrapExternalXmlWriter()
    {
        StringWriter stringWriter = new StringWriter();
        XmlWriter xmlWriter = XmlWriter.Create(stringWriter, global::docx.DocxXmlWriter.CreateSettings());
        global::docx.DocxXmlWriter writer = new global::docx.DocxXmlWriter(xmlWriter);

        writer.StartElementWithoutPrefix("root", "");
        writer.WriteElementTextWithoutPrefix("child", "", "value");
        writer.EndElement();
        writer.Flush();

        Assert.That(stringWriter.ToString(), Is.EqualTo("<root><child>value</child></root>"));
        Assert.Throws<InvalidOperationException>(() => writer.GetXml());
    }

    [Test]
    public void Writer_RejectsInvalidArguments()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxXmlWriter(null!));
        Assert.Throws<ArgumentNullException>(() => writer.StartElement(null!, "p", ""));
        Assert.Throws<ArgumentNullException>(() => writer.StartElement("", null!, ""));
        Assert.Throws<ArgumentNullException>(() => writer.StartElement("", "p", null!));
        Assert.Throws<ArgumentException>(() => writer.StartElement("", "", ""));
        Assert.Throws<ArgumentNullException>(() => writer.WriteElementTextWithoutPrefix("t", "", null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteAttributeWithoutPrefix("id", null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteNamespace(null!, ""));
        Assert.Throws<ArgumentNullException>(() => writer.WriteNamespace("w", null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteText(null!));
    }
}
