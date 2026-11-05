using System.Text;

namespace odf.Tests;

public class TextRangeModelTests
{
    [Test]
    public void MaterializesPointAndPairedMarksForEveryRangeFamily()
    {
        OdfDocument document = ReadFlat(
            "<text:p>"
            + "<text:bookmark text:name='PointBookmark' xml:id='bookmark-id'/>"
            + "<text:bookmark-start text:name='BookmarkRange'/><text:bookmark-end text:name='BookmarkRange'/>"
            + "<text:reference-mark text:name='PointReference'/>"
            + "<text:reference-mark-start text:name='ReferenceRange'/><text:reference-mark-end text:name='ReferenceRange'/>"
            + "<text:toc-mark text:string-value='PointToc' text:outline-level='2'/>"
            + "<text:toc-mark-start text:id='TocRange' text:outline-level='3'/><text:toc-mark-end text:id='TocRange'/>"
            + "<text:user-index-mark text:string-value='PointUser' text:index-name='UserIndex'/>"
            + "<text:user-index-mark-start text:id='UserRange' text:index-name='UserIndex'/><text:user-index-mark-end text:id='UserRange'/>"
            + "<text:alphabetical-index-mark text:string-value='PointAlpha' text:key1='K1' text:key2='K2'/>"
            + "<text:alphabetical-index-mark-start text:id='AlphaRange' text:key1='RangeKey'/><text:alphabetical-index-mark-end text:id='AlphaRange'/>"
            + "<text:bibliography-mark text:identifier='Book1' text:bibliography-type='book' text:author='Author'/>"
            + "</text:p>");

        OdfTextRange bookmark = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "BookmarkRange")!;
        OdfTextRange pointBookmark = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "PointBookmark")!;
        OdfTextRange bibliography = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.BibliographyMark,
            "Book1")!;
        OdfTextRun[] runs = document.TextBlocks[0].Runs;

        Assert.Multiple(() =>
        {
            Assert.That(document.TextRanges, Has.Length.EqualTo(11));
            Assert.That(bookmark.IsRange, Is.True);
            Assert.That(bookmark.IsResolved, Is.True);
            Assert.That(bookmark.StartRun?.Kind, Is.EqualTo(OdfTextRunKind.BookmarkStart));
            Assert.That(bookmark.EndRun?.Kind, Is.EqualTo(OdfTextRunKind.BookmarkEnd));
            Assert.That(pointBookmark.IsRange, Is.False);
            Assert.That(pointBookmark.IsResolved, Is.True);
            Assert.That(pointBookmark.XmlId, Is.EqualTo("bookmark-id"));
            Assert.That(bibliography.IsRange, Is.False);
            Assert.That(bibliography.StartRun?.BibliographyType, Is.EqualTo("book"));
            Assert.That(
                bibliography.StartRun?.Attributes.Single(
                    item => item.Name.LocalName == "author").Value,
                Is.EqualTo("Author"));
            Assert.That(
                runs.Single(item => item.Kind == OdfTextRunKind.TableOfContentMark)
                    .MarkerOutlineLevel,
                Is.EqualTo("2"));
            Assert.That(
                runs.Single(item => item.Kind == OdfTextRunKind.UserIndexMark)
                    .IndexName,
                Is.EqualTo("UserIndex"));
            Assert.That(
                runs.Single(item => item.Kind == OdfTextRunKind.AlphabeticalIndexMark)
                    .Key1,
                Is.EqualTo("K1"));
        });
    }

    [Test]
    public void ReportsMissingReversedAndDuplicateEndpointsAsUnsafe()
    {
        OdfDocument document = ReadFlat(
            "<text:p>"
            + "<text:bookmark-end text:name='Reversed'/>"
            + "<text:bookmark-start text:name='Reversed'/>"
            + "<text:bookmark-start text:name='Missing'/>"
            + "<text:bookmark-start text:name='Duplicate'/>"
            + "<text:bookmark-start text:name='Duplicate'/>"
            + "<text:bookmark-end text:name='Duplicate'/>"
            + "<text:bookmark-end text:name='Orphan'/>"
            + "</text:p>");
        OdfTextRange reversed = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "Reversed")!;
        OdfTextRange missing = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "Missing")!;
        OdfTextRange orphan = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "Orphan")!;

        Assert.Multiple(() =>
        {
            Assert.That(reversed.IsResolved, Is.False);
            Assert.That(reversed.IsReversed, Is.True);
            Assert.That(missing.IsResolved, Is.False);
            Assert.That(missing.EndNode, Is.Null);
            Assert.That(orphan.IsResolved, Is.False);
            Assert.That(orphan.StartNode, Is.Null);
            Assert.That(
                TextNavigator.FindTextRange(
                    document,
                    OdfTextRangeKind.Bookmark,
                    "Duplicate"),
                Is.Null);
            Assert.That(
                document.TextRanges.Where(item => item.Name == "Duplicate")
                    .All(item => item.HasDuplicateEndpoint),
                Is.True);
            Assert.That(
                TextNavigator.NavigateTextRange(document, reversed),
                Is.Empty);
            Assert.That(
                () => TextNavigator.RenameTextRange(
                    document,
                    missing,
                    "Unsafe"),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void NavigatesExtractsAndRenamesRangeWithReferencesSafely()
    {
        OdfDocument document = ReadFlat(
            "<text:p>"
            + "<text:bookmark-start text:name='Target' xml:id='target-id'/>"
            + "A<text:span xml:id='unique-id'>B</text:span><text:s text:c='2'/>"
            + "<text:bookmark-end text:name='Target'/>"
            + "<text:bookmark-ref text:ref-name='Target' text:reference-format='text'>Target text</text:bookmark-ref>"
            + "<text:span xml:id='duplicate-id'>One</text:span>"
            + "<text:span xml:id='duplicate-id'>Two</text:span>"
            + "</text:p>");
        OdfTextRange range = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "Target")!;

        Assert.That(range.IsResolved, Is.True);

        OdfNode[] nodes = TextNavigator.NavigateTextRange(document, range);
        string text = TextNavigator.ExtractTextRange(document, range);
        OdfNode? xmlId = TextNavigator.FindXmlIdNode(document, "unique-id");
        OdfNode? duplicateId = TextNavigator.FindXmlIdNode(
            document,
            "duplicate-id");
        TextNavigator.RenameTextRange(document, range, "Renamed");
        OdfDocument reread = WriteAndRead(document);
        OdfTextRange renamed = TextNavigator.FindTextRange(
            reread,
            OdfTextRangeKind.Bookmark,
            "Renamed")!;
        OdfNode reference = FindElement(reread, "bookmark-ref");

        Assert.Multiple(() =>
        {
            Assert.That(nodes, Is.Not.Empty);
            Assert.That(text, Is.EqualTo("AB  "));
            Assert.That(xmlId?.Name?.LocalName, Is.EqualTo("span"));
            Assert.That(duplicateId, Is.Null);
            Assert.That(renamed.IsResolved, Is.True);
            Assert.That(renamed.StartRun?.MarkerName, Is.EqualTo("Renamed"));
            Assert.That(renamed.EndRun?.MarkerName, Is.EqualTo("Renamed"));
            Assert.That(
                AttributeValue(reference, OdfNamespaces.Text, "ref-name"),
                Is.EqualTo("Renamed"));
        });
    }

    [Test]
    public void RenamesPointIndexValueWithoutInventingNameAttribute()
    {
        OdfDocument document = ReadFlat(
            "<text:p><text:toc-mark text:string-value='OldValue'/>"
            + "</text:p>");
        OdfTextRange range = TextNavigator.FindTextRange(
            document,
            OdfTextRangeKind.TableOfContentMark,
            "OldValue")!;

        TextNavigator.RenameTextRange(document, range, "NewValue");
        OdfDocument reread = WriteAndRead(document);
        OdfTextRange renamed = TextNavigator.FindTextRange(
            reread,
            OdfTextRangeKind.TableOfContentMark,
            "NewValue")!;
        OdfNode marker = FindElement(reread, "toc-mark");

        Assert.Multiple(() =>
        {
            Assert.That(renamed.StartRun?.StringValue, Is.EqualTo("NewValue"));
            Assert.That(renamed.StartRun?.MarkerName, Is.Empty);
            Assert.That(
                AttributeValue(marker, OdfNamespaces.Text, "string-value"),
                Is.EqualTo("NewValue"));
            Assert.That(
                marker.Attributes.Any(
                    item => item.Name.NamespaceName == OdfNamespaces.Text
                        && item.Name.LocalName == "name"),
                Is.False);
        });
    }

    [Test]
    public void ModelsAndWritesRubyWithoutAddingAnnotationToPlainText()
    {
        OdfDocument document = ReadFlat(
            "<text:p>Before"
            + "<text:ruby text:style-name='RubyStyle'>"
            + "<text:ruby-base>A<text:span text:style-name='BaseStyle'>B</text:span></text:ruby-base>"
            + "<text:ruby-text text:style-name='AnnotationStyle'>annotation</text:ruby-text>"
            + "</text:ruby>After</text:p>");
        OdfTextBlock paragraph = document.TextBlocks[0];
        OdfTextRun ruby = paragraph.Runs[1];

        ruby.Runs[0].Text = "Changed";
        ruby.RubyText = "new annotation";
        ruby.StyleName = "NewRuby";
        ruby.RubyTextStyleName = "NewAnnotation";
        OdfDocument reread = WriteAndRead(document);
        OdfTextRun rereadRuby = reread.TextBlocks[0].Runs[1];

        Assert.Multiple(() =>
        {
            Assert.That(ruby.Kind, Is.EqualTo(OdfTextRunKind.Ruby));
            Assert.That(ruby.Text, Is.EqualTo("ChangedB"));
            Assert.That(ruby.RubyText, Is.EqualTo("new annotation"));
            Assert.That(rereadRuby.StyleName, Is.EqualTo("NewRuby"));
            Assert.That(rereadRuby.RubyTextStyleName, Is.EqualTo("NewAnnotation"));
            Assert.That(rereadRuby.RubyText, Is.EqualTo("new annotation"));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("BeforeChangedBAfter"));
            Assert.That(
                TextExtractor.ExtractPlainText(reread),
                Is.EqualTo("BeforeChangedBAfter"));
        });
    }

    [Test]
    public void StyleRenameDistinguishesRubyAndAnnotationStyleFamilies()
    {
        string styles = "<office:styles>"
            + "<style:style style:name='RubyStyle' style:family='ruby'/>"
            + "<style:style style:name='AnnotationStyle' style:family='text'/>"
            + "</office:styles>";
        OdfDocument document = ReadFlatWithStyles(
            styles,
            "<text:p><text:ruby text:style-name='RubyStyle'>"
            + "<text:ruby-base>Base</text:ruby-base>"
            + "<text:ruby-text text:style-name='AnnotationStyle'>Note</text:ruby-text>"
            + "</text:ruby></text:p>");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle rubyStyle = editor.FindStyle(
            document,
            "RubyStyle",
            OdfStyleFamily.Ruby)!;
        OdfStyle annotationStyle = editor.FindStyle(
            document,
            "AnnotationStyle",
            OdfStyleFamily.Text)!;

        editor.RenameStyle(document, rubyStyle, "RenamedRuby");
        editor.RenameStyle(document, annotationStyle, "RenamedAnnotation");
        OdfDocument reread = WriteAndRead(document);
        OdfTextRun ruby = reread.TextBlocks[0].Runs[0];

        Assert.Multiple(() =>
        {
            Assert.That(ruby.StyleName, Is.EqualTo("RenamedRuby"));
            Assert.That(
                ruby.RubyTextStyleName,
                Is.EqualTo("RenamedAnnotation"));
        });
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
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-ranges.fodt",
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
            "text-ranges.fodt",
            new OdfLoadOptions());
    }

    private static OdfNode FindElement(OdfDocument document, string localName)
    {
        return new OdfTreeEditor(document.Nodes).TraverseAllNodes().Single(
            item => item.Name?.NamespaceName == OdfNamespaces.Text
                && item.Name.LocalName == localName);
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
