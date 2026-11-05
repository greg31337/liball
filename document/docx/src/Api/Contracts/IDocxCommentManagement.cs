namespace docx;

/// <summary>
/// Enumerates, creates, edits, deletes, and anchors document comments.
/// </summary>
public interface IDocxCommentManagement
{
    /// <summary>
    /// Returns all comment stories in part order.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the comments.</returns>
    List<DocxComment> GetComments(DocxDocument document);

    /// <summary>
    /// Finds a comment by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The matching comment, or null.</returns>
    DocxComment? FindComment(DocxDocument document, string id);

    /// <summary>
    /// Returns a comment identifier.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The identifier.</returns>
    string GetCommentId(DocxComment comment);

    /// <summary>
    /// Returns the review author.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The author.</returns>
    string GetCommentAuthor(DocxComment comment);

    /// <summary>
    /// Returns the review author initials.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The initials.</returns>
    string GetCommentInitials(DocxComment comment);

    /// <summary>
    /// Returns the review timestamp text.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The timestamp, or an empty string.</returns>
    string GetCommentDate(DocxComment comment);

    /// <summary>
    /// Returns the blocks in a comment story.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    List<DocxBlock> GetCommentBlocks(DocxComment comment);

    /// <summary>
    /// Creates a comment with automatically allocated identifier and no timestamp.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    DocxComment CreateComment(DocxDocument document, string author, string initials, string text);

    /// <summary>
    /// Creates a comment with complete review metadata and an allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    DocxComment CreateCommentWithMetadata(DocxDocument document, string author, string initials, string date, string text);

    /// <summary>
    /// Creates a comment with an explicit identifier and review metadata.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    DocxComment CreateCommentWithId(DocxDocument document, string id, string author, string initials, string date, string text);

    /// <summary>
    /// Replaces all comment content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="text">The replacement text.</param>
    void ReplaceCommentText(DocxDocument document, DocxComment comment, string text);

    /// <summary>
    /// Replaces a comment's review metadata.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    void SetCommentReviewMetadata(DocxDocument document, DocxComment comment, string author, string initials, string date);

    /// <summary>
    /// Appends a paragraph to a comment story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    void AppendCommentParagraph(DocxDocument document, DocxComment comment, DocxParagraph paragraph);

    /// <summary>
    /// Removes a block from a comment story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveCommentBlock(DocxDocument document, DocxComment comment, int index);

    /// <summary>
    /// Deletes a comment definition without changing document range or reference markup.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>True when a comment was deleted.</returns>
    bool DeleteComment(DocxDocument document, string id);

    /// <summary>
    /// Inserts a comment range and reference around a paragraph child range.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="comment">The referenced comment.</param>
    /// <returns>The inserted reference run.</returns>
    DocxRun InsertCommentRange(DocxParagraph paragraph, int startIndex, int endIndex, DocxComment comment);

    /// <summary>
    /// Inserts a comment reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="comment">The referenced comment.</param>
    /// <returns>The inserted reference run.</returns>
    DocxRun InsertCommentReference(DocxParagraph paragraph, int index, DocxComment comment);

    /// <summary>
    /// Returns comment range-start identifiers from direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    List<string> GetCommentRangeStartIds(DocxParagraph paragraph);

    /// <summary>
    /// Returns comment range-end identifiers from direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    List<string> GetCommentRangeEndIds(DocxParagraph paragraph);

    /// <summary>
    /// Returns comment reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    List<string> GetCommentReferenceIds(DocxParagraph paragraph);

    /// <summary>
    /// Removes matching comment range-start and range-end markers.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The number of removed markers.</returns>
    int RemoveCommentRangeMarkers(DocxParagraph paragraph, string id);

    /// <summary>
    /// Removes matching comment references while preserving other run content.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The number of removed reference elements.</returns>
    int RemoveCommentReferences(DocxParagraph paragraph, string id);
}
