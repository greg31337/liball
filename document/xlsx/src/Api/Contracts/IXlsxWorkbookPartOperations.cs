using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorkbookPart"/> data.
/// </summary>
public interface IXlsxWorkbookPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetSheetByName through the GetSheetByName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSheet? GetSheetByName(XlsxWorkbookPart data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetDefinedName through the GetDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetDefinedName(XlsxWorkbookPart data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetDefinedNameForSheet through the GetDefinedNameForSheet operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetDefinedNameForSheet(XlsxWorkbookPart data, string name, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetGlobalDefinedName through the GetGlobalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetGlobalDefinedName(XlsxWorkbookPart data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetLocalDefinedName through the GetLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetPrintArea through the GetPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetPrintArea(XlsxWorkbookPart data, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetPrintTitles through the GetPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName? GetPrintTitles(XlsxWorkbookPart data, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.SetPrintArea through the SetPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult SetPrintArea(XlsxWorkbookPart data, int localSheetId, string sheetName, string reference);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.SetPrintTitles through the SetPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="rowReference">The row reference value used by the operation.</param>
    /// <param name="columnReference">The column reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult SetPrintTitles(XlsxWorkbookPart data, int localSheetId, string sheetName, string rowReference, string columnReference);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearPrintArea through the ClearPrintArea operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ClearPrintArea(XlsxWorkbookPart data, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearPrintTitles through the ClearPrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ClearPrintTitles(XlsxWorkbookPart data, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForRowInsert through the UpdateDefinedNameReferencesForRowInsert operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowInsert(XlsxWorkbookPart data, int localSheetId, int startRow, int rowCount);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForRowDelete through the UpdateDefinedNameReferencesForRowDelete operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startRow">The start row value used by the operation.</param>
    /// <param name="rowCount">The row count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowDelete(XlsxWorkbookPart data, int localSheetId, int startRow, int rowCount);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForColumnInsert through the UpdateDefinedNameReferencesForColumnInsert operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnInsert(XlsxWorkbookPart data, int localSheetId, int startColumn, int columnCount);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.UpdateDefinedNameReferencesForColumnDelete through the UpdateDefinedNameReferencesForColumnDelete operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="startColumn">The start column value used by the operation.</param>
    /// <param name="columnCount">The column count value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnDelete(XlsxWorkbookPart data, int localSheetId, int startColumn, int columnCount);
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
    XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferences(XlsxWorkbookPart data, int localSheetId, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ClearLocalDefinedName through the ClearLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ClearLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.GetOrCreateLocalDefinedName through the GetOrCreateLocalDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="result">The result value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName GetOrCreateLocalDefinedName(XlsxWorkbookPart data, string name, int localSheetId, XlsxWorkbookPrintDefinitionResult result);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidateLocalDefinedNameAccess through the ValidateLocalDefinedNameAccess operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ValidateLocalDefinedNameAccess(XlsxWorkbookPart data, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidatePrintDefinition through the ValidatePrintDefinition operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="missingReferenceCode">The missing reference code value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ValidatePrintDefinition(XlsxWorkbookPart data, int localSheetId, string sheetName, string reference, string missingReferenceCode);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookPart"/>.ValidatePrintTitles through the ValidatePrintTitles operation.
    /// </summary>
    /// <param name="data">The xlsx workbook part data to operate on.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="sheetName">The sheet name value used by the operation.</param>
    /// <param name="rowReference">The row reference value used by the operation.</param>
    /// <param name="columnReference">The column reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookPrintDefinitionResult ValidatePrintTitles(XlsxWorkbookPart data, int localSheetId, string sheetName, string rowReference, string columnReference);
}
