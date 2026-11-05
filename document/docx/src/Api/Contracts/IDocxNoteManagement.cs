namespace docx;

/// <summary>
/// Enumerates, creates, edits, deletes, and references footnotes and endnotes.
/// </summary>
public interface IDocxNoteManagement
{
    /// <summary>
    /// Returns all footnote stories, including separator stories.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the footnotes.</returns>
    List<DocxFootnote> GetFootnotes(DocxDocument document);

    /// <summary>
    /// Returns all endnote stories, including separator stories.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the endnotes.</returns>
    List<DocxEndnote> GetEndnotes(DocxDocument document);

    /// <summary>
    /// Finds a footnote by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <returns>The matching footnote, or null.</returns>
    DocxFootnote? FindFootnote(DocxDocument document, string id);

    /// <summary>
    /// Finds an endnote by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <returns>The matching endnote, or null.</returns>
    DocxEndnote? FindEndnote(DocxDocument document, string id);

    /// <summary>
    /// Returns a footnote identifier.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>The identifier.</returns>
    string GetFootnoteId(DocxFootnote footnote);

    /// <summary>
    /// Returns an endnote identifier.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>The identifier.</returns>
    string GetEndnoteId(DocxEndnote endnote);

    /// <summary>
    /// Returns the footnote story type, or an empty string for a normal note.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>The story type.</returns>
    string GetFootnoteType(DocxFootnote footnote);

    /// <summary>
    /// Returns the endnote story type, or an empty string for a normal note.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>The story type.</returns>
    string GetEndnoteType(DocxEndnote endnote);

    /// <summary>
    /// Returns the blocks in a footnote story.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>A new list containing its blocks.</returns>
    List<DocxBlock> GetFootnoteBlocks(DocxFootnote footnote);

    /// <summary>
    /// Returns the blocks in an endnote story.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>A new list containing its blocks.</returns>
    List<DocxBlock> GetEndnoteBlocks(DocxEndnote endnote);

    /// <summary>
    /// Creates a footnote with an automatically allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created footnote.</returns>
    DocxFootnote CreateFootnote(DocxDocument document, string text);

    /// <summary>
    /// Creates a footnote with an explicit identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created footnote.</returns>
    DocxFootnote CreateFootnoteWithId(DocxDocument document, string id, string text);

    /// <summary>
    /// Creates an endnote with an automatically allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created endnote.</returns>
    DocxEndnote CreateEndnote(DocxDocument document, string text);

    /// <summary>
    /// Creates an endnote with an explicit identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created endnote.</returns>
    DocxEndnote CreateEndnoteWithId(DocxDocument document, string id, string text);

    /// <summary>
    /// Replaces all footnote story content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="text">The replacement text.</param>
    void ReplaceFootnoteText(DocxDocument document, DocxFootnote footnote, string text);

    /// <summary>
    /// Replaces all endnote story content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="text">The replacement text.</param>
    void ReplaceEndnoteText(DocxDocument document, DocxEndnote endnote, string text);

    /// <summary>
    /// Appends a paragraph to a footnote story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    void AppendFootnoteParagraph(DocxDocument document, DocxFootnote footnote, DocxParagraph paragraph);

    /// <summary>
    /// Appends a paragraph to an endnote story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    void AppendEndnoteParagraph(DocxDocument document, DocxEndnote endnote, DocxParagraph paragraph);

    /// <summary>
    /// Removes a footnote story block.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveFootnoteBlock(DocxDocument document, DocxFootnote footnote, int index);

    /// <summary>
    /// Removes an endnote story block.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveEndnoteBlock(DocxDocument document, DocxEndnote endnote, int index);

    /// <summary>
    /// Deletes a footnote definition without changing document references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <returns>True when a footnote was deleted.</returns>
    bool DeleteFootnote(DocxDocument document, string id);

    /// <summary>
    /// Deletes an endnote definition without changing document references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <returns>True when an endnote was deleted.</returns>
    bool DeleteEndnote(DocxDocument document, string id);

    /// <summary>
    /// Inserts a footnote reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="footnote">The referenced footnote.</param>
    /// <returns>The inserted reference run.</returns>
    DocxRun InsertFootnoteReference(DocxParagraph paragraph, int index, DocxFootnote footnote);

    /// <summary>
    /// Inserts an endnote reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="endnote">The referenced endnote.</param>
    /// <returns>The inserted reference run.</returns>
    DocxRun InsertEndnoteReference(DocxParagraph paragraph, int index, DocxEndnote endnote);

    /// <summary>
    /// Returns footnote reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    List<string> GetFootnoteReferenceIds(DocxParagraph paragraph);

    /// <summary>
    /// Returns endnote reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    List<string> GetEndnoteReferenceIds(DocxParagraph paragraph);

    /// <summary>
    /// Removes matching footnote reference elements from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The referenced footnote identifier.</param>
    /// <returns>The number of removed reference elements.</returns>
    int RemoveFootnoteReferences(DocxParagraph paragraph, string id);

    /// <summary>
    /// Removes matching endnote reference elements from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The referenced endnote identifier.</param>
    /// <returns>The number of removed reference elements.</returns>
    int RemoveEndnoteReferences(DocxParagraph paragraph, string id);
}
