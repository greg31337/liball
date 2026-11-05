using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads extension list data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxExtensionListReader : IXlsxExtensionListReader
{
    /// <summary>
    /// Initializes a new extension list reader with its default state.
    /// </summary>
    public XlsxExtensionListReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The extension list produced by the operation.</returns>
    public XlsxExtensionList ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxExtensionList list = ReadBytes(bytes);
        list.RawXml = xml;
        return list;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The extension list produced by the operation.</returns>
    public XlsxExtensionList ReadBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The extension list produced by the operation.</returns>
    public XlsxExtensionList ReadStream(Stream stream)
    {
        XlsxExtensionList list = new XlsxExtensionList();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(list, reader);
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && reader.LocalName == "ext" && reader.NamespaceURI == list.NamespaceUri)
                {
                    ReadExtension(list, reader);
                    continue;
                }

                list.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            list.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.missingRoot", "Extension list XML must contain an extLst root element."));
        }

        list.IsValid = list.Validation.IsValid;
        return list;
    }

    internal static XlsxExtensionList CreateMissingRootResult()
    {
        XlsxExtensionList list = new XlsxExtensionList();
        list.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.missingRoot", "Extension list XML must contain an extLst root element."));
        return list;
    }

    internal static void ReadRootElement(XlsxExtensionList list, XmlReader reader)
    {
        if (reader.LocalName != "extLst")
        {
            list.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.invalidRoot", "Extension list XML root must be an extLst element."));
        }

        list.NamespaceUri = reader.NamespaceURI;
        list.Prefix = reader.Prefix;
        ReadUnknownAttributes(reader, list.UnknownRootAttributes, "");
    }

    internal static void ReadExtension(XlsxExtensionList list, XmlReader reader)
    {
        XlsxExtension extension = new XlsxExtension();
        extension.Uri = reader.GetAttribute("uri");
        extension.NamespaceUri = reader.NamespaceURI;
        extension.Prefix = reader.Prefix;
        ReadUnknownAttributes(reader, extension.UnknownAttributes, "uri");
        if (extension.Uri == null || extension.Uri.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "extensions.uriMissing", "Extension element must have a uri attribute.");
            diagnostic.SetLocation("", "ext", "uri");
            list.AddDiagnostic(diagnostic);
        }

        extension.RawXml = reader.ReadOuterXml();
        list.Extensions.Add(extension);
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string knownLocalName)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = knownLocalName.Length > 0 && reader.NamespaceURI == "" && reader.LocalName == knownLocalName;
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                XlsxXmlAttribute attribute = XlsxMarkupCompatibility.CreateAttributeFromReader(reader);
                attributes.Add(attribute);
            }
        }

        reader.MoveToElement();
    }
}
