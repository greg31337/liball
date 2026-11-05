using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads workbook connections part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxWorkbookConnectionsPartReader : IXlsxWorkbookConnectionsPartReader
{
    /// <summary>
    /// Initializes a new workbook connections part reader with its default state.
    /// </summary>
    public XlsxWorkbookConnectionsPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The workbook connections part produced by the operation.</returns>
    public XlsxWorkbookConnectionsPart ReadString(string xml)
    {
        return ReadStringFromPart(XlsxPackagePaths.Connections, xml);
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The workbook connections part produced by the operation.</returns>
    public XlsxWorkbookConnectionsPart ReadStringFromPart(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxWorkbookConnectionsPart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The workbook connections part produced by the operation.</returns>
    public XlsxWorkbookConnectionsPart ReadBytes(string partPath, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(partPath, stream);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The workbook connections part produced by the operation.</returns>
    public XlsxWorkbookConnectionsPart ReadStream(string partPath, Stream stream)
    {
        XlsxWorkbookConnectionsPart part = new XlsxWorkbookConnectionsPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
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
                    ReadRoot(part, reader);
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        ReadConnectionChildren(part, reader);
                    }

                    continue;
                }

                reader.ReadOuterXml();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "connections.missingRoot", "Connections XML must contain a connections root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxWorkbookConnectionsPart CreateMissingRootResult(string partPath)
    {
        XlsxWorkbookConnectionsPart part = new XlsxWorkbookConnectionsPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "connections.missingRoot", "Connections XML must contain a connections root element."));
        return part;
    }

    internal static void ReadRoot(XlsxWorkbookConnectionsPart part, XmlReader reader)
    {
        if (reader.LocalName != "connections" || !IsConnectionsNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "connections.invalidRoot", "Connections XML root must be connections in the SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
            return;
        }

        SetOptionalUInt(part, reader.GetAttribute("count"), "connections", "count");
    }

    internal static void ReadConnectionChildren(XlsxWorkbookConnectionsPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsConnectionsNamespace(reader.NamespaceURI) && reader.LocalName == "connection")
            {
                part.Connections.Add(ReadConnection(part, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                reader.ReadOuterXml();
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxWorkbookConnectionInfo ReadConnection(XlsxWorkbookConnectionsPart part, XmlReader reader)
    {
        XlsxWorkbookConnectionInfo connection = new XlsxWorkbookConnectionInfo();
        SetOptionalUInt(part, reader.GetAttribute("id"), "connection", "id", connection);
        connection.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        ReadUnknownConnectionAttributes(reader, connection.UnknownAttributes);
        reader.ReadOuterXml();
        return connection;
    }

    internal static void SetOptionalUInt(XlsxWorkbookConnectionsPart part, string? text, string elementName, string attributeName)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (result.IsValid)
        {
            part.Count = result.UnsignedIntValue;
            part.HasCount = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorkbookConnectionsPart part, string? text, string elementName, string attributeName, XlsxWorkbookConnectionInfo connection)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (result.IsValid)
        {
            connection.Id = result.UnsignedIntValue;
            connection.HasId = true;
        }
    }

    internal static void AddParseDiagnostics(XlsxWorkbookConnectionsPart part, XlsxXmlValueParseResult result, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownConnectionAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "id" || reader.LocalName == "name");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsConnectionsNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
