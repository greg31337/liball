using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxContentFactoryTests
{
    [Test]
    public void Factory_CreatesTextRunsParagraphsBodiesAndTablesWithoutRawXml()
    {
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();

        global::docx.DocxText text = factory.CreateText(" spaced ");
        global::docx.DocxRun emptyRun = factory.CreateRun();
        global::docx.DocxRun textRun = factory.CreateTextRun("Run");
        global::docx.DocxParagraph emptyParagraph = factory.CreateParagraph();
        global::docx.DocxParagraph textParagraph = factory.CreateTextParagraph("Paragraph");
        global::docx.DocxBody body = factory.CreateBody();
        global::docx.DocxTable table = factory.CreateTable(2, 3);

        Assert.That(text.Value, Is.EqualTo(" spaced "));
        Assert.That(text.PreserveSpace, Is.True);
        Assert.That(emptyRun.ChildCount(), Is.EqualTo(0));
        Assert.That(textRun.GetText(), Is.EqualTo("Run"));
        Assert.That(textRun.TextCount(), Is.EqualTo(1));
        Assert.That(emptyParagraph.ChildCount(), Is.EqualTo(0));
        Assert.That(textParagraph.GetText(), Is.EqualTo("Paragraph"));
        Assert.That(textParagraph.RunCount(), Is.EqualTo(1));
        Assert.That(body.BlockCount(), Is.EqualTo(0));
        Assert.That(table.RowCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).CellCount(), Is.EqualTo(3));
        Assert.That(table.GetRow(1).CellCount(), Is.EqualTo(3));
    }

    [Test]
    public void Factory_CreatesTypedBodyBlocksThatComposeAndRoundTrip()
    {
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxBody body = factory.CreateBody();
        global::docx.DocxParagraph paragraph = factory.CreateTextParagraph("Paragraph");
        global::docx.DocxTable table = factory.CreateTable(1, 1);
        global::docx.DocxContentControl contentControl = factory.CreateBlockContentControl("Controlled");

        body.AddBlock(factory.CreateParagraphBlock(paragraph));
        body.AddBlock(factory.CreateTableBlock(table));
        body.AddBlock(factory.CreateContentControlBlock(contentControl));

        Assert.That(body.BlockCount(), Is.EqualTo(3));
        Assert.That(body.GetBlock(0).GetParagraph(), Is.SameAs(paragraph));
        Assert.That(body.GetBlock(1).GetTable(), Is.SameAs(table));
        Assert.That(body.GetBlock(2).GetContentControl(), Is.SameAs(contentControl));

        global::docx.DocxBody reparsed = new global::docx.DocxBody(body.WriteXml());

        Assert.That(reparsed.BlockCount(), Is.EqualTo(3));
        Assert.That(reparsed.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(reparsed.GetBlock(1).IsTable(), Is.True);
        Assert.That(reparsed.GetBlock(2).IsContentControl(), Is.True);
        Assert.That(reparsed.GetText(), Does.Contain("Paragraph"));
        Assert.That(reparsed.GetText(), Does.Contain("Controlled"));
    }

    [Test]
    public void Factory_CreatesHyperlinksBookmarksFieldsAndContentControls()
    {
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxHyperlink anchor = factory.CreateAnchorHyperlink("target", "Anchor");
        global::docx.DocxHyperlink relationship = factory.CreateRelationshipHyperlink("rId7", "External");
        global::docx.DocxBookmark start = factory.CreateBookmarkStart("4", "target");
        global::docx.DocxBookmark end = factory.CreateBookmarkEnd("4");
        global::docx.DocxField field = factory.CreateSimpleField("DATE", "Result");
        global::docx.DocxContentControl blockControl = factory.CreateBlockContentControl("Block");
        global::docx.DocxContentControl inlineControl = factory.CreateInlineContentControl("Inline");
        global::docx.DocxParagraph paragraph = factory.CreateParagraph();

        paragraph.InsertBookmark(0, start);
        paragraph.InsertHyperlink(1, anchor);
        paragraph.InsertField(2, field);
        paragraph.InsertContentControl(3, inlineControl);
        paragraph.InsertHyperlink(4, relationship);
        paragraph.InsertBookmark(5, end);

        Assert.That(anchor.GetText(), Is.EqualTo("Anchor"));
        Assert.That(relationship.GetText(), Is.EqualTo("External"));
        Assert.That(start.Kind, Is.EqualTo(global::docx.DocxBookmark.KindStart));
        Assert.That(end.Kind, Is.EqualTo(global::docx.DocxBookmark.KindEnd));
        Assert.That(field.GetText(), Is.EqualTo("Result"));
        Assert.That(blockControl.GetText(), Is.EqualTo("Block"));
        Assert.That(blockControl.BlockCount(), Is.EqualTo(1));
        Assert.That(blockControl.RunCount(), Is.EqualTo(0));
        Assert.That(inlineControl.GetText(), Is.EqualTo("Inline"));
        Assert.That(inlineControl.BlockCount(), Is.EqualTo(0));
        Assert.That(inlineControl.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("AnchorResultInlineExternal"));

        global::docx.DocxParagraph reparsed = new global::docx.DocxParagraph(paragraph.WriteXml());

        Assert.That(reparsed.BookmarkCount(), Is.EqualTo(2));
        Assert.That(reparsed.HyperlinkCount(), Is.EqualTo(2));
        Assert.That(reparsed.FieldCount(), Is.EqualTo(1));
        Assert.That(reparsed.ContentControlCount(), Is.EqualTo(1));
        Assert.That(reparsed.GetText(), Is.EqualTo("AnchorResultInlineExternal"));
    }

    [Test]
    public void Factory_CreatesEverySupportedRunControlKind()
    {
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxRun run = factory.CreateRun();

        run.InsertBreak(0, factory.CreateTab());
        run.InsertBreak(1, factory.CreateCarriageReturn());
        run.InsertBreak(2, factory.CreateLineBreak());
        run.InsertBreak(3, factory.CreatePageBreak());
        run.InsertBreak(4, factory.CreateColumnBreak());
        run.InsertBreak(5, factory.CreateSoftHyphen());
        run.InsertBreak(6, factory.CreateNoBreakHyphen());
        run.InsertBreak(7, factory.CreateSymbol("Wingdings", "F0FC"));

        Assert.That(run.TabCount(), Is.EqualTo(1));
        Assert.That(run.CarriageReturnCount(), Is.EqualTo(1));
        Assert.That(run.BreakElementCount(), Is.EqualTo(3));
        Assert.That(run.HyphenCount(), Is.EqualTo(2));
        Assert.That(run.SymbolCount(), Is.EqualTo(1));
        Assert.That(run.GetBreak(0).Kind, Is.EqualTo(global::docx.DocxBreak.KindTab));
        Assert.That(run.GetBreak(1).Kind, Is.EqualTo(global::docx.DocxBreak.KindCarriageReturn));
        Assert.That(run.GetBreak(2).Kind, Is.EqualTo(global::docx.DocxBreak.KindLineBreak));
        Assert.That(run.GetBreak(3).Kind, Is.EqualTo(global::docx.DocxBreak.KindPageBreak));
        Assert.That(run.GetBreak(4).Kind, Is.EqualTo(global::docx.DocxBreak.KindColumnBreak));
        Assert.That(run.GetBreak(5).Kind, Is.EqualTo(global::docx.DocxBreak.KindSoftHyphen));
        Assert.That(run.GetBreak(6).Kind, Is.EqualTo(global::docx.DocxBreak.KindNoBreakHyphen));
        Assert.That(run.GetBreak(7).Kind, Is.EqualTo(global::docx.DocxBreak.KindSymbol));

        global::docx.DocxRun reparsed = new global::docx.DocxRun(run.WriteXml());

        Assert.That(reparsed.BreakCount(), Is.EqualTo(8));
        Assert.That(reparsed.GetSymbol(0).Xml, Does.Contain("F0FC"));
    }

    [Test]
    public void Factory_RejectsInvalidConstructionArguments()
    {
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();

        Assert.Throws<ArgumentNullException>(() => factory.CreateText(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateTextRun(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateTextParagraph(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateTable(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateTable(1, -1));
        Assert.Throws<ArgumentNullException>(() => factory.CreateParagraphBlock(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateContentControlBlock(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateAnchorHyperlink(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateAnchorHyperlink("anchor", null!));
        Assert.Throws<ArgumentException>(() => factory.CreateAnchorHyperlink("", "text"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateRelationshipHyperlink(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateRelationshipHyperlink("rId1", null!));
        Assert.Throws<ArgumentException>(() => factory.CreateRelationshipHyperlink("", "text"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateBookmarkStart(null!, "name"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateBookmarkStart("1", null!));
        Assert.Throws<ArgumentException>(() => factory.CreateBookmarkStart("", "name"));
        Assert.Throws<ArgumentException>(() => factory.CreateBookmarkStart("1", ""));
        Assert.Throws<ArgumentNullException>(() => factory.CreateBookmarkEnd(null!));
        Assert.Throws<ArgumentException>(() => factory.CreateBookmarkEnd(""));
        Assert.Throws<ArgumentNullException>(() => factory.CreateSimpleField(null!, "result"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateSimpleField("DATE", null!));
        Assert.Throws<ArgumentException>(() => factory.CreateSimpleField("", "result"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateBlockContentControl(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateInlineContentControl(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateSymbol(null!, "F0FC"));
        Assert.Throws<ArgumentNullException>(() => factory.CreateSymbol("Wingdings", null!));
    }

    [Test]
    public void GenerationManagement_CreatesCommonContentAndRoundTripsIt()
    {
        global::docx.DocxGenerationManagement generation = new global::docx.DocxGenerationManagement();
        global::docx.DocxDocument document = generation.CreateMinimalDocument();
        global::docx.DocxParagraph heading = generation.AppendHeading(document, "Generated document", 2);
        global::docx.DocxParagraph paragraph = generation.AppendParagraph(document, "Plain");
        global::docx.DocxRun run = generation.AppendRun(document, paragraph, " run");
        global::docx.DocxRunFormat runFormat = new global::docx.DocxRunFormat();
        runFormat.HasBold = true;
        runFormat.Bold = true;
        global::docx.DocxRun formattedRun = generation.AppendFormattedText(document, paragraph, " bold", runFormat);
        string bookmarkId = generation.InsertBookmark(document, paragraph, 0, 1, "destination");
        global::docx.DocxHyperlink externalLink = generation.AppendExternalHyperlink(document, paragraph, "https://example.com", " external");
        global::docx.DocxHyperlink internalLink = generation.AppendInternalHyperlink(document, paragraph, "destination", " internal");
        generation.AppendLineBreak(document, formattedRun);
        generation.AppendPageBreak(document, formattedRun);

        global::docx.DocxTable table = generation.AppendTable(document, 2, 2);
        generation.AppendTableCellText(document, table, 0, 0, "A1");
        generation.AppendTableCellText(document, table, 1, 1, "B2");
        byte[] pngBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        global::docx.DocxDrawing drawing = generation.AppendInlineImage(document, paragraph, pngBytes, global::docx.DocxConstants.ContentTypePng, "generated.png", "Generated image", 914400, 914400);

        global::docx.DocxSectionFormat sectionFormat = new global::docx.DocxSectionFormat();
        sectionFormat.PageWidth = global::docx.DocxSectionFormat.PageSizeLetterWidth;
        sectionFormat.PageHeight = global::docx.DocxSectionFormat.PageSizeLetterHeight;
        global::docx.DocxSection section = generation.AppendSection(document, sectionFormat);
        global::docx.DocxHeader header = generation.CreateTextHeader(document, section, global::docx.DocxHeaderFooterKind.Default, "Generated header");
        global::docx.DocxFooter footer = generation.CreateTextFooter(document, section, global::docx.DocxHeaderFooterKind.Default, "Generated footer");
        global::docx.DocxFootnote footnote = generation.InsertFootnote(document, paragraph, paragraph.ChildCount(), "Footnote text");
        global::docx.DocxEndnote endnote = generation.InsertEndnote(document, paragraph, paragraph.ChildCount(), "Endnote text");
        global::docx.DocxComment comment = generation.InsertComment(document, paragraph, 1, 2, "Reviewer", "RV", "Comment text");
        global::docx.DocxStyle style = generation.CreateParagraphStyle(document, "GeneratedStyle", "Generated Style");
        global::docx.DocxNumberingInstance numbering = generation.CreateNumberingDefinition(document, "decimal", "%1.");
        generation.ApplyNumberingDefinition(document, paragraph, numbering, "0");

        Assert.That(heading.GetFormat().StyleId, Is.EqualTo("Heading2"));
        Assert.That(paragraph.GetText(), Is.EqualTo("Plain run bold\n\n external internal"));
        Assert.That(run.GetText(), Is.EqualTo(" run"));
        Assert.That(formattedRun.GetFormat().Bold, Is.True);
        Assert.That(formattedRun.BreakElementCount(), Is.EqualTo(2));
        Assert.That(bookmarkId, Is.Not.Empty);
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(2));
        Assert.That(externalLink.RelationshipId, Is.Not.Empty);
        Assert.That(internalLink.Anchor, Is.EqualTo("destination"));
        Assert.That(table.GetText(), Does.Contain("A1"));
        Assert.That(table.GetText(), Does.Contain("B2"));
        Assert.That(drawing.Description, Is.EqualTo("Generated image"));
        Assert.That(header.GetText(), Is.EqualTo("Generated header"));
        Assert.That(footer.GetText(), Is.EqualTo("Generated footer"));
        Assert.That(footnote.GetText(), Does.Contain("Footnote text"));
        Assert.That(endnote.GetText(), Does.Contain("Endnote text"));
        Assert.That(comment.GetText(), Does.Contain("Comment text"));
        Assert.That(style.StyleId, Is.EqualTo("GeneratedStyle"));
        Assert.That(paragraph.GetFormat().NumberingId, Is.EqualTo(numbering.NumberingId));

        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        global::docx.DocxDocument reopened = persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
        global::docx.DocxParagraph reopenedHeading = reopened.Body.GetBlock(0).GetParagraph()!;
        global::docx.DocxParagraph reopenedParagraph = reopened.Body.GetBlock(1).GetParagraph()!;
        global::docx.DocxTable reopenedTable = reopened.Body.GetBlock(2).GetTable()!;
        global::docx.DocxStyleManagement styles = new global::docx.DocxStyleManagement();
        global::docx.DocxNumberingManagement numberings = new global::docx.DocxNumberingManagement();
        global::docx.DocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.DocxCommentManagement comments = new global::docx.DocxCommentManagement();
        global::docx.DocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.DocxLinkBookmarkFieldManagement links = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();
        global::docx.DocxRun reopenedFormattedRun = reopenedParagraph.GetRuns().Single(value => value.GetText().StartsWith(" bold"));
        global::docx.DocxHyperlink reopenedExternalLink = reopenedParagraph.GetHyperlinks().Single(value => value.RelationshipId != "");
        global::docx.DocxHyperlink reopenedInternalLink = reopenedParagraph.GetHyperlinks().Single(value => value.Anchor != "");

        Assert.That(reopenedHeading.GetFormat().StyleId, Is.EqualTo("Heading2"));
        Assert.That(reopenedParagraph.GetText(), Is.EqualTo("Plain run bold\n\n external internal"));
        Assert.That(reopenedParagraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(reopenedParagraph.HyperlinkCount(), Is.EqualTo(2));
        Assert.That(reopenedFormattedRun.GetFormat().Bold, Is.True);
        Assert.That(reopenedFormattedRun.GetBreak(0).Kind, Is.EqualTo(global::docx.DocxBreak.KindLineBreak));
        Assert.That(reopenedFormattedRun.GetBreak(1).Kind, Is.EqualTo(global::docx.DocxBreak.KindPageBreak));
        Assert.That(links.ResolveMainDocumentHyperlink(reopened, reopenedExternalLink), Is.True);
        Assert.That(reopenedExternalLink.Target, Is.EqualTo("https://example.com"));
        Assert.That(reopenedInternalLink.Anchor, Is.EqualTo("destination"));
        Assert.That(reopenedTable.GetText(), Does.Contain("A1"));
        Assert.That(reopened.GetSections(), Has.Count.EqualTo(1));
        Assert.That(reopened.GetHeaders()[0].GetText(), Is.EqualTo("Generated header"));
        Assert.That(reopened.GetFooters()[0].GetText(), Is.EqualTo("Generated footer"));
        Assert.That(media.GetImages(reopened), Has.Count.EqualTo(1));
        Assert.That(reopenedParagraph.GetRuns().SelectMany(value => value.GetDrawings()).Single().Description, Is.EqualTo("Generated image"));
        Assert.That(styles.FindStyleById(reopened.GetStyles(), "Heading2"), Is.Not.Null);
        Assert.That(styles.FindStyleById(reopened.GetStyles(), "GeneratedStyle"), Is.Not.Null);
        Assert.That(numberings.FindNumberingInstance(reopened.GetNumbering(), numbering.NumberingId), Is.Not.Null);
        Assert.That(notes.FindFootnote(reopened, footnote.Id)!.GetText(), Does.Contain("Footnote text"));
        Assert.That(notes.FindEndnote(reopened, endnote.Id)!.GetText(), Does.Contain("Endnote text"));
        Assert.That(comments.FindComment(reopened, comment.Id)!.GetText(), Does.Contain("Comment text"));
        Assert.That(validator.ValidatePackage(reopened.Package).Errors, Is.Empty);
    }

    [Test]
    public void GenerationManagement_RunControlsRemainAttachedAfterSiblingEditsAndRoundTrip()
    {
        global::docx.DocxGenerationManagement generation = new global::docx.DocxGenerationManagement();
        global::docx.DocxDocument document = generation.CreateMinimalDocument();
        global::docx.DocxParagraph paragraph = generation.AppendParagraph(document, "Start");
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();
        format.HasBold = true;
        format.Bold = true;
        global::docx.DocxRun run = generation.AppendFormattedText(document, paragraph, " controlled", format);
        generation.AppendInternalHyperlink(document, paragraph, "target", " sibling");
        format.HasItalic = true;
        format.Italic = true;
        run.SetFormat(format);
        generation.AppendLineBreak(document, run);
        generation.AppendPageBreak(document, run);
        global::docx.DocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        System.IO.MemoryStream stream = new System.IO.MemoryStream();

        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        global::docx.DocxDocument reopened = persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
        global::docx.DocxRun reopenedRun = reopened.Body.GetParagraph(0).GetRuns().Single(value => value.GetText().StartsWith(" controlled"));

        Assert.That(reopenedRun.GetFormat().Bold, Is.True);
        Assert.That(reopenedRun.GetFormat().Italic, Is.True);
        Assert.That(reopenedRun.BreakCount(), Is.EqualTo(2));
        Assert.That(reopenedRun.GetBreak(0).Kind, Is.EqualTo(global::docx.DocxBreak.KindLineBreak));
        Assert.That(reopenedRun.GetBreak(1).Kind, Is.EqualTo(global::docx.DocxBreak.KindPageBreak));
    }

    [Test]
    public void GenerationManagement_RejectsHeadingLevelsOutsideWordRange()
    {
        global::docx.DocxGenerationManagement generation = new global::docx.DocxGenerationManagement();
        global::docx.DocxDocument document = generation.CreateMinimalDocument();

        Assert.Throws<ArgumentOutOfRangeException>(() => generation.AppendHeading(document, "Heading", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => generation.AppendHeading(document, "Heading", 10));
        Assert.That(document.Body.BlockCount(), Is.EqualTo(0));
    }
}
