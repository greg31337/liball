using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes chartsheet part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxChartsheetPartWriter : IXlsxChartsheetPartWriter
{
    /// <summary>
    /// Initializes a new chartsheet part writer with its default state.
    /// </summary>
    public XlsxChartsheetPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartsheetPartWriteResult WriteString(XlsxChartsheetPart part)
    {
        XlsxChartsheetPartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartsheetPartWriteResult WriteBytes(XlsxChartsheetPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxChartsheetPartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartsheetPartWriteResult WriteStream(Stream stream, XlsxChartsheetPart part)
    {
        XlsxChartsheetPartWriteResult result = ValidateChartsheetPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "chartsheet", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(part.NamespaceUri));
            writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteDrawing(writer, part);
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxChartsheetPartWriteResult ValidateChartsheetPart(XlsxChartsheetPart part)
    {
        XlsxChartsheetPartWriteResult result = new XlsxChartsheetPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.partNull", "Chartsheet part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.NamespaceUri == null || part.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.namespaceMissing", "Chartsheet must have a namespace URI."));
        }
        else if (!XlsxWorkbookPartReader.IsSpreadsheetNamespace(part.NamespaceUri))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.namespaceInvalid", "Chartsheet namespace URI must be a SpreadsheetML namespace."));
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.unknownRootAttributesNull", "Chartsheet unknown root attribute list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.unknownXmlNull", "Chartsheet unknown XML list must not be null."));
        }

        if (part.HasDrawing)
        {
            if (part.Drawing == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.drawingNull", "Chartsheet drawing reference must not be null when HasDrawing is true."));
            }
            else
            {
                ValidateDrawingReference(result, part.Drawing);
            }
        }

        return result;
    }

    internal static void ValidateDrawingReference(XlsxChartsheetPartWriteResult result, XlsxSheetDrawingReference drawing)
    {
        if (drawing.RelationshipId == null || drawing.RelationshipId.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.drawingRelationshipIdMissing", "Chartsheet drawing reference must have an r:id attribute."));
            return;
        }

        XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(drawing.RelationshipId);
        foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", "chartsheet", "r:id");
            result.AddDiagnostic(diagnostic);
        }

        if (drawing.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartsheet.drawingUnknownAttributesNull", "Chartsheet drawing reference unknown attribute list must not be null."));
        }
    }

    internal static void WriteDrawing(XmlWriter writer, XlsxChartsheetPart part)
    {
        if (!part.HasDrawing)
        {
            return;
        }

        XlsxWorksheetPartWriter.WriteDrawingReferenceElement(writer, part.NamespaceUri, part.Drawing);
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        foreach (string item in unknownXml)
        {
            if (item != null && item.Length > 0)
            {
                writer.WriteRaw(item);
            }
        }
    }
}
