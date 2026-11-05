using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class XmlLayerTests
{
    [Test]
    public void ValueParsing_CoversPrimitiveLexicalFormsAndStableWriters()
    {
        XlsxXmlValueParseResult boolean = XlsxXmlValueParser.ParseBoolean(" false ");
        XlsxXmlValueParseResult integer = XlsxXmlValueParser.ParseInteger("+42");
        XlsxXmlValueParseResult unsigned = XlsxXmlValueParser.ParseUnsignedInteger("0007");
        XlsxXmlValueParseResult number = XlsxXmlValueParser.ParseDouble("6.02e3");
        XlsxXmlValueParseResult relationship = XlsxXmlValueParser.ParseRelationshipId("_rel.1-2");
        XlsxXmlValueParseResult badRelationship = XlsxXmlValueParser.ParseRelationshipId("bad id");

        Assert.That(boolean.IsValid, Is.True);
        Assert.That(boolean.BooleanValue, Is.False);
        Assert.That(integer.IsValid, Is.True);
        Assert.That(integer.IntValue, Is.EqualTo(42));
        Assert.That(unsigned.IsValid, Is.True);
        Assert.That(unsigned.UnsignedIntValue, Is.EqualTo(7));
        Assert.That(number.IsValid, Is.True);
        Assert.That(number.DoubleValue, Is.EqualTo(6020d).Within(0.000000001d));
        Assert.That(relationship.IsValid, Is.True);
        Assert.That(relationship.TextValue, Is.EqualTo("_rel.1-2"));
        Assert.That(badRelationship.IsValid, Is.False);
        Assert.That(HasDiagnostic(badRelationship.Validation, "xml.relationshipId.invalid"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteBoolean(false), Is.EqualTo("0"));
        Assert.That(XlsxXmlValueWriter.WriteInteger(42), Is.EqualTo("42"));
        Assert.That(XlsxXmlValueWriter.WriteUnsignedInteger(7), Is.EqualTo("7"));
        Assert.That(XlsxXmlValueWriter.WriteRelationshipId("_rel.1-2"), Is.EqualTo("_rel.1-2"));
    }

    [Test]
    public void NamespaceWriterHelpers_WriteStableDeclarationsElementsAndAttributes()
    {
        MemoryStream stream = new MemoryStream();
        XlsxXmlNamespaceMap map = XlsxXmlWriterHelpers.CreateOpenXmlNamespaceMap();
        map.AddNamespace("", XlsxNamespaces.Spreadsheet);

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            XlsxXmlWriterHelpers.WriteStartElement(writer, map, XlsxNamespaces.Spreadsheet, "worksheet");
            XlsxXmlWriterHelpers.WriteNamespaceDeclarations(writer, map);
            XlsxXmlWriterHelpers.WriteStartElement(writer, map, XlsxNamespaces.Chart, "chartSpace");
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.OfficeDocumentRelationships, "r", "id", "rId1");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        string xml = Encoding.UTF8.GetString(stream.ToArray());

        Assert.That(xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\""));
        Assert.That(xml, Does.Contain("xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\""));
        Assert.That(xml, Does.Contain("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\""));
        Assert.That(xml, Does.Contain("<c:chartSpace r:id=\"rId1\" />"));
        stream.Dispose();
    }

    [Test]
    public void MarkupCompatibility_WorkbookPartRoundTripsMcAttributesNamespacesAndUnknownXml()
    {
        string xml = "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\" xmlns:mc=\"" + XlsxNamespaces.MarkupCompatibility + "\" xmlns:x15=\"urn:x15\" mc:Ignorable=\"x15\" mc:ProcessContent=\"x15:future\" x15:root=\"1\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" x15:sheet=\"s\" /></sheets>" +
            "<x15:future value=\"1\" />" +
            "</workbook>";

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadString(xml);
        XlsxWorkbookPartWriteResult write = new XlsxWorkbookPartWriter().WriteString(part);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.MarkupCompatibility.Ignorable, Is.EqualTo("x15"));
        Assert.That(part.MarkupCompatibility.ProcessContent, Is.EqualTo("x15:future"));
        Assert.That(part.MarkupCompatibility.NamespaceDeclarations.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Sheets[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("mc:Ignorable=\"x15\""));
        Assert.That(write.Xml, Does.Contain("mc:ProcessContent=\"x15:future\""));
        Assert.That(write.Xml, Does.Contain("x15:future"));
        Assert.That(roundTrip.MarkupCompatibility.Ignorable, Is.EqualTo("x15"));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void XmlReaderLimits_ReportDepthAndTokenDiagnostics()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxXmlDepth = 2;
        options.MaxXmlTokenCharacters = 12;
        string xml = "<root><a><b><c attr=\"01234567890123456789\">value</c></b></a></root>";

        XlsxXmlReadLimitResult result = XlsxXmlReaderHelpers.ValidateStringLimits(xml, options, "/xl/test.xml");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.MaxObservedDepth, Is.GreaterThan(2));
        Assert.That(result.MaxObservedTokenCharacters, Is.GreaterThan(12));
        Assert.That(HasDiagnostic(result.Validation, "xml.maxDepth"), Is.True);
        Assert.That(HasDiagnostic(result.Validation, "xml.maxTokenCharacters"), Is.True);
    }

    [Test]
    public void RawXmlPreservation_RoundTripsSharedStringUnknownAttributesAndFutureElements()
    {
        string xml = "<sst xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:x=\"urn:x\" x:root=\"r\" count=\"1\" uniqueCount=\"1\">" +
            "<si x:item=\"i\"><t>Value</t><x:future value=\"1\" /></si>" +
            "<x:futureRoot />" +
            "</sst>";

        XlsxSharedStringTablePart part = new XlsxSharedStringTableReader().ReadString(xml);
        XlsxSharedStringTableWriteResult write = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(write.Xml, Does.Contain("x:item=\"i\""));
        Assert.That(write.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Items[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    static bool HasDiagnostic(XlsxValidationResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
