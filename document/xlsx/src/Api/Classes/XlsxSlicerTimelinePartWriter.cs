using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Validates and writes authored slicer and timeline parts.
/// </summary>
public class XlsxSlicerTimelinePartWriter : IXlsxSlicerTimelinePartWriter
{
    /// <summary>Initializes a slicer and timeline writer.</summary>
    public XlsxSlicerTimelinePartWriter()
    {
    }

    /// <summary>Writes a slicer or timeline part to XML text.</summary>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    public XlsxSlicerPartWriteResult WriteString(XlsxSlicerTimelinePart? part)
    {
        XlsxSlicerPartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }
        return result;
    }

    /// <summary>Writes a slicer or timeline part to UTF-8 bytes.</summary>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    public XlsxSlicerPartWriteResult WriteBytes(XlsxSlicerTimelinePart? part)
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

    /// <summary>Writes a slicer or timeline part to a caller-owned stream.</summary>
    /// <param name="stream">The writable stream.</param>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    public XlsxSlicerPartWriteResult WriteStream(Stream stream, XlsxSlicerTimelinePart? part)
    {
        XlsxSlicerPartWriteResult result = ValidatePart(part);
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
        string namespaceUri = XlsxCellValueConverter.NormalizeText(part.NamespaceUri);
        if (namespaceUri.Length == 0)
        {
            namespaceUri = XlsxNamespaces.Office2013Spreadsheet;
        }
        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartElement("", part.RootElementName, namespaceUri);
            WriteStringAttribute(writer, "name", part.Name);
            WriteStringAttribute(writer, "sourceName", part.SourceName);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WritePivotTables(writer, namespaceUri, part);
            WriteState(writer, namespaceUri, part);
            WriteItems(writer, namespaceUri, part);
            XlsxSlicerCachePartWriter.WriteRawXml(writer, part.ExtensionListRawXml);
            if (part.UnknownXml != null)
            {
                foreach (string xml in part.UnknownXml)
                {
                    XlsxSlicerCachePartWriter.WriteRawXml(writer, xml);
                }
            }
            writer.WriteEndElement();
        }
        return result;
    }

    internal static XlsxSlicerPartWriteResult ValidatePart(XlsxSlicerTimelinePart? part)
    {
        XlsxSlicerPartWriteResult result = new XlsxSlicerPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimelineWriter.partNull", "Slicer or timeline part must not be null."));
            return result;
        }
        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }
        if (part.RootElementName == null || part.RootElementName.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimelineWriter.rootMissing", "Slicer or timeline part must have a root element name."));
        }
        if ((part.RootElementName == "timelines" || part.RootElementName == "slicers") && (part.Items == null || part.Items.Count == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimelineWriter.itemsMissing", "Visual slicer or timeline parts must contain at least one item."));
        }
        if (part.RootElementName == "timelineCacheDefinition" && (part.Name == null || part.Name.Length == 0 || part.SourceName == null || part.SourceName.Length == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimelineWriter.cacheIdentityMissing", "Timeline cache definitions must contain names."));
        }
        return result;
    }

    internal static void WritePivotTables(XmlWriter writer, string namespaceUri, XlsxSlicerTimelinePart part)
    {
        if (part.PivotTables == null || part.PivotTables.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "pivotTables", namespaceUri);
        foreach (XlsxSlicerPivotTableReference reference in part.PivotTables)
        {
            writer.WriteStartElement("", "pivotTable", namespaceUri);
            if (reference.HasTabId)
            {
                writer.WriteAttributeString("tabId", reference.TabId.ToString(CultureInfo.InvariantCulture));
            }
            WriteStringAttribute(writer, "name", reference.Name);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteState(XmlWriter writer, string namespaceUri, XlsxSlicerTimelinePart part)
    {
        if (!part.HasState || part.State == null)
        {
            return;
        }
        XlsxTimelineState state = part.State;
        writer.WriteStartElement("", "state", namespaceUri);
        WriteUIntAttribute(writer, "pivotCacheId", state.PivotCacheId, state.HasPivotCacheId);
        WriteUIntAttribute(writer, "filterId", state.FilterId, state.HasFilterId);
        WriteStringAttribute(writer, "filterType", state.FilterType);
        if (state.HasSingleRange)
        {
            writer.WriteAttributeString("singleRange", XlsxXmlValueWriter.WriteBoolean(state.SingleRange));
        }
        if (state.HasSelection)
        {
            writer.WriteStartElement("", "selection", namespaceUri);
            writer.WriteAttributeString("startDate", XlsxXmlValueWriter.WriteDateTime(state.StartDate));
            writer.WriteAttributeString("endDate", XlsxXmlValueWriter.WriteDateTime(state.EndDate));
            WriteUIntAttribute(writer, "level", state.Level, state.HasLevel);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteItems(XmlWriter writer, string namespaceUri, XlsxSlicerTimelinePart part)
    {
        if (part.Items == null)
        {
            return;
        }
        string itemName = part.RootElementName == "slicers" ? "slicer" : "timeline";
        foreach (XlsxSlicerTimelineItem item in part.Items)
        {
            writer.WriteStartElement("", itemName, namespaceUri);
            WriteStringAttribute(writer, "name", item.Name);
            WriteStringAttribute(writer, "cache", item.CacheName);
            WriteStringAttribute(writer, "caption", item.Caption);
            WriteStringAttribute(writer, "style", item.StyleName);
            WriteUIntAttribute(writer, "level", item.Level, item.HasLevel);
            WriteUIntAttribute(writer, "selectionLevel", item.SelectionLevel, item.HasSelectionLevel);
            WriteUIntAttribute(writer, "scrollPosition", item.ScrollPosition, item.HasScrollPosition);
            writer.WriteEndElement();
        }
    }

    internal static void WriteStringAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    internal static void WriteUIntAttribute(XmlWriter writer, string name, uint value, bool present)
    {
        if (present)
        {
            writer.WriteAttributeString(name, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
