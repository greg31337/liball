using System.Text;

namespace odf.Tests;

public class MasterTextDocumentModelTests
{
    [Test]
    public void MaterializesEveryHeaderFooterVariantThroughSharedTextModels()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles><style:master-page style:name='Standard'>"
            + "<style:header><text:p>Header <text:page-number>1</text:page-number></text:p></style:header>"
            + "<style:header-left><text:p>Left header</text:p></style:header-left>"
            + "<style:header-first><text:p>First header</text:p></style:header-first>"
            + "<style:footer><text:list><text:list-item><text:p>Footer list</text:p></text:list-item>"
            + "</text:list></style:footer>"
            + "<style:footer-left><text:p>Left footer</text:p></style:footer-left>"
            + "<style:footer-first><text:p>First footer</text:p></style:footer-first>"
            + "</style:master-page></office:master-styles>",
            "<text:p>Body</text:p>");
        OdfMasterPage master = document.MasterPages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(master.TextContents, Has.Length.EqualTo(6));
            Assert.That(Content(master, OdfMasterPageContentKind.Header)
                .TextBlocks.Single().Text, Is.EqualTo("Header 1"));
            Assert.That(Content(master, OdfMasterPageContentKind.HeaderLeft)
                .TextBlocks.Single().Text, Is.EqualTo("Left header"));
            Assert.That(Content(master, OdfMasterPageContentKind.HeaderFirst)
                .TextBlocks.Single().Text, Is.EqualTo("First header"));
            Assert.That(Content(master, OdfMasterPageContentKind.Footer)
                .TextBlocks.Single().Text, Is.EqualTo("Footer list"));
            Assert.That(Content(master, OdfMasterPageContentKind.FooterLeft)
                .TextBlocks.Single().Text, Is.EqualTo("Left footer"));
            Assert.That(Content(master, OdfMasterPageContentKind.FooterFirst)
                .TextBlocks.Single().Text, Is.EqualTo("First footer"));
            Assert.That(document.TextFields.Any(
                field => field.ElementName == "page-number"
                    && field.TextBlock?.Text == "Header 1"), Is.True);
            Assert.That(document.TextLists, Has.Length.EqualTo(1));
            Assert.That(document.TextBlocks.Any(block => block.Text == "Body"), Is.True);
        });
    }

    [Test]
    public void WritesHeaderFooterBlockAndFieldChanges()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles><style:master-page style:name='Standard'>"
            + "<style:header><text:p>Old header</text:p></style:header>"
            + "<style:footer><text:p>Page <text:page-number>1</text:page-number></text:p>"
            + "</style:footer></style:master-page></office:master-styles>",
            "<text:p>Body</text:p>");
        OdfMasterPage master = document.MasterPages.Single();
        Content(master, OdfMasterPageContentKind.Header)
            .TextBlocks[0].Text = "New header";
        OdfTextField pageNumber = document.TextFields.Single(
            field => field.ElementName == "page-number");
        pageNumber.DisplayText = "42";

        OdfDocument reread = WriteAndRead(document, OdfMediaTypes.Text);
        OdfMasterPage rereadMaster = reread.MasterPages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(Content(rereadMaster, OdfMasterPageContentKind.Header)
                .TextBlocks.Single().Text, Is.EqualTo("New header"));
            Assert.That(Content(rereadMaster, OdfMasterPageContentKind.Footer)
                .TextBlocks.Single().Text, Is.EqualTo("Page 42"));
            Assert.That(reread.TextFields.Single(
                field => field.ElementName == "page-number").DisplayText,
                Is.EqualTo("42"));
        });
    }

    [Test]
    public void ResolvesWritesAndExtendsPageSequencesAndTitlePatterns()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles>"
            + "<style:master-page style:name='Title' style:next-style-name='Body'>"
            + "<style:header-first><text:p>Title header</text:p></style:header-first>"
            + "</style:master-page>"
            + "<style:master-page style:name='Body'/><style:master-page style:name='Appendix'/>"
            + "</office:master-styles>",
            "<text:page-sequence><text:page text:master-page-name='Title'/>"
            + "<text:page text:master-page-name='Body'/></text:page-sequence>");
        OdfPageSequence sequence = document.PageSequences.Single();
        OdfTitlePagePattern pattern = document.TitlePagePatterns.Single();

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Pages[0].MasterPage?.Name, Is.EqualTo("Title"));
            Assert.That(sequence.Pages[1].MasterPage?.Name, Is.EqualTo("Body"));
            Assert.That(pattern.TitleMasterPage, Is.SameAs(sequence.Pages[0].MasterPage));
            Assert.That(pattern.FollowingMasterPage, Is.SameAs(sequence.Pages[1].MasterPage));
            Assert.That(pattern.UsesFirstHeader, Is.True);
        });

        pattern.FollowingMasterPageName = "Appendix";
        sequence.Pages =
        [
            sequence.Pages[0],
            sequence.Pages[1],
            new OdfPageSequenceEntry { MasterPageName = "Body" }
        ];
        OdfDocument reread = WriteAndRead(document, OdfMediaTypes.Text);

        Assert.Multiple(() =>
        {
            Assert.That(reread.PageSequences.Single().Pages, Has.Length.EqualTo(3));
            Assert.That(reread.PageSequences[0].Pages[1].MasterPageName,
                Is.EqualTo("Appendix"));
            Assert.That(reread.PageSequences[0].Pages[1].MasterPage?.Name,
                Is.EqualTo("Appendix"));
            Assert.That(reread.PageSequences[0].Pages[2].MasterPageName,
                Is.EqualTo("Body"));
            Assert.That(reread.TitlePagePatterns[0].FollowingMasterPageName,
                Is.EqualTo("Appendix"));
        });
    }

    [Test]
    public void MaterializesAndWritesMasterDocumentSubdocumentLinks()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.TextMaster,
            "<office:master-styles/>",
            "<text:section text:name='ChapterOne'><text:section-source "
            + "xlink:type='simple' xlink:href='chapter1.odt' text:section-name='Chapter' "
            + "text:filter-name='writer8' xlink:show='embed' xlink:actuate='onLoad'/>"
            + "<text:p>Cached chapter</text:p></text:section>");
        OdfMasterSubdocument linked = document.MasterSubdocuments.Single();

        Assert.Multiple(() =>
        {
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.TextMaster));
            Assert.That(linked.Name, Is.EqualTo("ChapterOne"));
            Assert.That(linked.Href, Is.EqualTo("chapter1.odt"));
            Assert.That(linked.SectionName, Is.EqualTo("Chapter"));
            Assert.That(linked.FilterName, Is.EqualTo("writer8"));
            Assert.That(linked.Section, Is.SameAs(document.TextSections.Single()));
        });

        linked.Name = "RenamedChapter";
        linked.Href = "chapters/new.odt";
        linked.SectionName = "NewSource";
        linked.FilterName = "NewFilter";
        OdfDocument reread = WriteAndRead(document, OdfMediaTypes.TextMaster);
        OdfMasterSubdocument changed = reread.MasterSubdocuments.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Name, Is.EqualTo("RenamedChapter"));
            Assert.That(changed.Href, Is.EqualTo("chapters/new.odt"));
            Assert.That(changed.SectionName, Is.EqualTo("NewSource"));
            Assert.That(changed.FilterName, Is.EqualTo("NewFilter"));
        });
    }

    [Test]
    public void InfersTitlePatternFromFirstPageVariantsWithoutPageSequence()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles>"
            + "<style:master-page style:name='Standard' style:next-style-name='Body'>"
            + "<style:footer-first><text:p>First footer</text:p></style:footer-first>"
            + "</style:master-page><style:master-page style:name='Body'/>"
            + "</office:master-styles>",
            "<text:p>Body</text:p>");
        OdfTitlePagePattern pattern = document.TitlePagePatterns.Single();

        Assert.Multiple(() =>
        {
            Assert.That(pattern.TitleMasterPageName, Is.EqualTo("Standard"));
            Assert.That(pattern.FollowingMasterPageName, Is.EqualTo("Body"));
            Assert.That(pattern.TitleMasterPage?.Name, Is.EqualTo("Standard"));
            Assert.That(pattern.FollowingMasterPage?.Name, Is.EqualTo("Body"));
            Assert.That(pattern.UsesFirstFooter, Is.True);
            Assert.That(pattern.PageSequence, Is.Null);
        });
    }

    private static OdfMasterPageContent Content(
        OdfMasterPage master,
        OdfMasterPageContentKind kind)
    {
        return master.TextContents.Single(content => content.Kind == kind);
    }

    private static OdfDocument ReadFlat(
        string mediaType,
        string masterStyles,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4' office:mimetype='" + mediaType + "'>"
            + masterStyles
            + "<office:body><office:text>" + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "master-text.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(
        OdfDocument document,
        string mediaType)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions { Deterministic = true });
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "master-text.fodt",
            new OdfLoadOptions());
        Assert.That(reread.MediaType, Is.EqualTo(mediaType));
        return reread;
    }
}
