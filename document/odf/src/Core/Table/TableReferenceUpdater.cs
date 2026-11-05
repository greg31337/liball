using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace odf;

internal class TableReferenceEdit
{
    internal string TableName = "";
    internal string Mode = "";
    internal long RowIndex;
    internal long ColumnIndex;
    internal long RowCount;
    internal long ColumnCount;
    internal long DestinationRowIndex;
    internal long DestinationColumnIndex;
    internal long RemovedCount;
    internal long InsertedCount;
    internal long TableIndex = -1;
    internal string ReplacementTableName = "";
}

internal class TableReferenceCoordinate
{
    internal BigInteger Value;
    internal bool IsDeleted;
}

internal static class TableReferenceUpdater
{
    internal static void AdjustRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long removedCount,
        long insertedCount)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "rows",
            rowIndex,
            0,
            removedCount,
            insertedCount);
        UpdateDocument(document, edit);
    }

    internal static void AdjustColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long removedCount,
        long insertedCount)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "columns",
            0,
            columnIndex,
            removedCount,
            insertedCount);
        UpdateDocument(document, edit);
    }

    internal static void AdjustCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long removedCount,
        long insertedCount)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "cells",
            rowIndex,
            columnIndex,
            removedCount,
            insertedCount);
        UpdateDocument(document, edit);
    }

    internal static void MoveRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "move-rows",
            rowIndex,
            0,
            0,
            0);
        edit.RowCount = rowCount;
        edit.DestinationRowIndex = destinationRowIndex;
        UpdateDocument(document, edit);
    }

    internal static void MoveColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "move-columns",
            0,
            columnIndex,
            0,
            0);
        edit.ColumnCount = columnCount;
        edit.DestinationColumnIndex = destinationColumnIndex;
        UpdateDocument(document, edit);
    }

    internal static void MoveRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "move-range",
            rowIndex,
            columnIndex,
            0,
            0);
        edit.RowCount = rowCount;
        edit.ColumnCount = columnCount;
        edit.DestinationRowIndex = destinationRowIndex;
        edit.DestinationColumnIndex = destinationColumnIndex;
        UpdateDocument(document, edit);
    }

    internal static void TransposeRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        TableReferenceEdit edit = NewEdit(
            table,
            "transpose-range",
            rowIndex,
            columnIndex,
            0,
            0);
        edit.RowCount = rowCount;
        edit.ColumnCount = columnCount;
        edit.DestinationRowIndex = destinationRowIndex;
        edit.DestinationColumnIndex = destinationColumnIndex;
        UpdateDocument(document, edit);
    }

    internal static void RebaseSubtree(
        OdfNode node,
        string tableName,
        long rowDelta,
        long columnDelta)
    {
        TableReferenceEdit edit = new TableReferenceEdit();
        edit.TableName = tableName;
        edit.Mode = "rebase";
        edit.DestinationRowIndex = rowDelta;
        edit.DestinationColumnIndex = columnDelta;
        UpdateNode(node, edit, tableName);
    }

    internal static void RenameSheetReferences(
        OdfDocument document,
        string oldName,
        string newName)
    {
        TableReferenceEdit edit = new TableReferenceEdit();
        edit.TableName = oldName;
        edit.ReplacementTableName = newName;
        edit.Mode = "rename-sheet";
        UpdateDocument(document, edit);
    }

    internal static void DeleteSheetReferences(
        OdfDocument document,
        string deletedName)
    {
        TableReferenceEdit edit = new TableReferenceEdit();
        edit.TableName = deletedName;
        edit.Mode = "delete-sheet";
        UpdateDocument(document, edit);
    }

    static TableReferenceEdit NewEdit(
        OdfTable table,
        string mode,
        long rowIndex,
        long columnIndex,
        long removedCount,
        long insertedCount)
    {
        if (table == null)
        {
            throw new OdfEditException(
                "A table is required for reference adjustment.");
        }
        TableReferenceEdit edit = new TableReferenceEdit();
        edit.TableName = table.Name;
        edit.Mode = mode;
        edit.RowIndex = rowIndex;
        edit.ColumnIndex = columnIndex;
        edit.RemovedCount = removedCount;
        edit.InsertedCount = insertedCount;
        return edit;
    }

    static void UpdateDocument(
        OdfDocument document,
        TableReferenceEdit edit)
    {
        edit.TableIndex = FindEditedTableIndex(document, edit.TableName);
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfNode? root = document.Parts[index].Root;
            if (root != null)
            {
                UpdateNode(root, edit, "");
            }
            index = index + 1;
        }
        EmbeddedContentManager.ReloadChartsAfterReferenceUpdate(document);
    }

    static void UpdateNode(
        OdfNode node,
        TableReferenceEdit edit,
        string inheritedTableName)
    {
        string tableName = inheritedTableName;
        if (IsTableNode(node))
        {
            tableName = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Table,
                "name");
        }
        UpdateTrackedCoordinateNode(node, edit);
        UpdateSelectedRanges(node, edit);
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (IsFormulaAttribute(attribute))
            {
                attribute.Value = UpdateFormulaText(
                    attribute.Value,
                    edit,
                    tableName);
            }
            else if (IsRangeAttribute(attribute))
            {
                attribute.Value = UpdateRangeList(
                    attribute.Value,
                    edit,
                    tableName);
            }
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            UpdateNode(node.Children[childIndex], edit, tableName);
            childIndex = childIndex + 1;
        }
    }

    static void UpdateSelectedRanges(
        OdfNode node,
        TableReferenceEdit edit)
    {
        if (node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Config
            || node.Name.LocalName != "config-item"
            || StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Config,
                "name") != "SelectedRanges")
        {
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                child.Value = UpdateRangeList(
                    child.Value,
                    edit,
                    edit.TableName);
            }
            index = index + 1;
        }
    }

    static long FindEditedTableIndex(
        OdfDocument document,
        string tableName)
    {
        int index = 0;
        while (index < document.Tables.Length)
        {
            if (document.Tables[index].Name == tableName)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static void UpdateTrackedCoordinateNode(
        OdfNode node,
        TableReferenceEdit edit)
    {
        if (edit.TableIndex < 0 || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Table)
        {
            return;
        }
        string localName = node.Name.LocalName;
        if (localName == "cell-address")
        {
            if (!NumericTableMatches(node, "table", edit.TableIndex))
            {
                return;
            }
            UpdateNumericCoordinate(node, "row", edit, true, true);
            UpdateNumericCoordinate(node, "column", edit, false, true);
        }
        else if (localName == "source-range-address"
            || localName == "target-range-address")
        {
            UpdateNumericRangeCoordinates(node, edit);
        }
    }

    static void UpdateNumericRangeCoordinates(
        OdfNode node,
        TableReferenceEdit edit)
    {
        if (NumericTableMatches(node, "start-table", edit.TableIndex))
        {
            UpdateNumericCoordinate(node, "start-row", edit, true, true);
            UpdateNumericCoordinate(node, "start-column", edit, false, true);
        }
        if (NumericTableMatches(node, "end-table", edit.TableIndex))
        {
            UpdateNumericCoordinate(node, "end-row", edit, true, false);
            UpdateNumericCoordinate(node, "end-column", edit, false, false);
        }
        if (NumericTableMatches(node, "table", edit.TableIndex))
        {
            UpdateNumericCoordinate(node, "row", edit, true, true);
            UpdateNumericCoordinate(node, "column", edit, false, true);
        }
    }

    static bool NumericTableMatches(
        OdfNode node,
        string localName,
        long tableIndex)
    {
        OdfAttribute? attribute = FindTableAttributeNode(node, localName);
        if (attribute == null)
        {
            return false;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            attribute.Value);
        return parsed.IsValid && parsed.IntegerValue == tableIndex;
    }

    static void UpdateNumericCoordinate(
        OdfNode node,
        string localName,
        TableReferenceEdit edit,
        bool row,
        bool rangeStart)
    {
        OdfAttribute? attribute = FindTableAttributeNode(node, localName);
        if (attribute == null)
        {
            return;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            attribute.Value);
        if (!parsed.IsValid || parsed.IntegerValue < 0)
        {
            return;
        }
        BigInteger value = parsed.IntegerValue + 1;
        if (row && edit.Mode == "rows")
        {
            TableReferenceCoordinate adjusted = AdjustCoordinate(
                value,
                edit.RowIndex,
                edit.RemovedCount,
                edit.InsertedCount,
                rangeStart);
            attribute.Value = NumericZeroBasedValue(adjusted.Value);
        }
        else if (!row && edit.Mode == "columns")
        {
            TableReferenceCoordinate adjusted = AdjustCoordinate(
                value,
                edit.ColumnIndex,
                edit.RemovedCount,
                edit.InsertedCount,
                rangeStart);
            attribute.Value = NumericZeroBasedValue(adjusted.Value);
        }
        else if (row && edit.Mode == "move-rows")
        {
            attribute.Value = NumericZeroBasedValue(MoveCoordinate(
                value,
                edit.RowIndex,
                edit.RowCount,
                edit.DestinationRowIndex));
        }
        else if (!row && edit.Mode == "move-columns")
        {
            attribute.Value = NumericZeroBasedValue(MoveCoordinate(
                value,
                edit.ColumnIndex,
                edit.ColumnCount,
                edit.DestinationColumnIndex));
        }
    }

    static string NumericZeroBasedValue(BigInteger value)
    {
        BigInteger zeroBased = value - 1;
        if (zeroBased < 0)
        {
            zeroBased = 0;
        }
        return zeroBased.ToString();
    }

    static OdfAttribute? FindTableAttributeNode(
        OdfNode node,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Table
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    internal static string UpdateFormulaText(
        string formula,
        TableReferenceEdit edit,
        string ownerTableName)
    {
        StringBuilder result = new StringBuilder();
        bool quoted = false;
        int copyStart = 0;
        int index = 0;
        while (index < formula.Length)
        {
            char character = formula[index];
            if (character == '"')
            {
                if (quoted
                    && index + 1 < formula.Length
                    && formula[index + 1] == '"')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && character == '[')
            {
                int end = FindBracketEnd(formula, index + 1);
                if (end >= 0)
                {
                    string lexical = formula.Substring(
                        index,
                        end - index + 1);
                    OdfCellRangeValue range =
                        OdfCellAddressParser.ParseFormulaReference(lexical);
                    if (range.IsValid
                        && RangeReferencesTable(
                            range,
                            edit.TableName,
                            ownerTableName))
                    {
                        result.Append(formula, copyStart, index - copyStart);
                        if (TransformRange(range, edit, ownerTableName))
                        {
                            result.Append(
                                OdfCellAddressFormatter.FormatFormulaReference(
                                    range));
                        }
                        else
                        {
                            result.Append(OdfFormulaNames.ErrorReference);
                        }
                        index = end + 1;
                        copyStart = index;
                        continue;
                    }
                }
            }
            index = index + 1;
        }
        if (copyStart == 0)
        {
            return UpdateNamedReferences(formula, edit);
        }
        result.Append(formula, copyStart, formula.Length - copyStart);
        return UpdateNamedReferences(result.ToString(), edit);
    }

    static int FindBracketEnd(string value, int start)
    {
        bool quoted = false;
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == '\'')
            {
                if (quoted
                    && index + 1 < value.Length
                    && value[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && value[index] == ']')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string UpdateNamedReferences(
        string formula,
        TableReferenceEdit edit)
    {
        if (edit.Mode != "rename-sheet" && edit.Mode != "delete-sheet")
        {
            return formula;
        }
        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(formula);
        StringBuilder result = new StringBuilder();
        int copyStart = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            OdfFormulaToken token = tokens[index];
            string replacement = UpdateNamedReferenceToken(token, edit);
            if (replacement != token.Text)
            {
                result.Append(
                    formula,
                    copyStart,
                    token.Start - copyStart);
                result.Append(replacement);
                copyStart = token.Start + token.Length;
            }
            index = index + 1;
        }
        if (copyStart == 0)
        {
            return formula;
        }
        result.Append(formula, copyStart, formula.Length - copyStart);
        return result.ToString();
    }

    static string UpdateNamedReferenceToken(
        OdfFormulaToken token,
        TableReferenceEdit edit)
    {
        if (token.Kind != OdfFormulaTokenKind.NamedExpression
            || token.Text.Length == 0
            || token.Text[0] != '\'')
        {
            return token.Text;
        }
        int quoteEnd = FindNamedQualifierEnd(token.Text);
        if (quoteEnd < 0
            || quoteEnd + 1 >= token.Text.Length
            || token.Text[quoteEnd + 1] != '.')
        {
            return token.Text;
        }
        string qualifier = token.Text.Substring(0, quoteEnd + 1);
        OdfQuotedSheetValue parsed =
            OdfCellAddressParser.ParseQuotedSheetName(qualifier);
        if (!parsed.IsValid || parsed.SheetName != edit.TableName)
        {
            return token.Text;
        }
        if (edit.Mode == "delete-sheet")
        {
            return OdfFormulaNames.ErrorReference;
        }
        OdfQuotedSheetValue renamed = new OdfQuotedSheetValue();
        renamed.LexicalValue = qualifier;
        renamed.SheetName = edit.ReplacementTableName;
        renamed.IsAbsolute = parsed.IsAbsolute;
        renamed.IsValid = true;
        return OdfCellAddressFormatter.FormatQuotedSheetName(renamed)
            + token.Text.Substring(quoteEnd + 1);
    }

    static int FindNamedQualifierEnd(string value)
    {
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] == '\'')
            {
                if (index + 1 < value.Length && value[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    return index;
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return -1;
    }

    static string UpdateRangeList(
        string lexical,
        TableReferenceEdit edit,
        string ownerTableName)
    {
        OdfCellRangeListValue list =
            OdfCellAddressParser.ParseRangeList(lexical);
        if (!list.IsValid)
        {
            return lexical;
        }
        bool changed = false;
        List<OdfCellRangeValue> retained =
            new List<OdfCellRangeValue>();
        int index = 0;
        while (index < list.Ranges.Length)
        {
            OdfCellRangeValue range = list.Ranges[index];
            if (RangeReferencesTable(
                range,
                edit.TableName,
                ownerTableName))
            {
                changed = true;
                if (TransformRange(range, edit, ownerTableName))
                {
                    retained.Add(range);
                }
            }
            else
            {
                retained.Add(range);
            }
            index = index + 1;
        }
        if (!changed)
        {
            return lexical;
        }
        if (retained.Count == 0)
        {
            return "";
        }
        list.Ranges = retained.ToArray();
        return OdfCellAddressFormatter.FormatRangeList(list);
    }

    static bool TransformRange(
        OdfCellRangeValue range,
        TableReferenceEdit edit,
        string ownerTableName)
    {
        if (edit.Mode == "delete-sheet")
        {
            return !RangeHasExplicitSheet(range, edit.TableName);
        }
        if (edit.Mode == "rename-sheet")
        {
            RenameRangeSheets(
                range,
                edit.TableName,
                edit.ReplacementTableName);
            return true;
        }
        if (RangeIsWhollyDeleted(range, edit))
        {
            return false;
        }
        bool startValid = TransformAddress(
            range.Start,
            edit,
            true,
            ownerTableName);
        bool endValid = true;
        if (range.HasEnd)
        {
            string endOwnerTableName = range.Start.HasSheet
                ? range.Start.SheetName
                : ownerTableName;
            endValid = TransformAddress(
                range.End,
                edit,
                false,
                endOwnerTableName);
        }
        if (!range.HasEnd)
        {
            return startValid;
        }
        return (startValid && endValid)
            || (AddressCoordinatesArePositive(range.Start)
                && AddressCoordinatesArePositive(range.End));
    }

    static bool RangeHasExplicitSheet(
        OdfCellRangeValue range,
        string sheetName)
    {
        if (range.Start.HasSource)
        {
            return false;
        }
        return (range.Start.HasSheet
                && range.Start.SheetName == sheetName)
            || (range.HasEnd
                && range.End.HasSheet
                && range.End.SheetName == sheetName);
    }

    static void RenameRangeSheets(
        OdfCellRangeValue range,
        string oldName,
        string newName)
    {
        if (range.Start.HasSource)
        {
            return;
        }
        if (range.Start.HasSheet && range.Start.SheetName == oldName)
        {
            range.Start.SheetName = newName;
        }
        if (range.HasEnd
            && range.End.HasSheet
            && range.End.SheetName == oldName)
        {
            range.End.SheetName = newName;
        }
    }

    static bool TransformAddress(
        OdfCellAddressValue address,
        TableReferenceEdit edit,
        bool rangeStart,
        string ownerTableName)
    {
        if (!ReferenceBelongsToTable(
                address,
                edit.TableName,
                ownerTableName))
        {
            return true;
        }
        if (edit.Mode == "rebase")
        {
            if (address.HasRow && !address.RowAbsolute)
            {
                address.Row = address.Row + edit.DestinationRowIndex;
            }
            if (address.HasColumn && !address.ColumnAbsolute)
            {
                address.ColumnIndex = address.ColumnIndex
                    + edit.DestinationColumnIndex;
            }
            return AddressCoordinatesArePositive(address);
        }
        if (edit.Mode == "rows" && address.HasRow)
        {
            TableReferenceCoordinate row = AdjustCoordinate(
                address.Row,
                edit.RowIndex,
                edit.RemovedCount,
                edit.InsertedCount,
                rangeStart);
            address.Row = row.Value;
            return !row.IsDeleted;
        }
        if (edit.Mode == "columns" && address.HasColumn)
        {
            TableReferenceCoordinate column = AdjustCoordinate(
                address.ColumnIndex,
                edit.ColumnIndex,
                edit.RemovedCount,
                edit.InsertedCount,
                rangeStart);
            address.ColumnIndex = column.Value;
            return !column.IsDeleted;
        }
        if (edit.Mode == "cells"
            && address.HasRow
            && address.HasColumn
            && address.Row == edit.RowIndex + 1)
        {
            TableReferenceCoordinate cell = AdjustCoordinate(
                address.ColumnIndex,
                edit.ColumnIndex,
                edit.RemovedCount,
                edit.InsertedCount,
                rangeStart);
            address.ColumnIndex = cell.Value;
            return !cell.IsDeleted;
        }
        if (edit.Mode == "move-rows" && address.HasRow)
        {
            address.Row = MoveCoordinate(
                address.Row,
                edit.RowIndex,
                edit.RowCount,
                edit.DestinationRowIndex);
        }
        else if (edit.Mode == "move-columns" && address.HasColumn)
        {
            address.ColumnIndex = MoveCoordinate(
                address.ColumnIndex,
                edit.ColumnIndex,
                edit.ColumnCount,
                edit.DestinationColumnIndex);
        }
        else if (edit.Mode == "move-range"
            && address.HasRow
            && address.HasColumn
            && CoordinateInRange(
                address.Row,
                edit.RowIndex,
                edit.RowCount)
            && CoordinateInRange(
                address.ColumnIndex,
                edit.ColumnIndex,
                edit.ColumnCount))
        {
            address.Row = address.Row
                + edit.DestinationRowIndex
                - edit.RowIndex;
            address.ColumnIndex = address.ColumnIndex
                + edit.DestinationColumnIndex
                - edit.ColumnIndex;
        }
        else if (edit.Mode == "transpose-range"
            && address.HasRow
            && address.HasColumn
            && CoordinateInRange(
                address.Row,
                edit.RowIndex,
                edit.RowCount)
            && CoordinateInRange(
                address.ColumnIndex,
                edit.ColumnIndex,
                edit.ColumnCount))
        {
            BigInteger oldRow = address.Row;
            BigInteger oldColumn = address.ColumnIndex;
            address.Row = edit.DestinationRowIndex
                + oldColumn
                - edit.ColumnIndex;
            address.ColumnIndex = edit.DestinationColumnIndex
                + oldRow
                - edit.RowIndex;
        }
        return true;
    }

    static TableReferenceCoordinate AdjustCoordinate(
        BigInteger value,
        long zeroBasedIndex,
        long removedCount,
        long insertedCount,
        bool rangeStart)
    {
        TableReferenceCoordinate result = new TableReferenceCoordinate();
        result.Value = value;
        BigInteger zeroBased = value - 1;
        if (removedCount > 0)
        {
            if (zeroBased >= zeroBasedIndex + removedCount)
            {
                result.Value = value - removedCount;
            }
            else if (zeroBased >= zeroBasedIndex)
            {
                result.IsDeleted = true;
                result.Value = rangeStart
                    ? zeroBasedIndex + 1
                    : zeroBasedIndex;
            }
        }
        if (insertedCount > 0 && zeroBased >= zeroBasedIndex)
        {
            result.Value = result.Value + insertedCount;
        }
        return result;
    }

    static BigInteger MoveCoordinate(
        BigInteger value,
        long zeroBasedStart,
        long count,
        long destination)
    {
        BigInteger zeroBased = value - 1;
        long finalStart = destination > zeroBasedStart
            ? destination - count
            : destination;
        if (zeroBased >= zeroBasedStart
            && zeroBased < zeroBasedStart + count)
        {
            return finalStart + (zeroBased - zeroBasedStart) + 1;
        }
        if (destination < zeroBasedStart
            && zeroBased >= destination
            && zeroBased < zeroBasedStart)
        {
            return value + count;
        }
        if (destination > zeroBasedStart + count
            && zeroBased >= zeroBasedStart + count
            && zeroBased < destination)
        {
            return value - count;
        }
        return value;
    }

    static bool RangeIsWhollyDeleted(
        OdfCellRangeValue range,
        TableReferenceEdit edit)
    {
        if (edit.RemovedCount <= 0)
        {
            return false;
        }
        if (!range.HasEnd)
        {
            if (edit.Mode == "rows" && range.Start.HasRow)
            {
                return CoordinateInRange(
                    range.Start.Row,
                    edit.RowIndex,
                    edit.RemovedCount);
            }
            if (edit.Mode == "columns" && range.Start.HasColumn)
            {
                return CoordinateInRange(
                    range.Start.ColumnIndex,
                    edit.ColumnIndex,
                    edit.RemovedCount);
            }
            if (edit.Mode == "cells"
                && range.Start.HasRow
                && range.Start.HasColumn
                && range.Start.Row == edit.RowIndex + 1)
            {
                return CoordinateInRange(
                    range.Start.ColumnIndex,
                    edit.ColumnIndex,
                    edit.RemovedCount);
            }
            return false;
        }
        if (edit.Mode == "rows")
        {
            return AxisRangeInside(
                range.Start.Row,
                range.End.Row,
                edit.RowIndex,
                edit.RemovedCount);
        }
        if (edit.Mode == "columns")
        {
            return AxisRangeInside(
                range.Start.ColumnIndex,
                range.End.ColumnIndex,
                edit.ColumnIndex,
                edit.RemovedCount);
        }
        if (edit.Mode == "cells"
            && range.Start.HasRow
            && range.End.HasRow
            && range.Start.Row == edit.RowIndex + 1
            && range.End.Row == edit.RowIndex + 1)
        {
            return AxisRangeInside(
                range.Start.ColumnIndex,
                range.End.ColumnIndex,
                edit.ColumnIndex,
                edit.RemovedCount);
        }
        return false;
    }

    static bool AxisRangeInside(
        BigInteger first,
        BigInteger second,
        long start,
        long count)
    {
        BigInteger low = first < second ? first : second;
        BigInteger high = first > second ? first : second;
        return low - 1 >= start && high - 1 < start + count;
    }

    static bool AddressCoordinatesArePositive(
        OdfCellAddressValue address)
    {
        return (!address.HasRow || address.Row > 0)
            && (!address.HasColumn || address.ColumnIndex > 0);
    }

    static bool CoordinateInRange(
        BigInteger coordinate,
        long start,
        long count)
    {
        BigInteger zeroBased = coordinate - 1;
        return zeroBased >= start && zeroBased < start + count;
    }

    static bool ReferenceBelongsToTable(
        OdfCellAddressValue address,
        string targetTableName,
        string ownerTableName)
    {
        if (address.HasSource)
        {
            return false;
        }
        return address.HasSheet
            ? address.SheetName == targetTableName
            : ownerTableName == targetTableName;
    }

    static bool RangeReferencesTable(
        OdfCellRangeValue range,
        string targetTableName,
        string ownerTableName)
    {
        if (ReferenceBelongsToTable(
            range.Start,
            targetTableName,
            ownerTableName))
        {
            return true;
        }
        if (!range.HasEnd)
        {
            return false;
        }
        string endOwnerTableName = range.Start.HasSheet
            ? range.Start.SheetName
            : ownerTableName;
        return ReferenceBelongsToTable(
            range.End,
            targetTableName,
            endOwnerTableName);
    }

    static bool IsTableNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == "table";
    }

    static bool IsRangeAttribute(OdfAttribute attribute)
    {
        string localName = attribute.Name.LocalName;
        return localName == "print-ranges"
            || localName == "buttons"
            || localName == "cell-range"
            || localName == "repeat-row-range"
            || localName == "repeat-column-range"
            || localName == "scenario-ranges"
            || localName == "source-cell-range-addresses"
            || localName == "error-lower-range"
            || localName == "error-upper-range"
            || localName.EndsWith("cell-address", StringComparison.Ordinal)
            || localName.EndsWith(
                "cell-range-address",
                StringComparison.Ordinal)
            || localName.EndsWith("range-address", StringComparison.Ordinal);
    }

    static bool IsFormulaAttribute(OdfAttribute attribute)
    {
        string localName = attribute.Name.LocalName;
        return localName == "formula"
            || localName == "expression"
            || localName == "condition";
    }
}
