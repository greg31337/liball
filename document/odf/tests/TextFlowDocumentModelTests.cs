using System.Text;

namespace odf.Tests;

public class TextFlowDocumentModelTests
{
    [Test]
    public void MaterializesSectionsColumnsFramesTextBoxesAnchorsAndBreaks()
    {
        OdfDocument document = ReadFlat(
            "<office:automatic-styles>"
            + "<style:style style:name='SectionStyle' style:family='section'>"
            + "<style:section-properties><style:columns fo:column-count='2' fo:column-gap='0.5cm'>"
            + "<style:column style:rel-width='1*' fo:end-indent='0.1cm'/>"
            + "<style:column style:rel-width='2*' fo:start-indent='0.1cm'/>"
            + "<style:column-sep style:style='solid' style:width='0.02cm' "
            + "style:color='#112233' style:height='80%' style:vertical-align='middle'/>"
            + "</style:columns></style:section-properties></style:style>"
            + "<style:style style:name='BreakStyle' style:family='paragraph'>"
            + "<style:paragraph-properties fo:break-before='page' fo:break-after='column'/>"
            + "</style:style></office:automatic-styles>",
            "<text:section text:name='Outer' text:style-name='SectionStyle' "
            + "text:condition='ooow:ready' text:display='condition' text:protected='true' "
            + "text:protection-key='encoded' text:protection-key-digest-algorithm='urn:digest'>"
            + "<text:p text:style-name='BreakStyle'>A<text:line-break/>B<text:soft-page-break/></text:p>"
            + "<text:section text:name='Linked'>"
            + "<text:section-source xlink:type='simple' xlink:href='linked.odt' "
            + "text:section-name='Remote' text:filter-name='writer8' "
            + "xlink:show='embed' xlink:actuate='onLoad'/>"
            + "<text:p>Linked text</text:p></text:section>"
            + "<draw:frame draw:name='FrameA' draw:style-name='FrameStyle' "
            + "draw:text-style-name='FrameParagraph' draw:layer='layout' "
            + "text:anchor-type='paragraph' svg:x='1cm' svg:y='2cm' "
            + "svg:width='4cm' svg:height='3cm' draw:z-index='4' "
            + "draw:chain-next-name='FrameB'>"
            + "<draw:text-box fo:min-width='2cm' fo:max-height='5cm' draw:corner-radius='0.1cm'>"
            + "<text:p>First box</text:p></draw:text-box></draw:frame>"
            + "<draw:frame draw:name='FrameB' text:anchor-type='as-char'>"
            + "<draw:text-box><text:p>Second box</text:p></draw:text-box>"
            + "</draw:frame></text:section>");
        OdfTextSection outer = TextFlowNavigator.FindSection(
            document,
            "Outer")!;
        OdfTextSection linked = TextFlowNavigator.FindSection(
            document,
            "Linked")!;
        OdfTextFrame first = TextFlowNavigator.FindFrame(
            document,
            "FrameA")!;
        OdfTextFrame second = TextFlowNavigator.FindFrame(
            document,
            "FrameB")!;

        Assert.Multiple(() =>
        {
            Assert.That(document.TextSections, Has.Length.EqualTo(2));
            Assert.That(outer.IsProtected, Is.True);
            Assert.That(outer.ProtectionKey, Is.EqualTo("encoded"));
            Assert.That(outer.Sections, Is.EqualTo(new[] { linked }));
            Assert.That(outer.TextBlocks, Has.Length.EqualTo(4));
            Assert.That(outer.Columns?.Count, Is.EqualTo(2));
            Assert.That(outer.Columns?.Gap, Is.EqualTo("0.5cm"));
            Assert.That(outer.Columns?.Columns[1].RelativeWidth, Is.EqualTo("2*"));
            Assert.That(outer.Columns?.SeparatorColor, Is.EqualTo("#112233"));
            Assert.That(linked.SourceHref, Is.EqualTo("linked.odt"));
            Assert.That(linked.SourceSectionName, Is.EqualTo("Remote"));
            Assert.That(linked.SourceFilterName, Is.EqualTo("writer8"));
            Assert.That(document.TextFrames, Has.Length.EqualTo(2));
            Assert.That(first.AnchorKind, Is.EqualTo(OdfTextAnchorKind.Paragraph));
            Assert.That(second.AnchorKind, Is.EqualTo(OdfTextAnchorKind.AsCharacter));
            Assert.That(first.NextFrame, Is.SameAs(second));
            Assert.That(second.PreviousFrame, Is.SameAs(first));
            Assert.That(first.TextBox?.TextBlocks[0].Text, Is.EqualTo("First box"));
            Assert.That(first.TextBox?.MinimumWidth, Is.EqualTo("2cm"));
            Assert.That(first.TextBox?.MaximumHeight, Is.EqualTo("5cm"));
            Assert.That(TextFlowNavigator.FollowFrameChain(first), Is.EqualTo(new[] { first, second }));
            Assert.That(document.TextBreaks, Has.Length.EqualTo(4));
            Assert.That(document.TextBreaks.Any(item => item.Kind == OdfTextBreakKind.PageBefore), Is.True);
            Assert.That(document.TextBreaks.Any(item => item.Kind == OdfTextBreakKind.ColumnAfter), Is.True);
            Assert.That(document.TextBreaks.Any(item => item.Kind == OdfTextBreakKind.Line), Is.True);
            Assert.That(document.TextBreaks.Any(item => item.Kind == OdfTextBreakKind.SoftPage), Is.True);
        });
    }

