using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads legacy comment drawing data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxLegacyCommentDrawingReader : IXlsxLegacyCommentDrawingReader
{
    /// <summary>
    /// Initializes a new legacy comment drawing reader with its default state.
    /// </summary>
    public XlsxLegacyCommentDrawingReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The legacy comment drawing part produced by the operation.</returns>
    public XlsxLegacyCommentDrawingPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxLegacyCommentDrawingPart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The legacy comment drawing part produced by the operation.</returns>
    public XlsxLegacyCommentDrawingPart ReadBytes(byte[] bytes)
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
    /// <returns>The legacy comment drawing part produced by the operation.</returns>
    public XlsxLegacyCommentDrawingPart ReadStream(Stream stream)
    {
        XlsxLegacyCommentDrawingPart part = new XlsxLegacyCommentDrawingPart();
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
                    if (reader.LocalName != "xml" && reader.LocalName != "shape")
                    {
                        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawing.invalidRoot", "Legacy comment drawing XML root must be an xml or shape element.");
                        diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
                        part.AddDiagnostic(diagnostic);
                    }
                }

                if (reader.LocalName == "shape")
                {
                    part.Drawings.Add(ReadShape(part, reader.ReadOuterXml()));
                    continue;
                }

                if (reader.Depth > 0)
                {
                    part.UnknownXml.Add(reader.ReadOuterXml());
                    continue;
                }

                reader.Read();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawing.missingRoot", "Legacy comment drawing XML must contain a root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxLegacyCommentDrawingPart CreateMissingRootResult()
    {
        XlsxLegacyCommentDrawingPart part = new XlsxLegacyCommentDrawingPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "legacyCommentDrawing.missingRoot", "Legacy comment drawing XML must contain a root element."));
        return part;
    }

    internal static XlsxLegacyCommentDrawing ReadShape(XlsxLegacyCommentDrawingPart part, string xml)
    {
        XlsxLegacyCommentDrawing drawing = new XlsxLegacyCommentDrawing();
        drawing.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "shape")
                    {
                        drawing.ShapeId = reader.GetAttribute("id");
                        drawing.Style = reader.GetAttribute("style");
                        ApplyStylePosition(drawing);
                        reader.Read();
                        continue;
                    }

                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "ClientData")
                    {
                        ReadClientData(part, drawing, reader);
                        continue;
                    }

                    reader.Read();
                }
            }
        }

        if (drawing.HasRow && drawing.HasColumn)
        {
            drawing.Reference = XlsxA1CellReferenceConverter.FormatString(drawing.Row + 1, drawing.Column + 1, false, false);
        }

        return drawing;
    }

    internal static void ReadClientData(XlsxLegacyCommentDrawingPart part, XlsxLegacyCommentDrawing drawing, XmlReader reader)
    {
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1)
            {
                ReadClientDataElement(part, drawing, reader);
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadClientDataElement(XlsxLegacyCommentDrawingPart part, XlsxLegacyCommentDrawing drawing, XmlReader reader)
    {
        if (reader.LocalName == "Anchor")
        {
            drawing.Anchor = reader.ReadElementContentAsString();
        }
        else if (reader.LocalName == "Row")
        {
            SetOptionalInt(part, drawing, reader.ReadElementContentAsString(), "Row", 1);
        }
        else if (reader.LocalName == "Column")
        {
            SetOptionalInt(part, drawing, reader.ReadElementContentAsString(), "Column", 2);
        }
        else if (reader.LocalName == "Visible")
        {
            drawing.Visible = true;
            drawing.HasVisible = true;
            reader.ReadOuterXml();
        }
        else if (reader.LocalName == "MoveWithCells")
        {
            drawing.MoveWithCells = true;
            reader.ReadOuterXml();
        }
        else if (reader.LocalName == "SizeWithCells")
        {
            drawing.SizeWithCells = true;
            reader.ReadOuterXml();
        }
        else
        {
            drawing.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void SetOptionalInt(XlsxLegacyCommentDrawingPart part, XlsxLegacyCommentDrawing drawing, string text, string elementName, int target)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, "");
            part.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            drawing.Row = (int)result.UnsignedIntValue;
            drawing.HasRow = true;
        }
        else if (target == 2)
        {
            drawing.Column = (int)result.UnsignedIntValue;
            drawing.HasColumn = true;
        }
    }

    internal static void ApplyStylePosition(XlsxLegacyCommentDrawing drawing)
    {
        drawing.Left = GetStyleProperty(drawing.Style, "margin-left");
        drawing.Top = GetStyleProperty(drawing.Style, "margin-top");
        drawing.Width = GetStyleProperty(drawing.Style, "width");
        drawing.Height = GetStyleProperty(drawing.Style, "height");
        string visibility = GetStyleProperty(drawing.Style, "visibility");
        if (visibility.Length > 0)
        {
            drawing.Visible = string.Equals(visibility, "visible", System.StringComparison.OrdinalIgnoreCase);
            drawing.HasVisible = true;
        }
    }

    internal static string GetStyleProperty(string? style, string name)
    {
        if (style == null || name == null)
        {
            return "";
        }

        string[] items = style.Split(';');
        foreach (string item in items)
        {
            int colonIndex = item.IndexOf(':');
            if (colonIndex > 0)
            {
                string key = item.Substring(0, colonIndex).Trim();
                string value = item.Substring(colonIndex + 1).Trim();
                if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }
        }

        return "";
    }
}
