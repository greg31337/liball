using System.Text;

namespace odf.Tests;

public class OdfTextEditorContractTests
{
    [Test]
    public void CoversBlocksRunsRangesAndPlainTextThroughTheContract()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles/>",
            "",
            "<text:p>Before <text:bookmark-start text:name='Selection'/>inside"
            + "<text:bookmark-end text:name='Selection'/></text:p>");
        IOdfTextEditor editor = new OdfTextEditor();

        OdfTextBlock paragraph = editor.AppendParagraph(document, "Body", "BodyStyle");
        OdfTextRun span = editor.AppendSpan(document, paragraph, " span", "Strong");
        OdfTextBlock heading = editor.AppendHeading(document, "Heading", 2, "HeadingStyle");
        OdfTextRun link = editor.AppendHyperlink(
            document,
            heading,
            " link",
            "https://example.test/",
            "LinkStyle");
        OdfTextRange range = editor.FindTextRange(
            document,
            OdfTextRangeKind.Bookmark,
            "Selection")!;
        OdfTextBlock[] blocks = editor.GetTextBlocks(document);
        OdfTextRange[] ranges = editor.GetTextRanges(document);
        string enclosed = editor.ExtractTextRange(document, range);
        OdfNode[] enclosedNodes = editor.NavigateTextRange(document, range);
        editor.RenameTextRange(document, range, "RenamedSelection");

        Assert.Multiple(() =>
        {
            Assert.That(editor.ExtractPlainText(document), Is.EqualTo(
                "Before inside\nBody span\nHeading link"));
            Assert.That(blocks, Has.Length.EqualTo(3));
            Assert.That(blocks, Is.Not.SameAs(document.TextBlocks));
            Assert.That(ranges, Has.Length.EqualTo(1));
            Assert.That(enclosed, Is.EqualTo("inside"));
            Assert.That(enclosedNodes, Is.Not.Empty);
            Assert.That(span.Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(link.Href, Is.EqualTo("https://example.test/"));
            Assert.That(editor.FindTextRange(
                document,
                OdfTextRangeKind.Bookmark,
                "RenamedSelection"), Is.SameAs(range));
        });
    }

    [Test]
    public void CoversFlowListsFieldsNotesIndexesAndMasterTextThroughTheContract()
    {
        string masterStyles = "<office:styles>"
            + "<text:notes-configuration text:note-class='footnote' text:start-value='1'/>"
            + "<text:bibliography-configuration text:prefix='[' text:suffix=']'/>"
            + "</office:styles><office:master-styles>"
            + "<style:master-page style:name='Title' style:next-style-name='Body'>"
            + "<style:header><text:p>Header</text:p></style:header>"
            + "<style:footer><text:p>Footer</text:p></style:footer>"
            + "<style:header-first><text:p>First header</text:p></style:header-first>"
            + "</style:master-page><style:master-page style:name='Body'/>"
            + "</office:master-styles>";
        string declarations = "<text:user-field-decls>"
            + "<text:user-field-decl text:name='Customer' office:value-type='string' "
            + "office:string-value='Ada'/></text:user-field-decls>";
        string body = "<text:page-sequence><text:page text:master-page-name='Title'/>"
            + "<text:page text:master-page-name='Body'/></text:page-sequence>"
            + "<text:section text:name='SectionOne'><text:p text:style-name='BreakStyle'>"
            + "Section<text:line-break/>line</text:p></text:section>"
            + "<draw:frame draw:name='FrameOne' draw:chain-next-name='FrameTwo' "
            + "text:anchor-type='paragraph'><draw:text-box><text:p>Frame one</text:p>"
            + "</draw:text-box></draw:frame>"
            + "<draw:frame draw:name='FrameTwo' text:anchor-type='paragraph'>"
            + "<draw:text-box><text:p>Frame two</text:p></draw:text-box></draw:frame>"
            + "<text:list xml:id='ListOne'><text:list-item><text:p>One</text:p>"
            + "</text:list-item></text:list>"
            + "<text:list xml:id='ListTwo' text:continue-list='ListOne'>"
            + "<text:list-item><text:p>Two</text:p></text:list-item></text:list>"
            + "<text:numbered-paragraph text:list-id='Numbers' text:level='1'>"
            + "<text:p>Numbered</text:p></text:numbered-paragraph>"
            + "<text:p><text:user-field-get text:name='Customer'>Ada</text:user-field-get>"
            + "<text:note text:note-class='footnote' text:id='note-one'>"
            + "<text:note-citation>1</text:note-citation><text:note-body>"
            + "<text:p>Note body</text:p></text:note-body></text:note>"
            + "<text:bibliography-mark text:identifier='book-one' "
            + "text:bibliography-type='book' text:author='Author'>[1]"
            + "</text:bibliography-mark></text:p>"
            + SimpleIndex("table-of-content", "Contents", "Generated")
            + "<text:bibliography text:name='References'><text:bibliography-source/>"
            + "<text:index-body><text:p>References</text:p></text:index-body>"
            + "</text:bibliography>";
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            masterStyles,
            declarations,
            body,
            "<office:automatic-styles><style:style style:name='BreakStyle' "
            + "style:family='paragraph'><style:paragraph-properties "
            + "fo:break-before='page'/></style:style></office:automatic-styles>");
        IOdfTextEditor editor = new OdfTextEditor();

        OdfTextSection section = editor.FindTextSection(document, "SectionOne")!;
        OdfTextFrame frame = editor.FindTextFrame(document, "FrameOne")!;
        OdfTextList firstList = editor.FindTextList(document, "ListOne")!;
        OdfTextList secondList = editor.FindTextList(document, "ListTwo")!;
        OdfTextFieldDeclaration declaration = editor.FindTextFieldDeclaration(
            document,
            OdfTextFieldDeclarationKind.UserField,
            "Customer")!;
        OdfNote note = editor.FindNote(document, "note-one")!;
        OdfNoteConfiguration configuration = editor.FindNoteConfiguration(
            document,
            OdfNoteKind.Footnote)!;
        OdfMasterPage masterPage = document.MasterPages[0];

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetTextSections(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetTextFrames(document), Has.Length.EqualTo(2));
            Assert.That(editor.FollowTextFrameChain(frame), Has.Length.EqualTo(2));
            Assert.That(editor.GetTextBreaks(document), Has.Length.EqualTo(2));
            Assert.That(editor.GetTextLists(document), Has.Length.EqualTo(2));
            Assert.That(editor.FollowTextListContinuation(secondList),
                Is.EqualTo(new[] { firstList, secondList }));
            Assert.That(editor.GetNumberedParagraphs(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetTextFields(document), Has.Length.EqualTo(1));
            Assert.That(editor.FindTextFields(document, OdfTextFieldKind.UserField),
                Has.Length.EqualTo(1));
            Assert.That(editor.GetTextFieldDeclarations(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetNotes(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetNoteConfigurations(document), Has.Length.EqualTo(1));
            Assert.That(configuration.Kind, Is.EqualTo(OdfNoteKind.Footnote));
            Assert.That(editor.GetTextIndexes(document), Has.Length.EqualTo(2));
            Assert.That(editor.GetBibliographyConfigurations(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetBibliographyEntries(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetPageSequences(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetTitlePagePatterns(document), Has.Length.EqualTo(1));
            Assert.That(editor.GetMasterSubdocuments(document), Is.Empty);
            Assert.That(editor.GetMasterPageText(document, masterPage), Has.Length.EqualTo(3));
        });

        editor.RenameTextSection(document, section, "SectionTwo");
        editor.RenameTextFrame(document, frame, "FrameRenamed");
        editor.RenameTextList(document, firstList, "ListRenamed");
        editor.RenameTextFieldDeclaration(document, declaration, "Client");
        editor.RenameNote(document, note, "note-renamed");

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindTextSection(document, "SectionTwo"), Is.SameAs(section));
            Assert.That(editor.FindTextFrame(document, "FrameRenamed"), Is.SameAs(frame));
            Assert.That(secondList.ContinueListId, Is.EqualTo("ListRenamed"));
            Assert.That(editor.FindTextFieldDeclaration(
                document,
                OdfTextFieldDeclarationKind.UserField,
                "Client"), Is.SameAs(declaration));
            Assert.That(editor.FindNote(document, "note-renamed"), Is.SameAs(note));
        });

        OdfDocument masterDocument = ReadFlat(
            OdfMediaTypes.TextMaster,
            "<office:master-styles/>",
            "",
            "<text:section text:name='Chapter'><text:section-source "
            + "xlink:type='simple' xlink:href='chapter.odt' text:section-name='Body'/>"
            + "<text:p>Cached</text:p></text:section>");
        Assert.That(editor.GetMasterSubdocuments(masterDocument), Has.Length.EqualTo(1));
    }

    [Test]
    public void CoversEveryCursorAndEditingOperationThroughTheContract()
    {
        IOdfTextEditor editor = new OdfTextEditor();
        OdfDocument selectionDocument = ReadBody(
            "<text:p>One</text:p><text:p>Two</text:p><text:p>Three</text:p>");
        OdfTextCursor selection = editor.CreateTextCursor(
            selectionDocument,
            selectionDocument.TextBlocks[2],
            2);
        editor.SetTextSelection(
            selectionDocument,
            selection,
            selectionDocument.TextBlocks[0],
            1);
        editor.DeleteTextSelection(selectionDocument, selection);
        editor.ClearTextSelection(selectionDocument, selection);

        OdfDocument editDocument = ReadBody("<text:p>Alpha</text:p>");
        OdfTextCursor editCursor = editor.CreateTextCursor(
            editDocument,
            editDocument.TextBlocks[0],
            2);
        editor.InsertText(editDocument, editCursor, "XX");
        editor.DeleteTextForward(editDocument, editCursor, 2);
        OdfTextBlock split = editor.SplitTextBlock(editDocument, editCursor);
        editor.MergeFollowingTextBlock(editDocument, editDocument.TextBlocks[0]);

        OdfDocument blockDocument = ReadBody(
            "<text:p><text:span text:style-name='Strong'>A</text:span></text:p>"
            + "<text:p>B</text:p><text:p>C</text:p>");
        OdfTextBlock moving = blockDocument.TextBlocks[2];
        OdfTextBlock copied = editor.CopyTextBlock(
            blockDocument,
            blockDocument.TextBlocks[0],
            blockDocument.TextBlocks[1],
            false);
        editor.MoveTextBlock(
            blockDocument,
            moving,
            blockDocument.TextBlocks[0],
            true);

        OdfDocument styleDocument = ReadBody(
            "<text:p>abcdef</text:p><text:p>second</text:p>");
        OdfTextCursor styleCursor = editor.CreateTextCursor(
            styleDocument,
            styleDocument.TextBlocks[0],
            5);
        editor.SetTextSelection(styleDocument, styleCursor, styleDocument.TextBlocks[0], 1);
        editor.ApplyInlineStyle(styleDocument, styleCursor, "Emphasis");
        editor.SetTextSelection(styleDocument, styleCursor, styleDocument.TextBlocks[1], 2);
        editor.ApplyParagraphStyle(styleDocument, styleCursor, "Selected");

        Assert.Multiple(() =>
        {
            Assert.That(selectionDocument.TextBlocks[0].Text, Is.EqualTo("Oree"));
            Assert.That(editor.GetTextCursors(selectionDocument), Has.Length.EqualTo(1));
            Assert.That(editDocument.TextBlocks[0].Text, Is.EqualTo("AlXXa"));
            Assert.That(split.Text, Is.EqualTo("a"));
            Assert.That(copied.Runs[0].StyleName, Is.EqualTo("Strong"));
            Assert.That(blockDocument.TextBlocks[0].Text, Is.EqualTo("C"));
            Assert.That(styleDocument.TextBlocks[0].StyleName, Is.EqualTo("Selected"));
            Assert.That(styleDocument.TextBlocks[1].StyleName, Is.EqualTo("Selected"));
            Assert.That(styleDocument.TextBlocks[0].Runs[1].StyleName,
                Is.EqualTo("Emphasis"));
        });
    }

    [Test]
    public void CoversEverySearchAndReplaceOperationThroughTheContract()
    {
        IOdfTextEditor editor = new OdfTextEditor();
        OdfQualifiedName spanName = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "span",
            Prefix = "text"
        };
        OdfDocument searchDocument = ReadBody(
            "<text:p>Alpha <text:span text:style-name='Emphasis'>beta</text:span></text:p>"
            + "<text:p text:style-name='Emphasis'>Gamma</text:p>");

        Assert.Multiple(() =>
        {
            Assert.That(editor.SearchLiteralText(searchDocument, "alpha", false),
                Has.Length.EqualTo(1));
            Assert.That(editor.SearchRegularExpressionText(
                searchDocument,
                "beta|Gamma",
                true), Has.Length.EqualTo(2));
            Assert.That(editor.SearchStyleText(searchDocument, "Emphasis"),
                Has.Length.EqualTo(2));
            Assert.That(editor.SearchElementText(searchDocument, spanName),
                Has.Length.EqualTo(1));
        });

        OdfEditResult literal = editor.ReplaceLiteralText(
            ReadBody("<text:p>cat cat</text:p>"),
            "cat",
            "dog",
            true);
        OdfEditResult regularExpression = editor.ReplaceRegularExpressionText(
            ReadBody("<text:p>A12 B345</text:p>"),
            "([A-Z])(\\d+)",
            "$1[$2]",
            true);
        OdfEditResult style = editor.ReplaceStyleText(
            ReadBody("<text:p><text:span text:style-name='Emphasis'>hot</text:span></text:p>"),
            "Emphasis",
            "warm");
        OdfEditResult element = editor.ReplaceElementText(
            ReadBody("<text:p><text:span>inside</text:span></text:p>"),
            spanName,
            "changed");

        Assert.Multiple(() =>
        {
            Assert.That(literal.AffectedCount, Is.EqualTo(2));
            Assert.That(regularExpression.AffectedCount, Is.EqualTo(2));
            Assert.That(style.AffectedCount, Is.EqualTo(1));
            Assert.That(element.AffectedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CoversBothTemplateOperationsThroughTheContract()
    {
        OdfDocument document = ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles/>",
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

        Assert.Multiple(() =>
        {
            Assert.That(placeholder.AffectedCount, Is.EqualTo(1));
            Assert.That(userField.AffectedCount, Is.EqualTo(1));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo(
                "Ada Analytical Engines"));
        });
    }

    private static string SimpleIndex(string elementName, string name, string body)
    {
        return "<text:" + elementName + " text:name='" + name + "'>"
            + "<text:" + elementName + "-source/>"
            + "<text:index-body><text:p>" + body + "</text:p></text:index-body>"
            + "</text:" + elementName + ">";
    }

    private static OdfDocument ReadBody(string body)
    {
        return ReadFlat(
            OdfMediaTypes.Text,
            "<office:master-styles/>",
            "",
            body);
    }

    private static OdfDocument ReadFlat(
        string mediaType,
        string masterStyles,
        string declarations,
        string body)
    {
        return ReadFlat(mediaType, masterStyles, declarations, body, "");
    }

    private static OdfDocument ReadFlat(
        string mediaType,
        string masterStyles,
        string declarations,
        string body,
        string automaticStyles)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4' office:mimetype='" + mediaType + "'>"
            + automaticStyles
            + masterStyles
            + "<office:body><office:text>"
            + declarations
            + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-editor-contract.fodt",
            new OdfLoadOptions());
    }
}
