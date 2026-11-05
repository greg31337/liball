using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxPivotTableDefinitionWriter"/>.
/// </summary>
public interface IXlsxPivotTableDefinitionWriter
{
    /// <summary>
    /// Creates a pivot table from the selected source kind.
    /// </summary>
    /// <param name="sourceKind">The source representation to use.</param>
    /// <param name="pivotTableName">The pivot table name value used by the operation.</param>
    /// <param name="cacheId">The cache id value used by the operation.</param>
    /// <param name="sourceSheetName">The source sheet name value used by the operation.</param>
    /// <param name="sourceReference">An A1-style source range reference.</param>
    /// <param name="tableName">The source table name value used by the operation.</param>
    /// <param name="destinationReference">An A1-style destination range reference.</param>
    /// <param name="fieldNames">The field names value used by the operation.</param>
    /// <returns>A result containing the generated pivot table and cache data.</returns>
    XlsxPivotTableCreateResult CreateFromSource(XlsxPivotTableSourceKind sourceKind, string pivotTableName, uint cacheId, string sourceSheetName, string sourceReference, string tableName, string destinationReference, List<string> fieldNames);
    /// <summary>
    /// Creates from range and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "pivotTableName">The pivot table name value used by the operation.</param>
    /// <param name = "cacheId">The cache id value used by the operation.</param>
    /// <param name = "sourceSheetName">The source sheet name value used by the operation.</param>
    /// <param name = "sourceReference">An A1-style cell or range reference.</param>
    /// <param name = "destinationReference">An A1-style cell or range reference.</param>
    /// <param name = "fieldNames">The field names value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxPivotTableCreateResult CreateFromRange(string pivotTableName, uint cacheId, string sourceSheetName, string sourceReference, string destinationReference, List<string> fieldNames);
    /// <summary>
    /// Creates from table and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "pivotTableName">The pivot table name value used by the operation.</param>
    /// <param name = "cacheId">The cache id value used by the operation.</param>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "destinationReference">An A1-style cell or range reference.</param>
    /// <param name = "fieldNames">The field names value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxPivotTableCreateResult CreateFromTable(string pivotTableName, uint cacheId, string tableName, string destinationReference, List<string> fieldNames);
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxPivotTableDefinitionWriteResult WriteString(XlsxPivotTableDefinitionPart part);
    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxPivotTableDefinitionWriteResult WriteBytes(XlsxPivotTableDefinitionPart part);
    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxPivotTableDefinitionWriteResult WriteStream(Stream stream, XlsxPivotTableDefinitionPart part);
}
