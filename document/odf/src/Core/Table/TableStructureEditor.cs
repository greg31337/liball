using System;
using System.Collections.Generic;

namespace odf;

internal class TableCellSequence
{
    internal OdfRow? Row;
    internal OdfNode[] Nodes = [];
}

internal static class TableStructureEditor
{
    internal static OdfTable InsertRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        RequireInsertion(rowIndex, rowCount, table.RowCount, "row");
        TableReferenceUpdater.AdjustRows(
            document,
            table,
            rowIndex,
            0,
            rowCount);
        InsertRowNodes(table, rowIndex, [CreateBlankRow(table, rowCount)]);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable DeleteRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        RequireRange(rowIndex, rowCount, table.RowCount, "row");
        IsolateRows(table, rowIndex, rowCount);
        RemoveSelectedRows(table, rowIndex, rowCount);
        TableReferenceUpdater.AdjustRows(
            document,
            table,
            rowIndex,
            rowCount,
            0);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable CopyRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex)
    {
        RequireRange(rowIndex, rowCount, table.RowCount, "row");
        RequireDestination(destinationRowIndex, table.RowCount, "row");
        OdfNode[] nodes = CaptureRows(table, rowIndex, rowCount);
        long delta = destinationRowIndex - rowIndex;
        RebaseNodes(nodes, table.Name, delta, 0);
        TableReferenceUpdater.AdjustRows(
            document,
            table,
            destinationRowIndex,
            0,
            rowCount);
        InsertRowNodes(table, destinationRowIndex, nodes);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable MoveRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex)
    {
        RequireRange(rowIndex, rowCount, table.RowCount, "row");
        RequireDestination(destinationRowIndex, table.RowCount, "row");
        if (destinationRowIndex >= rowIndex
            && destinationRowIndex <= rowIndex + rowCount)
        {
            return table;
        }
        OdfNode[] nodes = CaptureRows(table, rowIndex, rowCount);
        RemoveSelectedRows(table, rowIndex, rowCount);
        long finalIndex = destinationRowIndex > rowIndex
            ? destinationRowIndex - rowCount
            : destinationRowIndex;
        OdfTable shortened = ReloadTable(document, table.Node);
        InsertRowNodes(shortened, finalIndex, nodes);
        TableReferenceUpdater.MoveRows(
            document,
            shortened,
            rowIndex,
            rowCount,
            destinationRowIndex);
        return RefreshTable(document, shortened.Node);
    }

    internal static OdfTable InsertColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        RequireInsertion(
            columnIndex,
            columnCount,
            table.ColumnCount,
            "column");
        InsertColumnNodes(
            table,
            columnIndex,
            [CreateBlankColumn(columnCount)]);
        InsertCellsInAllRows(table, columnIndex, columnCount);
        TableReferenceUpdater.AdjustColumns(
            document,
            table,
            columnIndex,
            0,
            columnCount);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable DeleteColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        RequireRange(
            columnIndex,
            columnCount,
            table.ColumnCount,
            "column");
        RemoveSelectedColumns(table, columnIndex, columnCount);
        RemoveCellsInAllRows(table, columnIndex, columnCount);
        TableReferenceUpdater.AdjustColumns(
            document,
            table,
            columnIndex,
            columnCount,
            0);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable CopyColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex)
    {
        RequireRange(
            columnIndex,
            columnCount,
            table.ColumnCount,
            "column");
        RequireDestination(
            destinationColumnIndex,
            table.ColumnCount,
            "column");
        OdfNode[] columns = CaptureColumns(
            table,
            columnIndex,
            columnCount);
        TableCellSequence[] cells = CaptureCellsInRows(
            table,
            columnIndex,
            columnCount);
        long delta = destinationColumnIndex - columnIndex;
        RebaseNodes(columns, table.Name, 0, delta);
        RebaseCellSequences(cells, table.Name, 0, delta);
        TableReferenceUpdater.AdjustColumns(
            document,
            table,
            destinationColumnIndex,
            0,
            columnCount);
        InsertColumnNodes(table, destinationColumnIndex, columns);
        InsertCapturedCells(cells, destinationColumnIndex);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable MoveColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex)
    {
        RequireRange(
            columnIndex,
            columnCount,
            table.ColumnCount,
            "column");
        RequireDestination(
            destinationColumnIndex,
            table.ColumnCount,
            "column");
        if (destinationColumnIndex >= columnIndex
            && destinationColumnIndex <= columnIndex + columnCount)
        {
            return table;
        }
        OdfNode[] columns = CaptureColumns(
            table,
            columnIndex,
            columnCount);
        TableCellSequence[] cells = CaptureCellsInRows(
            table,
            columnIndex,
            columnCount);
        RemoveSelectedColumns(table, columnIndex, columnCount);
        RemoveCellsInAllRows(table, columnIndex, columnCount);
        long finalIndex = destinationColumnIndex > columnIndex
            ? destinationColumnIndex - columnCount
            : destinationColumnIndex;
        OdfTable shortened = ReloadTable(document, table.Node);
        InsertColumnNodes(shortened, finalIndex, columns);
        InsertCapturedCellsByOrder(
            shortened,
            cells,
            finalIndex);
        TableReferenceUpdater.MoveColumns(
            document,
            shortened,
            columnIndex,
            columnCount,
            destinationColumnIndex);
        return RefreshTable(document, shortened.Node);
    }

    internal static OdfTable InsertCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount)
    {
        RequireCellInsertion(table, rowIndex, columnIndex, cellCount);
        OdfRow row = TableRepeatRunEditor.SplitRowForEdit(table, rowIndex);
        EnsureRowWidth(row, columnIndex);
        InsertCellNodes(row, columnIndex, [CreateBlankCell(cellCount)]);
        TableReferenceUpdater.AdjustCells(
            document,
            table,
            rowIndex,
            columnIndex,
            0,
            cellCount);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable DeleteCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount)
    {
        RequireCellRange(table, rowIndex, columnIndex, cellCount);
        OdfRow row = TableRepeatRunEditor.SplitRowForEdit(table, rowIndex);
        RemoveCellsFromRow(row, columnIndex, cellCount);
        TableReferenceUpdater.AdjustCells(
            document,
            table,
            rowIndex,
            columnIndex,
            cellCount,
            0);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable CopyCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex)
    {
        return CopyRange(
            document,
            table,
            rowIndex,
            columnIndex,
            1,
            cellCount,
            rowIndex,
            destinationColumnIndex);
    }

    internal static OdfTable MoveCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex)
    {
        return MoveRange(
            document,
            table,
            rowIndex,
            columnIndex,
            1,
            cellCount,
            rowIndex,
            destinationColumnIndex);
    }

    internal static OdfTable CopyRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        RequireRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
        OdfNode[] cells = CaptureRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        RebaseNodes(
            cells,
            table.Name,
            destinationRowIndex - rowIndex,
            destinationColumnIndex - columnIndex);
        ReplaceRectangle(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            rowCount,
            columnCount,
            cells);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable MoveRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        RequireRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
        OdfNode[] cells = CaptureRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        ClearRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        table = ReloadTable(document, table.Node);
        ReplaceRectangle(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            rowCount,
            columnCount,
            cells);
        TableReferenceUpdater.MoveRange(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
        return RefreshTable(document, table.Node);
    }

    static void RequireInsertion(
        long index,
        long count,
        long available,
        string kind)
    {
        if (index < 0 || index > available || count <= 0)
        {
            throw new OdfEditException(
                "The " + kind + " insertion range is outside the table.");
        }
    }

    static void RequireRange(
        long index,
        long count,
        long available,
        string kind)
    {
        if (index < 0
            || count <= 0
            || index > available
            || count > available - index)
        {
            throw new OdfEditException(
                "The " + kind + " range is outside the table.");
        }
    }

    static void RequireDestination(
        long index,
        long available,
        string kind)
    {
        if (index < 0 || index > available)
        {
            throw new OdfEditException(
                "The destination " + kind + " index is outside the table.");
        }
    }

    static void RequireCellInsertion(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long count)
    {
        RequireRange(rowIndex, 1, table.RowCount, "row");
        RequireInsertion(columnIndex, count, table.ColumnCount, "cell");
    }

    static void RequireCellRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long count)
    {
        RequireRange(rowIndex, 1, table.RowCount, "row");
        RequireRange(columnIndex, count, table.ColumnCount, "cell");
    }

    static void RequireRectangle(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        RequireRange(rowIndex, rowCount, table.RowCount, "row");
        RequireRange(columnIndex, columnCount, table.ColumnCount, "column");
        RequireRange(
            destinationRowIndex,
            rowCount,
            table.RowCount,
            "destination row");
        RequireRange(
            destinationColumnIndex,
            columnCount,
            table.ColumnCount,
            "destination column");
    }

    static void IsolateRows(
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        TableRepeatRunEditor.SplitRowForEdit(table, rowIndex);
        if (rowIndex + rowCount < table.RowCount)
        {
            TableRepeatRunEditor.SplitRowForEdit(
                table,
                rowIndex + rowCount);
        }
    }

    static OdfNode[] CaptureRows(
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        IsolateRows(table, rowIndex, rowCount);
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < table.Rows.Length)
        {
            OdfRow row = table.Rows[index];
            if (row.StartIndex >= rowIndex
                && row.StartIndex < rowIndex + rowCount
                && row.Node != null)
            {
                result.Add(CloneNode(row.Node));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void RemoveSelectedRows(
        OdfTable table,
        long rowIndex,
        long rowCount)
    {
        IsolateRows(table, rowIndex, rowCount);
        int index = 0;
        while (index < table.Rows.Length)
        {
            OdfRow row = table.Rows[index];
            if (row.StartIndex >= rowIndex
                && row.StartIndex < rowIndex + rowCount
                && row.Node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(row.Node);
            }
            index = index + 1;
        }
    }

    static void InsertRowNodes(
        OdfTable table,
        long rowIndex,
        OdfNode[] nodes)
    {
        OdfNode tableNode = RequireTableNode(table);
        OdfNode parent = tableNode;
        int insertionIndex = tableNode.Children.Length;
        if (rowIndex < table.RowCount)
        {
            OdfRow? row = TableDocumentModel.GetRow(table, rowIndex);
            if (row != null && row.Node != null && row.Node.Parent != null)
            {
                parent = row.Node.Parent;
                insertionIndex = FindNode(parent.Children, row.Node);
            }
        }
        else if (table.Rows.Length > 0)
        {
            OdfNode? last = table.Rows[table.Rows.Length - 1].Node;
            if (last != null && last.Parent != null)
            {
                parent = last.Parent;
                insertionIndex = FindNode(parent.Children, last) + 1;
            }
        }
        InsertNodes(parent, insertionIndex, nodes);
    }

    static OdfNode CreateBlankRow(OdfTable table, long rowCount)
    {
        OdfNode row = CreateTableElement("table-row");
        SetRepeat(row, "number-rows-repeated", rowCount);
        if (table.ColumnCount > 0)
        {
            StyleDocumentModel.AppendNode(
                row,
                CreateBlankCell(table.ColumnCount));
        }
        return row;
    }

    static OdfNode[] CaptureColumns(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        if (table.Columns.Length == 0)
        {
            return [CreateBlankColumn(columnCount)];
        }
        IsolateColumns(table, columnIndex, columnCount);
        List<OdfNode> result = new List<OdfNode>();
        long capturedCount = 0;
        int index = 0;
        while (index < table.Columns.Length)
        {
            OdfColumn column = table.Columns[index];
            if (column.StartIndex >= columnIndex
                && column.StartIndex < columnIndex + columnCount
                && column.Node != null)
            {
                result.Add(CloneNode(column.Node));
                capturedCount = capturedCount + column.RepeatCount;
            }
            index = index + 1;
        }
        if (capturedCount < columnCount)
        {
            result.Add(CreateBlankColumn(columnCount - capturedCount));
        }
        return result.ToArray();
    }

    static void IsolateColumns(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        if (table.Columns.Length == 0)
        {
            return;
        }
        if (columnIndex < DeclaredColumnCount(table))
        {
            TableRepeatRunEditor.SplitColumnForEdit(table, columnIndex);
        }
        if (columnIndex + columnCount < DeclaredColumnCount(table))
        {
            TableRepeatRunEditor.SplitColumnForEdit(
                table,
                columnIndex + columnCount);
        }
    }

    static void RemoveSelectedColumns(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        IsolateColumns(table, columnIndex, columnCount);
        int index = 0;
        while (index < table.Columns.Length)
        {
            OdfColumn column = table.Columns[index];
            if (column.StartIndex >= columnIndex
                && column.StartIndex < columnIndex + columnCount
                && column.Node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(column.Node);
            }
            index = index + 1;
        }
    }

    static void InsertColumnNodes(
        OdfTable table,
        long columnIndex,
        OdfNode[] nodes)
    {
        OdfNode tableNode = RequireTableNode(table);
        OdfNode parent = tableNode;
        int insertionIndex = FirstRowPosition(tableNode);
        if (table.Columns.Length > 0)
        {
            OdfColumn? column = TableDocumentModel.GetColumn(
                table,
                columnIndex);
            if (column != null
                && column.Node != null
                && column.Node.Parent != null)
            {
                parent = column.Node.Parent;
                insertionIndex = FindNode(parent.Children, column.Node);
            }
            else
            {
                OdfNode? last = table.Columns[table.Columns.Length - 1].Node;
                if (last != null && last.Parent != null)
                {
                    parent = last.Parent;
                    insertionIndex = FindNode(parent.Children, last) + 1;
                }
            }
        }
        InsertNodes(parent, insertionIndex, nodes);
    }

    static OdfNode CreateBlankColumn(long columnCount)
    {
        OdfNode column = CreateTableElement("table-column");
        SetRepeat(column, "number-columns-repeated", columnCount);
        return column;
    }

    static long DeclaredColumnCount(OdfTable table)
    {
        if (table.Columns.Length == 0)
        {
            return 0;
        }
        OdfColumn last = table.Columns[table.Columns.Length - 1];
        return last.StartIndex + last.RepeatCount;
    }

    static void InsertCellsInAllRows(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        int index = 0;
        while (index < table.Rows.Length)
        {
            OdfRow row = table.Rows[index];
            EnsureRowWidth(row, columnIndex);
            InsertCellNodes(
                row,
                columnIndex,
                [CreateBlankCell(columnCount)]);
            index = index + 1;
        }
    }

    static void RemoveCellsInAllRows(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        int index = 0;
        while (index < table.Rows.Length)
        {
            RemoveCellsFromRow(
                table.Rows[index],
                columnIndex,
                columnCount);
            index = index + 1;
        }
    }

    static TableCellSequence[] CaptureCellsInRows(
        OdfTable table,
        long columnIndex,
        long columnCount)
    {
        TableCellSequence[] result = new TableCellSequence[table.Rows.Length];
        int index = 0;
        while (index < table.Rows.Length)
        {
            OdfRow row = table.Rows[index];
            EnsureRowWidth(row, columnIndex + columnCount);
            TableCellSequence sequence = new TableCellSequence();
            sequence.Row = row;
            sequence.Nodes = CaptureCellsFromRow(
                row,
                columnIndex,
                columnCount);
            result[index] = sequence;
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] CaptureCellsFromRow(
        OdfRow row,
        long columnIndex,
        long columnCount)
    {
        IsolateCells(row, columnIndex, columnCount);
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < row.Cells.Length)
        {
            OdfCell cell = row.Cells[index];
            if (cell.StartColumnIndex >= columnIndex
                && cell.StartColumnIndex < columnIndex + columnCount
                && cell.Node != null)
            {
                result.Add(CloneNode(cell.Node));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void IsolateCells(
        OdfRow row,
        long columnIndex,
        long columnCount)
    {
        TableRepeatRunEditor.SplitCellRunForEdit(row, columnIndex);
        if (columnIndex + columnCount < RowWidth(row))
        {
            TableRepeatRunEditor.SplitCellRunForEdit(
                row,
                columnIndex + columnCount);
        }
    }

    static void RemoveCellsFromRow(
        OdfRow row,
        long columnIndex,
        long columnCount)
    {
        long width = RowWidth(row);
        if (columnIndex >= width)
        {
            return;
        }
        long actualCount = columnCount < width - columnIndex
            ? columnCount
            : width - columnIndex;
        IsolateCells(row, columnIndex, actualCount);
        int index = 0;
        while (index < row.Cells.Length)
        {
            OdfCell cell = row.Cells[index];
            if (cell.StartColumnIndex >= columnIndex
                && cell.StartColumnIndex < columnIndex + actualCount
                && cell.Node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(cell.Node);
            }
            index = index + 1;
        }
    }

    static void InsertCellNodes(
        OdfRow row,
        long columnIndex,
        OdfNode[] nodes)
    {
        OdfNode rowNode = row.Node ?? new OdfNode();
        if (row.Node == null)
        {
            throw new OdfEditException(
                "An attached table row is required for cell insertion.");
        }
        int insertionIndex = rowNode.Children.Length;
        OdfCell? cell = FindCell(row, columnIndex);
        if (cell != null && cell.Node != null)
        {
            if (cell.RepeatCount > 1
                && columnIndex > cell.StartColumnIndex)
            {
                cell = TableRepeatRunEditor.SplitCellRunForEdit(
                    row,
                    columnIndex);
            }
            OdfNode cellNode = cell.Node ?? new OdfNode();
            insertionIndex = FindNode(rowNode.Children, cellNode);
        }
        else if (row.Cells.Length > 0)
        {
            OdfNode? last = row.Cells[row.Cells.Length - 1].Node;
            if (last != null)
            {
                insertionIndex = FindNode(rowNode.Children, last) + 1;
            }
        }
        InsertNodes(rowNode, insertionIndex, nodes);
    }

    static void InsertCapturedCells(
        TableCellSequence[] sequences,
        long columnIndex)
    {
        int index = 0;
        while (index < sequences.Length)
        {
            OdfRow? row = sequences[index].Row;
            if (row != null)
            {
                InsertCellNodes(row, columnIndex, sequences[index].Nodes);
            }
            index = index + 1;
        }
    }

    static void InsertCapturedCellsByOrder(
        OdfTable table,
        TableCellSequence[] sequences,
        long columnIndex)
    {
        int index = 0;
        while (index < table.Rows.Length && index < sequences.Length)
        {
            InsertCellNodes(
                table.Rows[index],
                columnIndex,
                sequences[index].Nodes);
            index = index + 1;
        }
    }

    static void EnsureRowWidth(OdfRow row, long width)
    {
        long current = RowWidth(row);
        if (current < width)
        {
            OdfNode node = CreateBlankCell(width - current);
            InsertCellNodes(
                row,
                current,
                [node]);
            OdfCell cell = new OdfCell();
            cell.StartColumnIndex = current;
            cell.RepeatCount = width - current;
            cell.Row = row;
            cell.Node = node;
            TableCellValueModel.InitializeCellValue(cell);
            TableDataViewModel.InitializeCellView(cell);
            OdfCell[] cells = new OdfCell[row.Cells.Length + 1];
            Array.Copy(row.Cells, cells, row.Cells.Length);
            cells[cells.Length - 1] = cell;
            row.Cells = cells;
        }
    }

    static long RowWidth(OdfRow row)
    {
        if (row.Cells.Length == 0)
        {
            return 0;
        }
        OdfCell last = row.Cells[row.Cells.Length - 1];
        return last.StartColumnIndex + last.RepeatCount;
    }

    static OdfCell? FindCell(OdfRow row, long columnIndex)
    {
        int index = 0;
        while (index < row.Cells.Length)
        {
            OdfCell cell = row.Cells[index];
            if (columnIndex >= cell.StartColumnIndex
                && columnIndex < cell.StartColumnIndex + cell.RepeatCount)
            {
                return cell;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateBlankCell(long cellCount)
    {
        OdfNode cell = CreateTableElement("table-cell");
        SetRepeat(cell, "number-columns-repeated", cellCount);
        return cell;
    }

    static OdfNode[] CaptureRectangle(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        if (rowCount > int.MaxValue
            || columnCount > int.MaxValue
            || rowCount > int.MaxValue / columnCount)
        {
            throw new OdfEditException(
                "The range is too large to materialize for copying.");
        }
        OdfNode[] result = new OdfNode[(int)(rowCount * columnCount)];
        int destination = 0;
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                result[destination] = CloneNode(
                    cell.Node ?? new OdfNode());
                destination = destination + 1;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return result;
    }

    static void ClearRectangle(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                ReplaceNode(cell.Node, CreateBlankCell(1));
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void ReplaceRectangle(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        OdfNode[] nodes)
    {
        int source = 0;
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                ReplaceNode(cell.Node, nodes[source]);
                source = source + 1;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void RebaseNodes(
        OdfNode[] nodes,
        string tableName,
        long rowDelta,
        long columnDelta)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            TableReferenceUpdater.RebaseSubtree(
                nodes[index],
                tableName,
                rowDelta,
                columnDelta);
            index = index + 1;
        }
    }

    static void RebaseCellSequences(
        TableCellSequence[] sequences,
        string tableName,
        long rowDelta,
        long columnDelta)
    {
        int index = 0;
        while (index < sequences.Length)
        {
            RebaseNodes(
                sequences[index].Nodes,
                tableName,
                rowDelta,
                columnDelta);
            index = index + 1;
        }
    }

    static void ReplaceNode(OdfNode? existing, OdfNode replacement)
    {
        if (existing == null || existing.Parent == null)
        {
            throw new OdfEditException(
                "An attached cell is required for replacement.");
        }
        OdfNode parent = existing.Parent;
        int index = FindNode(parent.Children, existing);
        parent.Children[index] = replacement;
        replacement.Parent = parent;
        existing.Parent = null;
    }

    static void InsertNodes(
        OdfNode parent,
        int insertionIndex,
        OdfNode[] nodes)
    {
        OdfNode[] children = new OdfNode[
            parent.Children.Length + nodes.Length];
        Array.Copy(parent.Children, 0, children, 0, insertionIndex);
        Array.Copy(nodes, 0, children, insertionIndex, nodes.Length);
        Array.Copy(
            parent.Children,
            insertionIndex,
            children,
            insertionIndex + nodes.Length,
            parent.Children.Length - insertionIndex);
        int index = 0;
        while (index < nodes.Length)
        {
            nodes[index].Parent = parent;
            index = index + 1;
        }
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static int FindNode(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The table node is detached from its parent.");
    }

    static int FirstRowPosition(OdfNode tableNode)
    {
        int index = 0;
        while (index < tableNode.Children.Length)
        {
            OdfNode child = tableNode.Children[index];
            if (child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Table
                && (child.Name.LocalName == "table-row"
                    || child.Name.LocalName == "table-header-rows"
                    || child.Name.LocalName == "table-row-group"))
            {
                return index;
            }
            index = index + 1;
        }
        return tableNode.Children.Length;
    }

    static OdfNode RequireTableNode(OdfTable table)
    {
        if (table == null || table.Node == null)
        {
            throw new OdfEditException(
                "An attached table is required for structural editing.");
        }
        return table.Node;
    }

    static OdfNode CreateTableElement(string localName)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Table;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.TablePrefix;
        result.WasEmptyElement = true;
        return result;
    }

    static void SetRepeat(
        OdfNode node,
        string localName,
        long count)
    {
        if (count > 1)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                count.ToString());
        }
    }

    static OdfNode CloneNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }

    static OdfTable ReloadTable(
        OdfDocument document,
        OdfNode? tableNode)
    {
        TableDocumentModel.LoadTables(document);
        int index = 0;
        while (index < document.Tables.Length)
        {
            if (document.Tables[index].Node == tableNode)
            {
                return document.Tables[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The edited table could not be rematerialized.");
    }

    static OdfTable RefreshTable(
        OdfDocument document,
        OdfNode? tableNode)
    {
        OdfTable table = ReloadTable(document, tableNode);
        TableRepeatRunEditor.CoalesceTableRuns(table);
        return ReloadTable(document, tableNode);
    }
}
