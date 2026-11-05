using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads pivot cache definition data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxPivotCacheDefinitionReader : IXlsxPivotCacheDefinitionReader
{
    /// <summary>
    /// Initializes a new pivot cache definition reader with its default state.
    /// </summary>
    public XlsxPivotCacheDefinitionReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The pivot cache definition part produced by the operation.</returns>
    public XlsxPivotCacheDefinitionPart ReadString(string xml)
    {
        return ReadStringFromPart("/xl/pivotCache/pivotCacheDefinition1.xml", xml);
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The pivot cache definition part produced by the operation.</returns>
    public XlsxPivotCacheDefinitionPart ReadStringFromPart(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxPivotCacheDefinitionPart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The pivot cache definition part produced by the operation.</returns>
    public XlsxPivotCacheDefinitionPart ReadBytes(string partPath, byte[] bytes)
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
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The pivot cache definition part produced by the operation.</returns>
    public XlsxPivotCacheDefinitionPart ReadStream(string partPath, Stream stream)
    {
        XlsxPivotCacheDefinitionPart part = new XlsxPivotCacheDefinitionPart();
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
                    }
                    else
                    {
                        ReadChildren(part, reader);
                    }
                    continue;
                }

                reader.ReadOuterXml();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinition.missingRoot", "Pivot cache definition XML must contain a pivotCacheDefinition root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxPivotCacheDefinitionPart CreateMissingRootResult(string partPath)
    {
        XlsxPivotCacheDefinitionPart part = new XlsxPivotCacheDefinitionPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinition.missingRoot", "Pivot cache definition XML must contain a pivotCacheDefinition root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
        if (reader.LocalName != "pivotCacheDefinition" || !IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinition.invalidRoot", "Pivot cache definition XML root must be pivotCacheDefinition in the SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.RefreshedBy = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("refreshedBy"));
        part.RecordsRelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships));
        string? recordCountText = reader.GetAttribute("recordCount");
        if (recordCountText != null && recordCountText.Length > 0)
        {
            XlsxXmlValueParseResult recordCount = XlsxXmlValueParser.ParseUnsignedInteger(recordCountText);
            if (recordCount.IsValid)
            {
                part.RecordCount = recordCount.UnsignedIntValue;
                part.HasRecordCount = true;
            }
            else
            {
                foreach (XlsxDiagnostic diagnostic in recordCount.Validation.Diagnostics)
                {
                    diagnostic.SetLocation(part.PartPath, "pivotCacheDefinition", "recordCount");
                    part.AddDiagnostic(diagnostic);
                }
            }
        }

        ReadRefreshSettings(part, reader);

        ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadChildren(XlsxPivotCacheDefinitionPart part, XmlReader reader)
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
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "cacheSource")
                {
                    ReadCacheSource(part, reader);
                    continue;
                }
                if (reader.LocalName == "cacheFields")
                {
                    ReadCacheFields(part, reader);
                    continue;
                }
                if (reader.LocalName == "calculatedItems")
                {
                    ReadCalculatedItems(part, reader);
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

    internal static void ReadCacheSource(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
        XlsxPivotCacheSource source = new XlsxPivotCacheSource();
        source.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
        XlsxXmlValueParseResult connection = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("connectionId")));
        if (connection.IsValid)
        {
            source.ConnectionId = connection.UnsignedIntValue;
            source.HasConnectionId = reader.GetAttribute("connectionId") != null;
        }
        if (reader.IsEmptyElement)
        {
            reader.Read();
            part.Source = source;
            part.HasSource = true;
            return;
        }
        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                part.Source = source;
                part.HasSource = true;
                return;
            }
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "worksheetSource" && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                source.WorksheetSource.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
                source.WorksheetSource.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                source.WorksheetSource.Sheet = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sheet"));
                source.WorksheetSource.RelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships));
                source.HasWorksheetSource = true;
                reader.ReadOuterXml();
                continue;
            }
            if (reader.NodeType == XmlNodeType.Element)
            {
                source.SourceRawXml = source.SourceRawXml + reader.ReadOuterXml();
                continue;
            }
            reader.Read();
        }
    }

    internal static void ReadCacheFields(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
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
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "cacheField" && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                part.Fields.Add(ReadCacheField(part, reader));
                continue;
            }
            reader.Read();
        }
    }

    internal static XlsxPivotCacheField ReadCacheField(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
        XlsxPivotCacheField field = new XlsxPivotCacheField();
        field.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        field.Caption = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("caption"));
        string numberFormatText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("numFmtId"));
        if (numberFormatText.Length > 0)
        {
            XlsxXmlValueParseResult numberFormat = XlsxXmlValueParser.ParseUnsignedInteger(numberFormatText);
            AddValueDiagnostics(part, numberFormat, "cacheField", "numFmtId");
            if (numberFormat.IsValid)
            {
                field.NumberFormatId = numberFormat.UnsignedIntValue;
                field.HasNumberFormatId = true;
            }
        }
        string databaseText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("databaseField"));
        if (databaseText.Length > 0)
        {
            XlsxXmlValueParseResult database = XlsxXmlValueParser.ParseBoolean(databaseText);
            AddValueDiagnostics(part, database, "cacheField", "databaseField");
            if (database.IsValid)
            {
                field.DatabaseField = database.BooleanValue;
                field.HasDatabaseField = true;
            }
        }
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return field;
        }
        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return field;
            }
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "sharedItems")
            {
                ReadValues(part, reader, field.SharedItems);
                continue;
            }
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "fieldGroup")
            {
                ReadGrouping(part, reader, field);
                continue;
            }
            if (reader.NodeType == XmlNodeType.Element)
            {
                field.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }
            reader.Read();
        }
        return field;
    }

    internal static void ReadValues(XlsxPivotCacheDefinitionPart part, XmlReader reader, List<XlsxPivotCacheValue> values)
    {
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
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1)
            {
                values.Add(ReadValue(part, reader));
                reader.ReadOuterXml();
                continue;
            }
            reader.Read();
        }
    }

    internal static XlsxPivotCacheValue ReadValue(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
        XlsxPivotCacheValue value = new XlsxPivotCacheValue();
        string text = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("v"));
        string localName = reader.LocalName;
        if (localName == "s")
        {
            value.Kind = XlsxPivotCacheValueKind.String;
            value.Text = text;
        }
        else if (localName == "n")
        {
            value.Kind = XlsxPivotCacheValueKind.Number;
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDouble(text);
            AddValueDiagnostics(part, parsed, "sharedItems", "v");
            value.Number = parsed.DoubleValue;
        }
        else if (localName == "b")
        {
            value.Kind = XlsxPivotCacheValueKind.Boolean;
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text);
            AddValueDiagnostics(part, parsed, "sharedItems", "v");
            value.Boolean = parsed.BooleanValue;
        }
        else if (localName == "d")
        {
            value.Kind = XlsxPivotCacheValueKind.DateTime;
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDateTime(text);
            AddValueDiagnostics(part, parsed, "sharedItems", "v");
            value.DateTime = parsed.DateTimeValue;
        }
        else if (localName == "e")
        {
            value.Kind = XlsxPivotCacheValueKind.Error;
            value.Text = text;
        }
        else if (localName == "x")
        {
            value.Kind = XlsxPivotCacheValueKind.SharedItemIndex;
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseInteger(text);
            AddValueDiagnostics(part, parsed, "pivotCacheRecord", "v");
            value.SharedItemIndex = parsed.IntValue;
            value.HasSharedItemIndex = parsed.IsValid;
        }
        string unusedText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("u"));
        if (unusedText.Length > 0)
        {
            XlsxXmlValueParseResult unused = XlsxXmlValueParser.ParseBoolean(unusedText);
            AddValueDiagnostics(part, unused, "sharedItems", "u");
            value.Unused = unused.BooleanValue;
            value.HasUnused = unused.IsValid;
        }
        return value;
    }

    internal static void ReadGrouping(XlsxPivotCacheDefinitionPart part, XmlReader reader, XlsxPivotCacheField field)
    {
        XlsxPivotCacheGrouping grouping = new XlsxPivotCacheGrouping();
        ReadGroupingIndex(part, reader.GetAttribute("base"), grouping, true);
        ReadGroupingIndex(part, reader.GetAttribute("par"), grouping, false);
        string xml = reader.ReadOuterXml();
        ReadGroupingChildren(part, grouping, xml);
        field.Grouping = grouping;
        field.HasGrouping = true;
    }

    internal static void ReadGroupingIndex(XlsxPivotCacheDefinitionPart part, string? text, XlsxPivotCacheGrouping grouping, bool baseField)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseInteger(normalized);
        AddValueDiagnostics(part, parsed, "fieldGroup", baseField ? "base" : "par");
        if (parsed.IsValid && baseField)
        {
            grouping.BaseField = parsed.IntValue;
            grouping.HasBaseField = true;
        }
        else if (parsed.IsValid)
        {
            grouping.ParentField = parsed.IntValue;
            grouping.HasParentField = true;
        }
    }

    internal static void ReadGroupingChildren(XlsxPivotCacheDefinitionPart part, XlsxPivotCacheGrouping grouping, string xml)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "rangePr")
                    {
                        string groupBy = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("groupBy"));
                        grouping.DateGrouping = groupBy.Length > 0;
                        grouping.DateGroupBy = ParseDateGroupBy(groupBy);
                        grouping.StartNumber = ParseDoubleValue(part, reader.GetAttribute("startNum"), "startNum");
                        grouping.EndNumber = ParseDoubleValue(part, reader.GetAttribute("endNum"), "endNum");
                        grouping.Interval = ParseDoubleValue(part, reader.GetAttribute("groupInterval"), "groupInterval");
                        grouping.StartDate = ParseDateValue(part, reader.GetAttribute("startDate"), "startDate");
                        grouping.EndDate = ParseDateValue(part, reader.GetAttribute("endDate"), "endDate");
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "x")
                    {
                        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("v")));
                        if (parsed.IsValid)
                        {
                            grouping.DiscreteIndexes.Add(parsed.IntValue);
                        }
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static void ReadCalculatedItems(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
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
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "calculatedItem")
            {
                XlsxPivotCacheCalculatedItem item = new XlsxPivotCacheCalculatedItem();
                item.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                item.Formula = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("formula"));
                XlsxXmlValueParseResult field = XlsxXmlValueParser.ParseInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("field")));
                item.FieldIndex = field.IntValue;
                item.HasFieldIndex = field.IsValid;
                part.CalculatedItems.Add(item);
                reader.ReadOuterXml();
                continue;
            }
            reader.Read();
        }
    }

    internal static void ReadRefreshSettings(XlsxPivotCacheDefinitionPart part, XmlReader reader)
    {
        ReadRefreshBool(part, reader.GetAttribute("saveData"), 1);
        ReadRefreshBool(part, reader.GetAttribute("refreshOnLoad"), 2);
        ReadRefreshBool(part, reader.GetAttribute("enableRefresh"), 3);
        ReadRefreshBool(part, reader.GetAttribute("backgroundQuery"), 4);
        ReadRefreshBool(part, reader.GetAttribute("upgradeOnRefresh"), 5);
        ReadRefreshUInt(part, reader.GetAttribute("createdVersion"), 1);
        ReadRefreshUInt(part, reader.GetAttribute("refreshedVersion"), 2);
        ReadRefreshUInt(part, reader.GetAttribute("minRefreshableVersion"), 3);
        ReadRefreshUInt(part, reader.GetAttribute("missingItemsLimit"), 4);
        string dateText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("refreshedDate"));
        if (dateText.Length > 0)
        {
            XlsxXmlValueParseResult date = XlsxXmlValueParser.ParseDouble(dateText);
            AddValueDiagnostics(part, date, "pivotCacheDefinition", "refreshedDate");
            if (date.IsValid)
            {
                part.RefreshSettings.RefreshedDate = date.DoubleValue;
                part.RefreshSettings.HasRefreshedDate = true;
            }
        }
    }

    internal static void ReadRefreshBool(XlsxPivotCacheDefinitionPart part, string? text, int target)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(normalized);
        AddValueDiagnostics(part, parsed, "pivotCacheDefinition", "refresh");
        if (!parsed.IsValid)
        {
            return;
        }
        if (target == 1)
        {
            part.RefreshSettings.SaveData = parsed.BooleanValue;
            part.RefreshSettings.HasSaveData = true;
        }
        else if (target == 2)
        {
            part.RefreshSettings.RefreshOnLoad = parsed.BooleanValue;
            part.RefreshSettings.HasRefreshOnLoad = true;
        }
        else if (target == 3)
        {
            part.RefreshSettings.EnableRefresh = parsed.BooleanValue;
            part.RefreshSettings.HasEnableRefresh = true;
        }
        else if (target == 4)
        {
            part.RefreshSettings.BackgroundQuery = parsed.BooleanValue;
            part.RefreshSettings.HasBackgroundQuery = true;
        }
        else
        {
            part.RefreshSettings.UpgradeOnRefresh = parsed.BooleanValue;
            part.RefreshSettings.HasUpgradeOnRefresh = true;
        }
    }

    internal static void ReadRefreshUInt(XlsxPivotCacheDefinitionPart part, string? text, int target)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(normalized);
        AddValueDiagnostics(part, parsed, "pivotCacheDefinition", "version");
        if (!parsed.IsValid)
        {
            return;
        }
        if (target == 1)
        {
            part.RefreshSettings.CreatedVersion = parsed.UnsignedIntValue;
            part.RefreshSettings.HasCreatedVersion = true;
        }
        else if (target == 2)
        {
            part.RefreshSettings.RefreshedVersion = parsed.UnsignedIntValue;
            part.RefreshSettings.HasRefreshedVersion = true;
        }
        else if (target == 3)
        {
            part.RefreshSettings.MinimumRefreshableVersion = parsed.UnsignedIntValue;
            part.RefreshSettings.HasMinimumRefreshableVersion = true;
        }
        else
        {
            part.RefreshSettings.MissingItemsLimit = parsed.UnsignedIntValue;
            part.RefreshSettings.HasMissingItemsLimit = true;
        }
    }

    internal static double ParseDoubleValue(XlsxPivotCacheDefinitionPart part, string? text, string attribute)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return 0;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDouble(normalized);
        AddValueDiagnostics(part, parsed, "rangePr", attribute);
        return parsed.DoubleValue;
    }

    internal static System.DateTime ParseDateValue(XlsxPivotCacheDefinitionPart part, string? text, string attribute)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return default(System.DateTime);
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDateTime(normalized);
        AddValueDiagnostics(part, parsed, "rangePr", attribute);
        return parsed.DateTimeValue;
    }

    internal static XlsxPivotDateGroupBy ParseDateGroupBy(string text)
    {
        if (text == "seconds") return XlsxPivotDateGroupBy.Seconds;
        if (text == "minutes") return XlsxPivotDateGroupBy.Minutes;
        if (text == "hours") return XlsxPivotDateGroupBy.Hours;
        if (text == "months") return XlsxPivotDateGroupBy.Months;
        if (text == "quarters") return XlsxPivotDateGroupBy.Quarters;
        if (text == "years") return XlsxPivotDateGroupBy.Years;
        return XlsxPivotDateGroupBy.Days;
    }

    internal static void AddValueDiagnostics(XlsxPivotCacheDefinitionPart part, XlsxXmlValueParseResult result, string element, string attribute)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, element, attribute);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsSpreadsheetNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }

    internal static void ReadUnknownRootAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            string name = reader.LocalName;
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool relationshipId = reader.NamespaceURI == XlsxNamespaces.OfficeDocumentRelationships && name == "id";
            bool known = reader.NamespaceURI == "" && (name == "recordCount" || name == "refreshedBy" || name == "saveData" || name == "refreshOnLoad" || name == "enableRefresh" || name == "backgroundQuery" || name == "upgradeOnRefresh" || name == "createdVersion" || name == "refreshedVersion" || name == "minRefreshableVersion" || name == "missingItemsLimit" || name == "refreshedDate");
            if (!namespaceDeclaration && !relationshipId && !known)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }
}
