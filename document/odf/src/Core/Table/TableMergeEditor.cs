using System;

namespace odf;

internal static class TableMergeEditor
{
    internal static OdfTable MergeRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        RequireRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            true);
        table = EnsureMaterializedWidth(
            document,
            table,
            rowIndex,
            rowCount,
            columnIndex + columnCount);
        RequireUnoccupiedRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        OdfCell origin = TableRepeatRunEditor.SplitCellForEdit(
            table,
            rowIndex,
            columnIndex);
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                if (rowOffset != 0 || columnOffset != 0)
                {
                    OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                        table,
                        rowIndex + rowOffset,
                        columnIndex + columnOffset);
                    ConvertToCovered(cell);
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        OdfNode originNode = RequireCellNode(origin);
        WriteSpan(originNode, "number-columns-spanned", columnCount);
        WriteSpan(originNode, "number-rows-spanned", rowCount);
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable UnmergeRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        RequireCellCoordinate(table, rowIndex, columnIndex);
        OdfCell? current = TableDocumentModel.GetCell(
            table,
            rowIndex,
            columnIndex);
        if (current == null
            || !current.IsMergeOrigin
            || current.ColumnSpan <= 0
            || current.RowSpan <= 0)
        {
            throw new OdfEditException(
                "The selected cell is not a merged-range origin.");
        }
        long columnCount = current.ColumnSpan;
        long rowCount = current.RowSpan;
        RequireRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            false);
        RequireCoveredRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        OdfCell origin = TableRepeatRunEditor.SplitCellForEdit(
            table,
            rowIndex,
            columnIndex);
        OdfNode originNode = RequireCellNode(origin);
        RemoveMergeSpans(originNode);
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                if (rowOffset != 0 || columnOffset != 0)
                {
                    OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                        table,
                        rowIndex + rowOffset,
                        columnIndex + columnOffset);
                    ConvertToOrdinary(cell);
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable SetMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula)
    {
        return ApplyMatrixFormula(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            formula);
    }

    internal static OdfTable SetArrayFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula)
    {
        return ApplyMatrixFormula(
            document,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            formula);
    }

    internal static OdfTable ClearMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        RequireCellCoordinate(table, rowIndex, columnIndex);
        OdfCell? current = TableDocumentModel.GetCell(
            table,
            rowIndex,
            columnIndex);
        if (current == null || !current.IsMatrixFormula)
        {
            throw new OdfEditException(
                "The selected cell is not a matrix-formula origin.");
        }
        OdfCell origin = TableRepeatRunEditor.SplitCellForEdit(
            table,
            rowIndex,
            columnIndex);
        OdfNode node = RequireCellNode(origin);
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-matrix-columns-spanned");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-matrix-rows-spanned");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "formula");
        return RefreshTable(document, table.Node);
    }

    static OdfTable ApplyMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula)
    {
        if (formula == null || formula.Length == 0)
        {
            throw new OdfEditException(
                "A matrix or array formula is required.");
        }
        RequireRectangle(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            false);
        table = EnsureMaterializedWidth(
            document,
            table,
            rowIndex,
            rowCount,
            columnIndex + columnCount);
        RequireUnoccupiedRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        OdfCell origin = TableRepeatRunEditor.SplitCellForEdit(
            table,
            rowIndex,
            columnIndex);
        OdfNode node = RequireCellNode(origin);
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            "formula",
            OdfNamespaces.TablePrefix,
            formula);
        WriteSpan(
            node,
            "number-matrix-columns-spanned",
            columnCount);
        WriteSpan(
            node,
            "number-matrix-rows-spanned",
            rowCount);
        return RefreshTable(document, table.Node);
    }

    static void RequireRectangle(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool requireMultipleCells)
    {
        if (table == null
            || rowIndex < 0
            || columnIndex < 0
            || rowCount <= 0
            || columnCount <= 0
            || rowIndex > table.RowCount
            || rowCount > table.RowCount - rowIndex
            || columnIndex > table.ColumnCount
            || columnCount > table.ColumnCount - columnIndex
            || requireMultipleCells && rowCount == 1 && columnCount == 1
            || rowCount > int.MaxValue
            || columnCount > int.MaxValue
            || rowCount > int.MaxValue / columnCount)
        {
            throw new OdfEditException(
                "The requested table rectangle is invalid or outside the table.");
        }
    }

    static void RequireCellCoordinate(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        if (table == null
            || rowIndex < 0
            || rowIndex >= table.RowCount
            || columnIndex < 0
            || columnIndex >= table.ColumnCount)
        {
            throw new OdfEditException(
                "The selected cell is outside the table.");
        }
    }

    static void RequireUnoccupiedRange(
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
                OdfCell? cell = TableDocumentModel.GetCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (cell == null
                    || cell.IsCovered
                    || cell.IsMergeOrigin
                    || cell.IsMatrixFormula
                    || (rowOffset != 0 || columnOffset != 0)
                        && cell.NestedTables.Length > 0)
                {
                    throw new OdfEditException(
                        "The requested range intersects a span, covered cell, or nested table.");
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void RequireCoveredRange(
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
                if (rowOffset != 0 || columnOffset != 0)
                {
                    OdfCell? cell = TableDocumentModel.GetCell(
                        table,
                        rowIndex + rowOffset,
                        columnIndex + columnOffset);
                    if (cell == null || !cell.IsCovered)
                    {
                        throw new OdfEditException(
                            "The merged range does not contain the required covered cells.");
                    }
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static OdfTable EnsureMaterializedWidth(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long requiredWidth)
    {
        bool changed = false;
        int physicalIndex = 0;
        while (physicalIndex < table.Rows.Length)
        {
            OdfRow row = table.Rows[physicalIndex];
            long lastRow = row.StartIndex + row.RepeatCount;
            if (row.StartIndex < rowIndex + rowCount
                && lastRow > rowIndex)
            {
                long width = RowWidth(row);
                if (width < requiredWidth)
                {
                    OdfNode rowNode = row.Node ?? new OdfNode();
                    if (row.Node == null)
                    {
                        throw new OdfEditException(
                            "A table row is required for range materialization.");
                    }
                    StyleDocumentModel.AppendNode(
                        rowNode,
                        CreateBlankCell(requiredWidth - width));
                    changed = true;
                }
            }
            physicalIndex = physicalIndex + 1;
        }
        return changed ? ReloadTable(document, table.Node) : table;
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

    static OdfNode CreateBlankCell(long count)
    {
        OdfNode node = new OdfNode();
        node.Kind = OdfNodeKind.Element;
        node.Name = new OdfQualifiedName();
        node.Name.NamespaceName = OdfNamespaces.Table;
        node.Name.LocalName = "table-cell";
        node.Name.Prefix = OdfNamespaces.TablePrefix;
        node.WasEmptyElement = true;
        if (count > 1)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                "number-columns-repeated",
                OdfNamespaces.TablePrefix,
                count.ToString());
        }
        return node;
    }

    static void ConvertToCovered(OdfCell cell)
    {
        if (cell.NestedTables.Length > 0)
        {
            throw new OdfEditException(
                "A cell containing a nested table cannot become covered.");
        }
        OdfNode node = RequireCellNode(cell);
        SetCellKind(node, true);
        RemoveMergeSpans(node);
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-matrix-columns-spanned");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-matrix-rows-spanned");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "formula");
        RemoveValueAttributes(node);
        ClearChildren(node);
    }

    static void ConvertToOrdinary(OdfCell cell)
    {
        SetCellKind(RequireCellNode(cell), false);
    }

    static void SetCellKind(OdfNode node, bool covered)
    {
        if (node.Name == null)
        {
            throw new OdfEditException(
                "A table cell requires an element name.");
        }
        node.Name.NamespaceName = OdfNamespaces.Table;
        node.Name.Prefix = OdfNamespaces.TablePrefix;
        node.Name.LocalName = covered
            ? "covered-table-cell"
            : "table-cell";
    }

    static void RemoveMergeSpans(OdfNode node)
    {
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-columns-spanned");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "number-rows-spanned");
    }

    static void RemoveValueAttributes(OdfNode node)
    {
        int index = node.Attributes.Length - 1;
        while (index >= 0)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && (attribute.Name.LocalName == "value-type"
                    || attribute.Name.LocalName.EndsWith(
                        "-value",
                        StringComparison.Ordinal)
                    || attribute.Name.LocalName == "currency"))
            {
                StyleDocumentModel.RemoveAttribute(
                    node,
                    attribute.Name.NamespaceName,
                    attribute.Name.LocalName);
            }
            index = index - 1;
        }
    }

    static void ClearChildren(OdfNode node)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        node.Children = [];
        node.WasEmptyElement = true;
    }

    static void WriteSpan(
        OdfNode node,
        string localName,
        long value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            value.ToString());
    }

    static OdfNode RequireCellNode(OdfCell cell)
    {
        if (cell == null || cell.Node == null)
        {
            throw new OdfEditException(
                "An attached table cell is required for structural editing.");
        }
        return cell.Node;
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
            "The structurally edited table could not be rematerialized.");
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