    [Test]
    public void WritesSectionFrameColumnTextBoxAndBreakChanges()
    {
        OdfDocument document = ReadFlat(
            "<office:automatic-styles>"
            + "<style:style style:name='SectionStyle' style:family='section'>"
            + "<style:section-properties><style:columns fo:column-count='2'>"
            + "<style:column style:rel-width='1*'/><style:column style:rel-width='1*'/>"
            + "</style:columns></style:section-properties></style:style>"
            + "<style:style style:name='BreakStyle' style:family='paragraph'>"
            + "<style:paragraph-properties fo:break-before='page'/>"
            + "</style:style></office:automatic-styles>",
            "<text:section text:name='Target' text:style-name='SectionStyle'>"
            + "<text:p text:style-name='BreakStyle'>Text</text:p></text:section>"
            + "<text:section text:name='LocalLink'><text:section-source "
            + "xlink:type='simple' text:section-name='Target'/></text:section>"
            + "<draw:frame draw:name='First' draw:chain-next-name='Second' "
            + "text:anchor-type='paragraph' svg:width='2cm'>"
            + "<draw:text-box fo:min-height='1cm'><text:p>Box</text:p></draw:text-box>"
            + "</draw:frame><draw:frame draw:name='Second' text:anchor-type='char'/>"
        );
        OdfTextSection target = TextFlowNavigator.FindSection(
            document,
            "Target")!;
        OdfTextFrame first = TextFlowNavigator.FindFrame(
            document,
            "First")!;
        OdfTextFrame second = TextFlowNavigator.FindFrame(
            document,
            "Second")!;

        TextFlowNavigator.RenameSection(document, target, "RenamedSection");
        TextFlowNavigator.RenameFrame(document, second, "RenamedFrame");
        target.IsProtected = true;
        target.ProtectionKey = "new-key";
        target.Columns!.Gap = "0.75cm";
        target.Columns.Columns[0].RelativeWidth = "3*";
        target.Columns.SeparatorStyle = "dotted";
        target.Columns.SeparatorHeight = "60%";
        first.AnchorType = "page";
        first.AnchorPageNumber = "3";
        first.Width = "6cm";
        first.TextBox!.MinimumHeight = "2cm";
        document.TextBlocks.Single(item => item.Text == "Text").BreakBefore = "even-page";
        OdfDocument reread = WriteAndRead(document);
        OdfTextSection renamed = TextFlowNavigator.FindSection(
            reread,
            "RenamedSection")!;
        OdfTextFrame rereadFirst = TextFlowNavigator.FindFrame(
            reread,
            "First")!;

        Assert.Multiple(() =>
        {
            Assert.That(renamed.IsProtected, Is.True);
            Assert.That(renamed.ProtectionKey, Is.EqualTo("new-key"));
            Assert.That(renamed.Columns?.Gap, Is.EqualTo("0.75cm"));
            Assert.That(renamed.Columns?.Columns[0].RelativeWidth, Is.EqualTo("3*"));
            Assert.That(renamed.Columns?.SeparatorStyle, Is.EqualTo("dotted"));
            Assert.That(TextFlowNavigator.FindSection(reread, "LocalLink")?.SourceSectionName, Is.EqualTo("RenamedSection"));
            Assert.That(rereadFirst.ChainNextName, Is.EqualTo("RenamedFrame"));
            Assert.That(rereadFirst.NextFrame?.Name, Is.EqualTo("RenamedFrame"));
            Assert.That(rereadFirst.AnchorKind, Is.EqualTo(OdfTextAnchorKind.Page));
            Assert.That(rereadFirst.AnchorPageNumber, Is.EqualTo("3"));
            Assert.That(rereadFirst.Width, Is.EqualTo("6cm"));
            Assert.That(rereadFirst.TextBox?.MinimumHeight, Is.EqualTo("2cm"));
            Assert.That(reread.TextBlocks.Single(item => item.Text == "Text").BreakBefore, Is.EqualTo("even-page"));
            Assert.That(reread.TextBreaks.Any(item => item.Kind == OdfTextBreakKind.EvenPageBefore), Is.True);
        });
    }

