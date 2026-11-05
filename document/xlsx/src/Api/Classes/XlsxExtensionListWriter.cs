using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes extension list data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxExtensionListWriter : IXlsxExtensionListWriter
{
    /// <summary>
    /// Initializes a new extension list writer with its default state.
    /// </summary>
    public XlsxExtensionListWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "list">The list value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxExtensionListWriteResult WriteString(XlsxExtensionList list)
    {
        XlsxExtensionListWriteResult result = WriteBytes(list);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "list">The list value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxExtensionListWriteResult WriteBytes(XlsxExtensionList list)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxExtensionListWriteResult result = WriteStream(stream, list);
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
    /// <param name = "list">The list value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxExtensionListWriteResult WriteStream(Stream stream, XlsxExtensionList list)
    {
        XlsxExtensionListWriteResult result = ValidateExtensionList(list);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(list.Prefix, "extLst", list.NamespaceUri);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, list.UnknownRootAttributes);
            foreach (XlsxExtension extension in list.Extensions)
            {
                WriteExtension(writer, extension, list.NamespaceUri, list.Prefix);
            }

            foreach (string unknownXml in list.UnknownXml)
            {
                if (unknownXml != null && unknownXml.Length > 0)
                {
                    writer.WriteRaw(unknownXml);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxExtensionListWriteResult ValidateExtensionList(XlsxExtensionList list)
    {
        XlsxExtensionListWriteResult result = new XlsxExtensionListWriteResult();
        if (list == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.listNull", "Extension list must not be null."));
            return result;
        }

        if (list.NamespaceUri == null || list.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.namespaceMissing", "Extension list must have a namespace URI."));
        }

        foreach (XlsxExtension extension in list.Extensions)
        {
            ValidateExtension(result, extension);
        }

        return result;
    }

    internal static void ValidateExtension(XlsxExtensionListWriteResult result, XlsxExtension extension)
    {
        if (extension == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.extensionNull", "Extension entry must not be null."));
            return;
        }

        if ((extension.RawXml == null || extension.RawXml.Length == 0) && (extension.Uri == null || extension.Uri.Length == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.uriMissing", "Extension entry must have a uri attribute when raw XML is not available."));
        }
    }

    internal static void WriteExtension(XmlWriter writer, XlsxExtension extension, string rootNamespaceUri, string rootPrefix)
    {
        if (extension.RawXml != null && extension.RawXml.Length > 0)
        {
            writer.WriteRaw(extension.RawXml);
            return;
        }

        string namespaceUri = extension.NamespaceUri;
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            namespaceUri = rootNamespaceUri;
        }

        string prefix = extension.Prefix;
        if (prefix == null)
        {
            prefix = rootPrefix;
        }

        writer.WriteStartElement(prefix, "ext", namespaceUri);
        writer.WriteAttributeString("uri", extension.Uri);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, extension.UnknownAttributes);
        writer.WriteEndElement();
    }
}
