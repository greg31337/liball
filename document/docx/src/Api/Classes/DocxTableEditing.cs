using System.Globalization;

namespace docx;

/// <summary>
/// Creates, navigates, edits, extracts, and projects tables and their nested content.
/// </summary>
public class DocxTableEditing : IDocxTableEditing
{
    /// <summary>
    /// Creates a table editing service.
    /// </summary>
    public DocxTableEditing()
    {
    }

    /// <summary>
    /// Creates a table with the requested dimensions.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    public DocxTable CreateTable(int rows, int columns)
    {
        return DocxTable.Create(rows, columns);
    }

    /// <summary>
    /// Creates and appends a table to a body.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    public DocxTable AppendTableToBody(DocxBody body, int rows, int columns)
    {
        ValidateBody(body);
        DocxTable table = CreateTable(rows, columns);
        body.AddBlock(DocxBlock.FromTable(table));
        return table;
    }

    /// <summary>
    /// Creates and inserts a table at a body block position.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="blockIndex">The zero-based body block insertion position.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    public DocxTable InsertTableIntoBody(DocxBody body, int blockIndex, int rows, int columns)
    {
        ValidateBody(body);
        DocxTable table = CreateTable(rows, columns);
        body.InsertTable(blockIndex, table);
        return table;
    }

    /// <summary>
    /// Removes and returns the table at a body block position.
    /// </summary>
    /// <param name="body">The body to edit.</param>
    /// <param name="blockIndex">The zero-based body block position.</param>
    /// <returns>The removed table.</returns>
    public DocxTable RemoveTableFromBody(DocxBody body, int blockIndex)
    {
        ValidateBody(body);
        DocxBlock block = body.GetBlock(blockIndex);
        DocxTable? table = block.GetTable();

        if (table == null)
        {
            throw new ArgumentException("The selected body block is not a table.", "blockIndex");
        }

        body.RemoveBlockAt(blockIndex);
        return table;
    }

    /// <summary>
    /// Returns all rows in table order.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>A new list containing the rows.</returns>
    public List<DocxTableRow> GetRows(DocxTable table)
    {
        ValidateTable(table);
        List<DocxTableRow> rows = new List<DocxTableRow>();

        for (int index = 0; index < table.RowCount(); index = index + 1)
        {
            rows.Add(table.GetRow(index));
        }

        return rows;
    }

    /// <summary>
    /// Returns all cells in row order.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>A new list containing the cells.</returns>
    public List<DocxTableCell> GetCells(DocxTableRow row)
    {
        ValidateRow(row);
        List<DocxTableCell> cells = new List<DocxTableCell>();

        for (int index = 0; index < row.CellCount(); index = index + 1)
        {
            cells.Add(row.GetCell(index));
        }

        return cells;
    }

    /// <summary>
    /// Returns paragraph and nested-table blocks in cell order.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    public List<DocxBlock> GetCellBlocks(DocxTableCell cell)
    {
        ValidateCell(cell);
        return new List<DocxBlock>(cell.Blocks);
    }

    /// <summary>
    /// Appends a row with the requested cell count.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="cellCount">The number of cells to create.</param>
    /// <returns>The created row.</returns>
    public DocxTableRow AppendRow(DocxTable table, int cellCount)
    {
        ValidateTable(table);
        return table.AddRowWithCells(cellCount);
    }

    /// <summary>
    /// Inserts a row with the requested cell count.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based insertion position.</param>
    /// <param name="cellCount">The number of cells to create.</param>
    /// <returns>The created row.</returns>
    public DocxTableRow InsertRow(DocxTable table, int rowIndex, int cellCount)
    {
        ValidateTable(table);
        return table.InsertRowWithCells(rowIndex, cellCount);
    }

    /// <summary>
    /// Removes and returns a row.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <returns>The removed row.</returns>
    public DocxTableRow RemoveRow(DocxTable table, int rowIndex)
    {
        ValidateTable(table);
        return table.RemoveRowAt(rowIndex);
    }

    /// <summary>
    /// Moves a row to another position.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    public void MoveRow(DocxTable table, int sourceIndex, int destinationIndex)
    {
        ValidateTable(table);
        ValidateMoveIndexes(sourceIndex, destinationIndex, table.RowCount());
        table.MoveRow(sourceIndex, destinationIndex);
    }

    /// <summary>
    /// Appends an empty cell containing a required final paragraph.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <returns>The created cell.</returns>
    public DocxTableCell AppendCell(DocxTableRow row)
    {
        ValidateRow(row);
        DocxTableCell cell = row.AddNewCell();
        EnsureFinalParagraph(cell);
        return cell;
    }

