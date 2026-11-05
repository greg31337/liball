using System.Text;

namespace odf.Tests;

public class TextEditingModelTests
{
    [Test]
    public void InsertsAndDeletesTextWhileTrackingEveryCursor()
    {
        OdfDocument document = ReadFlat("<text:p>Alpha</text:p>");
        OdfTextBlock block = document.TextBlocks[0];
        OdfTextCursor editor = TextEditingModel.CreateCursor(document, block, 2);
        OdfTextCursor observer = TextEditingModel.CreateCursor(document, block, 4);

        TextEditingModel.InsertText(document, editor, "XX");
        TextEditingModel.DeleteForward(document, editor, 2);

        Assert.Multiple(() =>
        {
            Assert.That(block.Text, Is.EqualTo("AlXXa"));
            Assert.That(editor.Offset, Is.EqualTo(4));
            Assert.That(observer.Offset, Is.EqualTo(4));
            Assert.That(editor.HasSelection, Is.False);
            Assert.That(document.TextCursors, Has.Length.EqualTo(2));
            Assert.That(WriteAndRead(document).TextBlocks[0].Text, Is.EqualTo("AlXXa"));
        });
    }

    [Test]
    public void DeletesASelectionAcrossBlocksAndRetargetsCursors()
    {
        OdfDocument document = ReadFlat(
            "<text:p>One</text:p>"
            + "<text:p>Two</text:p>"
            + "<text:p>Three</text:p>");
        OdfTextCursor selection = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[2],
            2);
        TextEditingModel.SetSelection(
            document,
            selection,
            document.TextBlocks[0],
            1);
        OdfTextCursor middle = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[1],
            1);
        OdfTextCursor trailing = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[2],
            5);

        TextEditingModel.DeleteSelectedText(document, selection);

        Assert.Multiple(() =>
        {
            Assert.That(document.TextBlocks, Has.Length.EqualTo(1));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("Oree"));
            Assert.That(selection.Block, Is.SameAs(document.TextBlocks[0]));
            Assert.That(selection.Offset, Is.EqualTo(1));
            Assert.That(selection.HasSelection, Is.False);
            Assert.That(middle.Block, Is.SameAs(document.TextBlocks[0]));
            Assert.That(middle.Offset, Is.EqualTo(1));
            Assert.That(trailing.Block, Is.SameAs(document.TextBlocks[0]));
            Assert.That(trailing.Offset, Is.EqualTo(4));
            Assert.That(WriteAndRead(document).TextBlocks[0].Text, Is.EqualTo("Oree"));
        });
    }

    [Test]
    public void SplitsAndMergesParagraphsWithTrackedPositions()
    {
        OdfDocument document = ReadFlat(
            "<text:p text:style-name='Body'>Hello</text:p>");
        OdfTextBlock original = document.TextBlocks[0];
        OdfTextCursor editor = TextEditingModel.CreateCursor(document, original, 2);
        OdfTextCursor observer = TextEditingModel.CreateCursor(document, original, 4);

        OdfTextBlock created = TextEditingModel.SplitBlock(document, editor);

        Assert.Multiple(() =>
        {
            Assert.That(document.TextBlocks, Has.Length.EqualTo(2));
            Assert.That(original.Text, Is.EqualTo("He"));
            Assert.That(created.Text, Is.EqualTo("llo"));
            Assert.That(created.StyleName, Is.EqualTo("Body"));
            Assert.That(editor.Block, Is.SameAs(created));
            Assert.That(editor.Offset, Is.Zero);
            Assert.That(observer.Block, Is.SameAs(created));
            Assert.That(observer.Offset, Is.EqualTo(2));
        });

        TextEditingModel.MergeNextBlock(document, original);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.TextBlocks, Has.Length.EqualTo(1));
            Assert.That(original.Text, Is.EqualTo("Hello"));
            Assert.That(editor.Block, Is.SameAs(original));
            Assert.That(editor.Offset, Is.EqualTo(2));
            Assert.That(observer.Block, Is.SameAs(original));
            Assert.That(observer.Offset, Is.EqualTo(4));
            Assert.That(reread.TextBlocks, Has.Length.EqualTo(1));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Hello"));
            Assert.That(reread.TextBlocks[0].StyleName, Is.EqualTo("Body"));
        });
    }

    [Test]
    public void CopiesAndMovesWholeFormattedBlocks()
    {
        OdfDocument document = ReadFlat(
            "<text:p><text:span text:style-name='Strong'>A</text:span></text:p>"
            + "<text:p>B</text:p>"
            + "<text:p>C</text:p>");
        OdfTextBlock first = document.TextBlocks[0];
        OdfTextBlock second = document.TextBlocks[1];
        OdfTextBlock third = document.TextBlocks[2];

        OdfTextBlock copy = TextEditingModel.CopyBlock(
            document,
            first,
            second,
            false);
        TextEditingModel.MoveBlock(document, third, first, true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                document.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "C", "A", "B", "A" }));
            Assert.That(copy, Is.Not.SameAs(first));
            Assert.That(copy.Node, Is.Not.SameAs(first.Node));
            Assert.That(copy.Runs[0].StyleName, Is.EqualTo("Strong"));
            Assert.That(
                reread.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "C", "A", "B", "A" }));
            Assert.That(reread.TextBlocks[1].Runs[0].StyleName, Is.EqualTo("Strong"));
            Assert.That(reread.TextBlocks[3].Runs[0].StyleName, Is.EqualTo("Strong"));
        });
    }

    [Test]
    public void AppliesParagraphAndInlineStylesToCursorRanges()
    {
        OdfDocument document = ReadFlat(
            "<text:p>abcdef</text:p><text:p>second</text:p>");
        OdfTextCursor cursor = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[0],
            5);
        TextEditingModel.SetSelection(
            document,
            cursor,
            document.TextBlocks[0],
            1);

        TextEditingModel.ApplyInlineStyle(document, cursor, "Emphasis");
        TextEditingModel.SetSelection(
            document,
            cursor,
            document.TextBlocks[1],
            2);
        TextEditingModel.ApplyParagraphStyle(document, cursor, "Selected");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextBlocks[0].StyleName, Is.EqualTo("Selected"));
            Assert.That(reread.TextBlocks[1].StyleName, Is.EqualTo("Selected"));
            Assert.That(reread.TextBlocks[0].Runs, Has.Length.EqualTo(3));
            Assert.That(reread.TextBlocks[0].Runs[0].Text, Is.EqualTo("a"));
            Assert.That(reread.TextBlocks[0].Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(reread.TextBlocks[0].Runs[1].StyleName, Is.EqualTo("Emphasis"));
            Assert.That(reread.TextBlocks[0].Runs[1].Text, Is.EqualTo("bcde"));
            Assert.That(reread.TextBlocks[0].Runs[2].Text, Is.EqualTo("f"));
        });
    }

    private static OdfDocument ReadFlat(string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles/>"
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-editing.fodt",
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
            "text-editing.fodt",
            new OdfLoadOptions());
    }
}
