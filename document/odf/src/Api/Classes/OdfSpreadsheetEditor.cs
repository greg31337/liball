namespace odf;

/// <summary>
/// Provides sheet lifecycle operations and access to spreadsheet-only data.
/// </summary>
public class OdfSpreadsheetEditor : IOdfSpreadsheetEditor
{
    /// <summary>Initializes a spreadsheet editor.</summary>
    public OdfSpreadsheetEditor()
    {
    }

    /// <summary>Gets the direct child sheets in display order.</summary>
    public OdfTable[] GetSheets(OdfDocument document)
    {
        return SpreadsheetEditorModel.GetSheets(document);
    }

    /// <summary>Finds an unambiguously named sheet.</summary>
    public OdfTable? FindSheet(OdfDocument document, string name)
    {
        return SpreadsheetEditorModel.FindSheet(document, name);
    }

    /// <summary>Creates a blank sheet.</summary>
    public OdfTable CreateSheet(
        OdfDocument document,
        string name,
        long rowCount,
        long columnCount)
    {
        return SpreadsheetEditorModel.CreateSheet(
            document,
            name,
            rowCount,
            columnCount);
    }

    /// <summary>Deletes a sheet.</summary>
    public void DeleteSheet(OdfDocument document, OdfTable sheet)
    {
        SpreadsheetEditorModel.DeleteSheet(document, sheet);
    }

    /// <summary>Renames a sheet.</summary>
    public void RenameSheet(
        OdfDocument document,
        OdfTable sheet,
        string newName)
    {
        SpreadsheetEditorModel.RenameSheet(document, sheet, newName);
    }

    /// <summary>Moves a sheet to a final display index.</summary>
    public OdfTable MoveSheet(
        OdfDocument document,
        OdfTable sheet,
        long destinationIndex)
    {
        return SpreadsheetEditorModel.MoveSheet(
            document,
            sheet,
            destinationIndex);
    }

    /// <summary>Gets the spreadsheet calculation and view settings.</summary>
    public OdfSpreadsheetSettings GetSpreadsheetSettings(OdfDocument document)
    {
        RequireDocument(document);
        return document.SpreadsheetSettings;
    }

    /// <summary>Enables or disables spreadsheet change tracking.</summary>
    public void SetSpreadsheetChangeTracking(
        OdfDocument document,
        bool enabled)
    {
        RequireDocument(document);
        document.TrackSpreadsheetChanges = enabled;
    }

    /// <summary>Gets named ranges and expressions.</summary>
    public OdfNamedExpression[] GetNamedExpressions(OdfDocument document)
    {
        RequireDocument(document);
        OdfNamedExpression[] result =
            new OdfNamedExpression[document.NamedExpressions.Length];
        global::System.Array.Copy(
            document.NamedExpressions,
            result,
            result.Length);
        return result;
    }

