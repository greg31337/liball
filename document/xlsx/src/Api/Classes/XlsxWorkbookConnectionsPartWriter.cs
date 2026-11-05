using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes workbook connections part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxWorkbookConnectionsPartWriter : IXlsxWorkbookConnectionsPartWriter
{
    /// <summary>
    /// Initializes a new workbook connections part writer with its default state.
    /// </summary>
    public XlsxWorkbookConnectionsPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookConnectionsPartWriteResult WriteString(XlsxWorkbookConnectionsPart part)
    {
        XlsxWorkbookConnectionsPartWriteResult result = WriteBytes(part);
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
    public XlsxWorkbookConnectionsPartWriteResult WriteBytes(XlsxWorkbookConnectionsPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxWorkbookConnectionsPartWriteResult result = WriteStream(stream, part);
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
    public XlsxWorkbookConnectionsPartWriteResult WriteStream(Stream stream, XlsxWorkbookConnectionsPart part)
    {
        XlsxWorkbookConnectionsPartWriteResult result = ValidateConnectionsPart(part);
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
            writer.WriteStartElement("", "connections", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("count", part.Connections.Count.ToString(CultureInfo.InvariantCulture));
            foreach (XlsxWorkbookConnectionInfo connection in part.Connections)
            {
                writer.WriteStartElement("", "connection", XlsxNamespaces.Spreadsheet);
                if (connection.HasId)
                {
                    writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(connection.Id));
                }

                if (connection.Name != null && connection.Name.Length > 0)
                {
                    writer.WriteAttributeString("name", connection.Name);
                }

                XlsxContentTypesWriter.WriteUnknownAttributes(writer, connection.UnknownAttributes);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        return result;
    }

    internal static XlsxWorkbookConnectionsPartWriteResult ValidateConnectionsPart(XlsxWorkbookConnectionsPart part)
    {
        XlsxWorkbookConnectionsPartWriteResult result = new XlsxWorkbookConnectionsPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "connectionsWriter.partNull", "Connections part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.Connections == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "connectionsWriter.connectionsNull", "Connections part connection list must not be null."));
        }

        return result;
    }
}
