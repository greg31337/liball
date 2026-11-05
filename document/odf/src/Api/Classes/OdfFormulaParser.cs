namespace odf;

/// <summary>
/// Parses, formats, and rebases OpenFormula expressions.
/// </summary>
public class OdfFormulaParser : IOdfFormulaParser
{
    /// <summary>Initializes a formula parser.</summary>
    public OdfFormulaParser()
    {
    }

    /// <summary>Parses an OpenFormula expression.</summary>
    public OdfFormula ParseFormula(string source, int maximumDepth)
    {
        return FormulaParser.Parse(source, maximumDepth);
    }

    /// <summary>Formats a formula while preserving unchanged lexical text.</summary>
    public string FormatFormulaPreserved(OdfFormula formula)
    {
        return FormulaFormatter.FormatPreserved(formula);
    }

    /// <summary>Formats a formula in canonical lexical form.</summary>
    public string FormatFormulaCanonical(OdfFormula formula)
    {
        return FormulaFormatter.FormatCanonical(formula);
    }

    /// <summary>Rebases relative references for a copied formula.</summary>
    public string RebaseFormulaCopy(
        string formula,
        string ownerSheet,
        long rowDelta,
        long columnDelta)
    {
        return FormulaReferenceRebaser.RebaseCopy(
            formula,
            ownerSheet,
            rowDelta,
            columnDelta);
    }

    /// <summary>Updates references that follow a moved cell range.</summary>
    public string RebaseFormulaMovedRange(
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
        return FormulaReferenceRebaser.FollowMovedRange(
            formula,
            ownerSheet,
            targetSheet,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            destinationRowIndex,
            destinationColumnIndex);
    }

    /// <summary>Updates references after row insertion or deletion.</summary>
    public string RebaseFormulaRows(
        string formula,
        string ownerSheet,
        string targetSheet,
        long rowIndex,
        long removedCount,
        long insertedCount)
    {
        return FormulaReferenceRebaser.AdjustInsertedOrDeletedRows(
            formula,
            ownerSheet,
            targetSheet,
            rowIndex,
            removedCount,
            insertedCount);
    }

    /// <summary>Updates references after column insertion or deletion.</summary>
    public string RebaseFormulaColumns(
        string formula,
        string ownerSheet,
        string targetSheet,
        long columnIndex,
        long removedCount,
        long insertedCount)
    {
        return FormulaReferenceRebaser.AdjustInsertedOrDeletedColumns(
            formula,
            ownerSheet,
            targetSheet,
            columnIndex,
            removedCount,
            insertedCount);
    }

    /// <summary>Updates references after a sheet rename.</summary>
    public string RebaseFormulaSheetRename(
        string formula,
        string ownerSheet,
        string oldName,
        string newName)
    {
        return FormulaReferenceRebaser.RenameSheet(
            formula,
            ownerSheet,
            oldName,
            newName);
    }

    /// <summary>Invalidates references after a sheet deletion.</summary>
    public string RebaseFormulaSheetDeletion(
        string formula,
        string ownerSheet,
        string deletedName)
    {
        return FormulaReferenceRebaser.DeleteSheet(
            formula,
            ownerSheet,
            deletedName);
    }

    /// <summary>Preserves references after a sheet insertion.</summary>
    public string PreserveFormulaSheetInsertion(
        string formula,
        string insertedName,
        long insertionIndex)
    {
        return FormulaReferenceRebaser.PreserveAfterSheetInsertion(
            formula,
            insertedName,
            insertionIndex);
    }

    /// <summary>Preserves references after a sheet reorder.</summary>
    public string PreserveFormulaSheetReorder(
        string formula,
        string movedName,
        long destinationIndex)
    {
        return FormulaReferenceRebaser.PreserveAfterSheetReorder(
            formula,
            movedName,
            destinationIndex);
    }
}
