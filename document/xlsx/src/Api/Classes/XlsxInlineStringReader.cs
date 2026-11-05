using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads inline string data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxInlineStringReader : IXlsxInlineStringReader
{
    /// <summary>
    /// Initializes a new inline string reader with its default state.
    /// </summary>
    public XlsxInlineStringReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult ReadBytes(byte[] bytes)
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
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult ReadStream(Stream stream)
    {
        XlsxInlineStringResult result = new XlsxInlineStringResult();
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
                    if (reader.LocalName != "is" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inlineString.invalidRoot", "Inline string XML root must be an is element in a SpreadsheetML namespace."));
                        reader.ReadOuterXml();
                        continue;
                    }

                    result.Value = ReadInlineStringElement(reader, reader.NamespaceURI);
                    continue;
                }

                result.Value.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inlineString.missingRoot", "Inline string XML must contain an is root element."));
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static XlsxInlineStringResult CreateMissingRootResult()
    {
        XlsxInlineStringResult result = new XlsxInlineStringResult();
        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inlineString.missingRoot", "Inline string XML must contain an is root element."));
        return result;
    }

    internal static XlsxInlineString ReadInlineStringElement(XmlReader reader, string namespaceUri)
    {
        XlsxInlineString value = new XlsxInlineString();
        StringBuilder builder = new StringBuilder();
        XlsxSharedStringTableReader.ReadUnknownAttributes(reader, value.UnknownAttributes, "", "");
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return value;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                value.Text = builder.ToString();
                return value;
            }

            if (reader.NodeType == XmlNodeType.Element && XlsxSharedStringTableReader.IsSpreadsheetNamespaceOrEmpty(namespaceUri, reader.NamespaceURI))
            {
                if (reader.LocalName == "t")
                {
                    builder.Append(reader.ReadElementContentAsString());
                    continue;
                }

                if (reader.LocalName == "r")
                {
                    value.IsRichText = true;
                    XlsxSharedStringRichTextRun run = ReadInlineRichTextRun(reader, namespaceUri);
                    value.RichTextRuns.Add(run);
                    builder.Append(run.Text);
                    continue;
                }

                if (reader.LocalName == "rPh")
                {
                    value.PhoneticTexts.Add(XlsxSharedStringTableReader.ReadPhoneticText(reader, namespaceUri));
                    continue;
                }

                if (reader.LocalName == "phoneticPr")
                {
                    value.PhoneticPropertiesRawXml = reader.ReadOuterXml();
                    value.PhoneticProperties = XlsxSharedStringTableReader.ReadPhoneticProperties(value.PhoneticPropertiesRawXml);
                    value.HasPhoneticProperties = true;
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                value.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        value.Text = builder.ToString();
        return value;
    }

    internal static XlsxSharedStringRichTextRun ReadInlineRichTextRun(XmlReader reader, string namespaceUri)
    {
        string rawXml = reader.ReadOuterXml();
        XlsxSharedStringRichTextRun run = XlsxSharedStringTableReader.ReadRichTextRunString(rawXml, namespaceUri);
        run.RawXml = rawXml;
        return run;
    }
}
