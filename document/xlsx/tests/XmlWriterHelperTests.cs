using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class XmlWriterHelperTests
{
    [Test]
    public void NamespaceMap_ReusesExistingNamespaceAndResolvesPrefixCollisions()
    {
        XlsxXmlNamespaceMap map = new XlsxXmlNamespaceMap();
        map.AddNamespace("r", XlsxNamespaces.OfficeDocumentRelationships);

        string existingPrefix = map.RequirePrefix(XlsxNamespaces.OfficeDocumentRelationships, "rel");
        string collisionPrefix = map.RequirePrefix("urn:other", "r");

        Assert.That(existingPrefix, Is.EqualTo("r"));
        Assert.That(collisionPrefix, Is.EqualTo("r1"));
        Assert.That(map.GetPrefix("urn:other"), Is.EqualTo("r1"));
        Assert.That(map.Namespaces.Count, Is.EqualTo(2));
    }

    [Test]
    public void NamespaceMap_TracksDefaultNamespaceAsExistingMapping()
    {
        XlsxXmlNamespaceMap map = new XlsxXmlNamespaceMap();
        map.AddNamespace("", XlsxNamespaces.Spreadsheet);
        map.AddNamespace("", XlsxNamespaces.Spreadsheet);

        string prefix = map.RequirePrefix(XlsxNamespaces.Spreadsheet, "x");

        Assert.That(prefix, Is.EqualTo(""));
        Assert.That(map.ContainsNamespace(XlsxNamespaces.Spreadsheet), Is.True);
        Assert.That(map.Namespaces.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteNamespaceDeclarations_WritesPrefixesInStableOrder()
    {
        XlsxXmlNamespaceMap map = new XlsxXmlNamespaceMap();
        map.AddNamespace("z", "urn:z");
        map.AddNamespace("", "urn:default");
        map.AddNamespace("a", "urn:a");
        MemoryStream stream = new MemoryStream();

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartElement("", "root", "urn:default");
            XlsxXmlWriterHelpers.WriteNamespaceDeclarations(writer, map);
            writer.WriteEndElement();
        }

        string xml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(xml.IndexOf("xmlns=\"urn:default\""), Is.LessThan(xml.IndexOf("xmlns:a=\"urn:a\"")));
        Assert.That(xml.IndexOf("xmlns:a=\"urn:a\""), Is.LessThan(xml.IndexOf("xmlns:z=\"urn:z\"")));
        stream.Dispose();
    }

    [Test]
    public void WriteStartElementAndAttributeString_UseStableNamespacePrefixes()
    {
        XlsxXmlNamespaceMap map = XlsxXmlWriterHelpers.CreateOpenXmlNamespaceMap();
        map.AddNamespace("", XlsxNamespaces.Spreadsheet);
        MemoryStream stream = new MemoryStream();

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            XlsxXmlWriterHelpers.WriteStartElement(writer, map, XlsxNamespaces.Spreadsheet, "workbook");
            XlsxXmlWriterHelpers.WriteNamespaceDeclarations(writer, map);
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.OfficeDocumentRelationships, "r", "id", "rId1");
            XlsxXmlWriterHelpers.WriteStartElement(writer, map, XlsxNamespaces.Drawing, "themeHint");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        string xml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(xml, Does.Contain("<workbook"));
        Assert.That(xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\""));
        Assert.That(xml, Does.Contain("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\""));
        Assert.That(xml, Does.Contain("r:id=\"rId1\""));
        Assert.That(xml, Does.Contain("<a:themeHint"));
        stream.Dispose();
    }

    [Test]
    public void CreateStableWriterSettings_UsesUtf8WithoutBomAndLeavesOutputOpen()
    {
        XmlWriterSettings settings = XlsxXmlWriterHelpers.CreateStableWriterSettings();
        MemoryStream stream = new MemoryStream();

        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("root");
            writer.WriteEndElement();
        }

        long lengthAfterXml = stream.Length;
        stream.WriteByte(32);
        byte[] bytes = stream.ToArray();

        Assert.That(settings.CloseOutput, Is.False);
        Assert.That(settings.OmitXmlDeclaration, Is.False);
        Assert.That(bytes[0], Is.Not.EqualTo(239));
        Assert.That(stream.Length, Is.EqualTo(lengthAfterXml + 1));
        stream.Dispose();
    }

    [Test]
    public void ContentTypesWriter_UsesSharedStableWriterSettings()
    {
        XmlWriterSettings shared = XlsxXmlWriterHelpers.CreateStableWriterSettings();
        XmlWriterSettings contentTypes = XlsxContentTypesWriter.CreateXmlWriterSettings();

        Assert.That(contentTypes.Encoding.WebName, Is.EqualTo(shared.Encoding.WebName));
        Assert.That(contentTypes.OmitXmlDeclaration, Is.EqualTo(shared.OmitXmlDeclaration));
        Assert.That(contentTypes.CloseOutput, Is.EqualTo(shared.CloseOutput));
    }
}
