using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads auto filter data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxAutoFilterReader : IXlsxAutoFilterReader
{
    /// <summary>
    /// Initializes a new auto filter reader with its default state.
    /// </summary>
    public XlsxAutoFilterReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The auto filter produced by the operation.</returns>
    public XlsxAutoFilter ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxAutoFilter filter = ReadBytes(bytes);
        filter.RawXml = xml;
        return filter;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The auto filter produced by the operation.</returns>
    public XlsxAutoFilter ReadBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The auto filter produced by the operation.</returns>
    public XlsxAutoFilter ReadStream(Stream stream)
    {
        XlsxAutoFilter filter = new XlsxAutoFilter();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    if (reader.LocalName == "autoFilter" && IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        ReadAutoFilterRoot(filter, reader);
                        continue;
                    }

                    if (reader.LocalName == "sortState" && IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        filter.NamespaceUri = reader.NamespaceURI;
                        filter.SortState = ReadSortState(filter, reader);
                        filter.HasSortState = true;
                        continue;
                    }

                    filter.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilter.invalidRoot", "Auto filter XML root must be an autoFilter or sortState element."));
                    reader.ReadOuterXml();
                    continue;
                }

                filter.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            filter.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilter.missingRoot", "Auto filter XML must contain an autoFilter or sortState root element."));
        }

        return filter;
    }

    internal static XlsxAutoFilter CreateMissingRootResult()
    {
        XlsxAutoFilter filter = new XlsxAutoFilter();
        filter.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "autoFilter.missingRoot", "Auto filter XML must contain an autoFilter or sortState root element."));
        return filter;
    }

    internal static void ReadAutoFilterRoot(XlsxAutoFilter filter, XmlReader reader)
    {
        filter.NamespaceUri = reader.NamespaceURI;
        filter.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
        ValidateRangeReference(filter, filter.Reference, "autoFilter", "ref");
        ReadUnknownAttributes(reader, filter.UnknownAttributes, "autoFilter");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "filterColumn")
                {
                    filter.FilterColumns.Add(ReadFilterColumn(filter, reader));
                    continue;
                }

                if (reader.LocalName == "sortState")
                {
                    filter.SortState = ReadSortState(filter, reader);
                    filter.HasSortState = true;
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    filter.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                filter.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxAutoFilterColumn ReadFilterColumn(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxAutoFilterColumn column = new XlsxAutoFilterColumn();
        SetOptionalUInt(filter, reader.GetAttribute("colId"), "filterColumn", "colId", column, 1);
        SetOptionalBool(filter, reader.GetAttribute("hiddenButton"), "filterColumn", "hiddenButton", column, 1);
        SetOptionalBool(filter, reader.GetAttribute("showButton"), "filterColumn", "showButton", column, 2);
        ReadUnknownAttributes(reader, column.UnknownAttributes, "filterColumn");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return column;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return column;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "filters")
                {
                    column.Values = ReadFilters(filter, reader);
                    column.HasValues = true;
                    continue;
                }

                if (reader.LocalName == "customFilters")
                {
                    column.CustomFilters = ReadCustomFilters(filter, reader);
                    column.HasCustomFilters = true;
                    continue;
                }

                if (reader.LocalName == "dynamicFilter")
                {
                    column.DynamicFilter = ReadDynamicFilter(filter, reader);
                    column.HasDynamicFilter = true;
                    continue;
                }

                if (reader.LocalName == "top10")
                {
                    column.Top10 = ReadTop10Filter(filter, reader);
                    column.HasTop10 = true;
                    continue;
                }

                if (reader.LocalName == "colorFilter")
                {
                    column.ColorFilter = ReadColorFilter(filter, reader);
                    column.HasColorFilter = true;
                    continue;
                }

                if (reader.LocalName == "iconFilter")
                {
                    column.IconFilter = ReadIconFilter(filter, reader);
                    column.HasIconFilter = true;
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    column.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                column.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return column;
    }

    internal static XlsxAutoFilterValues ReadFilters(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxAutoFilterValues values = new XlsxAutoFilterValues();
        SetOptionalBool(filter, reader.GetAttribute("blank"), "filters", "blank", values, 1);
        values.CalendarType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("calendarType"));
        ReadUnknownAttributes(reader, values.UnknownAttributes, "filters");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return values;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return values;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "filter")
                {
                    values.Values.Add(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")));
                    reader.ReadOuterXml();
                    continue;
                }

                if (reader.LocalName == "dateGroupItem")
                {
                    values.DateGroupItems.Add(ReadDateGroupItem(filter, reader));
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                values.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return values;
    }

    internal static XlsxDateGroupItem ReadDateGroupItem(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxDateGroupItem item = new XlsxDateGroupItem();
        SetOptionalUInt(filter, reader.GetAttribute("year"), "dateGroupItem", "year", item, 1);
        SetOptionalUInt(filter, reader.GetAttribute("month"), "dateGroupItem", "month", item, 2);
        SetOptionalUInt(filter, reader.GetAttribute("day"), "dateGroupItem", "day", item, 3);
        SetOptionalUInt(filter, reader.GetAttribute("hour"), "dateGroupItem", "hour", item, 4);
        SetOptionalUInt(filter, reader.GetAttribute("minute"), "dateGroupItem", "minute", item, 5);
        SetOptionalUInt(filter, reader.GetAttribute("second"), "dateGroupItem", "second", item, 6);
        item.DateTimeGrouping = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("dateTimeGrouping"));
        ReadUnknownAttributes(reader, item.UnknownAttributes, "dateGroupItem");
        reader.ReadOuterXml();
        return item;
    }

    internal static XlsxCustomFilters ReadCustomFilters(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxCustomFilters customFilters = new XlsxCustomFilters();
        SetOptionalBool(filter, reader.GetAttribute("and"), "customFilters", "and", customFilters, 1);
        ReadUnknownAttributes(reader, customFilters.UnknownAttributes, "customFilters");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return customFilters;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return customFilters;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI) && reader.LocalName == "customFilter")
            {
                customFilters.Filters.Add(ReadCustomFilter(reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                customFilters.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return customFilters;
    }

    internal static XlsxCustomFilter ReadCustomFilter(XmlReader reader)
    {
        XlsxCustomFilter customFilter = new XlsxCustomFilter();
        customFilter.Operator = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("operator"));
        customFilter.Value = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
        ReadUnknownAttributes(reader, customFilter.UnknownAttributes, "customFilter");
        reader.ReadOuterXml();
        return customFilter;
    }

    internal static XlsxDynamicFilter ReadDynamicFilter(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxDynamicFilter dynamicFilter = new XlsxDynamicFilter();
        dynamicFilter.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
        dynamicFilter.ValueIso = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("valIso"));
        dynamicFilter.MaxValueIso = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("maxValIso"));
        SetOptionalDouble(filter, reader.GetAttribute("val"), "dynamicFilter", "val", dynamicFilter, 1);
        SetOptionalDouble(filter, reader.GetAttribute("maxVal"), "dynamicFilter", "maxVal", dynamicFilter, 2);
        ReadUnknownAttributes(reader, dynamicFilter.UnknownAttributes, "dynamicFilter");
        reader.ReadOuterXml();
        return dynamicFilter;
    }

    internal static XlsxTop10Filter ReadTop10Filter(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxTop10Filter top10 = new XlsxTop10Filter();
        SetOptionalBool(filter, reader.GetAttribute("top"), "top10", "top", top10, 1);
        SetOptionalBool(filter, reader.GetAttribute("percent"), "top10", "percent", top10, 2);
        SetOptionalDouble(filter, reader.GetAttribute("val"), "top10", "val", top10, 1);
        SetOptionalDouble(filter, reader.GetAttribute("filterVal"), "top10", "filterVal", top10, 2);
        ReadUnknownAttributes(reader, top10.UnknownAttributes, "top10");
        reader.ReadOuterXml();
        return top10;
    }

    internal static XlsxColorFilter ReadColorFilter(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxColorFilter colorFilter = new XlsxColorFilter();
        SetOptionalUInt(filter, reader.GetAttribute("dxfId"), "colorFilter", "dxfId", colorFilter, 1);
        SetOptionalBool(filter, reader.GetAttribute("cellColor"), "colorFilter", "cellColor", colorFilter, 1);
        ReadUnknownAttributes(reader, colorFilter.UnknownAttributes, "colorFilter");
        reader.ReadOuterXml();
        return colorFilter;
    }

    internal static XlsxIconFilter ReadIconFilter(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxIconFilter iconFilter = new XlsxIconFilter();
        iconFilter.IconSet = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("iconSet"));
        SetOptionalUInt(filter, reader.GetAttribute("iconId"), "iconFilter", "iconId", iconFilter, 1);
        ReadUnknownAttributes(reader, iconFilter.UnknownAttributes, "iconFilter");
        reader.ReadOuterXml();
        return iconFilter;
    }

    internal static XlsxSortState ReadSortState(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxSortState sortState = new XlsxSortState();
        sortState.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
        sortState.SortMethod = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sortMethod"));
        SetOptionalBool(filter, reader.GetAttribute("columnSort"), "sortState", "columnSort", sortState, 1);
        SetOptionalBool(filter, reader.GetAttribute("caseSensitive"), "sortState", "caseSensitive", sortState, 2);
        ValidateRangeReference(filter, sortState.Reference, "sortState", "ref");
        ReadUnknownAttributes(reader, sortState.UnknownAttributes, "sortState");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return sortState;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return sortState;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI) && reader.LocalName == "sortCondition")
            {
                sortState.Conditions.Add(ReadSortCondition(filter, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                sortState.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return sortState;
    }

    internal static XlsxSortCondition ReadSortCondition(XlsxAutoFilter filter, XmlReader reader)
    {
        XlsxSortCondition condition = new XlsxSortCondition();
        SetOptionalBool(filter, reader.GetAttribute("descending"), "sortCondition", "descending", condition, 1);
        condition.SortBy = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sortBy"));
        condition.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
        condition.CustomList = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("customList"));
        condition.IconSet = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("iconSet"));
        SetOptionalUInt(filter, reader.GetAttribute("dxfId"), "sortCondition", "dxfId", condition, 1);
        SetOptionalUInt(filter, reader.GetAttribute("iconId"), "sortCondition", "iconId", condition, 2);
        ValidateRangeReference(filter, condition.Reference, "sortCondition", "ref");
        ReadUnknownAttributes(reader, condition.UnknownAttributes, "sortCondition");
        reader.ReadOuterXml();
        return condition;
    }

    internal static void SetOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxAutoFilterColumn column, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        column.ColumnId = result.UnsignedIntValue;
        column.HasColumnId = true;
    }

    internal static void SetOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxDateGroupItem item, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            item.Year = result.UnsignedIntValue;
            item.HasYear = true;
        }
        else if (target == 2)
        {
            item.Month = result.UnsignedIntValue;
            item.HasMonth = true;
        }
        else if (target == 3)
        {
            item.Day = result.UnsignedIntValue;
            item.HasDay = true;
        }
        else if (target == 4)
        {
            item.Hour = result.UnsignedIntValue;
            item.HasHour = true;
        }
        else if (target == 5)
        {
            item.Minute = result.UnsignedIntValue;
            item.HasMinute = true;
        }
        else if (target == 6)
        {
            item.Second = result.UnsignedIntValue;
            item.HasSecond = true;
        }
    }

    internal static void SetOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxColorFilter colorFilter, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        colorFilter.DxfId = result.UnsignedIntValue;
        colorFilter.HasDxfId = true;
    }

    internal static void SetOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxIconFilter iconFilter, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        iconFilter.IconId = result.UnsignedIntValue;
        iconFilter.HasIconId = true;
    }

    internal static void SetOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxSortCondition condition, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            condition.DxfId = result.UnsignedIntValue;
            condition.HasDxfId = true;
        }
        else if (target == 2)
        {
            condition.IconId = result.UnsignedIntValue;
            condition.HasIconId = true;
        }
    }

    internal static void SetOptionalDouble(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxDynamicFilter dynamicFilter, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            dynamicFilter.Value = result.DoubleValue;
            dynamicFilter.HasValue = true;
        }
        else if (target == 2)
        {
            dynamicFilter.MaxValue = result.DoubleValue;
            dynamicFilter.HasMaxValue = true;
        }
    }

    internal static void SetOptionalDouble(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxTop10Filter top10, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            top10.Value = result.DoubleValue;
            top10.HasValue = true;
        }
        else if (target == 2)
        {
            top10.FilterValue = result.DoubleValue;
            top10.HasFilterValue = true;
        }
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxAutoFilterColumn column, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            column.HiddenButton = result.BooleanValue;
            column.HasHiddenButton = true;
        }
        else if (target == 2)
        {
            column.ShowButton = result.BooleanValue;
            column.HasShowButton = true;
        }
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxAutoFilterValues values, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        values.Blank = result.BooleanValue;
        values.HasBlank = true;
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxCustomFilters customFilters, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        customFilters.And = result.BooleanValue;
        customFilters.HasAnd = true;
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxTop10Filter top10, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            top10.Top = result.BooleanValue;
            top10.HasTop = true;
        }
        else if (target == 2)
        {
            top10.Percent = result.BooleanValue;
            top10.HasPercent = true;
        }
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxColorFilter colorFilter, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        colorFilter.CellColor = result.BooleanValue;
        colorFilter.HasCellColor = true;
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxSortState sortState, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            sortState.ColumnSort = result.BooleanValue;
            sortState.HasColumnSort = true;
        }
        else if (target == 2)
        {
            sortState.CaseSensitive = result.BooleanValue;
            sortState.HasCaseSensitive = true;
        }
    }

    internal static void SetOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName, XlsxSortCondition condition, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(filter, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        condition.Descending = result.BooleanValue;
        condition.HasDescending = true;
    }

    internal static XlsxXmlValueParseResult ParseOptionalUInt(XlsxAutoFilter filter, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddValueDiagnostics(filter, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalDouble(XlsxAutoFilter filter, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseDouble(text);
        AddValueDiagnostics(filter, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalBool(XlsxAutoFilter filter, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseBoolean(text);
        AddValueDiagnostics(filter, result, elementName, attributeName);
        return result;
    }

    internal static void ValidateRangeReference(XlsxAutoFilter filter, string reference, string elementName, string attributeName)
    {
        if (reference == null || reference.Length == 0)
        {
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            filter.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddValueDiagnostics(XlsxAutoFilter filter, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            filter.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string elementName)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool known = reader.NamespaceURI == "" && IsKnownAttribute(elementName, reader.LocalName);
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownAttribute(string elementName, string localName)
    {
        if (elementName == "autoFilter")
        {
            return localName == "ref";
        }

        if (elementName == "filterColumn")
        {
            return localName == "colId" || localName == "hiddenButton" || localName == "showButton";
        }

        if (elementName == "filters")
        {
            return localName == "blank" || localName == "calendarType";
        }

        if (elementName == "dateGroupItem")
        {
            return localName == "year" || localName == "month" || localName == "day" || localName == "hour" || localName == "minute" || localName == "second" || localName == "dateTimeGrouping";
        }

        if (elementName == "customFilters")
        {
            return localName == "and";
        }

        if (elementName == "customFilter")
        {
            return localName == "operator" || localName == "val";
        }

        if (elementName == "dynamicFilter")
        {
            return localName == "type" || localName == "val" || localName == "maxVal" || localName == "valIso" || localName == "maxValIso";
        }

        if (elementName == "top10")
        {
            return localName == "top" || localName == "percent" || localName == "val" || localName == "filterVal";
        }

        if (elementName == "colorFilter")
        {
            return localName == "dxfId" || localName == "cellColor";
        }

        if (elementName == "iconFilter")
        {
            return localName == "iconSet" || localName == "iconId";
        }

        if (elementName == "sortState")
        {
            return localName == "ref" || localName == "columnSort" || localName == "caseSensitive" || localName == "sortMethod";
        }

        if (elementName == "sortCondition")
        {
            return localName == "descending" || localName == "sortBy" || localName == "ref" || localName == "customList" || localName == "dxfId" || localName == "iconSet" || localName == "iconId";
        }

        return false;
    }

    internal static bool IsSpreadsheetNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
