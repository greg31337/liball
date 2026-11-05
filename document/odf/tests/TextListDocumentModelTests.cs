using System.Text;

namespace odf.Tests;

public class TextListDocumentModelTests
{
    [Test]
    public void MaterializesNestedListKindsHeadersLabelsAndContinuations()
    {
        OdfDocument document = ReadFlat(
            ListStyles(),
            "<text:list text:style-name='Mixed' xml:id='ListOne'>"
            + "<text:list-header><text:p>Header</text:p></text:list-header>"
            + "<text:list-item text:start-value='5'><text:p>First</text:p>"
            + "<text:list><text:list-item><text:p>Bullet</text:p>"
            + "<text:list><text:list-item><text:p>Image</text:p></text:list-item></text:list>"
            + "</text:list-item></text:list></text:list-item>"
            + "<text:list-item><text:p>Second</text:p></text:list-item>"
            + "</text:list>"
            + "<text:list text:style-name='Mixed' xml:id='ListTwo' text:continue-list='ListOne'>"
            + "<text:list-item><text:p>Continued</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Outline' xml:id='OutlineList'>"
            + "<text:list-item><text:p>Outline item</text:p></text:list-item></text:list>"
            + "<text:numbered-paragraph text:list-id='Numbered' text:level='1' "
            + "text:style-name='Mixed' text:start-value='10'><text:p>Ten</text:p>"
            + "</text:numbered-paragraph>"
            + "<text:numbered-paragraph text:list-id='Numbered' text:level='1' "
            + "text:style-name='Mixed'><text:p>Eleven</text:p>"
            + "</text:numbered-paragraph>");
        OdfTextList first = TextListNavigator.FindListByXmlId(
            document,
            "ListOne")!;
        OdfTextList second = TextListNavigator.FindListByXmlId(
            document,
            "ListTwo")!;
        OdfTextList outline = TextListNavigator.FindListByXmlId(
            document,
            "OutlineList")!;
        OdfTextList bullet = first.Items[0].Lists[0];
        OdfTextList image = bullet.Items[0].Lists[0];
        OdfListLevel firstLevel = first.LevelStyle!;

        Assert.Multiple(() =>
        {
            Assert.That(document.TextLists, Has.Length.EqualTo(5));
            Assert.That(first.Header?.TextBlocks[0].Text, Is.EqualTo("Header"));
            Assert.That(first.Items, Has.Length.EqualTo(2));
            Assert.That(first.Items[0].TextBlocks[0].Text, Is.EqualTo("First"));
            Assert.That(first.Items[0].Ordinal, Is.EqualTo(5));
            Assert.That(first.Items[0].LabelText, Is.EqualTo("(5)"));
            Assert.That(first.Items[1].Ordinal, Is.EqualTo(6));
            Assert.That(bullet.Level, Is.EqualTo(2));
            Assert.That(bullet.Kind, Is.EqualTo(OdfTextListKind.Bullet));
            Assert.That(bullet.Items[0].LabelText, Is.EqualTo("•"));
            Assert.That(image.Level, Is.EqualTo(3));
            Assert.That(image.Kind, Is.EqualTo(OdfTextListKind.Image));
            Assert.That(image.Items[0].LabelText, Is.EqualTo("Pictures/bullet.png"));
            Assert.That(second.ContinuedList, Is.SameAs(first));
            Assert.That(second.Items[0].Ordinal, Is.EqualTo(7));
            Assert.That(TextListNavigator.FollowContinuation(second), Is.EqualTo(new[] { first, second }));
            Assert.That(outline.Kind, Is.EqualTo(OdfTextListKind.Outline));
            Assert.That(outline.Items[0].LabelText, Is.EqualTo("I."));
            Assert.That(document.NumberedParagraphs, Has.Length.EqualTo(2));
            Assert.That(document.NumberedParagraphs[0].Ordinal, Is.EqualTo(10));
            Assert.That(document.NumberedParagraphs[1].Ordinal, Is.EqualTo(11));
            Assert.That(firstLevel.PositionAndSpaceMode, Is.EqualTo("label-alignment"));
            Assert.That(firstLevel.LabelFollowedBy, Is.EqualTo("listtab"));
            Assert.That(firstLevel.ListTabStopPosition, Is.EqualTo("1.25cm"));
            Assert.That(firstLevel.LabelTextIndent, Is.EqualTo("-0.5cm"));
            Assert.That(firstLevel.LabelMarginLeft, Is.EqualTo("1cm"));
        });
    }

