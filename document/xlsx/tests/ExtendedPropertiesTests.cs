using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ExtendedPropertiesTests
{
    [Test]
    public void ReadString_ParsesCommonExtendedPropertiesAndPreservesVectors()
    {
        string xml = "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<Template>Normal</Template>" +
            "<Manager>Grace</Manager>" +
            "<Company>Example Co</Company>" +
            "<Pages>2</Pages>" +
            "<Words>100</Words>" +
            "<Characters>500</Characters>" +
            "<PresentationFormat>On-screen Show</PresentationFormat>" +
            "<Lines>10</Lines>" +
            "<Paragraphs>4</Paragraphs>" +
            "<Slides>3</Slides>" +
            "<Notes>1</Notes>" +
            "<TotalTime>9</TotalTime>" +
            "<HiddenSlides>0</HiddenSlides>" +
            "<MMClips>0</MMClips>" +
            "<ScaleCrop>true</ScaleCrop>" +
            "<HeadingPairs><vt:vector size=\"2\" baseType=\"variant\"><vt:variant><vt:lpstr>Worksheets</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs>" +
            "<TitlesOfParts><vt:vector size=\"1\" baseType=\"lpstr\"><vt:lpstr>Sheet1</vt:lpstr></vt:vector></TitlesOfParts>" +
            "<LinksUpToDate>false</LinksUpToDate>" +
            "<CharactersWithSpaces>600</CharactersWithSpaces>" +
            "<SharedDoc>0</SharedDoc>" +
            "<HyperlinksChanged>1</HyperlinksChanged>" +
            "<Application>Microsoft Excel</Application>" +
            "<AppVersion>16.0300</AppVersion>" +
            "<DocSecurity>0</DocSecurity>" +
            "<x:future><x:child /></x:future>" +
            "</Properties>";
        XlsxExtendedPropertiesReader reader = new XlsxExtendedPropertiesReader();

        XlsxExtendedPropertiesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Template, Is.EqualTo("Normal"));
        Assert.That(part.Manager, Is.EqualTo("Grace"));
        Assert.That(part.Company, Is.EqualTo("Example Co"));
        Assert.That(part.Pages, Is.EqualTo(2));
        Assert.That(part.Words, Is.EqualTo(100));
        Assert.That(part.Characters, Is.EqualTo(500));
        Assert.That(part.PresentationFormat, Is.EqualTo("On-screen Show"));
        Assert.That(part.Lines, Is.EqualTo(10));
        Assert.That(part.Paragraphs, Is.EqualTo(4));
        Assert.That(part.Slides, Is.EqualTo(3));
        Assert.That(part.Notes, Is.EqualTo(1));
        Assert.That(part.TotalTime, Is.EqualTo(9));
        Assert.That(part.ScaleCrop, Is.True);
        Assert.That(part.HasHeadingPairs, Is.True);
        Assert.That(part.HeadingPairsRawXml, Does.Contain("vt:vector"));
        Assert.That(part.HasTitlesOfParts, Is.True);
        Assert.That(part.LinksUpToDate, Is.False);
        Assert.That(part.CharactersWithSpaces, Is.EqualTo(600));
        Assert.That(part.SharedDoc, Is.False);
        Assert.That(part.HyperlinksChanged, Is.True);
        Assert.That(part.Application, Is.EqualTo("Microsoft Excel"));
        Assert.That(part.AppVersion, Is.EqualTo("16.0300"));
        Assert.That(part.HasDocSecurity, Is.True);
        Assert.That(part.DocSecurity, Is.EqualTo(0));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesExtendedPropertiesAndCanBeReadBack()
    {
        XlsxExtendedPropertiesPart part = new XlsxExtendedPropertiesPart();
        part.Template = "Normal";
        part.Manager = "Grace";
        part.Company = "Example Co";
        part.Pages = 2;
        part.HasPages = true;
        part.Words = 100;
        part.HasWords = true;
        part.Characters = 500;
        part.HasCharacters = true;
        part.PresentationFormat = "On-screen Show";
        part.Lines = 10;
        part.HasLines = true;
        part.Paragraphs = 4;
        part.HasParagraphs = true;
        part.Slides = 3;
        part.HasSlides = true;
        part.Notes = 1;
        part.HasNotes = true;
        part.TotalTime = 9;
        part.HasTotalTime = true;
        part.HiddenSlides = 0;
        part.HasHiddenSlides = true;
        part.MMClips = 0;
        part.HasMMClips = true;
        part.ScaleCrop = true;
        part.HasScaleCrop = true;
        part.HeadingPairsRawXml = "<HeadingPairs xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><vt:vector size=\"2\" baseType=\"variant\"><vt:variant><vt:lpstr>Worksheets</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs>";
        part.HasHeadingPairs = true;
        part.TitlesOfPartsRawXml = "<TitlesOfParts xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><vt:vector size=\"1\" baseType=\"lpstr\"><vt:lpstr>Sheet1</vt:lpstr></vt:vector></TitlesOfParts>";
        part.HasTitlesOfParts = true;
        part.LinksUpToDate = false;
        part.HasLinksUpToDate = true;
        part.CharactersWithSpaces = 600;
        part.HasCharactersWithSpaces = true;
        part.SharedDoc = false;
        part.HasSharedDoc = true;
        part.HyperlinksChanged = true;
        part.HasHyperlinksChanged = true;
        part.Application = "Microsoft Excel";
        part.AppVersion = "16.0300";
        part.DocSecurity = 0;
        part.HasDocSecurity = true;
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        part.UnknownXml.Add("<x:future xmlns:x=\"urn:x\"><x:child /></x:future>");

        XlsxExtendedPropertiesWriteResult result = new XlsxExtendedPropertiesWriter().WriteString(part);
        XlsxExtendedPropertiesPart readPart = new XlsxExtendedPropertiesReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<Properties"));
        Assert.That(result.Xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\""));
        Assert.That(result.Xml, Does.Contain("xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\""));
        Assert.That(result.Xml, Does.Contain("<Application>Microsoft Excel</Application>"));
        Assert.That(result.Xml, Does.Contain("vt:vector"));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Company, Is.EqualTo("Example Co"));
        Assert.That(readPart.HasPages, Is.True);
        Assert.That(readPart.Pages, Is.EqualTo(2));
        Assert.That(readPart.HasScaleCrop, Is.True);
        Assert.That(readPart.ScaleCrop, Is.True);
        Assert.That(readPart.HasHeadingPairs, Is.True);
        Assert.That(readPart.HasTitlesOfParts, Is.True);
        Assert.That(readPart.HyperlinksChanged, Is.True);
        Assert.That(readPart.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        XlsxExtendedPropertiesReader reader = new XlsxExtendedPropertiesReader();

        XlsxExtendedPropertiesPart missingRoot = reader.ReadString("");
        XlsxExtendedPropertiesPart invalidRoot = reader.ReadString("<Bad />");
        XlsxExtendedPropertiesPart invalidValues = reader.ReadString("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Pages>bad</Pages><ScaleCrop>maybe</ScaleCrop><DocSecurity>bad</DocSecurity></Properties>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "extendedProperties.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "extendedProperties.invalidRoot"), Is.True);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.integer.invalid"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPart()
    {
        XlsxExtendedPropertiesWriteResult? result = new XlsxExtendedPropertiesWriter().WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "extendedProperties.partNull"), Is.True);
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

    static bool HasDiagnostic(XlsxExtendedPropertiesPart part, string code)
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

    static bool HasDiagnostic(XlsxExtendedPropertiesWriteResult result, string code)
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
