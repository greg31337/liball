using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes pivot table definition data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxPivotTableDefinitionWriter : IXlsxPivotTableDefinitionWriter
{
    /// <summary>
    /// Initializes a new pivot table definition writer with its default state.
    /// </summary>
    public XlsxPivotTableDefinitionWriter()
    {
    }

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
    public XlsxPivotTableCreateResult CreateFromRange(string pivotTableName, uint cacheId, string sourceSheetName, string sourceReference, string destinationReference, List<string> fieldNames)
    {
        return CreateFromSource(XlsxPivotTableSourceKind.Range, pivotTableName, cacheId, sourceSheetName, sourceReference, "", destinationReference, fieldNames);
    }

    /// <summary>
    /// Creates from table and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "pivotTableName">The pivot table name value used by the operation.</param>
    /// <param name = "cacheId">The cache id value used by the operation.</param>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "destinationReference">An A1-style cell or range reference.</param>
    /// <param name = "fieldNames">The field names value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotTableCreateResult CreateFromTable(string pivotTableName, uint cacheId, string tableName, string destinationReference, List<string> fieldNames)
    {
        return CreateFromSource(XlsxPivotTableSourceKind.Table, pivotTableName, cacheId, "", "", tableName, destinationReference, fieldNames);
    }

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
    public XlsxPivotTableCreateResult CreateFromSource(XlsxPivotTableSourceKind sourceKind, string pivotTableName, uint cacheId, string sourceSheetName, string sourceReference, string tableName, string destinationReference, List<string> fieldNames)
    {
        XlsxPivotTableCreateResult result = ValidateCreateRequest(sourceKind, pivotTableName, cacheId, sourceSheetName, sourceReference, tableName, destinationReference, fieldNames);
        if (!result.IsValid)
        {
            return result;
        }

        string normalizedName = XlsxCellValueConverter.NormalizeText(pivotTableName);
        string normalizedSheetName = XlsxCellValueConverter.NormalizeText(sourceSheetName);
        string normalizedSourceReference = XlsxCellValueConverter.NormalizeText(sourceReference);
        string normalizedTableName = XlsxCellValueConverter.NormalizeText(tableName);
        string normalizedDestination = XlsxCellValueConverter.NormalizeText(destinationReference);
        string pivotTablePath = XlsxPackagePaths.PivotTablesDirectory + "pivotTable" + cacheId.ToString(CultureInfo.InvariantCulture) + ".xml";
        string pivotCachePath = "/xl/pivotCache/pivotCacheDefinition" + cacheId.ToString(CultureInfo.InvariantCulture) + ".xml";
        string pivotCacheRecordsPath = "/xl/pivotCache/pivotCacheRecords" + cacheId.ToString(CultureInfo.InvariantCulture) + ".xml";
        XlsxRelationshipsPart pivotTableRelationships = CreatePivotTableRelationships(pivotTablePath, pivotCachePath);
        XlsxRelationshipsPart pivotCacheRelationships = CreatePivotCacheRelationships(pivotCachePath, pivotCacheRecordsPath);
        string pivotTableXml = CreatePivotTableDefinitionXml(normalizedName, cacheId, normalizedDestination, fieldNames);
        string cacheDefinitionXml = CreatePivotCacheDefinitionXml(sourceKind, normalizedSheetName, normalizedSourceReference, normalizedTableName, fieldNames);
        string cacheRecordsXml = CreatePivotCacheRecordsXml();
        XlsxPivotTableDefinitionPart pivotTableDefinition = new XlsxPivotTableDefinitionReader().ReadStringWithRelationships(pivotTablePath, pivotTableXml, pivotTableRelationships);
        XlsxPivotCacheDefinitionPart pivotCacheDefinition = new XlsxPivotCacheDefinitionReader().ReadStringFromPart(pivotCachePath, cacheDefinitionXml);
        CopyCreateDiagnostics(result, pivotTableDefinition.Validation);
        CopyCreateDiagnostics(result, pivotCacheDefinition.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        result.PivotTableDefinition = pivotTableDefinition;
        result.HasPivotTableDefinition = true;
        result.PivotCacheDefinition = pivotCacheDefinition;
        result.HasPivotCacheDefinition = true;
        result.PivotCacheRecordsXml = cacheRecordsXml;
        result.PivotCacheRecordsPath = pivotCacheRecordsPath;
        result.PivotTableRelationships = pivotTableRelationships;
        result.HasPivotTableRelationships = true;
        result.PivotCacheRelationships = pivotCacheRelationships;
        result.HasPivotCacheRelationships = true;
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotTableDefinitionWriteResult WriteString(XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    internal static XlsxPivotTableCreateResult ValidateCreateRequest(XlsxPivotTableSourceKind sourceKind, string pivotTableName, uint cacheId, string sourceSheetName, string sourceReference, string tableName, string destinationReference, List<string> fieldNames)
    {
        XlsxPivotTableCreateResult result = new XlsxPivotTableCreateResult();
        string normalizedName = XlsxCellValueConverter.NormalizeText(pivotTableName);
        if (normalizedName.Length == 0)
        {
            result.AddError("pivotTableWriter.nameMissing", "Pivot table name must not be empty.");
        }

        if (cacheId == 0)
        {
            result.AddError("pivotTableWriter.cacheIdMissing", "Pivot table cache id must be greater than zero.");
        }

        string normalizedDestination = XlsxCellValueConverter.NormalizeText(destinationReference);
        if (normalizedDestination.Length == 0)
        {
            result.AddError("pivotTableWriter.destinationMissing", "Pivot table destination reference must not be empty.");
        }
        else
        {
            XlsxA1RangeReferenceResult destination = XlsxA1RangeReferenceConverter.Parse(normalizedDestination);
            AddRangeDiagnostics(result, destination, "pivotTableDefinition", "location");
        }

        if (sourceKind == XlsxPivotTableSourceKind.Range)
        {
            ValidateRangeSource(result, sourceSheetName, sourceReference);
        }
        else if (sourceKind == XlsxPivotTableSourceKind.Table)
        {
            ValidateTableSource(result, tableName);
        }
        else
        {
            result.AddError("pivotTableWriter.sourceKindUnsupported", "Pivot table source kind must be range or table.");
        }

        ValidateFieldNames(result, fieldNames);
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static void ValidateRangeSource(XlsxPivotTableCreateResult result, string sourceSheetName, string sourceReference)
    {
        string normalizedSheetName = XlsxCellValueConverter.NormalizeText(sourceSheetName);
        if (normalizedSheetName.Length == 0)
        {
            result.AddError("pivotTableWriter.sourceSheetMissing", "Range pivot table source must have a worksheet name.");
        }

        string normalizedSourceReference = XlsxCellValueConverter.NormalizeText(sourceReference);
        if (normalizedSourceReference.Length == 0)
        {
            result.AddError("pivotTableWriter.sourceReferenceMissing", "Range pivot table source must have a source reference.");
            return;
        }

        XlsxA1RangeReferenceResult source = XlsxA1RangeReferenceConverter.Parse(normalizedSourceReference);
        AddRangeDiagnostics(result, source, "worksheetSource", "ref");
    }

    internal static void ValidateTableSource(XlsxPivotTableCreateResult result, string tableName)
    {
        string normalizedTableName = XlsxCellValueConverter.NormalizeText(tableName);
        if (normalizedTableName.Length == 0)
        {
            result.AddError("pivotTableWriter.tableNameMissing", "Table pivot table source must have a table name.");
            return;
        }

        XlsxValidationResult tableNameResult = XlsxWorksheetPart.ValidateTableName(normalizedTableName);
        CopyCreateDiagnostics(result, tableNameResult);
    }

    internal static void ValidateFieldNames(XlsxPivotTableCreateResult result, List<string> fieldNames)
    {
        if (fieldNames == null)
        {
            result.AddError("pivotTableWriter.fieldNamesNull", "Pivot table field name list must not be null.");
            return;
        }

        if (fieldNames.Count == 0)
        {
            result.AddError("pivotTableWriter.fieldNamesEmpty", "Pivot table field name list must contain at least one field.");
            return;
        }

        Dictionary<string, bool> names = new Dictionary<string, bool>();
        int index = 0;
        foreach (string fieldName in fieldNames)
        {
            string normalized = XlsxCellValueConverter.NormalizeText(fieldName);
            if (normalized.Length == 0)
            {
                XlsxDiagnostic diagnostic = result.AddError("pivotTableWriter.fieldNameMissing", "Pivot table field names must not be empty.");
                diagnostic.SetLocation("", "cacheField", index.ToString(CultureInfo.InvariantCulture));
            }
            else if (names.ContainsKey(normalized))
            {
                XlsxDiagnostic diagnostic = result.AddError("pivotTableWriter.fieldNameDuplicate", "Pivot table field names must be unique.");
                diagnostic.SetLocation("", "cacheField", index.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                names.Add(normalized, true);
            }

            index = index + 1;
        }
    }

    internal static XlsxRelationshipsPart CreatePivotTableRelationships(string pivotTablePath, string pivotCachePath)
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = XlsxPackagePath.GetRelationshipSourcePartPath(pivotTablePath);
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(pivotTablePath);
        relationships.Relationships.Add(CreateRelationship("rIdPivotCache1", XlsxRelationshipTypes.PivotCacheDefinition, GetRelativePivotCacheTarget(pivotCachePath)));
        ResolveRelationshipTargets(relationships);
        return relationships;
    }

    internal static XlsxRelationshipsPart CreatePivotCacheRelationships(string pivotCachePath, string pivotCacheRecordsPath)
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = XlsxPackagePath.GetRelationshipSourcePartPath(pivotCachePath);
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(pivotCachePath);
        relationships.Relationships.Add(CreateRelationship("rIdRecords1", XlsxRelationshipTypes.PivotCacheRecords, GetFileName(pivotCacheRecordsPath)));
        ResolveRelationshipTargets(relationships);
        return relationships;
    }

    internal static string CreatePivotTableDefinitionXml(string pivotTableName, uint cacheId, string destinationReference, List<string> fieldNames)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
            {
                writer.WriteStartElement("", "pivotTableDefinition", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("name", pivotTableName);
                writer.WriteAttributeString("cacheId", cacheId.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("dataCaption", "Values");
                writer.WriteStartElement("", "location", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("ref", destinationReference);
                writer.WriteAttributeString("firstHeaderRow", "1");
                writer.WriteAttributeString("firstDataRow", "2");
                writer.WriteAttributeString("firstDataCol", "1");
                writer.WriteEndElement();
                writer.WriteStartElement("", "pivotFields", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", fieldNames.Count.ToString(CultureInfo.InvariantCulture));
                foreach (string fieldName in fieldNames)
                {
                    writer.WriteStartElement("", "pivotField", XlsxNamespaces.Spreadsheet);
                    writer.WriteAttributeString("name", XlsxCellValueConverter.NormalizeText(fieldName));
                    writer.WriteAttributeString("showAll", "0");
                    writer.WriteStartElement("", "items", XlsxNamespaces.Spreadsheet);
                    writer.WriteAttributeString("count", "1");
                    writer.WriteStartElement("", "item", XlsxNamespaces.Spreadsheet);
                    writer.WriteAttributeString("t", "default");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteStartElement("", "rowItems", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("", "i", XlsxNamespaces.Spreadsheet);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("", "colItems", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("", "i", XlsxNamespaces.Spreadsheet);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("", "pivotTableStyleInfo", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("name", "PivotStyleMedium9");
                writer.WriteAttributeString("showRowHeaders", "1");
                writer.WriteAttributeString("showColHeaders", "1");
                writer.WriteAttributeString("showRowStripes", "0");
                writer.WriteAttributeString("showColStripes", "1");
                writer.WriteAttributeString("showLastColumn", "0");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static string CreatePivotCacheDefinitionXml(XlsxPivotTableSourceKind sourceKind, string sourceSheetName, string sourceReference, string tableName, List<string> fieldNames)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
            {
                writer.WriteStartElement("", "pivotCacheDefinition", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
                writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, "rIdRecords1");
                writer.WriteAttributeString("refreshOnLoad", "1");
                writer.WriteAttributeString("recordCount", "0");
                writer.WriteStartElement("", "cacheSource", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("type", "worksheet");
                writer.WriteStartElement("", "worksheetSource", XlsxNamespaces.Spreadsheet);
                if (sourceKind == XlsxPivotTableSourceKind.Range)
                {
                    writer.WriteAttributeString("ref", sourceReference);
                    writer.WriteAttributeString("sheet", sourceSheetName);
                }
                else
                {
                    writer.WriteAttributeString("name", tableName);
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("", "cacheFields", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", fieldNames.Count.ToString(CultureInfo.InvariantCulture));
                foreach (string fieldName in fieldNames)
                {
                    writer.WriteStartElement("", "cacheField", XlsxNamespaces.Spreadsheet);
                    writer.WriteAttributeString("name", XlsxCellValueConverter.NormalizeText(fieldName));
                    writer.WriteAttributeString("numFmtId", "0");
                    writer.WriteStartElement("", "sharedItems", XlsxNamespaces.Spreadsheet);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static string CreatePivotCacheRecordsXml()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
            {
                writer.WriteStartElement("", "pivotCacheRecords", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", "0");
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static XlsxRelationship CreateRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        return relationship;
    }

    internal static void ResolveRelationshipTargets(XlsxRelationshipsPart relationships)
    {
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(relationships.SourcePartPath, relationship.Target, false);
            if (target.Validation.IsValid)
            {
                relationship.TargetPath = target.Path;
                relationship.TargetFragment = target.Fragment;
                relationship.ExternalTarget = target.ExternalTarget;
            }
        }
    }

    internal static string GetRelativePivotCacheTarget(string pivotCachePath)
    {
        return "../pivotCache/" + GetFileName(pivotCachePath);
    }

    internal static string GetFileName(string path)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(path);
        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return normalized;
        }

        return normalized.Substring(slashIndex + 1);
    }

    internal static void AddRangeDiagnostics(XlsxPivotTableCreateResult result, XlsxA1RangeReferenceResult range, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyCreateDiagnostics(XlsxPivotTableCreateResult result, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotTableDefinitionWriteResult WriteBytes(XlsxPivotTableDefinitionPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxPivotTableDefinitionWriteResult result = WriteStream(stream, part);
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
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotTableDefinitionWriteResult WriteStream(Stream stream, XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = ValidatePivotTableDefinition(part);
        if (!result.IsValid)
        {
            return result;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
        stream.Write(bytes, 0, bytes.Length);
        return result;
    }

    internal static XlsxPivotTableDefinitionWriteResult WriteStructuredString(XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = WriteStructuredBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    internal static XlsxPivotTableDefinitionWriteResult WriteStructuredBytes(XlsxPivotTableDefinitionPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxPivotTableDefinitionWriteResult result = WriteStructuredStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    internal static XlsxPivotTableDefinitionWriteResult WriteStructuredStream(Stream stream, XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = ValidateStructuredPivotTableDefinition(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartElement("", "pivotTableDefinition", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("name", part.Name);
            writer.WriteAttributeString("cacheId", part.CacheId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("dataCaption", "Values");
            WriteLocation(writer, part);
            WritePivotFields(writer, part.Fields);
            WriteFieldReferences(writer, "rowFields", "field", "x", part.RowFields);
            WriteFieldReferences(writer, "colFields", "field", "x", part.ColumnFields);
            WriteFieldReferences(writer, "pageFields", "pageField", "fld", part.PageFields);
            WriteDataFields(writer, part.DataFields);
            WriteCalculatedFields(writer, part.CalculatedFields);
            WriteFilters(writer, part.Filters);
            WriteFormats(writer, part.Formats);
            WritePivotItems(writer, "rowItems");
            WritePivotItems(writer, "colItems");
            WriteStyleInfo(writer, part);
            writer.WriteEndElement();
        }

        return result;
    }

    internal static void WriteLocation(XmlWriter writer, XlsxPivotTableDefinitionPart part)
    {
        if (!part.HasLocation || part.Location == null)
        {
            return;
        }

        writer.WriteStartElement("", "location", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("ref", part.Location.Reference);
        if (part.Location.HasFirstHeaderRow)
        {
            writer.WriteAttributeString("firstHeaderRow", part.Location.FirstHeaderRow.ToString(CultureInfo.InvariantCulture));
        }

        if (part.Location.HasFirstDataRow)
        {
            writer.WriteAttributeString("firstDataRow", part.Location.FirstDataRow.ToString(CultureInfo.InvariantCulture));
        }

        if (part.Location.HasFirstDataColumn)
        {
            writer.WriteAttributeString("firstDataCol", part.Location.FirstDataColumn.ToString(CultureInfo.InvariantCulture));
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotFields(XmlWriter writer, List<XlsxPivotTableField> fields)
    {
        if (fields == null)
        {
            return;
        }

        writer.WriteStartElement("", "pivotFields", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", fields.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableField field in fields)
        {
            WritePivotField(writer, field);
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotField(XmlWriter writer, XlsxPivotTableField field)
    {
        writer.WriteStartElement("", "pivotField", XlsxNamespaces.Spreadsheet);
        if (field != null)
        {
            if (field.Name != null && field.Name.Length > 0)
            {
                writer.WriteAttributeString("name", field.Name);
            }

            if (field.Axis != null && field.Axis.Length > 0)
            {
                writer.WriteAttributeString("axis", field.Axis);
            }

            if (field.SortType != null && field.SortType.Length > 0)
            {
                writer.WriteAttributeString("sortType", field.SortType);
            }

            if (field.HasDataField)
            {
                writer.WriteAttributeString("dataField", XlsxXmlValueWriter.WriteBoolean(field.DataField));
            }

            if (field.HasDefaultSubtotal)
            {
                writer.WriteAttributeString("defaultSubtotal", XlsxXmlValueWriter.WriteBoolean(field.DefaultSubtotal));
            }

            if (field.HasShowAll)
            {
                writer.WriteAttributeString("showAll", XlsxXmlValueWriter.WriteBoolean(field.ShowAll));
            }

            WritePivotFieldItems(writer, field);
            WritePivotFieldGroupItems(writer, field);
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotFieldItems(XmlWriter writer, XlsxPivotTableField field)
    {
        if (field.Items == null || field.Items.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "items", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", field.Items.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableItem item in field.Items)
        {
            writer.WriteStartElement("", "item", XlsxNamespaces.Spreadsheet);
            if (item != null)
            {
                if (item.HasIndex)
                {
                    writer.WriteAttributeString("x", item.Index.ToString(CultureInfo.InvariantCulture));
                }

                if (item.Type != null && item.Type.Length > 0)
                {
                    writer.WriteAttributeString("t", item.Type);
                }

                if (item.HasHidden)
                {
                    writer.WriteAttributeString("h", XlsxXmlValueWriter.WriteBoolean(item.Hidden));
                }
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotFieldGroupItems(XmlWriter writer, XlsxPivotTableField field)
    {
        if (!field.HasGrouping || field.GroupItems == null || field.GroupItems.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "groupItems", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", field.GroupItems.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableItem item in field.GroupItems)
        {
            string elementName = "s";
            if (item != null && item.Type != null && item.Type.Length > 0)
            {
                elementName = item.Type;
            }

            writer.WriteStartElement("", elementName, XlsxNamespaces.Spreadsheet);
            if (item != null && item.Name != null && item.Name.Length > 0)
            {
                writer.WriteAttributeString("v", item.Name);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteFieldReferences(XmlWriter writer, string containerName, string elementName, string fieldAttributeName, List<XlsxPivotTableFieldReference> references)
    {
        if (references == null || references.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", containerName, XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", references.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableFieldReference reference in references)
        {
            writer.WriteStartElement("", elementName, XlsxNamespaces.Spreadsheet);
            if (reference != null)
            {
                if (reference.HasFieldIndex)
                {
                    writer.WriteAttributeString(fieldAttributeName, reference.FieldIndex.ToString(CultureInfo.InvariantCulture));
                }

                if (elementName == "pageField" && reference.HasItemIndex)
                {
                    writer.WriteAttributeString("item", reference.ItemIndex.ToString(CultureInfo.InvariantCulture));
                }

                if (reference.Name != null && reference.Name.Length > 0 && elementName == "pageField")
                {
                    writer.WriteAttributeString("name", reference.Name);
                }
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteDataFields(XmlWriter writer, List<XlsxPivotTableDataField> dataFields)
    {
        if (dataFields == null || dataFields.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "dataFields", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", dataFields.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableDataField dataField in dataFields)
        {
            writer.WriteStartElement("", "dataField", XlsxNamespaces.Spreadsheet);
            if (dataField != null)
            {
                if (dataField.Name != null && dataField.Name.Length > 0)
                {
                    writer.WriteAttributeString("name", dataField.Name);
                }

                if (dataField.HasFieldIndex)
                {
                    writer.WriteAttributeString("fld", dataField.FieldIndex.ToString(CultureInfo.InvariantCulture));
                }

                string subtotal = dataField.Subtotal;
                if (dataField.HasSummaryFunction)
                {
                    subtotal = XlsxPivotService.GetSummaryToken(dataField.SummaryFunction);
                }
                if (subtotal != null && subtotal.Length > 0)
                {
                    writer.WriteAttributeString("subtotal", subtotal);
                }

                if (dataField.HasBaseField)
                {
                    writer.WriteAttributeString("baseField", dataField.BaseField.ToString(CultureInfo.InvariantCulture));
                }

                if (dataField.HasBaseItem)
                {
                    writer.WriteAttributeString("baseItem", dataField.BaseItem.ToString(CultureInfo.InvariantCulture));
                }

                if (dataField.HasNumberFormatId)
                {
                    writer.WriteAttributeString("numFmtId", dataField.NumberFormatId.ToString(CultureInfo.InvariantCulture));
                }
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteCalculatedFields(XmlWriter writer, List<XlsxPivotTableCalculatedField> calculatedFields)
    {
        if (calculatedFields == null || calculatedFields.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "calculatedFields", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", calculatedFields.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableCalculatedField calculatedField in calculatedFields)
        {
            writer.WriteStartElement("", "calculatedField", XlsxNamespaces.Spreadsheet);
            if (calculatedField != null)
            {
                if (calculatedField.Name != null && calculatedField.Name.Length > 0)
                {
                    writer.WriteAttributeString("name", calculatedField.Name);
                }

                if (calculatedField.Formula != null && calculatedField.Formula.Length > 0)
                {
                    writer.WriteAttributeString("formula", calculatedField.Formula);
                }

                if (calculatedField.HasFieldIndex)
                {
                    writer.WriteAttributeString("fld", calculatedField.FieldIndex.ToString(CultureInfo.InvariantCulture));
                }
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteFilters(XmlWriter writer, List<XlsxPivotTableFilter> filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "pivotFilters", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", filters.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableFilter filter in filters)
        {
            WriteFilter(writer, filter);
        }

        writer.WriteEndElement();
    }

    internal static void WriteFormats(XmlWriter writer, List<XlsxPivotTableFormat> formats)
    {
        if (formats == null || formats.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "formats", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", formats.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotTableFormat format in formats)
        {
            writer.WriteStartElement("", "format", XlsxNamespaces.Spreadsheet);
            if (format != null)
            {
                if (format.Action != null && format.Action.Length > 0)
                {
                    writer.WriteAttributeString("action", format.Action);
                }
                if (format.HasDifferentialFormatId)
                {
                    writer.WriteAttributeString("dxfId", format.DifferentialFormatId.ToString(CultureInfo.InvariantCulture));
                }
                if (format.HasArea)
                {
                    WriteArea(writer, format.Area);
                }
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteFilter(XmlWriter writer, XlsxPivotTableFilter filter)
    {
        writer.WriteStartElement("", "filter", XlsxNamespaces.Spreadsheet);
        if (filter != null)
        {
            if (filter.HasFieldIndex)
            {
                writer.WriteAttributeString("fld", filter.FieldIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (filter.Type != null && filter.Type.Length > 0)
            {
                writer.WriteAttributeString("type", filter.Type);
            }

            if (filter.HasId)
            {
                writer.WriteAttributeString("id", filter.Id.ToString(CultureInfo.InvariantCulture));
            }

            if (filter.Name != null && filter.Name.Length > 0)
            {
                writer.WriteAttributeString("name", filter.Name);
            }

            if (filter.HasDataFieldIndex)
            {
                writer.WriteAttributeString("iMeasureFld", filter.DataFieldIndex.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("stringValue1", XlsxXmlValueWriter.WriteDouble(filter.NumericValue));
            }
            else if (filter.HasFilterKind && filter.LabelValue != null && filter.LabelValue.Length > 0)
            {
                writer.WriteAttributeString("stringValue1", filter.LabelValue);
            }

            if (filter.Areas != null)
            {
                foreach (XlsxPivotTableArea area in filter.Areas)
                {
                    WriteArea(writer, area);
                }
            }
        }

        writer.WriteEndElement();
    }

    internal static void WriteArea(XmlWriter writer, XlsxPivotTableArea area)
    {
        writer.WriteStartElement("", "pivotArea", XlsxNamespaces.Spreadsheet);
        if (area != null)
        {
            if (area.Type != null && area.Type.Length > 0)
            {
                writer.WriteAttributeString("type", area.Type);
            }

            if (area.HasFieldIndex)
            {
                writer.WriteAttributeString("field", area.FieldIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (area.Axis != null && area.Axis.Length > 0)
            {
                writer.WriteAttributeString("axis", area.Axis);
            }

            if (area.References != null && area.References.Count > 0)
            {
                writer.WriteStartElement("", "references", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("count", area.References.Count.ToString(CultureInfo.InvariantCulture));
                foreach (XlsxPivotTableAreaReference reference in area.References)
                {
                    WriteAreaReference(writer, reference);
                }

                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    internal static void WriteAreaReference(XmlWriter writer, XlsxPivotTableAreaReference reference)
    {
        writer.WriteStartElement("", "reference", XlsxNamespaces.Spreadsheet);
        if (reference != null)
        {
            if (reference.HasFieldIndex)
            {
                writer.WriteAttributeString("field", reference.FieldIndex.ToString(CultureInfo.InvariantCulture));
            }

            if (reference.HasCount)
            {
                writer.WriteAttributeString("count", reference.Count.ToString(CultureInfo.InvariantCulture));
            }

            if (reference.HasSelected)
            {
                writer.WriteAttributeString("selected", XlsxXmlValueWriter.WriteBoolean(reference.Selected));
            }

            if (reference.HasByPosition)
            {
                writer.WriteAttributeString("byPosition", XlsxXmlValueWriter.WriteBoolean(reference.ByPosition));
            }
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotItems(XmlWriter writer, string elementName)
    {
        writer.WriteStartElement("", elementName, XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("", "i", XlsxNamespaces.Spreadsheet);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteStyleInfo(XmlWriter writer, XlsxPivotTableDefinitionPart part)
    {
        if (!part.HasStyleInfo || part.StyleInfo == null)
        {
            return;
        }

        writer.WriteStartElement("", "pivotTableStyleInfo", XlsxNamespaces.Spreadsheet);
        if (part.StyleInfo.Name != null && part.StyleInfo.Name.Length > 0)
        {
            writer.WriteAttributeString("name", part.StyleInfo.Name);
        }

        if (part.StyleInfo.HasShowRowHeaders)
        {
            writer.WriteAttributeString("showRowHeaders", XlsxXmlValueWriter.WriteBoolean(part.StyleInfo.ShowRowHeaders));
        }

        if (part.StyleInfo.HasShowColumnHeaders)
        {
            writer.WriteAttributeString("showColHeaders", XlsxXmlValueWriter.WriteBoolean(part.StyleInfo.ShowColumnHeaders));
        }

        if (part.StyleInfo.HasShowRowStripes)
        {
            writer.WriteAttributeString("showRowStripes", XlsxXmlValueWriter.WriteBoolean(part.StyleInfo.ShowRowStripes));
        }

        if (part.StyleInfo.HasShowColumnStripes)
        {
            writer.WriteAttributeString("showColStripes", XlsxXmlValueWriter.WriteBoolean(part.StyleInfo.ShowColumnStripes));
        }

        if (part.StyleInfo.HasShowLastColumn)
        {
            writer.WriteAttributeString("showLastColumn", XlsxXmlValueWriter.WriteBoolean(part.StyleInfo.ShowLastColumn));
        }

        writer.WriteEndElement();
    }

    internal static XlsxPivotTableDefinitionWriteResult ValidateStructuredPivotTableDefinition(XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = new XlsxPivotTableDefinitionWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.partNull", "Pivot table definition part must not be null."));
            return result;
        }

        if (part.Name == null || part.Name.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.nameMissing", "Pivot table definition must have a name."));
        }

        if (!part.HasCacheId)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.cacheIdMissing", "Pivot table definition must have a cache id."));
        }

        if (part.Fields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.fieldsNull", "Pivot table field list must not be null."));
        }

        if (part.RowFields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.rowFieldsNull", "Pivot table row field list must not be null."));
        }

        if (part.ColumnFields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.columnFieldsNull", "Pivot table column field list must not be null."));
        }

        if (part.PageFields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.pageFieldsNull", "Pivot table page field list must not be null."));
        }

        if (part.DataFields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.dataFieldsNull", "Pivot table data field list must not be null."));
        }

        if (part.CalculatedFields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.calculatedFieldsNull", "Pivot table calculated field list must not be null."));
        }

        if (part.Filters == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.filtersNull", "Pivot table filter list must not be null."));
        }

        return result;
    }

    internal static XlsxPivotTableDefinitionWriteResult ValidatePivotTableDefinition(XlsxPivotTableDefinitionPart part)
    {
        XlsxPivotTableDefinitionWriteResult result = new XlsxPivotTableDefinitionWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.partNull", "Pivot table definition part must not be null."));
            return result;
        }

        if (part.RawXml == null || part.RawXml.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotTableDefinitionWriter.rawXmlMissing", "Pivot table definition writer preserves raw XML and requires RawXml."));
        }

        return result;
    }
}
