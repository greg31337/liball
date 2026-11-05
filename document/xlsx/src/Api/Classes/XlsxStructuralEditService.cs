using System;
using System.Collections.Generic;
using System.Globalization;

namespace xlsx;

/// <summary>
/// Coordinates structural edits across worksheet content and workbook-level dependent features.
/// </summary>
public class XlsxStructuralEditService : IXlsxStructuralEditService
{
    /// <summary>
    /// Initializes a structural edit service.
    /// </summary>
    public XlsxStructuralEditService()
    {
    }

    /// <summary>
    /// Inserts rows and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="rowIndex">The one-based insertion row.</param>
    /// <param name="count">The positive row count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult InsertRows(XlsxWorkbook workbook, string sheetName, int rowIndex, int count)
    {
        return EditRows(workbook, sheetName, rowIndex, count, true);
    }

    /// <summary>
    /// Deletes rows and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="rowIndex">The one-based first row.</param>
    /// <param name="count">The positive row count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult DeleteRows(XlsxWorkbook workbook, string sheetName, int rowIndex, int count)
    {
        return EditRows(workbook, sheetName, rowIndex, count, false);
    }

    /// <summary>
    /// Inserts columns and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="columnIndex">The one-based insertion column.</param>
    /// <param name="count">The positive column count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult InsertColumns(XlsxWorkbook workbook, string sheetName, int columnIndex, int count)
    {
        return EditColumns(workbook, sheetName, columnIndex, count, true);
    }

    /// <summary>
    /// Deletes columns and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="columnIndex">The one-based first column.</param>
    /// <param name="count">The positive column count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult DeleteColumns(XlsxWorkbook workbook, string sheetName, int columnIndex, int count)
    {
        return EditColumns(workbook, sheetName, columnIndex, count, false);
    }

