using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads slicer cache part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxSlicerCachePartReader : IXlsxSlicerCachePartReader
{
    /// <summary>
    /// Initializes a new slicer cache part reader with its default state.
    /// </summary>
    public XlsxSlicerCachePartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The slicer cache part produced by the operation.</returns>
    public XlsxSlicerCachePart ReadString(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxSlicerCachePart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The slicer cache part produced by the operation.</returns>
    public XlsxSlicerCachePart ReadBytes(string partPath, byte[] bytes)
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
    /// <returns>The slicer cache part produced by the operation.</returns>
    public XlsxSlicerCachePart ReadStream(string partPath, Stream stream)
    {
        XlsxSlicerCachePart part = new XlsxSlicerCachePart();
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
                    ReadRootElement(part, reader);
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
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCache.missingRoot", "Slicer cache XML must contain a slicerCacheDefinition root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxSlicerCachePart CreateMissingRootResult(string partPath)
    {
        XlsxSlicerCachePart part = new XlsxSlicerCachePart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCache.missingRoot", "Slicer cache XML must contain a slicerCacheDefinition root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxSlicerCachePart part, XmlReader reader)
    {
        if (reader.LocalName != "slicerCacheDefinition")
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerCache.invalidRoot", "Slicer cache XML root must be slicerCacheDefinition.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        part.SourceName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sourceName"));
        ReadUnknownAttributes(reader, part.UnknownRootAttributes, "name", "sourceName");
    }

    internal static void ReadChild(XlsxSlicerCachePart part, XmlReader reader)
    {
        if (reader.LocalName == "tableSlicerCache")
        {
            part.HasTableSlicerCache = true;
            part.TableColumnName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("column"));
            part.CrossFilter = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("crossFilter"));
            part.SortOrder = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sortOrder"));
            string? tableIdText = reader.GetAttribute("tableId");
            if (tableIdText != null && tableIdText.Length > 0)
            {
                XlsxXmlValueParseResult tableId = XlsxXmlValueParser.ParseUnsignedInteger(tableIdText);
                if (tableId.IsValid)
                {
                    part.TableId = tableId.UnsignedIntValue;
                    part.HasTableId = true;
                }
                else
                {
                    foreach (XlsxDiagnostic diagnostic in tableId.Validation.Diagnostics)
                    {
                        diagnostic.SetLocation(part.PartPath, "tableSlicerCache", "tableId");
                        part.AddDiagnostic(diagnostic);
                    }
                }
            }

            reader.ReadOuterXml();
            return;
        }

        if (reader.LocalName == "pivotTable")
        {
            part.PivotTables.Add(ReadPivotTableReference(part, reader.ReadOuterXml()));
            return;
        }

        if (reader.LocalName == "slicerCacheItem")
        {
            XlsxSlicerSelectionItem item = new XlsxSlicerSelectionItem();
            XlsxXmlValueParseResult index = XlsxXmlValueParser.ParseInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("x")));
            if (index.IsValid)
            {
                item.Index = index.IntValue;
            }
            item.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("n"));
            string selectedText = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("s"));
            if (selectedText.Length > 0)
            {
                XlsxXmlValueParseResult selected = XlsxXmlValueParser.ParseBoolean(selectedText);
                if (selected.IsValid)
                {
                    item.Selected = selected.BooleanValue;
                    item.HasSelected = true;
                }
            }
            part.Selections.Add(item);
            reader.ReadOuterXml();
            return;
        }

        if (reader.LocalName == "extLst")
        {
            part.ExtensionListRawXml = reader.ReadOuterXml();
            return;
        }

        if (reader.LocalName != "pivotTables" && reader.LocalName != "slicerCacheItems")
        {
            part.UnknownXml.Add(reader.ReadOuterXml());
            return;
        }

        reader.Read();
    }

    internal static XlsxSlicerPivotTableReference ReadPivotTableReference(XlsxSlicerCachePart part, string xml)
    {
        XlsxSlicerPivotTableReference reference = new XlsxSlicerPivotTableReference();
        reference.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    reference.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    string? tabIdText = reader.GetAttribute("tabId");
                    if (tabIdText != null && tabIdText.Length > 0)
                    {
                        XlsxXmlValueParseResult tabId = XlsxXmlValueParser.ParseUnsignedInteger(tabIdText);
                        if (tabId.IsValid)
                        {
                            reference.TabId = tabId.UnsignedIntValue;
                            reference.HasTabId = true;
                        }
                        else
                        {
                            foreach (XlsxDiagnostic diagnostic in tabId.Validation.Diagnostics)
                            {
                                diagnostic.SetLocation(part.PartPath, "pivotTable", "tabId");
                                part.AddDiagnostic(diagnostic);
                            }
                        }
                    }

                    return reference;
                }
            }
        }

        return reference;
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string firstKnown, string secondKnown)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == firstKnown || reader.LocalName == secondKnown);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }
}
