using System;
using System.Collections.Generic;

namespace odf;

internal class TableParseState
{
    internal OdfTable? Table;
    internal List<OdfRow> Rows = new List<OdfRow>();
    internal List<OdfColumn> Columns = new List<OdfColumn>();
    internal List<OdfTableGroup> RowGroups = new List<OdfTableGroup>();
    internal List<OdfTableGroup> ColumnGroups = new List<OdfTableGroup>();
    internal long RowIndex;
    internal long ColumnIndex;
    internal long MaximumCellColumn;
}

internal static class TableDocumentModel
{
    internal static void LoadTables(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table materialization.");
        }
        List<OdfTable> tables = new List<OdfTable>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Root != null)
            {
                CollectTables(
                    part.Root,
                    part.Path,
                    null,
                    null,
                    tables);
            }
            partIndex = partIndex + 1;
        }
        document.Tables = tables.ToArray();
        SpreadsheetSettingsModel.LoadSpreadsheetSettings(document);
        TableCellStructureModel.LoadCellStructures(document);
        TableCellValueModel.LoadCellValues(document);
        SpreadsheetFormattingModel.LoadSpreadsheetFormatting(document);
        TableDataViewModel.LoadDataViews(document);
        SpreadsheetDataModel.LoadSpreadsheetData(document);
        PivotTableModel.LoadPivotTables(document);
        SpreadsheetChangeModel.LoadSpreadsheetChanges(document);
        ChartDocumentModel.AttachChartDataTables(document);
    }

    internal static OdfTable? FindTable(
        OdfDocument document,
        string name)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table lookup.");
        }
        OdfTable? result = null;
        int count = 0;
        int index = 0;
        while (index < document.Tables.Length)
        {
            if (document.Tables[index].Name == name)
            {
                result = document.Tables[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfRow? GetRow(OdfTable table, long rowIndex)
    {
        if (table == null || rowIndex < 0 || rowIndex >= table.RowCount)
        {
            return null;
        }
        int low = 0;
        int high = table.Rows.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            OdfRow row = table.Rows[middle];
            if (rowIndex < row.StartIndex)
            {
                high = middle - 1;
            }
            else if (DistanceFromStart(rowIndex, row.StartIndex)
                >= row.RepeatCount)
            {
                low = middle + 1;
            }
            else
            {
                return row;
            }
        }
        return null;
    }

    internal static OdfColumn? GetColumn(
        OdfTable table,
        long columnIndex)
    {
        if (table == null
            || columnIndex < 0
            || columnIndex >= table.ColumnCount)
        {
            return null;
        }
        int low = 0;
        int high = table.Columns.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            OdfColumn column = table.Columns[middle];
            if (columnIndex < column.StartIndex)
            {
                high = middle - 1;
            }
            else if (DistanceFromStart(columnIndex, column.StartIndex)
                >= column.RepeatCount)
            {
                low = middle + 1;
            }
            else
            {
                return column;
            }
        }
        return null;
    }

    internal static OdfCell? GetCell(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        OdfRow? row = GetRow(table, rowIndex);
        if (row == null || columnIndex < 0)
        {
            return null;
        }
        int low = 0;
        int high = row.Cells.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            OdfCell cell = row.Cells[middle];
            if (columnIndex < cell.StartColumnIndex)
            {
                high = middle - 1;
            }
            else if (DistanceFromStart(
                    columnIndex,
                    cell.StartColumnIndex) >= cell.RepeatCount)
            {
                low = middle + 1;
            }
            else
            {
                return cell;
            }
        }
        return null;
    }

    static void CollectTables(
        OdfNode node,
        string partPath,
        OdfTable? parentTable,
        OdfCell? parentCell,
        List<OdfTable> tables)
    {
        if (IsTableElement(node, "table"))
        {
            CreateTable(
                node,
                partPath,
                parentTable,
                parentCell,
                tables);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectTables(
                node.Children[index],
                partPath,
                parentTable,
                parentCell,
                tables);
            index = index + 1;
        }
    }

    static OdfTable CreateTable(
        OdfNode node,
        string partPath,
        OdfTable? parentTable,
        OdfCell? parentCell,
        List<OdfTable> tables)
    {
        OdfTable table = new OdfTable();
        table.Name = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            "name");
        table.ParentTable = parentTable;
        table.ParentCell = parentCell;
        table.PartPath = partPath;
        table.Node = node;
        tables.Add(table);
        TableParseState state = new TableParseState();
        state.Table = table;
        ParseTableChildren(node.Children, state, false, false, 0, 0, tables);
        table.Rows = state.Rows.ToArray();
        table.Columns = state.Columns.ToArray();
        table.RowGroups = state.RowGroups.ToArray();
        table.ColumnGroups = state.ColumnGroups.ToArray();
        table.RowCount = state.RowIndex;
        table.ColumnCount = state.ColumnIndex > state.MaximumCellColumn
            ? state.ColumnIndex
            : state.MaximumCellColumn;
        return table;
    }

    static void ParseTableChildren(
        OdfNode[] children,
        TableParseState state,
        bool headerRows,
        bool headerColumns,
        int rowGroupDepth,
        int columnGroupDepth,
        List<OdfTable> tables)
    {
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (IsTableElement(child, "table-row"))
            {
                AddRow(
                    child,
                    state,
                    headerRows,
                    rowGroupDepth,
                    tables);
            }
            else if (IsTableElement(child, "table-column"))
            {
                AddColumn(
                    child,
                    state,
                    headerColumns,
                    columnGroupDepth);
            }
            else if (IsTableElement(child, "table-header-rows"))
            {
                ParseTableChildren(
                    child.Children,
                    state,
                    true,
                    headerColumns,
                    rowGroupDepth,
                    columnGroupDepth,
                    tables);
            }
            else if (IsTableElement(child, "table-header-columns"))
            {
                ParseTableChildren(
                    child.Children,
                    state,
                    headerRows,
                    true,
                    rowGroupDepth,
                    columnGroupDepth,
                    tables);
            }
            else if (IsTableElement(child, "table-row-group"))
            {
                AddRowGroup(
                    child,
                    state,
                    headerRows,
                    headerColumns,
                    rowGroupDepth,
                    columnGroupDepth,
                    tables);
            }
            else if (IsTableElement(child, "table-column-group"))
            {
                AddColumnGroup(
                    child,
                    state,
                    headerRows,
                    headerColumns,
                    rowGroupDepth,
                    columnGroupDepth,
                    tables);
            }
            else if (IsTableElement(child, "table-rows")
                || IsTableElement(child, "table-columns"))
            {
                ParseTableChildren(
                    child.Children,
                    state,
                    headerRows,
                    headerColumns,
                    rowGroupDepth,
                    columnGroupDepth,
                    tables);
            }
            index = index + 1;
        }
    }

    static void AddRowGroup(
        OdfNode node,
        TableParseState state,
        bool headerRows,
        bool headerColumns,
        int rowGroupDepth,
        int columnGroupDepth,
        List<OdfTable> tables)
    {
        OdfTableGroup group = new OdfTableGroup();
        group.IsRowGroup = true;
        group.StartIndex = state.RowIndex;
        group.Depth = rowGroupDepth + 1;
        group.Node = node;
        state.RowGroups.Add(group);
        ParseTableChildren(
            node.Children,
            state,
            headerRows,
            headerColumns,
            rowGroupDepth + 1,
            columnGroupDepth,
            tables);
        group.Count = DistanceFromStart(state.RowIndex, group.StartIndex);
    }

    static void AddColumnGroup(
        OdfNode node,
        TableParseState state,
        bool headerRows,
        bool headerColumns,
        int rowGroupDepth,
        int columnGroupDepth,
        List<OdfTable> tables)
    {
        OdfTableGroup group = new OdfTableGroup();
        group.IsRowGroup = false;
        group.StartIndex = state.ColumnIndex;
        group.Depth = columnGroupDepth + 1;
        group.Node = node;
        state.ColumnGroups.Add(group);
        ParseTableChildren(
            node.Children,
            state,
            headerRows,
            headerColumns,
            rowGroupDepth,
            columnGroupDepth + 1,
            tables);
        group.Count = DistanceFromStart(
            state.ColumnIndex,
            group.StartIndex);
    }

    static void AddRow(
        OdfNode node,
        TableParseState state,
        bool isHeader,
        int groupDepth,
        List<OdfTable> tables)
    {
        OdfRow row = new OdfRow();
        row.StartIndex = state.RowIndex;
        row.RepeatCount = ReadRepeatCount(node, "number-rows-repeated");
        row.IsHeader = isHeader;
        row.GroupDepth = groupDepth;
        row.Table = state.Table;
        row.Node = node;
        row.Cells = CreateCells(node.Children, row, state, tables);
        state.Rows.Add(row);
        state.RowIndex = AddSaturated(state.RowIndex, row.RepeatCount);
    }

    static OdfCell[] CreateCells(
        OdfNode[] children,
        OdfRow row,
        TableParseState state,
        List<OdfTable> tables)
    {
        List<OdfCell> cells = new List<OdfCell>();
        long columnIndex = 0;
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            bool ordinary = IsTableElement(child, "table-cell");
            bool covered = IsTableElement(child, "covered-table-cell");
            if (ordinary || covered)
            {
                OdfCell cell = new OdfCell();
                cell.StartColumnIndex = columnIndex;
                cell.RepeatCount = ReadRepeatCount(
                    child,
                    "number-columns-repeated");
                cell.IsCovered = covered;
                cell.Row = row;
                cell.Node = child;
                List<OdfTable> nested = new List<OdfTable>();
                CollectNestedTables(
                    child.Children,
                    state.Table,
                    cell,
                    child.Location.PartPath,
                    tables,
                    nested);
                cell.NestedTables = nested.ToArray();
                cells.Add(cell);
                columnIndex = AddSaturated(columnIndex, cell.RepeatCount);
            }
            index = index + 1;
        }
        if (columnIndex > state.MaximumCellColumn)
        {
            state.MaximumCellColumn = columnIndex;
        }
        return cells.ToArray();
    }

    static void CollectNestedTables(
        OdfNode[] children,
        OdfTable? parentTable,
        OdfCell parentCell,
        string partPath,
        List<OdfTable> tables,
        List<OdfTable> nested)
    {
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (IsTableElement(child, "table"))
            {
                OdfTable table = CreateTable(
                    child,
                    partPath,
                    parentTable,
                    parentCell,
                    tables);
                nested.Add(table);
            }
            else
            {
                CollectNestedTables(
                    child.Children,
                    parentTable,
                    parentCell,
                    partPath,
                    tables,
                    nested);
            }
            index = index + 1;
        }
    }

    static void AddColumn(
        OdfNode node,
        TableParseState state,
        bool isHeader,
        int groupDepth)
    {
        OdfColumn column = new OdfColumn();
        column.StartIndex = state.ColumnIndex;
        column.RepeatCount = ReadRepeatCount(
            node,
            "number-columns-repeated");
        column.IsHeader = isHeader;
        column.GroupDepth = groupDepth;
        column.Table = state.Table;
        column.Node = node;
        state.Columns.Add(column);
        state.ColumnIndex = AddSaturated(
            state.ColumnIndex,
            column.RepeatCount);
    }

    static long ReadRepeatCount(OdfNode node, string localName)
    {
        string value = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            localName);
        if (value.Length == 0)
        {
            return 1;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        return parsed.IsValid && parsed.IntegerValue > 0
            ? parsed.IntegerValue
            : 1;
    }

    static long AddSaturated(long value, long count)
    {
        return count > long.MaxValue - value
            ? long.MaxValue
            : value + count;
    }

    static long DistanceFromStart(long index, long start)
    {
        return index >= start ? index - start : long.MaxValue;
    }

    static bool IsTableElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == localName;
    }
}
