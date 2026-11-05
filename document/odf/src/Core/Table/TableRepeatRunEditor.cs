using System;
using System.Collections.Generic;

namespace odf;

internal static class TableRepeatRunEditor
{
    internal static OdfRow SplitRowForEdit(
        OdfTable table,
        long rowIndex)
    {
        OdfRow? row = TableDocumentModel.GetRow(table, rowIndex);
        if (row == null)
        {
            throw new OdfEditException(
                "The logical row index is outside the table.");
        }
        if (row.RepeatCount == 1)
        {
            return row;
        }
        OdfNode node = RequireAttachedNode(
            row.Node,
            "A repeated row requires an attached table-row element.");
        long beforeCount = rowIndex - row.StartIndex;
        long afterCount = row.RepeatCount - beforeCount - 1;
        List<OdfNode> nodes = new List<OdfNode>();
        List<OdfRow> rows = new List<OdfRow>();
        if (beforeCount > 0)
        {
            OdfNode beforeNode = CloneNode(node);
            SetRepeatAttribute(
                beforeNode,
                "number-rows-repeated",
                beforeCount);
            nodes.Add(beforeNode);
            rows.Add(CloneRowRun(
                row,
                beforeNode,
                row.StartIndex,
                beforeCount));
        }
        SetRepeatAttribute(node, "number-rows-repeated", 1);
        row.StartIndex = rowIndex;
        row.RepeatCount = 1;
        nodes.Add(node);
        rows.Add(row);
        if (afterCount > 0)
        {
            OdfNode afterNode = CloneNode(node);
            SetRepeatAttribute(
                afterNode,
                "number-rows-repeated",
                afterCount);
            nodes.Add(afterNode);
            rows.Add(CloneRowRun(
                row,
                afterNode,
                rowIndex + 1,
                afterCount));
        }
        ReplaceAttachedNode(node, nodes.ToArray());
        int physicalIndex = FindRowIndex(table.Rows, row);
        table.Rows = ReplaceRowsAt(
            table.Rows,
            physicalIndex,
            rows.ToArray());
        return row;
    }

    internal static OdfColumn SplitColumnForEdit(
        OdfTable table,
        long columnIndex)
    {
        OdfColumn? column = TableDocumentModel.GetColumn(table, columnIndex);
        if (column == null)
        {
            throw new OdfEditException(
                "The logical column index is outside the declared columns.");
        }
        if (column.RepeatCount == 1)
        {
            return column;
        }
        OdfNode node = RequireAttachedNode(
            column.Node,
            "A repeated column requires an attached table-column element.");
        long beforeCount = columnIndex - column.StartIndex;
        long afterCount = column.RepeatCount - beforeCount - 1;
        List<OdfNode> nodes = new List<OdfNode>();
        List<OdfColumn> columns = new List<OdfColumn>();
        if (beforeCount > 0)
        {
            OdfNode beforeNode = CloneNode(node);
            SetRepeatAttribute(
                beforeNode,
                "number-columns-repeated",
                beforeCount);
            nodes.Add(beforeNode);
            columns.Add(CloneColumnRun(
                column,
                beforeNode,
                column.StartIndex,
                beforeCount));
        }
        SetRepeatAttribute(node, "number-columns-repeated", 1);
        column.StartIndex = columnIndex;
        column.RepeatCount = 1;
        nodes.Add(node);
        columns.Add(column);
        if (afterCount > 0)
        {
            OdfNode afterNode = CloneNode(node);
            SetRepeatAttribute(
                afterNode,
                "number-columns-repeated",
                afterCount);
            nodes.Add(afterNode);
            columns.Add(CloneColumnRun(
                column,
                afterNode,
                columnIndex + 1,
                afterCount));
        }
        ReplaceAttachedNode(node, nodes.ToArray());
        int physicalIndex = FindColumnIndex(table.Columns, column);
        table.Columns = ReplaceColumnsAt(
            table.Columns,
            physicalIndex,
            columns.ToArray());
        return column;
    }

    internal static OdfCell SplitCellForEdit(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        OdfRow row = SplitRowForEdit(table, rowIndex);
        return SplitCellRunForEdit(row, columnIndex);
    }

