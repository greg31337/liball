using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides typed pivot-cache and advanced pivot-table operations.
/// </summary>
public class XlsxPivotService : IXlsxPivotService
{
    /// <summary>Initializes a pivot service.</summary>
    public XlsxPivotService()
    {
    }

    /// <summary>Reads typed pivot-cache records.</summary>
    /// <param name="partPath">The records part path.</param>
    /// <param name="xml">The records XML.</param>
    /// <returns>The typed records part.</returns>
    public XlsxPivotCacheRecordsPart ReadRecords(string partPath, string xml)
    {
        XlsxPivotCacheRecordsPart part = new XlsxPivotCacheRecordsPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.RawXml = XlsxCellValueConverter.NormalizeText(xml);
        if (part.RawXml.Length == 0)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheRecords.missingRoot", "Pivot cache records XML must contain a pivotCacheRecords root element."));
            return part;
        }
        XlsxPivotCacheDefinitionPart diagnostics = new XlsxPivotCacheDefinitionPart();
        diagnostics.PartPath = part.PartPath;
        byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
        bool sawRoot = false;
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && !sawRoot)
                    {
                        sawRoot = true;
                        if (reader.LocalName != "pivotCacheRecords" || !XlsxPivotCacheDefinitionReader.IsSpreadsheetNamespace(reader.NamespaceURI))
                        {
                            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheRecords.invalidRoot", "Pivot cache records root must be pivotCacheRecords in a SpreadsheetML namespace."));
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "r" && XlsxPivotCacheDefinitionReader.IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        string recordXml = reader.ReadOuterXml();
                        part.Records.Add(ReadRecord(diagnostics, recordXml));
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        foreach (XlsxDiagnostic diagnostic in diagnostics.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
        return part;
    }

    /// <summary>Writes typed pivot-cache records.</summary>
    /// <param name="part">The records part.</param>
    /// <returns>The records write result.</returns>
    public XlsxPivotCacheRecordsWriteResult WriteRecords(XlsxPivotCacheRecordsPart? part)
    {
        XlsxPivotCacheRecordsWriteResult result = new XlsxPivotCacheRecordsWriteResult();
        if (part == null)
        {
            result.AddError("pivotCacheRecordsWriter.partNull", "Pivot cache records part must not be null.");
            return result;
        }
        if (part.Records == null)
        {
            result.AddError("pivotCacheRecordsWriter.recordsNull", "Pivot cache record list must not be null.");
            return result;
        }
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
            {
                writer.WriteStartElement("", "pivotCacheRecords", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", part.Records.Count.ToString(CultureInfo.InvariantCulture));
                foreach (XlsxPivotCacheRecord record in part.Records)
                {
                    writer.WriteStartElement("", "r", XlsxNamespaces.Spreadsheet);
                    if (record != null && record.Values != null)
                    {
                        foreach (XlsxPivotCacheValue value in record.Values)
                        {
                            XlsxPivotCacheDefinitionWriter.WriteValue(writer, value);
                        }
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            result.Xml = Encoding.UTF8.GetString(stream.ToArray());
        }
        return result;
    }

    /// <summary>Resolves the pivot-cache records relationship.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="relationships">The cache relationships.</param>
    /// <returns>The relationship result.</returns>
    public XlsxPivotCacheRelationshipResult ResolveCacheRelationships(XlsxPivotCacheDefinitionPart cache, XlsxRelationshipsPart? relationships)
    {
        XlsxPivotCacheRelationshipResult result = new XlsxPivotCacheRelationshipResult();
        if (cache == null)
        {
            result.AddError("pivotCacheRelationship.cacheNull", "Relationship resolution requires a pivot cache definition.");
            return result;
        }
        if (relationships == null || relationships.Relationships == null)
        {
            result.AddError("pivotCacheRelationship.relationshipsMissing", "Pivot cache relationships must be supplied.");
            return result;
        }
        XlsxRelationship? matched = null;
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship != null && relationship.Type == XlsxRelationshipTypes.PivotCacheRecords && (cache.RecordsRelationshipId.Length == 0 || relationship.Id == cache.RecordsRelationshipId))
            {
                matched = relationship;
                break;
            }
        }
        if (matched == null)
        {
            result.AddError("pivotCacheRelationship.recordsMissing", "Pivot cache relationships do not contain the records relationship.");
            return result;
        }
        cache.RecordsRelationship = matched;
        cache.HasRecordsRelationship = true;
        cache.RecordsRelationshipId = XlsxCellValueConverter.NormalizeText(matched.Id);
        cache.RecordsTargetPath = XlsxCellValueConverter.NormalizeText(matched.TargetPath);
        result.Relationship = matched;
        result.HasRelationship = true;
        return result;
    }

    /// <summary>Groups a pivot-cache field into numeric intervals.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The cache field name.</param>
    /// <param name="start">The first numeric boundary.</param>
    /// <param name="end">The last numeric boundary.</param>
    /// <param name="interval">The positive group interval.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult GroupNumericField(XlsxPivotCacheDefinitionPart cache, string fieldName, double start, double end, double interval)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        XlsxPivotCacheField? field = FindCacheField(cache, fieldName);
        if (field == null)
        {
            result.AddError("pivotCacheGrouping.fieldNotFound", "Pivot cache field was not found.");
        }
        if (interval <= 0 || end <= start)
        {
            result.AddError("pivotCacheGrouping.numericBoundsInvalid", "Numeric grouping requires an increasing range and a positive interval.");
        }
        if (!result.IsValid || field == null)
        {
            return result;
        }
        XlsxPivotCacheGrouping grouping = new XlsxPivotCacheGrouping();
        grouping.StartNumber = start;
        grouping.EndNumber = end;
        grouping.Interval = interval;
        field.Grouping = grouping;
        field.HasGrouping = true;
        cache.RawXml = "";
        return result;
    }

    /// <summary>Groups a pivot-cache field by a date unit.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The cache field name.</param>
    /// <param name="start">The first date boundary.</param>
    /// <param name="end">The last date boundary.</param>
    /// <param name="groupBy">The date grouping unit.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult GroupDateField(XlsxPivotCacheDefinitionPart cache, string fieldName, System.DateTime start, System.DateTime end, XlsxPivotDateGroupBy groupBy)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        XlsxPivotCacheField? field = FindCacheField(cache, fieldName);
        if (field == null)
        {
            result.AddError("pivotCacheGrouping.fieldNotFound", "Pivot cache field was not found.");
        }
        if (end <= start || groupBy < XlsxPivotDateGroupBy.Seconds || groupBy > XlsxPivotDateGroupBy.Years)
        {
            result.AddError("pivotCacheGrouping.dateBoundsInvalid", "Date grouping requires an increasing range and a supported date unit.");
        }
        if (!result.IsValid || field == null)
        {
            return result;
        }
        XlsxPivotCacheGrouping grouping = new XlsxPivotCacheGrouping();
        grouping.DateGrouping = true;
        grouping.DateGroupBy = groupBy;
        grouping.StartDate = start;
        grouping.EndDate = end;
        field.Grouping = grouping;
        field.HasGrouping = true;
        cache.RawXml = "";
        return result;
    }

    /// <summary>Adds a calculated item to a pivot cache.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="fieldName">The owning field name.</param>
    /// <param name="itemName">The calculated item name.</param>
    /// <param name="formula">The calculated item formula.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult AddCalculatedItem(XlsxPivotCacheDefinitionPart cache, string fieldName, string itemName, string formula)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        int fieldIndex = FindCacheFieldIndex(cache, fieldName);
        string name = XlsxCellValueConverter.NormalizeText(itemName);
        string formulaText = XlsxCellValueConverter.NormalizeText(formula);
        if (fieldIndex < 0) result.AddError("pivotCacheCalculatedItem.fieldNotFound", "Calculated item field was not found.");
        if (name.Length == 0) result.AddError("pivotCacheCalculatedItem.nameMissing", "Calculated item name must not be empty.");
        if (formulaText.Length == 0) result.AddError("pivotCacheCalculatedItem.formulaMissing", "Calculated item formula must not be empty.");
        if (!result.IsValid) return result;
        XlsxPivotCacheCalculatedItem item = new XlsxPivotCacheCalculatedItem();
        item.FieldIndex = fieldIndex;
        item.HasFieldIndex = true;
        item.Name = name;
        item.Formula = formulaText;
        cache.CalculatedItems.Add(item);
        cache.RawXml = "";
        return result;
    }

    /// <summary>Sets pivot-cache refresh settings.</summary>
    /// <param name="cache">The pivot-cache definition.</param>
    /// <param name="settings">The refresh settings.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult SetCacheRefresh(XlsxPivotCacheDefinitionPart cache, XlsxPivotCacheRefreshSettings? settings)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        if (cache == null) result.AddError("pivotCacheRefresh.cacheNull", "Refresh settings require a pivot cache definition.");
        if (settings == null) result.AddError("pivotCacheRefresh.settingsNull", "Pivot cache refresh settings must not be null.");
        if (!result.IsValid || cache == null || settings == null) return result;
        cache.RefreshSettings = ClonePivotRefreshSettings(settings);
        cache.RawXml = "";
        return result;
    }

    /// <summary>Adds a typed label filter.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="fieldName">The filtered field name.</param>
    /// <param name="kind">The typed label filter kind.</param>
    /// <param name="value">The label operand.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult AddLabelFilter(XlsxPivotTableDefinitionPart pivotTable, string fieldName, XlsxPivotFilterKind kind, string value)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        if (kind < XlsxPivotFilterKind.LabelEquals || kind > XlsxPivotFilterKind.LabelLessThan)
        {
            result.AddError("pivotTableFilter.labelKindInvalid", "The selected filter kind is not a label filter.");
            return result;
        }
        string operand = XlsxCellValueConverter.NormalizeText(value);
        if (operand.Length == 0)
        {
            result.AddError("pivotTableFilter.labelValueMissing", "Label filter value must not be empty.");
            return result;
        }
        result = pivotTable.AddFilter(fieldName, GetFilterToken(kind), "");
        if (result.IsValid)
        {
            XlsxPivotTableFilter filter = pivotTable.Filters[pivotTable.Filters.Count - 1];
            filter.FilterKind = kind;
            filter.HasFilterKind = true;
            filter.LabelValue = operand;
            pivotTable.RebuildRawXmlAfterOperation(result);
        }
        return result;
    }

    /// <summary>Adds a typed value filter.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="fieldName">The filtered field name.</param>
    /// <param name="dataFieldName">The summarized data-field name.</param>
    /// <param name="kind">The typed value filter kind.</param>
    /// <param name="value">The numeric operand.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult AddValueFilter(XlsxPivotTableDefinitionPart pivotTable, string fieldName, string dataFieldName, XlsxPivotFilterKind kind, double value)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        int dataIndex = FindDataFieldIndex(pivotTable, dataFieldName);
        if (kind < XlsxPivotFilterKind.ValueEquals || kind > XlsxPivotFilterKind.ValueLessThanOrEqual)
        {
            result.AddError("pivotTableFilter.valueKindInvalid", "The selected filter kind is not a value filter.");
        }
        if (dataIndex < 0)
        {
            result.AddError("pivotTableFilter.dataFieldNotFound", "Value filter data field was not found.");
        }
        if (!result.IsValid) return result;
        result = pivotTable.AddFilter(fieldName, GetFilterToken(kind), "");
        if (result.IsValid)
        {
            XlsxPivotTableFilter filter = pivotTable.Filters[pivotTable.Filters.Count - 1];
            filter.FilterKind = kind;
            filter.HasFilterKind = true;
            filter.NumericValue = value;
            filter.DataFieldIndex = dataIndex;
            filter.HasDataFieldIndex = true;
            pivotTable.RebuildRawXmlAfterOperation(result);
        }
        return result;
    }

    /// <summary>Sets a typed data-field summary function.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="dataFieldName">The data-field display name.</param>
    /// <param name="function">The typed summary function.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult SetSummaryFunction(XlsxPivotTableDefinitionPart pivotTable, string dataFieldName, XlsxPivotSummaryFunction function)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        int index = FindDataFieldIndex(pivotTable, dataFieldName);
        if (index < 0) result.AddError("pivotTableSummary.dataFieldNotFound", "Pivot data field was not found.");
        if (function < XlsxPivotSummaryFunction.Sum || function > XlsxPivotSummaryFunction.PopulationVariance) result.AddError("pivotTableSummary.functionInvalid", "Pivot summary function is not supported.");
        if (!result.IsValid) return result;
        XlsxPivotTableDataField field = pivotTable.DataFields[index];
        field.SummaryFunction = function;
        field.HasSummaryFunction = true;
        field.Subtotal = GetSummaryToken(function);
        pivotTable.RebuildRawXmlAfterOperation(result);
        return result;
    }

    /// <summary>Adds a pivot format for a typed area.</summary>
    /// <param name="pivotTable">The pivot-table definition.</param>
    /// <param name="area">The formatted pivot area.</param>
    /// <param name="differentialFormatId">The differential format identifier.</param>
    /// <param name="action">The format action token.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult AddFormat(XlsxPivotTableDefinitionPart pivotTable, XlsxPivotTableArea? area, uint differentialFormatId, string action)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        if (pivotTable == null || pivotTable.Formats == null) result.AddError("pivotTableFormat.tableMissing", "Pivot format requires a pivot table with format storage.");
        if (area == null) result.AddError("pivotTableFormat.areaNull", "Pivot format area must not be null.");
        if (!result.IsValid || pivotTable == null || pivotTable.Formats == null || area == null) return result;
        XlsxPivotTableFormat format = new XlsxPivotTableFormat();
        format.Action = XlsxCellValueConverter.NormalizeText(action);
        format.DifferentialFormatId = differentialFormatId;
        format.HasDifferentialFormatId = true;
        format.Area = ClonePivotFormatArea(area);
        format.HasArea = true;
        pivotTable.Formats.Add(format);
        pivotTable.RebuildRawXmlAfterOperation(result);
        return result;
    }

    internal static XlsxPivotCacheRefreshSettings ClonePivotRefreshSettings(XlsxPivotCacheRefreshSettings source)
    {
        XlsxPivotCacheRefreshSettings clone = new XlsxPivotCacheRefreshSettings();
        clone.SaveData = source.SaveData;
        clone.HasSaveData = source.HasSaveData;
        clone.RefreshOnLoad = source.RefreshOnLoad;
        clone.HasRefreshOnLoad = source.HasRefreshOnLoad;
        clone.EnableRefresh = source.EnableRefresh;
        clone.HasEnableRefresh = source.HasEnableRefresh;
        clone.BackgroundQuery = source.BackgroundQuery;
        clone.HasBackgroundQuery = source.HasBackgroundQuery;
        clone.UpgradeOnRefresh = source.UpgradeOnRefresh;
        clone.HasUpgradeOnRefresh = source.HasUpgradeOnRefresh;
        clone.CreatedVersion = source.CreatedVersion;
        clone.HasCreatedVersion = source.HasCreatedVersion;
        clone.RefreshedVersion = source.RefreshedVersion;
        clone.HasRefreshedVersion = source.HasRefreshedVersion;
        clone.MinimumRefreshableVersion = source.MinimumRefreshableVersion;
        clone.HasMinimumRefreshableVersion = source.HasMinimumRefreshableVersion;
        clone.MissingItemsLimit = source.MissingItemsLimit;
        clone.HasMissingItemsLimit = source.HasMissingItemsLimit;
        clone.RefreshedDate = source.RefreshedDate;
        clone.HasRefreshedDate = source.HasRefreshedDate;
        return clone;
    }

    internal static XlsxPivotTableArea ClonePivotFormatArea(XlsxPivotTableArea source)
    {
        XlsxPivotTableArea clone = new XlsxPivotTableArea();
        clone.Type = XlsxCellValueConverter.NormalizeText(source.Type);
        clone.FieldIndex = source.FieldIndex;
        clone.HasFieldIndex = source.HasFieldIndex;
        clone.Axis = XlsxCellValueConverter.NormalizeText(source.Axis);
        clone.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        foreach (XlsxPivotTableAreaReference reference in source.References)
        {
            clone.References.Add(ClonePivotAreaReference(reference));
        }
        return clone;
    }

    internal static XlsxPivotTableAreaReference ClonePivotAreaReference(XlsxPivotTableAreaReference source)
    {
        XlsxPivotTableAreaReference clone = new XlsxPivotTableAreaReference();
        if (source == null)
        {
            return clone;
        }
        clone.FieldIndex = source.FieldIndex;
        clone.HasFieldIndex = source.HasFieldIndex;
        clone.Count = source.Count;
        clone.HasCount = source.HasCount;
        clone.Selected = source.Selected;
        clone.HasSelected = source.HasSelected;
        clone.ByPosition = source.ByPosition;
        clone.HasByPosition = source.HasByPosition;
        clone.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        return clone;
    }

    /// <summary>Adds a reference to a pivot area.</summary>
    /// <param name="area">The pivot area.</param>
    /// <param name="fieldIndex">The referenced field index.</param>
    /// <param name="selected">Whether the reference is selected.</param>
    /// <param name="byPosition">Whether the reference is positional.</param>
    /// <returns>The operation result.</returns>
    public XlsxPivotTableOperationResult AddAreaReference(XlsxPivotTableArea? area, int fieldIndex, bool selected, bool byPosition)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        if (area == null || area.References == null) result.AddError("pivotTableArea.areaNull", "Pivot area reference requires an area with reference storage.");
        if (fieldIndex < 0) result.AddError("pivotTableArea.fieldIndexInvalid", "Pivot area field index must not be negative.");
        if (!result.IsValid || area == null || area.References == null) return result;
        XlsxPivotTableAreaReference reference = new XlsxPivotTableAreaReference();
        reference.FieldIndex = fieldIndex;
        reference.HasFieldIndex = true;
        reference.Selected = selected;
        reference.HasSelected = true;
        reference.ByPosition = byPosition;
        reference.HasByPosition = true;
        reference.Count = 1;
        reference.HasCount = true;
        area.References.Add(reference);
        return result;
    }

    internal static XlsxPivotCacheRecord ReadRecord(XlsxPivotCacheDefinitionPart diagnostics, string xml)
    {
        XlsxPivotCacheRecord record = new XlsxPivotCacheRecord();
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                {
                    record.Values.Add(XlsxPivotCacheDefinitionReader.ReadValue(diagnostics, reader));
                    reader.ReadOuterXml();
                    continue;
                }
                reader.Read();
            }
        }
        return record;
    }

    internal static XlsxPivotCacheField? FindCacheField(XlsxPivotCacheDefinitionPart cache, string name)
    {
        int index = FindCacheFieldIndex(cache, name);
        if (index < 0) return null;
        return cache.Fields[index];
    }

    internal static int FindCacheFieldIndex(XlsxPivotCacheDefinitionPart cache, string name)
    {
        if (cache == null || cache.Fields == null) return -1;
        string normalized = XlsxCellValueConverter.NormalizeText(name);
        int index = 0;
        foreach (XlsxPivotCacheField field in cache.Fields)
        {
            if (field != null && field.Name == normalized) return index;
            index = index + 1;
        }
        return -1;
    }

    internal static int FindDataFieldIndex(XlsxPivotTableDefinitionPart pivotTable, string name)
    {
        if (pivotTable == null || pivotTable.DataFields == null) return -1;
        string normalized = XlsxCellValueConverter.NormalizeText(name);
        int index = 0;
        foreach (XlsxPivotTableDataField field in pivotTable.DataFields)
        {
            if (field != null && field.Name == normalized) return index;
            index = index + 1;
        }
        return -1;
    }

    internal static string GetSummaryToken(XlsxPivotSummaryFunction function)
    {
        if (function == XlsxPivotSummaryFunction.Count) return "count";
        if (function == XlsxPivotSummaryFunction.Average) return "average";
        if (function == XlsxPivotSummaryFunction.Maximum) return "max";
        if (function == XlsxPivotSummaryFunction.Minimum) return "min";
        if (function == XlsxPivotSummaryFunction.Product) return "product";
        if (function == XlsxPivotSummaryFunction.CountNumbers) return "countNums";
        if (function == XlsxPivotSummaryFunction.StandardDeviation) return "stdDev";
        if (function == XlsxPivotSummaryFunction.PopulationStandardDeviation) return "stdDevP";
        if (function == XlsxPivotSummaryFunction.Variance) return "var";
        if (function == XlsxPivotSummaryFunction.PopulationVariance) return "varP";
        return "sum";
    }

    internal static XlsxPivotSummaryFunction ParseSummaryFunction(string token)
    {
        if (token == "count") return XlsxPivotSummaryFunction.Count;
        if (token == "average") return XlsxPivotSummaryFunction.Average;
        if (token == "max") return XlsxPivotSummaryFunction.Maximum;
        if (token == "min") return XlsxPivotSummaryFunction.Minimum;
        if (token == "product") return XlsxPivotSummaryFunction.Product;
        if (token == "countNums") return XlsxPivotSummaryFunction.CountNumbers;
        if (token == "stdDev") return XlsxPivotSummaryFunction.StandardDeviation;
        if (token == "stdDevP") return XlsxPivotSummaryFunction.PopulationStandardDeviation;
        if (token == "var") return XlsxPivotSummaryFunction.Variance;
        if (token == "varP") return XlsxPivotSummaryFunction.PopulationVariance;
        return XlsxPivotSummaryFunction.Sum;
    }

    internal static string GetFilterToken(XlsxPivotFilterKind kind)
    {
        if (kind == XlsxPivotFilterKind.LabelDoesNotEqual) return "captionNotEqual";
        if (kind == XlsxPivotFilterKind.LabelBeginsWith) return "captionBeginsWith";
        if (kind == XlsxPivotFilterKind.LabelEndsWith) return "captionEndsWith";
        if (kind == XlsxPivotFilterKind.LabelContains) return "captionContains";
        if (kind == XlsxPivotFilterKind.LabelGreaterThan) return "captionGreaterThan";
        if (kind == XlsxPivotFilterKind.LabelLessThan) return "captionLessThan";
        if (kind == XlsxPivotFilterKind.ValueEquals) return "valueEqual";
        if (kind == XlsxPivotFilterKind.ValueDoesNotEqual) return "valueNotEqual";
        if (kind == XlsxPivotFilterKind.ValueGreaterThan) return "valueGreaterThan";
        if (kind == XlsxPivotFilterKind.ValueLessThan) return "valueLessThan";
        if (kind == XlsxPivotFilterKind.ValueGreaterThanOrEqual) return "valueGreaterThanOrEqual";
        if (kind == XlsxPivotFilterKind.ValueLessThanOrEqual) return "valueLessThanOrEqual";
        return "captionEqual";
    }

    internal static bool IsKnownFilterToken(string token)
    {
        return token == "captionEqual" || token == "captionNotEqual" || token == "captionBeginsWith" || token == "captionEndsWith" || token == "captionContains" || token == "captionGreaterThan" || token == "captionLessThan" || token == "valueEqual" || token == "valueNotEqual" || token == "valueGreaterThan" || token == "valueLessThan" || token == "valueGreaterThanOrEqual" || token == "valueLessThanOrEqual";
    }

    internal static XlsxPivotFilterKind ParseFilterKind(string token)
    {
        if (token == "captionNotEqual") return XlsxPivotFilterKind.LabelDoesNotEqual;
        if (token == "captionBeginsWith") return XlsxPivotFilterKind.LabelBeginsWith;
        if (token == "captionEndsWith") return XlsxPivotFilterKind.LabelEndsWith;
        if (token == "captionContains") return XlsxPivotFilterKind.LabelContains;
        if (token == "captionGreaterThan") return XlsxPivotFilterKind.LabelGreaterThan;
        if (token == "captionLessThan") return XlsxPivotFilterKind.LabelLessThan;
        if (token == "valueEqual") return XlsxPivotFilterKind.ValueEquals;
        if (token == "valueNotEqual") return XlsxPivotFilterKind.ValueDoesNotEqual;
        if (token == "valueGreaterThan") return XlsxPivotFilterKind.ValueGreaterThan;
        if (token == "valueLessThan") return XlsxPivotFilterKind.ValueLessThan;
        if (token == "valueGreaterThanOrEqual") return XlsxPivotFilterKind.ValueGreaterThanOrEqual;
        if (token == "valueLessThanOrEqual") return XlsxPivotFilterKind.ValueLessThanOrEqual;
        return XlsxPivotFilterKind.LabelEquals;
    }
}
