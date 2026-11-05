using System;
using System.Data;

namespace xlsx;

/// <summary>
/// Provides high-level worksheet table conversion, projection, mapping, and transfer operations.
/// </summary>
public class XlsxTableService : IXlsxTableService
{
    /// <summary>
    /// Initializes a table service.
    /// </summary>
    public XlsxTableService()
    {
    }

    /// <summary>
    /// Converts a worksheet range to a table.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the range.</param>
    /// <param name="reference">The A1 range reference to convert.</param>
    /// <param name="tableName">The table name to create.</param>
    /// <param name="firstRowAsHeaders">Whether the first range row contains headers.</param>
    /// <returns>The table operation result.</returns>
    public XlsxWorksheetTableResult ConvertRange(XlsxWorksheetPart worksheet, string reference, string tableName, bool firstRowAsHeaders)
    {
        if (worksheet == null)
        {
            XlsxWorksheetTableResult result = new XlsxWorksheetTableResult();
            result.AddError("tableService.worksheetNull", "Table conversion requires a worksheet.");
            return result;
        }
        return worksheet.AddTableWithHeaderRow(reference, tableName, firstRowAsHeaders);
    }

    /// <summary>
    /// Projects table cells to typed row arrays.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to project.</param>
    /// <param name="includeHeaderRow">Whether to include the table header row.</param>
    /// <param name="includeTotalsRow">Whether to include the table totals row.</param>
    /// <returns>The typed row projection.</returns>
    public XlsxTableRowProjectionResult ProjectRows(XlsxWorksheetPart worksheet, string tableName, bool includeHeaderRow, bool includeTotalsRow)
    {
        XlsxTableRowProjectionResult result = new XlsxTableRowProjectionResult();
        if (worksheet == null)
        {
            result.AddError("tableService.worksheetNull", "Table projection requires a worksheet.");
            return result;
        }

        XlsxWorksheetTableResult tableResult = worksheet.ValidateExistingTable(tableName);
        CopyTableDiagnostics(result, tableResult);
        if (!result.IsValid || tableResult.Table == null)
        {
            return result;
        }

        XlsxTablePart table = tableResult.Table;
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetPart.NormalizeRangeBounds(range);
        int firstRow = range.First.Row;
        int lastRow = range.Last.Row;
        if (!includeHeaderRow && XlsxWorksheetPart.TableHasHeaderRow(table))
        {
            firstRow = firstRow + 1;
        }
        if (!includeTotalsRow && XlsxWorksheetPart.TableHasTotalsRow(table))
        {
            lastRow = lastRow - 1;
        }

        result.TableName = table.Name;
        result.Reference = table.Reference;
        result.ColumnCount = range.Last.Column - range.First.Column + 1;
        int row = firstRow;
        while (row <= lastRow)
        {
            XlsxCellValue[] values = new XlsxCellValue[result.ColumnCount];
            int offset = 0;
            while (offset < result.ColumnCount)
            {
                values[offset] = XlsxRangeService.CreateCellItem(worksheet, row, range.First.Column + offset).Value;
                offset = offset + 1;
            }
            result.Rows.Add(values);
            row = row + 1;
        }
        return result;
    }

    /// <summary>
    /// Opens a forward cursor over projected table rows.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to read.</param>
    /// <param name="includeHeaderRow">Whether to include the table header row.</param>
    /// <param name="includeTotalsRow">Whether to include the table totals row.</param>
    /// <returns>The opened row cursor.</returns>
    public XlsxTableRowCursor OpenRowCursor(XlsxWorksheetPart worksheet, string tableName, bool includeHeaderRow, bool includeTotalsRow)
    {
        XlsxTableRowProjectionResult projection = ProjectRows(worksheet, tableName, includeHeaderRow, includeTotalsRow);
        XlsxTableRowCursor cursor = new XlsxTableRowCursor();
        cursor.IsValid = projection.IsValid;
        cursor.TableName = projection.TableName;
        cursor.Rows = projection.Rows;
        foreach (XlsxDiagnostic diagnostic in projection.Validation.Diagnostics)
        {
            cursor.Validation.AddDiagnostic(diagnostic);
        }
        cursor.EndOfRows = false;
        return cursor;
    }

    /// <summary>
    /// Advances a table row cursor.
    /// </summary>
    /// <param name="cursor">The cursor to advance.</param>
    /// <returns><c>true</c> when a row was read; otherwise <c>false</c>.</returns>
    public bool ReadRow(XlsxTableRowCursor? cursor)
    {
        if (cursor == null)
        {
            return false;
        }
        return cursor.Read();
    }

    /// <summary>
    /// Resets a table row cursor to its initial position.
    /// </summary>
    /// <param name="cursor">The cursor to reset.</param>
    public void ResetRowCursor(XlsxTableRowCursor? cursor)
    {
        if (cursor != null)
        {
            cursor.Reset();
        }
    }

