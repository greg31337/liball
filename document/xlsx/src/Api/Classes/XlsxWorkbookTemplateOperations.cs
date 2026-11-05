using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbookTemplate"/> data.
/// </summary>
public class XlsxWorkbookTemplateOperations : IXlsxWorkbookTemplateOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook template operations instance.
    /// </summary>
    public XlsxWorkbookTemplateOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.OpenBytes through the OpenBytes operation.
    /// </summary>
    /// <param name="bytes">The bytes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookTemplateOpenResult OpenBytes(byte[] bytes)
    {
        return XlsxWorkbookTemplate.OpenBytes(bytes);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.OpenFile through the OpenFile operation.
    /// </summary>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookTemplateOpenResult OpenFile(string filePath)
    {
        return XlsxWorkbookTemplate.OpenFile(filePath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SetNamedRange through the SetNamedRange operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookTemplateEditResult SetNamedRange(XlsxWorkbookTemplate data, string name, string reference)
    {
        return data.SetNamedRange(name, reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SetLocalNamedRange through the SetLocalNamedRange operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookTemplateEditResult SetLocalNamedRange(XlsxWorkbookTemplate data, string name, int localSheetId, string reference)
    {
        return data.SetLocalNamedRange(name, localSheetId, reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.ResizeTable through the ResizeTable operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookTemplateEditResult ResizeTable(XlsxWorkbookTemplate data, string tableName, string reference)
    {
        return data.ResizeTable(tableName, reference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SaveAsBytes through the SaveAsBytes operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveAsBytes(XlsxWorkbookTemplate data)
    {
        return data.SaveAsBytes();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.SaveAsFile through the SaveAsFile operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="filePath">The file path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookSaveResult SaveAsFile(XlsxWorkbookTemplate data, string filePath)
    {
        return data.SaveAsFile(filePath);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.GetOrCreateDefinedName through the GetOrCreateDefinedName operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="local">The local value used by the operation.</param>
    /// <param name="localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDefinedName GetOrCreateDefinedName(XlsxWorkbookTemplate data, string name, bool local, int localSheetId)
    {
        return data.GetOrCreateDefinedName(name, local, localSheetId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookTemplate"/>.FindTableUnknownPart through the FindTableUnknownPart operation.
    /// </summary>
    /// <param name="data">The xlsx workbook template data to operate on.</param>
    /// <param name="tableName">The table name value used by the operation.</param>
    /// <param name="result">The result value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxUnknownPart? FindTableUnknownPart(XlsxWorkbookTemplate data, string tableName, XlsxWorkbookTemplateEditResult result)
    {
        return data.FindTableUnknownPart(tableName, result);
    }
}
