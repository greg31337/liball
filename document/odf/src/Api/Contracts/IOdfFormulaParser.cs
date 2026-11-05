namespace odf;

/// <summary>
/// Parses, formats, and rebases OpenFormula expressions.
/// </summary>
public interface IOdfFormulaParser
{
    /// <summary>Parses an OpenFormula expression.</summary>
    /// <param name="source">The formula source text.</param>
    /// <param name="maximumDepth">The positive nesting-depth limit.</param>
    /// <returns>The parsed formula and diagnostics.</returns>
    public OdfFormula ParseFormula(string source, int maximumDepth);
    /// <summary>Formats a formula while preserving unchanged lexical text.</summary>
    /// <param name="formula">The parsed formula.</param>
    /// <returns>The preserved or regenerated formula text.</returns>
    public string FormatFormulaPreserved(OdfFormula formula);
    /// <summary>Formats a formula in canonical lexical form.</summary>
    /// <param name="formula">The parsed formula.</param>
    /// <returns>The canonical formula text.</returns>
    public string FormatFormulaCanonical(OdfFormula formula);
    /// <summary>Rebases relative references for a copied formula.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="rowDelta">The signed row displacement.</param>
    /// <param name="columnDelta">The signed column displacement.</param>
    /// <returns>The rebased formula text.</returns>
    public string RebaseFormulaCopy(
        string formula,
        string ownerSheet,
        long rowDelta,
        long columnDelta);
    /// <summary>Updates references that follow a moved cell range.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="targetSheet">The moved range sheet.</param>
    /// <param name="rowIndex">The zero-based source row.</param>
    /// <param name="columnIndex">The zero-based source column.</param>
    /// <param name="rowCount">The moved row count.</param>
    /// <param name="columnCount">The moved column count.</param>
    /// <param name="destinationRowIndex">The zero-based destination row.</param>
    /// <param name="destinationColumnIndex">The zero-based destination column.</param>
    /// <returns>The updated formula text.</returns>
    public string RebaseFormulaMovedRange(
        string formula,
        string ownerSheet,
        string targetSheet,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex);
    /// <summary>Updates references after row insertion or deletion.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="targetSheet">The edited sheet.</param>
    /// <param name="rowIndex">The zero-based edit row.</param>
    /// <param name="removedCount">The removed row count.</param>
    /// <param name="insertedCount">The inserted row count.</param>
    /// <returns>The updated formula text.</returns>
    public string RebaseFormulaRows(
        string formula,
        string ownerSheet,
        string targetSheet,
        long rowIndex,
        long removedCount,
        long insertedCount);
    /// <summary>Updates references after column insertion or deletion.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="targetSheet">The edited sheet.</param>
    /// <param name="columnIndex">The zero-based edit column.</param>
    /// <param name="removedCount">The removed column count.</param>
    /// <param name="insertedCount">The inserted column count.</param>
    /// <returns>The updated formula text.</returns>
    public string RebaseFormulaColumns(
        string formula,
        string ownerSheet,
        string targetSheet,
        long columnIndex,
        long removedCount,
        long insertedCount);
    /// <summary>Updates references after a sheet rename.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="oldName">The previous sheet name.</param>
    /// <param name="newName">The replacement sheet name.</param>
    /// <returns>The updated formula text.</returns>
    public string RebaseFormulaSheetRename(
        string formula,
        string ownerSheet,
        string oldName,
        string newName);
    /// <summary>Invalidates references after a sheet deletion.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="ownerSheet">The owning sheet name.</param>
    /// <param name="deletedName">The deleted sheet name.</param>
    /// <returns>The updated formula text.</returns>
    public string RebaseFormulaSheetDeletion(
        string formula,
        string ownerSheet,
        string deletedName);
    /// <summary>Preserves references after a sheet insertion.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="insertedName">The inserted sheet name.</param>
    /// <param name="insertionIndex">The zero-based insertion index.</param>
    /// <returns>The preserved formula text.</returns>
    public string PreserveFormulaSheetInsertion(
        string formula,
        string insertedName,
        long insertionIndex);
    /// <summary>Preserves references after a sheet reorder.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="movedName">The moved sheet name.</param>
    /// <param name="destinationIndex">The zero-based destination index.</param>
    /// <returns>The preserved formula text.</returns>
    public string PreserveFormulaSheetReorder(
        string formula,
        string movedName,
        long destinationIndex);
}
