using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes auto filter data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxAutoFilterWriter : IXlsxAutoFilterWriter
{
    /// <summary>
    /// Initializes a new auto filter writer with its default state.
    /// </summary>
    public XlsxAutoFilterWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxAutoFilterWriteResult WriteString(XlsxAutoFilter filter)
    {
        XlsxAutoFilterWriteResult result = WriteBytes(filter);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxAutoFilterWriteResult WriteBytes(XlsxAutoFilter filter)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxAutoFilterWriteResult result = WriteStream(stream, filter);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxAutoFilterWriteResult WriteStream(Stream stream, XlsxAutoFilter filter)
    {
        XlsxAutoFilterWriteResult result = ValidateAutoFilter(filter);
        if (!result.IsValid)
        {
            return result;
        }

        if (filter.RawXml != null && filter.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(filter.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteAutoFilterElement(writer, filter);
            writer.WriteEndDocument();
        }

        return result;
    }

    /// <summary>
    /// Validates and writes sort state string in the requested representation.
    /// </summary>
    /// <param name = "sortState">The sort state value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxAutoFilterWriteResult WriteSortStateString(XlsxSortState sortState)
    {
        XlsxAutoFilterWriteResult result = WriteSortStateBytes(sortState);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    internal XlsxAutoFilterWriteResult WriteSortStateBytes(XlsxSortState sortState)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxAutoFilterWriteResult result = WriteSortStateStream(stream, sortState);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    internal XlsxAutoFilterWriteResult WriteSortStateStream(Stream stream, XlsxSortState sortState)
    {
        XlsxAutoFilterWriteResult result = new XlsxAutoFilterWriteResult();
        ValidateSortState(result, sortState, "sortState", "sortStateWriter");
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteSortStateElement(writer, XlsxNamespaces.Spreadsheet, sortState);
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxAutoFilterWriteResult ValidateAutoFilter(XlsxAutoFilter? filter)
    {
        XlsxAutoFilterWriteResult result = new XlsxAutoFilterWriteResult();
        if (filter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.filterNull", "Auto filter must not be null."));
            return result;
        }

        if (filter.RawXml != null && filter.RawXml.Length > 0)
        {
            return result;
        }

        if (filter.NamespaceUri == null || filter.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.namespaceMissing", "Auto filter must have a namespace URI."));
        }
        else if (!XlsxAutoFilterReader.IsSpreadsheetNamespace(filter.NamespaceUri))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.namespaceInvalid", "Auto filter namespace URI must be a SpreadsheetML namespace."));
        }

        if (filter.Reference == null || filter.Reference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.referenceMissing", "Auto filter must have a ref attribute."));
        }
        else
        {
            ValidateRange(result, filter.Reference, "autoFilter", "ref");
        }

        if (filter.FilterColumns == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.filterColumnsNull", "Auto filter column list must not be null."));
        }

        if (filter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.unknownAttributesNull", "Auto filter unknown attribute list must not be null."));
        }

        if (filter.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.unknownXmlNull", "Auto filter unknown XML list must not be null."));
        }

        if (filter.HasSortState)
        {
            ValidateSortState(result, filter.SortState, "sortState", "autoFilterWriter.sortState");
        }

        if (filter.FilterColumns != null)
        {
            int index = 0;
            foreach (XlsxAutoFilterColumn column in filter.FilterColumns)
            {
                ValidateFilterColumn(result, column, index);
                index = index + 1;
            }
        }

        return result;
    }

    internal static void ValidateFilterColumn(XlsxAutoFilterWriteResult result, XlsxAutoFilterColumn column, int index)
    {
        if (column == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.columnNull", "Auto filter column list must not contain null entries.");
            diagnostic.SetLocation("", "filterColumn", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (!column.HasColumnId)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.columnIdMissing", "Auto filter column must have a colId attribute.");
            diagnostic.SetLocation("", "filterColumn", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (column.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.columnUnknownAttributesNull", "Auto filter column unknown attribute list must not be null."));
        }

        if (column.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.columnUnknownXmlNull", "Auto filter column unknown XML list must not be null."));
        }

        if (column.HasValues)
        {
            ValidateValues(result, column.Values);
        }

        if (column.HasCustomFilters)
        {
            ValidateCustomFilters(result, column.CustomFilters);
        }

        if (column.HasDynamicFilter)
        {
            ValidateLeafUnknownAttributes(result, column.DynamicFilter, "dynamicFilter");
        }

        if (column.HasTop10)
        {
            ValidateLeafUnknownAttributes(result, column.Top10, "top10");
        }

        if (column.HasColorFilter)
        {
            ValidateLeafUnknownAttributes(result, column.ColorFilter, "colorFilter");
        }

        if (column.HasIconFilter)
        {
            ValidateLeafUnknownAttributes(result, column.IconFilter, "iconFilter");
        }
    }

    internal static void ValidateValues(XlsxAutoFilterWriteResult result, XlsxAutoFilterValues values)
    {
        if (values == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.valuesNull", "Auto filter values must not be null when present."));
            return;
        }

        if (values.Values == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.filterValuesNull", "Filter value list must not be null."));
        }
        else
        {
            foreach (string value in values.Values)
            {
                if (value == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.filterValueNull", "Filter value list must not contain null entries."));
                }
            }
        }

        if (values.DateGroupItems == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.dateGroupItemsNull", "Date group item list must not be null."));
        }
        else
        {
            foreach (XlsxDateGroupItem item in values.DateGroupItems)
            {
                if (item == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.dateGroupItemNull", "Date group item list must not contain null entries."));
                }
                else if (item.UnknownAttributes == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.dateGroupItemUnknownAttributesNull", "Date group item unknown attribute list must not be null."));
                }
            }
        }

        if (values.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.valuesUnknownAttributesNull", "Filter values unknown attribute list must not be null."));
        }

        if (values.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.valuesUnknownXmlNull", "Filter values unknown XML list must not be null."));
        }
    }

    internal static void ValidateCustomFilters(XlsxAutoFilterWriteResult result, XlsxCustomFilters customFilters)
    {
        if (customFilters == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFiltersNull", "Custom filters must not be null when present."));
            return;
        }

        if (customFilters.Filters == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFilterListNull", "Custom filter list must not be null."));
        }
        else
        {
            foreach (XlsxCustomFilter customFilter in customFilters.Filters)
            {
                if (customFilter == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFilterNull", "Custom filter list must not contain null entries."));
                }
                else if (customFilter.UnknownAttributes == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFilterUnknownAttributesNull", "Custom filter unknown attribute list must not be null."));
                }
            }
        }

        if (customFilters.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFiltersUnknownAttributesNull", "Custom filters unknown attribute list must not be null."));
        }

        if (customFilters.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.customFiltersUnknownXmlNull", "Custom filters unknown XML list must not be null."));
        }
    }

    internal static void ValidateLeafUnknownAttributes(XlsxAutoFilterWriteResult result, XlsxDynamicFilter filter, string elementName)
    {
        if (filter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.dynamicFilterNull", "Dynamic filter must not be null when present."));
        }
        else if (filter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.dynamicFilterUnknownAttributesNull", "Dynamic filter unknown attribute list must not be null."));
        }
    }

    internal static void ValidateLeafUnknownAttributes(XlsxAutoFilterWriteResult result, XlsxTop10Filter filter, string elementName)
    {
        if (filter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.top10Null", "Top10 filter must not be null when present."));
        }
        else if (filter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.top10UnknownAttributesNull", "Top10 filter unknown attribute list must not be null."));
        }
    }

    internal static void ValidateLeafUnknownAttributes(XlsxAutoFilterWriteResult result, XlsxColorFilter filter, string elementName)
    {
        if (filter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.colorFilterNull", "Color filter must not be null when present."));
        }
        else if (filter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.colorFilterUnknownAttributesNull", "Color filter unknown attribute list must not be null."));
        }
    }

    internal static void ValidateLeafUnknownAttributes(XlsxAutoFilterWriteResult result, XlsxIconFilter filter, string elementName)
    {
        if (filter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.iconFilterNull", "Icon filter must not be null when present."));
        }
        else if (filter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilterWriter.iconFilterUnknownAttributesNull", "Icon filter unknown attribute list must not be null."));
        }
    }

    internal static void ValidateSortState(XlsxAutoFilterWriteResult result, XlsxSortState sortState, string elementName, string codePrefix)
    {
        if (sortState == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".null", "Sort state must not be null when present."));
            return;
        }

        if (sortState.Reference == null || sortState.Reference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".referenceMissing", "Sort state must have a ref attribute."));
        }
        else
        {
            ValidateRange(result, sortState.Reference, elementName, "ref");
        }

        if (sortState.Conditions == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".conditionsNull", "Sort condition list must not be null."));
        }
        else
        {
            foreach (XlsxSortCondition condition in sortState.Conditions)
            {
                ValidateSortCondition(result, condition, codePrefix);
            }
        }

        if (sortState.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".unknownAttributesNull", "Sort state unknown attribute list must not be null."));
        }

        if (sortState.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".unknownXmlNull", "Sort state unknown XML list must not be null."));
        }
    }

    internal static void ValidateSortCondition(XlsxAutoFilterWriteResult result, XlsxSortCondition condition, string codePrefix)
    {
        if (condition == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".conditionNull", "Sort condition list must not contain null entries."));
            return;
        }

        if (condition.Reference == null || condition.Reference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".conditionReferenceMissing", "Sort condition must have a ref attribute."));
        }
        else
        {
            ValidateRange(result, condition.Reference, "sortCondition", "ref");
        }

        if (condition.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, codePrefix + ".conditionUnknownAttributesNull", "Sort condition unknown attribute list must not be null."));
        }
    }

    internal static void ValidateRange(XlsxAutoFilterWriteResult result, string reference, string elementName, string attributeName)
    {
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void WriteFilterColumn(XmlWriter writer, string namespaceUri, XlsxAutoFilterColumn column)
    {
        writer.WriteStartElement("", "filterColumn", namespaceUri);
        WriteOptionalUIntAttribute(writer, "colId", column.ColumnId, column.HasColumnId);
        WriteOptionalBoolAttribute(writer, "hiddenButton", column.HiddenButton, column.HasHiddenButton);
        WriteOptionalBoolAttribute(writer, "showButton", column.ShowButton, column.HasShowButton);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, column.UnknownAttributes);
        if (column.HasValues)
        {
            WriteValues(writer, namespaceUri, column.Values);
        }

        if (column.HasCustomFilters)
        {
            WriteCustomFilters(writer, namespaceUri, column.CustomFilters);
        }

        if (column.HasDynamicFilter)
        {
            WriteDynamicFilter(writer, namespaceUri, column.DynamicFilter);
        }

        if (column.HasTop10)
        {
            WriteTop10Filter(writer, namespaceUri, column.Top10);
        }

        if (column.HasColorFilter)
        {
            WriteColorFilter(writer, namespaceUri, column.ColorFilter);
        }

        if (column.HasIconFilter)
        {
            WriteIconFilter(writer, namespaceUri, column.IconFilter);
        }

        WriteRawXml(writer, column.ExtensionListRawXml);
        WriteUnknownXml(writer, column.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteAutoFilterElement(XmlWriter writer, XlsxAutoFilter? filter)
    {
        if (filter == null)
        {
            return;
        }

        if (filter.RawXml != null && filter.RawXml.Length > 0)
        {
            writer.WriteRaw(filter.RawXml);
            return;
        }

        writer.WriteStartElement("", "autoFilter", filter.NamespaceUri);
        WriteOptionalStringAttribute(writer, "ref", filter.Reference);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, filter.UnknownAttributes);
        foreach (XlsxAutoFilterColumn column in filter.FilterColumns)
        {
            WriteFilterColumn(writer, filter.NamespaceUri, column);
        }

        if (filter.HasSortState)
        {
            WriteSortStateElement(writer, filter.NamespaceUri, filter.SortState);
        }

        WriteRawXml(writer, filter.ExtensionListRawXml);
        WriteUnknownXml(writer, filter.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteValues(XmlWriter writer, string namespaceUri, XlsxAutoFilterValues values)
    {
        writer.WriteStartElement("", "filters", namespaceUri);
        WriteOptionalBoolAttribute(writer, "blank", values.Blank, values.HasBlank);
        WriteOptionalStringAttribute(writer, "calendarType", values.CalendarType);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, values.UnknownAttributes);
        foreach (string value in values.Values)
        {
            writer.WriteStartElement("", "filter", namespaceUri);
            writer.WriteAttributeString("val", value);
            writer.WriteEndElement();
        }

        foreach (XlsxDateGroupItem item in values.DateGroupItems)
        {
            WriteDateGroupItem(writer, namespaceUri, item);
        }

        WriteUnknownXml(writer, values.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteDateGroupItem(XmlWriter writer, string namespaceUri, XlsxDateGroupItem item)
    {
        writer.WriteStartElement("", "dateGroupItem", namespaceUri);
        WriteOptionalUIntAttribute(writer, "year", item.Year, item.HasYear);
        WriteOptionalUIntAttribute(writer, "month", item.Month, item.HasMonth);
        WriteOptionalUIntAttribute(writer, "day", item.Day, item.HasDay);
        WriteOptionalUIntAttribute(writer, "hour", item.Hour, item.HasHour);
        WriteOptionalUIntAttribute(writer, "minute", item.Minute, item.HasMinute);
        WriteOptionalUIntAttribute(writer, "second", item.Second, item.HasSecond);
        WriteOptionalStringAttribute(writer, "dateTimeGrouping", item.DateTimeGrouping);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, item.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteCustomFilters(XmlWriter writer, string namespaceUri, XlsxCustomFilters customFilters)
    {
        writer.WriteStartElement("", "customFilters", namespaceUri);
        WriteOptionalBoolAttribute(writer, "and", customFilters.And, customFilters.HasAnd);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, customFilters.UnknownAttributes);
        foreach (XlsxCustomFilter customFilter in customFilters.Filters)
        {
            writer.WriteStartElement("", "customFilter", namespaceUri);
            WriteOptionalStringAttribute(writer, "operator", customFilter.Operator);
            WriteOptionalStringAttribute(writer, "val", customFilter.Value);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, customFilter.UnknownAttributes);
            writer.WriteEndElement();
        }

        WriteUnknownXml(writer, customFilters.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteDynamicFilter(XmlWriter writer, string namespaceUri, XlsxDynamicFilter dynamicFilter)
    {
        writer.WriteStartElement("", "dynamicFilter", namespaceUri);
        WriteOptionalStringAttribute(writer, "type", dynamicFilter.Type);
        WriteOptionalDoubleAttribute(writer, "val", dynamicFilter.Value, dynamicFilter.HasValue);
        WriteOptionalDoubleAttribute(writer, "maxVal", dynamicFilter.MaxValue, dynamicFilter.HasMaxValue);
        WriteOptionalStringAttribute(writer, "valIso", dynamicFilter.ValueIso);
        WriteOptionalStringAttribute(writer, "maxValIso", dynamicFilter.MaxValueIso);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, dynamicFilter.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteTop10Filter(XmlWriter writer, string namespaceUri, XlsxTop10Filter top10)
    {
        writer.WriteStartElement("", "top10", namespaceUri);
        WriteOptionalBoolAttribute(writer, "top", top10.Top, top10.HasTop);
        WriteOptionalBoolAttribute(writer, "percent", top10.Percent, top10.HasPercent);
        WriteOptionalDoubleAttribute(writer, "val", top10.Value, top10.HasValue);
        WriteOptionalDoubleAttribute(writer, "filterVal", top10.FilterValue, top10.HasFilterValue);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, top10.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteColorFilter(XmlWriter writer, string namespaceUri, XlsxColorFilter colorFilter)
    {
        writer.WriteStartElement("", "colorFilter", namespaceUri);
        WriteOptionalUIntAttribute(writer, "dxfId", colorFilter.DxfId, colorFilter.HasDxfId);
        WriteOptionalBoolAttribute(writer, "cellColor", colorFilter.CellColor, colorFilter.HasCellColor);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, colorFilter.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteIconFilter(XmlWriter writer, string namespaceUri, XlsxIconFilter iconFilter)
    {
        writer.WriteStartElement("", "iconFilter", namespaceUri);
        WriteOptionalStringAttribute(writer, "iconSet", iconFilter.IconSet);
        WriteOptionalUIntAttribute(writer, "iconId", iconFilter.IconId, iconFilter.HasIconId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, iconFilter.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteSortStateElement(XmlWriter writer, string namespaceUri, XlsxSortState sortState)
    {
        writer.WriteStartElement("", "sortState", namespaceUri);
        WriteOptionalStringAttribute(writer, "ref", sortState.Reference);
        WriteOptionalBoolAttribute(writer, "columnSort", sortState.ColumnSort, sortState.HasColumnSort);
        WriteOptionalBoolAttribute(writer, "caseSensitive", sortState.CaseSensitive, sortState.HasCaseSensitive);
        WriteOptionalStringAttribute(writer, "sortMethod", sortState.SortMethod);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, sortState.UnknownAttributes);
        foreach (XlsxSortCondition condition in sortState.Conditions)
        {
            WriteSortCondition(writer, namespaceUri, condition);
        }

        WriteUnknownXml(writer, sortState.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteSortCondition(XmlWriter writer, string namespaceUri, XlsxSortCondition condition)
    {
        writer.WriteStartElement("", "sortCondition", namespaceUri);
        WriteOptionalBoolAttribute(writer, "descending", condition.Descending, condition.HasDescending);
        WriteOptionalStringAttribute(writer, "sortBy", condition.SortBy);
        WriteOptionalStringAttribute(writer, "ref", condition.Reference);
        WriteOptionalStringAttribute(writer, "customList", condition.CustomList);
        WriteOptionalUIntAttribute(writer, "dxfId", condition.DxfId, condition.HasDxfId);
        WriteOptionalStringAttribute(writer, "iconSet", condition.IconSet);
        WriteOptionalUIntAttribute(writer, "iconId", condition.IconId, condition.HasIconId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, condition.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteOptionalStringAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    internal static void WriteOptionalUIntAttribute(XmlWriter writer, string name, uint value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteUnsignedInteger(value));
        }
    }

    internal static void WriteOptionalDoubleAttribute(XmlWriter writer, string name, double value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteDouble(value));
        }
    }

    internal static void WriteOptionalBoolAttribute(XmlWriter writer, string name, bool value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteBoolean(value));
        }
    }

    internal static void WriteRawXml(XmlWriter writer, string rawXml)
    {
        if (rawXml != null && rawXml.Length > 0)
        {
            writer.WriteRaw(rawXml);
        }
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        foreach (string item in unknownXml)
        {
            WriteRawXml(writer, item);
        }
    }
}
