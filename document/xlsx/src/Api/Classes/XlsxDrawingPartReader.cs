using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads drawing part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxDrawingPartReader : IXlsxDrawingPartReader
{
    /// <summary>
    /// Initializes a new drawing part reader with its default state.
    /// </summary>
    public XlsxDrawingPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The drawing part produced by the operation.</returns>
    public XlsxDrawingPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxDrawingPart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The drawing part produced by the operation.</returns>
    public XlsxDrawingPart ReadBytes(byte[] bytes)
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
    /// <returns>The drawing part produced by the operation.</returns>
    public XlsxDrawingPart ReadStream(Stream stream)
    {
        XlsxDrawingPart part = new XlsxDrawingPart();
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

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && IsAnchorElement(reader.LocalName))
                {
                    string elementName = reader.LocalName;
                    string rawXml = reader.ReadOuterXml();
                    string anchorKind = GetAnchorPayloadKind(rawXml);
                    if (anchorKind == "image")
                    {
                        XlsxDrawingImageAnchor anchor = ReadAnchor(rawXml, elementName);
                        part.ImageAnchors.Add(anchor);
                    }
                    else if (anchorKind == "chart")
                    {
                        AddChartAnchor(part, rawXml, elementName);
                    }
                    else
                    {
                        AddRawAnchor(part, rawXml, elementName, anchorKind);
                    }

                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawing.missingRoot", "Drawing XML must contain a wsDr root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxDrawingPart CreateMissingRootResult()
    {
        XlsxDrawingPart part = new XlsxDrawingPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawing.missingRoot", "Drawing XML must contain a wsDr root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxDrawingPart part, XmlReader reader)
    {
        if (reader.LocalName != "wsDr" || reader.NamespaceURI != XlsxNamespaces.SpreadsheetDrawing)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawing.invalidRoot", "Drawing XML root must be a SpreadsheetDrawing wsDr element."));
        }

        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static XlsxDrawingImageAnchor ReadAnchor(string rawXml, string elementName)
    {
        XlsxDrawingImageAnchor anchor = new XlsxDrawingImageAnchor();
        anchor.RawXml = rawXml;
        anchor.AnchorType = GetAnchorType(elementName);
        anchor.AnchorKind = GetAnchorKind(anchor.AnchorType);
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        ReadAnchorElement(anchor, reader);
                        continue;
                    }

                    reader.Read();
                }
            }
        }

        ReadImageHyperlink(anchor, rawXml);
        return anchor;
    }

    internal static string GetAnchorPayloadKind(string rawXml)
    {
        bool sawPicture = false;
        bool sawShape = false;
        bool sawTextBody = false;
        bool sawConnector = false;
        bool sawControl = false;
        bool sawChart = false;
        bool sawDiagram = false;
        bool sawTextBoxMarker = false;
        bool sawPresetGeometry = false;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing)
                    {
                        string localName = reader.LocalName;
                        if (localName == "pic")
                        {
                            sawPicture = true;
                        }
                        else if (localName == "sp")
                        {
                            sawShape = true;
                        }
                        else if (localName == "txBody")
                        {
                            sawTextBody = true;
                        }
                        else if (localName == "cxnSp")
                        {
                            sawConnector = true;
                        }
                        else if (localName == "control")
                        {
                            sawControl = true;
                        }
                        else if (localName == "cNvSpPr" && XlsxCellValueConverter.NormalizeText(reader.GetAttribute("txBox")) == "1")
                        {
                            sawTextBoxMarker = true;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "chart")
                    {
                        sawChart = true;
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == "http://schemas.openxmlformats.org/drawingml/2006/diagram")
                    {
                        sawDiagram = true;
                    }

                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "graphicData")
                    {
                        string uri = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("uri"));
                        if (uri.Contains("diagram", System.StringComparison.OrdinalIgnoreCase))
                        {
                            sawDiagram = true;
                        }
                    }

                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "prstGeom")
                    {
                        sawPresetGeometry = true;
                    }

                    reader.Read();
                }
            }
        }

        if (sawPicture)
        {
            return "image";
        }

        if (sawConnector)
        {
            return "connector";
        }

        if (sawControl)
        {
            return "formControl";
        }

        if (sawChart)
        {
            return "chart";
        }

        if (sawDiagram)
        {
            return "diagram";
        }

        if (sawShape && (sawTextBoxMarker || (sawTextBody && !sawPresetGeometry)))
        {
            return "textBox";
        }

        if (sawShape)
        {
            return "shape";
        }

        return "unknown";
    }

    internal static void AddChartAnchor(XlsxDrawingPart part, string rawXml, string elementName)
    {
        XlsxDrawingChartAnchor anchor = ReadChartAnchor(rawXml, elementName);
        part.ChartAnchors.Add(anchor);
        XlsxDrawingRawAnchor rawAnchor = new XlsxDrawingRawAnchor();
        rawAnchor.AnchorType = anchor.AnchorType;
        rawAnchor.Kind = "chart";
        rawAnchor.Name = anchor.Name;
        rawAnchor.RelationshipId = anchor.RelationshipId;
        rawAnchor.Hyperlink = anchor.Hyperlink;
        rawAnchor.HasHyperlink = anchor.HasHyperlink;
        rawAnchor.RawXml = rawXml;
        part.RawAnchors.Add(rawAnchor);
    }

    internal static XlsxDrawingChartAnchor ReadChartAnchor(string rawXml, string elementName)
    {
        XlsxDrawingChartAnchor anchor = new XlsxDrawingChartAnchor();
        anchor.RawXml = rawXml;
        anchor.AnchorType = GetAnchorType(elementName);
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "cNvPr" && anchor.Name.Length == 0)
                        {
                            anchor.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                        }
                        else if (reader.LocalName == "hlinkClick")
                        {
                            anchor.Hyperlink = ReadDrawingHyperlink(reader);
                            anchor.HasHyperlink = true;
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "chart")
                        {
                            anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships));
                        }
                    }

                    reader.Read();
                }
            }
        }

        return anchor;
    }

    internal static void AddRawAnchor(XlsxDrawingPart part, string rawXml, string elementName, string kind)
    {
        XlsxDrawingRawAnchor anchor = new XlsxDrawingRawAnchor();
        anchor.RawXml = rawXml;
        anchor.AnchorType = GetAnchorType(elementName);
        anchor.AnchorKind = GetAnchorKind(anchor.AnchorType);
        anchor.Kind = kind;
        anchor.ObjectKind = GetObjectKind(kind);
        ReadRawAnchorMetadata(anchor);
        part.RawAnchors.Add(anchor);
        if (kind == "textBox")
        {
            part.TextBoxAnchors.Add(anchor);
        }
        else if (kind == "shape")
        {
            part.ShapeAnchors.Add(anchor);
        }
        else if (kind == "connector")
        {
            part.ConnectorAnchors.Add(anchor);
        }
        else if (kind == "formControl")
        {
            part.FormControlAnchors.Add(anchor);
        }
        else if (kind == "diagram")
        {
            part.DiagramAnchors.Add(anchor);
        }
        else
        {
            part.OtherAnchors.Add(anchor);
        }
    }

    internal static void ReadRawAnchorMetadata(XlsxDrawingRawAnchor anchor)
    {
        StringBuilder text = new StringBuilder();
        using (StringReader stringReader = new StringReader(anchor.RawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && IsAnchorElement(reader.LocalName))
                        {
                            anchor.EditAs = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("editAs"));
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "from")
                        {
                            ReadMarker(anchor.From, reader);
                            continue;
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "to")
                        {
                            ReadMarker(anchor.To, reader);
                            continue;
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "pos")
                        {
                            anchor.PositionXEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("x"));
                            anchor.PositionYEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("y"));
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "ext")
                        {
                            anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cx"));
                            anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cy"));
                            anchor.HasExtent = true;
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "cNvPr")
                        {
                            anchor.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                            anchor.Description = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("descr"));
                            XlsxXmlValueParseResult identifier = XlsxXmlValueParser.ParseUnsignedInteger(reader.GetAttribute("id"));
                            if (identifier.IsValid)
                            {
                                anchor.ObjectId = identifier.UnsignedIntValue;
                                anchor.HasObjectId = true;
                            }
                        }
                        else if (reader.LocalName == "hlinkClick")
                        {
                            anchor.Hyperlink = ReadDrawingHyperlink(reader);
                            anchor.HasHyperlink = true;
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "control")
                        {
                            anchor.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                            anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships));
                            XlsxXmlValueParseResult shapeIdentifier = XlsxXmlValueParser.ParseUnsignedInteger(reader.GetAttribute("shapeId"));
                            if (shapeIdentifier.IsValid)
                            {
                                anchor.ObjectId = shapeIdentifier.UnsignedIntValue;
                                anchor.HasObjectId = true;
                            }
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "prstGeom")
                        {
                            anchor.ShapeType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("prst"));
                        }
                        else if (reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "t")
                        {
                            if (text.Length > 0)
                            {
                                text.Append("\n");
                            }
                            text.Append(reader.ReadElementContentAsString());
                            continue;
                        }
                    }

                    reader.Read();
                }
            }
        }
        anchor.Text = text.ToString();
    }

    internal static void ReadAnchorElement(XlsxDrawingImageAnchor anchor, XmlReader reader)
    {
        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && IsAnchorElement(reader.LocalName))
        {
            anchor.EditAs = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("editAs"));
            reader.Read();
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "from")
        {
            ReadMarker(anchor.From, reader);
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "to")
        {
            ReadMarker(anchor.To, reader);
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "ext")
        {
            anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cx"));
            anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("cy"));
            anchor.HasExtent = true;
            reader.ReadOuterXml();
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "pos")
        {
            anchor.PositionXEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("x"));
            anchor.PositionYEmu = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("y"));
            reader.ReadOuterXml();
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing && reader.LocalName == "cNvPr")
        {
            SetOptionalUInt(anchor, reader.GetAttribute("id"), 1);
            anchor.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
            anchor.Description = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("descr"));
            ReadImageHyperlink(anchor, reader.ReadOuterXml());
            return;
        }

        if (reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "blip")
        {
            anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("embed", XlsxNamespaces.OfficeDocumentRelationships));
            reader.ReadOuterXml();
            return;
        }

        reader.Read();
    }

    internal static void ReadImageHyperlink(XlsxDrawingImageAnchor anchor, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "hlinkClick")
                    {
                        anchor.Hyperlink = ReadDrawingHyperlink(reader);
                        anchor.HasHyperlink = true;
                        return;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxDrawingHyperlink ReadDrawingHyperlink(XmlReader reader)
    {
        XlsxDrawingHyperlink hyperlink = new XlsxDrawingHyperlink();
        string? relationshipId = reader.GetAttribute("id", XlsxNamespaces.OfficeDocumentRelationships);
        if (relationshipId == null)
        {
            relationshipId = reader.GetAttribute("r:id");
        }
        hyperlink.RelationshipId = XlsxCellValueConverter.NormalizeText(relationshipId);
        hyperlink.Tooltip = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("tooltip"));
        hyperlink.Action = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("action"));
        hyperlink.TargetFrame = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("tgtFrame"));
        hyperlink.InvalidUrl = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("invalidUrl"));
        SetHyperlinkBoolean(hyperlink, reader.GetAttribute("history"), 1);
        SetHyperlinkBoolean(hyperlink, reader.GetAttribute("highlightClick"), 2);
        SetHyperlinkBoolean(hyperlink, reader.GetAttribute("endSnd"), 3);
        if (reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
            {
                bool knownRelationshipId = reader.NamespaceURI == XlsxNamespaces.OfficeDocumentRelationships && reader.LocalName == "id";
                bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
                bool known = reader.NamespaceURI == "" && (reader.LocalName == "tooltip" || reader.LocalName == "action" || reader.LocalName == "tgtFrame" || reader.LocalName == "invalidUrl" || reader.LocalName == "history" || reader.LocalName == "highlightClick" || reader.LocalName == "endSnd");
                if (!knownRelationshipId && !namespaceDeclaration && !known)
                {
                    hyperlink.UnknownAttributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
                }
            }
            reader.MoveToElement();
        }
        return hyperlink;
    }

    internal static void SetHyperlinkBoolean(XlsxDrawingHyperlink hyperlink, string? text, int kind)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text);
        if (!parsed.IsValid)
        {
            return;
        }
        if (kind == 1)
        {
            hyperlink.History = parsed.BooleanValue;
            hyperlink.HasHistory = true;
        }
        else if (kind == 2)
        {
            hyperlink.HighlightClick = parsed.BooleanValue;
            hyperlink.HasHighlightClick = true;
        }
        else
        {
            hyperlink.EndSound = parsed.BooleanValue;
            hyperlink.HasEndSound = true;
        }
    }

    internal static void ReadMarker(XlsxDrawingMarker marker, XmlReader reader)
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

            if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.SpreadsheetDrawing)
            {
                string localName = reader.LocalName;
                string text = reader.ReadElementContentAsString();
                if (localName == "col")
                {
                    marker.Column = ParseUnsignedText(text);
                }
                else if (localName == "colOff")
                {
                    marker.ColumnOffsetEmu = XlsxCellValueConverter.NormalizeText(text);
                }
                else if (localName == "row")
                {
                    marker.Row = ParseUnsignedText(text);
                }
                else if (localName == "rowOff")
                {
                    marker.RowOffsetEmu = XlsxCellValueConverter.NormalizeText(text);
                }

                continue;
            }

            reader.Read();
        }
    }

    internal static uint ParseUnsignedText(string text)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            return 0;
        }

        return result.UnsignedIntValue;
    }

    internal static void SetOptionalUInt(XlsxDrawingImageAnchor anchor, string? text, int target)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            return;
        }

        anchor.PictureId = result.UnsignedIntValue;
        anchor.HasPictureId = true;
    }

    internal static bool IsAnchorElement(string localName)
    {
        return localName == "twoCellAnchor" || localName == "oneCellAnchor" || localName == "absoluteAnchor";
    }

    internal static string GetAnchorType(string elementName)
    {
        if (elementName == "oneCellAnchor")
        {
            return "oneCell";
        }

        if (elementName == "absoluteAnchor")
        {
            return "absolute";
        }

        return "twoCell";
    }

    internal static XlsxDrawingAnchorKind GetAnchorKind(string anchorType)
    {
        if (anchorType == "oneCell")
        {
            return XlsxDrawingAnchorKind.OneCell;
        }

        if (anchorType == "absolute")
        {
            return XlsxDrawingAnchorKind.Absolute;
        }

        return XlsxDrawingAnchorKind.TwoCell;
    }

    internal static XlsxDrawingObjectKind GetObjectKind(string kind)
    {
        if (kind == "textBox") { return XlsxDrawingObjectKind.TextBox; }
        if (kind == "shape") { return XlsxDrawingObjectKind.Shape; }
        if (kind == "connector") { return XlsxDrawingObjectKind.Connector; }
        if (kind == "formControl") { return XlsxDrawingObjectKind.FormControl; }
        if (kind == "diagram") { return XlsxDrawingObjectKind.Diagram; }
        return XlsxDrawingObjectKind.Unknown;
    }
}