    [Test]
    public void RejectsUnsafeFrameChainsAndAmbiguousNames()
    {
        OdfDocument cycle = ReadFlat(
            "<office:automatic-styles/>",
            "<draw:frame draw:name='A' draw:chain-next-name='B'/>"
            + "<draw:frame draw:name='B' draw:chain-next-name='A'/>"
            + "<draw:frame draw:name='Broken' draw:chain-next-name='Missing'/>"
            + "<draw:frame draw:name='Duplicate'/><draw:frame draw:name='Duplicate'/>"
        );
        OdfTextFrame a = TextFlowNavigator.FindFrame(cycle, "A")!;
        OdfTextFrame broken = TextFlowNavigator.FindFrame(
            cycle,
            "Broken")!;

        Assert.Multiple(() =>
        {
            Assert.That(a.HasChainCycle, Is.True);
            Assert.That(TextFlowNavigator.FollowFrameChain(a), Is.Empty);
            Assert.That(broken.HasBrokenChain, Is.True);
            Assert.That(TextFlowNavigator.FollowFrameChain(broken), Is.Empty);
            Assert.That(TextFlowNavigator.FindFrame(cycle, "Duplicate"), Is.Null);
            Assert.That(
                () => TextFlowNavigator.RenameFrame(cycle, a, "Duplicate"),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void StyleRenameUpdatesTypedSectionAndFrameReferences()
    {
        OdfDocument document = ReadFlat(
            "<office:automatic-styles>"
            + "<style:style style:name='Sections' style:family='section'/>"
            + "<style:style style:name='Graphics' style:family='graphic'/>"
            + "<style:style style:name='FrameText' style:family='paragraph'/>"
            + "</office:automatic-styles>",
            "<text:section text:name='S' text:style-name='Sections'>"
            + "<draw:frame draw:name='F' draw:style-name='Graphics' "
            + "draw:text-style-name='FrameText'/></text:section>"
        );
        IOdfStyleEditor editor = new OdfStyleEditor();

        editor.RenameStyle(
            document,
            editor.FindStyle(document, "Sections", OdfStyleFamily.Section)!,
            "RenamedSections");
        editor.RenameStyle(
            document,
            editor.FindStyle(document, "Graphics", OdfStyleFamily.Graphic)!,
            "RenamedGraphics");
        editor.RenameStyle(
            document,
            editor.FindStyle(document, "FrameText", OdfStyleFamily.Paragraph)!,
            "RenamedFrameText");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextSections[0].StyleName, Is.EqualTo("RenamedSections"));
            Assert.That(reread.TextFrames[0].StyleName, Is.EqualTo("RenamedGraphics"));
            Assert.That(reread.TextFrames[0].TextStyleName, Is.EqualTo("RenamedFrameText"));
        });
    }

    private static OdfDocument ReadFlat(string styles, string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ooow='urn:example:condition'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-flow.fodt",
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
            "text-flow.fodt",
            new OdfLoadOptions());
    }
}