    [Test]
    public void WritesStartsContinuationsNumberedParagraphsAndLabelConfiguration()
    {
        OdfDocument document = ReadFlat(
            ListStyles(),
            "<text:list text:style-name='Mixed' xml:id='Old'>"
            + "<text:list-item><text:p>One</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='Next' text:continue-list='Old'>"
            + "<text:list-item><text:p>Two</text:p></text:list-item></text:list>"
            + "<text:numbered-paragraph text:list-id='Standalone' text:level='1' "
            + "text:style-name='Mixed'><text:p>Numbered</text:p></text:numbered-paragraph>");
        OdfTextList first = TextListNavigator.FindListByXmlId(document, "Old")!;
        OdfTextList next = TextListNavigator.FindListByXmlId(document, "Next")!;

        TextListNavigator.RenameListXmlId(document, first, "Renamed");
        first.Items[0].StartValue = "8";
        next.Items[0].StyleOverride = "OverrideList";
        document.NumberedParagraphs[0].StartValue = "20";
        OdfListLevel level = first.LevelStyle!;
        level.LabelFollowedBy = "space";
        level.ListTabStopPosition = "2cm";
        level.LabelTextIndent = "-0.75cm";
        level.LabelMarginLeft = "1.5cm";
        level.SpaceBefore = "0.25cm";
        OdfDocument reread = WriteAndRead(document);
        OdfTextList renamed = TextListNavigator.FindListByXmlId(
            reread,
            "Renamed")!;
        OdfTextList rereadNext = TextListNavigator.FindListByXmlId(
            reread,
            "Next")!;

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Items[0].Ordinal, Is.EqualTo(8));
            Assert.That(rereadNext.ContinueListId, Is.EqualTo("Renamed"));
            Assert.That(rereadNext.ContinuedList, Is.SameAs(renamed));
            Assert.That(rereadNext.Items[0].Ordinal, Is.EqualTo(9));
            Assert.That(rereadNext.Items[0].StyleOverride, Is.EqualTo("OverrideList"));
            Assert.That(reread.NumberedParagraphs[0].Ordinal, Is.EqualTo(20));
            Assert.That(renamed.LevelStyle?.LabelFollowedBy, Is.EqualTo("space"));
            Assert.That(renamed.LevelStyle?.ListTabStopPosition, Is.EqualTo("2cm"));
            Assert.That(renamed.LevelStyle?.LabelTextIndent, Is.EqualTo("-0.75cm"));
            Assert.That(renamed.LevelStyle?.LabelMarginLeft, Is.EqualTo("1.5cm"));
            Assert.That(renamed.LevelStyle?.SpaceBefore, Is.EqualTo("0.25cm"));
        });
    }

    [Test]
    public void ResolvesImplicitContinuationAndRejectsUnsafeReferences()
    {
        OdfDocument document = ReadFlat(
            ListStyles(),
            "<text:list text:style-name='Mixed' xml:id='First'>"
            + "<text:list-item><text:p>First</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='Implicit' text:continue-numbering='true'>"
            + "<text:list-item><text:p>Implicit</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='Broken' text:continue-list='Missing'>"
            + "<text:list-item><text:p>Broken</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='CycleA' text:continue-list='CycleB'>"
            + "<text:list-item><text:p>A</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='CycleB' text:continue-list='CycleA'>"
            + "<text:list-item><text:p>B</text:p></text:list-item></text:list>"
            + "<text:list text:style-name='Mixed' xml:id='Duplicate'/><text:list "
            + "text:style-name='Mixed' xml:id='Duplicate'/>");
        OdfTextList implicitList = TextListNavigator.FindListByXmlId(
            document,
            "Implicit")!;
        OdfTextList broken = TextListNavigator.FindListByXmlId(
            document,
            "Broken")!;
        OdfTextList cycle = TextListNavigator.FindListByXmlId(
            document,
            "CycleA")!;

        Assert.Multiple(() =>
        {
            Assert.That(implicitList.Items[0].Ordinal, Is.EqualTo(4));
            Assert.That(implicitList.ContinuedList?.XmlId, Is.EqualTo("First"));
            Assert.That(broken.HasBrokenContinuation, Is.True);
            Assert.That(TextListNavigator.FollowContinuation(broken), Is.Empty);
            Assert.That(cycle.HasContinuationCycle, Is.True);
            Assert.That(TextListNavigator.FollowContinuation(cycle), Is.Empty);
            Assert.That(TextListNavigator.FindListByXmlId(document, "Duplicate"), Is.Null);
            Assert.That(
                () => TextListNavigator.RenameListXmlId(document, cycle, "Duplicate"),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void PreservesLegacyAndModernLabelModesIndependently()
    {
        OdfDocument document = ReadFlat(
            "<office:automatic-styles><text:list-style style:name='Legacy'>"
            + "<text:list-level-style-bullet text:level='1' text:bullet-char='-'>"
            + "<style:list-level-properties text:list-level-position-and-space-mode='label-width-and-position' "
            + "text:space-before='0.4cm' text:min-label-width='0.6cm' "
            + "text:min-label-distance='0.2cm'/></text:list-level-style-bullet>"
            + "</text:list-style></office:automatic-styles>",
            "<text:list text:style-name='Legacy'><text:list-item><text:p>Item</text:p>"
            + "</text:list-item></text:list>");
        OdfListLevel level = document.TextLists[0].LevelStyle!;

        Assert.Multiple(() =>
        {
            Assert.That(level.PositionAndSpaceMode, Is.EqualTo("label-width-and-position"));
            Assert.That(level.SpaceBefore, Is.EqualTo("0.4cm"));
            Assert.That(level.MinimumLabelWidth, Is.EqualTo("0.6cm"));
            Assert.That(level.MinimumLabelDistance, Is.EqualTo("0.2cm"));
            Assert.That(level.LabelFollowedBy, Is.Empty);
        });
    }

    private static string ListStyles()
    {
        return "<office:styles><text:outline-style style:name='Outline'>"
            + "<text:outline-level-style text:level='1' style:num-format='I' "
            + "style:num-suffix='.'/></text:outline-style></office:styles>"
            + "<office:automatic-styles><text:list-style style:name='Mixed'>"
            + "<text:list-level-style-number text:level='1' style:num-format='1' "
            + "style:num-prefix='(' style:num-suffix=')' text:start-value='3'>"
            + "<style:list-level-properties text:list-level-position-and-space-mode='label-alignment'>"
            + "<style:list-level-label-alignment text:label-followed-by='listtab' "
            + "text:list-tab-stop-position='1.25cm' fo:text-indent='-0.5cm' "
            + "fo:margin-left='1cm'/></style:list-level-properties>"
            + "</text:list-level-style-number>"
            + "<text:list-level-style-bullet text:level='2' text:bullet-char='•'/>"
            + "<text:list-level-style-image text:level='3' xlink:href='Pictures/bullet.png'/>"
            + "</text:list-style></office:automatic-styles>";
    }

    private static OdfDocument ReadFlat(string styles, string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-lists.fodt",
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
            "text-lists.fodt",
            new OdfLoadOptions());
    }
}
