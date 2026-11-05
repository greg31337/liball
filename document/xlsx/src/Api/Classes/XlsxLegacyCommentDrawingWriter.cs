using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes legacy comment drawing data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxLegacyCommentDrawingWriter : IXlsxLegacyCommentDrawingWriter
{
    /// <summary>
    /// Initializes a new legacy comment drawing writer with its default state.
    /// </summary>
    public XlsxLegacyCommentDrawingWriter()
    {
    }

    /// <summary>
    /// Identifies the vml namespace OOXML value.
    /// </summary>
    internal const string VmlNamespace = "urn:schemas-microsoft-com:vml";
    /// <summary>
    /// Identifies the excel namespace OOXML value.
    /// </summary>
    internal const string ExcelNamespace = "urn:schemas-microsoft-com:office:excel";
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxLegacyCommentDrawingWriteResult WriteString(XlsxLegacyCommentDrawingPart part)
    {
        XlsxLegacyCommentDrawingWriteResult result = WriteBytes(part);
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
    public XlsxLegacyCommentDrawingWriteResult WriteBytes(XlsxLegacyCommentDrawingPart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxLegacyCommentDrawingWriteResult result = WriteStream(stream, part);
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
    public XlsxLegacyCommentDrawingWriteResult WriteStream(Stream stream, XlsxLegacyCommentDrawingPart? part)
    {
        XlsxLegacyCommentDrawingWriteResult result = ValidateDrawingPart(part);
        if (!result.IsValid || part == null)
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
            writer.WriteStartElement("", "xml", "");
            writer.WriteAttributeString("xmlns", "v", "http://www.w3.org/2000/xmlns/", VmlNamespace);
            writer.WriteAttributeString("xmlns", "x", "http://www.w3.org/2000/xmlns/", ExcelNamespace);
            foreach (XlsxLegacyCommentDrawing drawing in part.Drawings)
            {
                WriteDrawing(writer, drawing);
            }

            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxLegacyCommentDrawingWriteResult ValidateDrawingPart(XlsxLegacyCommentDrawingPart? part)
    {
        XlsxLegacyCommentDrawingWriteResult result = new XlsxLegacyCommentDrawingWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.partNull", "Legacy comment drawing part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.Drawings == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.drawingsNull", "Legacy comment drawing list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.unknownXmlNull", "Legacy comment drawing unknown XML list must not be null."));
        }

        if (part.Drawings != null)
        {
            foreach (XlsxLegacyCommentDrawing drawing in part.Drawings)
            {
                ValidateDrawing(result, part, drawing);
            }
        }

        return result;
    }

    internal static void ValidateDrawing(XlsxLegacyCommentDrawingWriteResult result, XlsxLegacyCommentDrawingPart part, XlsxLegacyCommentDrawing drawing)
    {
        if (drawing == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.drawingNull", "Legacy comment drawing list must not contain null entries."));
            return;
        }

        if (drawing.RawXml != null && drawing.RawXml.Length > 0)
        {
            return;
        }

        if (!drawing.HasRow || !drawing.HasColumn)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.positionMissing", "Legacy comment drawing must have row and column positions.");
            diagnostic.SetLocation(part.PartPath, "ClientData", "");
            result.AddDiagnostic(diagnostic);
        }

        if (drawing.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawingWriter.drawingUnknownXmlNull", "Legacy comment drawing unknown XML list must not be null."));
        }
    }

    internal static void WriteDrawing(XmlWriter writer, XlsxLegacyCommentDrawing drawing)
    {
        if (drawing.RawXml != null && drawing.RawXml.Length > 0)
        {
            writer.WriteRaw(drawing.RawXml);
            return;
        }

        writer.WriteStartElement("v", "shape", VmlNamespace);
        if (drawing.ShapeId != null && drawing.ShapeId.Length > 0)
        {
            writer.WriteAttributeString("id", drawing.ShapeId);
        }

        writer.WriteAttributeString("style", BuildStyle(drawing));
        writer.WriteStartElement("x", "ClientData", ExcelNamespace);
        writer.WriteAttributeString("ObjectType", "Note");
        if (drawing.MoveWithCells)
        {
            writer.WriteStartElement("x", "MoveWithCells", ExcelNamespace);
            writer.WriteEndElement();
        }

        if (drawing.SizeWithCells)
        {
            writer.WriteStartElement("x", "SizeWithCells", ExcelNamespace);
            writer.WriteEndElement();
        }

        if (drawing.Anchor != null && drawing.Anchor.Length > 0)
        {
            writer.WriteStartElement("x", "Anchor", ExcelNamespace);
            writer.WriteString(drawing.Anchor);
            writer.WriteEndElement();
        }

        if (drawing.HasRow)
        {
            writer.WriteStartElement("x", "Row", ExcelNamespace);
            writer.WriteString(drawing.Row.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        if (drawing.HasColumn)
        {
            writer.WriteStartElement("x", "Column", ExcelNamespace);
            writer.WriteString(drawing.Column.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        if (drawing.HasVisible && drawing.Visible)
        {
            writer.WriteStartElement("x", "Visible", ExcelNamespace);
            writer.WriteEndElement();
        }

        WriteUnknownXml(writer, drawing.UnknownXml);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static string BuildStyle(XlsxLegacyCommentDrawing drawing)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("position:absolute");
        AppendStyle(builder, "margin-left", drawing.Left);
        AppendStyle(builder, "margin-top", drawing.Top);
        AppendStyle(builder, "width", drawing.Width);
        AppendStyle(builder, "height", drawing.Height);
        if (drawing.HasVisible)
        {
            if (drawing.Visible)
            {
                AppendStyle(builder, "visibility", "visible");
            }
            else
            {
                AppendStyle(builder, "visibility", "hidden");
            }
        }

        return builder.ToString();
    }

    internal static void AppendStyle(StringBuilder builder, string name, string value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        builder.Append(";");
        builder.Append(name);
        builder.Append(":");
        builder.Append(value);
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        if (unknownXml == null)
        {
            return;
        }

        foreach (string item in unknownXml)
        {
            if (item != null && item.Length > 0)
            {
                writer.WriteRaw(item);
            }
        }
    }
}
