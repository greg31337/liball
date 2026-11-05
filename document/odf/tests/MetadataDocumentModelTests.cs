using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class MetadataDocumentModelTests
{
    [Test]
    public void ReadsStandardMetadataAndTypedUserDefinedValues()
    {
        OdfDocument document = ReadFlat(CompleteMetadataXml());
        OdfDocumentMetadata metadata = document.Metadata;

        Assert.That(metadata.Title, Is.EqualTo("Release plan"));
        Assert.That(metadata.Description, Is.EqualTo("Quarterly plan"));
        Assert.That(metadata.Subject, Is.EqualTo("Planning"));
        Assert.That(metadata.Creator, Is.EqualTo("Recent Author"));
        Assert.That(metadata.Date, Is.EqualTo("2026-09-01T12:30:00Z"));
        Assert.That(metadata.IsDateValid, Is.True);
        Assert.That(metadata.Language, Is.EqualTo("en-US"));
        Assert.That(metadata.Generator, Is.EqualTo("odf test"));
        Assert.That(metadata.InitialCreator, Is.EqualTo("Original Author"));
        Assert.That(metadata.CreationDate, Is.EqualTo("2026-08-01"));
        Assert.That(metadata.IsCreationDateValid, Is.True);
        Assert.That(metadata.PrintedBy, Is.EqualTo("Printer User"));
        Assert.That(metadata.PrintDate, Is.EqualTo("2026-08-31T09:00:00"));
        Assert.That(metadata.IsPrintDateValid, Is.True);
        Assert.That(metadata.EditingDuration, Is.EqualTo("PT2H15M"));
        Assert.That(metadata.IsEditingDurationValid, Is.True);
        Assert.That(metadata.EditingCycles, Is.EqualTo(7));
        Assert.That(metadata.IsEditingCyclesValid, Is.True);
        Assert.That(metadata.Keywords, Is.EqualTo(new[] { "odf", "metadata" }));

        Assert.That(metadata.Template.IsPresent, Is.True);
        Assert.That(metadata.Template.Href, Is.EqualTo("Templates/base.ott"));
        Assert.That(metadata.Template.XLinkType, Is.EqualTo("simple"));
        Assert.That(metadata.Template.Actuate, Is.EqualTo("onRequest"));
        Assert.That(metadata.Template.Title, Is.EqualTo("Base template"));
        Assert.That(metadata.Template.Date, Is.EqualTo("2026-07-01"));
        Assert.That(metadata.Template.IsDateValid, Is.True);
        Assert.That(metadata.AutoReload.IsPresent, Is.True);
        Assert.That(metadata.AutoReload.Href, Is.EqualTo("https://example.test/current.odt"));
        Assert.That(metadata.AutoReload.Show, Is.EqualTo("replace"));
        Assert.That(metadata.AutoReload.Actuate, Is.EqualTo("onLoad"));
        Assert.That(metadata.AutoReload.Delay, Is.EqualTo("PT30S"));
        Assert.That(metadata.AutoReload.IsDelayValid, Is.True);
        Assert.That(metadata.HyperlinkBehavior.IsPresent, Is.True);
        Assert.That(metadata.HyperlinkBehavior.TargetFrameName, Is.EqualTo("_blank"));
        Assert.That(metadata.HyperlinkBehavior.Show, Is.EqualTo("new"));

        Assert.That(metadata.UserDefined, Has.Length.EqualTo(6));
        Assert.That(metadata.UserDefined[0].Kind, Is.EqualTo(OdfValueKind.String));
        Assert.That(metadata.UserDefined[0].StringValue, Is.EqualTo("alpha"));
        Assert.That(metadata.UserDefined[1].Kind, Is.EqualTo(OdfValueKind.Float));
        Assert.That(metadata.UserDefined[1].NumericValue, Is.EqualTo(12.5));
        Assert.That(metadata.UserDefined[2].Kind, Is.EqualTo(OdfValueKind.DateTime));
        Assert.That(metadata.UserDefined[2].DateValue, Is.EqualTo("2026-09-02T10:00:00Z"));
        Assert.That(metadata.UserDefined[3].Kind, Is.EqualTo(OdfValueKind.Duration));
        Assert.That(metadata.UserDefined[3].DurationValue, Is.EqualTo("PT45M"));
        Assert.That(metadata.UserDefined[4].Kind, Is.EqualTo(OdfValueKind.Boolean));
        Assert.That(metadata.UserDefined[4].BooleanValue, Is.True);
        Assert.That(metadata.UserDefined[5].Kind, Is.EqualTo(OdfValueKind.Currency));
        Assert.That(metadata.UserDefined[5].NumericValue, Is.EqualTo(19.95));
        Assert.That(metadata.UserDefined[5].Currency, Is.EqualTo("USD"));
        Assert.That(metadata.UserDefined[5].IsValid, Is.True);

        Assert.That(metadata.Statistics.IsPresent, Is.True);
        Assert.That(metadata.Statistics.PageCount, Is.EqualTo(2));
        Assert.That(metadata.Statistics.TableCount, Is.EqualTo(3));
        Assert.That(metadata.Statistics.DrawCount, Is.EqualTo(4));
        Assert.That(metadata.Statistics.ImageCount, Is.EqualTo(5));
        Assert.That(metadata.Statistics.ObjectCount, Is.EqualTo(6));
        Assert.That(metadata.Statistics.OleObjectCount, Is.EqualTo(7));
        Assert.That(metadata.Statistics.ParagraphCount, Is.EqualTo(8));
        Assert.That(metadata.Statistics.WordCount, Is.EqualTo(9));
        Assert.That(metadata.Statistics.CharacterCount, Is.EqualTo(10));
        Assert.That(metadata.Statistics.NonWhitespaceCharacterCount, Is.EqualTo(11));
        Assert.That(metadata.Statistics.RowCount, Is.EqualTo(12));
        Assert.That(metadata.Statistics.CellCount, Is.EqualTo(16));
        Assert.That(metadata.Statistics.FrameCount, Is.EqualTo(13));
        Assert.That(metadata.Statistics.SentenceCount, Is.EqualTo(14));
        Assert.That(metadata.Statistics.SyllableCount, Is.EqualTo(15));
    }

    [Test]
    public void WritesMetadataChangesAndPreservesUnknownMetadataContent()
    {
        OdfDocument document = ReadFlat(CompleteMetadataXml());
        document.Metadata.Title = "Updated title";
        document.Metadata.Keywords = new[] { "one" };
        document.Metadata.Template.IsPresent = false;
        document.Metadata.AutoReload.Delay = "PT5M";
        document.Metadata.HyperlinkBehavior.TargetFrameName = "report";
        document.Metadata.UserDefined[0].StringValue = "changed";
        document.Metadata.UserDefined[1].NumericValue = 42.25;
        document.Metadata.Statistics.WordCount = 1234;

        OdfDocumentWriter writer = new OdfDocumentWriter();
        byte[] bytes = writer.WriteDocumentBytes(document, new OdfSaveOptions());
        string xml = Encoding.UTF8.GetString(bytes);
        OdfDocument roundTrip = new OdfDocumentReader().ReadDocumentBytes(
            bytes, "roundtrip.fodt", new OdfLoadOptions());

        Assert.That(roundTrip.Metadata.Title, Is.EqualTo("Updated title"));
        Assert.That(roundTrip.Metadata.Keywords, Is.EqualTo(new[] { "one" }));
        Assert.That(roundTrip.Metadata.Template.IsPresent, Is.False);
        Assert.That(roundTrip.Metadata.AutoReload.Delay, Is.EqualTo("PT5M"));
        Assert.That(roundTrip.Metadata.HyperlinkBehavior.TargetFrameName,
            Is.EqualTo("report"));
        Assert.That(roundTrip.Metadata.UserDefined[0].StringValue,
            Is.EqualTo("changed"));
        Assert.That(roundTrip.Metadata.UserDefined[1].NumericValue,
            Is.EqualTo(42.25));
        Assert.That(roundTrip.Metadata.Statistics.WordCount, Is.EqualTo(1234));
        Assert.That(xml, Does.Contain("acme:keep"));
        Assert.That(xml, Does.Contain("vendor data"));
    }

    [Test]
    public void CreatesMetadataPartForPackageDocument()
    {
        OdfDocument document = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        document.Metadata.Title = "New package";
        document.Metadata.Generator = "odf SDK";
        document.Metadata.Keywords = new[] { "new", "package" };
        document.Metadata.UserDefined = new[]
        {
            new OdfUserDefinedMetadata
            {
                Name = "Approved",
                Kind = OdfValueKind.Boolean,
                BooleanValue = true
            }
        };
        document.Metadata.Statistics.IsPresent = true;
        document.Metadata.Statistics.PageCount = 1;

        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        OdfDocument roundTrip = new OdfDocumentReader().ReadDocumentBytes(
            bytes, "created.odt", new OdfLoadOptions());

        Assert.That(roundTrip.Metadata.Title, Is.EqualTo("New package"));
        Assert.That(roundTrip.Metadata.Generator, Is.EqualTo("odf SDK"));
        Assert.That(roundTrip.Metadata.Keywords,
            Is.EqualTo(new[] { "new", "package" }));
        Assert.That(roundTrip.Metadata.UserDefined, Has.Length.EqualTo(1));
        Assert.That(roundTrip.Metadata.UserDefined[0].Kind,
            Is.EqualTo(OdfValueKind.Boolean));
        Assert.That(roundTrip.Metadata.UserDefined[0].BooleanValue, Is.True);
        Assert.That(roundTrip.Metadata.Statistics.PageCount, Is.EqualTo(1));
        Assert.That(roundTrip.Parts, Has.Some.Matches<OdfPart>(
            part => part.Path == OdfPartNames.Metadata));
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "metadata.fodt",
            new OdfLoadOptions());
    }

    static string CompleteMetadataXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<office:document xmlns:office=\"" + OdfNamespaces.Office
            + "\" xmlns:meta=\"" + OdfNamespaces.Metadata
            + "\" xmlns:dc=\"" + OdfNamespaces.DublinCore
            + "\" xmlns:xlink=\"" + OdfNamespaces.XLink
            + "\" xmlns:acme=\"urn:example:metadata\" office:version=\"1.4\" "
            + "office:mimetype=\"" + OdfMediaTypes.Text + "\">"
            + "<office:meta>"
            + "<meta:generator>odf test</meta:generator>"
            + "<dc:title>Release plan</dc:title>"
            + "<dc:description>Quarterly plan</dc:description>"
            + "<dc:subject>Planning</dc:subject>"
            + "<dc:creator>Recent Author</dc:creator>"
            + "<dc:date>2026-09-01T12:30:00Z</dc:date>"
            + "<dc:language>en-US</dc:language>"
            + "<meta:initial-creator>Original Author</meta:initial-creator>"
            + "<meta:creation-date>2026-08-01</meta:creation-date>"
            + "<meta:printed-by>Printer User</meta:printed-by>"
            + "<meta:print-date>2026-08-31T09:00:00</meta:print-date>"
            + "<meta:editing-duration>PT2H15M</meta:editing-duration>"
            + "<meta:editing-cycles>7</meta:editing-cycles>"
            + "<meta:keyword>odf</meta:keyword><meta:keyword>metadata</meta:keyword>"
            + "<meta:template xlink:type=\"simple\" xlink:href=\"Templates/base.ott\" "
            + "xlink:actuate=\"onRequest\" xlink:title=\"Base template\" "
            + "meta:date=\"2026-07-01\"/>"
            + "<meta:auto-reload xlink:type=\"simple\" "
            + "xlink:href=\"https://example.test/current.odt\" "
            + "xlink:show=\"replace\" xlink:actuate=\"onLoad\" meta:delay=\"PT30S\"/>"
            + "<meta:hyperlink-behaviour office:target-frame-name=\"_blank\" "
            + "xlink:show=\"new\"/>"
            + "<meta:user-defined meta:name=\"Label\" meta:value-type=\"string\">alpha</meta:user-defined>"
            + "<meta:user-defined meta:name=\"Score\" meta:value-type=\"float\">12.5</meta:user-defined>"
            + "<meta:user-defined meta:name=\"Due\" meta:value-type=\"date\">2026-09-02T10:00:00Z</meta:user-defined>"
            + "<meta:user-defined meta:name=\"Elapsed\" meta:value-type=\"time\">PT45M</meta:user-defined>"
            + "<meta:user-defined meta:name=\"Ready\" meta:value-type=\"boolean\">true</meta:user-defined>"
            + "<meta:user-defined meta:name=\"Cost\" meta:value-type=\"currency\" meta:currency=\"USD\">19.95</meta:user-defined>"
            + "<meta:document-statistic meta:page-count=\"2\" meta:table-count=\"3\" "
            + "meta:draw-count=\"4\" meta:image-count=\"5\" meta:object-count=\"6\" "
            + "meta:ole-object-count=\"7\" meta:paragraph-count=\"8\" meta:word-count=\"9\" "
            + "meta:character-count=\"10\" meta:non-whitespace-character-count=\"11\" "
            + "meta:row-count=\"12\" meta:frame-count=\"13\" meta:sentence-count=\"14\" "
            + "meta:syllable-count=\"15\" meta:cell-count=\"16\"/>"
            + "<acme:keep acme:flag=\"yes\">vendor data</acme:keep>"
            + "</office:meta><office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