    /// <summary>
    /// Inserts an empty cell containing a required final paragraph.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="cellIndex">The zero-based insertion position.</param>
    /// <returns>The created cell.</returns>
    public DocxTableCell InsertCell(DocxTableRow row, int cellIndex)
    {
        ValidateRow(row);
        DocxTableCell cell = row.InsertNewCell(cellIndex);
        EnsureFinalParagraph(cell);
        return cell;
    }

    /// <summary>
    /// Removes and returns a cell.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The removed cell.</returns>
    public DocxTableCell RemoveCell(DocxTableRow row, int cellIndex)
    {
        ValidateRow(row);
        return row.RemoveCellAt(cellIndex);
    }

    /// <summary>
    /// Moves a cell to another position.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    public void MoveCell(DocxTableRow row, int sourceIndex, int destinationIndex)
    {
        ValidateRow(row);
        ValidateMoveIndexes(sourceIndex, destinationIndex, row.CellCount());
        row.MoveCell(sourceIndex, destinationIndex);
    }

    /// <summary>
    /// Appends a paragraph to a cell.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    public void AppendParagraphToCell(DocxTableCell cell, DocxParagraph paragraph)
    {
        ValidateCell(cell);
        ValidateParagraph(paragraph);
        cell.InsertBlock(cell.BlockCount(), DocxBlock.FromParagraph(paragraph));
    }

    /// <summary>
    /// Inserts a paragraph at a cell block position.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block insertion position.</param>
    /// <param name="paragraph">The paragraph to insert.</param>
    public void InsertParagraphIntoCell(DocxTableCell cell, int blockIndex, DocxParagraph paragraph)
    {
        ValidateCell(cell);
        ValidateParagraph(paragraph);
        cell.InsertBlock(blockIndex, DocxBlock.FromParagraph(paragraph));
    }

    /// <summary>
    /// Creates and appends a nested table before the cell's final paragraph.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created nested table.</returns>
    public DocxTable AppendNestedTable(DocxTableCell cell, int rows, int columns)
    {
        ValidateCell(cell);
        EnsureFinalParagraph(cell);
        DocxTable table = CreateTable(rows, columns);
        cell.InsertBlock(cell.BlockCount() - 1, DocxBlock.FromTable(table));
        return table;
    }

    /// <summary>
    /// Creates and inserts a nested table at a cell block position.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block insertion position.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created nested table.</returns>
    public DocxTable InsertNestedTable(DocxTableCell cell, int blockIndex, int rows, int columns)
    {
        ValidateCell(cell);
        DocxTable table = CreateTable(rows, columns);
        cell.InsertBlock(blockIndex, DocxBlock.FromTable(table));
        EnsureFinalParagraph(cell);
        return table;
    }

    /// <summary>
    /// Removes a cell block while retaining a required final paragraph.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveCellBlock(DocxTableCell cell, int blockIndex)
    {
        ValidateCell(cell);
        DocxBlock block = cell.RemoveBlockAt(blockIndex);
        EnsureFinalParagraph(cell);
        return block;
    }

    /// <summary>
    /// Moves a paragraph or nested table within a cell.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="sourceIndex">The current zero-based block position.</param>
    /// <param name="destinationIndex">The resulting zero-based block position.</param>
    public void MoveCellBlock(DocxTableCell cell, int sourceIndex, int destinationIndex)
    {
        ValidateCell(cell);
        ValidateMoveIndexes(sourceIndex, destinationIndex, cell.BlockCount());
        cell.MoveBlock(sourceIndex, destinationIndex);
        EnsureFinalParagraph(cell);
    }

    /// <summary>
    /// Replaces grid column widths and applies them to the table and matching cells.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="widths">The grid column widths.</param>
    /// <param name="widthType">The WordprocessingML width type.</param>
    public void SetColumnWidths(DocxTable table, List<string> widths, string widthType)
    {
        ValidateTable(table);
        ValidateWidths(widths);

        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        table.SetGridColumnWidths(widths);
        table.SetWidthFromGrid(widthType);
        table.ApplyGridColumnWidthsToCells(widthType);
    }

    /// <summary>
    /// Sets whether a row repeats as a header row.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="repeat">Whether the row repeats.</param>
    public void SetHeaderRow(DocxTable table, int rowIndex, bool repeat)
    {
        ValidateTable(table);
        table.GetRow(rowIndex).SetTableHeader(repeat);
    }

