using System;

namespace odf;

/// <summary>Provides typed creation, access, navigation, search, and editing for document text.</summary>
public class OdfTextEditor : IOdfTextEditor
{
    /// <summary>Initializes a text editor.</summary>
    public OdfTextEditor()
    {
    }

    /// <summary>Extracts all paragraph and heading text.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The plain text in document order.</returns>
    public string ExtractPlainText(OdfDocument document)
    {
        RequireDocument(document);
        return TextExtractor.ExtractPlainText(document);
    }

    /// <summary>Gets the paragraphs and headings.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the typed text blocks.</returns>
    public OdfTextBlock[] GetTextBlocks(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextBlock[])document.TextBlocks.Clone();
    }

    /// <summary>Appends a paragraph.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="text">The paragraph text.</param>
    /// <param name="styleName">The paragraph style name.</param>
    /// <returns>The created paragraph.</returns>
    public OdfTextBlock AppendParagraph(
        OdfDocument document,
        string text,
        string styleName)
    {
        return AppendCreatedBlock(document, text, styleName, false, 0);
    }

    /// <summary>Appends a heading.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="text">The heading text.</param>
    /// <param name="outlineLevel">The positive outline level.</param>
    /// <param name="styleName">The heading style name.</param>
    /// <returns>The created heading.</returns>
    public OdfTextBlock AppendHeading(
        OdfDocument document,
        string text,
        int outlineLevel,
        string styleName)
    {
        if (outlineLevel <= 0)
        {
            throw new OdfEditException(
                "A heading requires a positive outline level.");
        }
        return AppendCreatedBlock(
            document,
            text,
            styleName,
            true,
            outlineLevel);
    }

    /// <summary>Appends a styled span to a block.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="block">The block to modify.</param>
    /// <param name="text">The span text.</param>
    /// <param name="styleName">The text style name.</param>
    /// <returns>The created span run.</returns>
    public OdfTextRun AppendSpan(
        OdfDocument document,
        OdfTextBlock block,
        string text,
        string styleName)
    {
        RequireTextValue(text, styleName);
        OdfTextRun run = CreateContainerRun(
            OdfTextRunKind.Span,
            text,
            styleName);
        AppendRun(document, block, run);
        return run;
    }

    /// <summary>Appends a hyperlink to a block.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="block">The block to modify.</param>
    /// <param name="text">The link text.</param>
    /// <param name="href">The link IRI.</param>
    /// <param name="styleName">The text style name.</param>
    /// <returns>The created hyperlink run.</returns>
    public OdfTextRun AppendHyperlink(
        OdfDocument document,
        OdfTextBlock block,
        string text,
        string href,
        string styleName)
    {
        RequireTextValue(text, styleName);
        if (href == null)
        {
            throw new OdfEditException("A hyperlink IRI is required.");
        }
        OdfTextRun run = CreateContainerRun(
            OdfTextRunKind.Hyperlink,
            text,
            styleName);
        run.Href = href;
        run.LinkShow = "replace";
        run.LinkActuate = "onRequest";
        AppendRun(document, block, run);
        return run;
    }

    /// <summary>Gets text markers and ranges.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the text ranges.</returns>
    public OdfTextRange[] GetTextRanges(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextRange[])document.TextRanges.Clone();
    }

    /// <summary>Finds one uniquely named text range.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The range family.</param>
    /// <param name="name">The range name.</param>
    /// <returns>The unique range, or <c>null</c>.</returns>
    public OdfTextRange? FindTextRange(
        OdfDocument document,
        OdfTextRangeKind kind,
        string name)
    {
        return TextNavigator.FindTextRange(document, kind, name);
    }

    /// <summary>Extracts text enclosed by a resolved range.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="range">The range to read.</param>
    /// <returns>The enclosed text.</returns>
    public string ExtractTextRange(
        OdfDocument document,
        OdfTextRange range)
    {
        return TextNavigator.ExtractTextRange(document, range);
    }

    /// <summary>Navigates nodes enclosed by a resolved range.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="range">The range to navigate.</param>
    /// <returns>The enclosed nodes in document order.</returns>
    public OdfNode[] NavigateTextRange(
        OdfDocument document,
        OdfTextRange range)
    {
        return TextNavigator.NavigateTextRange(document, range);
    }

    /// <summary>Renames a text range and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="range">The range to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextRange(
        OdfDocument document,
        OdfTextRange range,
        string newName)
    {
        TextNavigator.RenameTextRange(document, range, newName);
    }

    /// <summary>Gets text sections.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the sections.</returns>
    public OdfTextSection[] GetTextSections(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextSection[])document.TextSections.Clone();
    }

    /// <summary>Finds a uniquely named section.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The section name.</param>
    /// <returns>The unique section, or <c>null</c>.</returns>
    public OdfTextSection? FindTextSection(OdfDocument document, string name)
    {
        return TextFlowNavigator.FindSection(document, name);
    }

    /// <summary>Renames a section and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="section">The section to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextSection(
        OdfDocument document,
        OdfTextSection section,
        string newName)
    {
        TextFlowNavigator.RenameSection(document, section, newName);
    }

    /// <summary>Gets anchored text frames.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the frames.</returns>
    public OdfTextFrame[] GetTextFrames(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextFrame[])document.TextFrames.Clone();
    }

    /// <summary>Finds a uniquely named text frame.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The frame name.</param>
    /// <returns>The unique frame, or <c>null</c>.</returns>
    public OdfTextFrame? FindTextFrame(OdfDocument document, string name)
    {
        return TextFlowNavigator.FindFrame(document, name);
    }

    /// <summary>Follows a valid frame chain.</summary>
    /// <param name="frame">The first frame.</param>
    /// <returns>The chain, or an empty array for an invalid chain.</returns>
    public OdfTextFrame[] FollowTextFrameChain(OdfTextFrame frame)
    {
        return TextFlowNavigator.FollowFrameChain(frame);
    }

    /// <summary>Renames a frame and its chain references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="frame">The frame to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextFrame(
        OdfDocument document,
        OdfTextFrame frame,
        string newName)
    {
        TextFlowNavigator.RenameFrame(document, frame, newName);
    }

    /// <summary>Gets explicit and effective text breaks.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the breaks.</returns>
    public OdfTextBreak[] GetTextBreaks(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextBreak[])document.TextBreaks.Clone();
    }

    /// <summary>Gets typed text lists.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the lists.</returns>
    public OdfTextList[] GetTextLists(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextList[])document.TextLists.Clone();
    }

    /// <summary>Finds a uniquely identified text list.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="xmlId">The list XML ID.</param>
    /// <returns>The unique list, or <c>null</c>.</returns>
    public OdfTextList? FindTextList(OdfDocument document, string xmlId)
    {
        return TextListNavigator.FindListByXmlId(document, xmlId);
    }

    /// <summary>Follows a valid list-continuation chain.</summary>
    /// <param name="list">The final list in the chain.</param>
    /// <returns>The continuation chain.</returns>
    public OdfTextList[] FollowTextListContinuation(OdfTextList list)
    {
        return TextListNavigator.FollowContinuation(list);
    }

    /// <summary>Renames a list XML ID and updates continuation references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="list">The list to rename.</param>
    /// <param name="newXmlId">The new XML ID.</param>
    public void RenameTextList(
        OdfDocument document,
        OdfTextList list,
        string newXmlId)
    {
        TextListNavigator.RenameListXmlId(document, list, newXmlId);
    }

    /// <summary>Gets standalone numbered paragraphs.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the numbered paragraphs.</returns>
    public OdfNumberedParagraph[] GetNumberedParagraphs(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfNumberedParagraph[])document.NumberedParagraphs.Clone();
    }

    /// <summary>Gets all typed text fields.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the fields.</returns>
    public OdfTextField[] GetTextFields(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextField[])document.TextFields.Clone();
    }

    /// <summary>Finds fields in one family.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The field family.</param>
    /// <returns>The matching fields.</returns>
    public OdfTextField[] FindTextFields(
        OdfDocument document,
        OdfTextFieldKind kind)
    {
        return TextFieldNavigator.FindFields(document, kind);
    }

    /// <summary>Gets variable and field declarations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the declarations.</returns>
    public OdfTextFieldDeclaration[] GetTextFieldDeclarations(
        OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextFieldDeclaration[])document.TextFieldDeclarations.Clone();
    }

    /// <summary>Finds a unique text-field declaration.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The declaration family.</param>
    /// <param name="name">The declaration name.</param>
    /// <returns>The unique declaration, or <c>null</c>.</returns>
    public OdfTextFieldDeclaration? FindTextFieldDeclaration(
        OdfDocument document,
        OdfTextFieldDeclarationKind kind,
        string name)
    {
        return TextFieldNavigator.FindDeclaration(document, kind, name);
    }

    /// <summary>Renames a declaration and its field references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="declaration">The declaration to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextFieldDeclaration(
        OdfDocument document,
        OdfTextFieldDeclaration declaration,
        string newName)
    {
        TextFieldNavigator.RenameDeclaration(
            document,
            declaration,
            newName);
    }

    /// <summary>Gets footnotes and endnotes.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the notes.</returns>
    public OdfNote[] GetNotes(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfNote[])document.Notes.Clone();
    }

    /// <summary>Finds a uniquely identified note.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="id">The note ID.</param>
    /// <returns>The unique note, or <c>null</c>.</returns>
    public OdfNote? FindNote(OdfDocument document, string id)
    {
        return NoteNavigator.FindNote(document, id);
    }

    /// <summary>Gets note configurations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the configurations.</returns>
    public OdfNoteConfiguration[] GetNoteConfigurations(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfNoteConfiguration[])document.NoteConfigurations.Clone();
    }

    /// <summary>Finds a unique note configuration.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The note family.</param>
    /// <returns>The configuration, or <c>null</c>.</returns>
    public OdfNoteConfiguration? FindNoteConfiguration(
        OdfDocument document,
        OdfNoteKind kind)
    {
        return NoteNavigator.FindConfiguration(document, kind);
    }

    /// <summary>Renames a note ID and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="note">The note to rename.</param>
    /// <param name="newId">The new ID.</param>
    public void RenameNote(
        OdfDocument document,
        OdfNote note,
        string newId)
    {
        NoteNavigator.RenameNoteId(document, note, newId);
    }

    /// <summary>Gets generated text indexes.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the indexes.</returns>
    public OdfTextIndex[] GetTextIndexes(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextIndex[])document.TextIndexes.Clone();
    }

    /// <summary>Gets bibliography configurations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the configurations.</returns>
    public OdfBibliographyConfiguration[] GetBibliographyConfigurations(
        OdfDocument document)
    {
        RequireDocument(document);
        return (OdfBibliographyConfiguration[])
            document.BibliographyConfigurations.Clone();
    }

    /// <summary>Gets bibliography entries.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the entries.</returns>
    public OdfBibliographyEntry[] GetBibliographyEntries(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfBibliographyEntry[])document.BibliographyEntries.Clone();
    }

    /// <summary>Gets explicit page sequences.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the page sequences.</returns>
    public OdfPageSequence[] GetPageSequences(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfPageSequence[])document.PageSequences.Clone();
    }

    /// <summary>Gets inferred title-page patterns.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the patterns.</returns>
    public OdfTitlePagePattern[] GetTitlePagePatterns(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTitlePagePattern[])document.TitlePagePatterns.Clone();
    }

    /// <summary>Gets linked master-document subdocuments.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the subdocuments.</returns>
    public OdfMasterSubdocument[] GetMasterSubdocuments(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfMasterSubdocument[])document.MasterSubdocuments.Clone();
    }

    /// <summary>Gets typed header and footer content for a master page.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="masterPage">The master page to read.</param>
    /// <returns>A snapshot of its header and footer content.</returns>
    public OdfMasterPageContent[] GetMasterPageText(
        OdfDocument document,
        OdfMasterPage masterPage)
    {
        RequireMasterPage(document, masterPage);
        return (OdfMasterPageContent[])masterPage.TextContents.Clone();
    }

    /// <summary>Gets tracked text cursors.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the cursors.</returns>
    public OdfTextCursor[] GetTextCursors(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfTextCursor[])document.TextCursors.Clone();
    }

    /// <summary>Creates a tracked text cursor.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="block">The cursor block.</param>
    /// <param name="offset">The UTF-16 offset.</param>
    /// <returns>The created cursor.</returns>
    public OdfTextCursor CreateTextCursor(
        OdfDocument document,
        OdfTextBlock block,
        int offset)
    {
        return TextEditingModel.CreateCursor(document, block, offset);
    }

    /// <summary>Sets a cursor selection anchor.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="cursor">The cursor to modify.</param>
    /// <param name="anchorBlock">The anchor block.</param>
    /// <param name="anchorOffset">The anchor offset.</param>
    public void SetTextSelection(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfTextBlock anchorBlock,
        int anchorOffset)
    {
        TextEditingModel.SetSelection(
            document,
            cursor,
            anchorBlock,
            anchorOffset);
    }

    /// <summary>Clears a cursor selection.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="cursor">The cursor to modify.</param>
    public void ClearTextSelection(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        TextEditingModel.ClearSelection(document, cursor);
    }

    /// <summary>Inserts text at a cursor.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The insertion cursor.</param>
    /// <param name="text">The inserted text.</param>
    public void InsertText(
        OdfDocument document,
        OdfTextCursor cursor,
        string text)
    {
        TextEditingModel.InsertText(document, cursor, text);
    }

    /// <summary>Deletes the cursor selection.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selected cursor.</param>
    public void DeleteTextSelection(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        TextEditingModel.DeleteSelectedText(document, cursor);
    }

    /// <summary>Deletes characters following a cursor.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The deletion cursor.</param>
    /// <param name="count">The maximum character count.</param>
    public void DeleteTextForward(
        OdfDocument document,
        OdfTextCursor cursor,
        int count)
    {
        TextEditingModel.DeleteForward(document, cursor, count);
    }

    /// <summary>Splits the cursor block.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The split cursor.</param>
    /// <returns>The following block.</returns>
    public OdfTextBlock SplitTextBlock(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        return TextEditingModel.SplitBlock(document, cursor);
    }

    /// <summary>Merges a block with its following sibling.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="block">The first block.</param>
    public void MergeFollowingTextBlock(
        OdfDocument document,
        OdfTextBlock block)
    {
        TextEditingModel.MergeNextBlock(document, block);
    }

    /// <summary>Copies a whole text block.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="source">The source block.</param>
    /// <param name="destination">The destination block.</param>
    /// <param name="before">Whether to insert before the destination.</param>
    /// <returns>The copied block.</returns>
    public OdfTextBlock CopyTextBlock(
        OdfDocument document,
        OdfTextBlock source,
        OdfTextBlock destination,
        bool before)
    {
        return TextEditingModel.CopyBlock(
            document,
            source,
            destination,
            before);
    }

    /// <summary>Moves a whole text block.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="source">The source block.</param>
    /// <param name="destination">The destination block.</param>
    /// <param name="before">Whether to insert before the destination.</param>
    public void MoveTextBlock(
        OdfDocument document,
        OdfTextBlock source,
        OdfTextBlock destination,
        bool before)
    {
        TextEditingModel.MoveBlock(
            document,
            source,
            destination,
            before);
    }

    /// <summary>Applies a paragraph style to selected blocks.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selection cursor.</param>
    /// <param name="styleName">The paragraph style name.</param>
    public void ApplyParagraphStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName)
    {
        TextEditingModel.ApplyParagraphStyle(document, cursor, styleName);
    }

    /// <summary>Applies an inline style to selected text.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selection cursor.</param>
    /// <param name="styleName">The text style name.</param>
    public void ApplyInlineStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName)
    {
        TextEditingModel.ApplyInlineStyle(document, cursor, styleName);
    }

    /// <summary>Searches literal text.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="value">The literal query.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchLiteralText(
        OdfDocument document,
        string value,
        bool matchCase)
    {
        return TextSearchModel.SearchLiteral(document, value, matchCase);
    }

    /// <summary>Searches text with a regular expression.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchRegularExpressionText(
        OdfDocument document,
        string pattern,
        bool matchCase)
    {
        return TextSearchModel.SearchRegularExpression(
            document,
            pattern,
            matchCase);
    }

    /// <summary>Searches text carrying a style.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="styleName">The style name.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchStyleText(
        OdfDocument document,
        string styleName)
    {
        return TextSearchModel.SearchStyle(document, styleName);
    }

    /// <summary>Searches typed text elements.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The qualified element name.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchElementText(
        OdfDocument document,
        OdfQualifiedName name)
    {
        return TextSearchModel.SearchElement(document, name);
    }

    /// <summary>Replaces literal text.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="value">The literal query.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceLiteralText(
        OdfDocument document,
        string value,
        string replacement,
        bool matchCase)
    {
        return TextSearchModel.ReplaceLiteral(
            document,
            value,
            replacement,
            matchCase);
    }

    /// <summary>Replaces regular-expression matches.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="replacement">The replacement expression.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceRegularExpressionText(
        OdfDocument document,
        string pattern,
        string replacement,
        bool matchCase)
    {
        return TextSearchModel.ReplaceRegularExpression(
            document,
            pattern,
            replacement,
            matchCase);
    }

    /// <summary>Replaces text carrying a style.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="styleName">The style name.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceStyleText(
        OdfDocument document,
        string styleName,
        string replacement)
    {
        return TextSearchModel.ReplaceStyle(
            document,
            styleName,
            replacement);
    }

    /// <summary>Replaces text in qualified elements.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The qualified element name.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceElementText(
        OdfDocument document,
        OdfQualifiedName name,
        string replacement)
    {
        return TextSearchModel.ReplaceElement(
            document,
            name,
            replacement);
    }

    /// <summary>Fills matching template placeholders.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="key">The placeholder name, description, or display text.</param>
    /// <param name="value">The filled value.</param>
    /// <param name="retainFields">Whether to retain placeholder elements.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult FillPlaceholders(
        OdfDocument document,
        string key,
        string value,
        bool retainFields)
    {
        return TemplateFieldFiller.FillPlaceholders(
            document,
            key,
            value,
            retainFields);
    }

    /// <summary>Fills matching user fields and their declaration.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The user-field name.</param>
    /// <param name="value">The filled value.</param>
    /// <param name="retainFields">Whether to retain field elements.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult FillUserField(
        OdfDocument document,
        string name,
        string value,
        bool retainFields)
    {
        return TemplateFieldFiller.FillUserField(
            document,
            name,
            value,
            retainFields);
    }

    internal OdfTextBlock AppendCreatedBlock(
        OdfDocument document,
        string text,
        string styleName,
        bool heading,
        int outlineLevel)
    {
        RequireDocument(document);
        RequireTextValue(text, styleName);
        OdfTextRun run = new OdfTextRun();
        run.Kind = OdfTextRunKind.Text;
        run.Text = text;
        OdfTextBlock block = new OdfTextBlock();
        block.IsHeading = heading;
        block.OutlineLevel = outlineLevel;
        block.StyleName = styleName;
        block.Text = text;
        block.Runs = [run];
        block.OriginalText = text;
        OdfTextBlock[] blocks =
            new OdfTextBlock[document.TextBlocks.Length + 1];
        Array.Copy(document.TextBlocks, blocks, document.TextBlocks.Length);
        blocks[blocks.Length - 1] = block;
        document.TextBlocks = blocks;
        return block;
    }

    internal OdfTextRun CreateContainerRun(
        OdfTextRunKind kind,
        string text,
        string styleName)
    {
        OdfTextRun child = new OdfTextRun();
        child.Kind = OdfTextRunKind.Text;
        child.Text = text;
        OdfTextRun result = new OdfTextRun();
        result.Kind = kind;
        result.Text = text;
        result.StyleName = styleName;
        result.Runs = [child];
        return result;
    }

    internal void AppendRun(
        OdfDocument document,
        OdfTextBlock block,
        OdfTextRun run)
    {
        RequireTextBlock(document, block);
        string structuredText = TextExtractor.ExtractRunText(block.Runs);
        if (structuredText != block.Text)
        {
            OdfTextRun text = new OdfTextRun();
            text.Kind = OdfTextRunKind.Text;
            text.Text = block.Text;
            block.Runs = [text];
        }
        OdfTextRun[] runs = new OdfTextRun[block.Runs.Length + 1];
        Array.Copy(block.Runs, runs, block.Runs.Length);
        runs[runs.Length - 1] = run;
        block.Runs = runs;
        block.Text = TextExtractor.ExtractRunText(block.Runs);
        block.OriginalText = block.Text;
    }

    internal void RequireTextBlock(
        OdfDocument document,
        OdfTextBlock block)
    {
        RequireDocument(document);
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            if (document.TextBlocks[index] == block)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The text block does not belong to the document.");
    }

    internal void RequireMasterPage(
        OdfDocument document,
        OdfMasterPage masterPage)
    {
        RequireDocument(document);
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            if (document.MasterPages[index] == masterPage)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The master page does not belong to the document.");
    }

    internal void RequireTextValue(string text, string styleName)
    {
        if (text == null || styleName == null)
        {
            throw new OdfEditException(
                "Text and style-name values cannot be null.");
        }
    }

    internal void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text editing.");
        }
    }
}
