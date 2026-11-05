using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes slicer cache part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxSlicerCachePartWriter : IXlsxSlicerCachePartWriter
{
    /// <summary>
    /// Initializes a new slicer cache part writer with its default state.
    /// </summary>
    public XlsxSlicerCachePartWriter()
    {
    }

    internal XlsxSlicerCachePart CreateTableSlicerCache(string partPath, string name, string sourceName, uint tableId, string columnName)
    {
        XlsxSlicerCachePart part = new XlsxSlicerCachePart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.Name = XlsxCellValueConverter.NormalizeText(name);
        part.SourceName = XlsxCellValueConverter.NormalizeText(sourceName);
        part.HasTableSlicerCache = true;
        part.TableId = tableId;
        part.HasTableId = true;
        part.TableColumnName = XlsxCellValueConverter.NormalizeText(columnName);
        return part;
    }

    internal XlsxSlicerCachePart CreatePivotSlicerCache(string partPath, string name, string sourceName, string pivotTableName, uint tabId)
    {
        XlsxSlicerCachePart part = new XlsxSlicerCachePart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.Name = XlsxCellValueConverter.NormalizeText(name);
        part.SourceName = XlsxCellValueConverter.NormalizeText(sourceName);
        XlsxSlicerPivotTableReference reference = new XlsxSlicerPivotTableReference();
        reference.Name = XlsxCellValueConverter.NormalizeText(pivotTableName);
        reference.TabId = tabId;
        reference.HasTabId = true;
        part.PivotTables.Add(reference);
        return part;
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxSlicerPartWriteResult WriteString(XlsxSlicerCachePart part)
    {
        XlsxSlicerPartWriteResult result = WriteBytes(part);
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
    public XlsxSlicerPartWriteResult WriteBytes(XlsxSlicerCachePart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxSlicerPartWriteResult result = WriteStream(stream, part);
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
    public XlsxSlicerPartWriteResult WriteStream(Stream stream, XlsxSlicerCachePart part)
    {
        XlsxSlicerPartWriteResult result = ValidateSlicerCache(part);
        if (!result.IsValid)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(rawBytes, 0, rawBytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartElement("", "slicerCacheDefinition", XlsxNamespaces.Office2010Spreadsheet);
            writer.WriteAttributeString("name", part.Name);
            writer.WriteAttributeString("sourceName", part.SourceName);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            if (part.HasTableSlicerCache)
            {
                writer.WriteStartElement("", "tableSlicerCache", XlsxNamespaces.Office2010Spreadsheet);
                if (part.HasTableId)
                {
                    writer.WriteAttributeString("tableId", part.TableId.ToString(CultureInfo.InvariantCulture));
                }

                if (part.TableColumnName != null && part.TableColumnName.Length > 0)
                {
                    writer.WriteAttributeString("column", part.TableColumnName);
                }

                if (part.CrossFilter != null && part.CrossFilter.Length > 0)
                {
                    writer.WriteAttributeString("crossFilter", part.CrossFilter);
                }
                if (part.SortOrder != null && part.SortOrder.Length > 0)
                {
                    writer.WriteAttributeString("sortOrder", part.SortOrder);
                }

                writer.WriteEndElement();
            }

            if (part.PivotTables != null && part.PivotTables.Count > 0)
            {
                writer.WriteStartElement("", "pivotTables", XlsxNamespaces.Office2010Spreadsheet);
                foreach (XlsxSlicerPivotTableReference reference in part.PivotTables)
                {
                    writer.WriteStartElement("", "pivotTable", XlsxNamespaces.Office2010Spreadsheet);
                    if (reference != null)
                    {
                        if (reference.HasTabId)
                        {
                            writer.WriteAttributeString("tabId", reference.TabId.ToString(CultureInfo.InvariantCulture));
                        }

                        if (reference.Name != null && reference.Name.Length > 0)
                        {
                            writer.WriteAttributeString("name", reference.Name);
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            if (part.Selections != null && part.Selections.Count > 0)
            {
                writer.WriteStartElement("", "slicerCacheItems", XlsxNamespaces.Office2010Spreadsheet);
                writer.WriteAttributeString("count", part.Selections.Count.ToString(CultureInfo.InvariantCulture));
                foreach (XlsxSlicerSelectionItem item in part.Selections)
                {
                    writer.WriteStartElement("", "slicerCacheItem", XlsxNamespaces.Office2010Spreadsheet);
                    writer.WriteAttributeString("x", item.Index.ToString(CultureInfo.InvariantCulture));
                    if (item.HasSelected)
                    {
                        writer.WriteAttributeString("s", XlsxXmlValueWriter.WriteBoolean(item.Selected));
                    }
                    if (item.Name != null && item.Name.Length > 0)
                    {
                        writer.WriteAttributeString("n", item.Name);
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

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

        return result;
    }

    internal static XlsxSlicerPartWriteResult ValidateSlicerCache(XlsxSlicerCachePart part)
    {
        XlsxSlicerPartWriteResult result = new XlsxSlicerPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.partNull", "Slicer cache part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.Name == null || part.Name.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.nameMissing", "Slicer cache must have a name."));
        }

        if (part.SourceName == null || part.SourceName.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.sourceNameMissing", "Slicer cache must have a source name."));
        }

        if (!part.HasTableSlicerCache && (part.PivotTables == null || part.PivotTables.Count == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.sourceMissing", "Slicer cache must target a table or pivot table."));
        }

        if (part.HasTableSlicerCache && !part.HasTableId)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.tableIdMissing", "Table slicer cache must have a table id."));
        }

        if (part.PivotTables == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCacheWriter.pivotTablesNull", "Slicer cache pivot table list must not be null."));
        }

        return result;
    }

    internal static void WriteRawXml(XmlWriter writer, string xml)
    {
        if (xml != null && xml.Length > 0)
        {
            writer.WriteRaw(xml);
        }
    }
}
