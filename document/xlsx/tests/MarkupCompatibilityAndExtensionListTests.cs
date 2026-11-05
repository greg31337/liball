using System.Text;
using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class MarkupCompatibilityAndExtensionListTests
{
    [Test]
    public void ReadAndWriteMarkupCompatibilityAttributes_PreservesMcValuesAndNamespaces()
    {
        string xml = "<root xmlns=\"urn:root\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:x14=\"urn:x14\" mc:Ignorable=\"x14\" mc:PreserveAttributes=\"x14:uid\" mc:Future=\"1\" />";
        XlsxMarkupCompatibilityAttributes attributes = ReadMarkupAttributesFromRoot(xml);
        IXlsxMarkupCompatibilityAttributesOperations operations = new XlsxMarkupCompatibilityAttributesOperations();
        MemoryStream stream = new MemoryStream();
        XlsxXmlNamespaceMap map = XlsxXmlWriterHelpers.CreateOpenXmlNamespaceMap();

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "root", "urn:root");
            XlsxMarkupCompatibility.WriteNamespaceDeclarations(writer, attributes);
            XlsxMarkupCompatibility.WriteAttributes(writer, map, attributes);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        string written = Encoding.UTF8.GetString(stream.ToArray());
        XlsxMarkupCompatibilityAttributes roundTrip = ReadMarkupAttributesFromRoot(written);

        Assert.That(operations.HasValues(attributes), Is.True);
        Assert.That(attributes.Ignorable, Is.EqualTo("x14"));
        Assert.That(attributes.PreserveAttributes, Is.EqualTo("x14:uid"));
        Assert.That(attributes.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(attributes.UnknownAttributes[0].LocalName, Is.EqualTo("Future"));
        Assert.That(attributes.NamespaceDeclarations.Count, Is.EqualTo(3));
        Assert.That(written, Does.Contain("xmlns:x14=\"urn:x14\""));
        Assert.That(written, Does.Contain("mc:Ignorable=\"x14\""));
        Assert.That(roundTrip.Ignorable, Is.EqualTo("x14"));
        Assert.That(roundTrip.PreserveAttributes, Is.EqualTo("x14:uid"));
        stream.Dispose();
    }

    [Test]
    public void AlternateContent_PreservesRawXmlAndCountsChoicesAndFallback()
    {
        string xml = "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:x=\"urn:x\">" +
            "<mc:Choice Requires=\"x\"><x:future value=\"1\" /></mc:Choice>" +
            "<mc:Fallback><fallback /></mc:Fallback>" +
            "</mc:AlternateContent>";
        IXlsxMarkupCompatibilityAttributesOperations operations = new XlsxMarkupCompatibilityAttributesOperations();
        XlsxAlternateContent content = operations.ReadAlternateContentString(xml);
        MemoryStream stream = new MemoryStream();

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("root");
            XlsxMarkupCompatibility.WriteAlternateContent(writer, content);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        string written = Encoding.UTF8.GetString(stream.ToArray());

        Assert.That(content.IsValid, Is.True);
        Assert.That(content.ChoiceCount, Is.EqualTo(1));
        Assert.That(content.FallbackCount, Is.EqualTo(1));
        Assert.That(content.RawXml, Does.Contain("AlternateContent"));
        Assert.That(written, Does.Contain("<mc:Choice"));
        Assert.That(written, Does.Contain("<mc:Fallback>"));
        stream.Dispose();
    }

    [Test]
    public void ExtensionListReaderAndWriter_PreserveRawExtensionsAndUnknownElements()
    {
        string xml = "<extLst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x14=\"urn:x14\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<ext uri=\"{11111111-1111-1111-1111-111111111111}\" x:item=\"i\"><x14:future value=\"1\" /></ext>" +
            "<x:Other><x:Child /></x:Other>" +
            "</extLst>";
        XlsxExtensionListReader reader = new XlsxExtensionListReader();
        XlsxExtensionList list = reader.ReadString(xml);
        XlsxExtensionListWriteResult writeResult = new XlsxExtensionListWriter().WriteString(list);
        XlsxExtensionList roundTrip = reader.ReadString(writeResult.Xml);

        Assert.That(list.IsValid, Is.True);
        Assert.That(list.Extensions.Count, Is.EqualTo(1));
        Assert.That(list.Extensions[0].Uri, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(list.Extensions[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(list.Extensions[0].RawXml, Does.Contain("x14:future"));
        Assert.That(list.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(list.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("x14:future"));
        Assert.That(writeResult.Xml, Does.Contain("x:Other"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Extensions.Count, Is.EqualTo(1));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExtensionListReader_ReportsMissingRootAndMissingExtensionUri()
    {
        XlsxExtensionListReader reader = new XlsxExtensionListReader();
        XlsxExtensionList missingRoot = reader.ReadString("");
        XlsxExtensionList missingUri = reader.ReadString("<extLst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><ext /></extLst>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "extensions.missingRoot"), Is.True);
        Assert.That(missingUri.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingUri, "extensions.uriMissing"), Is.True);
        Assert.That(missingUri.Extensions.Count, Is.EqualTo(1));
    }

    static XlsxMarkupCompatibilityAttributes ReadMarkupAttributesFromRoot(string xml)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return XlsxMarkupCompatibility.ReadAttributes(reader);
                    }
                }
            }
        }

        return new XlsxMarkupCompatibilityAttributes();
    }

    static bool HasDiagnostic(XlsxExtensionList list, string code)
    {
        foreach (XlsxDiagnostic diagnostic in list.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
