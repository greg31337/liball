using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes core properties data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxCorePropertiesWriter : IXlsxCorePropertiesWriter
{
    /// <summary>
    /// Initializes a new core properties writer with its default state.
    /// </summary>
    public XlsxCorePropertiesWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxCorePropertiesWriteResult WriteString(XlsxCorePropertiesPart part)
    {
        XlsxCorePropertiesWriteResult result = WriteBytes(part);
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
    public XlsxCorePropertiesWriteResult WriteBytes(XlsxCorePropertiesPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxCorePropertiesWriteResult result = WriteStream(stream, part);
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
    public XlsxCorePropertiesWriteResult WriteStream(Stream stream, XlsxCorePropertiesPart part)
    {
        XlsxCorePropertiesWriteResult result = ValidateCorePropertiesPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("cp", "coreProperties", XlsxNamespaces.CoreProperties);
            writer.WriteAttributeString("xmlns", "dc", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.DublinCore);
            writer.WriteAttributeString("xmlns", "dcterms", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.DublinCoreTerms);
            writer.WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.XmlSchemaInstance);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "title", part.Title);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "subject", part.Subject);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "creator", part.Creator);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "keywords", part.Keywords);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "description", part.Description);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "lastModifiedBy", part.LastModifiedBy);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "revision", part.Revision);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "category", part.Category);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "contentStatus", part.ContentStatus);
            WriteDateElement(writer, "created", part.Created, part.HasCreated);
            WriteDateElement(writer, "modified", part.Modified, part.HasModified);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "language", part.Language);
            WriteTextElement(writer, "dc", XlsxNamespaces.DublinCore, "identifier", part.Identifier);
            WriteTextElement(writer, "cp", XlsxNamespaces.CoreProperties, "version", part.Version);
            foreach (string unknownXml in part.UnknownXml)
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

    internal static XlsxCorePropertiesWriteResult ValidateCorePropertiesPart(XlsxCorePropertiesPart part)
    {
        XlsxCorePropertiesWriteResult result = new XlsxCorePropertiesWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "coreProperties.partNull", "Core properties part must not be null."));
            return result;
        }

        return result;
    }

    internal static void WriteTextElement(XmlWriter writer, string prefix, string namespaceUri, string localName, string value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        writer.WriteStartElement(prefix, localName, namespaceUri);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    internal static void WriteDateElement(XmlWriter writer, string localName, System.DateTime value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        writer.WriteStartElement("dcterms", localName, XlsxNamespaces.DublinCoreTerms);
        writer.WriteAttributeString("xsi", "type", XlsxNamespaces.XmlSchemaInstance, "dcterms:W3CDTF");
        writer.WriteString(XlsxXmlValueWriter.WriteDateTime(value));
        writer.WriteEndElement();
    }
}
