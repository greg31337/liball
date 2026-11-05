using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads shared string table data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxSharedStringTableReader : IXlsxSharedStringTableReader
{
    /// <summary>
    /// Initializes a new shared string table reader with its default state.
    /// </summary>
    public XlsxSharedStringTableReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadString(string xml)
    {
        return ReadStringWithOptions(xml, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadStringWithOptions(string xml, XlsxReaderOptions options)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytesWithOptions(bytes, options);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadBytes(byte[] bytes)
    {
        return ReadBytesWithOptions(bytes, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadBytesWithOptions(byte[] bytes, XlsxReaderOptions options)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStreamWithOptions(stream, options);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadStream(Stream stream)
    {
        return ReadStreamWithOptions(stream, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    public XlsxSharedStringTablePart ReadStreamWithOptions(Stream stream, XlsxReaderOptions options)
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        if (options == null)
        {
            options = new XlsxReaderOptions();
        }

        XlsxValidationResult optionsValidation = options.Validate();
        AddValidationDiagnostics(part, optionsValidation);
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
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && IsSharedStringNamespace(part, reader.NamespaceURI))
                {
                    ReadSharedStringChild(part, reader, options);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTable.missingRoot", "Shared string table XML must contain an sst root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxSharedStringTablePart CreateMissingRootResult()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTable.missingRoot", "Shared string table XML must contain an sst root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxSharedStringTablePart part, XmlReader reader)
    {
        if (reader.LocalName != "sst" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTable.invalidRoot", "Shared string table XML root must be an sst element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        SetOptionalUnsignedInteger(part, reader.GetAttribute("count"), "sst", "count", 1);
        SetOptionalUnsignedInteger(part, reader.GetAttribute("uniqueCount"), "sst", "uniqueCount", 2);
        ReadUnknownAttributes(reader, part.UnknownRootAttributes, "count", "uniqueCount");
    }

    internal static void ReadSharedStringChild(XlsxSharedStringTablePart part, XmlReader reader, XlsxReaderOptions options)
    {
        if (reader.LocalName == "si")
        {
            if (part.Items.Count >= options.MaxSharedStrings)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTable.maxSharedStrings", "Shared string table exceeds the configured maximum shared string count.");
                diagnostic.SetLocation(part.PartPath, "si", "");
                part.AddDiagnostic(diagnostic);
                reader.ReadOuterXml();
                return;
            }

            XlsxSharedStringItem item = ReadSharedStringItem(part, reader);
            part.Items.Add(item);
            return;
        }

        part.UnknownXml.Add(reader.ReadOuterXml());
    }

    internal static XlsxSharedStringItem ReadSharedStringItem(XlsxSharedStringTablePart part, XmlReader reader)
    {
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        StringBuilder builder = new StringBuilder();
        ReadUnknownAttributes(reader, item.UnknownAttributes, "", "");
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return item;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                item.Text = builder.ToString();
                return item;
            }

            if (reader.NodeType == XmlNodeType.Element && IsSharedStringNamespace(part, reader.NamespaceURI))
            {
                if (reader.LocalName == "t")
                {
                    builder.Append(reader.ReadElementContentAsString());
                    continue;
                }

                if (reader.LocalName == "r")
                {
                    item.IsRichText = true;
                    XlsxSharedStringRichTextRun run = ReadRichTextRun(part, reader);
                    item.RichTextRuns.Add(run);
                    builder.Append(run.Text);
                    continue;
                }

                if (reader.LocalName == "rPh")
                {
                    item.PhoneticTexts.Add(ReadPhoneticText(reader, part.NamespaceUri));
                    continue;
                }

                if (reader.LocalName == "phoneticPr")
                {
                    item.PhoneticPropertiesRawXml = reader.ReadOuterXml();
                    item.PhoneticProperties = ReadPhoneticProperties(item.PhoneticPropertiesRawXml);
                    item.HasPhoneticProperties = true;
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                item.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        item.Text = builder.ToString();
        return item;
    }

    internal static XlsxSharedStringRichTextRun ReadRichTextRun(XlsxSharedStringTablePart part, XmlReader reader)
    {
        string rawXml = reader.ReadOuterXml();
        XlsxSharedStringRichTextRun run = ReadRichTextRunString(rawXml, part.NamespaceUri);
        run.RawXml = rawXml;
        return run;
    }

    internal static XlsxSharedStringRichTextRun ReadRichTextRunString(string xml, string namespaceUri)
    {
        XlsxRichTextRun run = XlsxRichTextRunReader.ReadRunString(xml, namespaceUri);
        return XlsxRichTextRunReader.ToSharedStringRun(run);
    }

    internal static void ReadRichTextRunRoot(XlsxSharedStringRichTextRun run, XmlReader reader, string namespaceUri)
    {
        ReadUnknownAttributes(reader, run.UnknownAttributes, "", "");
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && IsSpreadsheetNamespaceOrEmpty(namespaceUri, reader.NamespaceURI))
            {
                if (reader.LocalName == "rPr")
                {
                    run.PropertiesRawXml = reader.ReadOuterXml();
                    continue;
                }

                if (reader.LocalName == "t")
                {
                    run.Text = run.Text + reader.ReadElementContentAsString();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                run.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxPhoneticText ReadPhoneticText(XmlReader reader, string namespaceUri)
    {
        string rawXml = reader.ReadOuterXml();
        XlsxPhoneticText phoneticText = ReadPhoneticTextString(rawXml, namespaceUri);
        phoneticText.RawXml = rawXml;
        return phoneticText;
    }

    internal static XlsxPhoneticProperties ReadPhoneticProperties(string xml)
    {
        XlsxPhoneticProperties properties = new XlsxPhoneticProperties();
        if (xml == null || xml.Length == 0)
        {
            return properties;
        }
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "phoneticPr")
                    {
                        XlsxXmlValueParseResult fontId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("fontId")));
                        if (fontId.IsValid)
                        {
                            properties.FontId = fontId.UnsignedIntValue;
                            properties.HasFontId = true;
                        }
                        properties.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
                        properties.Alignment = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("alignment"));
                        ReadUnknownPhoneticPropertiesAttributes(reader, properties.UnknownAttributes);
                        return properties;
                    }
                    reader.Read();
                }
            }
        }
        return properties;
    }

    internal static void ReadUnknownPhoneticPropertiesAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == "fontId" || reader.LocalName == "type" || reader.LocalName == "alignment");
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }

    internal static XlsxPhoneticText ReadPhoneticTextString(string xml, string namespaceUri)
    {
        XlsxPhoneticText phoneticText = new XlsxPhoneticText();
        if (xml == null || xml.Length == 0)
        {
            return phoneticText;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.Read();
                        continue;
                    }

                    ReadPhoneticTextRoot(phoneticText, reader, namespaceUri);
                    return phoneticText;
                }
            }
        }

        return phoneticText;
    }

    internal static void ReadPhoneticTextRoot(XlsxPhoneticText phoneticText, XmlReader reader, string namespaceUri)
    {
        SetOptionalPhoneticIndex(phoneticText, reader.GetAttribute("sb"), 1);
        SetOptionalPhoneticIndex(phoneticText, reader.GetAttribute("eb"), 2);
        ReadUnknownAttributes(reader, phoneticText.UnknownAttributes, "sb", "eb");
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && IsSpreadsheetNamespaceOrEmpty(namespaceUri, reader.NamespaceURI) && reader.LocalName == "t")
            {
                phoneticText.Text = phoneticText.Text + reader.ReadElementContentAsString();
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                phoneticText.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void SetOptionalPhoneticIndex(XlsxPhoneticText phoneticText, string? text, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            phoneticText.StartBaseIndex = result.UnsignedIntValue;
            phoneticText.HasStartBaseIndex = true;
        }
        else if (target == 2)
        {
            phoneticText.EndBaseIndex = result.UnsignedIntValue;
            phoneticText.HasEndBaseIndex = true;
        }
    }

    internal static bool IsSharedStringNamespace(XlsxSharedStringTablePart part, string namespaceUri)
    {
        if (part == null)
        {
            return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
        }

        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }

    internal static bool IsSpreadsheetNamespaceOrEmpty(string expectedNamespaceUri, string namespaceUri)
    {
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            return true;
        }

        if (namespaceUri == expectedNamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }

    internal static void SetOptionalUnsignedInteger(XlsxSharedStringTablePart part, string? text, string elementName, string attributeName, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, elementName, attributeName);
            return;
        }

        if (target == 1)
        {
            part.Count = result.UnsignedIntValue;
            part.HasCount = true;
        }
        else if (target == 2)
        {
            part.UniqueCount = result.UnsignedIntValue;
            part.HasUniqueCount = true;
        }
    }

    internal static void AddParseDiagnostics(XlsxSharedStringTablePart part, XlsxXmlValueParseResult result, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddValidationDiagnostics(XlsxSharedStringTablePart part, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string knownName1, string knownName2)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        int index = 0;
        while (index < reader.AttributeCount)
        {
            reader.MoveToAttribute(index);
            if (!IsKnownAttribute(reader, knownName1, knownName2))
            {
                XlsxXmlAttribute attribute = new XlsxXmlAttribute();
                attribute.NamespaceUri = reader.NamespaceURI;
                attribute.Prefix = reader.Prefix;
                attribute.LocalName = reader.LocalName;
                attribute.Value = reader.Value;
                attributes.Add(attribute);
            }

            index = index + 1;
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownAttribute(XmlReader reader, string knownName1, string knownName2)
    {
        if (reader.NamespaceURI == "http://www.w3.org/2000/xmlns/")
        {
            return true;
        }

        if (reader.NamespaceURI != null && reader.NamespaceURI.Length > 0)
        {
            return false;
        }

        if (knownName1 != null && knownName1.Length > 0 && reader.LocalName == knownName1)
        {
            return true;
        }

        if (knownName2 != null && knownName2.Length > 0 && reader.LocalName == knownName2)
        {
            return true;
        }

        return false;
    }
}
