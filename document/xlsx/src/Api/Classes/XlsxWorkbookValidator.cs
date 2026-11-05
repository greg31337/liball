using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook validator data used by the public workbook API.
/// </summary>
public class XlsxWorkbookValidator : IXlsxWorkbookValidator, IXlsxWorkbookValidatorOperations
{
    /// <summary>
    /// Initializes a new workbook validator with its default state.
    /// </summary>
    public XlsxWorkbookValidator()
    {
    }

    /// <summary>
    /// Checks bytes and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxValidationResult ValidateBytes(byte[] bytes)
    {
        return ValidateBytesWithOptions(bytes, new XlsxWorkbookValidationOptions());
    }

    /// <summary>
    /// Checks bytes and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxValidationResult ValidateBytesWithOptions(byte[] bytes, XlsxWorkbookValidationOptions options)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxWorkbookValidationOptions effectiveOptions = NormalizeOptions(options);
        XlsxReaderOptions readerOptions = NormalizeReaderOptions(effectiveOptions);
        if (bytes == null || bytes.Length == 0)
        {
            result.AddError("workbookValidator.bytesEmpty", "Workbook package bytes must not be empty.");
            return result;
        }

        if (effectiveOptions.ValidateSecurityLimits)
        {
            AddDiagnostics(result, readerOptions.Validate());
        }

