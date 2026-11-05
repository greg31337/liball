namespace docx;

/// <summary>
/// Provides ordered navigation and editing for the main document body.
/// </summary>
public interface IDocxBodyModel
{
    /// <summary>
    /// Returns all body blocks in document order.
    /// </summary>
    /// <returns>A new list containing the body blocks.</returns>
    List<DocxBlock> GetBlocks();

    /// <summary>
    /// Returns the number of body blocks.
    /// </summary>
    /// <returns>The block count.</returns>
    int BlockCount();

    /// <summary>
    /// Returns a body block by position.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The requested block.</returns>
    DocxBlock GetBlock(int index);

    /// <summary>
    /// Returns all paragraphs in body order.
    /// </summary>
    /// <returns>A new list containing the body paragraphs.</returns>
    List<DocxParagraph> GetParagraphs();

    /// <summary>
    /// Returns the number of body paragraphs.
    /// </summary>
    /// <returns>The paragraph count.</returns>
    int ParagraphCount();

    /// <summary>
    /// Returns a paragraph by its position among body paragraphs.
    /// </summary>
    /// <param name="index">The zero-based paragraph position.</param>
    /// <returns>The requested paragraph.</returns>
    DocxParagraph GetParagraph(int index);

    /// <summary>
    /// Returns all tables in body order.
    /// </summary>
    /// <returns>A new list containing the body tables.</returns>
    List<DocxTable> GetTables();

    /// <summary>
    /// Returns the number of body tables.
    /// </summary>
    /// <returns>The table count.</returns>
    int TableCount();

    /// <summary>
    /// Returns a table by its position among body tables.
    /// </summary>
    /// <param name="index">The zero-based table position.</param>
    /// <returns>The requested table.</returns>
    DocxTable GetTable(int index);

    /// <summary>
    /// Returns all direct body content controls in body order.
    /// </summary>
    /// <returns>A new list containing the direct body content controls.</returns>
    List<DocxContentControl> GetContentControls();

    /// <summary>
    /// Returns the number of direct body content controls.
    /// </summary>
    /// <returns>The content-control count.</returns>
    int ContentControlCount();

    /// <summary>
    /// Returns a direct body content control by its position among content controls.
    /// </summary>
    /// <param name="index">The zero-based content-control position.</param>
    /// <returns>The requested content control.</returns>
    DocxContentControl GetContentControl(int index);

    /// <summary>
    /// Returns all body sections in document order, including final section properties.
    /// </summary>
    /// <returns>A new list containing the body sections.</returns>
    List<DocxSection> GetSections();

    /// <summary>
    /// Returns the number of body sections, including final section properties.
    /// </summary>
    /// <returns>The section count.</returns>
    int SectionCount();

    /// <summary>
    /// Returns a section by position.
    /// </summary>
    /// <param name="index">The zero-based section position.</param>
    /// <returns>The requested section.</returns>
    DocxSection GetSection(int index);

    /// <summary>
    /// Returns whether the body has final section properties.
    /// </summary>
    /// <returns><c>true</c> when final section properties are present.</returns>
    bool HasFinalSection();

    /// <summary>
    /// Returns the final section properties, or null when they are absent.
    /// </summary>
    /// <returns>The final section properties, or null.</returns>
    DocxSection? GetFinalSection();

    /// <summary>
    /// Appends a block to the body.
    /// </summary>
    /// <param name="block">The block to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    void AddBlock(DocxBlock block);

    /// <summary>
    /// Inserts a block at a body position.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    void InsertBlock(int index, DocxBlock block);

    /// <summary>
    /// Inserts a paragraph at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="paragraph">The paragraph to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>paragraph</c> is null.</exception>
    void InsertParagraph(int index, DocxParagraph paragraph);

    /// <summary>
    /// Inserts a table at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="table">The table to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    void InsertTable(int index, DocxTable table);

    /// <summary>
    /// Inserts a content control at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="contentControl">The content control to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    void InsertContentControl(int index, DocxContentControl contentControl);

    /// <summary>
    /// Replaces a body block.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="block">The replacement block.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    DocxBlock ReplaceBlock(int index, DocxBlock block);

    /// <summary>
    /// Replaces a body block with a paragraph.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="paragraph">The replacement paragraph.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>paragraph</c> is null.</exception>
    DocxBlock ReplaceBlockWithParagraph(int index, DocxParagraph paragraph);

    /// <summary>
    /// Replaces a body block with a table.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="table">The replacement table.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    DocxBlock ReplaceBlockWithTable(int index, DocxTable table);

    /// <summary>
    /// Replaces a body block with a content control.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="contentControl">The replacement content control.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    DocxBlock ReplaceBlockWithContentControl(int index, DocxContentControl contentControl);

    /// <summary>
    /// Moves a block to another body position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based block position.</param>
    /// <param name="destinationIndex">The resulting zero-based block position.</param>
    void MoveBlock(int sourceIndex, int destinationIndex);

    /// <summary>
    /// Removes a body block.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveBlockAt(int index);

    /// <summary>
    /// Sets or replaces the final section properties.
    /// </summary>
    /// <param name="section">The final section properties.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>section</c> is null.</exception>
    void SetFinalSection(DocxSection section);

    /// <summary>
    /// Removes the final section properties.
    /// </summary>
    /// <returns><c>true</c> when final section properties were removed.</returns>
    bool RemoveFinalSection();
}
