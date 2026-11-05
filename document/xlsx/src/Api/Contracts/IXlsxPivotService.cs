using System;

namespace xlsx;

/// <summary>
/// Defines typed pivot-cache and advanced pivot-table operations.
/// </summary>
public interface IXlsxPivotService
{
    /// <summary>Reads typed pivot-cache records.</summary>
    /// <param name="partPath">The records part path.</param>
    /// <param name="xml">The records XML.</param>
    /// <returns>The typed records part.</returns>
    XlsxPivotCacheRecordsPart ReadRecords(string partPath, string xml);
    /// <summary>Writes typed pivot-cache records.</summary>
    /// <param name="part">The records part.</param>
    /// <returns>The records write result.</returns>
    XlsxPivotCacheRecordsWriteResult WriteRecords(XlsxPivotCacheRecordsPart? part);
    /// <summary>Resolves the pivot-cache records relationship.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="relationships">The cache relationships.</param>
    /// <returns>The relationship result.</returns>
    XlsxPivotCacheRelationshipResult ResolveCacheRelationships(XlsxPivotCacheDefinitionPart cache, XlsxRelationshipsPart? relationships);
    /// <summary>Groups a pivot-cache field into numeric intervals.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The cache field name.</param>
    /// <param name="start">The first numeric boundary.</param>
    /// <param name="end">The last numeric boundary.</param>
    /// <param name="interval">The positive group interval.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult GroupNumericField(XlsxPivotCacheDefinitionPart cache, string fieldName, double start, double end, double interval);
    /// <summary>Groups a pivot-cache field by a date unit.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The cache field name.</param>
    /// <param name="start">The first date boundary.</param>
    /// <param name="end">The last date boundary.</param>
    /// <param name="groupBy">The date grouping unit.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult GroupDateField(XlsxPivotCacheDefinitionPart cache, string fieldName, System.DateTime start, System.DateTime end, XlsxPivotDateGroupBy groupBy);
    /// <summary>Adds a calculated item to a pivot cache.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The owning field name.</param>
    /// <param name="itemName">The calculated item name.</param>
    /// <param name="formula">The calculated item formula.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult AddCalculatedItem(XlsxPivotCacheDefinitionPart cache, string fieldName, string itemName, string formula);
    /// <summary>Sets pivot-cache refresh settings.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="settings">The refresh settings.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult SetCacheRefresh(XlsxPivotCacheDefinitionPart cache, XlsxPivotCacheRefreshSettings? settings);
    /// <summary>Adds a typed label filter.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="fieldName">The filtered field name.</param>
    /// <param name="kind">The typed label filter kind.</param>
    /// <param name="value">The label operand.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult AddLabelFilter(XlsxPivotTableDefinitionPart pivotTable, string fieldName, XlsxPivotFilterKind kind, string value);
    /// <summary>Adds a typed value filter.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="fieldName">The filtered field name.</param>
    /// <param name="dataFieldName">The summarized data-field name.</param>
    /// <param name="kind">The typed value filter kind.</param>
    /// <param name="value">The numeric operand.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult AddValueFilter(XlsxPivotTableDefinitionPart pivotTable, string fieldName, string dataFieldName, XlsxPivotFilterKind kind, double value);
    /// <summary>Sets a typed data-field summary function.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="dataFieldName">The data-field display name.</param>
    /// <param name="function">The typed summary function.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult SetSummaryFunction(XlsxPivotTableDefinitionPart pivotTable, string dataFieldName, XlsxPivotSummaryFunction function);
    /// <summary>Adds a pivot format for a typed area.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="area">The formatted pivot area.</param>
    /// <param name="differentialFormatId">The differential format identifier.</param>
    /// <param name="action">The format action token.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult AddFormat(XlsxPivotTableDefinitionPart pivotTable, XlsxPivotTableArea? area, uint differentialFormatId, string action);
    /// <summary>Adds a reference to a pivot area.</summary>
    /// <param name="area">The pivot area.</param>
    /// <param name="fieldIndex">The referenced field index.</param>
    /// <param name="selected">Whether the reference is selected.</param>
    /// <param name="byPosition">Whether the reference is positional.</param>
    /// <returns>The operation result.</returns>
    XlsxPivotTableOperationResult AddAreaReference(XlsxPivotTableArea? area, int fieldIndex, bool selected, bool byPosition);
}