        if (effectiveOptions.ValidatePackage)
        {
            AddDiagnostics(result, new XlsxMinimumPackageValidator().ValidateBytes(bytes, readerOptions));
        }

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes, readerOptions);
        AddDiagnostics(result, open.Validation);
        if (open.Workbook != null)
        {
            AddDiagnostics(result, ValidateWorkbookWithOptions(open.Workbook, effectiveOptions));
        }

        return result;
    }

    /// <summary>
    /// Checks workbook and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxValidationResult ValidateWorkbook(XlsxWorkbook workbook)
    {
        return ValidateWorkbookWithOptions(workbook, new XlsxWorkbookValidationOptions());
    }

    /// <summary>
    /// Checks workbook and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxValidationResult ValidateWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookValidationOptions options)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxWorkbookValidationOptions effectiveOptions = NormalizeOptions(options);
        if (workbook == null)
        {
            result.AddError("workbookValidator.workbookNull", "Workbook must not be null.");
            return result;
        }

        if (effectiveOptions.ValidateWorkbook)
        {
            ValidateWorkbookPart(result, workbook.WorkbookPart);
        }

        if (effectiveOptions.ValidateRelationships)
        {
            ValidateRelationships(result, XlsxPackagePaths.PackageRelationships, workbook.PackageRelationships);
            ValidateRelationships(result, XlsxPackagePaths.WorkbookRelationships, workbook.WorkbookRelationships);
            ValidateWorkbookSheetRelationships(result, workbook);
        }

        if (effectiveOptions.ValidateWorksheets)
        {
            ValidateWorksheets(result, workbook.Worksheets, effectiveOptions);
        }

        if (effectiveOptions.ValidateStyles)
        {
            ValidateStylesPart(result, workbook);
        }

        if (effectiveOptions.ValidatePivots)
        {
            ValidatePivotParts(result, workbook);
        }

        if (effectiveOptions.ValidateDrawings)
        {
            ValidateDrawingParts(result, workbook);
        }

        if (effectiveOptions.ValidateUnknownParts)
        {
            ValidateUnknownParts(result, workbook.UnknownParts);
        }

        if (effectiveOptions.ValidateSecurityLimits)
        {
            ValidateSecurityLimits(result, workbook, NormalizeReaderOptions(effectiveOptions));
        }

        return result;
    }

    internal static XlsxWorkbookValidationOptions NormalizeOptions(XlsxWorkbookValidationOptions options)
    {
        if (options == null)
        {
            return new XlsxWorkbookValidationOptions();
        }

        return options;
    }

    internal static XlsxReaderOptions NormalizeReaderOptions(XlsxWorkbookValidationOptions options)
    {
        if (options == null || options.ReaderOptions == null)
        {
            return new XlsxReaderOptions();
        }

        return options.ReaderOptions;
    }

    internal static void ValidateWorkbookPart(XlsxValidationResult result, XlsxWorkbookPart workbookPart)
    {
        XlsxWorkbookPartWriteResult workbookResult = XlsxWorkbookPartWriter.ValidateMinimumWorkbookPart(workbookPart);
        AddDiagnostics(result, workbookResult.Validation);
    }

    internal static void ValidateRelationships(XlsxValidationResult result, string partPath, XlsxRelationshipsPart relationships)
    {
        if (relationships == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.relationshipsNull", "Workbook relationships part must not be null.");
            diagnostic.SetLocation(partPath, "", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        XlsxRelationshipsWriteResult relationshipsResult = XlsxPartRelationshipsWriter.ValidateRelationshipsPart(relationships);
        AddDiagnostics(result, relationshipsResult.Validation);
    }

    internal static void ValidateWorkbookSheetRelationships(XlsxValidationResult result, XlsxWorkbook workbook)
    {
        if (workbook == null || workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null || workbook.WorkbookRelationships == null || workbook.WorkbookRelationships.Relationships == null)
        {
            return;
        }

        foreach (XlsxWorkbookSheet sheet in workbook.WorkbookPart.Sheets)
        {
            if (sheet == null || sheet.RelationshipId == null || sheet.RelationshipId.Length == 0)
            {
                continue;
            }

            XlsxRelationship? relationship = workbook.WorkbookRelationships.GetRelationship(sheet.RelationshipId);
            if (relationship == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.sheetRelationshipMissing", "Workbook sheet relationship id does not exist in workbook relationships.");
                diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "r:id");
                diagnostic.SetRelationshipId(sheet.RelationshipId);
                result.AddDiagnostic(diagnostic);
            }
            else if (!IsSheetRelationshipType(relationship.Type))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.sheetRelationshipWrongType", "Workbook sheet relationship must target a worksheet, chartsheet, dialogsheet, or macrosheet.");
                diagnostic.SetLocation(XlsxPackagePaths.WorkbookRelationships, "Relationship", "Type");
                diagnostic.SetRelationshipId(sheet.RelationshipId);
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static bool IsSheetRelationshipType(string? relationshipType)
    {
        return relationshipType == XlsxRelationshipTypes.Worksheet || relationshipType == XlsxRelationshipTypes.Chartsheet || relationshipType == XlsxRelationshipTypes.Dialogsheet || relationshipType == XlsxRelationshipTypes.Macrosheet;
    }

    internal static void ValidateWorksheets(XlsxValidationResult result, List<XlsxWorksheetPart> worksheets, XlsxWorkbookValidationOptions options)
    {
        if (worksheets == null)
        {
            result.AddError("workbookValidator.worksheetsNull", "Workbook worksheet list must not be null.");
            return;
        }

        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            XlsxWorksheetPartWriteResult worksheetResult = XlsxWorksheetPartWriter.ValidateBasicWorksheetPart(worksheet);
            AddDiagnostics(result, worksheetResult.Validation);
            if (worksheet != null && options.ValidateTables)
            {
                ValidateWorksheetTables(result, worksheet);
            }

            if (worksheet != null && options.ValidateFormulas)
            {
                ValidateWorksheetFormulas(result, worksheet);
            }
        }
    }

    internal static void ValidateWorksheetTables(XlsxValidationResult result, XlsxWorksheetPart worksheet)
    {
        if (worksheet.Tables == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.tablesNull", "Worksheet table list must not be null.");
            diagnostic.SetLocation(worksheet.PartPath, "tableParts", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        foreach (XlsxTablePart table in worksheet.Tables)
        {
            XlsxTablePartWriteResult tableResult = XlsxTablePartWriter.ValidateTablePart(table);
            AddDiagnostics(result, tableResult.Validation);
        }
    }

    internal static void ValidateWorksheetFormulas(XlsxValidationResult result, XlsxWorksheetPart worksheet)
    {
        if (worksheet.Rows == null)
        {
            return;
        }

        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null)
                    {
                        ValidateCellFormula(result, worksheet, row, rowSequence, cell, cellSequence);
                    }

                    cellSequence = cellSequence + 1;
                }
            }

            rowSequence = rowSequence + 1;
        }
    }

    internal static void ValidateCellFormula(XlsxValidationResult result, XlsxWorksheetPart worksheet, XlsxWorksheetRow row, int rowSequence, XlsxWorksheetCell cell, int cellSequence)
    {
        string formula = "";
        if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.Text != null && cell.FormulaInfo.Text.Length > 0)
        {
            formula = cell.FormulaInfo.Text;
        }
        else if (cell.Formula != null && cell.Formula.Length > 0)
        {
            formula = cell.Formula;
        }

        if (formula == null || formula.Length == 0)
        {
            return;
        }

        if (formula.StartsWith("="))
        {
            formula = formula.Substring(1);
        }

        XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(formula);
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(worksheet.PartPath, GetCellReference(worksheet, row, rowSequence, cell, cellSequence), "");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static string GetCellReference(XlsxWorksheetPart worksheet, XlsxWorksheetRow row, int rowSequence, XlsxWorksheetCell cell, int cellSequence)
    {
        if (cell != null && cell.Reference != null && cell.Reference.Length > 0)
        {
            return cell.Reference;
        }

        return XlsxA1CellReferenceConverter.FormatString(worksheet.GetEffectiveRowIndex(row, rowSequence), worksheet.GetEffectiveCellColumn(cell, cellSequence), false, false);
    }

    internal static void ValidateStylesPart(XlsxValidationResult result, XlsxWorkbook workbook)
    {
        if (workbook == null || (!workbook.HasStyles && !workbook.HasStylesContent()))
        {
            return;
        }

        XlsxStylesPartWriteResult stylesResult = XlsxStylesPartWriter.ValidateStylesPart(workbook.Styles);
        AddDiagnostics(result, stylesResult.Validation);
    }

    internal static void ValidatePivotParts(XlsxValidationResult result, XlsxWorkbook workbook)
    {
        if (workbook == null)
        {
            return;
        }

        if (workbook.PivotCacheDefinitions != null)
        {
            foreach (XlsxPivotCacheDefinitionPart part in workbook.PivotCacheDefinitions)
            {
                XlsxPivotCacheDefinitionWriteResult cacheResult = XlsxPivotCacheDefinitionWriter.ValidatePivotCacheDefinition(part);
                AddDiagnostics(result, cacheResult.Validation);
            }
        }

        if (workbook.PivotTableDefinitions != null)
        {
            foreach (XlsxPivotTableDefinitionPart part in workbook.PivotTableDefinitions)
            {
                XlsxPivotTableDefinitionWriteResult tableResult = XlsxPivotTableDefinitionWriter.ValidatePivotTableDefinition(part);
                AddDiagnostics(result, tableResult.Validation);
            }
        }
    }

    internal static void ValidateDrawingParts(XlsxValidationResult result, XlsxWorkbook workbook)
    {
        if (workbook == null || workbook.UnknownParts == null || workbook.UnknownParts.Parts == null)
        {
            return;
        }

        foreach (XlsxUnknownPart part in workbook.UnknownParts.Parts)
        {
            if (part != null && part.ContentType == XlsxContentTypes.Drawing)
            {
                XlsxDrawingPart drawing = new XlsxDrawingPartReader().ReadBytes(part.Data);
                XlsxDrawingPartWriteResult drawingResult = XlsxDrawingPartWriter.ValidateDrawingPart(drawing);
                AddDiagnostics(result, drawing.Validation);
                AddDiagnostics(result, drawingResult.Validation);
            }
        }
    }

    internal static void ValidateUnknownParts(XlsxValidationResult result, XlsxUnknownPartStore store)
    {
        if (store == null)
        {
            result.AddError("workbookValidator.unknownPartsNull", "Workbook unknown part store must not be null.");
            return;
        }

        AddDiagnostics(result, store.Validation);
        if (store.Parts == null)
        {
            result.AddError("workbookValidator.unknownPartListNull", "Workbook unknown part list must not be null.");
            return;
        }

        Dictionary<string, bool> paths = new Dictionary<string, bool>();
        foreach (XlsxUnknownPart part in store.Parts)
        {
            ValidateUnknownPart(result, part, paths);
        }
    }

    internal static void ValidateUnknownPart(XlsxValidationResult result, XlsxUnknownPart part, Dictionary<string, bool> paths)
    {
        if (part == null)
        {
            result.AddError("workbookValidator.unknownPartNull", "Workbook unknown part list must not contain null entries.");
            return;
        }

        XlsxPackagePathResult path = XlsxPackagePath.NormalizePartPath(part.Path);
        foreach (XlsxDiagnostic diagnostic in path.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.Path, "", "");
            result.AddDiagnostic(diagnostic);
        }

        if (path.Validation.IsValid)
        {
            if (paths.ContainsKey(path.Path))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.unknownPartDuplicate", "Workbook unknown part paths must be unique.");
                diagnostic.SetLocation(path.Path, "", "");
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                paths.Add(path.Path, true);
            }
        }

        if (part.Data == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbookValidator.unknownPartDataNull", "Workbook unknown part data must not be null.");
            diagnostic.SetLocation(part.Path, "", "");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateSecurityLimits(XlsxValidationResult result, XlsxWorkbook workbook, XlsxReaderOptions options)
    {
        if (options == null || workbook == null)
        {
            return;
        }

        int relationshipCount = CountRelationships(workbook.PackageRelationships) + CountRelationships(workbook.WorkbookRelationships);
        if (relationshipCount > options.MaxRelationshipsPerPart)
        {
            result.AddError("workbookValidator.relationshipLimit", "Workbook relationship count exceeds the configured relationship limit.");
        }

        int worksheetCount = 0;
        if (workbook.WorkbookPart != null && workbook.WorkbookPart.Sheets != null)
        {
            worksheetCount = workbook.WorkbookPart.Sheets.Count;
        }

        if (worksheetCount > options.MaxZipEntries)
        {
            result.AddError("workbookValidator.sheetLimit", "Workbook sheet count exceeds the configured package entry limit.");
        }

        int cellCount = CountCells(workbook.Worksheets);
        if (cellCount > options.MaxCells)
        {
            result.AddError("workbookValidator.cellLimit", "Workbook loaded cell count exceeds the configured cell limit.");
        }

        int maxRow = GetMaxRow(workbook.Worksheets);
        if (maxRow > options.MaxRows)
        {
            result.AddError("workbookValidator.rowLimit", "Workbook loaded row index exceeds the configured row limit.");
        }

        int maxColumn = GetMaxColumn(workbook.Worksheets);
        if (maxColumn > options.MaxColumns)
        {
            result.AddError("workbookValidator.columnLimit", "Workbook loaded column index exceeds the configured column limit.");
        }
    }

    internal static int CountRelationships(XlsxRelationshipsPart relationships)
    {
        if (relationships == null || relationships.Relationships == null)
        {
            return 0;
        }

        return relationships.Relationships.Count;
    }

    internal static int CountCells(List<XlsxWorksheetPart> worksheets)
    {
        int count = 0;
        if (worksheets == null)
        {
            return count;
        }

        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row != null && row.Cells != null)
                {
                    count = count + row.Cells.Count;
                }
            }
        }

        return count;
    }

    internal static int GetMaxRow(List<XlsxWorksheetPart> worksheets)
    {
        int maxRow = 0;
        if (worksheets == null)
        {
            return maxRow;
        }

        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            int rowSequence = 1;
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                int rowIndex = worksheet.GetEffectiveRowIndex(row, rowSequence);
                if (rowIndex > maxRow)
                {
                    maxRow = rowIndex;
                }

                rowSequence = rowSequence + 1;
            }
        }

        return maxRow;
    }

    internal static int GetMaxColumn(List<XlsxWorksheetPart> worksheets)
    {
        int maxColumn = 0;
        if (worksheets == null)
        {
            return maxColumn;
        }

        foreach (XlsxWorksheetPart worksheet in worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null)
                {
                    continue;
                }

                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    int column = worksheet.GetEffectiveCellColumn(cell, cellSequence);
                    if (column > maxColumn)
                    {
                        maxColumn = column;
                    }

                    cellSequence = cellSequence + 1;
                }
            }
        }

        return maxColumn;
    }

    internal static void AddDiagnostics(XlsxValidationResult result, XlsxValidationResult source)
    {
        if (source == null)
        {
            return;
        }

        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
