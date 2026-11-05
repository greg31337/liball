using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorkbookTemplate"/> data.
/// </summary>
public interface IXlsxWorkbookTemplateOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.OpenBytes through the OpenBytes operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookTemplateOpenResult OpenBytes(byte[] bytes);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.OpenFile through the OpenFile operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookTemplateOpenResult OpenFile(string filePath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SetNamedRange through the SetNamedRange operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookTemplateEditResult SetNamedRange(XlsxWorkbookTemplate data, string name, string reference);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SetLocalNamedRange through the SetLocalNamedRange operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookTemplateEditResult SetLocalNamedRange(XlsxWorkbookTemplate data, string name, int localSheetId, string reference);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.ResizeTable through the ResizeTable operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookTemplateEditResult ResizeTable(XlsxWorkbookTemplate data, string tableName, string reference);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SaveAsBytes through the SaveAsBytes operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveAsBytes(XlsxWorkbookTemplate data);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SaveAsFile through the SaveAsFile operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookSaveResult SaveAsFile(XlsxWorkbookTemplate data, string filePath);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.GetOrCreateDefinedName through the GetOrCreateDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="local">The local value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookDefinedName GetOrCreateDefinedName(XlsxWorkbookTemplate data, string name, bool local, int localSheetId);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.FindTableUnknownPart through the FindTableUnknownPart operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="result">The result value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxUnknownPart? FindTableUnknownPart(XlsxWorkbookTemplate data, string tableName, XlsxWorkbookTemplateEditResult result);
}
