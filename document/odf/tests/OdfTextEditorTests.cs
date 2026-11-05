using System.Text;

namespace odf.Tests;

public class OdfTextEditorTests
{
    [Test]
    public void CreatesParagraphHeadingSpanAndHyperlinkThroughPublicApi()
    {
        OdfDocument document = ReadFlat("", "<text:p>Existing</text:p>");
        IOdfTextEditor editor = new OdfTextEditor();

        OdfTextBlock paragraph = editor.AppendParagraph(
            document,
            "Body",
            "BodyStyle");
        OdfTextRun span = editor.AppendSpan(
            document,
            paragraph,
            " styled",
            "Strong");
        OdfTextBlock heading = editor.AppendHeading(
            document,
            "Title",
            2,
            "HeadingStyle");
        OdfTextRun link = editor.AppendHyperlink(
            document,
            heading,
            " link",
            "https://example.test/",
            "LinkStyle");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(span.Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(link.Kind, Is.EqualTo(OdfTextRunKind.Hyperlink));
            Assert.That(editor.ExtractPlainText(document), Is.EqualTo(
                "Existing\nBody styled\nTitle link"));
            Assert.That(reread.TextBlocks, Has.Length.EqualTo(3));
            Assert.That(reread.TextBlocks[1].Text, Is.EqualTo("Body styled"));
            Assert.That(reread.TextBlocks[1].StyleName, Is.EqualTo("BodyStyle"));
            Assert.That(reread.TextBlocks[1].Runs[1].StyleName, Is.EqualTo("Strong"));
            Assert.That(reread.TextBlocks[2].IsHeading, Is.True);
            Assert.That(reread.TextBlocks[2].OutlineLevel, Is.EqualTo(2));
            Assert.That(reread.TextBlocks[2].Runs[1].Href, Is.EqualTo(
                "https://example.test/"));
        });
    }

    [Test]
    public void ReturnsIndependentSnapshotsForEveryTypedTextCollection()
    {
        OdfDocument document = ReadFlat("", "<text:p>Text</text:p>");
        OdfMasterPage master = new OdfMasterPage();
        master.TextContents = [new OdfMasterPageContent()];
        document.MasterPages = [master];
        document.TextRanges = [new OdfTextRange()];
        document.TextSections = [new OdfTextSection()];
        document.TextFrames = [new OdfTextFrame()];
        document.TextBreaks = [new OdfTextBreak()];
        document.TextLists = [new OdfTextList()];
        document.NumberedParagraphs = [new OdfNumberedParagraph()];
        document.TextFields = [new OdfTextField()];
        document.TextFieldDeclarations = [new OdfTextFieldDeclaration()];
        document.Notes = [new OdfNote()];
        document.NoteConfigurations = [new OdfNoteConfiguration()];
        document.TextIndexes = [new OdfTextIndex()];
        document.BibliographyConfigurations = [new OdfBibliographyConfiguration()];
        document.BibliographyEntries = [new OdfBibliographyEntry()];
        document.PageSequences = [new OdfPageSequence()];
        document.TitlePagePatterns = [new OdfTitlePagePattern()];
        document.MasterSubdocuments = [new OdfMasterSubdocument()];
        IOdfTextEditor editor = new OdfTextEditor();

        Assert.Multiple(() =>
        {
            AssertSnapshot(editor.GetTextBlocks(document), document.TextBlocks);
            AssertSnapshot(editor.GetTextRanges(document), document.TextRanges);
            AssertSnapshot(editor.GetTextSections(document), document.TextSections);
            AssertSnapshot(editor.GetTextFrames(document), document.TextFrames);
            AssertSnapshot(editor.GetTextBreaks(document), document.TextBreaks);
            AssertSnapshot(editor.GetTextLists(document), document.TextLists);
            AssertSnapshot(
                editor.GetNumberedParagraphs(document),
                document.NumberedParagraphs);
            AssertSnapshot(editor.GetTextFields(document), document.TextFields);
            AssertSnapshot(
                editor.GetTextFieldDeclarations(document),
                document.TextFieldDeclarations);
            AssertSnapshot(editor.GetNotes(document), document.Notes);
            AssertSnapshot(
                editor.GetNoteConfigurations(document),
                document.NoteConfigurations);
            AssertSnapshot(editor.GetTextIndexes(document), document.TextIndexes);
            AssertSnapshot(
                editor.GetBibliographyConfigurations(document),
                document.BibliographyConfigurations);
            AssertSnapshot(
                editor.GetBibliographyEntries(document),
                document.BibliographyEntries);
            AssertSnapshot(editor.GetPageSequences(document), document.PageSequences);
            AssertSnapshot(
                editor.GetTitlePagePatterns(document),
                document.TitlePagePatterns);
            AssertSnapshot(
                editor.GetMasterSubdocuments(document),
                document.MasterSubdocuments);
            AssertSnapshot(editor.GetMasterPageText(document, master), master.TextContents);
        });
    }

    [Test]
    public void NavigatesAndRenamesTypedTextReferencesThroughPublicApi()
    {
        OdfDocument document = ReadFlat(
            "<text:user-field-decls><text:user-field-decl text:name='OldField'/>"
            + "</text:user-field-decls>",
            "<text:section text:name='OldSection'><text:p>Section</text:p>"
            + "</text:section>"
            + "<text:list xml:id='OldList'><text:list-item><text:p>Item</text:p>"
            + "</text:list-item></text:list>"
            + "<text:p><text:bookmark text:name='OldRange'/>"
            + "<text:user-field-get text:name='OldField'>Value</text:user-field-get>"
            + "<text:note text:note-class='footnote' text:id='OldNote'>"
            + "<text:note-citation>1</text:note-citation>"
            + "<text:note-body><text:p>Note</text:p></text:note-body>"
            + "</text:note></text:p>");
        IOdfTextEditor editor = new OdfTextEditor();
        OdfTextRange range = editor.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "OldRange")!;
        OdfTextSection section = editor.FindTextSection(document, "OldSection")!;
        OdfTextList list = editor.FindTextList(document, "OldList")!;
        OdfTextFieldDeclaration declaration = editor.FindTextFieldDeclaration(
            document,
            OdfTextFieldDeclarationKind.UserField,
            "OldField")!;
        OdfNote note = editor.FindNote(document, "OldNote")!;

        Assert.That(editor.NavigateTextRange(document, range), Has.Length.EqualTo(1));

        editor.RenameTextRange(document, range, "NewRange");
        editor.RenameTextSection(document, section, "NewSection");
        editor.RenameTextList(document, list, "NewList");
        editor.RenameTextFieldDeclaration(document, declaration, "NewField");
        editor.RenameNote(document, note, "NewNote");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindTextRange(
                reread,
                OdfTextRangeKind.Bookmark,
                "NewRange"), Is.Not.Null);
            Assert.That(editor.FindTextSection(reread, "NewSection"), Is.Not.Null);
            Assert.That(editor.FindTextList(reread, "NewList"), Is.Not.Null);
            Assert.That(editor.FindTextFieldDeclaration(
                reread,
                OdfTextFieldDeclarationKind.UserField,
                "NewField"), Is.Not.Null);
            Assert.That(editor.FindNote(reread, "NewNote"), Is.Not.Null);
            Assert.That(editor.FindTextFields(
                reread,
                OdfTextFieldKind.UserField)[0].Name, Is.EqualTo("NewField"));
        });
    }

    [Test]
    public void EditsSearchesAndStylesTextThroughPublicApi()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p>Alpha beta</text:p><text:p>Gamma</text:p>");
        IOdfTextEditor editor = new OdfTextEditor();
        OdfTextCursor cursor = editor.CreateTextCursor(
            document,
            document.TextBlocks[0],
            5);

        editor.InsertText(document, cursor, "!");
        editor.SetTextSelection(document, cursor, document.TextBlocks[0], 0);
        editor.ApplyInlineStyle(document, cursor, "Emphasis");
        editor.ClearTextSelection(document, cursor);
        OdfSearchMatch[] matches = editor.SearchRegularExpressionText(
            document,
            "beta|Gamma",
            true);
        OdfEditResult replaced = editor.ReplaceLiteralText(
            document,
            "Gamma",
            "Delta",
            true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Length.EqualTo(2));
            Assert.That(editor.GetTextCursors(document), Is.EqualTo(
                new[] { cursor }));
            Assert.That(replaced.AffectedCount, Is.EqualTo(1));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Alpha! beta"));
            Assert.That(reread.TextBlocks[0].Runs[0].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(reread.TextBlocks[0].Runs[0].StyleName, Is.EqualTo("Emphasis"));
            Assert.That(reread.TextBlocks[1].Text, Is.EqualTo("Delta"));
        });
    }

    [Test]
    public void FillsTemplatesThroughPublicApi()
    {
        OdfDocument document = ReadFlat(
            "<text:user-field-decls><text:user-field-decl text:name='Company' "
            + "office:value-type='string' office:string-value='Old'/>"
            + "</text:user-field-decls>",
            "<text:p><text:placeholder text:description='Person'>Name</text:placeholder> "
            + "<text:user-field-get text:name='Company'>Old</text:user-field-get>"
            + "</text:p>");
        IOdfTextEditor editor = new OdfTextEditor();

        OdfEditResult placeholder = editor.FillPlaceholders(
            document,
            "Person",
            "Ada",
            false);
        OdfEditResult userField = editor.FillUserField(
            document,
            "Company",
            "Analytical Engines",
            true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(placeholder.AffectedCount, Is.EqualTo(1));
            Assert.That(userField.AffectedCount, Is.EqualTo(1));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo(
                "Ada Analytical Engines"));
            Assert.That(reread.TextFields, Has.Length.EqualTo(1));
            Assert.That(reread.TextFields[0].Kind, Is.EqualTo(
                OdfTextFieldKind.UserField));
            Assert.That(reread.TextFieldDeclarations[0].StringValue, Is.EqualTo(
                "Analytical Engines"));
        });
    }

    private static void AssertSnapshot<T>(T[] actual, T[] source)
    {
        Assert.That(actual, Is.Not.SameAs(source));
        Assert.That(actual, Is.EqualTo(source));
    }

    private static OdfDocument ReadFlat(string declarations, string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles/>"
            + "<office:body><office:text>"
            + declarations
            + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-editor.fodt",
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
            "text-editor.fodt",
            new OdfLoadOptions());
    }
}
