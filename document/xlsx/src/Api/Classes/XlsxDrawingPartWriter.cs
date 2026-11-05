using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes drawing part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxDrawingPartWriter : IXlsxDrawingPartWriter
{
    /// <summary>
    /// Initializes a new drawing part writer with its default state.
    /// </summary>
    public XlsxDrawingPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDrawingPartWriteResult WriteString(XlsxDrawingPart part)
    {
        XlsxDrawingPartWriteResult result = WriteBytes(part);
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
    public XlsxDrawingPartWriteResult WriteBytes(XlsxDrawingPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxDrawingPartWriteResult result = WriteStream(stream, part);
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
    public XlsxDrawingPartWriteResult WriteStream(Stream stream, XlsxDrawingPart part)
    {
        XlsxDrawingPartWriteResult result = ValidateDrawingPart(part);
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
            writer.WriteStartElement("xdr", "wsDr", XlsxNamespaces.SpreadsheetDrawing);
            writer.WriteAttributeString("xmlns", "a", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.Drawing);
            writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeDocumentRelationships);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxDrawingImageAnchor anchor in part.ImageAnchors)
            {
                WriteImageAnchor(writer, anchor);
            }

            if (part.RawAnchors != null && part.RawAnchors.Count > 0)
            {
                WriteRawAnchors(writer, part.RawAnchors);
            }
            else
            {
                WriteChartAnchors(writer, part.ChartAnchors);
                WriteRawAnchors(writer, part.TextBoxAnchors);
                WriteRawAnchors(writer, part.ShapeAnchors);
                WriteRawAnchors(writer, part.ConnectorAnchors);
                WriteRawAnchors(writer, part.FormControlAnchors);
                WriteRawAnchors(writer, part.DiagramAnchors);
                WriteRawAnchors(writer, part.OtherAnchors);
            }

            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxDrawingPartWriteResult ValidateDrawingPart(XlsxDrawingPart part)
    {
        XlsxDrawingPartWriteResult result = new XlsxDrawingPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.partNull", "Drawing part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.ImageAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.imageAnchorsNull", "Drawing image anchor list must not be null."));
        }
        else
        {
            foreach (XlsxDrawingImageAnchor anchor in part.ImageAnchors)
            {
                ValidateImageAnchor(result, anchor);
            }
        }

        ValidateRawAnchorLists(result, part);
        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.unknownRootAttributesNull", "Drawing unknown root attribute list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.unknownXmlNull", "Drawing unknown XML list must not be null."));
        }

        return result;
    }

    internal static void ValidateRawAnchorLists(XlsxDrawingPartWriteResult result, XlsxDrawingPart part)
    {
        if (part.RawAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.rawAnchorsNull", "Drawing raw anchor list must not be null."));
        }

        if (part.ChartAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.chartAnchorsNull", "Drawing chart anchor list must not be null."));
        }

        if (part.TextBoxAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.textBoxAnchorsNull", "Drawing text box anchor list must not be null."));
        }

        if (part.ShapeAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.shapeAnchorsNull", "Drawing shape anchor list must not be null."));
        }

        if (part.ConnectorAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.connectorAnchorsNull", "Drawing connector anchor list must not be null."));
        }

        if (part.FormControlAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.formControlAnchorsNull", "Drawing form control anchor list must not be null."));
        }

        if (part.DiagramAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.diagramAnchorsNull", "Drawing diagram anchor list must not be null."));
        }

        if (part.OtherAnchors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.otherAnchorsNull", "Drawing other anchor list must not be null."));
        }

        if (part.RawAnchors != null && part.RawAnchors.Count > 0)
        {
            ValidateRawAnchorListEntries(result, part.RawAnchors);
            return;
        }

        if (part.ChartAnchors != null)
        {
            ValidateChartAnchorListEntries(result, part.ChartAnchors);
        }

        if (part.TextBoxAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.TextBoxAnchors);
        }

        if (part.ShapeAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.ShapeAnchors);
        }

        if (part.ConnectorAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.ConnectorAnchors);
        }

        if (part.FormControlAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.FormControlAnchors);
        }

        if (part.DiagramAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.DiagramAnchors);
        }

        if (part.OtherAnchors != null)
        {
            ValidateRawAnchorListEntries(result, part.OtherAnchors);
        }
    }

    internal static void ValidateChartAnchorListEntries(XlsxDrawingPartWriteResult result, List<XlsxDrawingChartAnchor> anchors)
    {
        foreach (XlsxDrawingChartAnchor anchor in anchors)
        {
            if (anchor == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.chartAnchorNull", "Drawing chart anchor list must not contain null entries."));
            }
            else if (anchor.RawXml == null || anchor.RawXml.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.chartAnchorXmlMissing", "Drawing chart anchor must contain preserved XML."));
            }
        }
    }

    internal static void ValidateRawAnchorListEntries(XlsxDrawingPartWriteResult result, List<XlsxDrawingRawAnchor> anchors)
    {
        foreach (XlsxDrawingRawAnchor anchor in anchors)
        {
            if (anchor == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.rawAnchorNull", "Drawing raw anchor list must not contain null entries."));
            }
            else if ((anchor.RawXml == null || anchor.RawXml.Length == 0) && anchor.ObjectKind == XlsxDrawingObjectKind.Unknown)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.rawAnchorXmlMissing", "Drawing raw anchor must contain preserved XML."));
            }
            else if ((anchor.RawXml == null || anchor.RawXml.Length == 0) && anchor.ObjectKind == XlsxDrawingObjectKind.Diagram)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.diagramXmlMissing", "A diagram anchor requires preserved XML."));
            }
            else if ((anchor.RawXml == null || anchor.RawXml.Length == 0) && anchor.AnchorKind != XlsxDrawingAnchorKind.TwoCell && (!anchor.HasExtent || anchor.ExtentCxEmu.Length == 0 || anchor.ExtentCyEmu.Length == 0))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.objectExtentMissing", "One-cell and absolute drawing objects require an extent."));
            }
        }
    }

    internal static void ValidateImageAnchor(XlsxDrawingPartWriteResult result, XlsxDrawingImageAnchor anchor)
    {
        if (anchor == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.imageAnchorNull", "Drawing image anchor list must not contain null entries."));
            return;
        }

        if (anchor.RawXml != null && anchor.RawXml.Length > 0)
        {
            return;
        }

        if (anchor.RelationshipId == null || anchor.RelationshipId.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.imageRelationshipIdMissing", "Drawing image anchor must have a relationship id."));
        }
        else
        {
            XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(anchor.RelationshipId);
            foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
            {
                diagnostic.SetLocation("", "pic", "r:embed");
                result.AddDiagnostic(diagnostic);
            }
        }

        if (anchor.From == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.fromMarkerNull", "Drawing image anchor from marker must not be null."));
        }

        XlsxDrawingAnchorKind anchorKind = GetImageAnchorKind(anchor);
        if (anchorKind == XlsxDrawingAnchorKind.TwoCell && anchor.To == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.toMarkerNull", "Two-cell drawing image anchor to marker must not be null."));
        }

        if (anchorKind != XlsxDrawingAnchorKind.TwoCell && (!anchor.HasExtent || anchor.ExtentCxEmu == null || anchor.ExtentCxEmu.Length == 0 || anchor.ExtentCyEmu == null || anchor.ExtentCyEmu.Length == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.extentMissing", "One-cell drawing image anchor must have an extent."));
        }

        if (anchor.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "drawingWriter.anchorUnknownXmlNull", "Drawing image anchor unknown XML list must not be null."));
        }
    }

    internal static void WriteImageAnchor(XmlWriter writer, XlsxDrawingImageAnchor anchor)
    {
        if (anchor.RawXml != null && anchor.RawXml.Length > 0)
        {
            writer.WriteRaw(anchor.RawXml);
            return;
        }

        XlsxDrawingAnchorKind anchorKind = GetImageAnchorKind(anchor);
        string elementName = "twoCellAnchor";
        if (anchorKind == XlsxDrawingAnchorKind.OneCell)
        {
            elementName = "oneCellAnchor";
        }
        else if (anchorKind == XlsxDrawingAnchorKind.Absolute)
        {
            elementName = "absoluteAnchor";
        }

        writer.WriteStartElement("xdr", elementName, XlsxNamespaces.SpreadsheetDrawing);
        if (anchor.EditAs != null && anchor.EditAs.Length > 0)
        {
            writer.WriteAttributeString("editAs", anchor.EditAs);
        }

        if (anchorKind == XlsxDrawingAnchorKind.Absolute)
        {
            WritePosition(writer, anchor.PositionXEmu, anchor.PositionYEmu);
            WriteExtent(writer, anchor.ExtentCxEmu, anchor.ExtentCyEmu);
        }
        else
        {
            WriteMarker(writer, "from", anchor.From);
            if (anchorKind == XlsxDrawingAnchorKind.OneCell)
            {
                WriteExtent(writer, anchor.ExtentCxEmu, anchor.ExtentCyEmu);
            }
            else
            {
                WriteMarker(writer, "to", anchor.To);
            }
        }

        WritePicture(writer, anchor);
        WriteUnknownXml(writer, anchor.UnknownXml);
        writer.WriteStartElement("xdr", "clientData", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteMarker(XmlWriter writer, string elementName, XlsxDrawingMarker marker)
    {
        writer.WriteStartElement("xdr", elementName, XlsxNamespaces.SpreadsheetDrawing);
        WriteTextElement(writer, "col", marker.Column.ToString(CultureInfo.InvariantCulture));
        WriteTextElement(writer, "colOff", NormalizeEmuText(marker.ColumnOffsetEmu));
        WriteTextElement(writer, "row", marker.Row.ToString(CultureInfo.InvariantCulture));
        WriteTextElement(writer, "rowOff", NormalizeEmuText(marker.RowOffsetEmu));
        writer.WriteEndElement();
    }

    internal static XlsxDrawingAnchorKind GetImageAnchorKind(XlsxDrawingImageAnchor anchor)
    {
        if (anchor.AnchorType == "oneCell") { return XlsxDrawingAnchorKind.OneCell; }
        if (anchor.AnchorType == "absolute") { return XlsxDrawingAnchorKind.Absolute; }
        if (anchor.AnchorKind == XlsxDrawingAnchorKind.OneCell || anchor.AnchorKind == XlsxDrawingAnchorKind.Absolute) { return anchor.AnchorKind; }
        return XlsxDrawingAnchorKind.TwoCell;
    }

    internal static void WritePosition(XmlWriter writer, string x, string y)
    {
        writer.WriteStartElement("xdr", "pos", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteAttributeString("x", NormalizeEmuText(x));
        writer.WriteAttributeString("y", NormalizeEmuText(y));
        writer.WriteEndElement();
    }

    internal static void WriteExtent(XmlWriter writer, string cx, string cy)
    {
        writer.WriteStartElement("xdr", "ext", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteAttributeString("cx", NormalizeEmuText(cx));
        writer.WriteAttributeString("cy", NormalizeEmuText(cy));
        writer.WriteEndElement();
    }

    internal static void WritePicture(XmlWriter writer, XlsxDrawingImageAnchor anchor)
    {
        uint pictureId = anchor.PictureId;
        if (!anchor.HasPictureId)
        {
            pictureId = 1;
        }

        string name = anchor.Name;
        if (name == null || name.Length == 0)
        {
            name = "Picture " + pictureId.ToString(CultureInfo.InvariantCulture);
        }

        writer.WriteStartElement("xdr", "pic", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("xdr", "nvPicPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("xdr", "cNvPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(pictureId));
        writer.WriteAttributeString("name", name);
        if (anchor.Description != null && anchor.Description.Length > 0)
        {
            writer.WriteAttributeString("descr", anchor.Description);
        }

        WriteDrawingHyperlink(writer, anchor);
        writer.WriteEndElement();
        writer.WriteStartElement("xdr", "cNvPicPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("xdr", "blipFill", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("a", "blip", XlsxNamespaces.Drawing);
        writer.WriteAttributeString("r", "embed", XlsxNamespaces.OfficeDocumentRelationships, anchor.RelationshipId);
        writer.WriteEndElement();
        writer.WriteStartElement("a", "stretch", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "fillRect", XlsxNamespaces.Drawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("xdr", "spPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("a", "prstGeom", XlsxNamespaces.Drawing);
        writer.WriteAttributeString("prst", "rect");
        writer.WriteStartElement("a", "avLst", XlsxNamespaces.Drawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteDrawingHyperlink(XmlWriter writer, XlsxDrawingImageAnchor anchor)
    {
        if (!anchor.HasHyperlink)
        {
            return;
        }
        XlsxDrawingHyperlink hyperlink = anchor.Hyperlink;
        writer.WriteStartElement("a", "hlinkClick", XlsxNamespaces.Drawing);
        if (hyperlink.RelationshipId.Length > 0) { writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, hyperlink.RelationshipId); }
        if (hyperlink.InvalidUrl.Length > 0) { writer.WriteAttributeString("invalidUrl", hyperlink.InvalidUrl); }
        if (hyperlink.Action.Length > 0) { writer.WriteAttributeString("action", hyperlink.Action); }
        if (hyperlink.TargetFrame.Length > 0) { writer.WriteAttributeString("tgtFrame", hyperlink.TargetFrame); }
        if (hyperlink.Tooltip.Length > 0) { writer.WriteAttributeString("tooltip", hyperlink.Tooltip); }
        if (hyperlink.HasHistory) { writer.WriteAttributeString("history", XlsxXmlValueWriter.WriteBoolean(hyperlink.History)); }
        if (hyperlink.HasHighlightClick) { writer.WriteAttributeString("highlightClick", XlsxXmlValueWriter.WriteBoolean(hyperlink.HighlightClick)); }
        if (hyperlink.HasEndSound) { writer.WriteAttributeString("endSnd", XlsxXmlValueWriter.WriteBoolean(hyperlink.EndSound)); }
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, hyperlink.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteTextElement(XmlWriter writer, string elementName, string value)
    {
        writer.WriteStartElement("xdr", elementName, XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    internal static string NormalizeEmuText(string value)
    {
        if (value == null || value.Length == 0)
        {
            return "0";
        }

        return value;
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

    internal static void WriteRawAnchors(XmlWriter writer, List<XlsxDrawingRawAnchor> anchors)
    {
        foreach (XlsxDrawingRawAnchor anchor in anchors)
        {
            if (anchor.RawXml != null && anchor.RawXml.Length > 0)
            {
                writer.WriteRaw(anchor.RawXml);
            }
            else
            {
                WriteTypedDrawingObject(writer, anchor);
            }
        }
    }

    internal static void WriteTypedDrawingObject(XmlWriter writer, XlsxDrawingRawAnchor anchor)
    {
        string elementName = "twoCellAnchor";
        if (anchor.AnchorKind == XlsxDrawingAnchorKind.OneCell) { elementName = "oneCellAnchor"; }
        else if (anchor.AnchorKind == XlsxDrawingAnchorKind.Absolute) { elementName = "absoluteAnchor"; }
        writer.WriteStartElement("xdr", elementName, XlsxNamespaces.SpreadsheetDrawing);
        if (anchor.EditAs.Length > 0) { writer.WriteAttributeString("editAs", anchor.EditAs); }
        if (anchor.AnchorKind == XlsxDrawingAnchorKind.Absolute)
        {
            WritePosition(writer, anchor.PositionXEmu, anchor.PositionYEmu);
            WriteExtent(writer, anchor.ExtentCxEmu, anchor.ExtentCyEmu);
        }
        else
        {
            WriteMarker(writer, "from", anchor.From);
            if (anchor.AnchorKind == XlsxDrawingAnchorKind.OneCell) { WriteExtent(writer, anchor.ExtentCxEmu, anchor.ExtentCyEmu); }
            else { WriteMarker(writer, "to", anchor.To); }
        }
        if (anchor.ObjectKind == XlsxDrawingObjectKind.FormControl) { WriteControl(writer, anchor); }
        else if (anchor.ObjectKind == XlsxDrawingObjectKind.Connector) { WriteConnector(writer, anchor); }
        else { WriteShape(writer, anchor); }
        writer.WriteStartElement("xdr", "clientData", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteShape(XmlWriter writer, XlsxDrawingRawAnchor anchor)
    {
        writer.WriteStartElement("xdr", "sp", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("xdr", "nvSpPr", XlsxNamespaces.SpreadsheetDrawing);
        WriteObjectNonVisualProperties(writer, anchor);
        writer.WriteStartElement("xdr", "cNvSpPr", XlsxNamespaces.SpreadsheetDrawing);
        if (anchor.ObjectKind == XlsxDrawingObjectKind.TextBox) { writer.WriteAttributeString("txBox", "1"); }
        writer.WriteEndElement();
        writer.WriteEndElement();
        WriteShapeProperties(writer, anchor.ShapeType);
        if (anchor.ObjectKind == XlsxDrawingObjectKind.TextBox || anchor.Text.Length > 0) { WriteShapeText(writer, anchor.Text); }
        writer.WriteEndElement();
    }

    internal static void WriteConnector(XmlWriter writer, XlsxDrawingRawAnchor anchor)
    {
        writer.WriteStartElement("xdr", "cxnSp", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("xdr", "nvCxnSpPr", XlsxNamespaces.SpreadsheetDrawing);
        WriteObjectNonVisualProperties(writer, anchor);
        writer.WriteStartElement("xdr", "cNvCxnSpPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
        WriteShapeProperties(writer, anchor.ShapeType);
        writer.WriteEndElement();
    }

    internal static void WriteControl(XmlWriter writer, XlsxDrawingRawAnchor anchor)
    {
        writer.WriteStartElement("xdr", "control", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, anchor.RelationshipId);
        writer.WriteAttributeString("name", anchor.Name);
        writer.WriteAttributeString("shapeId", XlsxXmlValueWriter.WriteUnsignedInteger(anchor.ObjectId));
        writer.WriteEndElement();
    }

    internal static void WriteObjectNonVisualProperties(XmlWriter writer, XlsxDrawingRawAnchor anchor)
    {
        writer.WriteStartElement("xdr", "cNvPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(anchor.ObjectId));
        writer.WriteAttributeString("name", anchor.Name);
        if (anchor.Description.Length > 0) { writer.WriteAttributeString("descr", anchor.Description); }
        if (anchor.HasHyperlink) { WriteObjectHyperlink(writer, anchor.Hyperlink); }
        writer.WriteEndElement();
    }

    internal static void WriteObjectHyperlink(XmlWriter writer, XlsxDrawingHyperlink hyperlink)
    {
        writer.WriteStartElement("a", "hlinkClick", XlsxNamespaces.Drawing);
        if (hyperlink.RelationshipId.Length > 0) { writer.WriteAttributeString("r", "id", XlsxNamespaces.OfficeDocumentRelationships, hyperlink.RelationshipId); }
        if (hyperlink.Tooltip.Length > 0) { writer.WriteAttributeString("tooltip", hyperlink.Tooltip); }
        writer.WriteEndElement();
    }

    internal static void WriteShapeProperties(XmlWriter writer, string shapeType)
    {
        writer.WriteStartElement("xdr", "spPr", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("a", "prstGeom", XlsxNamespaces.Drawing);
        writer.WriteAttributeString("prst", shapeType.Length == 0 ? "rect" : shapeType);
        writer.WriteStartElement("a", "avLst", XlsxNamespaces.Drawing);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteShapeText(XmlWriter writer, string text)
    {
        writer.WriteStartElement("xdr", "txBody", XlsxNamespaces.SpreadsheetDrawing);
        writer.WriteStartElement("a", "bodyPr", XlsxNamespaces.Drawing); writer.WriteEndElement();
        writer.WriteStartElement("a", "lstStyle", XlsxNamespaces.Drawing); writer.WriteEndElement();
        writer.WriteStartElement("a", "p", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "r", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "t", XlsxNamespaces.Drawing); writer.WriteString(text); writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteChartAnchors(XmlWriter writer, List<XlsxDrawingChartAnchor> anchors)
    {
        foreach (XlsxDrawingChartAnchor anchor in anchors)
        {
            writer.WriteRaw(anchor.RawXml);
        }
    }
}
