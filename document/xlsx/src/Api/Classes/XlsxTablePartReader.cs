using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads table part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxTablePartReader : IXlsxTablePartReader
{
    /// <summary>
    /// Initializes a new table part reader with its default state.
    /// </summary>
    public XlsxTablePartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The table part produced by the operation.</returns>
    public XlsxTablePart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxTablePart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The table part produced by the operation.</returns>
    public XlsxTablePart ReadBytes(byte[] bytes)
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
    /// <returns>The table part produced by the operation.</returns>
    public XlsxTablePart ReadStream(Stream stream)
    {
        XlsxTablePart part = new XlsxTablePart();
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
                    ReadRootElement(part, reader);
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }

                    ReadTableChildren(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "table.missingRoot", "Table XML must contain a table root element."));
        }

        return part;
    }

    internal static XlsxTablePart CreateMissingRootResult()
    {
        XlsxTablePart part = new XlsxTablePart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "table.missingRoot", "Table XML must contain a table root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxTablePart part, XmlReader reader)
    {
        if (reader.LocalName != "table" || !IsTableNamespace(reader.NamespaceURI))
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "table.invalidRoot", "Table XML root must be a SpreadsheetML table element."));
            reader.ReadOuterXml();
            return;
        }

        part.NamespaceUri = reader.NamespaceURI;
        SetOptionalUInt(part, reader.GetAttribute("id"), "table", "id", 1);
        part.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        part.DisplayName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("displayName"));
        part.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
        part.TableType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("tableType"));
        part.Comment = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("comment"));
        SetOptionalUInt(part, reader.GetAttribute("headerRowCount"), "table", "headerRowCount", 2);
        SetOptionalUInt(part, reader.GetAttribute("totalsRowCount"), "table", "totalsRowCount", 3);
        SetOptionalBool(part, reader.GetAttribute("totalsRowShown"), "table", "totalsRowShown", 1);
        SetOptionalBool(part, reader.GetAttribute("insertRow"), "table", "insertRow", 2);
        SetOptionalBool(part, reader.GetAttribute("insertRowShift"), "table", "insertRowShift", 3);
        SetOptionalBool(part, reader.GetAttribute("published"), "table", "published", 4);
        SetOptionalUInt(part, reader.GetAttribute("connectionId"), "table", "connectionId", 4);
        SetOptionalUInt(part, reader.GetAttribute("headerRowDxfId"), "table", "headerRowDxfId", 6);
        SetOptionalUInt(part, reader.GetAttribute("dataDxfId"), "table", "dataDxfId", 7);
        SetOptionalUInt(part, reader.GetAttribute("totalsRowDxfId"), "table", "totalsRowDxfId", 8);
        SetOptionalUInt(part, reader.GetAttribute("headerRowBorderDxfId"), "table", "headerRowBorderDxfId", 9);
        SetOptionalUInt(part, reader.GetAttribute("tableBorderDxfId"), "table", "tableBorderDxfId", 10);
        SetOptionalUInt(part, reader.GetAttribute("totalsRowBorderDxfId"), "table", "totalsRowBorderDxfId", 11);
        part.HeaderRowCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("headerRowCellStyle"));
        part.DataCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("dataCellStyle"));
        part.TotalsRowCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("totalsRowCellStyle"));
        ReadUnknownAttributes(reader, part.UnknownRootAttributes, "table");
        ValidateTableReference(part);
    }

    internal static void ReadTableChildren(XlsxTablePart part, XmlReader reader)
    {
        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsTableNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "autoFilter")
                {
                    part.AutoFilterRawXml = reader.ReadOuterXml();
                    part.AutoFilterEnabled = true;
                    continue;
                }

                if (reader.LocalName == "sortState")
                {
                    part.SortStateRawXml = reader.ReadOuterXml();
                    part.SortStateEnabled = true;
                    continue;
                }

                if (reader.LocalName == "tableColumns")
                {
                    ReadTableColumns(part, reader);
                    continue;
                }

                if (reader.LocalName == "tableStyleInfo")
                {
                    ReadTableStyleInfo(part, reader);
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    part.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadTableColumns(XlsxTablePart part, XmlReader reader)
    {
        SetOptionalUInt(part, reader.GetAttribute("count"), "tableColumns", "count", 5);
        ReadUnknownAttributes(reader, part.TableColumnsUnknownAttributes, "tableColumns");
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsTableNamespace(reader.NamespaceURI) && reader.LocalName == "tableColumn")
            {
                part.Columns.Add(ReadTableColumn(part, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxTableColumn ReadTableColumn(XlsxTablePart part, XmlReader reader)
    {
        XlsxTableColumn column = new XlsxTableColumn();
        SetOptionalUInt(part, reader.GetAttribute("id"), "tableColumn", "id", column, 1);
        column.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        column.UniqueName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("uniqueName"));
        column.TotalsRowFunction = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("totalsRowFunction"));
        if (IsKnownTotalFunction(column.TotalsRowFunction))
        {
            column.TotalFunction = ParseTotalFunction(column.TotalsRowFunction);
            column.HasTotalFunction = true;
        }
        column.TotalsRowLabel = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("totalsRowLabel"));
        SetOptionalUInt(part, reader.GetAttribute("queryTableFieldId"), "tableColumn", "queryTableFieldId", column, 2);
        SetOptionalUInt(part, reader.GetAttribute("headerRowDxfId"), "tableColumn", "headerRowDxfId", column, 3);
        SetOptionalUInt(part, reader.GetAttribute("dataDxfId"), "tableColumn", "dataDxfId", column, 4);
        SetOptionalUInt(part, reader.GetAttribute("totalsRowDxfId"), "tableColumn", "totalsRowDxfId", column, 5);
        column.HeaderRowCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("headerRowCellStyle"));
        column.DataCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("dataCellStyle"));
        column.TotalsRowCellStyle = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("totalsRowCellStyle"));
        ReadUnknownAttributes(reader, column.UnknownAttributes, "tableColumn");
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsTableNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "calculatedColumnFormula")
                {
                    column.CalculatedColumnFormulaRawXml = reader.ReadOuterXml();
                    column.CalculatedColumnFormula = ReadSimpleElementText(column.CalculatedColumnFormulaRawXml);
                    continue;
                }

                if (reader.LocalName == "totalsRowFormula")
                {
                    column.TotalsRowFormulaRawXml = reader.ReadOuterXml();
                    column.TotalsRowFormula = ReadSimpleElementText(column.TotalsRowFormulaRawXml);
                    continue;
                }

                if (reader.LocalName == "xmlColumnPr")
                {
                    column.XmlColumnPropertiesRawXml = reader.ReadOuterXml();
                    column.XmlColumnProperties = ReadXmlColumnProperties(part, column.XmlColumnPropertiesRawXml);
                    column.HasXmlColumnProperties = true;
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

    internal static XlsxTableXmlColumnProperties ReadXmlColumnProperties(XlsxTablePart part, string xml)
    {
        XlsxTableXmlColumnProperties properties = new XlsxTableXmlColumnProperties();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "xmlColumnPr" && IsTableNamespace(reader.NamespaceURI))
                    {
                        XlsxXmlValueParseResult mapId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("mapId")));
                        AddParseDiagnostics(part, mapId, "xmlColumnPr", "mapId");
                        if (mapId.IsValid)
                        {
                            properties.MapId = mapId.UnsignedIntValue;
                            properties.HasMapId = true;
                        }

                        properties.XPath = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("xpath"));
                        properties.XmlDataType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("xmlDataType"));
                        string denormalizedText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("denormalized"));
                        if (denormalizedText.Length > 0)
                        {
                            XlsxXmlValueParseResult denormalized = XlsxXmlValueParser.ParseBoolean(denormalizedText);
                            AddParseDiagnostics(part, denormalized, "xmlColumnPr", "denormalized");
                            if (denormalized.IsValid)
                            {
                                properties.Denormalized = denormalized.BooleanValue;
                                properties.HasDenormalized = true;
                            }
                        }

                        ReadUnknownAttributes(reader, properties.UnknownAttributes, "xmlColumnPr");
                        if (reader.IsEmptyElement)
                        {
                            return properties;
                        }

                        int depth = reader.Depth;
                        reader.Read();
                        while (!reader.EOF)
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                            {
                                return properties;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "extLst" && IsTableNamespace(reader.NamespaceURI))
                            {
                                properties.ExtensionListRawXml = reader.ReadOuterXml();
                                continue;
                            }

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                properties.UnknownXml.Add(reader.ReadOuterXml());
                                continue;
                            }

                            reader.Read();
                        }
                    }

                    reader.Read();
                }
            }
        }

        return properties;
    }

    internal static bool IsKnownTotalFunction(string text)
    {
        return text == "none" || text == "average" || text == "count" || text == "countNums" || text == "max" || text == "min" || text == "sum" || text == "stdDev" || text == "var" || text == "custom";
    }

    internal static XlsxTableTotalFunction ParseTotalFunction(string text)
    {
        if (text == "average")
        {
            return XlsxTableTotalFunction.Average;
        }
        if (text == "count")
        {
            return XlsxTableTotalFunction.Count;
        }
        if (text == "countNums")
        {
            return XlsxTableTotalFunction.CountNumbers;
        }
        if (text == "max")
        {
            return XlsxTableTotalFunction.Maximum;
        }
        if (text == "min")
        {
            return XlsxTableTotalFunction.Minimum;
        }
        if (text == "sum")
        {
            return XlsxTableTotalFunction.Sum;
        }
        if (text == "stdDev")
        {
            return XlsxTableTotalFunction.StandardDeviation;
        }
        if (text == "var")
        {
            return XlsxTableTotalFunction.Variance;
        }
        if (text == "custom")
        {
            return XlsxTableTotalFunction.Custom;
        }
        return XlsxTableTotalFunction.None;
    }

    internal static void ReadTableStyleInfo(XlsxTablePart part, XmlReader reader)
    {
        XlsxTableStyleInfo style = new XlsxTableStyleInfo();
        style.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        SetOptionalBool(part, reader.GetAttribute("showFirstColumn"), "tableStyleInfo", "showFirstColumn", style, 1);
        SetOptionalBool(part, reader.GetAttribute("showLastColumn"), "tableStyleInfo", "showLastColumn", style, 2);
        SetOptionalBool(part, reader.GetAttribute("showRowStripes"), "tableStyleInfo", "showRowStripes", style, 3);
        SetOptionalBool(part, reader.GetAttribute("showColumnStripes"), "tableStyleInfo", "showColumnStripes", style, 4);
        ReadUnknownAttributes(reader, style.UnknownAttributes, "tableStyleInfo");
        part.StyleInfo = style;
        part.HasStyleInfo = true;
        reader.ReadOuterXml();
    }

    internal static string ReadSimpleElementText(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                    {
                        builder.Append(reader.Value);
                    }

                    reader.Read();
                }
            }
        }

        return builder.ToString();
    }

    internal static void ValidateTableReference(XlsxTablePart part)
    {
        if (part.Reference == null || part.Reference.Length == 0)
        {
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(part.Reference);
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void SetOptionalUInt(XlsxTablePart part, string? text, string elementName, string attributeName, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            part.Id = result.UnsignedIntValue;
            part.HasId = true;
        }
        else if (target == 2)
        {
            part.HeaderRowCount = result.UnsignedIntValue;
            part.HasHeaderRowCount = true;
        }
        else if (target == 3)
        {
            part.TotalsRowCount = result.UnsignedIntValue;
            part.HasTotalsRowCount = true;
        }
        else if (target == 4)
        {
            part.ConnectionId = result.UnsignedIntValue;
            part.HasConnectionId = true;
        }
        else if (target == 5)
        {
            part.TableColumnCount = result.UnsignedIntValue;
            part.HasTableColumnCount = true;
        }
        else if (target == 6)
        {
            part.HeaderRowDxfId = result.UnsignedIntValue;
            part.HasHeaderRowDxfId = true;
        }
        else if (target == 7)
        {
            part.DataDxfId = result.UnsignedIntValue;
            part.HasDataDxfId = true;
        }
        else if (target == 8)
        {
            part.TotalsRowDxfId = result.UnsignedIntValue;
            part.HasTotalsRowDxfId = true;
        }
        else if (target == 9)
        {
            part.HeaderRowBorderDxfId = result.UnsignedIntValue;
            part.HasHeaderRowBorderDxfId = true;
        }
        else if (target == 10)
        {
            part.TableBorderDxfId = result.UnsignedIntValue;
            part.HasTableBorderDxfId = true;
        }
        else if (target == 11)
        {
            part.TotalsRowBorderDxfId = result.UnsignedIntValue;
            part.HasTotalsRowBorderDxfId = true;
        }
    }

    internal static void SetOptionalUInt(XlsxTablePart part, string? text, string elementName, string attributeName, XlsxTableColumn column, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            column.Id = result.UnsignedIntValue;
            column.HasId = true;
        }
        else if (target == 2)
        {
            column.QueryTableFieldId = result.UnsignedIntValue;
            column.HasQueryTableFieldId = true;
        }
        else if (target == 3)
        {
            column.HeaderRowDxfId = result.UnsignedIntValue;
            column.HasHeaderRowDxfId = true;
        }
        else if (target == 4)
        {
            column.DataDxfId = result.UnsignedIntValue;
            column.HasDataDxfId = true;
        }
        else if (target == 5)
        {
            column.TotalsRowDxfId = result.UnsignedIntValue;
            column.HasTotalsRowDxfId = true;
        }
    }

    internal static void SetOptionalBool(XlsxTablePart part, string? text, string elementName, string attributeName, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            part.TotalsRowShown = result.BooleanValue;
            part.HasTotalsRowShown = true;
        }
        else if (target == 2)
        {
            part.InsertRow = result.BooleanValue;
            part.HasInsertRow = true;
        }
        else if (target == 3)
        {
            part.InsertRowShift = result.BooleanValue;
            part.HasInsertRowShift = true;
        }
        else if (target == 4)
        {
            part.Published = result.BooleanValue;
            part.HasPublished = true;
        }
    }

    internal static void SetOptionalBool(XlsxTablePart part, string? text, string elementName, string attributeName, XlsxTableStyleInfo style, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            style.ShowFirstColumn = result.BooleanValue;
            style.HasShowFirstColumn = true;
        }
        else if (target == 2)
        {
            style.ShowLastColumn = result.BooleanValue;
            style.HasShowLastColumn = true;
        }
        else if (target == 3)
        {
            style.ShowRowStripes = result.BooleanValue;
            style.HasShowRowStripes = true;
        }
        else if (target == 4)
        {
            style.ShowColumnStripes = result.BooleanValue;
            style.HasShowColumnStripes = true;
        }
    }

    internal static void AddParseDiagnostics(XlsxTablePart part, XlsxXmlValueParseResult result, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
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
        if (elementName == "table")
        {
            return localName == "id" || localName == "name" || localName == "displayName" || localName == "ref" || localName == "tableType" || localName == "comment" || localName == "headerRowCount" || localName == "totalsRowCount" || localName == "totalsRowShown" || localName == "insertRow" || localName == "insertRowShift" || localName == "published" || localName == "connectionId" || localName == "headerRowDxfId" || localName == "dataDxfId" || localName == "totalsRowDxfId" || localName == "headerRowBorderDxfId" || localName == "tableBorderDxfId" || localName == "totalsRowBorderDxfId" || localName == "headerRowCellStyle" || localName == "dataCellStyle" || localName == "totalsRowCellStyle";
        }

        if (elementName == "tableColumns")
        {
            return localName == "count";
        }

        if (elementName == "tableColumn")
        {
            return localName == "id" || localName == "name" || localName == "uniqueName" || localName == "totalsRowFunction" || localName == "totalsRowLabel" || localName == "queryTableFieldId" || localName == "headerRowDxfId" || localName == "dataDxfId" || localName == "totalsRowDxfId" || localName == "headerRowCellStyle" || localName == "dataCellStyle" || localName == "totalsRowCellStyle";
        }

        if (elementName == "tableStyleInfo")
        {
            return localName == "name" || localName == "showFirstColumn" || localName == "showLastColumn" || localName == "showRowStripes" || localName == "showColumnStripes";
        }

        if (elementName == "xmlColumnPr")
        {
            return localName == "mapId" || localName == "xpath" || localName == "denormalized" || localName == "xmlDataType";
        }

        return false;
    }

    internal static bool IsTableNamespace(string? namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