    /// <summary>Replaces named ranges and expressions.</summary>
    public void SetNamedExpressions(
        OdfDocument document,
        OdfNamedExpression[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A named-expression collection is required.");
        }
        OdfNamedExpression[] copy = new OdfNamedExpression[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.NamedExpressions = copy;
    }

    /// <summary>Gets label ranges.</summary>
    public OdfLabelRange[] GetLabelRanges(OdfDocument document)
    {
        RequireDocument(document);
        OdfLabelRange[] result = new OdfLabelRange[document.LabelRanges.Length];
        global::System.Array.Copy(document.LabelRanges, result, result.Length);
        return result;
    }

    /// <summary>Replaces label ranges.</summary>
    public void SetLabelRanges(OdfDocument document, OdfLabelRange[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException("A label-range collection is required.");
        }
        OdfLabelRange[] copy = new OdfLabelRange[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.LabelRanges = copy;
    }

    /// <summary>Gets database ranges.</summary>
    public OdfDatabaseRange[] GetDatabaseRanges(OdfDocument document)
    {
        RequireDocument(document);
        OdfDatabaseRange[] result =
            new OdfDatabaseRange[document.DatabaseRanges.Length];
        global::System.Array.Copy(document.DatabaseRanges, result, result.Length);
        return result;
    }

    /// <summary>Replaces database ranges.</summary>
    public void SetDatabaseRanges(
        OdfDocument document,
        OdfDatabaseRange[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A database-range collection is required.");
        }
        OdfDatabaseRange[] copy = new OdfDatabaseRange[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.DatabaseRanges = copy;
    }

    /// <summary>Gets consolidation declarations.</summary>
    public OdfConsolidation[] GetConsolidations(OdfDocument document)
    {
        RequireDocument(document);
        OdfConsolidation[] result =
            new OdfConsolidation[document.Consolidations.Length];
        global::System.Array.Copy(document.Consolidations, result, result.Length);
        return result;
    }

    /// <summary>Replaces consolidation declarations.</summary>
    public void SetConsolidations(
        OdfDocument document,
        OdfConsolidation[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A consolidation collection is required.");
        }
        OdfConsolidation[] copy = new OdfConsolidation[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.Consolidations = copy;
    }

    /// <summary>Gets spreadsheet scenarios.</summary>
    public OdfScenario[] GetScenarios(OdfDocument document)
    {
        RequireDocument(document);
        OdfScenario[] result = new OdfScenario[document.Scenarios.Length];
        global::System.Array.Copy(document.Scenarios, result, result.Length);
        return result;
    }

    /// <summary>Replaces spreadsheet scenarios.</summary>
    public void SetScenarios(OdfDocument document, OdfScenario[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException("A scenario collection is required.");
        }
        OdfScenario[] copy = new OdfScenario[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.Scenarios = copy;
    }

    /// <summary>Gets cell content validations.</summary>
    public OdfContentValidation[] GetContentValidations(OdfDocument document)
    {
        RequireDocument(document);
        OdfContentValidation[] result =
            new OdfContentValidation[document.ContentValidations.Length];
        global::System.Array.Copy(
            document.ContentValidations,
            result,
            result.Length);
        return result;
    }

    /// <summary>Replaces cell content validations.</summary>
    public void SetContentValidations(
        OdfDocument document,
        OdfContentValidation[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A content-validation collection is required.");
        }
        OdfContentValidation[] copy = new OdfContentValidation[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.ContentValidations = copy;
    }

    /// <summary>Gets conditional style mappings.</summary>
    public OdfConditionalStyle[] GetConditionalStyles(OdfDocument document)
    {
        RequireDocument(document);
        OdfConditionalStyle[] result =
            new OdfConditionalStyle[document.ConditionalStyles.Length];
        global::System.Array.Copy(
            document.ConditionalStyles,
            result,
            result.Length);
        return result;
    }

    /// <summary>Replaces conditional style mappings.</summary>
    public void SetConditionalStyles(
        OdfDocument document,
        OdfConditionalStyle[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A conditional-style collection is required.");
        }
        OdfConditionalStyle[] copy = new OdfConditionalStyle[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.ConditionalStyles = copy;
    }

    /// <summary>Gets data-pilot or pivot tables.</summary>
    public OdfPivotTable[] GetPivotTables(OdfDocument document)
    {
        RequireDocument(document);
        OdfPivotTable[] result = new OdfPivotTable[document.PivotTables.Length];
        global::System.Array.Copy(document.PivotTables, result, result.Length);
        return result;
    }

    /// <summary>Replaces data-pilot or pivot tables.</summary>
    public void SetPivotTables(OdfDocument document, OdfPivotTable[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException("A pivot-table collection is required.");
        }
        OdfPivotTable[] copy = new OdfPivotTable[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.PivotTables = copy;
    }

    /// <summary>Gets detective trace ranges.</summary>
    public OdfDetectiveRange[] GetDetectiveRanges(OdfDocument document)
    {
        RequireDocument(document);
        OdfDetectiveRange[] result =
            new OdfDetectiveRange[document.DetectiveRanges.Length];
        global::System.Array.Copy(document.DetectiveRanges, result, result.Length);
        return result;
    }

    /// <summary>Replaces detective trace ranges.</summary>
    public void SetDetectiveRanges(
        OdfDocument document,
        OdfDetectiveRange[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A detective-range collection is required.");
        }
        OdfDetectiveRange[] copy = new OdfDetectiveRange[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.DetectiveRanges = copy;
    }

    /// <summary>Gets detective operations.</summary>
    public OdfDetectiveOperation[] GetDetectiveOperations(
        OdfDocument document)
    {
        RequireDocument(document);
        OdfDetectiveOperation[] result =
            new OdfDetectiveOperation[document.DetectiveOperations.Length];
        global::System.Array.Copy(
            document.DetectiveOperations,
            result,
            result.Length);
        return result;
    }

    /// <summary>Replaces detective operations.</summary>
    public void SetDetectiveOperations(
        OdfDocument document,
        OdfDetectiveOperation[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A detective-operation collection is required.");
        }
        OdfDetectiveOperation[] copy =
            new OdfDetectiveOperation[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.DetectiveOperations = copy;
    }

    /// <summary>Gets tracked spreadsheet changes.</summary>
    public OdfSpreadsheetChange[] GetSpreadsheetChanges(OdfDocument document)
    {
        RequireDocument(document);
        OdfSpreadsheetChange[] result =
            new OdfSpreadsheetChange[document.SpreadsheetChanges.Length];
        global::System.Array.Copy(
            document.SpreadsheetChanges,
            result,
            result.Length);
        return result;
    }

    /// <summary>Replaces tracked spreadsheet changes.</summary>
    public void SetSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] items)
    {
        RequireDocument(document);
        if (items == null)
        {
            throw new OdfDocumentException(
                "A spreadsheet-change collection is required.");
        }
        OdfSpreadsheetChange[] copy =
            new OdfSpreadsheetChange[items.Length];
        global::System.Array.Copy(items, copy, items.Length);
        document.SpreadsheetChanges = copy;
    }

    internal void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet editing.");
        }
    }
}