    /// <summary>
    /// Merges adjacent cells by using one grid-spanning cell and retaining their content.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="startCellIndex">The first zero-based cell position.</param>
    /// <param name="cellCount">The number of cells to merge.</param>
    public void MergeCellsHorizontally(DocxTable table, int rowIndex, int startCellIndex, int cellCount)
    {
        ValidateTable(table);

        if (cellCount < 2)
        {
            throw new ArgumentOutOfRangeException("cellCount");
        }

        DocxTableRow row = table.GetRow(rowIndex);

        if (startCellIndex < 0 || startCellIndex > row.CellCount() - cellCount)
        {
            throw new ArgumentOutOfRangeException("startCellIndex");
        }

        DocxTableCell firstCell = row.GetCell(startCellIndex);
        EnsureFinalParagraph(firstCell);
        int mergedCellIndex = startCellIndex + 1;
        int remaining = cellCount - 1;

        while (remaining > 0)
        {
            DocxTableCell mergedCell = row.GetCell(mergedCellIndex);
            List<DocxBlock> blocks = new List<DocxBlock>(mergedCell.Blocks);

            foreach (DocxBlock block in blocks)
            {
                firstCell.InsertBlock(firstCell.BlockCount(), block);
            }

            row.RemoveCellAt(mergedCellIndex);
            remaining = remaining - 1;
        }

        EnsureFinalParagraph(firstCell);
        firstCell.SetGridSpan(cellCount.ToString(CultureInfo.InvariantCulture));
        firstCell.ClearHorizontalMerge();
    }

    /// <summary>
    /// Merges vertically aligned cells across adjacent rows.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="cellIndex">The zero-based cell position in every row.</param>
    /// <param name="startRowIndex">The first zero-based row position.</param>
    /// <param name="rowCount">The number of rows to merge.</param>
    public void MergeCellsVertically(DocxTable table, int cellIndex, int startRowIndex, int rowCount)
    {
        ValidateTable(table);

        if (rowCount < 2)
        {
            throw new ArgumentOutOfRangeException("rowCount");
        }

        if (startRowIndex < 0 || startRowIndex > table.RowCount() - rowCount)
        {
            throw new ArgumentOutOfRangeException("startRowIndex");
        }

        int rowOffset = 0;

        while (rowOffset < rowCount)
        {
            DocxTableRow row = table.GetRow(startRowIndex + rowOffset);

            if (cellIndex < 0 || cellIndex >= row.CellCount())
            {
                throw new ArgumentOutOfRangeException("cellIndex");
            }

            rowOffset = rowOffset + 1;
        }

        table.GetRow(startRowIndex).GetCell(cellIndex).SetVerticalMerge("restart");
        rowOffset = 1;

        while (rowOffset < rowCount)
        {
            table.GetRow(startRowIndex + rowOffset).GetCell(cellIndex).SetVerticalMerge("continue");
            rowOffset = rowOffset + 1;
        }
    }

    /// <summary>
    /// Extracts table text with default separators.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The extracted text.</returns>
    public string ExtractTableText(DocxTable table)
    {
        ValidateTable(table);
        return table.GetText();
    }

    /// <summary>
    /// Extracts table text with caller-supplied options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The extracted text.</returns>
    public string ExtractTableTextWithOptions(DocxTable table, DocxTextOptions options)
    {
        ValidateTable(table);
        ValidateOptions(options);
        return table.GetTextWithOptions(options);
    }

    /// <summary>
    /// Projects table text to a rectangular-aware row matrix using default options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The projected rows.</returns>
    public List<List<string>> ProjectTableToMatrix(DocxTable table)
    {
        ValidateTable(table);
        return table.ToTextMatrix();
    }

    /// <summary>
    /// Projects table text to a rectangular-aware row matrix using caller-supplied options.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The projected rows.</returns>
    public List<List<string>> ProjectTableToMatrixWithOptions(DocxTable table, DocxTextOptions options)
    {
        ValidateTable(table);
        ValidateOptions(options);
        return table.ToTextMatrixWithOptions(options);
    }

    internal static void EnsureFinalParagraph(DocxTableCell cell)
    {
        ValidateCell(cell);

        if (cell.BlockCount() == 0 || !cell.GetBlock(cell.BlockCount() - 1).IsParagraph())
        {
            cell.InsertBlock(cell.BlockCount(), DocxBlock.FromParagraph(new DocxParagraph("")));
        }
    }

    internal static void ValidateMoveIndexes(int sourceIndex, int destinationIndex, int count)
    {
        if (sourceIndex < 0 || sourceIndex >= count)
        {
            throw new ArgumentOutOfRangeException("sourceIndex");
        }

        if (destinationIndex < 0 || destinationIndex >= count)
        {
            throw new ArgumentOutOfRangeException("destinationIndex");
        }
    }

    internal static void ValidateWidths(List<string> widths)
    {
        if (widths == null)
        {
            throw new ArgumentNullException("widths");
        }

        if (widths.Count == 0)
        {
            throw new ArgumentException("At least one column width is required.", "widths");
        }

        foreach (string width in widths)
        {
            if (width == null)
            {
                throw new ArgumentNullException("widths");
            }
        }
    }

    internal static void ValidateTable(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }
    }

    internal static void ValidateRow(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }
    }

    internal static void ValidateCell(DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }
    }

    internal static void ValidateBody(DocxBody body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }
    }

    internal static void ValidateParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }
    }

    internal static void ValidateOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }
}
