namespace docx;

/// <summary>
/// Manages document sections and their default, even-page, and first-page header and footer stories.
/// </summary>
public interface IDocxSectionManagement
{
    /// <summary>
    /// Returns all sections in document order.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the sections.</returns>
    List<DocxSection> GetSections(DocxDocument document);

    /// <summary>
    /// Creates an unattached section.
    /// </summary>
    /// <returns>The created section.</returns>
    DocxSection CreateSection();

    /// <summary>
    /// Appends a section-break paragraph to the body.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section properties stored on the break.</param>
    /// <returns>The created section-break paragraph.</returns>
    DocxParagraph AppendSectionBreak(DocxDocument document, DocxSection section);

    /// <summary>
    /// Inserts a section-break paragraph at a body block position.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="blockIndex">The zero-based body block insertion position.</param>
    /// <param name="section">The section properties stored on the break.</param>
    /// <returns>The created section-break paragraph.</returns>
    DocxParagraph InsertSectionBreak(DocxDocument document, int blockIndex, DocxSection section);

    /// <summary>
    /// Sets the final section properties for a document.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The final section properties.</param>
    void SetFinalSection(DocxDocument document, DocxSection section);

    /// <summary>
    /// Removes an intermediate or final section and its owning break markup.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to remove.</param>
    /// <returns>True when the section was found and removed.</returns>
    bool RemoveSection(DocxDocument document, DocxSection section);

    /// <summary>
    /// Enables or disables distinct first-page header and footer stories for a section.
    /// </summary>
    /// <param name="section">The section to edit.</param>
    /// <param name="enabled">Whether first-page stories are enabled.</param>
    void SetFirstPageStories(DocxSection section, bool enabled);

    /// <summary>
    /// Enables or disables distinct even-page header and footer stories for the document.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether even-page stories are enabled.</param>
    void SetEvenPageStories(DocxDocument document, bool enabled);

    /// <summary>
    /// Returns all header parts discovered by the document.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the headers.</returns>
    List<DocxHeader> GetHeaders(DocxDocument document);

    /// <summary>
    /// Returns all footer parts discovered by the document.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the footers.</returns>
    List<DocxFooter> GetFooters(DocxDocument document);

    /// <summary>
    /// Creates a header part and assigns it to a section reference.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The created header.</returns>
    DocxHeader CreateHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Creates a footer part and assigns it to a section reference.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The created footer.</returns>
    DocxFooter CreateFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Assigns an existing document header to a section reference.
    /// </summary>
    /// <param name="document">The document containing the header.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <param name="header">The header to assign.</param>
    void AssignHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, DocxHeader header);

    /// <summary>
    /// Assigns an existing document footer to a section reference.
    /// </summary>
    /// <param name="document">The document containing the footer.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <param name="footer">The footer to assign.</param>
    void AssignFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, DocxFooter footer);

    /// <summary>
    /// Finds the header directly assigned to a section reference.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The directly assigned header, or null.</returns>
    DocxHeader? FindHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Finds the footer directly assigned to a section reference.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The directly assigned footer, or null.</returns>
    DocxFooter? FindFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Finds the effective header after following section inheritance.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The effective header, or null.</returns>
    DocxHeader? FindEffectiveHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Finds the effective footer after following section inheritance.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The effective footer, or null.</returns>
    DocxFooter? FindEffectiveFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Returns whether a header reference inherits from the previous section.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>True when the header is linked to the previous section.</returns>
    bool IsHeaderLinkedToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Returns whether a footer reference inherits from the previous section.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>True when the footer is linked to the previous section.</returns>
    bool IsFooterLinkedToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Links a header reference to the previous section.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    void LinkHeaderToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Links a footer reference to the previous section.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    void LinkFooterToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind);

    /// <summary>
    /// Removes a header part, its relationship, and direct section references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="header">The header to remove.</param>
    /// <returns>True when the header belonged to the document and was removed.</returns>
    bool RemoveHeader(DocxDocument document, DocxHeader header);

    /// <summary>
    /// Removes a footer part, its relationship, and direct section references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footer">The footer to remove.</param>
    /// <returns>True when the footer belonged to the document and was removed.</returns>
    bool RemoveFooter(DocxDocument document, DocxFooter footer);

    /// <summary>
    /// Returns paragraph and table blocks in header order.
    /// </summary>
    /// <param name="header">The header to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    List<DocxBlock> GetHeaderBlocks(DocxHeader header);

    /// <summary>
    /// Appends a block to a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="block">The block to append.</param>
    void AppendHeaderBlock(DocxHeader header, DocxBlock block);

    /// <summary>
    /// Inserts a block into a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="blockIndex">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    void InsertHeaderBlock(DocxHeader header, int blockIndex, DocxBlock block);

    /// <summary>
    /// Moves a block within a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    void MoveHeaderBlock(DocxHeader header, int sourceIndex, int destinationIndex);

    /// <summary>
    /// Removes a block from a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveHeaderBlock(DocxHeader header, int blockIndex);

    /// <summary>
    /// Returns paragraph and table blocks in footer order.
    /// </summary>
    /// <param name="footer">The footer to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    List<DocxBlock> GetFooterBlocks(DocxFooter footer);

    /// <summary>
    /// Appends a block to a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="block">The block to append.</param>
    void AppendFooterBlock(DocxFooter footer, DocxBlock block);

    /// <summary>
    /// Inserts a block into a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="blockIndex">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    void InsertFooterBlock(DocxFooter footer, int blockIndex, DocxBlock block);

    /// <summary>
    /// Moves a block within a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    void MoveFooterBlock(DocxFooter footer, int sourceIndex, int destinationIndex);

    /// <summary>
    /// Removes a block from a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveFooterBlock(DocxFooter footer, int blockIndex);
}
