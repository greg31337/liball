namespace odf;

/// <summary>
/// Provides typed lookup and editing for tables in any OpenDocument content kind.
/// </summary>
public class OdfTableEditor : IOdfTableEditor
{
    /// <summary>Initializes a table editor.</summary>
    public OdfTableEditor()
    {
    }

    /// <summary>Gets the document tables in source order.</summary>
    public OdfTable[] GetTables(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table lookup.");
        }
        OdfTable[] tables = new OdfTable[document.Tables.Length];
        int index = 0;
        while (index < document.Tables.Length)
        {
            tables[index] = document.Tables[index];
            index = index + 1;
        }
        return tables;
    }

    /// <summary>Finds an unambiguously named table.</summary>
    public OdfTable? FindTable(OdfDocument document, string name)
    {
        return TableDocumentModel.FindTable(document, name);
    }

    /// <summary>Gets the row run containing a logical row.</summary>
    public OdfRow? GetTableRow(OdfTable table, long rowIndex)
    {
        return TableDocumentModel.GetRow(table, rowIndex);
    }

    /// <summary>Gets the column run containing a logical column.</summary>
    public OdfColumn? GetTableColumn(OdfTable table, long columnIndex)
    {
        return TableDocumentModel.GetColumn(table, columnIndex);
    }

    /// <summary>Gets the cell run containing a logical cell.</summary>
    public OdfCell? GetTableCell(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        return TableDocumentModel.GetCell(table, rowIndex, columnIndex);
    }

    /// <summary>Inserts logical rows.</summary>
    public OdfTable InsertTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        return TableStructureEditor.InsertRows(
            document,
            table,
            rowIndex,
            rowCount);
    }

    /// <summary>Deletes logical rows.</summary>
    public OdfTable DeleteTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        return TableStructureEditor.DeleteRows(
            document,
            table,
            rowIndex,
            rowCount);
    }

    /// <summary>Copies logical rows.</summary>
    public OdfTable CopyTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex)
    {
        return TableStructureEditor.CopyRows(
            document,
            table,
            rowIndex,
            rowCount,
            destinationRowIndex);
    }

    /// <summary>Moves logical rows.</summary>
    public OdfTable MoveTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex)
    {
        return TableStructureEditor.MoveRows(
            document,
            table,
            rowIndex,
            rowCount,
            destinationRowIndex);
    }

    /// <summary>Inserts logical columns.</summary>
    public OdfTable InsertTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        return TableStructureEditor.InsertColumns(
            document,
            table,
            columnIndex,
            columnCount);
    }

    /// <summary>Deletes logical columns.</summary>
    public OdfTable DeleteTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        return TableStructureEditor.DeleteColumns(
            document,
            table,
            columnIndex,
            columnCount);
    }

    /// <summary>Copies logical columns.</summary>
    public OdfTable CopyTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex)
    {
        return TableStructureEditor.CopyColumns(
            document,
            table,
            columnIndex,
            columnCount,
            destinationColumnIndex);
    }

    /// <summary>Moves logical columns.</summary>
    public OdfTable MoveTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex)
    {
        return TableStructureEditor.MoveColumns(
            document,
            table,
            columnIndex,
            columnCount,
            destinationColumnIndex);
    }

    /// <summary>Inserts cells into one logical row.</summary>
    public OdfTable InsertTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount)
    {
        return TableStructureEditor.InsertCells(
            document,
            table,
            rowIndex,
            columnIndex,
            cellCount);
    }

    /// <summary>Deletes cells from one logical row.</summary>
    public OdfTable DeleteTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount)
    {
        return TableStructureEditor.DeleteCells(
            document,
            table,
            rowIndex,
            columnIndex,
            cellCount);
    }

    /// <summary>Copies cells within one logical row.</summary>
    public OdfTable CopyTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex)
    {
        return TableStructureEditor.CopyCells(
            document,
            table,
            rowIndex,
            columnIndex,
            cellCount,
            destinationColumnIndex);
    }

    /// <summary>Moves cells within one logical row.</summary>
    public OdfTable MoveTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex)
    {
        return TableStructureEditor.MoveCells(
            document,
            table,
            rowIndex,
            columnIndex,
            cellCount,
            destinationColumnIndex);
    }

    /// <summary>Copies a rectangular cell range.</summary>
    public OdfTable CopyTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        return TableStructureEditor.CopyRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
    }

    /// <summary>Moves a rectangular cell range.</summary>
    public OdfTable MoveTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        return TableStructureEditor.MoveRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
    }

    /// <summary>Merges a rectangular cell range.</summary>
    public OdfTable MergeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        return TableMergeEditor.MergeRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
    }

    /// <summary>Unmerges the range beginning at a merge origin.</summary>
    public OdfTable UnmergeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        return TableMergeEditor.UnmergeRange(
            document,
            table,
            rowIndex,
            columnIndex);
    }

    /// <summary>Sets a matrix formula over a rectangular range.</summary>
    public OdfTable SetTableMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula)
    {
        return TableMergeEditor.SetMatrixFormula(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            formula);
    }

    /// <summary>Sets an array formula over a rectangular range.</summary>
    public OdfTable SetTableArrayFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula)
    {
        return TableMergeEditor.SetArrayFormula(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            formula);
    }

    /// <summary>Clears a matrix or array formula at its origin.</summary>
    public OdfTable ClearTableMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        return TableMergeEditor.ClearMatrixFormula(
            document,
            table,
            rowIndex,
            columnIndex);
    }

    /// <summary>Clears cell contents while retaining formatting.</summary>
    public OdfTable ClearTableContents(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        return TableRangeEditor.ClearContents(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
    }

    /// <summary>Clears cell formatting while retaining contents.</summary>
    public OdfTable ClearTableFormats(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        return TableRangeEditor.ClearFormats(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
    }

    /// <summary>Transposes a rectangular cell range.</summary>
    public OdfTable TransposeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        return TableRangeEditor.TransposeRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
    }

    /// <summary>Fills a range with a numeric arithmetic series.</summary>
    public OdfTable FillTableSeries(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        double startValue,
        double stepValue)
    {
        return TableRangeEditor.FillSeries(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            startValue,
            stepValue);
    }

    /// <summary>Repeats a seed range over a destination range.</summary>
    public OdfTable CopyTableSeries(
        OdfDocument document,
        OdfTable table,
        long sourceRowIndex,
        long sourceColumnIndex,
        long sourceRowCount,
        long sourceColumnCount,
        long destinationRowIndex,
        long destinationColumnIndex,
        long destinationRowCount,
        long destinationColumnCount)
    {
        return TableRangeEditor.CopySeries(
            document,
            table,
            sourceRowIndex,
            sourceColumnIndex,
            sourceRowCount,
            sourceColumnCount,
            destinationRowIndex,
            destinationColumnIndex,
            destinationRowCount,
            destinationColumnCount);
    }

    /// <summary>Searches cell values and formulas in a range.</summary>
    public OdfSearchMatch[] SearchTableRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string searchValue,
        bool matchCase,
        bool exactMatch)
    {
        return TableRangeEditor.SearchRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            searchValue,
            matchCase,
            exactMatch);
    }

    /// <summary>Replaces matching cell values and formulas in a range.</summary>
    public OdfSearchMatch[] ReplaceTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string searchValue,
        string replacementValue,
        bool matchCase,
        bool replaceAll)
    {
        return TableRangeEditor.ReplaceRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            searchValue,
            replacementValue,
            matchCase,
            replaceAll);
    }
}
