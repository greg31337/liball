using System;
using System.Collections.Generic;
using System.Globalization;

namespace odf;

internal class TableRangeCellCopy
{
    internal OdfNode Node = new OdfNode();
    internal long SourceRowIndex;
    internal long SourceColumnIndex;
}

internal static class TableRangeEditor
{
    internal static OdfTable ClearContents(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        RequireRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (!cell.IsCovered)
                {
                    cell = TableRepeatRunEditor.SplitCellForEdit(
                        table,
                        rowIndex + rowOffset,
                        columnIndex + columnOffset);
                    ClearCellContent(RequireNode(cell));
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable ClearFormats(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
    {
        RequireRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
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
                OdfNode node = RequireNode(cell);
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Table,
                    "style-name");
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Table,
                    "protect");
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable TransposeRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        RequireRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        RequireRange(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            columnCount,
            rowCount);
        RequireTransposableRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        if (rowIndex != destinationRowIndex
            || columnIndex != destinationColumnIndex
            || rowCount != columnCount)
        {
            RequireOrdinaryRange(
                table,
                destinationRowIndex,
                destinationColumnIndex,
                columnCount,
                rowCount);
        }
        TableRangeCellCopy[] source = CaptureRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            false);
        TableRangeCellCopy[] transposed = new TableRangeCellCopy[source.Length];
        long sourceRowOffset = 0;
        while (sourceRowOffset < rowCount)
        {
            long sourceColumnOffset = 0;
            while (sourceColumnOffset < columnCount)
            {
                int sourceIndex = (int)(
                    sourceRowOffset * columnCount
                    + sourceColumnOffset);
                int destinationIndex = (int)(
                    sourceColumnOffset * rowCount
                    + sourceRowOffset);
                TableRangeCellCopy copy = source[sourceIndex];
                long targetRow = destinationRowIndex
                    + sourceColumnOffset;
                long targetColumn = destinationColumnIndex
                    + sourceRowOffset;
                TableReferenceUpdater.RebaseSubtree(
                    copy.Node,
                    table.Name,
                    targetRow - copy.SourceRowIndex,
                    targetColumn - copy.SourceColumnIndex);
                SwapSpanAxes(copy.Node);
                transposed[destinationIndex] = copy;
                sourceColumnOffset = sourceColumnOffset + 1;
            }
            sourceRowOffset = sourceRowOffset + 1;
        }
        ClearRangeNodes(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        table = ReloadTable(document, table.Node);
        ReplaceRange(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            columnCount,
            rowCount,
            transposed);
        TableReferenceUpdater.TransposeRange(
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

    internal static OdfTable FillSeries(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        double startValue,
        double stepValue)
    {
        RequireRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        RequireOrdinaryRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        long offset = 0;
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
                WriteNumericCell(
                    RequireNode(cell),
                    startValue + stepValue * offset);
                offset = offset + 1;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return RefreshTable(document, table.Node);
    }

    internal static OdfTable CopySeries(
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
        RequireRange(
            table,
            sourceRowIndex,
            sourceColumnIndex,
            sourceRowCount,
            sourceColumnCount);
        RequireRange(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            destinationRowCount,
            destinationColumnCount);
        RequireOrdinaryRange(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            destinationRowCount,
            destinationColumnCount);
        TableRangeCellCopy[] source = CaptureRange(
            table,
            sourceRowIndex,
            sourceColumnIndex,
            sourceRowCount,
            sourceColumnCount,
            false);
        TableRangeCellCopy[] copies = new TableRangeCellCopy[
            (int)(destinationRowCount * destinationColumnCount)];
        long rowOffset = 0;
        while (rowOffset < destinationRowCount)
        {
            long columnOffset = 0;
            while (columnOffset < destinationColumnCount)
            {
                long seedRow = rowOffset % sourceRowCount;
                long seedColumn = columnOffset % sourceColumnCount;
                int seedIndex = (int)(
                    seedRow * sourceColumnCount + seedColumn);
                TableRangeCellCopy seed = source[seedIndex];
                TableRangeCellCopy copy = new TableRangeCellCopy();
                copy.Node = CloneNode(seed.Node);
                copy.SourceRowIndex = seed.SourceRowIndex;
                copy.SourceColumnIndex = seed.SourceColumnIndex;
                long targetRow = destinationRowIndex + rowOffset;
                long targetColumn = destinationColumnIndex + columnOffset;
                TableReferenceUpdater.RebaseSubtree(
                    copy.Node,
                    table.Name,
                    targetRow - seed.SourceRowIndex,
                    targetColumn - seed.SourceColumnIndex);
                int destination = (int)(
                    rowOffset * destinationColumnCount + columnOffset);
                copies[destination] = copy;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        ReplaceRange(
            table,
            destinationRowIndex,
            destinationColumnIndex,
            destinationRowCount,
            destinationColumnCount,
            copies);
        return RefreshTable(document, table.Node);
    }

    internal static OdfSearchMatch[] SearchRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string searchValue,
        bool matchCase,
        bool exactMatch)
    {
        if (searchValue == null || searchValue.Length == 0)
        {
            throw new OdfEditException(
                "A non-empty table search value is required.");
        }
        RequireRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount);
        List<OdfSearchMatch> matches = new List<OdfSearchMatch>();
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                AddCellMatch(
                    matches,
                    table,
                    cell,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset,
                    searchValue,
                    matchCase,
                    exactMatch);
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return matches.ToArray();
    }

    internal static OdfSearchMatch[] ReplaceRange(
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
        if (replacementValue == null)
        {
            throw new OdfEditException(
                "A table replacement value is required.");
        }
        OdfSearchMatch[] found = SearchRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            searchValue,
            matchCase,
            false);
        int replaceCount = replaceAll ? found.Length : 1;
        if (replaceCount > found.Length)
        {
            replaceCount = found.Length;
        }
        int index = 0;
        while (index < replaceCount)
        {
            OdfSearchMatch match = found[index];
            OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                table,
                match.RowIndex,
                match.ColumnIndex);
            OdfNode node = RequireNode(cell);
            string replaced = ReplaceText(
                match.Context,
                searchValue,
                replacementValue,
                matchCase);
            if (match.IsFormula)
            {
                StyleDocumentModel.WriteAttribute(
                    node,
                    OdfNamespaces.Table,
                    "formula",
                    OdfNamespaces.TablePrefix,
                    replaced);
            }
            else
            {
                WriteStringCell(node, replaced);
            }
            index = index + 1;
        }
        OdfTable refreshed = RefreshTable(document, table.Node);
        index = 0;
        while (index < replaceCount)
        {
            found[index].Table = refreshed;
            OdfCell? refreshedCell = TableDocumentModel.GetCell(
                refreshed,
                found[index].RowIndex,
                found[index].ColumnIndex);
            found[index].Cell = refreshedCell;
            found[index].Node = refreshedCell == null
                ? null
                : refreshedCell.Node;
            index = index + 1;
        }
        if (replaceCount == found.Length)
        {
            return found;
        }
        OdfSearchMatch[] result = new OdfSearchMatch[replaceCount];
        Array.Copy(found, result, replaceCount);
        return result;
    }

    static void RequireRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount)
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
            || rowCount > int.MaxValue
            || columnCount > int.MaxValue
            || rowCount > int.MaxValue / columnCount)
        {
            throw new OdfEditException(
                "The requested table range is invalid or outside the table.");
        }
    }

    static void RequireOrdinaryRange(
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
                OdfCell cell = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (cell.IsCovered
                    || cell.IsMergeOrigin
                    || cell.IsMatrixFormula
                    || cell.NestedTables.Length > 0)
                {
                    throw new OdfEditException(
                        "The table range intersects a span, covered cell, matrix, or nested table.");
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void RequireTransposableRange(
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
                OdfCell cell = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (cell.NestedTables.Length > 0
                    || cell.IsMergeOrigin
                        && (cell.RowSpan > rowCount - rowOffset
                            || cell.ColumnSpan
                                > columnCount - columnOffset)
                    || cell.IsMatrixFormula
                        && (cell.MatrixRowSpan > rowCount - rowOffset
                            || cell.MatrixColumnSpan
                                > columnCount - columnOffset)
                    || cell.IsCovered
                        && !CoveredByOrigin(
                            table,
                            rowIndex,
                            columnIndex,
                            rowCount,
                            columnCount,
                            rowIndex + rowOffset,
                            columnIndex + columnOffset))
                {
                    throw new OdfEditException(
                        "A transposed range must contain complete spans and cannot contain nested tables.");
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static bool CoveredByOrigin(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long coveredRowIndex,
        long coveredColumnIndex)
    {
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell cell = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (cell.IsMergeOrigin
                    && coveredRowIndex >= rowIndex + rowOffset
                    && coveredRowIndex
                        < rowIndex + rowOffset + cell.RowSpan
                    && coveredColumnIndex >= columnIndex + columnOffset
                    && coveredColumnIndex
                        < columnIndex + columnOffset + cell.ColumnSpan)
                {
                    return true;
                }
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return false;
    }

    static TableRangeCellCopy[] CaptureRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool allowNestedTables)
    {
        TableRangeCellCopy[] result = new TableRangeCellCopy[
            (int)(rowCount * columnCount)];
        int destination = 0;
        long rowOffset = 0;
        while (rowOffset < rowCount)
        {
            long columnOffset = 0;
            while (columnOffset < columnCount)
            {
                OdfCell current = RequireCell(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                if (!allowNestedTables && current.NestedTables.Length > 0)
                {
                    throw new OdfEditException(
                        "A range containing nested tables cannot be transposed or repeated.");
                }
                OdfCell cell = TableRepeatRunEditor.SplitCellForEdit(
                    table,
                    rowIndex + rowOffset,
                    columnIndex + columnOffset);
                TableRangeCellCopy copy = new TableRangeCellCopy();
                copy.Node = CloneNode(RequireNode(cell));
                copy.SourceRowIndex = rowIndex + rowOffset;
                copy.SourceColumnIndex = columnIndex + columnOffset;
                result[destination] = copy;
                destination = destination + 1;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
        return result;
    }

    static void ReplaceRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        TableRangeCellCopy[] copies)
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
                ReplaceNode(RequireNode(cell), copies[source].Node);
                source = source + 1;
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void ClearCellContent(OdfNode node)
    {
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Table,
            "formula");
        RemoveOfficeValues(node);
        ClearChildren(node);
    }

    static void ClearRangeNodes(
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
                OdfNode blank = new OdfNode();
                blank.Kind = OdfNodeKind.Element;
                blank.Name = new OdfQualifiedName();
                blank.Name.NamespaceName = OdfNamespaces.Table;
                blank.Name.LocalName = "table-cell";
                blank.Name.Prefix = OdfNamespaces.TablePrefix;
                blank.WasEmptyElement = true;
                ReplaceNode(RequireNode(cell), blank);
                columnOffset = columnOffset + 1;
            }
            rowOffset = rowOffset + 1;
        }
    }

    static void WriteNumericCell(OdfNode node, double value)
    {
        ClearCellContent(node);
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "float");
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Office,
            "value",
            OdfNamespaces.OfficePrefix,
            value.ToString("R", CultureInfo.InvariantCulture));
    }

    static void WriteStringCell(OdfNode node, string value)
    {
        ClearCellContent(node);
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "string");
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Office,
            "string-value",
            OdfNamespaces.OfficePrefix,
            value);
    }

    static void RemoveOfficeValues(OdfNode node)
    {
        int index = node.Attributes.Length - 1;
        while (index >= 0)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && (attribute.Name.LocalName == "value-type"
                    || attribute.Name.LocalName == "currency"
                    || attribute.Name.LocalName.EndsWith(
                        "-value",
                        StringComparison.Ordinal)))
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

    static void SwapSpanAxes(OdfNode node)
    {
        SwapAttributeValues(
            node,
            "number-columns-spanned",
            "number-rows-spanned");
        SwapAttributeValues(
            node,
            "number-matrix-columns-spanned",
            "number-matrix-rows-spanned");
    }

    static void SwapAttributeValues(
        OdfNode node,
        string firstName,
        string secondName)
    {
        string first = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            firstName);
        string second = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            secondName);
        WriteOptionalTableAttribute(node, firstName, second);
        WriteOptionalTableAttribute(node, secondName, first);
    }

    static void WriteOptionalTableAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
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
                value);
        }
    }

    static void AddCellMatch(
        List<OdfSearchMatch> matches,
        OdfTable table,
        OdfCell cell,
        long rowIndex,
        long columnIndex,
        string searchValue,
        bool matchCase,
        bool exactMatch)
    {
        string formula = cell.Formula;
        if (formula.Length > 0
            && TextMatches(formula, searchValue, matchCase, exactMatch))
        {
            matches.Add(CreateMatch(
                table,
                cell,
                rowIndex,
                columnIndex,
                formula,
                searchValue,
                matchCase,
                true));
            return;
        }
        string value = CellSearchText(cell);
        if (value.Length > 0
            && TextMatches(value, searchValue, matchCase, exactMatch))
        {
            matches.Add(CreateMatch(
                table,
                cell,
                rowIndex,
                columnIndex,
                value,
                searchValue,
                matchCase,
                false));
        }
    }

    static OdfSearchMatch CreateMatch(
        OdfTable table,
        OdfCell cell,
        long rowIndex,
        long columnIndex,
        string value,
        string searchValue,
        bool matchCase,
        bool formula)
    {
        OdfSearchMatch result = new OdfSearchMatch();
        result.Table = table;
        result.Cell = cell;
        result.Node = cell.Node;
        result.RowIndex = rowIndex;
        result.ColumnIndex = columnIndex;
        result.MatchedValue = searchValue;
        result.Context = value;
        result.IsExact = TextEquals(value, searchValue, matchCase);
        result.IsFormula = formula;
        if (cell.Node != null)
        {
            result.Location = cell.Node.Location;
        }
        return result;
    }

    static string CellSearchText(OdfCell cell)
    {
        if (cell.Value.Kind == OdfValueKind.String
            || cell.Value.Kind == OdfValueKind.Error)
        {
            return cell.Value.StringValue;
        }
        if (cell.Value.Kind == OdfValueKind.Boolean)
        {
            return cell.Value.BooleanValue ? "true" : "false";
        }
        return cell.Value.LexicalValue;
    }

    static bool TextMatches(
        string value,
        string searchValue,
        bool matchCase,
        bool exactMatch)
    {
        if (exactMatch)
        {
            return TextEquals(value, searchValue, matchCase);
        }
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        return value.IndexOf(searchValue, comparison) >= 0;
    }

    static bool TextEquals(
        string value,
        string searchValue,
        bool matchCase)
    {
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        return string.Equals(value, searchValue, comparison);
    }

    static string ReplaceText(
        string value,
        string searchValue,
        string replacementValue,
        bool matchCase)
    {
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        return value.Replace(
            searchValue,
            replacementValue,
            comparison);
    }

    static OdfCell RequireCell(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        OdfCell? cell = TableDocumentModel.GetCell(
            table,
            rowIndex,
            columnIndex);
        if (cell == null)
        {
            throw new OdfEditException(
                "The table range contains an unmaterialized cell.");
        }
        return cell;
    }

    static OdfNode RequireNode(OdfCell cell)
    {
        if (cell.Node == null)
        {
            throw new OdfEditException(
                "An attached cell node is required for range editing.");
        }
        return cell.Node;
    }

    static void ReplaceNode(OdfNode existing, OdfNode replacement)
    {
        OdfNode? parent = existing.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "The cell node is detached from its row.");
        }
        int index = 0;
        while (index < parent.Children.Length
            && parent.Children[index] != existing)
        {
            index = index + 1;
        }
        if (index == parent.Children.Length)
        {
            throw new OdfEditException(
                "The cell row does not contain the cell node.");
        }
        parent.Children[index] = replacement;
        replacement.Parent = parent;
        existing.Parent = null;
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
            "The range-edited table could not be rematerialized.");
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
