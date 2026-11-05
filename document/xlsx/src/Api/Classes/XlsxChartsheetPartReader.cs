using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads chartsheet part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxChartsheetPartReader : IXlsxChartsheetPartReader
{
    /// <summary>
    /// Initializes a new chartsheet part reader with its default state.
    /// </summary>
    public XlsxChartsheetPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    public XlsxChartsheetPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxChartsheetPart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    public XlsxChartsheetPart ReadStringWithRelationships(string xml, XlsxRelationshipsPart relationships)
    {
        XlsxChartsheetPart part = ReadString(xml);
        XlsxSheetDrawingRelationshipResult result = part.ResolveDrawingRelationship(relationships);
        AddDrawingRelationshipDiagnostics(part, result);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    public XlsxChartsheetPart ReadBytes(byte[] bytes)
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
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    public XlsxChartsheetPart ReadBytesWithRelationships(byte[] bytes, XlsxRelationshipsPart relationships)
    {
        XlsxChartsheetPart part = ReadBytes(bytes);
        XlsxSheetDrawingRelationshipResult result = part.ResolveDrawingRelationship(relationships);
        AddDrawingRelationshipDiagnostics(part, result);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    public XlsxChartsheetPart ReadStream(Stream stream)
    {
        XlsxChartsheetPart part = new XlsxChartsheetPart();
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

                if (reader.Depth == 1 && IsChartsheetNamespace(part, reader.NamespaceURI))
                {
                    ReadChartsheetChild(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.missingRoot", "Chartsheet XML must contain a chartsheet root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxChartsheetPart CreateMissingRootResult()
    {
        XlsxChartsheetPart part = new XlsxChartsheetPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.missingRoot", "Chartsheet XML must contain a chartsheet root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxChartsheetPart part, XmlReader reader)
    {
        if (reader.LocalName != "chartsheet" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.invalidRoot", "Chartsheet XML root must be a chartsheet element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadChartsheetChild(XlsxChartsheetPart part, XmlReader reader)
    {
        if (reader.LocalName == "drawing")
        {
            part.Drawing = XlsxWorksheetPartReader.ReadDrawingReference(reader);
            part.HasDrawing = true;
        }
        else
        {
            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static bool IsChartsheetNamespace(XlsxChartsheetPart part, string namespaceUri)
    {
        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }

    internal static void AddDrawingRelationshipDiagnostics(XlsxChartsheetPart part, XlsxSheetDrawingRelationshipResult result)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }
}
