namespace docx;

/// <summary>
/// Traverses, extracts, accepts, rejects, and configures tracked revisions.
/// </summary>
public interface IDocxRevisionManagement
{
    /// <summary>
    /// Returns revisions from the main document and all supported related stories.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The revisions in story and XML order.</returns>
    List<DocxRevisionInfo> GetDocumentRevisions(DocxDocument document);

    /// <summary>
    /// Returns revisions nested anywhere in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The revisions in XML order.</returns>
    List<DocxRevisionInfo> GetParagraphRevisions(DocxParagraph paragraph);

    /// <summary>
    /// Returns revisions nested anywhere in a run.
    /// </summary>
    /// <param name="run">The run to inspect.</param>
    /// <returns>The revisions in XML order.</returns>
    List<DocxRevisionInfo> GetRunRevisions(DocxRun run);

    /// <summary>
    /// Returns document revisions of one semantic kind.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="kind">The revision kind to select.</param>
    /// <returns>The matching revisions in story and XML order.</returns>
    List<DocxRevisionInfo> GetDocumentRevisionsByKind(DocxDocument document, DocxRevisionKind kind);

    /// <summary>
    /// Finds the first document revision with an identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The revision identifier.</param>
    /// <returns>The matching revision, or null.</returns>
    DocxRevisionInfo? FindDocumentRevisionById(DocxDocument document, string id);

    /// <summary>
    /// Extracts selected document stories with explicit revision visibility.
    /// </summary>
    /// <param name="document">The document to extract.</param>
    /// <param name="includeInsertedText">Whether inserted and move-to content is included.</param>
    /// <param name="includeDeletedText">Whether deleted and move-from content is included.</param>
    /// <returns>The extracted text.</returns>
    string ExtractDocumentRevisionText(DocxDocument document, bool includeInsertedText, bool includeDeletedText);

    /// <summary>
    /// Extracts paragraph text with explicit revision visibility.
    /// </summary>
    /// <param name="paragraph">The paragraph to extract.</param>
    /// <param name="includeInsertedText">Whether inserted and move-to content is included.</param>
    /// <param name="includeDeletedText">Whether deleted and move-from content is included.</param>
    /// <returns>The extracted text.</returns>
    string ExtractParagraphRevisionText(DocxParagraph paragraph, bool includeInsertedText, bool includeDeletedText);

    /// <summary>
    /// Accepts every revision in the main document and supported related stories.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The number of accepted revision elements.</returns>
    int AcceptDocumentRevisions(DocxDocument document);

    /// <summary>
    /// Rejects every revision in the main document and supported related stories.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The number of rejected revision elements.</returns>
    int RejectDocumentRevisions(DocxDocument document);

    /// <summary>
    /// Accepts revisions within a range of direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first selected child.</param>
    /// <param name="endIndex">The exclusive selected end position.</param>
    /// <returns>The number of accepted revision elements.</returns>
    int AcceptParagraphChildRange(DocxParagraph paragraph, int startIndex, int endIndex);

    /// <summary>
    /// Rejects revisions within a range of direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first selected child.</param>
    /// <param name="endIndex">The exclusive selected end position.</param>
    /// <returns>The number of rejected revision elements.</returns>
    int RejectParagraphChildRange(DocxParagraph paragraph, int startIndex, int endIndex);

    /// <summary>
    /// Returns whether the document enables change tracking for subsequent edits.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>True when change tracking is enabled.</returns>
    bool IsTrackChangesEnabled(DocxDocument document);

    /// <summary>
    /// Enables or disables change tracking for subsequent edits.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether change tracking is enabled.</param>
    void SetTrackChangesEnabled(DocxDocument document, bool enabled);
}