    internal static OdfCell SplitCellRunForEdit(
        OdfRow row,
        long columnIndex)
    {
        if (row == null)
        {
            throw new OdfEditException(
                "A row is required for cell repeat-run editing.");
        }
        OdfCell? cell = FindCellInRow(row, columnIndex);
        if (cell == null)
        {
            throw new OdfEditException(
                "The logical column index has no materialized cell.");
        }
        if (cell.RepeatCount == 1)
        {
            return cell;
        }
        OdfNode node = RequireAttachedNode(
            cell.Node,
            "A repeated cell requires an attached table-cell element.");
        long beforeCount = columnIndex - cell.StartColumnIndex;
        long afterCount = cell.RepeatCount - beforeCount - 1;
        List<OdfNode> nodes = new List<OdfNode>();
        List<OdfCell> cells = new List<OdfCell>();
        if (beforeCount > 0)
        {
            OdfNode beforeNode = CloneNode(node);
            SetRepeatAttribute(
                beforeNode,
                "number-columns-repeated",
                beforeCount);
            nodes.Add(beforeNode);
            cells.Add(CloneCellRun(
                cell,
                row,
                beforeNode,
                cell.StartColumnIndex,
                beforeCount));
        }
        SetRepeatAttribute(node, "number-columns-repeated", 1);
        cell.StartColumnIndex = columnIndex;
        cell.RepeatCount = 1;
        cell.Row = row;
        nodes.Add(node);
        cells.Add(cell);
        if (afterCount > 0)
        {
            OdfNode afterNode = CloneNode(node);
            SetRepeatAttribute(
                afterNode,
                "number-columns-repeated",
                afterCount);
            nodes.Add(afterNode);
            cells.Add(CloneCellRun(
                cell,
                row,
                afterNode,
                columnIndex + 1,
                afterCount));
        }
        ReplaceAttachedNode(node, nodes.ToArray());
        int physicalIndex = FindCellIndex(row.Cells, cell);
        row.Cells = ReplaceCellsAt(
            row.Cells,
            physicalIndex,
            cells.ToArray());
        return cell;
    }

