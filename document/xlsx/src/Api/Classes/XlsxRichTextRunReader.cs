using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads rich text run data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxRichTextRunReader : IXlsxRichTextRunReader
{
    /// <summary>
    /// Initializes a new rich text run reader with its default state.
    /// </summary>
    public XlsxRichTextRunReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRichTextRunResult ReadString(string xml)
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
    public XlsxRichTextRunResult ReadBytes(byte[] bytes)
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
    public XlsxRichTextRunResult ReadStream(Stream stream)
    {
        XlsxRichTextRunResult result = new XlsxRichTextRunResult();
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
                    if (reader.LocalName != "r" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "richTextRun.invalidRoot", "Rich text run XML root must be an r element in a SpreadsheetML namespace."));
                        reader.ReadOuterXml();
                        continue;
                    }

                    result.Run = ReadRunElement(reader, reader.NamespaceURI);
                    continue;
                }

                result.Run.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "richTextRun.missingRoot", "Rich text run XML must contain an r root element."));
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static XlsxRichTextRunResult CreateMissingRootResult()
    {
        XlsxRichTextRunResult result = new XlsxRichTextRunResult();
        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "richTextRun.missingRoot", "Rich text run XML must contain an r root element."));
        return result;
    }

    internal static XlsxRichTextRun ReadRunElement(XmlReader reader, string namespaceUri)
    {
        XlsxRichTextRun run = new XlsxRichTextRun();
        XlsxSharedStringTableReader.ReadUnknownAttributes(reader, run.UnknownAttributes, "", "");
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return run;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return run;
            }

            if (reader.NodeType == XmlNodeType.Element && XlsxSharedStringTableReader.IsSpreadsheetNamespaceOrEmpty(namespaceUri, reader.NamespaceURI))
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

        return run;
    }

    internal static XlsxRichTextRun ReadRunString(string xml, string namespaceUri)
    {
        XlsxRichTextRun run = new XlsxRichTextRun();
        if (xml == null || xml.Length == 0)
        {
            return run;
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

                    run = ReadRunElement(reader, namespaceUri);
                    run.RawXml = xml;
                    return run;
                }
            }
        }

        return run;
    }

    internal static XlsxSharedStringRichTextRun ToSharedStringRun(XlsxRichTextRun run)
    {
        XlsxSharedStringRichTextRun value = new XlsxSharedStringRichTextRun();
        if (run == null)
        {
            return value;
        }

        value.Text = XlsxCellValueConverter.NormalizeText(run.Text);
        value.RawXml = XlsxCellValueConverter.NormalizeText(run.RawXml);
        value.PropertiesRawXml = XlsxCellValueConverter.NormalizeText(run.PropertiesRawXml);
        foreach (XlsxXmlAttribute attribute in run.UnknownAttributes)
        {
            value.UnknownAttributes.Add(attribute);
        }

        foreach (string unknownXml in run.UnknownXml)
        {
            value.UnknownXml.Add(unknownXml);
        }

        return value;
    }
}
