using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxXmlReaderTests
{
    [Test]
    public void ReadToElement_FindsElementByLocalNameAndNamespace()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.LocalName(), Is.EqualTo("body"));
        Assert.That(reader.NamespaceUri(), Is.EqualTo(global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.That(reader.ReadToElement("body", "urn:other"), Is.True);
        Assert.That(reader.NamespaceUri(), Is.EqualTo("urn:other"));
        Assert.That(reader.ReadToElement("missing", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.False);
    }

    [Test]
    public void ReadToChildElement_SearchesOnlyDirectChildren()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("document", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadToChildElement("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.False);

        reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadToChildElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.Depth(), Is.EqualTo(2));
    }

    [Test]
    public void ReadToNextSiblingElement_SkipsCurrentSubtree()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("id", ""), Is.EqualTo("first"));
        Assert.That(reader.ReadToNextSiblingElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("id", ""), Is.EqualTo("second"));
        Assert.That(reader.ReadToNextSiblingElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.False);
    }

    [Test]
    public void ReadElementText_ReadsExpectedElementText()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadElementText("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo("First"));
    }

    [Test]
    public void ReadPastCurrentElement_SkipsNestedAndEmptyElementSubtrees()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root><parent><child><leaf /></child></parent><empty /><last /></root>");

        Assert.That(reader.ReadToElement("parent", ""), Is.True);

        reader.ReadPastCurrentElement();

        Assert.That(reader.IsElement("empty", ""), Is.True);

        reader.ReadPastCurrentElement();

        Assert.That(reader.IsElement("last", ""), Is.True);
    }

    [Test]
    public void IsEndElement_DetectsCurrentEndElement()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root><child>value</child></root>");

        Assert.That(reader.ReadToElement("child", ""), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.IsEndElement("child", ""), Is.True);
    }

    [Test]
    public void GetAttribute_ReturnsNamespaceAwareAttributeOrEmptyString()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.GetAttribute("id", ""), Is.EqualTo("first"));
        Assert.That(reader.GetAttribute("custom", "urn:other"), Is.EqualTo("one"));
        Assert.That(reader.GetAttribute("missing", ""), Is.EqualTo(""));
    }

    [Test]
    public void XmlReader_RejectsInvalidArgumentsAndUnexpectedTextElement()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml());

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxXmlReader(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlReader.Create(null!));
        Assert.Throws<ArgumentNullException>(() => reader.IsElement(null!, ""));
        Assert.Throws<ArgumentNullException>(() => reader.IsElement("p", null!));
        Assert.Throws<ArgumentException>(() => reader.IsElement("", ""));
        Assert.Throws<ArgumentNullException>(() => reader.IsEndElement(null!, ""));
        Assert.Throws<ArgumentNullException>(() => reader.ReadToElement(null!, ""));
        Assert.Throws<ArgumentNullException>(() => reader.ReadToChildElement(null!, ""));
        Assert.Throws<ArgumentNullException>(() => reader.ReadToNextSiblingElement(null!, ""));
        Assert.Throws<ArgumentNullException>(() => reader.GetAttribute(null!, ""));

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.Throws<InvalidOperationException>(() => reader.ReadElementText("t", global::docx.DocxConstants.NamespaceWordprocessingMl));
    }

    static string CreateDocumentXml()
    {
        return string.Join("\n", new string[]
        {
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:o=\"urn:other\">",
            "  <w:body>",
            "    <w:p id=\"first\" o:custom=\"one\"><w:r><w:t>First</w:t></w:r></w:p>",
            "    <w:p id=\"second\"><w:r><w:t>Second</w:t></w:r><o:body /></w:p>",
            "  </w:body>",
            "  <o:body />",
            "</w:document>"
        });
    }
}
