using System.Collections.Generic;
using System.Text;
using System.Xml;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MetadataXmlReaderTests
{
    [Test]
    public void CreateSafeSettings_DisablesDtdProcessingAndExternalResolver()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        XmlReaderSettings settings = MetadataXmlReader.CreateSafeSettings(100, diagnostics, "XMP");

        Assert.That(settings.DtdProcessing, Is.EqualTo(DtdProcessing.Ignore));
        Assert.That(settings.MaxCharactersFromEntities, Is.EqualTo(0));
        Assert.That(settings.MaxCharactersInDocument, Is.EqualTo(100));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ReadRootName_ReturnsFirstElementName()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        string xml = "<?xml version=\"1.0\"?><xmpmeta><title>Sample</title></xmpmeta>";

        string root = MetadataXmlReader.ReadRootName(xml, 1000, diagnostics, "XMP");

        Assert.That(root, Is.EqualTo("xmpmeta"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ReadElementTextValues_ReturnsMatchingElementTextInOrder()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        string xml = "<root><title>One</title><ignore>Two</ignore><title>Three</title></root>";

        List<string> values = MetadataXmlReader.ReadElementTextValues(xml, "title", 1000, diagnostics, "XMP");

        Assert.That(values, Has.Count.EqualTo(2));
        Assert.That(values[0], Is.EqualTo("One"));
        Assert.That(values[1], Is.EqualTo("Three"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void DtdPresence_IsIgnoredAndReported()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        string xml = "<!DOCTYPE root SYSTEM \"file:///tmp/missing.dtd\"><root><title>Safe</title></root>";

        string root = MetadataXmlReader.ReadRootName(xml, 1000, diagnostics, "XMP");

        Assert.That(root, Is.EqualTo("root"));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
        Assert.That(diagnostics[0].Code, Is.EqualTo("xml.dtd.ignored"));
    }

    [Test]
    public void DecodeXmlBytes_HandlesUtf8BomAndUtf16Bom()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] utf8 = new byte[] { 239, 187, 191, 60, 114, 111, 111, 116, 47, 62 };
        byte[] utf16 = new byte[] { 255, 254, 60, 0, 114, 0, 111, 0, 111, 0, 116, 0, 47, 0, 62, 0 };

        string utf8Text = MetadataXmlReader.DecodeXmlBytes(utf8, 100, diagnostics, "XMP");
        string utf16Text = MetadataXmlReader.DecodeXmlBytes(utf16, 100, diagnostics, "XMP");

        Assert.That(utf8Text, Is.EqualTo("<root/>"));
        Assert.That(utf16Text, Is.EqualTo("<root/>"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ReadRootNameFromBytes_DecodesAndReadsRoot()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.UTF8.GetBytes("<root><child>value</child></root>");

        string root = MetadataXmlReader.ReadRootNameFromBytes(bytes, 100, 100, diagnostics, "XMP");

        Assert.That(root, Is.EqualTo("root"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void LimitsAndInvalidArguments_ReturnEmptyWithDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.UTF8.GetBytes("<root/>");

        string byteLimited = MetadataXmlReader.ReadRootNameFromBytes(bytes, 3, 100, diagnostics, "XMP");
        string charLimited = MetadataXmlReader.ReadRootName("<root/>", 3, diagnostics, "XMP");
        string nullText = MetadataXmlReader.ReadRootName(null, 100, diagnostics, "XMP");
        List<string> noElementName = MetadataXmlReader.ReadElementTextValues("<root/>", "", 100, diagnostics, "XMP");

        Assert.That(byteLimited, Is.EqualTo(""));
        Assert.That(charLimited, Is.EqualTo(""));
        Assert.That(nullText, Is.EqualTo(""));
        Assert.That(noElementName, Is.Empty);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.LimitExceeded), Is.EqualTo(2));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(3));
    }

    [Test]
    public void ContainsDtd_IsCaseInsensitive()
    {
        Assert.That(MetadataXmlReader.ContainsDtd("<!doctype root><root/>"), Is.True);
        Assert.That(MetadataXmlReader.ContainsDtd("<root/>"), Is.False);
        Assert.That(MetadataXmlReader.ContainsDtd(null), Is.False);
    }
}
