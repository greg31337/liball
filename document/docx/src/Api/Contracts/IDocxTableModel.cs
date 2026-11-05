namespace docx;

/// <summary>
/// Provides access to parsed table data and row repetition.
/// </summary>
public interface IDocxTableModel
{
    /// <summary>
    /// Returns whether direct table formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasFormat();

    /// <summary>
    /// Returns the direct table format model.
    /// </summary>
    /// <returns>The table format model.</returns>
    DocxTableFormat GetFormat();

    /// <summary>
    /// Replaces the direct table format model.
    /// </summary>
    /// <param name="format">The replacement table format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    void SetFormat(DocxTableFormat format);

    /// <summary>
    /// Returns the table grid definition.
    /// </summary>
    /// <returns>The table grid definition.</returns>
    DocxTableGrid GetGrid();

    /// <summary>
    /// Returns a table row by position.
    /// </summary>
    /// <param name="index">The zero-based row position.</param>
    /// <returns>The requested table row.</returns>
    DocxTableRow GetRow(int index);

    /// <summary>
    /// Repeats a template row for each supplied data row.
    /// </summary>
    /// <param name="templateRowIndex">The zero-based template row position.</param>
    /// <param name="data">The values used to populate repeated rows.</param>
    /// <returns>The number of rows inserted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    int RepeatRow(int templateRowIndex, DocxTableData data);
}
