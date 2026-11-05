using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes table part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxTablePartWriter : IXlsxTablePartWriter
{
    /// <summary>
    /// Initializes a new table part writer with its default state.
    /// </summary>
    public XlsxTablePartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxTablePartWriteResult WriteString(XlsxTablePart? part)
    {
        XlsxTablePartWriteResult result = WriteBytes(part);
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
    public XlsxTablePartWriteResult WriteBytes(XlsxTablePart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxTablePartWriteResult result = WriteStream(stream, part);
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
    public XlsxTablePartWriteResult WriteStream(Stream stream, XlsxTablePart? part)
    {
        XlsxTablePartWriteResult result = ValidateTablePart(part);
        if (!result.IsValid || part == null)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "table", part.NamespaceUri);
            WriteOptionalUIntAttribute(writer, "id", part.Id, part.HasId);
            WriteOptionalStringAttribute(writer, "name", part.Name);
            WriteOptionalStringAttribute(writer, "displayName", part.DisplayName);
            WriteOptionalStringAttribute(writer, "ref", part.Reference);
            WriteOptionalStringAttribute(writer, "tableType", part.TableType);
            WriteOptionalStringAttribute(writer, "comment", part.Comment);
            WriteOptionalUIntAttribute(writer, "headerRowCount", part.HeaderRowCount, part.HasHeaderRowCount);
            WriteOptionalUIntAttribute(writer, "totalsRowCount", part.TotalsRowCount, part.HasTotalsRowCount);
            WriteOptionalBoolAttribute(writer, "totalsRowShown", part.TotalsRowShown, part.HasTotalsRowShown);
            WriteOptionalBoolAttribute(writer, "insertRow", part.InsertRow, part.HasInsertRow);
            WriteOptionalBoolAttribute(writer, "insertRowShift", part.InsertRowShift, part.HasInsertRowShift);
            WriteOptionalBoolAttribute(writer, "published", part.Published, part.HasPublished);
            WriteOptionalUIntAttribute(writer, "connectionId", part.ConnectionId, part.HasConnectionId);
            WriteOptionalUIntAttribute(writer, "headerRowDxfId", part.HeaderRowDxfId, part.HasHeaderRowDxfId);
            WriteOptionalUIntAttribute(writer, "dataDxfId", part.DataDxfId, part.HasDataDxfId);
            WriteOptionalUIntAttribute(writer, "totalsRowDxfId", part.TotalsRowDxfId, part.HasTotalsRowDxfId);
            WriteOptionalUIntAttribute(writer, "headerRowBorderDxfId", part.HeaderRowBorderDxfId, part.HasHeaderRowBorderDxfId);
            WriteOptionalUIntAttribute(writer, "tableBorderDxfId", part.TableBorderDxfId, part.HasTableBorderDxfId);
            WriteOptionalUIntAttribute(writer, "totalsRowBorderDxfId", part.TotalsRowBorderDxfId, part.HasTotalsRowBorderDxfId);
            WriteOptionalStringAttribute(writer, "headerRowCellStyle", part.HeaderRowCellStyle);
            WriteOptionalStringAttribute(writer, "dataCellStyle", part.DataCellStyle);
            WriteOptionalStringAttribute(writer, "totalsRowCellStyle", part.TotalsRowCellStyle);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteRawXml(writer, part.AutoFilterRawXml);
            WriteRawXml(writer, part.SortStateRawXml);
            WriteTableColumns(writer, part);
            WriteTableStyleInfo(writer, part);
            WriteRawXml(writer, part.ExtensionListRawXml);
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxTablePartWriteResult ValidateTablePart(XlsxTablePart? part)
    {
        XlsxTablePartWriteResult result = new XlsxTablePartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.partNull", "Table part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.NamespaceUri == null || part.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.namespaceMissing", "Table part must have a namespace URI."));
        }

        if (!XlsxTablePartReader.IsTableNamespace(part.NamespaceUri))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.namespaceInvalid", "Table part namespace URI must be a SpreadsheetML namespace."));
        }

        if (!part.HasId)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.idMissing", "Table part must have an id attribute."));
        }

        if (part.Name == null || part.Name.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.nameMissing", "Table part must have a name attribute."));
        }

        if (part.DisplayName == null || part.DisplayName.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.displayNameMissing", "Table part must have a displayName attribute."));
        }

        if (part.Reference == null || part.Reference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.referenceMissing", "Table part must have a ref attribute."));
        }
        else
        {
            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(part.Reference);
            foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
            {
                diagnostic.SetLocation(part.PartPath, "table", "ref");
                result.AddDiagnostic(diagnostic);
            }
        }

        if (part.Columns == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnsNull", "Table column list must not be null."));
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.unknownRootAttributesNull", "Table unknown root attributes list must not be null."));
        }

        if (part.TableColumnsUnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.tableColumnsUnknownAttributesNull", "Table columns unknown attributes list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.unknownXmlNull", "Table unknown XML list must not be null."));
        }

        if (part.HasStyleInfo)
        {
            ValidateTableStyleInfo(result, part);
        }

        if (part.Columns != null)
        {
            if (part.Columns.Count == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnsEmpty", "Table part must contain at least one table column."));
            }

            if (part.HasTableColumnCount && part.TableColumnCount != (uint)part.Columns.Count)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.tableColumnCountMismatch", "Table columns count attribute must match the number of tableColumn elements."));
            }

            int index = 0;
            foreach (XlsxTableColumn column in part.Columns)
            {
                ValidateTableColumn(result, part, column, index);
                index = index + 1;
            }
        }

        return result;
    }

    internal static void ValidateTableColumn(XlsxTablePartWriteResult result, XlsxTablePart part, XlsxTableColumn column, int index)
    {
        if (column == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnNull", "Table column list must not contain null entries.");
            diagnostic.SetLocation(part.PartPath, "tableColumn", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (!column.HasId)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnIdMissing", "Table column must have an id attribute.");
            diagnostic.SetLocation(part.PartPath, "tableColumn", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (column.Name == null || column.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnNameMissing", "Table column must have a name attribute.");
            diagnostic.SetLocation(part.PartPath, "tableColumn", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (column.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnUnknownAttributesNull", "Table column unknown attributes list must not be null."));
        }

        if (column.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.columnUnknownXmlNull", "Table column unknown XML list must not be null."));
        }

        if (column.HasXmlColumnProperties)
        {
            ValidateXmlColumnProperties(result, column.XmlColumnProperties);
        }
    }

    internal static void ValidateXmlColumnProperties(XlsxTablePartWriteResult result, XlsxTableXmlColumnProperties properties)
    {
        if (properties == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnPropertiesNull", "Typed XML column properties must not be null when present."));
            return;
        }
        if (!properties.HasMapId)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnMapIdMissing", "Typed XML column properties must contain a map identifier."));
        }
        if (properties.XPath == null || properties.XPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnXPathMissing", "Typed XML column properties must contain an XPath."));
        }
        if (properties.XmlDataType == null || properties.XmlDataType.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnDataTypeMissing", "Typed XML column properties must contain an XML data type."));
        }
        if (properties.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnUnknownAttributesNull", "Typed XML column unknown attributes must not be null."));
        }
        if (properties.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.xmlColumnUnknownXmlNull", "Typed XML column unknown XML must not be null."));
        }
    }

    internal static void ValidateTableStyleInfo(XlsxTablePartWriteResult result, XlsxTablePart part)
    {
        if (part.StyleInfo == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.styleInfoNull", "Table style info must not be null when present."));
            return;
        }

        if (part.StyleInfo.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "tableWriter.styleInfoUnknownAttributesNull", "Table style info unknown attributes list must not be null."));
        }
    }

    internal static void WriteTableColumns(XmlWriter writer, XlsxTablePart part)
    {
        writer.WriteStartElement("", "tableColumns", part.NamespaceUri);
        uint count = (uint)part.Columns.Count;
        if (part.HasTableColumnCount)
        {
            count = part.TableColumnCount;
        }

        writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger(count));
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.TableColumnsUnknownAttributes);
        foreach (XlsxTableColumn column in part.Columns)
        {
            WriteTableColumn(writer, part, column);
        }

        writer.WriteEndElement();
    }

    internal static void WriteTableColumn(XmlWriter writer, XlsxTablePart part, XlsxTableColumn column)
    {
        writer.WriteStartElement("", "tableColumn", part.NamespaceUri);
        WriteOptionalUIntAttribute(writer, "id", column.Id, column.HasId);
        WriteOptionalStringAttribute(writer, "name", column.Name);
        WriteOptionalStringAttribute(writer, "uniqueName", column.UniqueName);
        string totalFunction = column.TotalsRowFunction;
        if (column.HasTotalFunction)
        {
            totalFunction = GetTotalFunctionToken(column.TotalFunction);
        }
        WriteOptionalStringAttribute(writer, "totalsRowFunction", totalFunction);
        WriteOptionalStringAttribute(writer, "totalsRowLabel", column.TotalsRowLabel);
        WriteOptionalUIntAttribute(writer, "queryTableFieldId", column.QueryTableFieldId, column.HasQueryTableFieldId);
        WriteOptionalUIntAttribute(writer, "headerRowDxfId", column.HeaderRowDxfId, column.HasHeaderRowDxfId);
        WriteOptionalUIntAttribute(writer, "dataDxfId", column.DataDxfId, column.HasDataDxfId);
        WriteOptionalUIntAttribute(writer, "totalsRowDxfId", column.TotalsRowDxfId, column.HasTotalsRowDxfId);
        WriteOptionalStringAttribute(writer, "headerRowCellStyle", column.HeaderRowCellStyle);
        WriteOptionalStringAttribute(writer, "dataCellStyle", column.DataCellStyle);
        WriteOptionalStringAttribute(writer, "totalsRowCellStyle", column.TotalsRowCellStyle);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, column.UnknownAttributes);
        WriteFormulaElement(writer, part.NamespaceUri, "calculatedColumnFormula", column.CalculatedColumnFormulaRawXml, column.CalculatedColumnFormula);
        WriteFormulaElement(writer, part.NamespaceUri, "totalsRowFormula", column.TotalsRowFormulaRawXml, column.TotalsRowFormula);
        if (column.HasXmlColumnProperties)
        {
            WriteXmlColumnProperties(writer, part.NamespaceUri, column.XmlColumnProperties);
        }
        else
        {
            WriteRawXml(writer, column.XmlColumnPropertiesRawXml);
        }
        WriteRawXml(writer, column.ExtensionListRawXml);
        WriteUnknownXml(writer, column.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteXmlColumnProperties(XmlWriter writer, string namespaceUri, XlsxTableXmlColumnProperties properties)
    {
        writer.WriteStartElement("", "xmlColumnPr", namespaceUri);
        WriteOptionalUIntAttribute(writer, "mapId", properties.MapId, properties.HasMapId);
        WriteOptionalStringAttribute(writer, "xpath", properties.XPath);
        WriteOptionalBoolAttribute(writer, "denormalized", properties.Denormalized, properties.HasDenormalized);
        WriteOptionalStringAttribute(writer, "xmlDataType", properties.XmlDataType);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, properties.UnknownAttributes);
        WriteRawXml(writer, properties.ExtensionListRawXml);
        WriteUnknownXml(writer, properties.UnknownXml);
        writer.WriteEndElement();
    }

    internal static string GetTotalFunctionToken(XlsxTableTotalFunction function)
    {
        if (function == XlsxTableTotalFunction.Average)
        {
            return "average";
        }
        if (function == XlsxTableTotalFunction.Count)
        {
            return "count";
        }
        if (function == XlsxTableTotalFunction.CountNumbers)
        {
            return "countNums";
        }
        if (function == XlsxTableTotalFunction.Maximum)
        {
            return "max";
        }
        if (function == XlsxTableTotalFunction.Minimum)
        {
            return "min";
        }
        if (function == XlsxTableTotalFunction.Sum)
        {
            return "sum";
        }
        if (function == XlsxTableTotalFunction.StandardDeviation)
        {
            return "stdDev";
        }
        if (function == XlsxTableTotalFunction.Variance)
        {
            return "var";
        }
        if (function == XlsxTableTotalFunction.Custom)
        {
            return "custom";
        }
        return "none";
    }

    internal static void WriteTableStyleInfo(XmlWriter writer, XlsxTablePart part)
    {
        if (!part.HasStyleInfo)
        {
            return;
        }

        writer.WriteStartElement("", "tableStyleInfo", part.NamespaceUri);
        WriteOptionalStringAttribute(writer, "name", part.StyleInfo.Name);
        WriteOptionalBoolAttribute(writer, "showFirstColumn", part.StyleInfo.ShowFirstColumn, part.StyleInfo.HasShowFirstColumn);
        WriteOptionalBoolAttribute(writer, "showLastColumn", part.StyleInfo.ShowLastColumn, part.StyleInfo.HasShowLastColumn);
        WriteOptionalBoolAttribute(writer, "showRowStripes", part.StyleInfo.ShowRowStripes, part.StyleInfo.HasShowRowStripes);
        WriteOptionalBoolAttribute(writer, "showColumnStripes", part.StyleInfo.ShowColumnStripes, part.StyleInfo.HasShowColumnStripes);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.StyleInfo.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteFormulaElement(XmlWriter writer, string namespaceUri, string elementName, string rawXml, string formula)
    {
        if (rawXml != null && rawXml.Length > 0)
        {
            writer.WriteRaw(rawXml);
            return;
        }

        if (formula == null || formula.Length == 0)
        {
            return;
        }

        writer.WriteStartElement("", elementName, namespaceUri);
        writer.WriteString(formula);
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
