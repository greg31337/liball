using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorksheetPart"/> data.
/// </summary>
public class XlsxWorksheetPartOperations : IXlsxWorksheetPartOperations
{
    /// <summary>
    /// Initializes a new xlsx worksheet part operations instance.
    /// </summary>
    public XlsxWorksheetPartOperations()
    {
    }

    /// <summary>Sets the worksheet tab color from a six- or eight-digit RGB value.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="rgb">The RGB or ARGB hexadecimal color.</param>
    /// <returns>The mutation result and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult SetTabColorRgb(XlsxWorksheetPart data, string rgb)
    {
        return data.SetTabColorRgb(rgb);
    }

    /// <summary>Sets worksheet outline summary and display properties.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="summaryBelow">Whether summary rows appear below detail rows.</param>
    /// <param name="summaryRight">Whether summary columns appear to the right.</param>
    /// <param name="showOutlineSymbols">Whether outline symbols are displayed.</param>
    /// <param name="applyStyles">Whether styles are applied to outlines.</param>
    /// <returns>The mutation result and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult SetOutlineProperties(XlsxWorksheetPart data, bool summaryBelow, bool summaryRight, bool showOutlineSymbols, bool applyStyles)
    {
        return data.SetOutlineProperties(summaryBelow, summaryRight, showOutlineSymbols, applyStyles);
    }

    /// <summary>Adds a named worksheet scenario.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="name">The unique scenario name.</param>
    /// <returns>The added scenario and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult AddScenario(XlsxWorksheetPart data, string name)
    {
        return data.AddScenario(name);
    }

    /// <summary>Adds a custom worksheet view.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="guid">The unique custom-view identifier.</param>
    /// <returns>The added custom view and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult AddCustomSheetView(XlsxWorksheetPart data, string guid)
    {
        return data.AddCustomSheetView(guid);
    }

    /// <summary>Adds an ignored-error rule for one or more A1 ranges.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="squareReference">The whitespace-separated A1 ranges.</param>
    /// <returns>The added ignored-error rule and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult AddIgnoredError(XlsxWorksheetPart data, string squareReference)
    {
        return data.AddIgnoredError(squareReference);
    }

    /// <summary>Adds a typed smart tag to a worksheet cell.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="reference">The target A1 cell reference.</param>
    /// <param name="typeId">The positive smart-tag type identifier.</param>
    /// <returns>The added smart tag and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult AddSmartTag(XlsxWorksheetPart data, string reference, uint typeId)
    {
        return data.AddSmartTag(reference, typeId);
    }

    /// <summary>Adds a worksheet web-publish item.</summary>
    /// <param name="data">The worksheet to update.</param>
    /// <param name="id">The positive publish item identifier.</param>
    /// <param name="destinationFile">The destination file or URI text.</param>
    /// <returns>The added web-publish item and validation diagnostics.</returns>
    public XlsxWorksheetMetadataResult AddWebPublishItem(XlsxWorksheetPart data, uint id, string destinationFile)
    {
        return data.AddWebPublishItem(id, destinationFile);
    }

