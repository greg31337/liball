using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes query table part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxQueryTablePartWriter : IXlsxQueryTablePartWriter
{
    /// <summary>
    /// Initializes a new query table part writer with its default state.
    /// </summary>
    public XlsxQueryTablePartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxQueryTablePartWriteResult WriteString(XlsxQueryTablePart part)
    {
        XlsxQueryTablePartWriteResult result = WriteBytes(part);
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
    public XlsxQueryTablePartWriteResult WriteBytes(XlsxQueryTablePart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxQueryTablePartWriteResult result = WriteStream(stream, part);
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
    public XlsxQueryTablePartWriteResult WriteStream(Stream stream, XlsxQueryTablePart part)
    {
        XlsxQueryTablePartWriteResult result = ValidateQueryTablePart(part);
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
            writer.WriteStartElement("", "queryTable", XlsxNamespaces.Spreadsheet);
            if (part.Name != null && part.Name.Length > 0)
            {
                writer.WriteAttributeString("name", part.Name);
            }

            if (part.HasConnectionId)
            {
                writer.WriteAttributeString("connectionId", XlsxXmlValueWriter.WriteUnsignedInteger(part.ConnectionId));
            }

            writer.WriteStartElement("", "queryTableFields", XlsxNamespaces.Spreadsheet);
            writer.WriteAttributeString("count", part.Fields.Count.ToString(CultureInfo.InvariantCulture));
            foreach (XlsxQueryTableField field in part.Fields)
            {
                writer.WriteStartElement("", "queryTableField", XlsxNamespaces.Spreadsheet);
                if (field.HasId)
                {
                    writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(field.Id));
                }

                if (field.HasTableColumnId)
                {
                    writer.WriteAttributeString("tableColumnId", XlsxXmlValueWriter.WriteUnsignedInteger(field.TableColumnId));
                }

                if (field.Name != null && field.Name.Length > 0)
                {
                    writer.WriteAttributeString("name", field.Name);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        return result;
    }

    internal static XlsxQueryTablePartWriteResult ValidateQueryTablePart(XlsxQueryTablePart part)
    {
        XlsxQueryTablePartWriteResult result = new XlsxQueryTablePartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "queryTableWriter.partNull", "Query table part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.Fields == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "queryTableWriter.fieldsNull", "Query table field list must not be null."));
        }

        return result;
    }
}
