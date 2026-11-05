using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxXmlTests
{
    [Test]
    public void LoadString_PreservesNamespacesAndWhitespace()
    {
        string xml =
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">" +
            "\n  <p:sldIdLst />\n" +
            "</p:presentation>";

        XmlDocument document = PptxXml.LoadString(xml);

        Assert.That(document.PreserveWhitespace, Is.True);
        Assert.That(document.DocumentElement!.LocalName, Is.EqualTo("presentation"));
        Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo(PptxNamespaces.Presentation));
        Assert.That(document.DocumentElement.ChildNodes[0]!.NodeType, Is.EqualTo(XmlNodeType.Whitespace));
    }

    [Test]
    public void LoadBytes_UsesUtf8AndNamespaceManagerFindsKnownPrefixes()
    {
        string xml =
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">" +
            "<p:sldIdLst />" +
            "</p:presentation>";

        XmlDocument document = PptxXml.LoadBytes(Encoding.UTF8.GetBytes(xml));
        XmlNamespaceManager manager = PptxXml.CreateNamespaceManager(document);
        XmlNode? node = document.SelectSingleNode("/p:presentation/p:sldIdLst", manager);

        Assert.That(node, Is.Not.Null);
        Assert.That(manager.LookupNamespace("p"), Is.EqualTo(PptxNamespaces.Presentation));
        Assert.That(manager.LookupNamespace("a"), Is.EqualTo(PptxNamespaces.Drawing));
        Assert.That(manager.LookupNamespace("r"), Is.EqualTo(PptxNamespaces.Relationships));
        Assert.That(manager.LookupNamespace("ct"), Is.EqualTo(PptxNamespaces.ContentTypes));
    }

    [Test]
    public void LoadStream_RejectsDtds()
    {
        string xml = "<!DOCTYPE root [<!ENTITY xxe SYSTEM \"file:///tmp/secret\">]><root>&xxe;</root>";

        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        XmlException exception = Assert.Throws<XmlException>(() => PptxXml.LoadStream(stream))!;

        Assert.That(exception.Message, Does.Contain("DTD"));
    }

    [Test]
    public void CreateReaderSettings_UsesSafeDefaults()
    {
        XmlReaderSettings settings = PptxXml.CreateReaderSettings();

        Assert.That(settings.DtdProcessing, Is.EqualTo(DtdProcessing.Prohibit));
        Assert.That(settings.MaxCharactersFromEntities, Is.EqualTo(0));
    }

    [Test]
    public void CreateWriterSettings_UsesUtf8WithoutBomAndKeepsXmlDeclaration()
    {
        XmlWriterSettings settings = PptxXml.CreateWriterSettings();

        Assert.That(settings.Encoding, Is.TypeOf<UTF8Encoding>());
        Assert.That(((UTF8Encoding)settings.Encoding).GetPreamble(), Is.Empty);
        Assert.That(settings.OmitXmlDeclaration, Is.False);
        Assert.That(settings.Indent, Is.False);
    }

    [Test]
    public void WriteString_NormalizesKnownPowerPointNamespacePrefixes()
    {
        string xml =
            "<x:presentation xmlns:x=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:y=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:z=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<x:sldIdLst><x:sldId z:id=\"rId1\"><y:ext /></x:sldId></x:sldIdLst>" +
            "</x:presentation>";

        XmlDocument document = PptxXml.LoadString(xml);
        string serialized = PptxXml.WriteString(document);

        Assert.That(serialized, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
        Assert.That(serialized, Does.Contain("<p:presentation"));
        Assert.That(serialized, Does.Contain("<p:sldIdLst>"));
        Assert.That(serialized, Does.Contain("r:id=\"rId1\""));
        Assert.That(serialized, Does.Contain("<a:ext"));
        Assert.That(serialized, Does.Not.Contain("<x:presentation"));
        Assert.That(serialized, Does.Not.Contain("z:id="));
    }

    [Test]
    public void WriteString_PreservesUnknownExtensionPrefixesForMarkupCompatibility()
    {
        string xml =
            "<x:presentation xmlns:x=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:m=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" " +
            "xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" " +
            "m:Ignorable=\"p14\">" +
            "<p14:creationId val=\"123\" />" +
            "</x:presentation>";

        XmlDocument document = PptxXml.LoadString(xml);
        string serialized = PptxXml.WriteString(document);

        Assert.That(serialized, Does.Contain("<p:presentation"));
        Assert.That(serialized, Does.Contain("mc:Ignorable=\"p14\""));
        Assert.That(serialized, Does.Contain("xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\""));
        Assert.That(serialized, Does.Contain("<p14:creationId"));
    }

    [Test]
    public void WriteBytes_EmitsUtf8WithoutBom()
    {
        XmlDocument document = PptxXml.LoadString(
            "<x:presentation xmlns:x=\"http://schemas.openxmlformats.org/presentationml/2006/main\" />");

        byte[] bytes = PptxXml.WriteBytes(document);

        Assert.That(bytes[0], Is.EqualTo((byte)'<'));
        Assert.That(bytes[1], Is.EqualTo((byte)'?'));
        Assert.That(bytes[2], Is.EqualTo((byte)'x'));
    }

    [Test]
    public void ExistingContentTypesParser_UsesSharedSafeLoader()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Succeeded(), Is.True);
        Assert.That(manifest.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
    }
}
