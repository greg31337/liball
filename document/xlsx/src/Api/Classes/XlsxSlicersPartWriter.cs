using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes slicers part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxSlicersPartWriter : IXlsxSlicersPartWriter
{
    /// <summary>
    /// Initializes a new slicers part writer with its default state.
    /// </summary>
    public XlsxSlicersPartWriter()
    {
    }

    internal XlsxSlicersPart CreateSlicersPart(string partPath, string name, string cacheName, string caption)
    {
        XlsxSlicersPart part = new XlsxSlicersPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        XlsxSlicerDefinition slicer = new XlsxSlicerDefinition();
        slicer.Name = XlsxCellValueConverter.NormalizeText(name);
        slicer.CacheName = XlsxCellValueConverter.NormalizeText(cacheName);
        slicer.Caption = XlsxCellValueConverter.NormalizeText(caption);
        part.Slicers.Add(slicer);
        return part;
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxSlicerPartWriteResult WriteString(XlsxSlicersPart part)
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
    public XlsxSlicerPartWriteResult WriteBytes(XlsxSlicersPart part)
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
    public XlsxSlicerPartWriteResult WriteStream(Stream stream, XlsxSlicersPart part)
    {
        XlsxSlicerPartWriteResult result = ValidateSlicersPart(part);
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
            writer.WriteStartElement("", "slicers", XlsxNamespaces.Office2010Spreadsheet);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxSlicerDefinition slicer in part.Slicers)
            {
                writer.WriteStartElement("", "slicer", XlsxNamespaces.Office2010Spreadsheet);
                if (slicer != null)
                {
                    if (slicer.Name != null && slicer.Name.Length > 0)
                    {
                        writer.WriteAttributeString("name", slicer.Name);
                    }

                    if (slicer.CacheName != null && slicer.CacheName.Length > 0)
                    {
                        writer.WriteAttributeString("cache", slicer.CacheName);
                    }

                    if (slicer.Caption != null && slicer.Caption.Length > 0)
                    {
                        writer.WriteAttributeString("caption", slicer.Caption);
                    }

                    if (slicer.HasColumnCount)
                    {
                        writer.WriteAttributeString("columnCount", slicer.ColumnCount.ToString(CultureInfo.InvariantCulture));
                    }
                    if (slicer.StyleName != null && slicer.StyleName.Length > 0)
                    {
                        writer.WriteAttributeString("slicerStyle", slicer.StyleName);
                    }
                    if (slicer.HasShowCaption)
                    {
                        writer.WriteAttributeString("showCaption", XlsxXmlValueWriter.WriteBoolean(slicer.ShowCaption));
                    }
                    if (slicer.HasLockedPosition)
                    {
                        writer.WriteAttributeString("lockedPosition", XlsxXmlValueWriter.WriteBoolean(slicer.LockedPosition));
                    }
                    if (slicer.HasRowHeight)
                    {
                        writer.WriteAttributeString("rowHeight", slicer.RowHeight.ToString(CultureInfo.InvariantCulture));
                    }
                    if (slicer.HasStartItem)
                    {
                        writer.WriteAttributeString("startItem", slicer.StartItem.ToString(CultureInfo.InvariantCulture));
                    }
                    XlsxContentTypesWriter.WriteUnknownAttributes(writer, slicer.UnknownAttributes);
                    if (slicer.UnknownXml != null)
                    {
                        foreach (string xml in slicer.UnknownXml)
                        {
                            XlsxSlicerCachePartWriter.WriteRawXml(writer, xml);
                        }
                    }
                }

                writer.WriteEndElement();
            }

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

    internal static XlsxSlicerPartWriteResult ValidateSlicersPart(XlsxSlicersPart part)
    {
        XlsxSlicerPartWriteResult result = new XlsxSlicerPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.partNull", "Slicers part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.Slicers == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.slicersNull", "Slicers part slicer list must not be null."));
            return result;
        }

        if (part.Slicers.Count == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.slicersEmpty", "Slicers part must contain at least one slicer."));
        }

        foreach (XlsxSlicerDefinition slicer in part.Slicers)
        {
            if (slicer == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.slicerNull", "Slicers part must not contain null slicer entries."));
                continue;
            }

            if (slicer.Name == null || slicer.Name.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.nameMissing", "Slicer must have a name."));
            }

            if (slicer.CacheName == null || slicer.CacheName.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicersWriter.cacheMissing", "Slicer must reference a slicer cache."));
            }
        }

        return result;
    }
}
