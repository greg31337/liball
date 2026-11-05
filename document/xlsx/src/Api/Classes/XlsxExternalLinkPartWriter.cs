using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes external link part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxExternalLinkPartWriter : IXlsxExternalLinkPartWriter
{
    /// <summary>
    /// Initializes a new external link part writer with its default state.
    /// </summary>
    public XlsxExternalLinkPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxExternalLinkPartWriteResult WriteString(XlsxExternalLinkPart part)
    {
        XlsxExternalLinkPartWriteResult result = WriteBytes(part);
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
    public XlsxExternalLinkPartWriteResult WriteBytes(XlsxExternalLinkPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxExternalLinkPartWriteResult result = WriteStream(stream, part);
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
    public XlsxExternalLinkPartWriteResult WriteStream(Stream stream, XlsxExternalLinkPart part)
    {
        XlsxExternalLinkPartWriteResult result = ValidateExternalLinkPart(part);
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
            writer.WriteStartElement("", "externalLink", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
            writer.WriteStartElement("", "externalBook", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, part.RelationshipId);
            if (part.SheetNames != null && part.SheetNames.Count > 0)
            {
                writer.WriteStartElement("", "sheetNames", XlsxNamespaces.Spreadsheet);
                foreach (string sheetName in part.SheetNames)
                {
                    writer.WriteStartElement("", "sheetName", XlsxNamespaces.Spreadsheet);
                    writer.WriteAttributeString("val", XlsxCellValueConverter.NormalizeText(sheetName));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        return result;
    }

    internal static XlsxExternalLinkPartWriteResult ValidateExternalLinkPart(XlsxExternalLinkPart part)
    {
        XlsxExternalLinkPartWriteResult result = new XlsxExternalLinkPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLinkWriter.partNull", "External link part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.RelationshipId == null || part.RelationshipId.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLinkWriter.relationshipIdMissing", "External link part must have an externalBook r:id."));
        }

        if (part.SheetNames == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLinkWriter.sheetNamesNull", "External link sheet name list must not be null."));
        }

        return result;
    }
}
