using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CorePropertiesTests
{
    [Test]
    public void ReadString_ParsesStandardCorePropertiesDatesAndUnknowns()
    {
        string xml = "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<dc:title>Budget</dc:title>" +
            "<dc:subject>Planning</dc:subject>" +
            "<dc:creator>Ada</dc:creator>" +
            "<cp:keywords>xlsx,tests</cp:keywords>" +
            "<dc:description>Quarterly plan</dc:description>" +
            "<cp:lastModifiedBy>Grace</cp:lastModifiedBy>" +
            "<cp:revision>7</cp:revision>" +
            "<cp:category>Finance</cp:category>" +
            "<cp:contentStatus>Draft</cp:contentStatus>" +
            "<dcterms:created xsi:type=\"dcterms:W3CDTF\">2026-08-09T12:34:56Z</dcterms:created>" +
            "<dcterms:modified xsi:type=\"dcterms:W3CDTF\">2026-08-10T01:02:03Z</dcterms:modified>" +
            "<dc:language>en-US</dc:language>" +
            "<dc:identifier>ID-1</dc:identifier>" +
            "<cp:version>1.0</cp:version>" +
            "<x:future><x:child /></x:future>" +
            "</cp:coreProperties>";
        XlsxCorePropertiesReader reader = new XlsxCorePropertiesReader();

        XlsxCorePropertiesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Title, Is.EqualTo("Budget"));
        Assert.That(part.Subject, Is.EqualTo("Planning"));
        Assert.That(part.Creator, Is.EqualTo("Ada"));
        Assert.That(part.Keywords, Is.EqualTo("xlsx,tests"));
        Assert.That(part.Description, Is.EqualTo("Quarterly plan"));
        Assert.That(part.LastModifiedBy, Is.EqualTo("Grace"));
        Assert.That(part.Revision, Is.EqualTo("7"));
        Assert.That(part.Category, Is.EqualTo("Finance"));
        Assert.That(part.ContentStatus, Is.EqualTo("Draft"));
        Assert.That(part.HasCreated, Is.True);
        Assert.That(part.Created.Kind, Is.EqualTo(System.DateTimeKind.Utc));
        Assert.That(part.Created.Year, Is.EqualTo(2026));
        Assert.That(part.HasModified, Is.True);
        Assert.That(part.Modified.Day, Is.EqualTo(10));
        Assert.That(part.Language, Is.EqualTo("en-US"));
        Assert.That(part.Identifier, Is.EqualTo("ID-1"));
        Assert.That(part.Version, Is.EqualTo("1.0"));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml[0], Does.Contain("future"));
    }

    [Test]
    public void WriteString_WritesStableCorePropertiesAndCanBeReadBack()
    {
        XlsxCorePropertiesPart part = new XlsxCorePropertiesPart();
        part.Title = "Budget";
        part.Subject = "Planning";
        part.Creator = "Ada";
        part.Keywords = "xlsx,tests";
        part.Description = "Quarterly plan";
        part.LastModifiedBy = "Grace";
        part.Revision = "7";
        part.Category = "Finance";
        part.ContentStatus = "Draft";
        part.Created = new System.DateTime(2026, 8, 9, 12, 34, 56, System.DateTimeKind.Utc);
        part.HasCreated = true;
        part.Modified = new System.DateTime(2026, 8, 10, 1, 2, 3, System.DateTimeKind.Utc);
        part.HasModified = true;
        part.Language = "en-US";
        part.Identifier = "ID-1";
        part.Version = "1.0";
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        part.UnknownXml.Add("<x:future xmlns:x=\"urn:x\"><x:child /></x:future>");
        XlsxCorePropertiesWriter writer = new XlsxCorePropertiesWriter();

        XlsxCorePropertiesWriteResult result = writer.WriteString(part);
        XlsxCorePropertiesPart readPart = new XlsxCorePropertiesReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<cp:coreProperties"));
        Assert.That(result.Xml, Does.Contain("xmlns:dc=\"http://purl.org/dc/elements/1.1/\""));
        Assert.That(result.Xml, Does.Contain("xsi:type=\"dcterms:W3CDTF\""));
        Assert.That(result.Xml, Does.Contain("2026-08-09T12:34:56Z"));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Title, Is.EqualTo("Budget"));
        Assert.That(readPart.Creator, Is.EqualTo("Ada"));
        Assert.That(readPart.HasCreated, Is.True);
        Assert.That(readPart.Created.Kind, Is.EqualTo(System.DateTimeKind.Utc));
        Assert.That(readPart.HasModified, Is.True);
        Assert.That(readPart.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidDates()
    {
        XlsxCorePropertiesReader reader = new XlsxCorePropertiesReader();

        XlsxCorePropertiesPart missingRoot = reader.ReadString("");
        XlsxCorePropertiesPart invalidRoot = reader.ReadString("<bad />");
        XlsxCorePropertiesPart invalidDate = reader.ReadString("<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dcterms=\"http://purl.org/dc/terms/\"><dcterms:created>bad</dcterms:created></cp:coreProperties>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "coreProperties.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "coreProperties.invalidRoot"), Is.True);
        Assert.That(invalidDate.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidDate, "xml.dateTime.invalid"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPart()
    {
        XlsxCorePropertiesWriteResult? result = new XlsxCorePropertiesWriter().WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "coreProperties.partNull"), Is.True);
    }

    static XlsxXmlAttribute CreateAttribute(string namespaceUri, string prefix, string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = namespaceUri;
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxCorePropertiesPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxCorePropertiesWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
