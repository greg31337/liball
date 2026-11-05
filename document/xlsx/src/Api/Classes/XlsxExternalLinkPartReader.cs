using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads external link part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxExternalLinkPartReader : IXlsxExternalLinkPartReader
{
    /// <summary>
    /// Initializes a new external link part reader with its default state.
    /// </summary>
    public XlsxExternalLinkPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The external link part produced by the operation.</returns>
    public XlsxExternalLinkPart ReadString(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxExternalLinkPart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The external link part produced by the operation.</returns>
    public XlsxExternalLinkPart ReadStringWithRelationships(string partPath, string xml, XlsxRelationshipsPart relationships)
    {
        XlsxExternalLinkPart part = ReadString(partPath, xml);
        part.ResolveExternalBookRelationship(relationships);
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The external link part produced by the operation.</returns>
    public XlsxExternalLinkPart ReadBytes(string partPath, byte[] bytes)
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
    /// <returns>The external link part produced by the operation.</returns>
    public XlsxExternalLinkPart ReadStream(string partPath, Stream stream)
    {
        XlsxExternalLinkPart part = new XlsxExternalLinkPart();
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
                        int rootDepth = reader.Depth;
                        reader.Read();
                        while (!reader.EOF)
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth)
                            {
                                reader.Read();
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                ReadChild(part, reader);
                                continue;
                            }

                            reader.Read();
                        }
                    }

                    continue;
                }

                reader.ReadOuterXml();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLink.missingRoot", "External link XML must contain an externalLink root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxExternalLinkPart CreateMissingRootResult(string partPath)
    {
        XlsxExternalLinkPart part = new XlsxExternalLinkPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLink.missingRoot", "External link XML must contain an externalLink root element."));
        return part;
    }

    internal static void ReadRoot(XlsxExternalLinkPart part, XmlReader reader)
    {
        if (reader.LocalName != "externalLink" || reader.NamespaceURI != XlsxNamespaces.Spreadsheet)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "externalLink.invalidRoot", "External link XML root must be externalLink in the SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadChild(XlsxExternalLinkPart part, XmlReader reader)
    {
        if (reader.LocalName == "externalBook")
        {
            part.RelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships));
            if (reader.IsEmptyElement)
            {
                reader.Read();
            }
            else
            {
                int depth = reader.Depth;
                reader.Read();
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                    {
                        reader.Read();
                        break;
                    }

                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheetName")
                    {
                        string value = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        if (value.Length > 0)
                        {
                            part.SheetNames.Add(value);
                        }

                        reader.ReadOuterXml();
                        continue;
                    }

                    reader.Read();
                }
            }

            return;
        }

        reader.ReadOuterXml();
    }
}
