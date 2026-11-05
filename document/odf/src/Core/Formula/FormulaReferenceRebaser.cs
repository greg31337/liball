namespace odf;

internal static class FormulaReferenceRebaser
{
    internal static string RebaseCopy(
        string formula,
        string ownerSheet,
        long rowDelta,
        long columnDelta)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(ownerSheet, "rebase");
        edit.DestinationRowIndex = rowDelta;
        edit.DestinationColumnIndex = columnDelta;
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string FollowMovedRange(
        string formula,
        string ownerSheet,
        string targetSheet,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(
            targetSheet,
            "move-range");
        edit.RowIndex = rowIndex;
        edit.ColumnIndex = columnIndex;
        edit.RowCount = rowCount;
        edit.ColumnCount = columnCount;
        edit.DestinationRowIndex = destinationRowIndex;
        edit.DestinationColumnIndex = destinationColumnIndex;
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string AdjustInsertedOrDeletedRows(
        string formula,
        string ownerSheet,
        string targetSheet,
        long rowIndex,
        long removedCount,
        long insertedCount)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(targetSheet, "rows");
        edit.RowIndex = rowIndex;
        edit.RemovedCount = removedCount;
        edit.InsertedCount = insertedCount;
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string AdjustInsertedOrDeletedColumns(
        string formula,
        string ownerSheet,
        string targetSheet,
        long columnIndex,
        long removedCount,
        long insertedCount)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(targetSheet, "columns");
        edit.ColumnIndex = columnIndex;
        edit.RemovedCount = removedCount;
        edit.InsertedCount = insertedCount;
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string RenameSheet(
        string formula,
        string ownerSheet,
        string oldName,
        string newName)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(oldName, "rename-sheet");
        edit.ReplacementTableName = newName;
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string DeleteSheet(
        string formula,
        string ownerSheet,
        string deletedName)
    {
        RequireFormula(formula);
        TableReferenceEdit edit = NewFormulaEdit(
            deletedName,
            "delete-sheet");
        return TableReferenceUpdater.UpdateFormulaText(
            formula,
            edit,
            ownerSheet);
    }

    internal static string PreserveAfterSheetInsertion(
        string formula,
        string insertedName,
        long insertionIndex)
    {
        RequireFormula(formula);
        RequireSheetChange(insertedName, insertionIndex);
        return formula;
    }

    internal static string PreserveAfterSheetReorder(
        string formula,
        string movedName,
        long destinationIndex)
    {
        RequireFormula(formula);
        RequireSheetChange(movedName, destinationIndex);
        return formula;
    }

    static TableReferenceEdit NewFormulaEdit(
        string targetSheet,
        string mode)
    {
        if (targetSheet == null || targetSheet.Length == 0)
        {
            throw new OdfFormulaException(
                "A target sheet name is required for formula rebasing.");
        }
        TableReferenceEdit result = new TableReferenceEdit();
        result.TableName = targetSheet;
        result.Mode = mode;
        return result;
    }

    static void RequireFormula(string formula)
    {
        if (formula == null)
        {
            throw new OdfFormulaException(
                "A formula is required for reference rebasing.");
        }
    }

    static void RequireSheetChange(string sheetName, long index)
    {
        if (sheetName == null || sheetName.Length == 0 || index < 0)
        {
            throw new OdfFormulaException(
                "A sheet name and non-negative index are required.");
        }
    }
}
