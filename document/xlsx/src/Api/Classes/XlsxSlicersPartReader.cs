using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads slicers part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxSlicersPartReader : IXlsxSlicersPartReader
{
    /// <summary>
    /// Initializes a new slicers part reader with its default state.
    /// </summary>
    public XlsxSlicersPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The slicers part produced by the operation.</returns>
    public XlsxSlicersPart ReadString(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxSlicersPart part = ReadBytes(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The slicers part produced by the operation.</returns>
    public XlsxSlicersPart ReadBytes(string partPath, byte[] bytes)
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
    /// <returns>The slicers part produced by the operation.</returns>
    public XlsxSlicersPart ReadStream(string partPath, Stream stream)
    {
        XlsxSlicersPart part = new XlsxSlicersPart();
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
                    if (reader.LocalName != "slicers")
                    {
                        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicers.invalidRoot", "Slicers XML root must be slicers.");
                        diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
                        part.AddDiagnostic(diagnostic);
                    }

                    ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);

                    reader.Read();
                    continue;
                }

                if (reader.LocalName == "slicer")
                {
                    part.Slicers.Add(ReadSlicer(part, reader.ReadOuterXml()));
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    part.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicers.missingRoot", "Slicers XML must contain a slicers root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxSlicersPart CreateMissingRootResult(string partPath)
    {
        XlsxSlicersPart part = new XlsxSlicersPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "slicers.missingRoot", "Slicers XML must contain a slicers root element."));
        return part;
    }

    internal static XlsxSlicerDefinition ReadSlicer(XlsxSlicersPart part, string xml)
    {
        XlsxSlicerDefinition slicer = new XlsxSlicerDefinition();
        slicer.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    slicer.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                    slicer.CacheName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cache"));
                    slicer.Caption = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("caption"));
                    slicer.StyleName = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("slicerStyle"));
                    ReadOptionalBool(part, reader.GetAttribute("showCaption"), slicer, 1);
                    ReadOptionalBool(part, reader.GetAttribute("lockedPosition"), slicer, 2);
                    ReadOptionalUInt(part, reader.GetAttribute("rowHeight"), slicer, 1);
                    ReadOptionalUInt(part, reader.GetAttribute("startItem"), slicer, 2);
                    ReadSlicerUnknownAttributes(reader, slicer.UnknownAttributes);
                    string? columnCountText = reader.GetAttribute("columnCount");
                    if (columnCountText != null && columnCountText.Length > 0)
                    {
                        XlsxXmlValueParseResult columnCount = XlsxXmlValueParser.ParseUnsignedInteger(columnCountText);
                        if (columnCount.IsValid)
                        {
                            slicer.ColumnCount = columnCount.UnsignedIntValue;
                            slicer.HasColumnCount = true;
                        }
                        else
                        {
                            foreach (XlsxDiagnostic diagnostic in columnCount.Validation.Diagnostics)
                            {
                                diagnostic.SetLocation(part.PartPath, "slicer", "columnCount");
                                part.AddDiagnostic(diagnostic);
                            }
                        }
                    }

                    return slicer;
                }
            }
        }

        return slicer;
    }

    internal static void ReadOptionalBool(XlsxSlicersPart part, string? text, XlsxSlicerDefinition slicer, int target)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(normalized);
        if (!parsed.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
            {
                part.AddDiagnostic(diagnostic);
            }
            return;
        }
        if (target == 1)
        {
            slicer.ShowCaption = parsed.BooleanValue;
            slicer.HasShowCaption = true;
        }
        else
        {
            slicer.LockedPosition = parsed.BooleanValue;
            slicer.HasLockedPosition = true;
        }
    }

    internal static void ReadOptionalUInt(XlsxSlicersPart part, string? text, XlsxSlicerDefinition slicer, int target)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(normalized);
        if (!parsed.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
            {
                part.AddDiagnostic(diagnostic);
            }
            return;
        }
        if (target == 1)
        {
            slicer.RowHeight = parsed.UnsignedIntValue;
            slicer.HasRowHeight = true;
        }
        else
        {
            slicer.StartItem = parsed.UnsignedIntValue;
            slicer.HasStartItem = true;
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
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }

    internal static void ReadSlicerUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            string name = reader.LocalName;
            bool known = reader.NamespaceURI == "" && (name == "name" || name == "cache" || name == "caption" || name == "columnCount" || name == "slicerStyle" || name == "showCaption" || name == "lockedPosition" || name == "rowHeight" || name == "startItem");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }
}
