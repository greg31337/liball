using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads pivot table definition data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxPivotTableDefinitionReader : IXlsxPivotTableDefinitionReader
{
    /// <summary>
    /// Initializes a new pivot table definition reader with its default state.
    /// </summary>
    public XlsxPivotTableDefinitionReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadString(string xml)
    {
        return ReadStringFromPart("/xl/pivotTables/pivotTable1.xml", xml);
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadStringFromPart(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxPivotTableDefinitionPart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadStringWithRelationships(string partPath, string xml, XlsxRelationshipsPart relationships)
    {
        XlsxPivotTableDefinitionPart part = ReadStringFromPart(partPath, xml);
        XlsxPivotTableCacheRelationshipResult result = part.ResolveCacheRelationship(relationships);
        CopyRelationshipDiagnostics(part, result);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadBytes(string partPath, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(partPath, stream);
        }
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadBytesWithRelationships(string partPath, byte[] bytes, XlsxRelationshipsPart relationships)
    {
        XlsxPivotTableDefinitionPart part = ReadBytes(partPath, bytes);
        XlsxPivotTableCacheRelationshipResult result = part.ResolveCacheRelationship(relationships);
        CopyRelationshipDiagnostics(part, result);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The pivot table definition part produced by the operation.</returns>
    public XlsxPivotTableDefinitionPart ReadStream(string partPath, Stream stream)
    {
        XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
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

                    int rootDepth = reader.Depth;
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth)
                        {
                            reader.Read();
                            break;
                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Depth == rootDepth + 1 && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                        {
                            ReadChildElement(part, reader);
                            continue;
                        }

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            reader.ReadOuterXml();
                            continue;
                        }

                        reader.Read();
                    }

                    continue;
                }

                reader.ReadOuterXml();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinition.missingRoot", "Pivot table definition XML must contain a pivotTableDefinition root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxPivotTableDefinitionPart CreateMissingRootResult(string partPath)
    {
        XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinition.missingRoot", "Pivot table definition XML must contain a pivotTableDefinition root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxPivotTableDefinitionPart part, XmlReader reader)
    {
        if (reader.LocalName != "pivotTableDefinition" || reader.NamespaceURI != XlsxNamespaces.Spreadsheet)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinition.invalidRoot", "Pivot table definition XML root must be pivotTableDefinition in the SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        string? cacheIdText = reader.GetAttribute("cacheId");
        if (cacheIdText != null && cacheIdText.Length > 0)
        {
            XlsxXmlValueParseResult cacheId = XlsxXmlValueParser.ParseUnsignedInteger(cacheIdText);
            if (cacheId.IsValid)
            {
                part.CacheId = cacheId.UnsignedIntValue;
                part.HasCacheId = true;
            }
            else
            {
                foreach (XlsxDiagnostic diagnostic in cacheId.Validation.Diagnostics)
                {
                    diagnostic.SetLocation(part.PartPath, "pivotTableDefinition", "cacheId");
                    part.AddDiagnostic(diagnostic);
                }
            }
        }

        ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadUnknownRootAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool knownName = reader.NamespaceURI.Length == 0 && reader.LocalName == "name";
            bool knownCacheId = reader.NamespaceURI.Length == 0 && reader.LocalName == "cacheId";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!knownName && !knownCacheId && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadChildElement(XlsxPivotTableDefinitionPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        string xml = reader.ReadOuterXml();
        if (localName == "location")
        {
            part.Location = ReadLocation(part, xml);
            part.HasLocation = true;
        }
        else if (localName == "pivotFields")
        {
            ReadPivotFields(part, xml);
        }
        else if (localName == "rowFields")
        {
            ReadFieldReferences(part, xml, "field", part.RowFields);
        }
        else if (localName == "colFields")
        {
            ReadFieldReferences(part, xml, "field", part.ColumnFields);
        }
        else if (localName == "pageFields")
        {
            ReadFieldReferences(part, xml, "pageField", part.PageFields);
        }
        else if (localName == "dataFields")
        {
            ReadDataFields(part, xml);
        }
        else if (localName == "calculatedFields")
        {
            ReadCalculatedFields(part, xml);
        }
        else if (localName == "pivotFilters")
        {
            ReadFilters(part, xml);
        }
        else if (localName == "formats")
        {
            ReadFormats(part, xml);
        }
        else if (localName == "pivotTableStyleInfo")
        {
            part.StyleInfo = ReadStyleInfo(part, xml);
            part.HasStyleInfo = true;
        }
    }

    internal static XlsxPivotTableLocation ReadLocation(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableLocation location = new XlsxPivotTableLocation();
        location.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    location.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
                    SetOptionalUInt(part, reader.GetAttribute("firstHeaderRow"), "location", "firstHeaderRow", location, 1);
                    SetOptionalUInt(part, reader.GetAttribute("firstDataRow"), "location", "firstDataRow", location, 2);
                    SetOptionalUInt(part, reader.GetAttribute("firstDataCol"), "location", "firstDataCol", location, 3);
                    return location;
                }
            }
        }

        return location;
    }

    internal static void ReadPivotFields(XlsxPivotTableDefinitionPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pivotField" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string fieldXml = reader.ReadOuterXml();
                    part.Fields.Add(ReadPivotField(part, fieldXml));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableField ReadPivotField(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableField field = new XlsxPivotTableField();
        field.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pivotField" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    field.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    field.Axis = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("axis"));
                    field.SortType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sortType"));
                    SetOptionalBool(part, reader.GetAttribute("dataField"), "pivotField", "dataField", field, 1);
                    SetOptionalBool(part, reader.GetAttribute("defaultSubtotal"), "pivotField", "defaultSubtotal", field, 2);
                    SetOptionalBool(part, reader.GetAttribute("showAll"), "pivotField", "showAll", field, 3);
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        int fieldDepth = reader.Depth;
                        reader.Read();
                        while (!reader.EOF)
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == fieldDepth)
                            {
                                reader.Read();
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Depth == fieldDepth + 1 && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                            {
                                string localName = reader.LocalName;
                                string childXml = reader.ReadOuterXml();
                                if (localName == "items")
                                {
                                    ReadItems(part, childXml, field.Items);
                                    field.ItemsCount = CountChildItems(childXml, "item");
                                    field.HasItemsCount = true;
                                }
                                else if (localName == "groupItems")
                                {
                                    ReadItems(part, childXml, field.GroupItems);
                                    field.HasGrouping = true;
                                }
                                else if (localName == "fieldGroup" || localName == "groupLevel")
                                {
                                    field.HasGrouping = true;
                                }

                                continue;
                            }

                            reader.Read();
                        }
                    }

                    continue;
                }

                reader.Read();
            }
        }

        return field;
    }

    internal static void ReadItems(XlsxPivotTableDefinitionPart part, string xml, List<XlsxPivotTableItem> items)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "item" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string itemXml = reader.ReadOuterXml();
                    items.Add(ReadItem(part, itemXml, "item"));
                    continue;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string localName = reader.LocalName;
                    string itemXml = reader.ReadOuterXml();
                    items.Add(ReadItem(part, itemXml, localName));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableItem ReadItem(XlsxPivotTableDefinitionPart part, string xml, string elementName)
    {
        XlsxPivotTableItem item = new XlsxPivotTableItem();
        item.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    item.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("t"));
                    item.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("n"));
                    if (item.Name.Length == 0)
                    {
                        item.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("v"));
                    }

                    SetOptionalInt(part, reader.GetAttribute("x"), elementName, "x", item, 1);
                    SetOptionalBool(part, reader.GetAttribute("h"), elementName, "h", item, 1);
                    return item;
                }
            }
        }

        return item;
    }

    internal static uint CountChildItems(string xml, string childName)
    {
        uint count = 0;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == childName && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    count = count + 1;
                }
            }
        }

        return count;
    }

    internal static void ReadFieldReferences(XlsxPivotTableDefinitionPart part, string xml, string childName, List<XlsxPivotTableFieldReference> references)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == childName && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string referenceXml = reader.ReadOuterXml();
                    references.Add(ReadFieldReference(part, referenceXml, childName));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableFieldReference ReadFieldReference(XlsxPivotTableDefinitionPart part, string xml, string elementName)
    {
        XlsxPivotTableFieldReference reference = new XlsxPivotTableFieldReference();
        reference.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    SetOptionalInt(part, reader.GetAttribute("x"), elementName, "x", reference, 1);
                    SetOptionalInt(part, reader.GetAttribute("fld"), elementName, "fld", reference, 2);
                    SetOptionalInt(part, reader.GetAttribute("item"), elementName, "item", reference, 3);
                    reference.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    return reference;
                }
            }
        }

        return reference;
    }

    internal static void ReadDataFields(XlsxPivotTableDefinitionPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "dataField" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string dataFieldXml = reader.ReadOuterXml();
                    part.DataFields.Add(ReadDataField(part, dataFieldXml));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableDataField ReadDataField(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableDataField dataField = new XlsxPivotTableDataField();
        dataField.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    dataField.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    dataField.Subtotal = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("subtotal"));
                    if (dataField.Subtotal.Length > 0)
                    {
                        dataField.SummaryFunction = XlsxPivotService.ParseSummaryFunction(dataField.Subtotal);
                        dataField.HasSummaryFunction = true;
                    }
                    SetOptionalInt(part, reader.GetAttribute("fld"), "dataField", "fld", dataField, 1);
                    SetOptionalInt(part, reader.GetAttribute("baseField"), "dataField", "baseField", dataField, 2);
                    SetOptionalInt(part, reader.GetAttribute("baseItem"), "dataField", "baseItem", dataField, 3);
                    SetOptionalUInt(part, reader.GetAttribute("numFmtId"), "dataField", "numFmtId", dataField, 1);
                    return dataField;
                }
            }
        }

        return dataField;
    }

    internal static void ReadCalculatedFields(XlsxPivotTableDefinitionPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "calculatedField" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string calculatedFieldXml = reader.ReadOuterXml();
                    part.CalculatedFields.Add(ReadCalculatedField(part, calculatedFieldXml));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableCalculatedField ReadCalculatedField(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableCalculatedField calculatedField = new XlsxPivotTableCalculatedField();
        calculatedField.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    calculatedField.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    calculatedField.Formula = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("formula"));
                    SetOptionalInt(part, reader.GetAttribute("fld"), "calculatedField", "fld", calculatedField, 1);
                    return calculatedField;
                }
            }
        }

        return calculatedField;
    }

    internal static void ReadFilters(XlsxPivotTableDefinitionPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "filter" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string filterXml = reader.ReadOuterXml();
                    part.Filters.Add(ReadFilter(part, filterXml));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableFilter ReadFilter(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableFilter filter = new XlsxPivotTableFilter();
        filter.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "filter" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    filter.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
                    filter.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    if (XlsxPivotService.IsKnownFilterToken(filter.Type))
                    {
                        filter.FilterKind = XlsxPivotService.ParseFilterKind(filter.Type);
                        filter.HasFilterKind = true;
                    }
                    filter.LabelValue = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("stringValue1"));
                    SetOptionalInt(part, reader.GetAttribute("fld"), "filter", "fld", filter, 1);
                    SetOptionalUInt(part, reader.GetAttribute("id"), "filter", "id", filter, 1);
                    SetOptionalInt(part, reader.GetAttribute("evalOrder"), "filter", "evalOrder", filter, 2);
                    SetOptionalInt(part, reader.GetAttribute("iMeasureFld"), "filter", "iMeasureFld", filter, 3);
                    if (filter.HasDataFieldIndex && filter.LabelValue.Length > 0)
                    {
                        XlsxXmlValueParseResult numericValue = XlsxXmlValueParser.ParseDouble(filter.LabelValue);
                        AddValueDiagnostics(part, numericValue, "filter", "stringValue1");
                        filter.NumericValue = numericValue.DoubleValue;
                        filter.LabelValue = "";
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pivotArea" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string areaXml = reader.ReadOuterXml();
                    filter.Areas.Add(ReadArea(part, areaXml));
                    continue;
                }

                reader.Read();
            }
        }

        return filter;
    }

    internal static void ReadFormats(XlsxPivotTableDefinitionPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "format" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string formatXml = reader.ReadOuterXml();
                    part.Formats.Add(ReadFormat(part, formatXml));
                    continue;
                }

                reader.Read();
            }
        }
    }

    internal static XlsxPivotTableFormat ReadFormat(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableFormat format = new XlsxPivotTableFormat();
        format.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "format" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    format.Action = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("action"));
                    SetOptionalUInt(part, reader.GetAttribute("dxfId"), "format", "dxfId", format, 1);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pivotArea" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string areaXml = reader.ReadOuterXml();
                    format.Area = ReadArea(part, areaXml);
                    format.HasArea = true;
                    continue;
                }

                reader.Read();
            }
        }

        return format;
    }

    internal static XlsxPivotTableArea ReadArea(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableArea area = new XlsxPivotTableArea();
        area.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pivotArea" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    area.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
                    area.Axis = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("axis"));
                    SetOptionalInt(part, reader.GetAttribute("field"), "pivotArea", "field", area, 1);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "reference" && reader.NamespaceURI == XlsxNamespaces.Spreadsheet)
                {
                    string referenceXml = reader.ReadOuterXml();
                    area.References.Add(ReadAreaReference(part, referenceXml));
                    continue;
                }

                reader.Read();
            }
        }

        return area;
    }

    internal static XlsxPivotTableAreaReference ReadAreaReference(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableAreaReference reference = new XlsxPivotTableAreaReference();
        reference.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    SetOptionalInt(part, reader.GetAttribute("field"), "reference", "field", reference, 1);
                    SetOptionalUInt(part, reader.GetAttribute("count"), "reference", "count", reference, 1);
                    SetOptionalBool(part, reader.GetAttribute("selected"), "reference", "selected", reference, 1);
                    SetOptionalBool(part, reader.GetAttribute("byPosition"), "reference", "byPosition", reference, 2);
                    return reference;
                }
            }
        }

        return reference;
    }

    internal static XlsxPivotTableStyleInfo ReadStyleInfo(XlsxPivotTableDefinitionPart part, string xml)
    {
        XlsxPivotTableStyleInfo style = new XlsxPivotTableStyleInfo();
        style.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    style.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    SetOptionalBool(part, reader.GetAttribute("showRowHeaders"), "pivotTableStyleInfo", "showRowHeaders", style, 1);
                    SetOptionalBool(part, reader.GetAttribute("showColHeaders"), "pivotTableStyleInfo", "showColHeaders", style, 2);
                    SetOptionalBool(part, reader.GetAttribute("showRowStripes"), "pivotTableStyleInfo", "showRowStripes", style, 3);
                    SetOptionalBool(part, reader.GetAttribute("showColStripes"), "pivotTableStyleInfo", "showColStripes", style, 4);
                    SetOptionalBool(part, reader.GetAttribute("showLastColumn"), "pivotTableStyleInfo", "showLastColumn", style, 5);
                    return style;
                }
            }
        }

        return style;
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableItem item, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            item.Index = result.IntValue;
            item.HasIndex = true;
        }
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableFieldReference reference, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1 || slot == 2)
        {
            reference.FieldIndex = result.IntValue;
            reference.HasFieldIndex = true;
        }
        else if (slot == 3)
        {
            reference.ItemIndex = result.IntValue;
            reference.HasItemIndex = true;
        }
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableDataField dataField, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            dataField.FieldIndex = result.IntValue;
            dataField.HasFieldIndex = true;
        }
        else if (slot == 2)
        {
            dataField.BaseField = result.IntValue;
            dataField.HasBaseField = true;
        }
        else if (slot == 3)
        {
            dataField.BaseItem = result.IntValue;
            dataField.HasBaseItem = true;
        }
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableCalculatedField calculatedField, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        calculatedField.FieldIndex = result.IntValue;
        calculatedField.HasFieldIndex = true;
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableFilter filter, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            filter.FieldIndex = result.IntValue;
            filter.HasFieldIndex = true;
        }
        else if (slot == 2)
        {
            filter.EvaluationOrder = result.IntValue;
            filter.HasEvaluationOrder = true;
        }
        else if (slot == 3)
        {
            filter.DataFieldIndex = result.IntValue;
            filter.HasDataFieldIndex = true;
        }
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableArea area, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        area.FieldIndex = result.IntValue;
        area.HasFieldIndex = true;
    }

    internal static void SetOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableAreaReference reference, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        reference.FieldIndex = result.IntValue;
        reference.HasFieldIndex = true;
    }

    internal static void SetOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableLocation location, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            location.FirstHeaderRow = result.UnsignedIntValue;
            location.HasFirstHeaderRow = true;
        }
        else if (slot == 2)
        {
            location.FirstDataRow = result.UnsignedIntValue;
            location.HasFirstDataRow = true;
        }
        else if (slot == 3)
        {
            location.FirstDataColumn = result.UnsignedIntValue;
            location.HasFirstDataColumn = true;
        }
    }

    internal static void SetOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableDataField dataField, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        dataField.NumberFormatId = result.UnsignedIntValue;
        dataField.HasNumberFormatId = true;
    }

    internal static void SetOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableFilter filter, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        filter.Id = result.UnsignedIntValue;
        filter.HasId = true;
    }

    internal static void SetOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableFormat format, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        format.DifferentialFormatId = result.UnsignedIntValue;
        format.HasDifferentialFormatId = true;
    }

    internal static void SetOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableAreaReference reference, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        reference.Count = result.UnsignedIntValue;
        reference.HasCount = true;
    }

    internal static void SetOptionalBool(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableField field, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalBool(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            field.DataField = result.BooleanValue;
            field.HasDataField = true;
        }
        else if (slot == 2)
        {
            field.DefaultSubtotal = result.BooleanValue;
            field.HasDefaultSubtotal = true;
        }
        else if (slot == 3)
        {
            field.ShowAll = result.BooleanValue;
            field.HasShowAll = true;
        }
    }

    internal static void SetOptionalBool(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableItem item, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalBool(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        item.Hidden = result.BooleanValue;
        item.HasHidden = true;
    }

    internal static void SetOptionalBool(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableAreaReference reference, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalBool(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            reference.Selected = result.BooleanValue;
            reference.HasSelected = true;
        }
        else if (slot == 2)
        {
            reference.ByPosition = result.BooleanValue;
            reference.HasByPosition = true;
        }
    }

    internal static void SetOptionalBool(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName, XlsxPivotTableStyleInfo style, int slot)
    {
        XlsxXmlValueParseResult? result = ParseOptionalBool(part, text, elementName, attributeName);
        if (result == null)
        {
            return;
        }

        if (slot == 1)
        {
            style.ShowRowHeaders = result.BooleanValue;
            style.HasShowRowHeaders = true;
        }
        else if (slot == 2)
        {
            style.ShowColumnHeaders = result.BooleanValue;
            style.HasShowColumnHeaders = true;
        }
        else if (slot == 3)
        {
            style.ShowRowStripes = result.BooleanValue;
            style.HasShowRowStripes = true;
        }
        else if (slot == 4)
        {
            style.ShowColumnStripes = result.BooleanValue;
            style.HasShowColumnStripes = true;
        }
        else if (slot == 5)
        {
            style.ShowLastColumn = result.BooleanValue;
            style.HasShowLastColumn = true;
        }
    }

    internal static XlsxXmlValueParseResult? ParseOptionalInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName)
    {
        if (text == null || text.Length == 0)
        {
            return null;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseInteger(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return null;
        }

        return result;
    }

    internal static XlsxXmlValueParseResult? ParseOptionalUInt(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName)
    {
        if (text == null || text.Length == 0)
        {
            return null;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return null;
        }

        return result;
    }

    internal static XlsxXmlValueParseResult? ParseOptionalBool(XlsxPivotTableDefinitionPart part, string? text, string elementName, string attributeName)
    {
        if (text == null || text.Length == 0)
        {
            return null;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return null;
        }

        return result;
    }

    internal static void AddValueDiagnostics(XlsxPivotTableDefinitionPart part, XlsxXmlValueParseResult result, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyRelationshipDiagnostics(XlsxPivotTableDefinitionPart part, XlsxPivotTableCacheRelationshipResult result)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }
}
