namespace docx;

/// <summary>
/// Creates, navigates, edits, extracts, and projects tables and their nested content.
/// </summary>
public interface IDocxTableEditing
{
    /// <summary>
    /// Creates a table with the requested dimensions.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    DocxTable CreateTable(int rows, int columns);

    /// <summary>
    /// Creates and appends a table to a body.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    DocxTable AppendTableToBody(DocxBody body, int rows, int columns);

    /// <summary>
    /// Creates and inserts a table at a body block position.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="blockIndex">The zero-based body block insertion position.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    DocxTable InsertTableIntoBody(DocxBody body, int blockIndex, int rows, int columns);

    /// <summary>
    /// Removes and returns the table at a body block position.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="blockIndex">The zero-based body block position.</param>
    /// <returns>The removed table.</returns>
    DocxTable RemoveTableFromBody(DocxBody body, int blockIndex);

    /// <summary>
    /// Returns all rows in table order.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>A new list containing the rows.</returns>
    List<DocxTableRow> GetRows(DocxTable table);

    /// <summary>
    /// Returns all cells in row order.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>A new list containing the cells.</returns>
    List<DocxTableCell> GetCells(DocxTableRow row);

    /// <summary>
    /// Returns paragraph and nested-table blocks in cell order.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    List<DocxBlock> GetCellBlocks(DocxTableCell cell);

    /// <summary>
    /// Appends a row with the requested cell count.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="cellCount">The number of cells to create.</param>
    /// <returns>The created row.</returns>
    DocxTableRow AppendRow(DocxTable table, int cellCount);

    /// <summary>
    /// Inserts a row with the requested cell count.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based insertion position.</param>
    /// <param name="cellCount">The number of cells to create.</param>
    /// <returns>The created row.</returns>
    DocxTableRow InsertRow(DocxTable table, int rowIndex, int cellCount);

    /// <summary>
    /// Removes and returns a row.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <returns>The removed row.</returns>
    DocxTableRow RemoveRow(DocxTable table, int rowIndex);

    /// <summary>
    /// Moves a row to another position.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    void MoveRow(DocxTable table, int sourceIndex, int destinationIndex);

    /// <summary>
    /// Appends an empty cell containing a required final paragraph.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <returns>The created cell.</returns>
    DocxTableCell AppendCell(DocxTableRow row);

    /// <summary>
    /// Inserts an empty cell containing a required final paragraph.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="cellIndex">The zero-based insertion position.</param>
    /// <returns>The created cell.</returns>
    DocxTableCell InsertCell(DocxTableRow row, int cellIndex);

    /// <summary>
    /// Removes and returns a cell.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The removed cell.</returns>
    DocxTableCell RemoveCell(DocxTableRow row, int cellIndex);

    /// <summary>
    /// Moves a cell to another position.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    void MoveCell(DocxTableRow row, int sourceIndex, int destinationIndex);

    /// <summary>
    /// Appends a paragraph to a cell.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    void AppendParagraphToCell(DocxTableCell cell, DocxParagraph paragraph);

    /// <summary>
    /// Inserts a paragraph at a cell block position.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block insertion position.</param>
    /// <param name="paragraph">The paragraph to insert.</param>
    void InsertParagraphIntoCell(DocxTableCell cell, int blockIndex, DocxParagraph paragraph);

    /// <summary>
    /// Creates and appends a nested table before the cell's final paragraph.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created nested table.</returns>
    DocxTable AppendNestedTable(DocxTableCell cell, int rows, int columns);

    /// <summary>
    /// Creates and inserts a nested table at a cell block position.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block insertion position.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created nested table.</returns>
    DocxTable InsertNestedTable(DocxTableCell cell, int blockIndex, int rows, int columns);

    /// <summary>
    /// Removes a cell block while retaining a required final paragraph.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    DocxBlock RemoveCellBlock(DocxTableCell cell, int blockIndex);

    /// <summary>
    /// Moves a paragraph or nested table within a cell.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="sourceIndex">The current zero-based block position.</param>
    /// <param name="destinationIndex">The resulting zero-based block position.</param>
    void MoveCellBlock(DocxTableCell cell, int sourceIndex, int destinationIndex);

    /// <summary>
    /// Replaces grid column widths and applies them to the table and matching cells.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="widths">The grid column widths.</param>
    /// <param name="widthType">The WordprocessingML width type.</param>
    void SetColumnWidths(DocxTable table, List<string> widths, string widthType);

    /// <summary>
    /// Sets whether a row repeats as a header row.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="repeat">Whether the row repeats.</param>
    void SetHeaderRow(DocxTable table, int rowIndex, bool repeat);

    /// <summary>
    /// Merges adjacent cells by using one grid-spanning cell and retaining their content.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="startCellIndex">The first zero-based cell position.</param>
    /// <param name="cellCount">The number of cells to merge.</param>
    void MergeCellsHorizontally(DocxTable table, int rowIndex, int startCellIndex, int cellCount);

    /// <summary>
    /// Merges vertically aligned cells across adjacent rows.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="cellIndex">The zero-based cell position in every row.</param>
    /// <param name="startRowIndex">The first zero-based row position.</param>
    /// <param name="rowCount">The number of rows to merge.</param>
    void MergeCellsVertically(DocxTable table, int cellIndex, int startRowIndex, int rowCount);

    /// <summary>
    /// Extracts table text with default separators.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The extracted text.</returns>
    string ExtractTableText(DocxTable table);

    /// <summary>
    /// Extracts table text with caller-supplied options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The extracted text.</returns>
    string ExtractTableTextWithOptions(DocxTable table, DocxTextOptions options);

    /// <summary>
    /// Projects table text to a rectangular-aware row matrix using default options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The projected rows.</returns>
    List<List<string>> ProjectTableToMatrix(DocxTable table);

    /// <summary>
    /// Projects table text to a rectangular-aware row matrix using caller-supplied options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The projected rows.</returns>
    List<List<string>> ProjectTableToMatrixWithOptions(DocxTable table, DocxTextOptions options);
}
