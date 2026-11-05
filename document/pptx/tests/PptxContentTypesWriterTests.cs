using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxContentTypesWriterTests
{
    [Test]
    public void WriteString_EmitsDeterministicDefaultAndOverrideOrder()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", PptxContentTypes.Xml);
        manifest.AddDefault("rels", PptxContentTypes.Relationships);
        manifest.AddOverride("/ppt/slides/slide1.xml", PptxContentTypes.Slide);
        manifest.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);

        string xml = PptxContentTypesWriter.WriteString(manifest);

        Assert.That(xml.IndexOf("Extension=\"rels\""), Is.LessThan(xml.IndexOf("Extension=\"xml\"")));
        Assert.That(xml.IndexOf("PartName=\"/ppt/presentation.xml\""), Is.LessThan(xml.IndexOf("PartName=\"/ppt/slides/slide1.xml\"")));
        Assert.That(xml, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
        Assert.That(xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\""));
    }

    [Test]
    public void WriteBytes_RoundTripsThroughParser()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("rels", PptxContentTypes.Relationships);
        manifest.AddDefault("png", PptxContentTypes.Png);
        manifest.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);
        manifest.AddOverride("/ppt/slides/slide1.xml", PptxContentTypes.Slide);

        byte[] bytes = PptxContentTypesWriter.WriteBytes(manifest);
        PptxContentTypeManifest parsed = PptxContentTypesParser.ParseBytes(bytes);

        Assert.That(parsed.Succeeded(), Is.True);
        Assert.That(parsed.DefaultCount(), Is.EqualTo(2));
        Assert.That(parsed.OverrideCount(), Is.EqualTo(2));
        Assert.That(parsed.GetContentTypeForPartName("/ppt/media/image1.png"), Is.EqualTo(PptxContentTypes.Png));
        Assert.That(parsed.GetContentTypeForPartName("/ppt/slides/slide1.xml"), Is.EqualTo(PptxContentTypes.Slide));
    }

    [Test]
    public void WriteString_ProducesSameXmlForDifferentInsertionOrders()
    {
        PptxContentTypeManifest first = new PptxContentTypeManifest();
        first.AddDefault("xml", PptxContentTypes.Xml);
        first.AddDefault("rels", PptxContentTypes.Relationships);
        first.AddOverride("/ppt/slides/slide1.xml", PptxContentTypes.Slide);
        first.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);

        PptxContentTypeManifest second = new PptxContentTypeManifest();
        second.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);
        second.AddOverride("/ppt/slides/slide1.xml", PptxContentTypes.Slide);
        second.AddDefault("rels", PptxContentTypes.Relationships);
        second.AddDefault("XML", PptxContentTypes.Xml);

        Assert.That(PptxContentTypesWriter.WriteString(first), Is.EqualTo(PptxContentTypesWriter.WriteString(second)));
    }

    [Test]
    public void WriteString_EscapesXmlAttributeValues()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", "application/vnd.example+xml");
        manifest.AddOverride("/ppt/custom/item.xml", "application/vnd.example.custom+xml;name=a&b");

        string xml = PptxContentTypesWriter.WriteString(manifest);

        Assert.That(xml, Does.Contain("ContentType=\"application/vnd.example.custom+xml;name=a&amp;b\""));
        Assert.That(PptxContentTypesParser.ParseString(xml).Succeeded(), Is.True);
    }

    [Test]
    public void ValidateManifest_ReportsInvalidDeclarations()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", PptxContentTypes.Xml);
        manifest.AddDefault("XML", PptxContentTypes.Xml);
        manifest.AddDefault(".bad", PptxContentTypes.Xml);
        manifest.AddOverride("/ppt/../presentation.xml", PptxContentTypes.Presentation);
        manifest.AddOverride("/ppt/slides/slide1.xml", "");

        PptxResult result = PptxContentTypesWriter.ValidateManifest(manifest);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("CT020"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("CT011"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("URI010"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("CT030"));
    }

    [Test]
    public void WriteString_ThrowsForInvalidManifest()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", PptxContentTypes.Xml);
        manifest.AddDefault("XML", PptxContentTypes.Xml);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PptxContentTypesWriter.WriteString(manifest))!;

        Assert.That(exception.Message, Does.Contain("CT020"));
    }

    [Test]
    public void WriteBytes_UsesUtf8WithoutBom()
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", PptxContentTypes.Xml);

        byte[] bytes = PptxContentTypesWriter.WriteBytes(manifest);

        Assert.That(bytes[0], Is.EqualTo((byte)'<'));
        Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("encoding=\"utf-8\""));
    }
}
