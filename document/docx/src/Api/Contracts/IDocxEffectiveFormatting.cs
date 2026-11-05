namespace docx;

/// <summary>
/// Calculates direct, inherited, and effective formatting in document context.
/// </summary>
public interface IDocxEffectiveFormatting
{
    /// <summary>
    /// Calculates formatting for a run in its paragraph context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="paragraph">The containing paragraph.</param>
    /// <param name="run">The run to inspect.</param>
    /// <returns>The separated run formatting.</returns>
    DocxEffectiveRunFormatting GetRunFormatting(DocxDocument document, DocxParagraph paragraph, DocxRun run);

    /// <summary>
    /// Calculates formatting for a paragraph.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The separated paragraph formatting.</returns>
    DocxEffectiveParagraphFormatting GetParagraphFormatting(DocxDocument document, DocxParagraph paragraph);

    /// <summary>
    /// Calculates formatting for a table.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The separated table formatting.</returns>
    DocxEffectiveTableFormatting GetTableFormatting(DocxDocument document, DocxTable table);

    /// <summary>
    /// Calculates formatting for a row in its table context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The containing table.</param>
    /// <param name="row">The row to inspect.</param>
    /// <returns>The separated row formatting.</returns>
    DocxEffectiveTableRowFormatting GetRowFormatting(DocxDocument document, DocxTable table, DocxTableRow row);

    /// <summary>
    /// Calculates formatting for a cell in its table context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The separated cell formatting.</returns>
    DocxEffectiveTableCellFormatting GetCellFormatting(DocxDocument document, DocxTable table, int rowIndex, int cellIndex);
}
