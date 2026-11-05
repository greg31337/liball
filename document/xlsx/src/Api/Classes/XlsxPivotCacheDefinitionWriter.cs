using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes pivot cache definition data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxPivotCacheDefinitionWriter : IXlsxPivotCacheDefinitionWriter
{
    /// <summary>
    /// Initializes a new pivot cache definition writer with its default state.
    /// </summary>
    public XlsxPivotCacheDefinitionWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotCacheDefinitionWriteResult WriteString(XlsxPivotCacheDefinitionPart part)
    {
        XlsxPivotCacheDefinitionWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxPivotCacheDefinitionWriteResult WriteBytes(XlsxPivotCacheDefinitionPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxPivotCacheDefinitionWriteResult result = WriteStream(stream, part);
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
    public XlsxPivotCacheDefinitionWriteResult WriteStream(Stream stream, XlsxPivotCacheDefinitionPart part)
    {
        XlsxPivotCacheDefinitionWriteResult result = ValidatePivotCacheDefinition(part);
        if (!result.IsValid)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        WriteStructured(stream, part);
        return result;
    }

    internal static XlsxPivotCacheDefinitionWriteResult ValidatePivotCacheDefinition(XlsxPivotCacheDefinitionPart part)
    {
        XlsxPivotCacheDefinitionWriteResult result = new XlsxPivotCacheDefinitionWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinitionWriter.partNull", "Pivot cache definition part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }
        if (!part.HasSource || part.Source == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinitionWriter.sourceMissing", "Typed pivot cache definitions must contain a cache source."));
        }
        if (part.Fields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "pivotCacheDefinitionWriter.fieldsNull", "Typed pivot cache field list must not be null."));
        }

        return result;
    }

    internal static void WriteStructured(Stream stream, XlsxPivotCacheDefinitionPart part)
    {
        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartElement("", "pivotCacheDefinition", XlsxNamespaces.Spreadsheet);
            if (part.RecordsRelationshipId != null && part.RecordsRelationshipId.Length > 0)
            {
                writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
                writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, part.RecordsRelationshipId);
            }
            if (part.RefreshedBy != null && part.RefreshedBy.Length > 0)
            {
                writer.WriteAttributeString("refreshedBy", part.RefreshedBy);
            }
            if (part.HasRecordCount)
            {
                writer.WriteAttributeString("recordCount", XlsxXmlValueWriter.WriteUnsignedInteger(part.RecordCount));
            }
            WriteRefreshAttributes(writer, part.RefreshSettings);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteSource(writer, part.Source);
            WriteFields(writer, part.Fields);
            WriteCalculatedItems(writer, part.CalculatedItems);
            WriteRawXml(writer, part.ExtensionListRawXml);
            if (part.UnknownXml != null)
            {
                foreach (string xml in part.UnknownXml)
                {
                    WriteRawXml(writer, xml);
                }
            }
            writer.WriteEndElement();
        }
    }

    internal static void WriteRefreshAttributes(XmlWriter writer, XlsxPivotCacheRefreshSettings settings)
    {
        if (settings == null)
        {
            return;
        }
        WriteBoolAttribute(writer, "saveData", settings.SaveData, settings.HasSaveData);
        WriteBoolAttribute(writer, "refreshOnLoad", settings.RefreshOnLoad, settings.HasRefreshOnLoad);
        WriteBoolAttribute(writer, "enableRefresh", settings.EnableRefresh, settings.HasEnableRefresh);
        WriteBoolAttribute(writer, "backgroundQuery", settings.BackgroundQuery, settings.HasBackgroundQuery);
        WriteBoolAttribute(writer, "upgradeOnRefresh", settings.UpgradeOnRefresh, settings.HasUpgradeOnRefresh);
        WriteUIntAttribute(writer, "createdVersion", settings.CreatedVersion, settings.HasCreatedVersion);
        WriteUIntAttribute(writer, "refreshedVersion", settings.RefreshedVersion, settings.HasRefreshedVersion);
        WriteUIntAttribute(writer, "minRefreshableVersion", settings.MinimumRefreshableVersion, settings.HasMinimumRefreshableVersion);
        WriteUIntAttribute(writer, "missingItemsLimit", settings.MissingItemsLimit, settings.HasMissingItemsLimit);
        if (settings.HasRefreshedDate)
        {
            writer.WriteAttributeString("refreshedDate", XlsxXmlValueWriter.WriteDouble(settings.RefreshedDate));
        }
    }

    internal static void WriteSource(XmlWriter writer, XlsxPivotCacheSource source)
    {
        writer.WriteStartElement("", "cacheSource", XlsxNamespaces.Spreadsheet);
        if (source.Type != null && source.Type.Length > 0)
        {
            writer.WriteAttributeString("type", source.Type);
        }
        WriteUIntAttribute(writer, "connectionId", source.ConnectionId, source.HasConnectionId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, source.UnknownAttributes);
        if (source.HasWorksheetSource && source.WorksheetSource != null)
        {
            XlsxPivotCacheWorksheetSource worksheet = source.WorksheetSource;
            writer.WriteStartElement("", "worksheetSource", XlsxNamespaces.Spreadsheet);
            WriteStringAttribute(writer, "ref", worksheet.Reference);
            WriteStringAttribute(writer, "name", worksheet.Name);
            WriteStringAttribute(writer, "sheet", worksheet.Sheet);
            if (worksheet.RelationshipId != null && worksheet.RelationshipId.Length > 0)
            {
                writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, worksheet.RelationshipId);
            }
            writer.WriteEndElement();
        }
        WriteRawXml(writer, source.SourceRawXml);
        writer.WriteEndElement();
    }

    internal static void WriteFields(XmlWriter writer, List<XlsxPivotCacheField> fields)
    {
        writer.WriteStartElement("", "cacheFields", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", fields.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotCacheField field in fields)
        {
            writer.WriteStartElement("", "cacheField", XlsxNamespaces.Spreadsheet);
            WriteStringAttribute(writer, "name", field.Name);
            WriteStringAttribute(writer, "caption", field.Caption);
            WriteUIntAttribute(writer, "numFmtId", field.NumberFormatId, field.HasNumberFormatId);
            WriteBoolAttribute(writer, "databaseField", field.DatabaseField, field.HasDatabaseField);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, field.UnknownAttributes);
            WriteValues(writer, "sharedItems", field.SharedItems);
            if (field.HasGrouping)
            {
                WriteGrouping(writer, field.Grouping);
            }
            if (field.UnknownXml != null)
            {
                foreach (string xml in field.UnknownXml)
                {
                    WriteRawXml(writer, xml);
                }
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteValues(XmlWriter writer, string container, List<XlsxPivotCacheValue> values)
    {
        writer.WriteStartElement("", container, XlsxNamespaces.Spreadsheet);
        if (values != null && values.Count > 0)
        {
            writer.WriteAttributeString("count", values.Count.ToString(CultureInfo.InvariantCulture));
            foreach (XlsxPivotCacheValue value in values)
            {
                WriteValue(writer, value);
            }
        }
        writer.WriteEndElement();
    }

    internal static void WriteValue(XmlWriter writer, XlsxPivotCacheValue value)
    {
        string elementName = "m";
        string text = "";
        if (value.Kind == XlsxPivotCacheValueKind.String)
        {
            elementName = "s";
            text = value.Text;
        }
        else if (value.Kind == XlsxPivotCacheValueKind.Number)
        {
            elementName = "n";
            text = XlsxXmlValueWriter.WriteDouble(value.Number);
        }
        else if (value.Kind == XlsxPivotCacheValueKind.Boolean)
        {
            elementName = "b";
            text = XlsxXmlValueWriter.WriteBoolean(value.Boolean);
        }
        else if (value.Kind == XlsxPivotCacheValueKind.DateTime)
        {
            elementName = "d";
            text = XlsxXmlValueWriter.WriteDateTime(value.DateTime);
        }
        else if (value.Kind == XlsxPivotCacheValueKind.Error)
        {
            elementName = "e";
            text = value.Text;
        }
        else if (value.Kind == XlsxPivotCacheValueKind.SharedItemIndex)
        {
            elementName = "x";
            text = value.SharedItemIndex.ToString(CultureInfo.InvariantCulture);
        }
        writer.WriteStartElement("", elementName, XlsxNamespaces.Spreadsheet);
        if (text != null && text.Length > 0)
        {
            writer.WriteAttributeString("v", text);
        }
        WriteBoolAttribute(writer, "u", value.Unused, value.HasUnused);
        writer.WriteEndElement();
    }

    internal static void WriteGrouping(XmlWriter writer, XlsxPivotCacheGrouping grouping)
    {
        writer.WriteStartElement("", "fieldGroup", XlsxNamespaces.Spreadsheet);
        if (grouping.HasBaseField)
        {
            writer.WriteAttributeString("base", grouping.BaseField.ToString(CultureInfo.InvariantCulture));
        }
        if (grouping.HasParentField)
        {
            writer.WriteAttributeString("par", grouping.ParentField.ToString(CultureInfo.InvariantCulture));
        }
        if (grouping.DiscreteIndexes != null && grouping.DiscreteIndexes.Count > 0)
        {
            writer.WriteStartElement("", "discretePr", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("count", grouping.DiscreteIndexes.Count.ToString(CultureInfo.InvariantCulture));
            foreach (int index in grouping.DiscreteIndexes)
            {
                writer.WriteStartElement("", "x", XlsxNamespaces.Spreadsheet);
                writer.WriteAttributeString("v", index.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        writer.WriteStartElement("", "rangePr", XlsxNamespaces.Spreadsheet);
        if (grouping.DateGrouping)
        {
            writer.WriteAttributeString("groupBy", GetDateGroupByToken(grouping.DateGroupBy));
            writer.WriteAttributeString("startDate", XlsxXmlValueWriter.WriteDateTime(grouping.StartDate));
            writer.WriteAttributeString("endDate", XlsxXmlValueWriter.WriteDateTime(grouping.EndDate));
        }
        else
        {
            writer.WriteAttributeString("startNum", XlsxXmlValueWriter.WriteDouble(grouping.StartNumber));
            writer.WriteAttributeString("endNum", XlsxXmlValueWriter.WriteDouble(grouping.EndNumber));
            writer.WriteAttributeString("groupInterval", XlsxXmlValueWriter.WriteDouble(grouping.Interval));
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteCalculatedItems(XmlWriter writer, List<XlsxPivotCacheCalculatedItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "calculatedItems", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("count", items.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxPivotCacheCalculatedItem item in items)
        {
            writer.WriteStartElement("", "calculatedItem", XlsxNamespaces.Spreadsheet);
            if (item.HasFieldIndex)
            {
                writer.WriteAttributeString("field", item.FieldIndex.ToString(CultureInfo.InvariantCulture));
            }
            WriteStringAttribute(writer, "name", item.Name);
            WriteStringAttribute(writer, "formula", item.Formula);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static string GetDateGroupByToken(XlsxPivotDateGroupBy groupBy)
    {
        if (groupBy == XlsxPivotDateGroupBy.Seconds) return "seconds";
        if (groupBy == XlsxPivotDateGroupBy.Minutes) return "minutes";
        if (groupBy == XlsxPivotDateGroupBy.Hours) return "hours";
        if (groupBy == XlsxPivotDateGroupBy.Months) return "months";
        if (groupBy == XlsxPivotDateGroupBy.Quarters) return "quarters";
        if (groupBy == XlsxPivotDateGroupBy.Years) return "years";
        return "days";
    }

    internal static void WriteBoolAttribute(XmlWriter writer, string name, bool value, bool present)
    {
        if (present)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteBoolean(value));
        }
    }

    internal static void WriteUIntAttribute(XmlWriter writer, string name, uint value, bool present)
    {
        if (present)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteUnsignedInteger(value));
        }
    }

    internal static void WriteStringAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    internal static void WriteRawXml(XmlWriter writer, string xml)
    {
        if (xml != null && xml.Length > 0)
        {
            writer.WriteRaw(xml);
        }
    }
}
