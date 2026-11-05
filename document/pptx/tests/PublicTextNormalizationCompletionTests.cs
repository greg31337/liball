using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PublicTextNormalizationCompletionTests
{
    [Test]
    public void PublicTextAuthoring_NormalizesUnicodeAndRemovesInvalidXmlCharactersAcrossContainers()
    {
        PptxDocument document = new PptxDocumentFactory().CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();
        PptxShapeOperations shapes = new PptxShapeOperations();
        PptxTextFrameOperations frames = new PptxTextFrameOperations();
        PptxParagraphOperations paragraphs = new PptxParagraphOperations();
        PptxRunOperations runs = new PptxRunOperations();
        PptxTableOperations tables = new PptxTableOperations();
        PptxTableCellOperations cells = new PptxTableCellOperations();
        PptxCommentOperations comments = new PptxCommentOperations();
        PptxSlide slide = presentations.GetSlide(presentation, 0);
        PptxShape textBox = slides.AddTextBox(slide, "Cafe\u0301\u0001\tOne\r\nSecond", new PptxBounds(0L, 0L, 4000000L, 1000000L));
        int textBoxId = textBox.Id;
        PptxTextFrame frame = shapes.GetTextFrame(textBox);
        PptxParagraph firstParagraph = frames.GetParagraph(frame, 0);
        PptxRun addedRun = paragraphs.AddRun(firstParagraph, " A\u030A\u0002");
        runs.SetText(addedRun, " O\u0308\u0004");
        PptxTable table = slides.AddTable(slide, 1, 1, new PptxBounds(0L, 1200000L, 3000000L, 1000000L));
        cells.SetText(tables.GetCell(table, 0, 0), "Cell n\u0303\u0006");
        presentations.SetSlideNotesText(presentation, 0, "Note e\u0301\u0005");
        PptxCommentAuthor author = presentations.AddCommentAuthor(presentation, "Author", "A");
        PptxComment comment = presentations.AddSlideComment(presentation, slide, author.Id, "Comment a\u030A\u0007");
        comments.SetText(comment, "Review o\u0308\u0008");

        PptxDocument reopenedDocument = new PptxDocumentFactory().OpenBytes(document.SaveBytes());
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        PptxSlide reopenedSlide = presentations.GetSlide(reopened, 0);
        PptxShape? reopenedTextBox = new PptxShapeTreeOperations().FindById(slides.GetShapeTree(reopenedSlide), textBoxId);

        Assert.That(reopenedTextBox, Is.Not.Null);
        Assert.That(shapes.GetText(reopenedTextBox!), Is.EqualTo("Café\tOne Ö\nSecond"));
        Assert.That(tables.GetText(slides.GetTable(reopenedSlide, 0), 0, 0), Is.EqualTo("Cell ñ"));
        Assert.That(presentations.GetSlideNotesText(reopened, 0), Is.EqualTo("Note é"));
        Assert.That(presentations.GetSlideComments(reopened, reopenedSlide), Has.Count.EqualTo(1));
        Assert.That(presentations.GetSlideComments(reopened, reopenedSlide)[0].Text, Is.EqualTo("Review ö"));
        Assert.That(presentations.ExtractText(reopened), Does.Contain("Café\tOne Ö"));
        Assert.That(presentations.ExtractText(reopened), Does.Contain("Cell ñ"));
        Assert.That(presentations.ExtractText(reopened), Does.Contain("Note é"));
        Assert.That(presentations.ExportOutline(reopened), Does.Contain("Café\tOne Ö"));
        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
    }

    [Test]
    public void ParagraphAndRunReplacement_NormalizesValuesAndTreatsEmptySearchAsNoOp()
    {
        PptxDocument document = new PptxDocumentFactory().CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();
        PptxShapeOperations shapes = new PptxShapeOperations();
        PptxTextFrameOperations frames = new PptxTextFrameOperations();
        PptxParagraphOperations paragraphs = new PptxParagraphOperations();
        PptxRunOperations runs = new PptxRunOperations();
        PptxShape textBox = slides.AddTextBox(presentations.GetSlide(presentation, 0), "Cafe\u0301", new PptxBounds(0L, 0L, 3000000L, 1000000L));
        PptxTextFrame frame = shapes.GetTextFrame(textBox);
        PptxParagraph paragraph = frames.GetParagraph(frame, 0);

        Assert.That(paragraphs.ReplaceText(paragraph, "", "ignored"), Is.EqualTo(0));
        Assert.That(paragraphs.GetText(paragraph), Is.EqualTo("Café"));
        Assert.That(paragraphs.ReplaceText(paragraph, "Cafe\u0301", "Re\u0301sume\u0301\u0003"), Is.EqualTo(1));

        paragraph = frames.GetParagraph(frame, 0);
        PptxRun run = paragraphs.GetRun(paragraph, 0);
        Assert.That(runs.ReplaceText(run, "Re\u0301", "De\u0301\u0004"), Is.EqualTo(1));
        Assert.That(frames.GetText(frame), Is.EqualTo("Désumé"));
        Assert.That(runs.ReplaceText(run, "", "ignored"), Is.EqualTo(0));

        PptxDocument reopened = new PptxDocumentFactory().OpenBytes(document.SaveBytes());
        Assert.That(presentations.ExtractText(reopened.LoadPresentation()), Does.Contain("Désumé"));
        Assert.That(reopened.Validate().Succeeded(), Is.True, reopened.Validate().FormatDiagnostics());
    }
}
