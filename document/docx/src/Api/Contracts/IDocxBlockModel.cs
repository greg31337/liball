namespace docx;

/// <summary>
/// Provides typed access to one block in a document body.
/// </summary>
public interface IDocxBlockModel
{
    /// <summary>
    /// Returns whether this block contains a paragraph.
    /// </summary>
    /// <returns><c>true</c> when the block contains a paragraph.</returns>
    bool IsParagraph();

    /// <summary>
    /// Returns whether this block contains a table.
    /// </summary>
    /// <returns><c>true</c> when the block contains a table.</returns>
    bool IsTable();

    /// <summary>
    /// Returns whether this block contains a content control.
    /// </summary>
    /// <returns><c>true</c> when the block contains a content control.</returns>
    bool IsContentControl();

    /// <summary>
    /// Returns whether this block contains preserved block-level markup that is not otherwise modeled.
    /// </summary>
    /// <returns><c>true</c> when the block contains preserved markup.</returns>
    bool IsPreserved();

    /// <summary>
    /// Returns the paragraph stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The paragraph, or null.</returns>
    DocxParagraph? GetParagraph();

    /// <summary>
    /// Returns the table stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The table, or null.</returns>
    DocxTable? GetTable();

    /// <summary>
    /// Returns the content control stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The content control, or null.</returns>
    DocxContentControl? GetContentControl();

    /// <summary>
    /// Returns the preserved block-level XML, or an empty string for a typed block.
    /// </summary>
    /// <returns>The preserved XML, or an empty string.</returns>
    string GetPreservedXml();
}