    /// <summary>
    /// Selects the worksheet storage representation using the supplied options.
    /// </summary>
    /// <param name="data">The worksheet data to inspect.</param>
    /// <param name="options">The worksheet storage selection options.</param>
    /// <returns>The selected worksheet storage kind.</returns>
    public XlsxWorksheetStorageKind SelectStorageKind(XlsxWorksheetPart data, XlsxWorksheetStorageOptions options)
    {
        XlsxWorksheetStorageSelectionResult result = new XlsxWorksheetStorageSelector().SelectWorksheetPartWithOptions(data, options);
        return result.Kind;
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddSparkline through the AddSparkline operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="cellReference">The cell reference value used by the operation.</param>
    /// <param name="dataRange">The data range value used by the operation.</param>
    /// <param name="type">The type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSparklineOperationResult AddSparkline(XlsxWorksheetPart data, string cellReference, string dataRange, string type)
    {
        return data.AddSparkline(cellReference, dataRange, type);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetSparklineGroupByType through the GetSparklineGroupByType operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="type">The type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSparklineGroup? GetSparklineGroupByType(XlsxWorksheetPart data, string type)
    {
        return data.GetSparklineGroupByType(type);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetRowByUnsignedIndex through the GetRowByUnsignedIndex operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRow? GetRowByUnsignedIndex(XlsxWorksheetPart data, uint rowIndex)
    {
        return data.GetRowByUnsignedIndex(rowIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetRow through the GetRow operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRow? GetRow(XlsxWorksheetPart data, int rowIndex)
    {
        return data.GetRow(rowIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetCell through the GetCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCell? GetCell(XlsxWorksheetPart data, string? reference)
    {
        return data.GetCell(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetCellByReference through the GetCellByReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCell? GetCellByReference(XlsxWorksheetPart data, string? reference)
    {
        return data.GetCellByReference(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetCellAt through the GetCellAt operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCell? GetCellAt(XlsxWorksheetPart data, int rowIndex, int columnIndex)
    {
        return data.GetCellAt(rowIndex, columnIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ContainsCell through the ContainsCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool ContainsCell(XlsxWorksheetPart data, string reference)
    {
        return data.ContainsCell(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ContainsCellAt through the ContainsCellAt operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool ContainsCellAt(XlsxWorksheetPart data, int rowIndex, int columnIndex)
    {
        return data.ContainsCellAt(rowIndex, columnIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetRange through the GetRange operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRange GetRange(XlsxWorksheetPart data, string reference)
    {
        return data.GetRange(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.Range through the Range operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRange Range(XlsxWorksheetPart data, string reference)
    {
        return data.Range(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportDataTable through the ImportDataTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="dataTable">The data table value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableImportResult ImportDataTable(XlsxWorksheetPart data, string topLeftCell, System.Data.DataTable? dataTable)
    {
        return data.ImportDataTable(topLeftCell, dataTable);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportDataTableWithHeaders through the ImportDataTableWithHeaders operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="dataTable">The data table value used by the operation.</param>
    /// <param name="includeHeaders">The include headers value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableImportResult ImportDataTableWithHeaders(XlsxWorksheetPart data, string topLeftCell, System.Data.DataTable? dataTable, bool includeHeaders)
    {
        return data.ImportDataTableWithHeaders(topLeftCell, dataTable, includeHeaders);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportDataTableAsTable through the ImportDataTableAsTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="dataTable">The data table value used by the operation.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableImportResult ImportDataTableAsTable(XlsxWorksheetPart data, string topLeftCell, System.Data.DataTable? dataTable, string tableName)
    {
        return data.ImportDataTableAsTable(topLeftCell, dataTable, tableName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportDataTableAsTableWithHeaders through the ImportDataTableAsTableWithHeaders operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="dataTable">The data table value used by the operation.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="includeHeaders">The include headers value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableImportResult ImportDataTableAsTableWithHeaders(XlsxWorksheetPart data, string topLeftCell, System.Data.DataTable? dataTable, string tableName, bool includeHeaders)
    {
        return data.ImportDataTableAsTableWithHeaders(topLeftCell, dataTable, tableName, includeHeaders);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExportDataTable through the ExportDataTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableExportResult ExportDataTable(XlsxWorksheetPart data, string reference)
    {
        return data.ExportDataTable(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExportDataTableWithHeaders through the ExportDataTableWithHeaders operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="firstRowAsHeaders">The first row as headers value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableExportResult ExportDataTableWithHeaders(XlsxWorksheetPart data, string reference, bool firstRowAsHeaders)
    {
        return data.ExportDataTableWithHeaders(reference, firstRowAsHeaders);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportCsv through the ImportCsv operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="csv">The csv value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxCsvImportResult ImportCsv(XlsxWorksheetPart data, string topLeftCell, string csv)
    {
        return data.ImportCsv(topLeftCell, csv);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ImportCsvWithOptions through the ImportCsvWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <param name="csv">The csv value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxCsvImportResult ImportCsvWithOptions(XlsxWorksheetPart data, string topLeftCell, string csv, XlsxCsvOptions options)
    {
        return data.ImportCsvWithOptions(topLeftCell, csv, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExportCsv through the ExportCsv operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxCsvExportResult ExportCsv(XlsxWorksheetPart data, string reference)
    {
        return data.ExportCsv(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExportCsvWithOptions through the ExportCsvWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxCsvExportResult ExportCsvWithOptions(XlsxWorksheetPart data, string reference, XlsxCsvOptions options)
    {
        return data.ExportCsvWithOptions(reference, options);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExportTableToDataTable through the ExportTableToDataTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDataTableExportResult ExportTableToDataTable(XlsxWorksheetPart data, string tableName)
    {
        return data.ExportTableToDataTable(tableName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetTable through the GetTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxTablePart? GetTable(XlsxWorksheetPart data, string name)
    {
        return data.GetTable(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTable through the AddTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult AddTable(XlsxWorksheetPart data, string reference, string name)
    {
        return data.AddTable(reference, name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTableWithHeaderRow through the AddTableWithHeaderRow operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="hasHeaderRow">The has header row value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult AddTableWithHeaderRow(XlsxWorksheetPart data, string reference, string name, bool hasHeaderRow)
    {
        return data.AddTableWithHeaderRow(reference, name, hasHeaderRow);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ResizeTable through the ResizeTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult ResizeTable(XlsxWorksheetPart data, string tableName, string reference)
    {
        return data.ResizeTable(tableName, reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.RenameTable through the RenameTable operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="oldName">The old name value used by the operation.</param>
    /// <param name="newName">The new name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult RenameTable(XlsxWorksheetPart data, string oldName, string newName)
    {
        return data.RenameTable(oldName, newName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTableColumn through the AddTableColumn operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult AddTableColumn(XlsxWorksheetPart data, string tableName, string columnName)
    {
        return data.AddTableColumn(tableName, columnName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.RenameTableColumn through the RenameTableColumn operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="oldColumnName">The old column name value used by the operation.</param>
    /// <param name="newColumnName">The new column name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult RenameTableColumn(XlsxWorksheetPart data, string tableName, string oldColumnName, string newColumnName)
    {
        return data.RenameTableColumn(tableName, oldColumnName, newColumnName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.DeleteTableColumn through the DeleteTableColumn operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult DeleteTableColumn(XlsxWorksheetPart data, string tableName, string columnName)
    {
        return data.DeleteTableColumn(tableName, columnName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableHeaderRow through the SetTableHeaderRow operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="visible">The visible value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableHeaderRow(XlsxWorksheetPart data, string tableName, bool visible)
    {
        return data.SetTableHeaderRow(tableName, visible);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableTotalsRow through the SetTableTotalsRow operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="visible">The visible value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableTotalsRow(XlsxWorksheetPart data, string tableName, bool visible)
    {
        return data.SetTableTotalsRow(tableName, visible);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableColumnFormula through the SetTableColumnFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableColumnFormula(XlsxWorksheetPart data, string tableName, string columnName, string formula)
    {
        return data.SetTableColumnFormula(tableName, columnName, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableColumnTotalsRowFormula through the SetTableColumnTotalsRowFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableColumnTotalsRowFormula(XlsxWorksheetPart data, string tableName, string columnName, string formula)
    {
        return data.SetTableColumnTotalsRowFormula(tableName, columnName, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableColumnTotalsRowFunction through the SetTableColumnTotalsRowFunction operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="functionName">The function name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableColumnTotalsRowFunction(XlsxWorksheetPart data, string tableName, string columnName, string functionName)
    {
        return data.SetTableColumnTotalsRowFunction(tableName, columnName, functionName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableColumnTotalsRowLabel through the SetTableColumnTotalsRowLabel operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="label">The label value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableColumnTotalsRowLabel(XlsxWorksheetPart data, string tableName, string columnName, string label)
    {
        return data.SetTableColumnTotalsRowLabel(tableName, columnName, label);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableAutoFilter through the SetTableAutoFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="enabled">The enabled value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableAutoFilter(XlsxWorksheetPart data, string tableName, bool enabled)
    {
        return data.SetTableAutoFilter(tableName, enabled);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearTableAutoFilter through the ClearTableAutoFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult ClearTableAutoFilter(XlsxWorksheetPart data, string tableName)
    {
        return data.ClearTableAutoFilter(tableName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableSortState through the SetTableSortState operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="descending">The descending value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableSortState(XlsxWorksheetPart data, string tableName, string columnName, bool descending)
    {
        return data.SetTableSortState(tableName, columnName, descending);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearTableSortState through the ClearTableSortState operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult ClearTableSortState(XlsxWorksheetPart data, string tableName)
    {
        return data.ClearTableSortState(tableName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddAutoFilter through the AddAutoFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult AddAutoFilter(XlsxWorksheetPart data, string reference)
    {
        return data.AddAutoFilter(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearAutoFilter through the ClearAutoFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult ClearAutoFilter(XlsxWorksheetPart data)
    {
        return data.ClearAutoFilter();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearAutoFilterColumn through the ClearAutoFilterColumn operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult ClearAutoFilterColumn(XlsxWorksheetPart data, uint columnId)
    {
        return data.ClearAutoFilterColumn(columnId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterValues through the SetAutoFilterValues operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="values">The values value used by the operation.</param>
    /// <param name="includeBlanks">The include blanks value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterValues(XlsxWorksheetPart data, uint columnId, List<string> values, bool includeBlanks)
    {
        return data.SetAutoFilterValues(columnId, values, includeBlanks);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterCustom through the SetAutoFilterCustom operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="firstOperator">The first operator value used by the operation.</param>
    /// <param name="firstValue">The first value value used by the operation.</param>
    /// <param name="secondOperator">The second operator value used by the operation.</param>
    /// <param name="secondValue">The second value value used by the operation.</param>
    /// <param name="andOperator">The and operator value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterCustom(XlsxWorksheetPart data, uint columnId, string firstOperator, string firstValue, string secondOperator, string secondValue, bool andOperator)
    {
        return data.SetAutoFilterCustom(columnId, firstOperator, firstValue, secondOperator, secondValue, andOperator);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterDynamic through the SetAutoFilterDynamic operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="filterType">The filter type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterDynamic(XlsxWorksheetPart data, uint columnId, string filterType)
    {
        return data.SetAutoFilterDynamic(columnId, filterType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterDateGroup through the SetAutoFilterDateGroup operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="year">The year value used by the operation.</param>
    /// <param name="month">The month value used by the operation.</param>
    /// <param name="day">The day value used by the operation.</param>
    /// <param name="grouping">The grouping value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterDateGroup(XlsxWorksheetPart data, uint columnId, uint year, uint month, uint day, string grouping)
    {
        return data.SetAutoFilterDateGroup(columnId, year, month, day, grouping);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterTop10 through the SetAutoFilterTop10 operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="top">The top value used by the operation.</param>
    /// <param name="percent">The percent value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterTop10(XlsxWorksheetPart data, uint columnId, double value, bool top, bool percent)
    {
        return data.SetAutoFilterTop10(columnId, value, top, percent);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterColor through the SetAutoFilterColor operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="dxfId">The dxf id value used by the operation.</param>
    /// <param name="cellColor">The cell color value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterColor(XlsxWorksheetPart data, uint columnId, uint dxfId, bool cellColor)
    {
        return data.SetAutoFilterColor(columnId, dxfId, cellColor);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetAutoFilterIcon through the SetAutoFilterIcon operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnId">The column id value used by the operation.</param>
    /// <param name="iconSet">The icon set value used by the operation.</param>
    /// <param name="iconId">The icon id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetAutoFilterIcon(XlsxWorksheetPart data, uint columnId, string iconSet, uint iconId)
    {
        return data.SetAutoFilterIcon(columnId, iconSet, iconId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearTableColumnFilter through the ClearTableColumnFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult ClearTableColumnFilter(XlsxWorksheetPart data, string tableName, string columnName)
    {
        return data.ClearTableColumnFilter(tableName, columnName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableFilterValues through the SetTableFilterValues operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="values">The values value used by the operation.</param>
    /// <param name="includeBlanks">The include blanks value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableFilterValues(XlsxWorksheetPart data, string tableName, string columnName, List<string> values, bool includeBlanks)
    {
        return data.SetTableFilterValues(tableName, columnName, values, includeBlanks);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableCustomFilter through the SetTableCustomFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="firstOperator">The first operator value used by the operation.</param>
    /// <param name="firstValue">The first value value used by the operation.</param>
    /// <param name="secondOperator">The second operator value used by the operation.</param>
    /// <param name="secondValue">The second value value used by the operation.</param>
    /// <param name="andOperator">The and operator value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableCustomFilter(XlsxWorksheetPart data, string tableName, string columnName, string firstOperator, string firstValue, string secondOperator, string secondValue, bool andOperator)
    {
        return data.SetTableCustomFilter(tableName, columnName, firstOperator, firstValue, secondOperator, secondValue, andOperator);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableDynamicFilter through the SetTableDynamicFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="filterType">The filter type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableDynamicFilter(XlsxWorksheetPart data, string tableName, string columnName, string filterType)
    {
        return data.SetTableDynamicFilter(tableName, columnName, filterType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableDateGroupFilter through the SetTableDateGroupFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="year">The year value used by the operation.</param>
    /// <param name="month">The month value used by the operation.</param>
    /// <param name="day">The day value used by the operation.</param>
    /// <param name="grouping">The grouping value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableDateGroupFilter(XlsxWorksheetPart data, string tableName, string columnName, uint year, uint month, uint day, string grouping)
    {
        return data.SetTableDateGroupFilter(tableName, columnName, year, month, day, grouping);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableTop10Filter through the SetTableTop10Filter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="top">The top value used by the operation.</param>
    /// <param name="percent">The percent value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableTop10Filter(XlsxWorksheetPart data, string tableName, string columnName, double value, bool top, bool percent)
    {
        return data.SetTableTop10Filter(tableName, columnName, value, top, percent);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableColorFilter through the SetTableColorFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="dxfId">The dxf id value used by the operation.</param>
    /// <param name="cellColor">The cell color value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableColorFilter(XlsxWorksheetPart data, string tableName, string columnName, uint dxfId, bool cellColor)
    {
        return data.SetTableColorFilter(tableName, columnName, dxfId, cellColor);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableIconFilter through the SetTableIconFilter operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="columnName">The column name value used by the operation.</param>
    /// <param name="iconSet">The icon set value used by the operation.</param>
    /// <param name="iconId">The icon id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetFilterResult SetTableIconFilter(XlsxWorksheetPart data, string tableName, string columnName, string iconSet, uint iconId)
    {
        return data.SetTableIconFilter(tableName, columnName, iconSet, iconId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddDataValidation through the AddDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddDataValidation(XlsxWorksheetPart data, string range)
    {
        return data.AddDataValidation(range);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddListDataValidation through the AddListDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="values">The values value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddListDataValidation(XlsxWorksheetPart data, string range, List<string> values)
    {
        return data.AddListDataValidation(range, values);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddListDataValidationFormula through the AddListDataValidationFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddListDataValidationFormula(XlsxWorksheetPart data, string range, string formula)
    {
        return data.AddListDataValidationFormula(range, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddWholeNumberDataValidation through the AddWholeNumberDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddWholeNumberDataValidation(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddWholeNumberDataValidation(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddDecimalDataValidation through the AddDecimalDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddDecimalDataValidation(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddDecimalDataValidation(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddDateDataValidation through the AddDateDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddDateDataValidation(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddDateDataValidation(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTimeDataValidation through the AddTimeDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddTimeDataValidation(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddTimeDataValidation(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTextLengthDataValidation through the AddTextLengthDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddTextLengthDataValidation(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddTextLengthDataValidation(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddCustomDataValidation through the AddCustomDataValidation operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult AddCustomDataValidation(XlsxWorksheetPart data, string range, string formula)
    {
        return data.AddCustomDataValidation(range, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetDataValidationMessages through the SetDataValidationMessages operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="validation">The validation value used by the operation.</param>
    /// <param name="showInputMessage">The show input message value used by the operation.</param>
    /// <param name="promptTitle">The prompt title value used by the operation.</param>
    /// <param name="prompt">The prompt value used by the operation.</param>
    /// <param name="showErrorMessage">The show error message value used by the operation.</param>
    /// <param name="errorTitle">The error title value used by the operation.</param>
    /// <param name="error">The error value used by the operation.</param>
    /// <param name="errorStyle">The error style value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult SetDataValidationMessages(XlsxWorksheetPart data, XlsxDataValidation validation, bool showInputMessage, string promptTitle, string prompt, bool showErrorMessage, string errorTitle, string error, string errorStyle)
    {
        return data.SetDataValidationMessages(validation, showInputMessage, promptTitle, prompt, showErrorMessage, errorTitle, error, errorStyle);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetDataValidationAllowBlank through the SetDataValidationAllowBlank operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="validation">The validation value used by the operation.</param>
    /// <param name="allowBlank">The allow blank value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult SetDataValidationAllowBlank(XlsxWorksheetPart data, XlsxDataValidation? validation, bool allowBlank)
    {
        return data.SetDataValidationAllowBlank(validation, allowBlank);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetDataValidationShowDropDown through the SetDataValidationShowDropDown operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="validation">The validation value used by the operation.</param>
    /// <param name="showDropDown">The show drop down value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult SetDataValidationShowDropDown(XlsxWorksheetPart data, XlsxDataValidation validation, bool showDropDown)
    {
        return data.SetDataValidationShowDropDown(validation, showDropDown);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearDataValidations through the ClearDataValidations operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetDataValidationResult ClearDataValidations(XlsxWorksheetPart data)
    {
        return data.ClearDataValidations();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddConditionalFormattingRule through the AddConditionalFormattingRule operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="type">The type value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddConditionalFormattingRule(XlsxWorksheetPart data, string range, string type, string formula)
    {
        return data.AddConditionalFormattingRule(range, type, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddExpressionConditionalFormatting through the AddExpressionConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddExpressionConditionalFormatting(XlsxWorksheetPart data, string range, string formula)
    {
        return data.AddExpressionConditionalFormatting(range, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddCellIsConditionalFormatting through the AddCellIsConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    /// <param name="formula1">The formula1 value used by the operation.</param>
    /// <param name="formula2">The formula2 value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddCellIsConditionalFormatting(XlsxWorksheetPart data, string range, string operation, string formula1, string formula2)
    {
        return data.AddCellIsConditionalFormatting(range, operation, formula1, formula2);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddContainsTextConditionalFormatting through the AddContainsTextConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddContainsTextConditionalFormatting(XlsxWorksheetPart data, string range, string text)
    {
        return data.AddContainsTextConditionalFormatting(range, text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddTop10ConditionalFormatting through the AddTop10ConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="rank">The rank value used by the operation.</param>
    /// <param name="top">The top value used by the operation.</param>
    /// <param name="percent">The percent value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddTop10ConditionalFormatting(XlsxWorksheetPart data, string range, uint rank, bool top, bool percent)
    {
        return data.AddTop10ConditionalFormatting(range, rank, top, percent);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddAboveAverageConditionalFormatting through the AddAboveAverageConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="aboveAverage">The above average value used by the operation.</param>
    /// <param name="equalAverage">The equal average value used by the operation.</param>
    /// <param name="stdDev">The std dev value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddAboveAverageConditionalFormatting(XlsxWorksheetPart data, string range, bool aboveAverage, bool equalAverage, uint stdDev)
    {
        return data.AddAboveAverageConditionalFormatting(range, aboveAverage, equalAverage, stdDev);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddDuplicateValuesConditionalFormatting through the AddDuplicateValuesConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddDuplicateValuesConditionalFormatting(XlsxWorksheetPart data, string range)
    {
        return data.AddDuplicateValuesConditionalFormatting(range);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddUniqueValuesConditionalFormatting through the AddUniqueValuesConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddUniqueValuesConditionalFormatting(XlsxWorksheetPart data, string range)
    {
        return data.AddUniqueValuesConditionalFormatting(range);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddColorScaleConditionalFormatting through the AddColorScaleConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="minColorRgb">The min color rgb value used by the operation.</param>
    /// <param name="midpointType">The midpoint type value used by the operation.</param>
    /// <param name="midpointValue">The midpoint value value used by the operation.</param>
    /// <param name="midpointColorRgb">The midpoint color rgb value used by the operation.</param>
    /// <param name="maxColorRgb">The max color rgb value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddColorScaleConditionalFormatting(XlsxWorksheetPart data, string range, string minColorRgb, string midpointType, string midpointValue, string midpointColorRgb, string maxColorRgb)
    {
        return data.AddColorScaleConditionalFormatting(range, minColorRgb, midpointType, midpointValue, midpointColorRgb, maxColorRgb);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddDataBarConditionalFormatting through the AddDataBarConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="colorRgb">The color rgb value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddDataBarConditionalFormatting(XlsxWorksheetPart data, string range, string colorRgb)
    {
        return data.AddDataBarConditionalFormatting(range, colorRgb);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddIconSetConditionalFormatting through the AddIconSetConditionalFormatting operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="iconSet">The icon set value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult AddIconSetConditionalFormatting(XlsxWorksheetPart data, string range, string iconSet)
    {
        return data.AddIconSetConditionalFormatting(range, iconSet);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetConditionalFormattingDxfId through the SetConditionalFormattingDxfId operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rule">The rule value used by the operation.</param>
    /// <param name="dxfId">The dxf id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult SetConditionalFormattingDxfId(XlsxWorksheetPart data, XlsxConditionalFormatRule? rule, uint dxfId)
    {
        return data.SetConditionalFormattingDxfId(rule, dxfId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetConditionalFormattingStopIfTrue through the SetConditionalFormattingStopIfTrue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rule">The rule value used by the operation.</param>
    /// <param name="stopIfTrue">The stop if true value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult SetConditionalFormattingStopIfTrue(XlsxWorksheetPart data, XlsxConditionalFormatRule rule, bool stopIfTrue)
    {
        return data.SetConditionalFormattingStopIfTrue(rule, stopIfTrue);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearConditionalFormattings through the ClearConditionalFormattings operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetConditionalFormattingResult ClearConditionalFormattings(XlsxWorksheetPart data)
    {
        return data.ClearConditionalFormattings();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableStyle through the SetTableStyle operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="styleName">The style name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableStyle(XlsxWorksheetPart data, string tableName, string styleName)
    {
        return data.SetTableStyle(tableName, styleName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTableStyleWithStripeOptions through the SetTableStyleWithStripeOptions operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="styleName">The style name value used by the operation.</param>
    /// <param name="showRowStripes">The show row stripes value used by the operation.</param>
    /// <param name="showColumnStripes">The show column stripes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetTableResult SetTableStyleWithStripeOptions(XlsxWorksheetPart data, string tableName, string styleName, bool showRowStripes, bool showColumnStripes)
    {
        return data.SetTableStyleWithStripeOptions(tableName, styleName, showRowStripes, showColumnStripes);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.InsertRows through the InsertRows operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="count">The count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRowEditResult InsertRows(XlsxWorksheetPart data, int rowIndex, int count)
    {
        return data.InsertRows(rowIndex, count);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.DeleteRows through the DeleteRows operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="count">The count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRowEditResult DeleteRows(XlsxWorksheetPart data, int rowIndex, int count)
    {
        return data.DeleteRows(rowIndex, count);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.InsertColumns through the InsertColumns operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <param name="count">The count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetColumnEditResult InsertColumns(XlsxWorksheetPart data, int columnIndex, int count)
    {
        return data.InsertColumns(columnIndex, count);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.DeleteColumns through the DeleteColumns operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <param name="count">The count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetColumnEditResult DeleteColumns(XlsxWorksheetPart data, int columnIndex, int count)
    {
        return data.DeleteColumns(columnIndex, count);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetOrCreateCellByReference through the GetOrCreateCellByReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult GetOrCreateCellByReference(XlsxWorksheetPart data, string reference)
    {
        return data.GetOrCreateCellByReference(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetOrCreateCell through the GetOrCreateCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult GetOrCreateCell(XlsxWorksheetPart data, int rowIndex, int columnIndex)
    {
        return data.GetOrCreateCell(rowIndex, columnIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetCellByReference through the SetCellByReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="cell">The cell value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetCellByReference(XlsxWorksheetPart data, string reference, XlsxWorksheetCell cell)
    {
        return data.SetCellByReference(reference, cell);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetCell through the SetCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <param name="cell">The cell value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetCell(XlsxWorksheetPart data, int rowIndex, int columnIndex, XlsxWorksheetCell cell)
    {
        return data.SetCell(rowIndex, columnIndex, cell);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetCellValueByReference through the SetCellValueByReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetCellValueByReference(XlsxWorksheetPart data, string reference, XlsxCellValue value)
    {
        return data.SetCellValueByReference(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetCellValue through the SetCellValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetCellValue(XlsxWorksheetPart data, string reference, XlsxCellValue value)
    {
        return data.SetCellValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetCellValueAt through the SetCellValueAt operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetCellValueAt(XlsxWorksheetPart data, int rowIndex, int columnIndex, XlsxCellValue value)
    {
        return data.SetCellValueAt(rowIndex, columnIndex, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetBlankValue through the SetBlankValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetBlankValue(XlsxWorksheetPart data, string reference)
    {
        return data.SetBlankValue(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetNumberValue through the SetNumberValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetNumberValue(XlsxWorksheetPart data, string reference, double value)
    {
        return data.SetNumberValue(reference, value);
    }

    /// <summary>Sets an exact invariant decimal cell value.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="value">The decimal value.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetDecimalValue(XlsxWorksheetPart data, string reference, decimal value)
    {
        return data.SetDecimalValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetTextValue through the SetTextValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetTextValue(XlsxWorksheetPart data, string reference, string value)
    {
        return data.SetTextValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetSharedStringValue through the SetSharedStringValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetSharedStringValue(XlsxWorksheetPart data, string reference, uint index)
    {
        return data.SetSharedStringValue(reference, index);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetInlineStringValue through the SetInlineStringValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetInlineStringValue(XlsxWorksheetPart data, string reference, string value)
    {
        return data.SetInlineStringValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetBooleanValue through the SetBooleanValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetBooleanValue(XlsxWorksheetPart data, string reference, bool value)
    {
        return data.SetBooleanValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetErrorValue through the SetErrorValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetErrorValue(XlsxWorksheetPart data, string reference, string value)
    {
        return data.SetErrorValue(reference, value);
    }

    /// <summary>Sets a typed Excel error cell value.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="value">The Excel error kind.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetTypedErrorValue(XlsxWorksheetPart data, string reference, XlsxCellErrorKind value)
    {
        return data.SetTypedErrorValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetDateTimeValue through the SetDateTimeValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetDateTimeValue(XlsxWorksheetPart data, string reference, System.DateTime value)
    {
        return data.SetDateTimeValue(reference, value);
    }

    /// <summary>Sets a date cell from exact ISO date or date-time text.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="isoDateText">ISO date or date-time text.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetIsoDateValue(XlsxWorksheetPart data, string reference, string isoDateText)
    {
        return data.SetIsoDateValue(reference, isoDateText);
    }

    /// <summary>Sets an elapsed duration as an exact Excel day fraction.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="value">The elapsed duration.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetDurationValue(XlsxWorksheetPart data, string reference, System.TimeSpan value)
    {
        return data.SetDurationValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetFormulaCachedValue through the SetFormulaCachedValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="cachedValue">The cached value value used by the operation.</param>
    /// <param name="cachedType">The cached type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetFormulaCachedValue(XlsxWorksheetPart data, string reference, string formula, string cachedValue, string cachedType)
    {
        return data.SetFormulaCachedValue(reference, formula, cachedValue, cachedType);
    }

    /// <summary>Sets a formula with a typed cached result.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="formula">Excel formula text without a leading equals sign.</param>
    /// <param name="cachedValue">The typed cached formula result.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetFormulaValue(XlsxWorksheetPart data, string reference, string formula, XlsxCellValue cachedValue)
    {
        return data.SetFormulaValue(reference, formula, cachedValue);
    }

    /// <summary>Sets typed rich text and phonetic content on a cell.</summary>
    /// <param name="data">The worksheet to modify.</param>
    /// <param name="reference">An A1 cell reference.</param>
    /// <param name="value">The typed inline rich-text content.</param>
    /// <returns>A result containing the affected cell and diagnostics.</returns>
    public XlsxWorksheetCellAccessResult SetRichTextValue(XlsxWorksheetPart data, string reference, XlsxInlineString value)
    {
        return data.SetRichTextValue(reference, value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetArrayFormula through the SetArrayFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="rangeReference">The range reference value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="cachedValue">The cached value value used by the operation.</param>
    /// <param name="cachedType">The cached type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetArrayFormula(XlsxWorksheetPart data, string reference, string rangeReference, string formula, string cachedValue, string cachedType)
    {
        return data.SetArrayFormula(reference, rangeReference, formula, cachedValue, cachedType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetDynamicArrayFormula through the SetDynamicArrayFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="spillRangeReference">The spill range reference value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="cachedValue">The cached value value used by the operation.</param>
    /// <param name="cachedType">The cached type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetDynamicArrayFormula(XlsxWorksheetPart data, string reference, string spillRangeReference, string formula, string cachedValue, string cachedType)
    {
        return data.SetDynamicArrayFormula(reference, spillRangeReference, formula, cachedValue, cachedType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SetRangeFormula through the SetRangeFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="rangeReference">The range reference value used by the operation.</param>
    /// <param name="formulaInfo">The formula info value used by the operation.</param>
    /// <param name="cachedValue">The cached value value used by the operation.</param>
    /// <param name="cachedType">The cached type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCellAccessResult SetRangeFormula(XlsxWorksheetPart data, string reference, string rangeReference, XlsxCellFormula formulaInfo, string cachedValue, string cachedType)
    {
        return data.SetRangeFormula(reference, rangeReference, formulaInfo, cachedValue, cachedType);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.BuildFormulaDependencyGraph through the BuildFormulaDependencyGraph operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaDependencyGraphResult BuildFormulaDependencyGraph(XlsxWorksheetPart data)
    {
        return data.BuildFormulaDependencyGraph();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ExpandSharedFormulas through the ExpandSharedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSharedFormulaResult ExpandSharedFormulas(XlsxWorksheetPart data)
    {
        return data.ExpandSharedFormulas();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CompactSharedFormulas through the CompactSharedFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSharedFormulaResult CompactSharedFormulas(XlsxWorksheetPart data)
    {
        return data.CompactSharedFormulas();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateFormula through the CalculateFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationResult CalculateFormula(XlsxWorksheetPart data, string formula)
    {
        return data.CalculateFormula(formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateCellFormula through the CalculateCellFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationResult CalculateCellFormula(XlsxWorksheetPart data, string reference)
    {
        return data.CalculateCellFormula(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateCellFormulaWithSpill through the CalculateCellFormulaWithSpill operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationResult CalculateCellFormulaWithSpill(XlsxWorksheetPart data, string? reference)
    {
        return data.CalculateCellFormulaWithSpill(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateAllFormulas through the CalculateAllFormulas operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationResult CalculateAllFormulas(XlsxWorksheetPart data)
    {
        return data.CalculateAllFormulas();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UpdateFormulaReferencesForRowInsert through the UpdateFormulaReferencesForRowInsert operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForRowInsert(XlsxWorksheetPart data, int startRow, int rowCount)
    {
        return data.UpdateFormulaReferencesForRowInsert(startRow, rowCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UpdateFormulaReferencesForRowDelete through the UpdateFormulaReferencesForRowDelete operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForRowDelete(XlsxWorksheetPart data, int startRow, int rowCount)
    {
        return data.UpdateFormulaReferencesForRowDelete(startRow, rowCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UpdateFormulaReferencesForColumnInsert through the UpdateFormulaReferencesForColumnInsert operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForColumnInsert(XlsxWorksheetPart data, int startColumn, int columnCount)
    {
        return data.UpdateFormulaReferencesForColumnInsert(startColumn, columnCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UpdateFormulaReferencesForColumnDelete through the UpdateFormulaReferencesForColumnDelete operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForColumnDelete(XlsxWorksheetPart data, int startColumn, int columnCount)
    {
        return data.UpdateFormulaReferencesForColumnDelete(startColumn, columnCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.MoveFormulaReferences through the MoveFormulaReferences operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="sourceFirstRow">The source first row value used by the operation.</param>
    /// <param name="sourceFirstColumn">The source first column value used by the operation.</param>
    /// <param name="sourceLastRow">The source last row value used by the operation.</param>
    /// <param name="sourceLastColumn">The source last column value used by the operation.</param>
    /// <param name="rowDelta">The row delta value used by the operation.</param>
    /// <param name="columnDelta">The column delta value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult MoveFormulaReferences(XlsxWorksheetPart data, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        return data.MoveFormulaReferences(sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CopyFormulaReferences through the CopyFormulaReferences operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="rowDelta">The row delta value used by the operation.</param>
    /// <param name="columnDelta">The column delta value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult CopyFormulaReferences(XlsxWorksheetPart data, int rowDelta, int columnDelta)
    {
        return data.CopyFormulaReferences(rowDelta, columnDelta);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.RenameFormulaSheetReferences through the RenameFormulaSheetReferences operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="oldSheetName">The old sheet name value used by the operation.</param>
    /// <param name="newSheetName">The new sheet name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult RenameFormulaSheetReferences(XlsxWorksheetPart data, string oldSheetName, string newSheetName)
    {
        return data.RenameFormulaSheetReferences(oldSheetName, newSheetName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.MoveFormulaReferences through the MoveFormulaTextReferences operation.
    /// </summary>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="sourceFirstRow">The source first row value used by the operation.</param>
    /// <param name="sourceFirstColumn">The source first column value used by the operation.</param>
    /// <param name="sourceLastRow">The source last row value used by the operation.</param>
    /// <param name="sourceLastColumn">The source last column value used by the operation.</param>
    /// <param name="rowDelta">The row delta value used by the operation.</param>
    /// <param name="columnDelta">The column delta value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult MoveFormulaTextReferences(string formula, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        return XlsxWorksheetPart.MoveFormulaReferences(formula, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CopyFormulaReferences through the CopyFormulaTextReferences operation.
    /// </summary>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="rowDelta">The row delta value used by the operation.</param>
    /// <param name="columnDelta">The column delta value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult CopyFormulaTextReferences(string formula, int rowDelta, int columnDelta)
    {
        return XlsxWorksheetPart.CopyFormulaReferences(formula, rowDelta, columnDelta);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.RenameFormulaSheetReferences through the RenameFormulaTextSheetReferences operation.
    /// </summary>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <param name="oldSheetName">The old sheet name value used by the operation.</param>
    /// <param name="newSheetName">The new sheet name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult RenameFormulaTextSheetReferences(string formula, string oldSheetName, string newSheetName)
    {
        return XlsxWorksheetPart.RenameFormulaSheetReferences(formula, oldSheetName, newSheetName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetUsedRange through the GetUsedRange operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetUsedRangeResult GetUsedRange(XlsxWorksheetPart data)
    {
        return data.GetUsedRange();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateUsedRange through the CalculateUsedRange operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetUsedRangeResult CalculateUsedRange(XlsxWorksheetPart data)
    {
        return data.CalculateUsedRange();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CalculateDimensionReference through the CalculateDimensionReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public string CalculateDimensionReference(XlsxWorksheetPart data)
    {
        return data.CalculateDimensionReference();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ApplyCalculatedDimension through the ApplyCalculatedDimension operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetUsedRangeResult ApplyCalculatedDimension(XlsxWorksheetPart data)
    {
        return data.ApplyCalculatedDimension();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetDimensionRange through the GetDimensionRange operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetUsedRangeResult GetDimensionRange(XlsxWorksheetPart data)
    {
        return data.GetDimensionRange();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ValidateMergedCells through the ValidateMergedCells operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCellsResult ValidateMergedCells(XlsxWorksheetPart data)
    {
        return data.ValidateMergedCells();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.MergeCells through the MergeCells operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCellsResult MergeCells(XlsxWorksheetPart data, string reference)
    {
        return data.MergeCells(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.MergeCellsByCoordinates through the MergeCellsByCoordinates operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="firstRow">The first row value used by the operation.</param>
    /// <param name="firstColumn">The first column value used by the operation.</param>
    /// <param name="lastRow">The last row value used by the operation.</param>
    /// <param name="lastColumn">The last column value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCellsResult MergeCellsByCoordinates(XlsxWorksheetPart data, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        return data.MergeCellsByCoordinates(firstRow, firstColumn, lastRow, lastColumn);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UnmergeCells through the UnmergeCells operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCellsResult UnmergeCells(XlsxWorksheetPart data, string reference)
    {
        return data.UnmergeCells(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.UnmergeCellsByCoordinates through the UnmergeCellsByCoordinates operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="firstRow">The first row value used by the operation.</param>
    /// <param name="firstColumn">The first column value used by the operation.</param>
    /// <param name="lastRow">The last row value used by the operation.</param>
    /// <param name="lastColumn">The last column value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCellsResult UnmergeCellsByCoordinates(XlsxWorksheetPart data, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        return data.UnmergeCellsByCoordinates(firstRow, firstColumn, lastRow, lastColumn);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.GetMergedCell through the GetMergedCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetMergeCell? GetMergedCell(XlsxWorksheetPart data, string reference)
    {
        return data.GetMergedCell(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.IsCellMerged through the IsCellMerged operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool IsCellMerged(XlsxWorksheetPart data, string reference)
    {
        return data.IsCellMerged(reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.FreezePanes through the FreezePanes operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPaneResult FreezePanes(XlsxWorksheetPart data, string topLeftCell)
    {
        return data.FreezePanes(topLeftCell);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.FreezePanesByCount through the FreezePanesByCount operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="frozenRows">The frozen rows value used by the operation.</param>
    /// <param name="frozenColumns">The frozen columns value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPaneResult FreezePanesByCount(XlsxWorksheetPart data, int frozenRows, int frozenColumns)
    {
        return data.FreezePanesByCount(frozenRows, frozenColumns);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SplitPanes through the SplitPanes operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="xSplit">The x split value used by the operation.</param>
    /// <param name="ySplit">The y split value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPaneResult SplitPanes(XlsxWorksheetPart data, double xSplit, double ySplit)
    {
        return data.SplitPanes(xSplit, ySplit);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.SplitPanesAtCell through the SplitPanesAtCell operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="xSplit">The x split value used by the operation.</param>
    /// <param name="ySplit">The y split value used by the operation.</param>
    /// <param name="topLeftCell">The top left cell value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPaneResult SplitPanesAtCell(XlsxWorksheetPart data, double xSplit, double ySplit, string topLeftCell)
    {
        return data.SplitPanesAtCell(xSplit, ySplit, topLeftCell);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ClearPanes through the ClearPanes operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPaneResult ClearPanes(XlsxWorksheetPart data)
    {
        return data.ClearPanes();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddComment through the AddComment operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="cellAddress">The cell address value used by the operation.</param>
    /// <param name="author">The author value used by the operation.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCommentResult AddComment(XlsxWorksheetPart data, string cellAddress, string author, string text)
    {
        return data.AddComment(cellAddress, author, text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddCommentWithVisibility through the AddCommentWithVisibility operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="cellAddress">The cell address value used by the operation.</param>
    /// <param name="author">The author value used by the operation.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <param name="visible">The visible value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetCommentResult AddCommentWithVisibility(XlsxWorksheetPart data, string cellAddress, string author, string text, bool visible)
    {
        return data.AddCommentWithVisibility(cellAddress, author, text, visible);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddHyperlink through the AddHyperlink operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="target">The target value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult AddHyperlink(XlsxWorksheetPart data, string reference, string target)
    {
        return data.AddHyperlink(reference, target);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddHyperlinkWithDisplay through the AddHyperlinkWithDisplay operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="target">The target value used by the operation.</param>
    /// <param name="display">The display value used by the operation.</param>
    /// <param name="tooltip">The tooltip value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult AddHyperlinkWithDisplay(XlsxWorksheetPart data, string reference, string target, string display, string tooltip)
    {
        return data.AddHyperlinkWithDisplay(reference, target, display, tooltip);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddExternalHyperlink through the AddExternalHyperlink operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="target">The target value used by the operation.</param>
    /// <param name="display">The display value used by the operation.</param>
    /// <param name="tooltip">The tooltip value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult AddExternalHyperlink(XlsxWorksheetPart data, string reference, string target, string display, string tooltip)
    {
        return data.AddExternalHyperlink(reference, target, display, tooltip);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.AddInternalHyperlink through the AddInternalHyperlink operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="location">The location value used by the operation.</param>
    /// <param name="display">The display value used by the operation.</param>
    /// <param name="tooltip">The tooltip value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult AddInternalHyperlink(XlsxWorksheetPart data, string reference, string location, string display, string tooltip)
    {
        return data.AddInternalHyperlink(reference, location, display, tooltip);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ResolveHyperlinkRelationships through the ResolveHyperlinkRelationships operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult ResolveHyperlinkRelationships(XlsxWorksheetPart data, XlsxRelationshipsPart relationships)
    {
        return data.ResolveHyperlinkRelationships(relationships);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ResolveDrawingRelationship through the ResolveDrawingRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSheetDrawingRelationshipResult ResolveDrawingRelationship(XlsxWorksheetPart data, XlsxRelationshipsPart relationships)
    {
        return data.ResolveDrawingRelationship(relationships);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ResolvePivotTableRelationships through the ResolvePivotTableRelationships operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetPivotTableRelationshipResult ResolvePivotTableRelationships(XlsxWorksheetPart data, XlsxRelationshipsPart relationships)
    {
        return data.ResolvePivotTableRelationships(relationships);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CreateHyperlinkRelationships through the CreateHyperlinkRelationships operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult CreateHyperlinkRelationships(XlsxWorksheetPart data)
    {
        return data.CreateHyperlinkRelationships();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.CreateHyperlinkRelationshipsForPartPath through the CreateHyperlinkRelationshipsForPartPath operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet part data to operate on.</param>
    /// <param name="worksheetPartPath">The worksheet part path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetHyperlinkResult CreateHyperlinkRelationshipsForPartPath(XlsxWorksheetPart data, string worksheetPartPath)
    {
        return data.CreateHyperlinkRelationshipsForPartPath(worksheetPartPath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetPart"/>.ResolveDrawingRelationship through the ResolveDrawingRelationshipResult operation.
    /// </summary>
    /// <param name="result">The result value used by the operation.</param>
    /// <param name="drawing">The drawing value used by the operation.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <param name="ownerName">The owner name value used by the operation.</param>
    public void ResolveDrawingRelationshipResult(XlsxSheetDrawingRelationshipResult result, XlsxSheetDrawingReference drawing, XlsxRelationshipsPart relationships, string ownerName)
    {
        XlsxWorksheetPart.ResolveDrawingRelationship(result, drawing, relationships, ownerName);
    }
}