    /// <summary>
    /// Sets a typed totals-row function on a table column.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to change.</param>
    /// <param name="columnName">The table column name to change.</param>
    /// <param name="function">The typed total function.</param>
    /// <returns>The table operation result.</returns>
    public XlsxWorksheetTableResult SetTotalFunction(XlsxWorksheetPart worksheet, string tableName, string columnName, XlsxTableTotalFunction function)
    {
        if (worksheet == null)
        {
            XlsxWorksheetTableResult missing = new XlsxWorksheetTableResult();
            missing.AddError("tableService.worksheetNull", "Setting a table total function requires a worksheet.");
            return missing;
        }
        if (function < XlsxTableTotalFunction.None || function > XlsxTableTotalFunction.Custom)
        {
            XlsxWorksheetTableResult invalid = new XlsxWorksheetTableResult();
            invalid.AddError("tableService.totalFunctionInvalid", "The table total function is not supported.");
            return invalid;
        }

        string token = XlsxTablePartWriter.GetTotalFunctionToken(function);
        XlsxWorksheetTableResult result = worksheet.SetTableColumnTotalsRowFunction(tableName, columnName, token);
        if (result.IsValid && result.Column != null)
        {
            result.Column.TotalFunction = function;
            result.Column.HasTotalFunction = true;
            result.Column.TotalsRowFormula = "";
            result.Column.TotalsRowFormulaRawXml = "";
            if (function != XlsxTableTotalFunction.None)
            {
                result.Column.TotalsRowLabel = "";
            }
            if (result.Table != null)
            {
                worksheet.ApplyTableTotalsRowCells(result, result.Table);
            }
        }
        return result;
    }

    /// <summary>
    /// Sets typed XML mapping properties on a table column.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to change.</param>
    /// <param name="columnName">The table column name to change.</param>
    /// <param name="properties">The XML mapping properties.</param>
    /// <returns>The table operation result.</returns>
    public XlsxWorksheetTableResult SetXmlColumnMapping(XlsxWorksheetPart worksheet, string tableName, string columnName, XlsxTableXmlColumnProperties? properties)
    {
        if (worksheet == null)
        {
            XlsxWorksheetTableResult missing = new XlsxWorksheetTableResult();
            missing.AddError("tableService.worksheetNull", "Setting an XML column mapping requires a worksheet.");
            return missing;
        }
        XlsxWorksheetTableResult result = worksheet.ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }
        if (properties == null)
        {
            result.AddError("tableService.xmlMappingNull", "XML column mapping properties must not be null.");
        }
        else if (!properties.HasMapId)
        {
            result.AddError("tableService.xmlMapIdMissing", "XML column mapping properties must contain a map identifier.");
        }
        else if (properties.XPath == null || properties.XPath.Length == 0)
        {
            result.AddError("tableService.xmlXPathMissing", "XML column mapping properties must contain an XPath.");
        }
        else if (properties.XmlDataType == null || properties.XmlDataType.Length == 0)
        {
            result.AddError("tableService.xmlDataTypeMissing", "XML column mapping properties must contain an XML data type.");
        }
        if (!result.IsValid || properties == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Column.XmlColumnPropertiesRawXml = "";
        result.Column.XmlColumnProperties = CloneTableXmlMapping(properties);
        result.Column.HasXmlColumnProperties = true;
        return result;
    }

    internal static XlsxTableXmlColumnProperties CloneTableXmlMapping(XlsxTableXmlColumnProperties source)
    {
        XlsxTableXmlColumnProperties clone = new XlsxTableXmlColumnProperties();
        clone.MapId = source.MapId;
        clone.HasMapId = source.HasMapId;
        clone.XPath = XlsxCellValueConverter.NormalizeText(source.XPath);
        clone.XmlDataType = XlsxCellValueConverter.NormalizeText(source.XmlDataType);
        clone.Denormalized = source.Denormalized;
        clone.HasDenormalized = source.HasDenormalized;
        clone.ExtensionListRawXml = XlsxCellValueConverter.NormalizeText(source.ExtensionListRawXml);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneTableXmlAttribute(attribute));
        }
        foreach (string unknownXml in source.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }
        return clone;
    }

    internal static XlsxXmlAttribute CloneTableXmlAttribute(XlsxXmlAttribute source)
    {
        XlsxXmlAttribute clone = new XlsxXmlAttribute();
        if (source == null)
        {
            return clone;
        }
        clone.NamespaceUri = XlsxCellValueConverter.NormalizeText(source.NamespaceUri);
        clone.LocalName = XlsxCellValueConverter.NormalizeText(source.LocalName);
        clone.Prefix = XlsxCellValueConverter.NormalizeText(source.Prefix);
        clone.Value = XlsxCellValueConverter.NormalizeText(source.Value);
        return clone;
    }

    /// <summary>
    /// Imports a <see cref="DataTable"/> and converts the imported range to a worksheet table.
    /// </summary>
    /// <param name="worksheet">The destination worksheet.</param>
    /// <param name="topLeftCell">The top-left destination cell.</param>
    /// <param name="dataTable">The data table to import.</param>
    /// <param name="tableName">The worksheet table name to create.</param>
    /// <param name="includeHeaders">Whether to write data-table column names as headers.</param>
    /// <returns>The import result.</returns>
    public XlsxDataTableImportResult ImportDataTable(XlsxWorksheetPart worksheet, string topLeftCell, DataTable? dataTable, string tableName, bool includeHeaders)
    {
        if (worksheet == null)
        {
            XlsxDataTableImportResult result = new XlsxDataTableImportResult();
            result.AddError("tableService.worksheetNull", "DataTable import requires a worksheet.");
            return result;
        }
        return worksheet.ImportDataTableAsTableWithHeaders(topLeftCell, dataTable, tableName, includeHeaders);
    }

    /// <summary>
    /// Exports a worksheet table to a built-in <see cref="DataTable"/>.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to export.</param>
    /// <returns>The export result.</returns>
    public XlsxDataTableExportResult ExportDataTable(XlsxWorksheetPart worksheet, string tableName)
    {
        if (worksheet == null)
        {
            XlsxDataTableExportResult result = new XlsxDataTableExportResult();
            result.AddError("tableService.worksheetNull", "DataTable export requires a worksheet.");
            return result;
        }
        return worksheet.ExportTableToDataTable(tableName);
    }

    internal static void CopyTableDiagnostics(XlsxTableRowProjectionResult result, XlsxWorksheetTableResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