    internal static void CoalesceTableRuns(OdfTable table)
    {
        if (table == null)
        {
            throw new OdfDocumentException(
                "A table is required for repeat-run coalescing.");
        }
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            table.Rows[rowIndex].Cells = CoalesceCells(
                table.Rows[rowIndex].Cells);
            ReindexCells(table.Rows[rowIndex]);
            rowIndex = rowIndex + 1;
        }
        table.Rows = CoalesceRows(table.Rows);
        table.Columns = CoalesceColumns(table.Columns);
        ReindexRows(table);
        ReindexColumns(table);
    }

    static OdfRow[] CoalesceRows(OdfRow[] source)
    {
        List<OdfRow> result = new List<OdfRow>();
        int index = 0;
        while (index < source.Length)
        {
            OdfRow row = source[index];
            if (result.Count > 0
                && CanMergeRows(result[result.Count - 1], row))
            {
                OdfRow previous = result[result.Count - 1];
                previous.RepeatCount = AddSaturated(
                    previous.RepeatCount,
                    row.RepeatCount);
                SetRepeatAttribute(
                    previous.Node ?? new OdfNode(),
                    "number-rows-repeated",
                    previous.RepeatCount);
                RemoveMergedNode(row.Node);
            }
            else
            {
                result.Add(row);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfColumn[] CoalesceColumns(OdfColumn[] source)
    {
        List<OdfColumn> result = new List<OdfColumn>();
        int index = 0;
        while (index < source.Length)
        {
            OdfColumn column = source[index];
            if (result.Count > 0
                && CanMergeColumns(result[result.Count - 1], column))
            {
                OdfColumn previous = result[result.Count - 1];
                previous.RepeatCount = AddSaturated(
                    previous.RepeatCount,
                    column.RepeatCount);
                SetRepeatAttribute(
                    previous.Node ?? new OdfNode(),
                    "number-columns-repeated",
                    previous.RepeatCount);
                RemoveMergedNode(column.Node);
            }
            else
            {
                result.Add(column);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfCell[] CoalesceCells(OdfCell[] source)
    {
        List<OdfCell> result = new List<OdfCell>();
        int index = 0;
        while (index < source.Length)
        {
            OdfCell cell = source[index];
            if (result.Count > 0
                && CanMergeCells(result[result.Count - 1], cell))
            {
                OdfCell previous = result[result.Count - 1];
                previous.RepeatCount = AddSaturated(
                    previous.RepeatCount,
                    cell.RepeatCount);
                SetRepeatAttribute(
                    previous.Node ?? new OdfNode(),
                    "number-columns-repeated",
                    previous.RepeatCount);
                RemoveMergedNode(cell.Node);
            }
            else
            {
                result.Add(cell);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool CanMergeRows(OdfRow left, OdfRow right)
    {
        return left.IsHeader == right.IsHeader
            && left.GroupDepth == right.GroupDepth
            && NodesAreMergeable(
                left.Node,
                right.Node,
                "number-rows-repeated");
    }

    static bool CanMergeColumns(OdfColumn left, OdfColumn right)
    {
        return left.IsHeader == right.IsHeader
            && left.GroupDepth == right.GroupDepth
            && NodesAreMergeable(
                left.Node,
                right.Node,
                "number-columns-repeated");
    }

    static bool CanMergeCells(OdfCell left, OdfCell right)
    {
        return left.IsCovered == right.IsCovered
            && NodesAreMergeable(
                left.Node,
                right.Node,
                "number-columns-repeated");
    }

    static bool NodesAreMergeable(
        OdfNode? left,
        OdfNode? right,
        string ignoredAttribute)
    {
        return left != null
            && right != null
            && SiblingOrderAllowsMerge(left, right)
            && NodesEquivalent(left, right, ignoredAttribute, true);
    }

    static bool NodesEquivalent(
        OdfNode left,
        OdfNode right,
        string ignoredAttribute,
        bool root)
    {
        if (left.Kind != right.Kind
            || left.Value != right.Value
            || !NamesEquivalent(left.Name, right.Name)
            || !AttributesEquivalent(
                left.Attributes,
                right.Attributes,
                ignoredAttribute,
                root)
            || left.Children.Length != right.Children.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Children.Length)
        {
            if (!NodesEquivalent(
                left.Children[index],
                right.Children[index],
                ignoredAttribute,
                false))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool NamesEquivalent(
        OdfQualifiedName? left,
        OdfQualifiedName? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }
        return left.NamespaceName == right.NamespaceName
            && left.LocalName == right.LocalName;
    }

    static bool AttributesEquivalent(
        OdfAttribute[] left,
        OdfAttribute[] right,
        string ignoredAttribute,
        bool root)
    {
        int leftCount = CountComparedAttributes(
            left,
            ignoredAttribute,
            root);
        int rightCount = CountComparedAttributes(
            right,
            ignoredAttribute,
            root);
        if (leftCount != rightCount)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            OdfAttribute attribute = left[index];
            if (!IsIgnoredAttribute(attribute, ignoredAttribute, root)
                && !ContainsEquivalentAttribute(right, attribute))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static int CountComparedAttributes(
        OdfAttribute[] attributes,
        string ignoredAttribute,
        bool root)
    {
        int count = 0;
        int index = 0;
        while (index < attributes.Length)
        {
            if (!IsIgnoredAttribute(
                attributes[index],
                ignoredAttribute,
                root))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    static bool IsIgnoredAttribute(
        OdfAttribute attribute,
        string ignoredAttribute,
        bool root)
    {
        return root
            && attribute.Name.NamespaceName == OdfNamespaces.Table
            && attribute.Name.LocalName == ignoredAttribute;
    }

    static bool ContainsEquivalentAttribute(
        OdfAttribute[] attributes,
        OdfAttribute target)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute candidate = attributes[index];
            if (candidate.Name.NamespaceName == target.Name.NamespaceName
                && candidate.Name.LocalName == target.Name.LocalName
                && candidate.Value == target.Value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool SiblingOrderAllowsMerge(OdfNode left, OdfNode right)
    {
        if (left.Parent == null || left.Parent != right.Parent)
        {
            return false;
        }
        OdfNode parent = left.Parent;
        int leftIndex = FindNodeIndex(parent.Children, left);
        int rightIndex = FindNodeIndex(parent.Children, right);
        if (leftIndex < 0 || rightIndex <= leftIndex)
        {
            return false;
        }
        int index = leftIndex + 1;
        while (index < rightIndex)
        {
            OdfNode node = parent.Children[index];
            if (node.Kind != OdfNodeKind.Text || !IsWhitespace(node.Value))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' '
                && character != '\t'
                && character != '\r'
                && character != '\n')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfRow CloneRowRun(
        OdfRow source,
        OdfNode node,
        long startIndex,
        long repeatCount)
    {
        OdfRow result = new OdfRow();
        result.StartIndex = startIndex;
        result.RepeatCount = repeatCount;
        result.IsHeader = source.IsHeader;
        result.GroupDepth = source.GroupDepth;
        result.Table = source.Table;
        result.Node = node;
        result.Cells = CloneCellsForRow(source.Cells, result, node);
        TableDataViewModel.InitializeRowView(result);
        result.Style = source.Style;
        result.DefaultStyle = source.DefaultStyle;
        result.DefaultCellStyle = source.DefaultCellStyle;
        return result;
    }

    static OdfCell[] CloneCellsForRow(
        OdfCell[] source,
        OdfRow row,
        OdfNode rowNode)
    {
        List<OdfNode> nodes = new List<OdfNode>();
        int nodeIndex = 0;
        while (nodeIndex < rowNode.Children.Length)
        {
            OdfNode node = rowNode.Children[nodeIndex];
            if (IsCellElement(node))
            {
                nodes.Add(node);
            }
            nodeIndex = nodeIndex + 1;
        }
        OdfCell[] result = new OdfCell[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfNode node = index < nodes.Count
                ? nodes[index]
                : new OdfNode();
            result[index] = CloneCellRun(
                source[index],
                row,
                node,
                source[index].StartColumnIndex,
                source[index].RepeatCount);
            index = index + 1;
        }
        return result;
    }

    static OdfColumn CloneColumnRun(
        OdfColumn source,
        OdfNode node,
        long startIndex,
        long repeatCount)
    {
        OdfColumn result = new OdfColumn();
        result.StartIndex = startIndex;
        result.RepeatCount = repeatCount;
        result.IsHeader = source.IsHeader;
        result.GroupDepth = source.GroupDepth;
        result.Table = source.Table;
        result.Node = node;
        TableDataViewModel.InitializeColumnView(result);
        result.Style = source.Style;
        result.DefaultStyle = source.DefaultStyle;
        result.DefaultCellStyle = source.DefaultCellStyle;
        return result;
    }

    static OdfCell CloneCellRun(
        OdfCell source,
        OdfRow row,
        OdfNode node,
        long startColumnIndex,
        long repeatCount)
    {
        OdfCell result = new OdfCell();
        result.StartColumnIndex = startColumnIndex;
        result.RepeatCount = repeatCount;
        result.IsCovered = source.IsCovered;
        result.Row = row;
        result.NestedTables = [];
        result.Node = node;
        TableCellValueModel.InitializeCellValue(result);
        TableDataViewModel.InitializeCellView(result);
        result.Style = source.Style;
        result.DefaultStyle = source.DefaultStyle;
        result.EffectiveStyle = source.EffectiveStyle;
        return result;
    }

    static OdfCell? FindCellInRow(OdfRow row, long columnIndex)
    {
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
            else if (columnIndex - cell.StartColumnIndex >= cell.RepeatCount)
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

    static void SetRepeatAttribute(
        OdfNode node,
        string localName,
        long repeatCount)
    {
        if (repeatCount <= 1)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                repeatCount.ToString());
        }
    }

    static OdfNode CloneNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }

    static OdfNode RequireAttachedNode(OdfNode? node, string message)
    {
        if (node == null || node.Parent == null)
        {
            throw new OdfEditException(message);
        }
        return node;
    }

    static void ReplaceAttachedNode(OdfNode node, OdfNode[] replacements)
    {
        OdfNode parent = node.Parent ?? new OdfNode();
        int index = FindNodeIndex(parent.Children, node);
        if (index < 0)
        {
            throw new OdfEditException(
                "The repeated table element is detached from its parent.");
        }
        OdfNode[] children = new OdfNode[
            parent.Children.Length - 1 + replacements.Length];
        Array.Copy(parent.Children, 0, children, 0, index);
        Array.Copy(replacements, 0, children, index, replacements.Length);
        Array.Copy(
            parent.Children,
            index + 1,
            children,
            index + replacements.Length,
            parent.Children.Length - index - 1);
        int replacementIndex = 0;
        while (replacementIndex < replacements.Length)
        {
            replacements[replacementIndex].Parent = parent;
            replacementIndex = replacementIndex + 1;
        }
        parent.Children = children;
    }

    static void RemoveMergedNode(OdfNode? node)
    {
        if (node != null && node.Parent != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
        }
    }

    static int FindNodeIndex(OdfNode[] nodes, OdfNode target)
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
        return -1;
    }

    static int FindRowIndex(OdfRow[] rows, OdfRow target)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int FindColumnIndex(OdfColumn[] columns, OdfColumn target)
    {
        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int FindCellIndex(OdfCell[] cells, OdfCell target)
    {
        int index = 0;
        while (index < cells.Length)
        {
            if (cells[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static OdfRow[] ReplaceRowsAt(
        OdfRow[] source,
        int index,
        OdfRow[] replacements)
    {
        OdfRow[] result = new OdfRow[
            source.Length - 1 + replacements.Length];
        Array.Copy(source, 0, result, 0, index);
        Array.Copy(replacements, 0, result, index, replacements.Length);
        Array.Copy(
            source,
            index + 1,
            result,
            index + replacements.Length,
            source.Length - index - 1);
        return result;
    }

    static OdfColumn[] ReplaceColumnsAt(
        OdfColumn[] source,
        int index,
        OdfColumn[] replacements)
    {
        OdfColumn[] result = new OdfColumn[
            source.Length - 1 + replacements.Length];
        Array.Copy(source, 0, result, 0, index);
        Array.Copy(replacements, 0, result, index, replacements.Length);
        Array.Copy(
            source,
            index + 1,
            result,
            index + replacements.Length,
            source.Length - index - 1);
        return result;
    }

    static OdfCell[] ReplaceCellsAt(
        OdfCell[] source,
        int index,
        OdfCell[] replacements)
    {
        OdfCell[] result = new OdfCell[
            source.Length - 1 + replacements.Length];
        Array.Copy(source, 0, result, 0, index);
        Array.Copy(replacements, 0, result, index, replacements.Length);
        Array.Copy(
            source,
            index + 1,
            result,
            index + replacements.Length,
            source.Length - index - 1);
        return result;
    }

    static void ReindexRows(OdfTable table)
    {
        long index = 0;
        int physical = 0;
        while (physical < table.Rows.Length)
        {
            table.Rows[physical].StartIndex = index;
            index = AddSaturated(index, table.Rows[physical].RepeatCount);
            physical = physical + 1;
        }
        table.RowCount = index;
    }

    static void ReindexColumns(OdfTable table)
    {
        long index = 0;
        int physical = 0;
        while (physical < table.Columns.Length)
        {
            table.Columns[physical].StartIndex = index;
            index = AddSaturated(index, table.Columns[physical].RepeatCount);
            physical = physical + 1;
        }
        if (index > table.ColumnCount)
        {
            table.ColumnCount = index;
        }
    }

    static void ReindexCells(OdfRow row)
    {
        long index = 0;
        int physical = 0;
        while (physical < row.Cells.Length)
        {
            row.Cells[physical].StartColumnIndex = index;
            index = AddSaturated(index, row.Cells[physical].RepeatCount);
            physical = physical + 1;
        }
    }

    static long AddSaturated(long value, long count)
    {
        return count > long.MaxValue - value
            ? long.MaxValue
            : value + count;
    }

    static bool IsCellElement(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && (node.Name.LocalName == "table-cell"
                || node.Name.LocalName == "covered-table-cell");
    }
}
