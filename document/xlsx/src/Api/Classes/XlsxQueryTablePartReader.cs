using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads query table part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxQueryTablePartReader : IXlsxQueryTablePartReader
{
    /// <summary>
    /// Initializes a new query table part reader with its default state.
    /// </summary>
    public XlsxQueryTablePartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The query table part produced by the operation.</returns>
    public XlsxQueryTablePart ReadString(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxQueryTablePart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The query table part produced by the operation.</returns>
    public XlsxQueryTablePart ReadBytes(string partPath, byte[] bytes)
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
    /// <returns>The query table part produced by the operation.</returns>
    public XlsxQueryTablePart ReadStream(string partPath, Stream stream)
    {
        XlsxQueryTablePart part = new XlsxQueryTablePart();
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
                        ReadQueryTableChildren(part, reader);
                    }

                    continue;
                }

                reader.ReadOuterXml();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "queryTable.missingRoot", "Query table XML must contain a queryTable root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxQueryTablePart CreateMissingRootResult(string partPath)
    {
        XlsxQueryTablePart part = new XlsxQueryTablePart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "queryTable.missingRoot", "Query table XML must contain a queryTable root element."));
        return part;
    }

    internal static void ReadRoot(XlsxQueryTablePart part, XmlReader reader)
    {
        if (reader.LocalName != "queryTable" || !IsQueryTableNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "queryTable.invalidRoot", "Query table XML root must be queryTable in the SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
            return;
        }

        part.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        SetOptionalUInt(part, reader.GetAttribute("connectionId"), "queryTable", "connectionId", 1);
    }

    internal static void ReadQueryTableChildren(XlsxQueryTablePart part, XmlReader reader)
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsQueryTableNamespace(reader.NamespaceURI) && reader.LocalName == "queryTableFields")
            {
                ReadQueryTableFields(part, reader);
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

    internal static void ReadQueryTableFields(XlsxQueryTablePart part, XmlReader reader)
    {
        SetOptionalUInt(part, reader.GetAttribute("count"), "queryTableFields", "count", 2);
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsQueryTableNamespace(reader.NamespaceURI) && reader.LocalName == "queryTableField")
            {
                part.Fields.Add(ReadQueryTableField(part, reader));
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

    internal static XlsxQueryTableField ReadQueryTableField(XlsxQueryTablePart part, XmlReader reader)
    {
        XlsxQueryTableField field = new XlsxQueryTableField();
        SetOptionalUInt(part, reader.GetAttribute("id"), "queryTableField", "id", field, 1);
        SetOptionalUInt(part, reader.GetAttribute("tableColumnId"), "queryTableField", "tableColumnId", field, 2);
        field.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        reader.ReadOuterXml();
        return field;
    }

    internal static void SetOptionalUInt(XlsxQueryTablePart part, string? text, string elementName, string attributeName, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            part.ConnectionId = result.UnsignedIntValue;
            part.HasConnectionId = true;
        }
        else if (target == 2)
        {
            part.QueryTableFieldCount = result.UnsignedIntValue;
            part.HasQueryTableFieldCount = true;
        }
    }

    internal static void SetOptionalUInt(XlsxQueryTablePart part, string? text, string elementName, string attributeName, XlsxQueryTableField field, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddParseDiagnostics(part, result, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            field.Id = result.UnsignedIntValue;
            field.HasId = true;
        }
        else if (target == 2)
        {
            field.TableColumnId = result.UnsignedIntValue;
            field.HasTableColumnId = true;
        }
    }

    internal static void AddParseDiagnostics(XlsxQueryTablePart part, XlsxXmlValueParseResult result, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsQueryTableNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
