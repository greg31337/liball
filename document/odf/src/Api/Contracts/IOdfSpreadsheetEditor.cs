namespace odf;

/// <summary>
/// Provides sheet lifecycle operations and access to spreadsheet-only data.
/// </summary>
public interface IOdfSpreadsheetEditor
{
    /// <summary>Gets the direct child sheets in display order.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent array containing the sheets.</returns>
    public OdfTable[] GetSheets(OdfDocument document);
    /// <summary>Finds an unambiguously named sheet.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="name">The sheet name.</param>
    /// <returns>The matching sheet, or <c>null</c>.</returns>
    public OdfTable? FindSheet(OdfDocument document, string name);
    /// <summary>Creates a blank sheet.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="name">The unique sheet name.</param>
    /// <param name="rowCount">The initial logical row count.</param>
    /// <param name="columnCount">The initial logical column count.</param>
    /// <returns>The created sheet.</returns>
    public OdfTable CreateSheet(
        OdfDocument document,
        string name,
        long rowCount,
        long columnCount);
    /// <summary>Deletes a sheet.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="sheet">The sheet to delete.</param>
    public void DeleteSheet(OdfDocument document, OdfTable sheet);
    /// <summary>Renames a sheet.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="sheet">The sheet to rename.</param>
    /// <param name="newName">The unique new name.</param>
    public void RenameSheet(
        OdfDocument document,
        OdfTable sheet,
        string newName);
    /// <summary>Moves a sheet to a final display index.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="sheet">The sheet to move.</param>
    /// <param name="destinationIndex">The zero-based final display index.</param>
    /// <returns>The refreshed moved sheet.</returns>
    public OdfTable MoveSheet(
        OdfDocument document,
        OdfTable sheet,
        long destinationIndex);
    /// <summary>Gets the spreadsheet calculation and view settings.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>The editable settings object.</returns>
    public OdfSpreadsheetSettings GetSpreadsheetSettings(OdfDocument document);
    /// <summary>Enables or disables spreadsheet change tracking.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="enabled">Whether change tracking is enabled.</param>
    public void SetSpreadsheetChangeTracking(
        OdfDocument document,
        bool enabled);
    /// <summary>Gets named ranges and expressions.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfNamedExpression[] GetNamedExpressions(OdfDocument document);
    /// <summary>Replaces named ranges and expressions.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetNamedExpressions(
        OdfDocument document,
        OdfNamedExpression[] items);
    /// <summary>Gets label ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfLabelRange[] GetLabelRanges(OdfDocument document);
    /// <summary>Replaces label ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetLabelRanges(OdfDocument document, OdfLabelRange[] items);
    /// <summary>Gets database ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfDatabaseRange[] GetDatabaseRanges(OdfDocument document);
    /// <summary>Replaces database ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetDatabaseRanges(
        OdfDocument document,
        OdfDatabaseRange[] items);
    /// <summary>Gets consolidation declarations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfConsolidation[] GetConsolidations(OdfDocument document);
    /// <summary>Replaces consolidation declarations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetConsolidations(
        OdfDocument document,
        OdfConsolidation[] items);
    /// <summary>Gets spreadsheet scenarios.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfScenario[] GetScenarios(OdfDocument document);
    /// <summary>Replaces spreadsheet scenarios.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetScenarios(OdfDocument document, OdfScenario[] items);
    /// <summary>Gets cell content validations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfContentValidation[] GetContentValidations(OdfDocument document);
    /// <summary>Replaces cell content validations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetContentValidations(
        OdfDocument document,
        OdfContentValidation[] items);
    /// <summary>Gets conditional style mappings.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfConditionalStyle[] GetConditionalStyles(OdfDocument document);
    /// <summary>Replaces conditional style mappings.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetConditionalStyles(
        OdfDocument document,
        OdfConditionalStyle[] items);
    /// <summary>Gets data-pilot or pivot tables.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfPivotTable[] GetPivotTables(OdfDocument document);
    /// <summary>Replaces data-pilot or pivot tables.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetPivotTables(OdfDocument document, OdfPivotTable[] items);
    /// <summary>Gets detective trace ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfDetectiveRange[] GetDetectiveRanges(OdfDocument document);
    /// <summary>Replaces detective trace ranges.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetDetectiveRanges(
        OdfDocument document,
        OdfDetectiveRange[] items);
    /// <summary>Gets detective operations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfDetectiveOperation[] GetDetectiveOperations(
        OdfDocument document);
    /// <summary>Replaces detective operations.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetDetectiveOperations(
        OdfDocument document,
        OdfDetectiveOperation[] items);
    /// <summary>Gets tracked spreadsheet changes.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <returns>An independent collection snapshot.</returns>
    public OdfSpreadsheetChange[] GetSpreadsheetChanges(OdfDocument document);
    /// <summary>Replaces tracked spreadsheet changes.</summary>
    /// <param name="document">The spreadsheet document.</param>
    /// <param name="items">The new collection.</param>
    public void SetSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] items);
}
