using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbookPart"/> data.
/// </summary>
public class XlsxWorkbookPartOperations : IXlsxWorkbookPartOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook part operations instance.
    /// </summary>
    public XlsxWorkbookPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetSheetByName through the GetSheetByName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSheet? GetSheetByName(XlsxWorkbookPart data, string name)
    {
        return data.GetSheetByName(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetDefinedName through the GetDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetDefinedName(XlsxWorkbookPart data, string name)
    {
        return data.GetDefinedName(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetDefinedNameForSheet through the GetDefinedNameForSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetDefinedNameForSheet(XlsxWorkbookPart data, string name, int localSheetId)
    {
        return data.GetDefinedNameForSheet(name, localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetGlobalDefinedName through the GetGlobalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetGlobalDefinedName(XlsxWorkbookPart data, string name)
    {
        return data.GetGlobalDefinedName(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetLocalDefinedName through the GetLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId)
    {
        return data.GetLocalDefinedName(name, localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetPrintArea through the GetPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetPrintArea(XlsxWorkbookPart data, int localSheetId)
    {
        return data.GetPrintArea(localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetPrintTitles through the GetPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName? GetPrintTitles(XlsxWorkbookPart data, int localSheetId)
    {
        return data.GetPrintTitles(localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.SetPrintArea through the SetPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult SetPrintArea(XlsxWorkbookPart data, int localSheetId, string sheetName, string reference)
    {
        return data.SetPrintArea(localSheetId, sheetName, reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.SetPrintTitles through the SetPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="rowReference">The row reference value used by the operation.</param>
    /// <param name="columnReference">The column reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult SetPrintTitles(XlsxWorkbookPart data, int localSheetId, string sheetName, string rowReference, string columnReference)
    {
        return data.SetPrintTitles(localSheetId, sheetName, rowReference, columnReference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearPrintArea through the ClearPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ClearPrintArea(XlsxWorkbookPart data, int localSheetId)
    {
        return data.ClearPrintArea(localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearPrintTitles through the ClearPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ClearPrintTitles(XlsxWorkbookPart data, int localSheetId)
    {
        return data.ClearPrintTitles(localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForRowInsert through the UpdateDefinedNameReferencesForRowInsert operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowInsert(XlsxWorkbookPart data, int localSheetId, int startRow, int rowCount)
    {
        return data.UpdateDefinedNameReferencesForRowInsert(localSheetId, startRow, rowCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForRowDelete through the UpdateDefinedNameReferencesForRowDelete operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowDelete(XlsxWorkbookPart data, int localSheetId, int startRow, int rowCount)
    {
        return data.UpdateDefinedNameReferencesForRowDelete(localSheetId, startRow, rowCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForColumnInsert through the UpdateDefinedNameReferencesForColumnInsert operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnInsert(XlsxWorkbookPart data, int localSheetId, int startColumn, int columnCount)
    {
        return data.UpdateDefinedNameReferencesForColumnInsert(localSheetId, startColumn, columnCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForColumnDelete through the UpdateDefinedNameReferencesForColumnDelete operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnDelete(XlsxWorkbookPart data, int localSheetId, int startColumn, int columnCount)
    {
        return data.UpdateDefinedNameReferencesForColumnDelete(localSheetId, startColumn, columnCount);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferences through the UpdateDefinedNameReferences operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="kind">The kind value used by the operation.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="count">The count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferences(XlsxWorkbookPart data, int localSheetId, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count)
    {
        return data.UpdateDefinedNameReferences(localSheetId, kind, startRow, startColumn, count);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearLocalDefinedName through the ClearLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ClearLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId)
    {
        return data.ClearLocalDefinedName(name, localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetOrCreateLocalDefinedName through the GetOrCreateLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="result">The result value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName GetOrCreateLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId, XlsxWorkbookPrintDefinitionResult result)
    {
        return data.GetOrCreateLocalDefinedName(name, localSheetId, result);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidateLocalDefinedNameAccess through the ValidateLocalDefinedNameAccess operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ValidateLocalDefinedNameAccess(XlsxWorkbookPart data, int localSheetId)
    {
        return data.ValidateLocalDefinedNameAccess(localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidatePrintDefinition through the ValidatePrintDefinition operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="missingReferenceCode">The missing reference code value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ValidatePrintDefinition(XlsxWorkbookPart data, int localSheetId, string sheetName, string reference, string missingReferenceCode)
    {
        return data.ValidatePrintDefinition(localSheetId, sheetName, reference, missingReferenceCode);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidatePrintTitles through the ValidatePrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="rowReference">The row reference value used by the operation.</param>
    /// <param name="columnReference">The column reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookPrintDefinitionResult ValidatePrintTitles(XlsxWorkbookPart data, int localSheetId, string sheetName, string rowReference, string columnReference)
    {
        return data.ValidatePrintTitles(localSheetId, sheetName, rowReference, columnReference);
    }
}
