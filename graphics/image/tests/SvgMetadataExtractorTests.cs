using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class SvgMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsTitleDescriptionDimensionsAndViewBox()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"2in\" height=\"2.54cm\" viewBox=\"-1, -2 200 100\" preserveAspectRatio=\"xMidYMid meet\"><title>Sample icon</title><desc>Accessible description</desc></svg>";

        ImageMetadataDocument document = Parse(xml);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(StringValue(document, "Svg.Document.Width"), Is.EqualTo("2in"));
        Assert.That(StringValue(document, "Svg.Document.Height"), Is.EqualTo("2.54cm"));
        Assert.That(StringValue(document, "Svg.Document.ViewBox"), Is.EqualTo("-1, -2 200 100"));
        Assert.That(DoubleValue(document, "Svg.Document.ViewBoxMinX"), Is.EqualTo(-1));
        Assert.That(DoubleValue(document, "Svg.Document.ViewBoxMinY"), Is.EqualTo(-2));
        Assert.That(DoubleValue(document, "Svg.Document.ViewBoxWidth"), Is.EqualTo(200));
        Assert.That(DoubleValue(document, "Svg.Document.ViewBoxHeight"), Is.EqualTo(100));
        Assert.That(StringValue(document, "Svg.Document.PreserveAspectRatio"), Is.EqualTo("xMidYMid meet"));
        Assert.That(StringValue(document, "Svg.Title"), Is.EqualTo("Sample icon"));
        Assert.That(StringValue(document, "Svg.Description"), Is.EqualTo("Accessible description"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(192));
        Assert.That(document.Frames[0].Height, Is.EqualTo(96));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Summary.Width, Is.EqualTo(192));
        Assert.That(document.Summary.Height, Is.EqualTo(96));
        Assert.That(document.Summary.Title, Is.EqualTo("Sample icon"));
        Assert.That(document.Summary.Description, Is.EqualTo("Accessible description"));
    }

    [Test]
    public void Parse_ViewBoxProvidesDimensionsWhenLengthsAreAbsent()
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300.4 199.6\"/>");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(300));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
    }

    [Test]
    public void Parse_OneExplicitDimensionUsesViewBoxAspectRatio()
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400px\" viewBox=\"0 0 200 100\"/>");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Width, Is.EqualTo(400));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
    }

    [TestCase("1in", 96)]
    [TestCase("2.54cm", 96)]
    [TestCase("25.4mm", 96)]
    [TestCase("101.6q", 96)]
    [TestCase("72pt", 96)]
    [TestCase("6pc", 96)]
    [TestCase("9.6e1px", 96)]
    public void Parse_ConvertsAbsoluteCssLengthsToPixels(string length, int expected)
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + length + "\" height=\"1px\"/>");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Width, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_RelativeDimensionsArePreservedAndViewBoxProvidesFrame()
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100%\" height=\"auto\" viewBox=\"0 0 80 60\"/>");

        Assert.That(StringValue(document, "Svg.Document.Width"), Is.EqualTo("100%"));
        Assert.That(StringValue(document, "Svg.Document.Height"), Is.EqualTo("auto"));
        AssertDiagnostic(document, "svg.width.unresolved", MetadataSeverity.Warning);
        AssertDiagnostic(document, "svg.height.unresolved", MetadataSeverity.Warning);
        Assert.That(document.Frames[0].Width, Is.EqualTo(80));
        Assert.That(document.Frames[0].Height, Is.EqualTo(60));
    }

    [Test]
    public void Parse_InvalidViewBoxIsPreservedAndReported()
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 nope 20\"/>");

        Assert.That(StringValue(document, "Svg.Document.ViewBox"), Is.EqualTo("0 0 nope 20"));
        Assert.That(document.Frames, Is.Empty);
        AssertDiagnostic(document, "svg.viewbox.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_ZeroViewBoxDoesNotCreateFrame()
    {
        ImageMetadataDocument document = Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 0 20\"/>");

        Assert.That(document.Frames, Is.Empty);
        AssertDiagnostic(document, "svg.viewbox.size.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_CollectsModernAndLegacyImageLinksWithoutLoadingThem()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 10 10\"><image href=\"images/photo.png\"/><g><image xlink:href=\"https://example.invalid/remote.jpg\"/><image href=\"data:image/png;base64,AA==\"/></g></svg>";

        ImageMetadataDocument document = Parse(xml);

        MetadataTag links = RequiredMetadataQuery.FindFirst(document, "Svg.ImageLinks");
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(links.Value.Kind, Is.EqualTo(MetadataValueKind.StringList));
        Assert.That(links.Value.TextValues, Is.EqualTo(new string[] { "images/photo.png", "https://example.invalid/remote.jpg", "data:image/png;base64,AA==" }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.ImageLinkCount").Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Parse_DoesNotTreatForeignNamespaceImageElementsAsSvgLinks()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:m=\"urn:metadata\" viewBox=\"0 0 10 10\"><m:image href=\"not-an-svg-image.png\"/><image href=\"actual.png\"/></svg>";

        ImageMetadataDocument document = Parse(xml);

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.ImageLinks").Value.TextValues, Is.EqualTo(new string[] { "actual.png" }));
    }

    [Test]
    public void Parse_PreservesGenericMetadataAsXml()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata><custom xmlns=\"urn:test\"><value>42</value></custom></metadata></svg>";

        ImageMetadataDocument document = Parse(xml);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[1].Family, Is.EqualTo(MetadataFamily.Svg));
        Assert.That(document.Directories[1].Group, Is.EqualTo("Metadata"));
        Assert.That(document.Directories[1].Path, Is.EqualTo("SVG/Metadata/0"));
        MetadataTag metadata = RequiredMetadataQuery.FindFirst(document, "Svg.Metadata.Xml");
        Assert.That(metadata.Value.Kind, Is.EqualTo(MetadataValueKind.XmlText));
        Assert.That(metadata.Value.Text, Does.Contain("<custom"));
        Assert.That(metadata.Value.Text, Does.Contain("<value>42</value>"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Metadata.IsXmp").Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_PreservesRdfMetadataAsXmpProfile()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description/></rdf:RDF></metadata></svg>";

        ImageMetadataDocument document = Parse(xml);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Metadata.IsXmp").Value.BooleanValue, Is.True);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.SVG.Metadata"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/rdf+xml"));
        Assert.That(document.Profiles[0].Text, Does.Contain("rdf:RDF"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(Encoding.UTF8.GetBytes(document.Profiles[0].Text)));
        Assert.That(document.Summary.HasColorProfile, Is.False);
    }

    [Test]
    public void Parse_ParseXmpFalseKeepsMetadataWithoutProfile()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata><x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/></metadata></svg>";
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;

        ImageMetadataDocument document = SvgMetadataParserTests.Parse(Encoding.UTF8.GetBytes(xml), ImageFormatKind.Svg, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Metadata.IsXmp").Value.BooleanValue, Is.True);
    }

    [Test]
    public void Parse_XmpProfileLimitKeepsProfileMetadataWithoutPayload()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description/></rdf:RDF></metadata></svg>";
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 10;

        ImageMetadataDocument document = SvgMetadataParserTests.Parse(Encoding.UTF8.GetBytes(xml), ImageFormatKind.Svg, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.GreaterThan(10));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Profiles[0].Text, Is.Empty);
        AssertDiagnostic(document, "svg.xmp.profile.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_OnlyDirectRootTitleAndDescriptionBecomeDocumentText()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g><title>Nested title</title><desc>Nested description</desc></g><title>Document title</title><desc>Document description</desc></svg>";

        ImageMetadataDocument document = Parse(xml);

        Assert.That(StringValue(document, "Svg.Title"), Is.EqualTo("Document title"));
        Assert.That(StringValue(document, "Svg.Description"), Is.EqualTo("Document description"));
    }

    [Test]
    public void Parse_FrameLimitPreservesFieldsWithoutAddingFrame()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\"/>";
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 0;

        ImageMetadataDocument document = SvgMetadataParserTests.Parse(Encoding.UTF8.GetBytes(xml), ImageFormatKind.Svg, options);

        Assert.That(document.Frames, Is.Empty);
        Assert.That(StringValue(document, "Svg.Document.Width"), Is.EqualTo("20"));
        AssertDiagnostic(document, "svg.frame.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_SvgzReceivesTheSameSemanticExtraction()
    {
        string xml = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40\" height=\"30\"><title>Compressed</title></svg>";
        byte[] gzip = SvgMetadataParserTests.Compress(Encoding.UTF8.GetBytes(xml));

        ImageMetadataDocument document = SvgMetadataParserTests.Parse(gzip, ImageFormatKind.Svgz, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Width, Is.EqualTo(40));
        Assert.That(document.Frames[0].Height, Is.EqualTo(30));
        Assert.That(document.Summary.Title, Is.EqualTo("Compressed"));
    }

    public static ImageMetadataDocument Parse(string xml)
    {
        return SvgMetadataParserTests.Parse(Encoding.UTF8.GetBytes(xml), ImageFormatKind.Svg, new ImageMetadataReaderOptions());
    }

    public static string StringValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.Text;
    }

    public static double DoubleValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.DoubleValue;
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        SvgMetadataParserTests.AssertDiagnostic(document, code, severity);
    }
}
