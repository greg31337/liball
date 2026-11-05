namespace odf;

/// <summary>Creates, accesses, navigates, searches, and modifies typed document text.</summary>
public interface IOdfTextEditor
{
    /// <summary>Extracts all paragraph and heading text.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The plain text in document order.</returns>
    public string ExtractPlainText(OdfDocument document);
    /// <summary>Gets the paragraphs and headings.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the typed text blocks.</returns>
    public OdfTextBlock[] GetTextBlocks(OdfDocument document);
    /// <summary>Appends a paragraph.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="text">The paragraph text.</param>
    /// <param name="styleName">The paragraph style name.</param>
    /// <returns>The created paragraph.</returns>
    public OdfTextBlock AppendParagraph(
        OdfDocument document,
        string text,
        string styleName);
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
        string styleName);
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
        string styleName);
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
        string styleName);
    /// <summary>Gets text markers and ranges.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the text ranges.</returns>
    public OdfTextRange[] GetTextRanges(OdfDocument document);
    /// <summary>Finds one uniquely named text range.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The range family.</param>
    /// <param name="name">The range name.</param>
    /// <returns>The unique range, or <c>null</c>.</returns>
    public OdfTextRange? FindTextRange(
        OdfDocument document,
        OdfTextRangeKind kind,
        string name);
    /// <summary>Extracts text enclosed by a resolved range.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="range">The range to read.</param>
    /// <returns>The enclosed text.</returns>
    public string ExtractTextRange(
        OdfDocument document,
        OdfTextRange range);
    /// <summary>Navigates nodes enclosed by a resolved range.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="range">The range to navigate.</param>
    /// <returns>The enclosed nodes in document order.</returns>
    public OdfNode[] NavigateTextRange(
        OdfDocument document,
        OdfTextRange range);
    /// <summary>Renames a text range and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="range">The range to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextRange(
        OdfDocument document,
        OdfTextRange range,
        string newName);
    /// <summary>Gets text sections.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the sections.</returns>
    public OdfTextSection[] GetTextSections(OdfDocument document);
    /// <summary>Finds a uniquely named section.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The section name.</param>
    /// <returns>The unique section, or <c>null</c>.</returns>
    public OdfTextSection? FindTextSection(OdfDocument document, string name);
    /// <summary>Renames a section and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="section">The section to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextSection(
        OdfDocument document,
        OdfTextSection section,
        string newName);
    /// <summary>Gets anchored text frames.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the frames.</returns>
    public OdfTextFrame[] GetTextFrames(OdfDocument document);
    /// <summary>Finds a uniquely named text frame.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The frame name.</param>
    /// <returns>The unique frame, or <c>null</c>.</returns>
    public OdfTextFrame? FindTextFrame(OdfDocument document, string name);
    /// <summary>Follows a valid frame chain.</summary>
    /// <param name="frame">The first frame.</param>
    /// <returns>The chain, or an empty array for an invalid chain.</returns>
    public OdfTextFrame[] FollowTextFrameChain(OdfTextFrame frame);
    /// <summary>Renames a frame and its chain references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="frame">The frame to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextFrame(
        OdfDocument document,
        OdfTextFrame frame,
        string newName);
    /// <summary>Gets explicit and effective text breaks.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the breaks.</returns>
    public OdfTextBreak[] GetTextBreaks(OdfDocument document);
    /// <summary>Gets typed text lists.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the lists.</returns>
    public OdfTextList[] GetTextLists(OdfDocument document);
    /// <summary>Finds a uniquely identified text list.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="xmlId">The list XML ID.</param>
    /// <returns>The unique list, or <c>null</c>.</returns>
    public OdfTextList? FindTextList(OdfDocument document, string xmlId);
    /// <summary>Follows a valid list-continuation chain.</summary>
    /// <param name="list">The final list in the chain.</param>
    /// <returns>The continuation chain.</returns>
    public OdfTextList[] FollowTextListContinuation(OdfTextList list);
    /// <summary>Renames a list XML ID and updates continuation references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="list">The list to rename.</param>
    /// <param name="newXmlId">The new XML ID.</param>
    public void RenameTextList(
        OdfDocument document,
        OdfTextList list,
        string newXmlId);
    /// <summary>Gets standalone numbered paragraphs.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the numbered paragraphs.</returns>
    public OdfNumberedParagraph[] GetNumberedParagraphs(OdfDocument document);
    /// <summary>Gets all typed text fields.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the fields.</returns>
    public OdfTextField[] GetTextFields(OdfDocument document);
    /// <summary>Finds fields in one family.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The field family.</param>
    /// <returns>The matching fields.</returns>
    public OdfTextField[] FindTextFields(
        OdfDocument document,
        OdfTextFieldKind kind);
    /// <summary>Gets variable and field declarations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the declarations.</returns>
    public OdfTextFieldDeclaration[] GetTextFieldDeclarations(
        OdfDocument document);
    /// <summary>Finds a unique text-field declaration.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The declaration family.</param>
    /// <param name="name">The declaration name.</param>
    /// <returns>The unique declaration, or <c>null</c>.</returns>
    public OdfTextFieldDeclaration? FindTextFieldDeclaration(
        OdfDocument document,
        OdfTextFieldDeclarationKind kind,
        string name);
    /// <summary>Renames a declaration and its field references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="declaration">The declaration to rename.</param>
    /// <param name="newName">The new name.</param>
    public void RenameTextFieldDeclaration(
        OdfDocument document,
        OdfTextFieldDeclaration declaration,
        string newName);
    /// <summary>Gets footnotes and endnotes.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the notes.</returns>
    public OdfNote[] GetNotes(OdfDocument document);
    /// <summary>Finds a uniquely identified note.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="id">The note ID.</param>
    /// <returns>The unique note, or <c>null</c>.</returns>
    public OdfNote? FindNote(OdfDocument document, string id);
    /// <summary>Gets note configurations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the configurations.</returns>
    public OdfNoteConfiguration[] GetNoteConfigurations(OdfDocument document);
    /// <summary>Finds a unique note configuration.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The note family.</param>
    /// <returns>The configuration, or <c>null</c>.</returns>
    public OdfNoteConfiguration? FindNoteConfiguration(
        OdfDocument document,
        OdfNoteKind kind);
    /// <summary>Renames a note ID and its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="note">The note to rename.</param>
    /// <param name="newId">The new ID.</param>
    public void RenameNote(
        OdfDocument document,
        OdfNote note,
        string newId);
    /// <summary>Gets generated text indexes.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the indexes.</returns>
    public OdfTextIndex[] GetTextIndexes(OdfDocument document);
    /// <summary>Gets bibliography configurations.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the configurations.</returns>
    public OdfBibliographyConfiguration[] GetBibliographyConfigurations(
        OdfDocument document);
    /// <summary>Gets bibliography entries.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the entries.</returns>
    public OdfBibliographyEntry[] GetBibliographyEntries(OdfDocument document);
    /// <summary>Gets explicit page sequences.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the page sequences.</returns>
    public OdfPageSequence[] GetPageSequences(OdfDocument document);
    /// <summary>Gets inferred title-page patterns.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the patterns.</returns>
    public OdfTitlePagePattern[] GetTitlePagePatterns(OdfDocument document);
    /// <summary>Gets linked master-document subdocuments.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the subdocuments.</returns>
    public OdfMasterSubdocument[] GetMasterSubdocuments(OdfDocument document);
    /// <summary>Gets typed header and footer content for a master page.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="masterPage">The master page to read.</param>
    /// <returns>A snapshot of its header and footer content.</returns>
    public OdfMasterPageContent[] GetMasterPageText(
        OdfDocument document,
        OdfMasterPage masterPage);
    /// <summary>Gets tracked text cursors.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>A snapshot of the cursors.</returns>
    public OdfTextCursor[] GetTextCursors(OdfDocument document);
    /// <summary>Creates a tracked text cursor.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="block">The cursor block.</param>
    /// <param name="offset">The UTF-16 offset.</param>
    /// <returns>The created cursor.</returns>
    public OdfTextCursor CreateTextCursor(
        OdfDocument document,
        OdfTextBlock block,
        int offset);
    /// <summary>Sets a cursor selection anchor.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="cursor">The cursor to modify.</param>
    /// <param name="anchorBlock">The anchor block.</param>
    /// <param name="anchorOffset">The anchor offset.</param>
    public void SetTextSelection(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfTextBlock anchorBlock,
        int anchorOffset);
    /// <summary>Clears a cursor selection.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="cursor">The cursor to modify.</param>
    public void ClearTextSelection(
        OdfDocument document,
        OdfTextCursor cursor);
    /// <summary>Inserts text at a cursor.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The insertion cursor.</param>
    /// <param name="text">The inserted text.</param>
    public void InsertText(
        OdfDocument document,
        OdfTextCursor cursor,
        string text);
    /// <summary>Deletes the cursor selection.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selected cursor.</param>
    public void DeleteTextSelection(
        OdfDocument document,
        OdfTextCursor cursor);
    /// <summary>Deletes characters following a cursor.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The deletion cursor.</param>
    /// <param name="count">The maximum character count.</param>
    public void DeleteTextForward(
        OdfDocument document,
        OdfTextCursor cursor,
        int count);
    /// <summary>Splits the cursor block.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The split cursor.</param>
    /// <returns>The following block.</returns>
    public OdfTextBlock SplitTextBlock(
        OdfDocument document,
        OdfTextCursor cursor);
    /// <summary>Merges a block with its following sibling.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="block">The first block.</param>
    public void MergeFollowingTextBlock(
        OdfDocument document,
        OdfTextBlock block);
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
        bool before);
    /// <summary>Moves a whole text block.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="source">The source block.</param>
    /// <param name="destination">The destination block.</param>
    /// <param name="before">Whether to insert before the destination.</param>
    public void MoveTextBlock(
        OdfDocument document,
        OdfTextBlock source,
        OdfTextBlock destination,
        bool before);
    /// <summary>Applies a paragraph style to selected blocks.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selection cursor.</param>
    /// <param name="styleName">The paragraph style name.</param>
    public void ApplyParagraphStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName);
    /// <summary>Applies an inline style to selected text.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The selection cursor.</param>
    /// <param name="styleName">The text style name.</param>
    public void ApplyInlineStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName);
    /// <summary>Searches literal text.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="value">The literal query.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchLiteralText(
        OdfDocument document,
        string value,
        bool matchCase);
    /// <summary>Searches text with a regular expression.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="matchCase">Whether case must match.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchRegularExpressionText(
        OdfDocument document,
        string pattern,
        bool matchCase);
    /// <summary>Searches text carrying a style.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="styleName">The style name.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchStyleText(
        OdfDocument document,
        string styleName);
    /// <summary>Searches typed text elements.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The qualified element name.</param>
    /// <returns>The matches.</returns>
    public OdfSearchMatch[] SearchElementText(
        OdfDocument document,
        OdfQualifiedName name);
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
        bool matchCase);
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
        bool matchCase);
    /// <summary>Replaces text carrying a style.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="styleName">The style name.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceStyleText(
        OdfDocument document,
        string styleName,
        string replacement);
    /// <summary>Replaces text in qualified elements.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The qualified element name.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceElementText(
        OdfDocument document,
        OdfQualifiedName name,
        string replacement);
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
        bool retainFields);
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
        bool retainFields);
}
