using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class XmpPacketParserTests
{
    [Test]
    public void Parse_CommonNamespacesProduceStableTypedTags()
    {
        string xml = """
            <x:xmpmeta xmlns:x="adobe:ns:meta/">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description xmlns:basic="http://ns.adobe.com/xap/1.0/"
                                 xmlns:photo="http://ns.adobe.com/photoshop/1.0/"
                                 basic:Rating="4">
                  <basic:CreatorTool>Metadata Camera</basic:CreatorTool>
                  <photo:DateCreated>2026-08-12T10:11:12-07:00</photo:DateCreated>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """;
        ImageMetadataDocument document = Parse(xml);

        MetadataTag rating = RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.Rating");
        MetadataTag tool = RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.CreatorTool");
        MetadataTag date = RequiredMetadataQuery.FindFirst(document, "Xmp.photoshop.DateCreated");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Group, Is.EqualTo("xmp"));
        Assert.That(document.Directories[0].Path, Is.EqualTo("Test/XMP/xmp"));
        Assert.That(rating.ValueType, Is.EqualTo(MetadataValueKind.Integer));
        Assert.That(rating.Value.IntegerValue, Is.EqualTo(4));
        Assert.That(tool.Value.Text, Is.EqualTo("Metadata Camera"));
        Assert.That(date.ValueType, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(date.Value.Text, Is.EqualTo("2026-08-12T10:11:12-07:00"));
    }

    [Test]
    public void Parse_MicrosoftPhotoRatingUsesStableNamespaceAndNormalizedScale()
    {
        string xml = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns:MicrosoftPhoto="http://ns.microsoft.com/photo/1.0/">
              <rdf:Description MicrosoftPhoto:Rating="88" />
            </rdf:RDF>
            """;
        ImageMetadataDocument document = Parse(xml);

        MetadataTag rating = RequiredMetadataQuery.FindFirst(document, "Xmp.MicrosoftPhoto.Rating");
        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(rating, Is.Not.Null);
        Assert.That(rating.Value.Kind, Is.EqualTo(MetadataValueKind.Integer));
        Assert.That(summary.Rating, Is.EqualTo(5));
    }

    [Test]
    public void Parse_BagSequenceAndLanguageAlternativeRetainValues()
    {
        string xml = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns:dc="http://purl.org/dc/elements/1.1/">
              <rdf:Description>
                <dc:creator><rdf:Seq><rdf:li>Ada</rdf:li><rdf:li>Grace</rdf:li></rdf:Seq></dc:creator>
                <dc:subject><rdf:Bag><rdf:li>camera</rdf:li><rdf:li>night</rdf:li></rdf:Bag></dc:subject>
                <dc:title><rdf:Alt><rdf:li xml:lang="fr">Bonjour</rdf:li><rdf:li xml:lang="x-default">Hello</rdf:li></rdf:Alt></dc:title>
              </rdf:Description>
            </rdf:RDF>
            """;
        ImageMetadataDocument document = Parse(xml);

        MetadataTag creators = RequiredMetadataQuery.FindFirst(document, "Xmp.dc.creator");
        MetadataTag subjects = RequiredMetadataQuery.FindFirst(document, "Xmp.dc.subject");
        MetadataTag title = RequiredMetadataQuery.FindFirst(document, "Xmp.dc.title");

        Assert.That(creators.ValueType, Is.EqualTo(MetadataValueKind.StringList));
        Assert.That(creators.Value.TextValues, Is.EqualTo(new[] { "Ada", "Grace" }));
        Assert.That(subjects.Value.TextValues, Is.EqualTo(new[] { "camera", "night" }));
        Assert.That(title.Value.Text, Is.EqualTo("Hello"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.title.fr").Value.Text, Is.EqualTo("Bonjour"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.title.x-default").Value.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void Parse_StructuredValueAndStructuredArrayCreateNestedDirectories()
    {
        string xml = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns:xmp="http://ns.adobe.com/xap/1.0/"
                     xmlns:dim="http://ns.example.test/dim/"
                     xmlns:iptcExt="http://iptc.org/std/Iptc4xmpExt/2008-02-29/">
              <rdf:Description>
                <xmp:Dimensions rdf:parseType="Resource"><dim:w>640</dim:w><dim:h>480</dim:h></xmp:Dimensions>
                <iptcExt:LocationCreated><rdf:Bag>
                  <rdf:li rdf:parseType="Resource"><iptcExt:City>Paris</iptcExt:City><iptcExt:CountryName>France</iptcExt:CountryName></rdf:li>
                </rdf:Bag></iptcExt:LocationCreated>
              </rdf:Description>
            </rdf:RDF>
            """;
        ImageMetadataDocument document = Parse(xml);

        MetadataTag dimensions = RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.Dimensions");
        MetadataTag locations = RequiredMetadataQuery.FindFirst(document, "Xmp.iptcExt.LocationCreated");
        MetadataDirectory dimensionsDirectory = dimensions.Value.DirectoryValue ?? throw new InvalidOperationException("Expected dimensions directory was not found.");
        MetadataDirectory locationsDirectory = locations.Value.DirectoryValue ?? throw new InvalidOperationException("Expected locations directory was not found.");

        Assert.That(dimensions.ValueType, Is.EqualTo(MetadataValueKind.DirectoryReference));
        Assert.That(dimensionsDirectory.Tags, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dim.Dimensions.w").Value.Text, Is.EqualTo("640"));
        Assert.That(locations.ValueType, Is.EqualTo(MetadataValueKind.DirectoryReference));
        Assert.That(locationsDirectory.Children, Has.Count.EqualTo(1));
        Assert.That(locationsDirectory.Children[0].RepeatIndex, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.iptcExt.LocationCreated.City").Value.Text, Is.EqualTo("Paris"));
    }

    [Test]
    public void Parse_ResourceValueAndUnknownNamespaceArePreserved()
    {
        string xml = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     xmlns:vendor.data="http://vendor.example/xmp/">
              <rdf:Description vendor.data:Asset="A1">
                <vendor.data:Source rdf:resource="https://example.test/image" />
              </rdf:Description>
            </rdf:RDF>
            """;
        ImageMetadataDocument document = Parse(xml);

        Assert.That(document.Directories[0].Group, Is.EqualTo("vendor_data"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.vendor_data.Asset").Value.Text, Is.EqualTo("A1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.vendor_data.Source").Value.Text, Is.EqualTo("https://example.test/image"));
    }

    [Test]
    public void Parse_ReportsMalformedDtdAndConfiguredLimits()
    {
        string malformed = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description>";
        ImageMetadataDocument malformedDocument = Parse(malformed);
        Assert.That(malformedDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.xml.malformed"));

        string dtd = "<!DOCTYPE rdf:RDF [<!ENTITY x \"expanded\">]><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description /></rdf:RDF>";
        ImageMetadataDocument dtdDocument = Parse(dtd);
        Assert.That(dtdDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.dtd.rejected"));
        Assert.That(dtdDocument.Directories, Is.Empty);

        string tags = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"><rdf:Description xmp:Rating=\"1\"><xmp:CreatorTool>A</xmp:CreatorTool><xmp:Label>B</xmp:Label></rdf:Description></rdf:RDF>";
        ImageMetadataDocument limitedDocument = new ImageMetadataDocument();
        XmpPacketParser.Parse(limitedDocument, tags, 12, tags.Length, "Test/XMP", "XMP", 10000, 2, 8);
        Assert.That(limitedDocument.Directories.Sum(item => item.Tags.Count), Is.EqualTo(2));
        Assert.That(limitedDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.tag.limit"));
    }

    [Test]
    public void Parse_ReportsStructuredNestingLimit()
    {
        string xml = """
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
              <rdf:Description><xmp:Outer rdf:parseType="Resource"><xmp:Inner rdf:parseType="Resource"><xmp:Value>deep</xmp:Value></xmp:Inner></xmp:Outer></rdf:Description>
            </rdf:RDF>
            """;
        ImageMetadataDocument document = new ImageMetadataDocument();
        XmpPacketParser.Parse(document, xml, 12, xml.Length, "Test/XMP", "XMP", 10000, 100, 1);

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.Outer"), Is.Not.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Xmp.xmp.Outer.Inner"), Is.Null);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.nesting.limit"));
    }

    public static ImageMetadataDocument Parse(string xml)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        XmpPacketParser.Parse(document, xml, 12, xml.Length, "Test/XMP", "XMP", 100000, 100, 8);
        return document;
    }
}
