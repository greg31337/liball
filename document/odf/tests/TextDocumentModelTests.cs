using System.Text;

namespace odf.Tests;

public class TextDocumentModelTests
{
    [Test]
    public void MaterializesParagraphHeadingInlineAndWhitespaceStructure()
    {
        OdfDocument document = ReadFlat(StructuredBody());
        OdfTextBlock heading = document.TextBlocks[0];
        OdfTextBlock paragraph = document.TextBlocks[1];
        OdfTextRun span = paragraph.Runs[2];
        OdfTextRun hyperlink = span.Runs[2];

        Assert.Multiple(() =>
        {
            Assert.That(document.TextBlocks, Has.Length.EqualTo(2));
            Assert.That(heading.IsHeading, Is.True);
            Assert.That(heading.OutlineLevel, Is.EqualTo(2));
            Assert.That(heading.RestartNumbering, Is.EqualTo("true"));
            Assert.That(heading.StartValue, Is.EqualTo("3"));
            Assert.That(heading.IsListHeader, Is.EqualTo("false"));
            Assert.That(heading.StyleName, Is.EqualTo("Heading"));
            Assert.That(heading.Text, Is.EqualTo("Title"));
            Assert.That(paragraph.IsHeading, Is.False);
            Assert.That(paragraph.StyleName, Is.EqualTo("Body"));
            Assert.That(paragraph.ClassNames, Is.EqualTo("First Second"));
            Assert.That(paragraph.ConditionalStyleName, Is.EqualTo("Conditional"));
            Assert.That(paragraph.Runs.Select(item => item.Kind), Is.EqualTo(
                new[]
                {
                    OdfTextRunKind.Text,
                    OdfTextRunKind.Space,
                    OdfTextRunKind.Span,
                    OdfTextRunKind.SoftPageBreak,
                    OdfTextRunKind.Unknown,
                    OdfTextRunKind.Text
                }));
            Assert.That(paragraph.Runs[1].SpaceCount, Is.EqualTo(3));
            Assert.That(span.StyleName, Is.EqualTo("Emphasis"));
            Assert.That(span.ClassNames, Is.EqualTo("Inline Strong"));
            Assert.That(span.Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Tab));
            Assert.That(span.Runs[1].TabReference, Is.EqualTo("2"));
            Assert.That(hyperlink.Kind, Is.EqualTo(OdfTextRunKind.Hyperlink));
            Assert.That(hyperlink.Href, Is.EqualTo("https://example.test/"));
            Assert.That(hyperlink.LinkName, Is.EqualTo("Example"));
            Assert.That(hyperlink.LinkTitle, Is.EqualTo("Example title"));
            Assert.That(hyperlink.TargetFrameName, Is.EqualTo("_blank"));
            Assert.That(hyperlink.LinkShow, Is.EqualTo("new"));
            Assert.That(hyperlink.LinkActuate, Is.EqualTo("onRequest"));
            Assert.That(hyperlink.StyleName, Is.EqualTo("Link"));
            Assert.That(hyperlink.VisitedStyleName, Is.EqualTo("Visited"));
            Assert.That(hyperlink.Runs[1].Kind, Is.EqualTo(OdfTextRunKind.LineBreak));
            Assert.That(
                paragraph.Text,
                Is.EqualTo("Alpha   Beta\tLink\nEnd2026Omega"));
        });
    }

    [Test]
    public void ExtractsPlainAndStructuredTextInDocumentOrder()
    {
        OdfDocument document = ReadFlat(StructuredBody());

        string plainText = TextExtractor.ExtractPlainText(document);
        OdfTextBlock[] structured = TextExtractor.ExtractStructuredText(
            document);

        Assert.Multiple(() =>
        {
            Assert.That(
                plainText,
                Is.EqualTo("Title\nAlpha   Beta\tLink\nEnd2026Omega"));
            Assert.That(structured, Is.Not.SameAs(document.TextBlocks));
            Assert.That(structured, Is.EqualTo(document.TextBlocks));
            Assert.That(structured[0], Is.SameAs(document.TextBlocks[0]));
            Assert.That(
                structured[1].Runs[3].Kind,
                Is.EqualTo(OdfTextRunKind.SoftPageBreak));
        });
    }

    [Test]
    public void WritesStructuredRunAndBlockFieldChanges()
    {
        OdfDocument document = ReadFlat(StructuredBody());
        OdfTextBlock heading = document.TextBlocks[0];
        OdfTextBlock paragraph = document.TextBlocks[1];
        OdfTextRun span = paragraph.Runs[2];
        OdfTextRun hyperlink = span.Runs[2];
        heading.OutlineLevel = 4;
        heading.StyleName = "NewHeading";
        paragraph.Runs[1].SpaceCount = 2;
        span.StyleName = "NewEmphasis";
        hyperlink.Href = "https://changed.test/";
        hyperlink.Runs[0].Text = "Changed";

        OdfDocument reread = WriteAndRead(document);
        OdfTextBlock rereadHeading = reread.TextBlocks[0];
        OdfTextBlock rereadParagraph = reread.TextBlocks[1];
        OdfTextRun rereadSpan = rereadParagraph.Runs[2];
        OdfTextRun rereadLink = rereadSpan.Runs[2];

        Assert.Multiple(() =>
        {
            Assert.That(rereadHeading.OutlineLevel, Is.EqualTo(4));
            Assert.That(rereadHeading.StyleName, Is.EqualTo("NewHeading"));
            Assert.That(rereadParagraph.Runs[1].SpaceCount, Is.EqualTo(2));
            Assert.That(rereadSpan.StyleName, Is.EqualTo("NewEmphasis"));
            Assert.That(rereadLink.Href, Is.EqualTo("https://changed.test/"));
            Assert.That(rereadLink.Text, Is.EqualTo("Changed\nEnd"));
            Assert.That(
                rereadParagraph.Text,
                Is.EqualTo("Alpha  Beta\tChanged\nEnd2026Omega"));
            Assert.That(
                AttributeValue(rereadParagraph.Node!, "urn:example:text", "keep"),
                Is.EqualTo("yes"));
        });
    }

    [Test]
    public void AddsRemovesAndReplacesTextBlocksOnWrite()
    {
        OdfDocument document = ReadFlat(
            "<text:h text:outline-level='1'>Remove</text:h>"
            + "<text:p text:style-name='Keep'>Old</text:p>");
        OdfTextBlock existing = document.TextBlocks[1];
        existing.Text = "Replaced text";
        OdfTextBlock added = new OdfTextBlock
        {
            IsHeading = true,
            OutlineLevel = 3,
            StyleName = "AddedHeading",
            Text = "Added"
        };
        document.TextBlocks = [existing, added];

        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextBlocks, Has.Length.EqualTo(2));
            Assert.That(
                reread.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "Replaced text", "Added" }));
            Assert.That(reread.TextBlocks[0].IsHeading, Is.False);
            Assert.That(reread.TextBlocks[0].StyleName, Is.EqualTo("Keep"));
            Assert.That(reread.TextBlocks[0].Runs, Has.Length.EqualTo(1));
            Assert.That(reread.TextBlocks[1].IsHeading, Is.True);
            Assert.That(reread.TextBlocks[1].OutlineLevel, Is.EqualTo(3));
            Assert.That(reread.TextBlocks[1].StyleName, Is.EqualTo("AddedHeading"));
            Assert.That(added.Node, Is.Not.Null);
        });
    }

    [Test]
    public void StyleRenameUpdatesTypedBlockAndInlineReferences()
    {
        string styles = "<office:styles>"
            + "<style:style style:name='Body' style:family='paragraph'/>"
            + "<style:style style:name='Inline' style:family='text'/>"
            + "</office:styles>";
        OdfDocument document = ReadFlatWithStyles(
            styles,
            "<text:p text:style-name='Body'>"
            + "<text:span text:style-name='Inline'>Text</text:span>"
            + "</text:p>");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle body = editor.FindStyle(
            document,
            "Body",
            OdfStyleFamily.Paragraph)!;
        OdfStyle inline = editor.FindStyle(
            document,
            "Inline",
            OdfStyleFamily.Text)!;

        editor.RenameStyle(document, body, "NewBody");
        editor.RenameStyle(document, inline, "NewInline");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.TextBlocks[0].StyleName, Is.EqualTo("NewBody"));
            Assert.That(
                document.TextBlocks[0].Runs[0].StyleName,
                Is.EqualTo("NewInline"));
            Assert.That(reread.TextBlocks[0].StyleName, Is.EqualTo("NewBody"));
            Assert.That(
                reread.TextBlocks[0].Runs[0].StyleName,
                Is.EqualTo("NewInline"));
        });
    }

    private static string StructuredBody()
    {
        return "<text:h text:outline-level='2' text:restart-numbering='true' "
            + "text:start-value='3' text:is-list-header='false' "
            + "text:style-name='Heading'>Title</text:h>"
            + "<text:p text:style-name='Body' text:class-names='First Second' "
            + "text:cond-style-name='Conditional' ext:keep='yes'>"
            + "Alpha<text:s text:c='3'/>"
            + "<text:span text:style-name='Emphasis' "
            + "text:class-names='Inline Strong'>"
            + "Beta<text:tab text:tab-ref='2'/>"
            + "<text:a xlink:type='simple' xlink:href='https://example.test/' "
            + "office:name='Example' office:title='Example title' "
            + "office:target-frame-name='_blank' xlink:show='new' "
            + "xlink:actuate='onRequest' text:style-name='Link' "
            + "text:visited-style-name='Visited'>"
            + "Link<text:line-break/>End"
            + "</text:a></text:span>"
            + "<text:soft-page-break/><text:date>2026</text:date>Omega"
            + "</text:p>";
    }

    private static OdfDocument ReadFlat(string body)
    {
        return ReadFlatWithStyles("<office:styles/>", body);
    }

    private static OdfDocument ReadFlatWithStyles(
        string styles,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:text'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-model.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "text-model.fodt",
            new OdfLoadOptions());
    }

    private static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName).Value;
    }
}