    /// <summary>
    /// Shifts a rectangular cell selection and reports its structural effects.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="reference">The selected A1 range.</param>
    /// <param name="direction">The shift direction.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult ShiftCells(XlsxWorkbook workbook, string sheetName, string reference, XlsxCellShiftDirection direction)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.CellShift, sheetName);
        int sheetIndex = FindSheetIndex(workbook, sheetName);
        XlsxWorksheetPart? worksheet = GetWorksheet(workbook, sheetIndex, result);
        if (worksheet == null)
        {
            return result;
        }
        XlsxWorksheetRange range = worksheet.GetRange(reference);
        XlsxWorksheetRangeResult rangeResult = new XlsxWorksheetRangeResult();
        XlsxRangeService service = new XlsxRangeService();
        if (direction == XlsxCellShiftDirection.Down)
        {
            rangeResult = service.ShiftCellsDown(range);
        }
        else if (direction == XlsxCellShiftDirection.Up)
        {
            rangeResult = service.ShiftCellsUp(range);
        }
        else if (direction == XlsxCellShiftDirection.Right)
        {
            rangeResult = service.ShiftCellsRight(range);
        }
        else if (direction == XlsxCellShiftDirection.Left)
        {
            rangeResult = service.ShiftCellsLeft(range);
        }
        else
        {
            result.AddError("structuralEdit.shiftDirectionInvalid", "Cell shift direction is not supported.");
            return result;
        }
        CopyRangeDiagnostics(result, rangeResult);
        result.AffectedCount = rangeResult.AffectedCellCount;
        if (result.IsValid)
        {
            string directionText = direction.ToString();
            AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, sheetName + "/cells", reference, directionText, false);
        }
        return result;
    }

    /// <summary>
    /// Moves a range within or between worksheets and reports its structural effects.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sourceSheetName">The source worksheet name.</param>
    /// <param name="sourceReference">The source A1 range.</param>
    /// <param name="targetSheetName">The destination worksheet name.</param>
    /// <param name="targetTopLeftReference">The destination top-left cell.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult MoveRange(XlsxWorkbook workbook, string sourceSheetName, string sourceReference, string targetSheetName, string targetTopLeftReference)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.RangeMove, sourceSheetName);
        int sourceIndex = FindSheetIndex(workbook, sourceSheetName);
        int targetIndex = FindSheetIndex(workbook, targetSheetName);
        XlsxWorksheetPart? source = GetWorksheet(workbook, sourceIndex, result);
        XlsxWorksheetPart? target = GetWorksheet(workbook, targetIndex, result);
        if (source == null || target == null)
        {
            return result;
        }
        XlsxWorksheetRange sourceRange = source.GetRange(sourceReference);
        XlsxWorksheetRangeResult moved;
        if (source == target)
        {
            moved = sourceRange.MoveTo(targetTopLeftReference);
        }
        else
        {
            moved = new XlsxRangeService().MoveAcrossSheets(sourceRange, target, targetTopLeftReference);
        }
        CopyRangeDiagnostics(result, moved);
        result.AffectedCount = moved.AffectedCellCount;
        if (result.IsValid)
        {
            AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, sourceSheetName + "/range", sourceReference, XlsxFormulaService.QualifyReference(targetSheetName, targetTopLeftReference), false);
        }
        return result;
    }

    /// <summary>
    /// Adds a sheet and synchronizes worksheet and relationship collections.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The new sheet name.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult AddSheet(XlsxWorkbook workbook, string sheetName)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.SheetAdd, sheetName);
        if (!ValidateWorkbook(result, workbook))
        {
            return result;
        }
        XlsxWorkbookSheetOperationResult added = workbook.AddSheet(sheetName);
        CopyValidation(result, added.Validation);
        if (!result.IsValid || added.Sheet == null)
        {
            return result;
        }
        while (workbook.Worksheets.Count < workbook.WorkbookPart.Sheets.Count)
        {
            XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
            worksheet.PartPath = "/xl/worksheets/sheet" + (workbook.Worksheets.Count + 1).ToString(CultureInfo.InvariantCulture) + ".xml";
            workbook.Worksheets.Add(worksheet);
        }
        result.AffectedCount = 1;
        AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, sheetName, "", added.Sheet.SheetId.ToString(CultureInfo.InvariantCulture), false);
        AddUpdate(result, XlsxStructuralReferenceCategory.Relationship, XlsxCellValueConverter.NormalizeText(added.Sheet.RelationshipId), "", "worksheet", false);
        return result;
    }

    /// <summary>
    /// Removes a sheet and synchronizes dependent scopes and relationships.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The sheet name to remove.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult RemoveSheet(XlsxWorkbook workbook, string sheetName)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.SheetRemove, sheetName);
        int index = FindSheetIndex(workbook, sheetName);
        if (index < 0 || !ValidateWorkbook(result, workbook))
        {
            if (index < 0)
            {
                result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            }
            return result;
        }
        string relationshipId = XlsxCellValueConverter.NormalizeText(workbook.WorkbookPart.Sheets[index].RelationshipId);
        Dictionary<XlsxWorkbookDefinedName, int> previousScopes = CaptureDefinedNameScopes(workbook);
        XlsxValidationResult removed = workbook.RemoveSheet(sheetName);
        CopyValidation(result, removed);
        if (!result.IsValid)
        {
            return result;
        }
        InvalidateRemovedSheetReferences(workbook, sheetName, index, result);
        AddDefinedNameScopeUpdates(workbook, previousScopes, result);
        result.AffectedCount = 1;
        AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, sheetName, sheetName, "", true);
        AddUpdate(result, XlsxStructuralReferenceCategory.Relationship, relationshipId, "worksheet", "", true);
        return result;
    }

    /// <summary>
    /// Renames a sheet and updates formulas, names, and charts.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="oldSheetName">The existing sheet name.</param>
    /// <param name="newSheetName">The replacement sheet name.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult RenameSheet(XlsxWorkbook workbook, string oldSheetName, string newSheetName)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.SheetRename, newSheetName);
        result.PreviousSheetName = oldSheetName;
        if (!ValidateWorkbook(result, workbook))
        {
            return result;
        }
        XlsxValidationResult renamed = workbook.RenameSheet(oldSheetName, newSheetName);
        CopyValidation(result, renamed);
        if (!result.IsValid)
        {
            return result;
        }
        RenameWorksheetFormulas(workbook, oldSheetName, newSheetName, result);
        RenameDefinedNames(workbook, oldSheetName, newSheetName, result);
        RenameChartFormulas(workbook, oldSheetName, newSheetName, result);
        result.AffectedCount = 1;
        AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, oldSheetName, oldSheetName, newSheetName, false);
        return result;
    }

    /// <summary>
    /// Reorders a sheet and updates worksheet order and local defined-name scopes.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The sheet name to move.</param>
    /// <param name="newIndex">The zero-based destination index.</param>
    /// <returns>The deterministic structural edit result.</returns>
    public XlsxStructuralEditResult ReorderSheet(XlsxWorkbook workbook, string sheetName, int newIndex)
    {
        XlsxStructuralEditResult result = CreateResult(XlsxStructuralEditKind.SheetReorder, sheetName);
        int oldIndex = FindSheetIndex(workbook, sheetName);
        if (oldIndex < 0 || !ValidateWorkbook(result, workbook))
        {
            if (oldIndex < 0)
            {
                result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            }
            return result;
        }
        Dictionary<XlsxWorkbookDefinedName, int> previousScopes = CaptureDefinedNameScopes(workbook);
        XlsxValidationResult reordered = workbook.ReorderSheet(sheetName, newIndex);
        CopyValidation(result, reordered);
        if (!result.IsValid)
        {
            return result;
        }
        if (oldIndex < workbook.Worksheets.Count && newIndex < workbook.Worksheets.Count)
        {
            XlsxWorksheetPart worksheet = workbook.Worksheets[oldIndex];
            workbook.Worksheets.RemoveAt(oldIndex);
            workbook.Worksheets.Insert(newIndex, worksheet);
        }
        AddDefinedNameScopeUpdates(workbook, previousScopes, result);
        result.AffectedCount = 1;
        AddUpdate(result, XlsxStructuralReferenceCategory.Sheet, sheetName, oldIndex.ToString(CultureInfo.InvariantCulture), newIndex.ToString(CultureInfo.InvariantCulture), false);
        return result;
    }

    internal static XlsxStructuralEditResult EditRows(XlsxWorkbook workbook, string sheetName, int rowIndex, int count, bool insert)
    {
        XlsxStructuralEditKind editKind = insert ? XlsxStructuralEditKind.RowInsert : XlsxStructuralEditKind.RowDelete;
        XlsxFormulaReferenceUpdateKind referenceKind = insert ? XlsxFormulaReferenceUpdateKind.RowInsert : XlsxFormulaReferenceUpdateKind.RowDelete;
        XlsxStructuralEditResult result = CreateResult(editKind, sheetName);
        int sheetIndex = FindSheetIndex(workbook, sheetName);
        XlsxWorksheetPart? worksheet = GetWorksheet(workbook, sheetIndex, result);
        if (worksheet == null)
        {
            return result;
        }
        Dictionary<string, string> before = CaptureWorksheetReferences(worksheet);
        XlsxWorksheetRowEditResult edit = insert ? worksheet.InsertRows(rowIndex, count) : worksheet.DeleteRows(rowIndex, count);
        CopyRowDiagnostics(result, edit);
        if (!result.IsValid)
        {
            return result;
        }
        result.AffectedCount = edit.AffectedRowCount;
        Dictionary<string, string> after = CaptureWorksheetReferences(worksheet);
        AddSnapshotDifferences(result, before, after);
        if (edit.FormulaReferenceUpdateCount > result.FormulaUpdateCount)
        {
            result.FormulaUpdateCount = edit.FormulaReferenceUpdateCount;
        }
        UpdateDefinedNames(workbook, sheetIndex, sheetName, referenceKind, rowIndex, 0, count, result);
        UpdatePivotLocations(workbook, worksheet, referenceKind, rowIndex, 0, count, result);
        UpdateDrawingAnchors(workbook, worksheet, referenceKind, rowIndex, count, result);
        UpdateChartReferences(workbook, sheetName, referenceKind, rowIndex, 0, count, result);
        return result;
    }

    internal static XlsxStructuralEditResult EditColumns(XlsxWorkbook workbook, string sheetName, int columnIndex, int count, bool insert)
    {
        XlsxStructuralEditKind editKind = insert ? XlsxStructuralEditKind.ColumnInsert : XlsxStructuralEditKind.ColumnDelete;
        XlsxFormulaReferenceUpdateKind referenceKind = insert ? XlsxFormulaReferenceUpdateKind.ColumnInsert : XlsxFormulaReferenceUpdateKind.ColumnDelete;
        XlsxStructuralEditResult result = CreateResult(editKind, sheetName);
        int sheetIndex = FindSheetIndex(workbook, sheetName);
        XlsxWorksheetPart? worksheet = GetWorksheet(workbook, sheetIndex, result);
        if (worksheet == null)
        {
            return result;
        }
        Dictionary<string, string> before = CaptureWorksheetReferences(worksheet);
        XlsxWorksheetColumnEditResult edit = insert ? worksheet.InsertColumns(columnIndex, count) : worksheet.DeleteColumns(columnIndex, count);
        CopyColumnDiagnostics(result, edit);
        if (!result.IsValid)
        {
            return result;
        }
        result.AffectedCount = edit.AffectedCellCount + edit.AffectedColumnCount;
        Dictionary<string, string> after = CaptureWorksheetReferences(worksheet);
        AddSnapshotDifferences(result, before, after);
        if (edit.FormulaReferenceUpdateCount > result.FormulaUpdateCount)
        {
            result.FormulaUpdateCount = edit.FormulaReferenceUpdateCount;
        }
        UpdateDefinedNames(workbook, sheetIndex, sheetName, referenceKind, 0, columnIndex, count, result);
        UpdatePivotLocations(workbook, worksheet, referenceKind, 0, columnIndex, count, result);
        UpdateDrawingAnchors(workbook, worksheet, referenceKind, columnIndex, count, result);
        UpdateChartReferences(workbook, sheetName, referenceKind, 0, columnIndex, count, result);
        return result;
    }

    internal static XlsxStructuralEditResult CreateResult(XlsxStructuralEditKind kind, string sheetName)
    {
        XlsxStructuralEditResult result = new XlsxStructuralEditResult();
        result.Kind = kind;
        result.SheetName = XlsxCellValueConverter.NormalizeText(sheetName);
        return result;
    }

    internal static bool ValidateWorkbook(XlsxStructuralEditResult result, XlsxWorkbook workbook)
    {
        if (workbook == null || workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null || workbook.Worksheets == null)
        {
            result.AddError("structuralEdit.workbookMissing", "A workbook with sheet and worksheet collections is required.");
            return false;
        }
        return true;
    }

    internal static int FindSheetIndex(XlsxWorkbook workbook, string sheetName)
    {
        if (workbook == null || workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null)
        {
            return -1;
        }
        int index = 0;
        foreach (XlsxWorkbookSheet sheet in workbook.WorkbookPart.Sheets)
        {
            if (sheet != null && string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static XlsxWorksheetPart? GetWorksheet(XlsxWorkbook workbook, int sheetIndex, XlsxStructuralEditResult result)
    {
        if (!ValidateWorkbook(result, workbook))
        {
            return null;
        }
        if (sheetIndex < 0)
        {
            result.AddError("workbook.sheet.notFound", "Workbook sheet was not found.");
            return null;
        }
        if (sheetIndex >= workbook.Worksheets.Count)
        {
            result.AddError("structuralEdit.worksheetMissing", "Workbook worksheet aggregate is missing the requested sheet.");
            return null;
        }
        return workbook.Worksheets[sheetIndex];
    }

    internal static Dictionary<string, string> CaptureWorksheetReferences(XlsxWorksheetPart worksheet)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        int index = 0;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && cell.HasFormula)
                    {
                        values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Formula, index), XlsxWorksheetPart.GetFormulaText(cell));
                        index = index + 1;
                    }
                }
            }
        }
        CaptureMerges(values, worksheet.MergedCells);
        CaptureTables(values, worksheet.Tables);
        CaptureValidations(values, worksheet.DataValidations);
        CaptureConditionalFormatting(values, worksheet.ConditionalFormattings);
        CaptureHyperlinks(values, worksheet.Hyperlinks);
        CaptureComments(values, worksheet.CommentsPart);
        CapturePageBreaks(values, worksheet.RowBreaks, worksheet.ColumnBreaks);
        if (worksheet.HasAutoFilter && worksheet.AutoFilter != null)
        {
            values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Filter, 0), worksheet.AutoFilter.Reference);
        }
        return values;
    }

    internal static void CaptureMerges(Dictionary<string, string> values, List<XlsxWorksheetMergeCell> items)
    {
        int index = 0;
        foreach (XlsxWorksheetMergeCell item in items)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Merge, index), XlsxCellValueConverter.NormalizeText(item.Reference));
            }
            index = index + 1;
        }
    }

    internal static void CaptureTables(Dictionary<string, string> values, List<XlsxTablePart> items)
    {
        int index = 0;
        foreach (XlsxTablePart item in items)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Table, index), item.Reference);
            }
            index = index + 1;
        }
    }

    internal static void CaptureValidations(Dictionary<string, string> values, XlsxDataValidations validations)
    {
        if (validations == null || validations.Items == null)
        {
            return;
        }
        int index = 0;
        foreach (XlsxDataValidation item in validations.Items)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Validation, index), item.Sqref + "|" + item.Formula1 + "|" + item.Formula2);
            }
            index = index + 1;
        }
    }

    internal static void CaptureConditionalFormatting(Dictionary<string, string> values, List<XlsxConditionalFormatting> items)
    {
        int index = 0;
        foreach (XlsxConditionalFormatting item in items)
        {
            if (item != null)
            {
                string text = item.Sqref;
                foreach (XlsxConditionalFormatRule rule in item.Rules)
                {
                    foreach (string formula in rule.Formulas)
                    {
                        text = text + "|" + formula;
                    }
                }
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.ConditionalFormatting, index), text);
            }
            index = index + 1;
        }
    }

    internal static void CaptureHyperlinks(Dictionary<string, string> values, List<XlsxWorksheetHyperlink> items)
    {
        int index = 0;
        foreach (XlsxWorksheetHyperlink item in items)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Hyperlink, index), XlsxCellValueConverter.NormalizeText(item.Reference));
            }
            index = index + 1;
        }
    }

    internal static void CaptureComments(Dictionary<string, string> values, XlsxCommentsPart comments)
    {
        if (comments == null || comments.Comments == null)
        {
            return;
        }
        int index = 0;
        foreach (XlsxComment item in comments.Comments)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.Comment, index), XlsxCellValueConverter.NormalizeText(item.Reference));
            }
            index = index + 1;
        }
    }

    internal static void CapturePageBreaks(Dictionary<string, string> values, List<XlsxWorksheetPageBreak> rows, List<XlsxWorksheetPageBreak> columns)
    {
        int index = 0;
        foreach (XlsxWorksheetPageBreak item in rows)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.PageBreak, index), "R" + item.Id.ToString(CultureInfo.InvariantCulture));
            }
            index = index + 1;
        }
        foreach (XlsxWorksheetPageBreak item in columns)
        {
            if (item != null)
            {
                values.Add(MakeSnapshotKey(XlsxStructuralReferenceCategory.PageBreak, index), "C" + item.Id.ToString(CultureInfo.InvariantCulture));
            }
            index = index + 1;
        }
    }

    internal static string MakeSnapshotKey(XlsxStructuralReferenceCategory category, int index)
    {
        return ((int)category).ToString(CultureInfo.InvariantCulture) + "/" + index.ToString(CultureInfo.InvariantCulture);
    }

    internal static void AddSnapshotDifferences(XlsxStructuralEditResult result, Dictionary<string, string> before, Dictionary<string, string> after)
    {
        List<string> beforeKeys = new List<string>();
        foreach (KeyValuePair<string, string> item in before)
        {
            beforeKeys.Add(item.Key);
        }
        beforeKeys.Sort(StringComparer.Ordinal);
        foreach (string key in beforeKeys)
        {
            string previous = before[key];
            string updated = "";
            bool removed = !after.ContainsKey(key);
            if (!removed)
            {
                updated = after[key];
            }
            if (removed || previous != updated)
            {
                XlsxStructuralReferenceCategory category = GetSnapshotCategory(key);
                AddUpdate(result, category, key, previous, updated, removed);
            }
        }
        List<string> afterKeys = new List<string>();
        foreach (KeyValuePair<string, string> item in after)
        {
            afterKeys.Add(item.Key);
        }
        afterKeys.Sort(StringComparer.Ordinal);
        foreach (string key in afterKeys)
        {
            if (!before.ContainsKey(key))
            {
                XlsxStructuralReferenceCategory category = GetSnapshotCategory(key);
                AddUpdate(result, category, key, "", after[key], false);
            }
        }
    }

    internal static XlsxStructuralReferenceCategory GetSnapshotCategory(string key)
    {
        int slash = key.IndexOf('/');
        string number = slash >= 0 ? key.Substring(0, slash) : key;
        return (XlsxStructuralReferenceCategory)int.Parse(number, CultureInfo.InvariantCulture);
    }

    internal static void UpdateDefinedNames(XlsxWorkbook workbook, int sheetIndex, string sheetName, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count, XlsxStructuralEditResult result)
    {
        foreach (XlsxWorkbookDefinedName name in workbook.WorkbookPart.DefinedNames)
        {
            if (name != null && NameTargetsSheet(name, sheetIndex, sheetName))
            {
                string previous = name.Text;
                XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.UpdateFormulaReferences(previous, kind, rowIndex, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
                CopyFormulaDiagnostics(result, update);
                if (update.IsValid && update.Formula != previous)
                {
                    name.Text = update.Formula;
                    XlsxStructuralReferenceCategory category = IsPrintDefinition(name) ? XlsxStructuralReferenceCategory.PrintDefinition : XlsxStructuralReferenceCategory.Name;
                    AddUpdate(result, category, XlsxCellValueConverter.NormalizeText(name.Name), previous, name.Text, name.Text.IndexOf("#REF!", StringComparison.Ordinal) >= 0);
                }
            }
        }
    }

    internal static bool NameTargetsSheet(XlsxWorkbookDefinedName name, int sheetIndex, string sheetName)
    {
        if (name.HasLocalSheetId)
        {
            return name.LocalSheetId == sheetIndex;
        }
        return FormulaHasSheetQualifier(name.Text, sheetName);
    }

    internal static bool IsPrintDefinition(XlsxWorkbookDefinedName name)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(name.Name);
        return string.Equals(normalized, "_xlnm.Print_Area", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "_xlnm.Print_Titles", StringComparison.OrdinalIgnoreCase);
    }

    internal static void UpdatePivotLocations(XlsxWorkbook workbook, XlsxWorksheetPart worksheet, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count, XlsxStructuralEditResult result)
    {
        foreach (XlsxWorksheetPivotTableReference reference in worksheet.PivotTableReferences)
        {
            if (reference != null)
            {
                foreach (XlsxPivotTableDefinitionPart pivot in workbook.PivotTableDefinitions)
                {
                    if (pivot != null && pivot.HasLocation && pivot.Location != null && XlsxRangeService.PivotPathMatches(reference, pivot))
                    {
                        string previous = pivot.Location.Reference;
                        XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.UpdateFormulaReferences(previous, kind, rowIndex, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
                        CopyFormulaDiagnostics(result, update);
                        if (update.IsValid && update.Formula != previous)
                        {
                            pivot.Location.Reference = update.Formula;
                            AddUpdate(result, XlsxStructuralReferenceCategory.Pivot, pivot.PartPath, previous, pivot.Location.Reference, pivot.Location.Reference.IndexOf("#REF!", StringComparison.Ordinal) >= 0);
                        }
                    }
                }
            }
        }
    }

    internal static void UpdateDrawingAnchors(XlsxWorkbook workbook, XlsxWorksheetPart worksheet, XlsxFormulaReferenceUpdateKind kind, int start, int count, XlsxStructuralEditResult result)
    {
        if (!worksheet.HasDrawing || worksheet.Drawing == null)
        {
            return;
        }
        foreach (XlsxDrawingPart drawing in workbook.Drawings)
        {
            if (drawing != null && DrawingMatchesWorksheet(drawing, worksheet.Drawing))
            {
                int index = 0;
                foreach (XlsxDrawingImageAnchor anchor in drawing.ImageAnchors)
                {
                    if (anchor != null)
                    {
                        string previous = FormatAnchor(anchor);
                        bool changed = UpdateMarker(anchor.From, kind, start, count);
                        if (UpdateMarker(anchor.To, kind, start, count))
                        {
                            changed = true;
                        }
                        if (changed)
                        {
                            AddUpdate(result, XlsxStructuralReferenceCategory.Drawing, drawing.PartPath + "/image/" + index.ToString(CultureInfo.InvariantCulture), previous, FormatAnchor(anchor), false);
                        }
                    }
                    index = index + 1;
                }
            }
        }
    }

    internal static bool DrawingMatchesWorksheet(XlsxDrawingPart drawing, XlsxSheetDrawingReference reference)
    {
        string targetPath = XlsxCellValueConverter.NormalizeText(reference.TargetPath);
        string target = XlsxCellValueConverter.NormalizeText(reference.Target);
        return string.Equals(drawing.PartPath, targetPath, StringComparison.OrdinalIgnoreCase) || string.Equals(drawing.PartPath, target, StringComparison.OrdinalIgnoreCase) || targetPath.EndsWith("/" + XlsxRangeService.GetFileName(drawing.PartPath), StringComparison.OrdinalIgnoreCase) || target.EndsWith("/" + XlsxRangeService.GetFileName(drawing.PartPath), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool UpdateMarker(XlsxDrawingMarker marker, XlsxFormulaReferenceUpdateKind kind, int start, int count)
    {
        if (marker == null)
        {
            return false;
        }
        bool rowEdit = kind == XlsxFormulaReferenceUpdateKind.RowInsert || kind == XlsxFormulaReferenceUpdateKind.RowDelete;
        uint original = rowEdit ? marker.Row : marker.Column;
        int cellIndex = (int)original + 1;
        int updated = UpdateAxisIndex(cellIndex, kind, start, count);
        if (updated == cellIndex)
        {
            return false;
        }
        if (rowEdit)
        {
            marker.Row = (uint)(updated - 1);
        }
        else
        {
            marker.Column = (uint)(updated - 1);
        }
        return true;
    }

    internal static int UpdateAxisIndex(int value, XlsxFormulaReferenceUpdateKind kind, int start, int count)
    {
        bool insert = kind == XlsxFormulaReferenceUpdateKind.RowInsert || kind == XlsxFormulaReferenceUpdateKind.ColumnInsert;
        if (insert)
        {
            return value >= start ? value + count : value;
        }
        int last = start + count - 1;
        if (value > last)
        {
            return value - count;
        }
        if (value >= start)
        {
            return start;
        }
        return value;
    }

    internal static string FormatAnchor(XlsxDrawingImageAnchor anchor)
    {
        return anchor.From.Row.ToString(CultureInfo.InvariantCulture) + "," + anchor.From.Column.ToString(CultureInfo.InvariantCulture) + ":" + anchor.To.Row.ToString(CultureInfo.InvariantCulture) + "," + anchor.To.Column.ToString(CultureInfo.InvariantCulture);
    }

    internal static void UpdateChartReferences(XlsxWorkbook workbook, string sheetName, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count, XlsxStructuralEditResult result)
    {
        int chartIndex = 0;
        foreach (XlsxChartPart chart in workbook.Charts)
        {
            if (chart != null)
            {
                int seriesIndex = 0;
                foreach (XlsxChartSeriesReference series in chart.Series)
                {
                    if (series != null)
                    {
                        UpdateChartSeriesFormula(series, chart, chartIndex, seriesIndex, "text", series.TextFormula, sheetName, kind, rowIndex, columnIndex, count, result);
                        UpdateChartSeriesFormula(series, chart, chartIndex, seriesIndex, "category", series.CategoryFormula, sheetName, kind, rowIndex, columnIndex, count, result);
                        UpdateChartSeriesFormula(series, chart, chartIndex, seriesIndex, "values", series.ValuesFormula, sheetName, kind, rowIndex, columnIndex, count, result);
                    }
                    seriesIndex = seriesIndex + 1;
                }
            }
            chartIndex = chartIndex + 1;
        }
    }

    internal static void UpdateChartSeriesFormula(XlsxChartSeriesReference series, XlsxChartPart chart, int chartIndex, int seriesIndex, string field, string formula, string sheetName, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count, XlsxStructuralEditResult result)
    {
        if (formula == null || formula.Length == 0 || !FormulaHasSheetQualifier(formula, sheetName))
        {
            return;
        }
        XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.UpdateFormulaReferences(formula, kind, rowIndex, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
        CopyFormulaDiagnostics(result, update);
        if (!update.IsValid || update.Formula == formula)
        {
            return;
        }
        if (field == "text")
        {
            series.TextFormula = update.Formula;
        }
        else if (field == "category")
        {
            series.CategoryFormula = update.Formula;
        }
        else
        {
            series.ValuesFormula = update.Formula;
        }
        string target = chart.PartPath + "/series/" + seriesIndex.ToString(CultureInfo.InvariantCulture) + "/" + field + "/" + chartIndex.ToString(CultureInfo.InvariantCulture);
        AddUpdate(result, XlsxStructuralReferenceCategory.Chart, target, formula, update.Formula, update.Formula.IndexOf("#REF!", StringComparison.Ordinal) >= 0);
    }

    internal static bool FormulaHasSheetQualifier(string formula, string sheetName)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(formula);
        string plain = sheetName + "!";
        string quoted = "'" + sheetName.Replace("'", "''", StringComparison.Ordinal) + "'!";
        return normalized.IndexOf(plain, StringComparison.OrdinalIgnoreCase) >= 0 || normalized.IndexOf(quoted, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static void RenameWorksheetFormulas(XlsxWorkbook workbook, string oldName, string newName, XlsxStructuralEditResult result)
    {
        int sheetIndex = 0;
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet != null)
            {
                int formulaIndex = 0;
                foreach (XlsxWorksheetRow row in worksheet.Rows)
                {
                    foreach (XlsxWorksheetCell cell in row.Cells)
                    {
                        if (cell != null && cell.HasFormula)
                        {
                            string previous = XlsxWorksheetPart.GetFormulaText(cell);
                            XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(previous, oldName, newName);
                            CopyFormulaDiagnostics(result, update);
                            if (update.IsValid && update.Formula != previous)
                            {
                                cell.Formula = update.Formula;
                                cell.FormulaInfo.Text = update.Formula;
                                AddUpdate(result, XlsxStructuralReferenceCategory.Formula, "sheet/" + sheetIndex.ToString(CultureInfo.InvariantCulture) + "/formula/" + formulaIndex.ToString(CultureInfo.InvariantCulture), previous, update.Formula, false);
                            }
                            formulaIndex = formulaIndex + 1;
                        }
                    }
                }
            }
            sheetIndex = sheetIndex + 1;
        }
    }

    internal static void RenameDefinedNames(XlsxWorkbook workbook, string oldName, string newName, XlsxStructuralEditResult result)
    {
        foreach (XlsxWorkbookDefinedName name in workbook.WorkbookPart.DefinedNames)
        {
            if (name != null && FormulaHasSheetQualifier(name.Text, oldName))
            {
                string previous = name.Text;
                XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(previous, oldName, newName);
                CopyFormulaDiagnostics(result, update);
                if (update.IsValid && update.Formula != previous)
                {
                    name.Text = update.Formula;
                    XlsxStructuralReferenceCategory category = IsPrintDefinition(name) ? XlsxStructuralReferenceCategory.PrintDefinition : XlsxStructuralReferenceCategory.Name;
                    AddUpdate(result, category, XlsxCellValueConverter.NormalizeText(name.Name), previous, name.Text, false);
                }
            }
        }
    }

    internal static void RenameChartFormulas(XlsxWorkbook workbook, string oldName, string newName, XlsxStructuralEditResult result)
    {
        int chartIndex = 0;
        foreach (XlsxChartPart chart in workbook.Charts)
        {
            int seriesIndex = 0;
            foreach (XlsxChartSeriesReference series in chart.Series)
            {
                RenameChartField(series, chart, chartIndex, seriesIndex, "text", series.TextFormula, oldName, newName, result);
                RenameChartField(series, chart, chartIndex, seriesIndex, "category", series.CategoryFormula, oldName, newName, result);
                RenameChartField(series, chart, chartIndex, seriesIndex, "values", series.ValuesFormula, oldName, newName, result);
                seriesIndex = seriesIndex + 1;
            }
            chartIndex = chartIndex + 1;
        }
    }

    internal static void RenameChartField(XlsxChartSeriesReference series, XlsxChartPart chart, int chartIndex, int seriesIndex, string field, string formula, string oldName, string newName, XlsxStructuralEditResult result)
    {
        if (!FormulaHasSheetQualifier(formula, oldName))
        {
            return;
        }
        XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(formula, oldName, newName);
        CopyFormulaDiagnostics(result, update);
        if (!update.IsValid || update.Formula == formula)
        {
            return;
        }
        if (field == "text")
        {
            series.TextFormula = update.Formula;
        }
        else if (field == "category")
        {
            series.CategoryFormula = update.Formula;
        }
        else
        {
            series.ValuesFormula = update.Formula;
        }
        string target = chart.PartPath + "/series/" + seriesIndex.ToString(CultureInfo.InvariantCulture) + "/" + field + "/" + chartIndex.ToString(CultureInfo.InvariantCulture);
        AddUpdate(result, XlsxStructuralReferenceCategory.Chart, target, formula, update.Formula, false);
    }

    internal static Dictionary<XlsxWorkbookDefinedName, int> CaptureDefinedNameScopes(XlsxWorkbook workbook)
    {
        Dictionary<XlsxWorkbookDefinedName, int> scopes = new Dictionary<XlsxWorkbookDefinedName, int>();
        foreach (XlsxWorkbookDefinedName name in workbook.WorkbookPart.DefinedNames)
        {
            if (name != null && name.HasLocalSheetId)
            {
                scopes.Add(name, name.LocalSheetId);
            }
        }
        return scopes;
    }

    internal static void AddDefinedNameScopeUpdates(XlsxWorkbook workbook, Dictionary<XlsxWorkbookDefinedName, int> previousScopes, XlsxStructuralEditResult result)
    {
        foreach (KeyValuePair<XlsxWorkbookDefinedName, int> item in previousScopes)
        {
            XlsxWorkbookDefinedName name = item.Key;
            XlsxStructuralReferenceCategory category = IsPrintDefinition(name) ? XlsxStructuralReferenceCategory.PrintDefinition : XlsxStructuralReferenceCategory.Name;
            if (!workbook.WorkbookPart.DefinedNames.Contains(name))
            {
                AddUpdate(result, category, XlsxCellValueConverter.NormalizeText(name.Name), name.Text, "", true);
            }
            else if (name.LocalSheetId != item.Value)
            {
                AddUpdate(result, category, XlsxCellValueConverter.NormalizeText(name.Name), item.Value.ToString(CultureInfo.InvariantCulture), name.LocalSheetId.ToString(CultureInfo.InvariantCulture), false);
            }
        }
    }

    internal static void InvalidateRemovedSheetReferences(XlsxWorkbook workbook, string sheetName, int removedIndex, XlsxStructuralEditResult result)
    {
        int worksheetIndex = 0;
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            int formulaIndex = 0;
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && cell.HasFormula)
                    {
                        string previous = XlsxWorksheetPart.GetFormulaText(cell);
                        string updated = InvalidateSheetFormula(previous, sheetName, result);
                        if (updated != previous)
                        {
                            cell.Formula = updated;
                            cell.FormulaInfo.Text = updated;
                            AddUpdate(result, XlsxStructuralReferenceCategory.Formula, "sheet/" + worksheetIndex.ToString(CultureInfo.InvariantCulture) + "/formula/" + formulaIndex.ToString(CultureInfo.InvariantCulture), previous, updated, true);
                        }
                        formulaIndex = formulaIndex + 1;
                    }
                }
            }
            worksheetIndex = worksheetIndex + 1;
        }
        foreach (XlsxWorkbookDefinedName name in workbook.WorkbookPart.DefinedNames)
        {
            if (name != null)
            {
                string previous = name.Text;
                string updated = InvalidateSheetFormula(previous, sheetName, result);
                if (updated != previous)
                {
                    name.Text = updated;
                    XlsxStructuralReferenceCategory category = IsPrintDefinition(name) ? XlsxStructuralReferenceCategory.PrintDefinition : XlsxStructuralReferenceCategory.Name;
                    AddUpdate(result, category, XlsxCellValueConverter.NormalizeText(name.Name), previous, updated, true);
                }
            }
        }
        int chartIndex = 0;
        foreach (XlsxChartPart chart in workbook.Charts)
        {
            int seriesIndex = 0;
            foreach (XlsxChartSeriesReference series in chart.Series)
            {
                InvalidateChartField(series, chart, chartIndex, seriesIndex, "text", series.TextFormula, sheetName, result);
                InvalidateChartField(series, chart, chartIndex, seriesIndex, "category", series.CategoryFormula, sheetName, result);
                InvalidateChartField(series, chart, chartIndex, seriesIndex, "values", series.ValuesFormula, sheetName, result);
                seriesIndex = seriesIndex + 1;
            }
            chartIndex = chartIndex + 1;
        }
    }

    internal static string InvalidateSheetFormula(string formula, string sheetName, XlsxStructuralEditResult result)
    {
        if (!FormulaHasSheetQualifier(formula, sheetName))
        {
            return formula;
        }
        XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(formula, sheetName, "__DELETED_SHEET__");
        CopyFormulaDiagnostics(result, update);
        return update.Formula.Replace("__DELETED_SHEET__!", "#REF!", StringComparison.Ordinal);
    }

    internal static void InvalidateChartField(XlsxChartSeriesReference series, XlsxChartPart chart, int chartIndex, int seriesIndex, string field, string formula, string sheetName, XlsxStructuralEditResult result)
    {
        string updated = InvalidateSheetFormula(formula, sheetName, result);
        if (updated == formula)
        {
            return;
        }
        if (field == "text")
        {
            series.TextFormula = updated;
        }
        else if (field == "category")
        {
            series.CategoryFormula = updated;
        }
        else
        {
            series.ValuesFormula = updated;
        }
        string target = chart.PartPath + "/series/" + seriesIndex.ToString(CultureInfo.InvariantCulture) + "/" + field + "/" + chartIndex.ToString(CultureInfo.InvariantCulture);
        AddUpdate(result, XlsxStructuralReferenceCategory.Chart, target, formula, updated, true);
    }

    internal static void AddUpdate(XlsxStructuralEditResult result, XlsxStructuralReferenceCategory category, string target, string previous, string updated, bool removed)
    {
        XlsxStructuralReferenceUpdate item = new XlsxStructuralReferenceUpdate();
        item.Category = category;
        item.Target = XlsxCellValueConverter.NormalizeText(target);
        item.PreviousReference = XlsxCellValueConverter.NormalizeText(previous);
        item.Reference = XlsxCellValueConverter.NormalizeText(updated);
        item.Removed = removed;
        result.Updates.Add(item);
        IncrementCategoryCount(result, category);
        if (removed)
        {
            result.RemovedReferenceCount = result.RemovedReferenceCount + 1;
        }
    }

    internal static void IncrementCategoryCount(XlsxStructuralEditResult result, XlsxStructuralReferenceCategory category)
    {
        if (category == XlsxStructuralReferenceCategory.Formula)
        {
            result.FormulaUpdateCount = result.FormulaUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Name)
        {
            result.NameUpdateCount = result.NameUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Table)
        {
            result.TableUpdateCount = result.TableUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Pivot)
        {
            result.PivotUpdateCount = result.PivotUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Validation)
        {
            result.ValidationUpdateCount = result.ValidationUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.ConditionalFormatting)
        {
            result.ConditionalFormattingUpdateCount = result.ConditionalFormattingUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Hyperlink)
        {
            result.HyperlinkUpdateCount = result.HyperlinkUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Comment)
        {
            result.CommentUpdateCount = result.CommentUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Drawing)
        {
            result.DrawingUpdateCount = result.DrawingUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Chart)
        {
            result.ChartUpdateCount = result.ChartUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.PageBreak)
        {
            result.PageBreakUpdateCount = result.PageBreakUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.PrintDefinition)
        {
            result.PrintDefinitionUpdateCount = result.PrintDefinitionUpdateCount + 1;
        }
        else if (category == XlsxStructuralReferenceCategory.Relationship)
        {
            result.RelationshipUpdateCount = result.RelationshipUpdateCount + 1;
        }
    }

    internal static void CopyRowDiagnostics(XlsxStructuralEditResult result, XlsxWorksheetRowEditResult source)
    {
        CopyValidation(result, source.Validation);
    }

    internal static void CopyColumnDiagnostics(XlsxStructuralEditResult result, XlsxWorksheetColumnEditResult source)
    {
        CopyValidation(result, source.Validation);
    }

    internal static void CopyRangeDiagnostics(XlsxStructuralEditResult result, XlsxWorksheetRangeResult source)
    {
        CopyValidation(result, source.Validation);
    }

    internal static void CopyFormulaDiagnostics(XlsxStructuralEditResult result, XlsxFormulaReferenceUpdateResult source)
    {
        CopyValidation(result, source.Validation);
    }

    internal static void CopyValidation(XlsxStructuralEditResult result, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
