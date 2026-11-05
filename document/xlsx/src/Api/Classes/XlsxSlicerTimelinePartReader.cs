using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads slicer timeline part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxSlicerTimelinePartReader : IXlsxSlicerTimelinePartReader
{
    /// <summary>
    /// Initializes a new slicer timeline part reader with its default state.
    /// </summary>
    public XlsxSlicerTimelinePartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    public XlsxSlicerTimelinePart ReadString(string partPath, string xml, string expectedRootName)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath, expectedRootName);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxSlicerTimelinePart part = ReadBytes(partPath, bytes, expectedRootName);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    public XlsxSlicerTimelinePart ReadBytes(string partPath, byte[] bytes, string expectedRootName)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult(partPath, expectedRootName);
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(partPath, stream, expectedRootName);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    public XlsxSlicerTimelinePart ReadStream(string partPath, Stream stream, string expectedRootName)
    {
        XlsxSlicerTimelinePart part = new XlsxSlicerTimelinePart();
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
                    ReadRootElement(part, reader, expectedRootName);
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

                            if (reader.NodeType == XmlNodeType.Element && reader.Depth == rootDepth + 1)
                            {
                                string childXml = reader.ReadOuterXml();
                                ReadChildItem(part, childXml);
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
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimeline.missingRoot", "Slicer or timeline XML must contain the expected root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxSlicerTimelinePart CreateMissingRootResult(string partPath, string expectedRootName)
    {
        XlsxSlicerTimelinePart part = new XlsxSlicerTimelinePart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.RootElementName = XlsxCellValueConverter.NormalizeText(expectedRootName);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimeline.missingRoot", "Slicer or timeline XML must contain the expected root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxSlicerTimelinePart part, XmlReader reader, string expectedRootName)
    {
        part.RootElementName = reader.LocalName;
        part.NamespaceUri = reader.NamespaceURI;
        if (reader.LocalName != expectedRootName)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicerTimeline.invalidRoot", "Slicer or timeline XML root does not match the expected element.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
        part.SourceName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sourceName"));
        ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadChildItem(XlsxSlicerTimelinePart part, string xml)
    {
        bool recognized = false;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "slicer" || reader.LocalName == "timeline")
                    {
                        XlsxSlicerTimelineItem item = new XlsxSlicerTimelineItem();
                        item.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                        item.CacheName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cache"));
                        item.Caption = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("caption"));
                        item.StyleName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("style"));
                        ReadItemUInt(reader.GetAttribute("level"), item, 1);
                        ReadItemUInt(reader.GetAttribute("selectionLevel"), item, 2);
                        ReadItemUInt(reader.GetAttribute("scrollPosition"), item, 3);
                        item.RawXml = xml;
                        part.Items.Add(item);
                        recognized = true;
                        reader.ReadOuterXml();
                        continue;
                    }
                    if (reader.LocalName == "pivotTable")
                    {
                        XlsxSlicerPivotTableReference reference = new XlsxSlicerPivotTableReference();
                        reference.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                        XlsxXmlValueParseResult tabId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("tabId")));
                        if (tabId.IsValid)
                        {
                            reference.TabId = tabId.UnsignedIntValue;
                            reference.HasTabId = true;
                        }
                        part.PivotTables.Add(reference);
                        recognized = true;
                        reader.ReadOuterXml();
                        continue;
                    }
                    if (reader.LocalName == "state")
                    {
                        ReadState(part, reader, xml);
                        recognized = true;
                    }
                    if (reader.LocalName == "extLst")
                    {
                        part.ExtensionListRawXml = reader.ReadOuterXml();
                        recognized = true;
                        continue;
                    }
                }
                reader.Read();
            }
        }
        if (!recognized)
        {
            part.UnknownXml.Add(xml);
        }
    }

    internal static void ReadState(XlsxSlicerTimelinePart part, XmlReader reader, string xml)
    {
        XlsxTimelineState state = new XlsxTimelineState();
        state.FilterType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("filterType"));
        ReadStateUInt(reader.GetAttribute("pivotCacheId"), state, 1);
        ReadStateUInt(reader.GetAttribute("filterId"), state, 2);
        ReadStateBool(reader.GetAttribute("singleRange"), state);
        using (StringReader stateReader = new StringReader(xml))
        using (XmlReader child = XmlReader.Create(stateReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (child.Read())
            {
                if (child.NodeType == XmlNodeType.Element && child.LocalName == "selection")
                {
                    XlsxXmlValueParseResult start = XlsxXmlValueParser.ParseDateTime(XlsxCellValueConverter.NormalizeText(child.GetAttribute("startDate")));
                    XlsxXmlValueParseResult end = XlsxXmlValueParser.ParseDateTime(XlsxCellValueConverter.NormalizeText(child.GetAttribute("endDate")));
                    if (start.IsValid && end.IsValid)
                    {
                        state.StartDate = start.DateTimeValue;
                        state.EndDate = end.DateTimeValue;
                        state.HasSelection = true;
                    }
                    ReadStateUInt(child.GetAttribute("level"), state, 3);
                }
            }
        }
        part.State = state;
        part.HasState = true;
    }

    internal static void ReadItemUInt(string? text, XlsxSlicerTimelineItem item, int target)
    {
        XlsxXmlValueParseResult value = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(text));
        if (!value.IsValid || text == null || text.Length == 0)
        {
            return;
        }
        if (target == 1)
        {
            item.Level = value.UnsignedIntValue;
            item.HasLevel = true;
        }
        else if (target == 2)
        {
            item.SelectionLevel = value.UnsignedIntValue;
            item.HasSelectionLevel = true;
        }
        else
        {
            item.ScrollPosition = value.UnsignedIntValue;
            item.HasScrollPosition = true;
        }
    }

    internal static void ReadStateUInt(string? text, XlsxTimelineState state, int target)
    {
        XlsxXmlValueParseResult value = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(text));
        if (!value.IsValid || text == null || text.Length == 0)
        {
            return;
        }
        if (target == 1)
        {
            state.PivotCacheId = value.UnsignedIntValue;
            state.HasPivotCacheId = true;
        }
        else if (target == 2)
        {
            state.FilterId = value.UnsignedIntValue;
            state.HasFilterId = true;
        }
        else
        {
            state.Level = value.UnsignedIntValue;
            state.HasLevel = true;
        }
    }

    internal static void ReadStateBool(string? text, XlsxTimelineState state)
    {
        XlsxXmlValueParseResult value = XlsxXmlValueParser.ParseBoolean(XlsxCellValueConverter.NormalizeText(text));
        if (value.IsValid && text != null && text.Length > 0)
        {
            state.SingleRange = value.BooleanValue;
            state.HasSingleRange = true;
        }
    }

    internal static void ReadUnknownRootAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "name" || reader.LocalName == "sourceName");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }
}
