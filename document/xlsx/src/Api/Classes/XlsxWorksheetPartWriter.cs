using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes worksheet part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxWorksheetPartWriter : IXlsxWorksheetPartWriter
{
    /// <summary>
    /// Initializes a new worksheet part writer with its default state.
    /// </summary>
    public XlsxWorksheetPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorksheetPartWriteResult WriteString(XlsxWorksheetPart part)
    {
        XlsxWorksheetPartWriteResult result = WriteBytes(part);
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
    public XlsxWorksheetPartWriteResult WriteBytes(XlsxWorksheetPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxWorksheetPartWriteResult result = WriteStream(stream, part);
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
    public XlsxWorksheetPartWriteResult WriteStream(Stream stream, XlsxWorksheetPart part)
    {
        XlsxWorksheetPartWriteResult result = ValidateBasicWorksheetPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "worksheet", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(part.NamespaceUri));
            writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", GetOutputRelationshipNamespace(part.NamespaceUri));
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteSheetProperties(writer, part);
            WriteDimension(writer, part);
            WriteSheetViews(writer, part);
            WriteFormatProperties(writer, part);
            WriteColumns(writer, part);
            WriteSheetData(writer, part);
            WriteMergeCells(writer, part);
            WriteHyperlinks(writer, part);
            WritePrintOptions(writer, part);
            WritePageMargins(writer, part);
            WritePageSetup(writer, part);
            WriteHeaderFooter(writer, part);
            WritePageBreaks(writer, "rowBreaks", part.RowBreaks);
            WritePageBreaks(writer, "colBreaks", part.ColumnBreaks);
            WriteSheetProtection(writer, part);
            WriteScenarios(writer, part);
            WriteCustomSheetViews(writer, part);
            WriteAutoFilter(writer, part);
            WriteConditionalFormattings(writer, part);
            WriteDataValidations(writer, part);
            WriteIgnoredErrors(writer, part);
            WriteSmartTags(writer, part);
            WriteWebPublishItems(writer, part);
            WriteTableReferences(writer, part);
            WritePivotTableReferences(writer, part);
            WriteDrawing(writer, part);
            WriteEmbeddedObjectReferences(writer, part.EmbeddedObjectReferences);
            WriteExtensionList(writer, part);
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static string GetOutputSpreadsheetNamespace(XmlWriter writer)
    {
        string? strictPrefix = writer.LookupPrefix(XlsxNamespaces.StrictSpreadsheet);
        if (strictPrefix != null) { return XlsxNamespaces.StrictSpreadsheet; }
        return XlsxNamespaces.Spreadsheet;
    }

    internal static string GetOutputRelationshipNamespace(string? spreadsheetNamespace)
    {
        if (spreadsheetNamespace == XlsxNamespaces.StrictSpreadsheet) { return XlsxNamespaces.StrictOfficeDocumentRelationships; }
        return XlsxNamespaces.OfficeDocumentRelationships;
    }

    internal static void WriteSheetProperties(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasSheetProperties)
        {
            return;
        }

        XlsxWorksheetProperties properties = part.SheetProperties;
        if (properties.RawXml != null && properties.RawXml.Length > 0)
        {
            writer.WriteRaw(properties.RawXml);
            return;
        }

        writer.WriteStartElement("", "sheetPr", GetOutputSpreadsheetNamespace(writer));
        if (properties.CodeName != null && properties.CodeName.Length > 0)
        {
            writer.WriteAttributeString("codeName", properties.CodeName);
        }

        if (properties.HasEnableFormatConditionsCalculation)
        {
            writer.WriteAttributeString("enableFormatConditionsCalculation", XlsxXmlValueWriter.WriteBoolean(properties.EnableFormatConditionsCalculation));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, properties.UnknownAttributes);
        WriteTabColor(writer, properties);
        WriteOutlineProperties(writer, properties);
        WritePageSetupProperties(writer, properties);
        WriteUnknownXml(writer, properties.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteTabColor(XmlWriter writer, XlsxWorksheetProperties properties)
    {
        if (!properties.HasTabColor)
        {
            return;
        }
        XlsxSpreadsheetColor color = properties.TabColor;
        writer.WriteStartElement("", "tabColor", GetOutputSpreadsheetNamespace(writer));
        if (color.HasAuto) { writer.WriteAttributeString("auto", XlsxXmlValueWriter.WriteBoolean(color.IsAuto)); }
        if (color.HasIndexed) { writer.WriteAttributeString("indexed", XlsxXmlValueWriter.WriteUnsignedInteger(color.Indexed)); }
        if (color.HasRgb) { writer.WriteAttributeString("rgb", color.Rgb); }
        if (color.HasTheme) { writer.WriteAttributeString("theme", XlsxXmlValueWriter.WriteUnsignedInteger(color.Theme)); }
        if (color.HasTint) { writer.WriteAttributeString("tint", XlsxXmlValueWriter.WriteDouble(color.Tint)); }
        writer.WriteEndElement();
    }

    internal static void WriteOutlineProperties(XmlWriter writer, XlsxWorksheetProperties properties)
    {
        if (!properties.HasOutlineProperties)
        {
            return;
        }
        XlsxWorksheetOutlineProperties outline = properties.OutlineProperties;
        writer.WriteStartElement("", "outlinePr", GetOutputSpreadsheetNamespace(writer));
        if (outline.HasApplyStyles) { writer.WriteAttributeString("applyStyles", XlsxXmlValueWriter.WriteBoolean(outline.ApplyStyles)); }
        if (outline.HasSummaryBelow) { writer.WriteAttributeString("summaryBelow", XlsxXmlValueWriter.WriteBoolean(outline.SummaryBelow)); }
        if (outline.HasSummaryRight) { writer.WriteAttributeString("summaryRight", XlsxXmlValueWriter.WriteBoolean(outline.SummaryRight)); }
        if (outline.HasShowOutlineSymbols) { writer.WriteAttributeString("showOutlineSymbols", XlsxXmlValueWriter.WriteBoolean(outline.ShowOutlineSymbols)); }
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, outline.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WritePageSetupProperties(XmlWriter writer, XlsxWorksheetProperties properties)
    {
        if (!properties.HasPageSetupProperties)
        {
            return;
        }
        XlsxWorksheetPageSetupProperties pageSetup = properties.PageSetupProperties;
        writer.WriteStartElement("", "pageSetUpPr", GetOutputSpreadsheetNamespace(writer));
        if (pageSetup.HasAutoPageBreaks) { writer.WriteAttributeString("autoPageBreaks", XlsxXmlValueWriter.WriteBoolean(pageSetup.AutoPageBreaks)); }
        if (pageSetup.HasFitToPage) { writer.WriteAttributeString("fitToPage", XlsxXmlValueWriter.WriteBoolean(pageSetup.FitToPage)); }
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, pageSetup.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteAutoFilter(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasAutoFilter)
        {
            return;
        }

        part.AutoFilter.NamespaceUri = GetOutputSpreadsheetNamespace(writer);
        XlsxAutoFilterWriter.WriteAutoFilterElement(writer, part.AutoFilter);
    }

    internal static void WriteDataValidations(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasDataValidations)
        {
            return;
        }

        part.DataValidations.NamespaceUri = GetOutputSpreadsheetNamespace(writer);
        XlsxDataValidationWriter.WriteDataValidationsElement(writer, part.DataValidations);
    }

    internal static void WriteConditionalFormattings(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.ConditionalFormattings == null)
        {
            return;
        }

        foreach (XlsxConditionalFormatting formatting in part.ConditionalFormattings)
        {
            formatting.NamespaceUri = GetOutputSpreadsheetNamespace(writer);
            XlsxConditionalFormattingWriter.WriteConditionalFormattingElement(writer, formatting);
        }
    }

    internal static void WriteDrawing(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasDrawing)
        {
            return;
        }

        WriteDrawingReferenceElement(writer, part.NamespaceUri, part.Drawing);
    }

    internal static void WriteDrawingReferenceElement(XmlWriter writer, string namespaceUri, XlsxSheetDrawingReference drawing)
    {
        if (drawing.RawXml != null && drawing.RawXml.Length > 0)
        {
            writer.WriteRaw(drawing.RawXml);
            return;
        }

        writer.WriteStartElement("", "drawing", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(namespaceUri));
        writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), drawing.RelationshipId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, drawing.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteEmbeddedObjectReferences(XmlWriter writer, List<XlsxWorksheetEmbeddedObjectReference> references)
    {
        if (references.Count == 0) { return; }
        writer.WriteStartElement("", "oleObjects", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetEmbeddedObjectReference reference in references)
        {
            if (reference.RawXml != null && reference.RawXml.Length > 0)
            {
                writer.WriteRaw(reference.RawXml);
                continue;
            }
            writer.WriteStartElement("", "oleObject", GetOutputSpreadsheetNamespace(writer));
            writer.WriteAttributeString("progId", reference.ProgramId);
            if (reference.HasShapeId) { writer.WriteAttributeString("shapeId", XlsxXmlValueWriter.WriteUnsignedInteger(reference.ShapeId)); }
            writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), reference.RelationshipId);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteDimension(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasDimension)
        {
            return;
        }

        writer.WriteStartElement("", "dimension", GetOutputSpreadsheetNamespace(writer));
        if (part.Dimension.Reference != null && part.Dimension.Reference.Length > 0)
        {
            writer.WriteAttributeString("ref", part.Dimension.Reference);
        }

        writer.WriteEndElement();
    }

    internal static void WriteSheetViews(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.SheetViews.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "sheetViews", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetView view in part.SheetViews)
        {
            WriteSheetView(writer, view);
        }

        writer.WriteEndElement();
    }

    internal static void WriteSheetView(XmlWriter writer, XlsxWorksheetView view)
    {
        writer.WriteStartElement("", "sheetView", GetOutputSpreadsheetNamespace(writer));
        if (view.HasWorkbookViewId)
        {
            writer.WriteAttributeString("workbookViewId", XlsxXmlValueWriter.WriteUnsignedInteger(view.WorkbookViewId));
        }

        if (view.View != null && view.View.Length > 0)
        {
            writer.WriteAttributeString("view", view.View);
        }

        if (view.HasShowGridLines)
        {
            writer.WriteAttributeString("showGridLines", XlsxXmlValueWriter.WriteBoolean(view.ShowGridLines));
        }

        if (view.HasShowRowColHeaders)
        {
            writer.WriteAttributeString("showRowColHeaders", XlsxXmlValueWriter.WriteBoolean(view.ShowRowColHeaders));
        }

        if (view.HasRightToLeft)
        {
            writer.WriteAttributeString("rightToLeft", XlsxXmlValueWriter.WriteBoolean(view.RightToLeft));
        }

        if (view.HasTabSelected)
        {
            writer.WriteAttributeString("tabSelected", XlsxXmlValueWriter.WriteBoolean(view.TabSelected));
        }

        if (view.HasZoomScale)
        {
            writer.WriteAttributeString("zoomScale", XlsxXmlValueWriter.WriteUnsignedInteger(view.ZoomScale));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, view.UnknownAttributes);
        WritePane(writer, view);
        WriteSelections(writer, view);
        WriteUnknownXml(writer, view.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WritePane(XmlWriter writer, XlsxWorksheetView view)
    {
        if (!view.HasPane || view.Pane == null)
        {
            return;
        }

        XlsxWorksheetPane pane = view.Pane;
        writer.WriteStartElement("", "pane", GetOutputSpreadsheetNamespace(writer));
        if (pane.HasXSplit)
        {
            writer.WriteAttributeString("xSplit", XlsxXmlValueWriter.WriteDouble(pane.XSplit));
        }

        if (pane.HasYSplit)
        {
            writer.WriteAttributeString("ySplit", XlsxXmlValueWriter.WriteDouble(pane.YSplit));
        }

        if (pane.TopLeftCell != null && pane.TopLeftCell.Length > 0)
        {
            writer.WriteAttributeString("topLeftCell", pane.TopLeftCell);
        }

        if (pane.ActivePane != null && pane.ActivePane.Length > 0)
        {
            writer.WriteAttributeString("activePane", pane.ActivePane);
        }

        if (pane.State != null && pane.State.Length > 0)
        {
            writer.WriteAttributeString("state", pane.State);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, pane.UnknownAttributes);
        WriteUnknownXml(writer, pane.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteSelections(XmlWriter writer, XlsxWorksheetView view)
    {
        if (view.Selections == null)
        {
            return;
        }

        foreach (XlsxWorksheetSelection selection in view.Selections)
        {
            WriteSelection(writer, selection);
        }
    }

    internal static void WriteSelection(XmlWriter writer, XlsxWorksheetSelection selection)
    {
        writer.WriteStartElement("", "selection", GetOutputSpreadsheetNamespace(writer));
        if (selection.Pane != null && selection.Pane.Length > 0)
        {
            writer.WriteAttributeString("pane", selection.Pane);
        }

        if (selection.ActiveCell != null && selection.ActiveCell.Length > 0)
        {
            writer.WriteAttributeString("activeCell", selection.ActiveCell);
        }

        if (selection.HasActiveCellId)
        {
            writer.WriteAttributeString("activeCellId", XlsxXmlValueWriter.WriteUnsignedInteger(selection.ActiveCellId));
        }

        if (selection.Sqref != null && selection.Sqref.Length > 0)
        {
            writer.WriteAttributeString("sqref", selection.Sqref);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, selection.UnknownAttributes);
        WriteUnknownXml(writer, selection.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteFormatProperties(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasFormatProperties)
        {
            return;
        }

        XlsxWorksheetFormatProperties format = part.FormatProperties;
        writer.WriteStartElement("", "sheetFormatPr", GetOutputSpreadsheetNamespace(writer));
        if (format.HasBaseColumnWidth)
        {
            writer.WriteAttributeString("baseColWidth", XlsxXmlValueWriter.WriteUnsignedInteger(format.BaseColumnWidth));
        }

        if (format.HasDefaultColumnWidth)
        {
            writer.WriteAttributeString("defaultColWidth", XlsxXmlValueWriter.WriteDouble(format.DefaultColumnWidth));
        }

        if (format.HasDefaultRowHeight)
        {
            writer.WriteAttributeString("defaultRowHeight", XlsxXmlValueWriter.WriteDouble(format.DefaultRowHeight));
        }

        if (format.HasCustomHeight)
        {
            writer.WriteAttributeString("customHeight", XlsxXmlValueWriter.WriteBoolean(format.CustomHeight));
        }

        if (format.HasZeroHeight)
        {
            writer.WriteAttributeString("zeroHeight", XlsxXmlValueWriter.WriteBoolean(format.ZeroHeight));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, format.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteColumns(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.Columns.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "cols", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetColumn column in part.Columns)
        {
            WriteColumn(writer, column);
        }

        writer.WriteEndElement();
    }

    internal static void WriteColumn(XmlWriter writer, XlsxWorksheetColumn column)
    {
        writer.WriteStartElement("", "col", GetOutputSpreadsheetNamespace(writer));
        if (column.Min > 0)
        {
            writer.WriteAttributeString("min", XlsxXmlValueWriter.WriteUnsignedInteger(column.Min));
        }

        if (column.Max > 0)
        {
            writer.WriteAttributeString("max", XlsxXmlValueWriter.WriteUnsignedInteger(column.Max));
        }

        if (column.HasWidth)
        {
            writer.WriteAttributeString("width", XlsxXmlValueWriter.WriteDouble(column.Width));
        }

        if (column.HasStyle)
        {
            writer.WriteAttributeString("style", XlsxXmlValueWriter.WriteUnsignedInteger(column.Style));
        }

        if (column.HasHidden)
        {
            writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(column.Hidden));
        }

        if (column.HasBestFit)
        {
            writer.WriteAttributeString("bestFit", XlsxXmlValueWriter.WriteBoolean(column.BestFit));
        }

        if (column.HasCustomWidth)
        {
            writer.WriteAttributeString("customWidth", XlsxXmlValueWriter.WriteBoolean(column.CustomWidth));
        }

        if (column.HasOutlineLevel)
        {
            writer.WriteAttributeString("outlineLevel", XlsxXmlValueWriter.WriteUnsignedInteger(column.OutlineLevel));
        }

        if (column.HasCollapsed)
        {
            writer.WriteAttributeString("collapsed", XlsxXmlValueWriter.WriteBoolean(column.Collapsed));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, column.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteSheetData(XmlWriter writer, XlsxWorksheetPart part)
    {
        writer.WriteStartElement("", "sheetData", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetRow row in part.Rows)
        {
            WriteRow(writer, row);
        }

        writer.WriteEndElement();
    }

    internal static void WriteMergeCells(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.MergedCells.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "mergeCells", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("count", part.MergedCells.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxWorksheetMergeCell mergeCell in part.MergedCells)
        {
            writer.WriteStartElement("", "mergeCell", GetOutputSpreadsheetNamespace(writer));
            if (mergeCell.Reference != null && mergeCell.Reference.Length > 0)
            {
                writer.WriteAttributeString("ref", mergeCell.Reference);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WriteHyperlinks(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.Hyperlinks.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "hyperlinks", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetHyperlink hyperlink in part.Hyperlinks)
        {
            WriteHyperlink(writer, hyperlink);
        }

        writer.WriteEndElement();
    }

    internal static void WriteHyperlink(XmlWriter writer, XlsxWorksheetHyperlink hyperlink)
    {
        writer.WriteStartElement("", "hyperlink", GetOutputSpreadsheetNamespace(writer));
        if (hyperlink.Reference != null && hyperlink.Reference.Length > 0)
        {
            writer.WriteAttributeString("ref", hyperlink.Reference);
        }

        if (hyperlink.RelationshipId != null && hyperlink.RelationshipId.Length > 0)
        {
            writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), hyperlink.RelationshipId);
        }

        if (hyperlink.Location != null && hyperlink.Location.Length > 0)
        {
            writer.WriteAttributeString("location", hyperlink.Location);
        }

        if (hyperlink.Display != null && hyperlink.Display.Length > 0)
        {
            writer.WriteAttributeString("display", hyperlink.Display);
        }

        if (hyperlink.Tooltip != null && hyperlink.Tooltip.Length > 0)
        {
            writer.WriteAttributeString("tooltip", hyperlink.Tooltip);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, hyperlink.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WritePrintOptions(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasPrintOptions)
        {
            return;
        }

        XlsxWorksheetPrintOptions? options = part.PrintOptions;
        if (options == null)
        {
            return;
        }

        writer.WriteStartElement("", "printOptions", GetOutputSpreadsheetNamespace(writer));
        if (options.HasHorizontalCentered)
        {
            writer.WriteAttributeString("horizontalCentered", XlsxXmlValueWriter.WriteBoolean(options.HorizontalCentered));
        }

        if (options.HasVerticalCentered)
        {
            writer.WriteAttributeString("verticalCentered", XlsxXmlValueWriter.WriteBoolean(options.VerticalCentered));
        }

        if (options.HasHeadings)
        {
            writer.WriteAttributeString("headings", XlsxXmlValueWriter.WriteBoolean(options.Headings));
        }

        if (options.HasGridLines)
        {
            writer.WriteAttributeString("gridLines", XlsxXmlValueWriter.WriteBoolean(options.GridLines));
        }

        writer.WriteEndElement();
    }

    internal static void WritePageMargins(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasPageMargins)
        {
            return;
        }

        XlsxWorksheetPageMargins? margins = part.PageMargins;
        if (margins == null)
        {
            return;
        }

        writer.WriteStartElement("", "pageMargins", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("left", XlsxXmlValueWriter.WriteDouble(margins.Left));
        writer.WriteAttributeString("right", XlsxXmlValueWriter.WriteDouble(margins.Right));
        writer.WriteAttributeString("top", XlsxXmlValueWriter.WriteDouble(margins.Top));
        writer.WriteAttributeString("bottom", XlsxXmlValueWriter.WriteDouble(margins.Bottom));
        writer.WriteAttributeString("header", XlsxXmlValueWriter.WriteDouble(margins.Header));
        writer.WriteAttributeString("footer", XlsxXmlValueWriter.WriteDouble(margins.Footer));
        writer.WriteEndElement();
    }

    internal static void WritePageSetup(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasPageSetup)
        {
            return;
        }

        XlsxWorksheetPageSetup? setup = part.PageSetup;
        if (setup == null)
        {
            return;
        }

        writer.WriteStartElement("", "pageSetup", GetOutputSpreadsheetNamespace(writer));
        if (setup.HasPaperSize)
        {
            writer.WriteAttributeString("paperSize", XlsxXmlValueWriter.WriteUnsignedInteger(setup.PaperSize));
        }

        if (setup.Orientation != null && setup.Orientation.Length > 0)
        {
            writer.WriteAttributeString("orientation", setup.Orientation);
        }

        if (setup.HasScale)
        {
            writer.WriteAttributeString("scale", XlsxXmlValueWriter.WriteUnsignedInteger(setup.Scale));
        }

        if (setup.HasFitToWidth)
        {
            writer.WriteAttributeString("fitToWidth", XlsxXmlValueWriter.WriteUnsignedInteger(setup.FitToWidth));
        }

        if (setup.HasFitToHeight)
        {
            writer.WriteAttributeString("fitToHeight", XlsxXmlValueWriter.WriteUnsignedInteger(setup.FitToHeight));
        }

        if (setup.HasFirstPageNumber)
        {
            writer.WriteAttributeString("firstPageNumber", XlsxXmlValueWriter.WriteUnsignedInteger(setup.FirstPageNumber));
        }

        if (setup.HasUseFirstPageNumber)
        {
            writer.WriteAttributeString("useFirstPageNumber", XlsxXmlValueWriter.WriteBoolean(setup.UseFirstPageNumber));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, setup.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteHeaderFooter(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasHeaderFooter)
        {
            return;
        }

        XlsxWorksheetHeaderFooter? headerFooter = part.HeaderFooter;
        if (headerFooter == null)
        {
            return;
        }

        writer.WriteStartElement("", "headerFooter", GetOutputSpreadsheetNamespace(writer));
        if (headerFooter.HasDifferentOddEven)
        {
            writer.WriteAttributeString("differentOddEven", XlsxXmlValueWriter.WriteBoolean(headerFooter.DifferentOddEven));
        }

        if (headerFooter.HasDifferentFirst)
        {
            writer.WriteAttributeString("differentFirst", XlsxXmlValueWriter.WriteBoolean(headerFooter.DifferentFirst));
        }

        if (headerFooter.HasScaleWithDoc)
        {
            writer.WriteAttributeString("scaleWithDoc", XlsxXmlValueWriter.WriteBoolean(headerFooter.ScaleWithDoc));
        }

        if (headerFooter.HasAlignWithMargins)
        {
            writer.WriteAttributeString("alignWithMargins", XlsxXmlValueWriter.WriteBoolean(headerFooter.AlignWithMargins));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, headerFooter.UnknownAttributes);
        WriteHeaderFooterText(writer, "oddHeader", headerFooter.OddHeader);
        WriteHeaderFooterText(writer, "oddFooter", headerFooter.OddFooter);
        WriteHeaderFooterText(writer, "evenHeader", headerFooter.EvenHeader);
        WriteHeaderFooterText(writer, "evenFooter", headerFooter.EvenFooter);
        WriteHeaderFooterText(writer, "firstHeader", headerFooter.FirstHeader);
        WriteHeaderFooterText(writer, "firstFooter", headerFooter.FirstFooter);
        WriteUnknownXml(writer, headerFooter.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteHeaderFooterText(XmlWriter writer, string elementName, string text)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        writer.WriteStartElement("", elementName, GetOutputSpreadsheetNamespace(writer));
        writer.WriteString(text);
        writer.WriteEndElement();
    }

    internal static void WritePageBreaks(XmlWriter writer, string elementName, List<XlsxWorksheetPageBreak> breaks)
    {
        if (breaks == null || breaks.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", elementName, GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("count", breaks.Count.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("manualBreakCount", CountManualPageBreaks(breaks).ToString(CultureInfo.InvariantCulture));
        foreach (XlsxWorksheetPageBreak pageBreak in breaks)
        {
            WritePageBreak(writer, pageBreak);
        }

        writer.WriteEndElement();
    }

    internal static void WritePageBreak(XmlWriter writer, XlsxWorksheetPageBreak pageBreak)
    {
        writer.WriteStartElement("", "brk", GetOutputSpreadsheetNamespace(writer));
        if (pageBreak.HasId)
        {
            writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(pageBreak.Id));
        }

        if (pageBreak.HasMin)
        {
            writer.WriteAttributeString("min", XlsxXmlValueWriter.WriteUnsignedInteger(pageBreak.Min));
        }

        if (pageBreak.HasMax)
        {
            writer.WriteAttributeString("max", XlsxXmlValueWriter.WriteUnsignedInteger(pageBreak.Max));
        }

        if (pageBreak.HasManual)
        {
            writer.WriteAttributeString("man", XlsxXmlValueWriter.WriteBoolean(pageBreak.Manual));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, pageBreak.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static int CountManualPageBreaks(List<XlsxWorksheetPageBreak> breaks)
    {
        int count = 0;
        foreach (XlsxWorksheetPageBreak pageBreak in breaks)
        {
            if (pageBreak != null && pageBreak.HasManual && pageBreak.Manual)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static void WriteTableReferences(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.TableReferences == null || part.TableReferences.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "tableParts", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("count", part.TableReferences.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxWorksheetTableReference tableReference in part.TableReferences)
        {
            writer.WriteStartElement("", "tablePart", GetOutputSpreadsheetNamespace(writer));
            if (tableReference.RelationshipId != null && tableReference.RelationshipId.Length > 0)
            {
                writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), tableReference.RelationshipId);
            }

            XlsxContentTypesWriter.WriteUnknownAttributes(writer, tableReference.UnknownAttributes);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotTableReferences(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.PivotTableReferences == null || part.PivotTableReferences.Count == 0)
        {
            return;
        }

        foreach (XlsxWorksheetPivotTableReference pivotTableReference in part.PivotTableReferences)
        {
            writer.WriteStartElement("", "pivotTableDefinition", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(part.NamespaceUri));
            if (pivotTableReference.RelationshipId != null && pivotTableReference.RelationshipId.Length > 0)
            {
                writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), pivotTableReference.RelationshipId);
            }

            XlsxContentTypesWriter.WriteUnknownAttributes(writer, pivotTableReference.UnknownAttributes);
            writer.WriteEndElement();
        }
    }

    internal static void WriteSheetProtection(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasSheetProtection)
        {
            return;
        }

        XlsxWorksheetProtection? protection = part.SheetProtection;
        if (protection == null)
        {
            return;
        }

        writer.WriteStartElement("", "sheetProtection", GetOutputSpreadsheetNamespace(writer));
        if (protection.HasSheet)
        {
            writer.WriteAttributeString("sheet", XlsxXmlValueWriter.WriteBoolean(protection.Sheet));
        }

        if (protection.HasObjects)
        {
            writer.WriteAttributeString("objects", XlsxXmlValueWriter.WriteBoolean(protection.Objects));
        }

        if (protection.HasScenarios)
        {
            writer.WriteAttributeString("scenarios", XlsxXmlValueWriter.WriteBoolean(protection.Scenarios));
        }

        if (protection.HasFormatCells)
        {
            writer.WriteAttributeString("formatCells", XlsxXmlValueWriter.WriteBoolean(protection.FormatCells));
        }

        if (protection.HasFormatColumns)
        {
            writer.WriteAttributeString("formatColumns", XlsxXmlValueWriter.WriteBoolean(protection.FormatColumns));
        }

        if (protection.HasFormatRows)
        {
            writer.WriteAttributeString("formatRows", XlsxXmlValueWriter.WriteBoolean(protection.FormatRows));
        }

        if (protection.HasInsertColumns)
        {
            writer.WriteAttributeString("insertColumns", XlsxXmlValueWriter.WriteBoolean(protection.InsertColumns));
        }

        if (protection.HasInsertRows)
        {
            writer.WriteAttributeString("insertRows", XlsxXmlValueWriter.WriteBoolean(protection.InsertRows));
        }

        if (protection.HasInsertHyperlinks)
        {
            writer.WriteAttributeString("insertHyperlinks", XlsxXmlValueWriter.WriteBoolean(protection.InsertHyperlinks));
        }

        if (protection.HasDeleteColumns)
        {
            writer.WriteAttributeString("deleteColumns", XlsxXmlValueWriter.WriteBoolean(protection.DeleteColumns));
        }

        if (protection.HasDeleteRows)
        {
            writer.WriteAttributeString("deleteRows", XlsxXmlValueWriter.WriteBoolean(protection.DeleteRows));
        }

        if (protection.HasSelectLockedCells)
        {
            writer.WriteAttributeString("selectLockedCells", XlsxXmlValueWriter.WriteBoolean(protection.SelectLockedCells));
        }

        if (protection.HasSort)
        {
            writer.WriteAttributeString("sort", XlsxXmlValueWriter.WriteBoolean(protection.Sort));
        }

        if (protection.HasAutoFilter)
        {
            writer.WriteAttributeString("autoFilter", XlsxXmlValueWriter.WriteBoolean(protection.AutoFilter));
        }

        if (protection.HasPivotTables)
        {
            writer.WriteAttributeString("pivotTables", XlsxXmlValueWriter.WriteBoolean(protection.PivotTables));
        }

        if (protection.HasSelectUnlockedCells)
        {
            writer.WriteAttributeString("selectUnlockedCells", XlsxXmlValueWriter.WriteBoolean(protection.SelectUnlockedCells));
        }

        if (protection.Password != null && protection.Password.Length > 0)
        {
            writer.WriteAttributeString("password", protection.Password);
        }

        if (protection.AlgorithmName != null && protection.AlgorithmName.Length > 0)
        {
            writer.WriteAttributeString("algorithmName", protection.AlgorithmName);
        }

        if (protection.HashValue != null && protection.HashValue.Length > 0)
        {
            writer.WriteAttributeString("hashValue", protection.HashValue);
        }

        if (protection.SaltValue != null && protection.SaltValue.Length > 0)
        {
            writer.WriteAttributeString("saltValue", protection.SaltValue);
        }

        if (protection.HasSpinCount)
        {
            writer.WriteAttributeString("spinCount", XlsxXmlValueWriter.WriteUnsignedInteger(protection.SpinCount));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, protection.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteExtensionList(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasExtensionList && !part.HasSparklineGroups)
        {
            return;
        }

        if (part.HasExtensionList && part.ExtensionList.RawXml != null && part.ExtensionList.RawXml.Length > 0 && !part.HasSparklineGroups)
        {
            writer.WriteRaw(part.ExtensionList.RawXml);
            return;
        }

        writer.WriteStartElement("", "extLst", GetOutputSpreadsheetNamespace(writer));
        if (part.HasExtensionList && part.ExtensionList != null)
        {
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.ExtensionList.UnknownRootAttributes);
            foreach (XlsxExtension extension in part.ExtensionList.Extensions)
            {
                if (!part.HasSparklineGroups || !XlsxSparklineGroupsWriter.IsSparklineExtension(extension))
                {
                    XlsxExtensionListWriter.WriteExtension(writer, extension, GetOutputSpreadsheetNamespace(writer), "");
                }
            }

            WriteUnknownXml(writer, part.ExtensionList.UnknownXml);
        }

        if (part.HasSparklineGroups)
        {
            XlsxSparklineGroupsWriter.WriteSparklineExtension(writer, part.SparklineGroups);
        }

        writer.WriteEndElement();
    }

    internal static void WriteScenarios(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (!part.HasScenarios)
        {
            return;
        }
        XlsxWorksheetScenarios scenarios = part.Scenarios;
        writer.WriteStartElement("", "scenarios", GetOutputSpreadsheetNamespace(writer));
        if (scenarios.HasCurrent) { writer.WriteAttributeString("current", XlsxXmlValueWriter.WriteUnsignedInteger(scenarios.Current)); }
        if (scenarios.HasShow) { writer.WriteAttributeString("show", XlsxXmlValueWriter.WriteUnsignedInteger(scenarios.Show)); }
        if (scenarios.SquareReference.Length > 0) { writer.WriteAttributeString("sqref", scenarios.SquareReference); }
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, scenarios.UnknownAttributes);
        foreach (XlsxWorksheetScenario scenario in scenarios.Items)
        {
            writer.WriteStartElement("", "scenario", GetOutputSpreadsheetNamespace(writer));
            writer.WriteAttributeString("name", scenario.Name);
            if (scenario.Comment.Length > 0) { writer.WriteAttributeString("comment", scenario.Comment); }
            if (scenario.User.Length > 0) { writer.WriteAttributeString("user", scenario.User); }
            if (scenario.HasLocked) { writer.WriteAttributeString("locked", XlsxXmlValueWriter.WriteBoolean(scenario.Locked)); }
            if (scenario.HasHidden) { writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(scenario.Hidden)); }
            writer.WriteAttributeString("count", scenario.InputCells.Count.ToString(CultureInfo.InvariantCulture));
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, scenario.UnknownAttributes);
            foreach (XlsxWorksheetScenarioInputCell input in scenario.InputCells)
            {
                writer.WriteStartElement("", "inputCells", GetOutputSpreadsheetNamespace(writer));
                writer.WriteAttributeString("r", input.Reference);
                writer.WriteAttributeString("val", input.Value);
                if (input.HasNumberFormatId) { writer.WriteAttributeString("numFmtId", XlsxXmlValueWriter.WriteUnsignedInteger(input.NumberFormatId)); }
                if (input.HasDeleted) { writer.WriteAttributeString("deleted", XlsxXmlValueWriter.WriteBoolean(input.Deleted)); }
                if (input.HasUndone) { writer.WriteAttributeString("undone", XlsxXmlValueWriter.WriteBoolean(input.Undone)); }
                XlsxContentTypesWriter.WriteUnknownAttributes(writer, input.UnknownAttributes);
                writer.WriteEndElement();
            }
            WriteUnknownXml(writer, scenario.UnknownXml);
            writer.WriteEndElement();
        }
        WriteUnknownXml(writer, scenarios.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteCustomSheetViews(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.CustomSheetViews.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "customSheetViews", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetCustomView view in part.CustomSheetViews)
        {
            if (view.RawXml.Length > 0)
            {
                writer.WriteRaw(view.RawXml);
                continue;
            }
            writer.WriteStartElement("", "customSheetView", GetOutputSpreadsheetNamespace(writer));
            writer.WriteAttributeString("guid", view.Guid);
            if (view.HasScale) { writer.WriteAttributeString("scale", XlsxXmlValueWriter.WriteUnsignedInteger(view.Scale)); }
            if (view.HasColorId) { writer.WriteAttributeString("colorId", XlsxXmlValueWriter.WriteUnsignedInteger(view.ColorId)); }
            if (view.HasShowFormulas) { writer.WriteAttributeString("showFormulas", XlsxXmlValueWriter.WriteBoolean(view.ShowFormulas)); }
            if (view.HasShowGridLines) { writer.WriteAttributeString("showGridLines", XlsxXmlValueWriter.WriteBoolean(view.ShowGridLines)); }
            if (view.HasShowRowColumnHeaders) { writer.WriteAttributeString("showRowCol", XlsxXmlValueWriter.WriteBoolean(view.ShowRowColumnHeaders)); }
            if (view.HasShowPageBreaks) { writer.WriteAttributeString("showPageBreaks", XlsxXmlValueWriter.WriteBoolean(view.ShowPageBreaks)); }
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, view.UnknownAttributes);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteIgnoredErrors(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.IgnoredErrors.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "ignoredErrors", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetIgnoredError ignored in part.IgnoredErrors)
        {
            writer.WriteStartElement("", "ignoredError", GetOutputSpreadsheetNamespace(writer));
            writer.WriteAttributeString("sqref", ignored.SquareReference);
            if (ignored.HasEvaluationError) { writer.WriteAttributeString("evalError", XlsxXmlValueWriter.WriteBoolean(ignored.EvaluationError)); }
            if (ignored.HasTwoDigitTextYear) { writer.WriteAttributeString("twoDigitTextYear", XlsxXmlValueWriter.WriteBoolean(ignored.TwoDigitTextYear)); }
            if (ignored.HasNumberStoredAsText) { writer.WriteAttributeString("numberStoredAsText", XlsxXmlValueWriter.WriteBoolean(ignored.NumberStoredAsText)); }
            if (ignored.HasFormula) { writer.WriteAttributeString("formula", XlsxXmlValueWriter.WriteBoolean(ignored.Formula)); }
            if (ignored.HasFormulaRange) { writer.WriteAttributeString("formulaRange", XlsxXmlValueWriter.WriteBoolean(ignored.FormulaRange)); }
            if (ignored.HasUnlockedFormula) { writer.WriteAttributeString("unlockedFormula", XlsxXmlValueWriter.WriteBoolean(ignored.UnlockedFormula)); }
            if (ignored.HasEmptyCellReference) { writer.WriteAttributeString("emptyCellReference", XlsxXmlValueWriter.WriteBoolean(ignored.EmptyCellReference)); }
            if (ignored.HasListDataValidation) { writer.WriteAttributeString("listDataValidation", XlsxXmlValueWriter.WriteBoolean(ignored.ListDataValidation)); }
            if (ignored.HasCalculatedColumn) { writer.WriteAttributeString("calculatedColumn", XlsxXmlValueWriter.WriteBoolean(ignored.CalculatedColumn)); }
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, ignored.UnknownAttributes);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteSmartTags(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.SmartTags.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "cellSmartTags", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorksheetSmartTag smartTag in part.SmartTags)
        {
            writer.WriteStartElement("", "cellSmartTag", GetOutputSpreadsheetNamespace(writer));
            writer.WriteAttributeString("r", smartTag.Reference);
            if (smartTag.HasTypeId) { writer.WriteAttributeString("type", XlsxXmlValueWriter.WriteUnsignedInteger(smartTag.TypeId)); }
            if (smartTag.HasDeleted) { writer.WriteAttributeString("deleted", XlsxXmlValueWriter.WriteBoolean(smartTag.Deleted)); }
            if (smartTag.HasXmlBased) { writer.WriteAttributeString("xmlBased", XlsxXmlValueWriter.WriteBoolean(smartTag.XmlBased)); }
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, smartTag.UnknownAttributes);
            foreach (XlsxWorksheetSmartTagProperty property in smartTag.Properties)
            {
                writer.WriteStartElement("", "cellSmartTagPr", GetOutputSpreadsheetNamespace(writer));
                writer.WriteAttributeString("key", property.Key);
                writer.WriteAttributeString("val", property.Value);
                XlsxContentTypesWriter.WriteUnknownAttributes(writer, property.UnknownAttributes);
                writer.WriteEndElement();
            }
            WriteUnknownXml(writer, smartTag.UnknownXml);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteWebPublishItems(XmlWriter writer, XlsxWorksheetPart part)
    {
        if (part.WebPublishItems.Count == 0)
        {
            return;
        }
        writer.WriteStartElement("", "webPublishItems", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("count", part.WebPublishItems.Count.ToString(CultureInfo.InvariantCulture));
        foreach (XlsxWorksheetWebPublishItem item in part.WebPublishItems)
        {
            writer.WriteStartElement("", "webPublishItem", GetOutputSpreadsheetNamespace(writer));
            if (item.HasId) { writer.WriteAttributeString("id", XlsxXmlValueWriter.WriteUnsignedInteger(item.Id)); }
            if (item.DivisionId.Length > 0) { writer.WriteAttributeString("divId", item.DivisionId); }
            if (item.SourceType.Length > 0) { writer.WriteAttributeString("sourceType", item.SourceType); }
            if (item.SourceReference.Length > 0) { writer.WriteAttributeString("sourceRef", item.SourceReference); }
            if (item.SourceObject.Length > 0) { writer.WriteAttributeString("sourceObject", item.SourceObject); }
            if (item.DestinationFile.Length > 0) { writer.WriteAttributeString("destinationFile", item.DestinationFile); }
            if (item.Title.Length > 0) { writer.WriteAttributeString("title", item.Title); }
            if (item.HasAutoRepublish) { writer.WriteAttributeString("autoRepublish", XlsxXmlValueWriter.WriteBoolean(item.AutoRepublish)); }
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, item.UnknownAttributes);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXmlItems)
    {
        if (unknownXmlItems == null)
        {
            return;
        }

        foreach (string unknownXml in unknownXmlItems)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }
    }

    internal static void WriteRow(XmlWriter writer, XlsxWorksheetRow row)
    {
        writer.WriteStartElement("", "row", GetOutputSpreadsheetNamespace(writer));
        if (row.RowIndex > 0)
        {
            writer.WriteAttributeString("r", XlsxXmlValueWriter.WriteUnsignedInteger(row.RowIndex));
        }

        if (row.Spans != null && row.Spans.Length > 0)
        {
            writer.WriteAttributeString("spans", row.Spans);
        }

        if (row.HasStyle)
        {
            writer.WriteAttributeString("s", XlsxXmlValueWriter.WriteUnsignedInteger(row.Style));
        }

        if (row.HasHeight)
        {
            writer.WriteAttributeString("ht", XlsxXmlValueWriter.WriteDouble(row.Height));
        }

        if (row.HasHidden)
        {
            writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(row.Hidden));
        }

        if (row.HasCustomHeight)
        {
            writer.WriteAttributeString("customHeight", XlsxXmlValueWriter.WriteBoolean(row.CustomHeight));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, row.UnknownAttributes);
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            WriteCell(writer, cell);
        }

        writer.WriteEndElement();
    }

    internal static void WriteCell(XmlWriter writer, XlsxWorksheetCell cell)
    {
        writer.WriteStartElement("", "c", GetOutputSpreadsheetNamespace(writer));
        if (cell.Reference != null && cell.Reference.Length > 0)
        {
            writer.WriteAttributeString("r", cell.Reference);
        }

        if (cell.HasStyle)
        {
            writer.WriteAttributeString("s", XlsxXmlValueWriter.WriteUnsignedInteger(cell.Style));
        }

        WriteCellTypeAttribute(writer, cell);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, cell.UnknownAttributes);
        if (cell.HasFormula && cell.FormulaInfo != null)
        {
            WriteCellFormula(writer, cell.FormulaInfo, cell.Formula);
        }
        else if (cell.Formula != null && cell.Formula.Length > 0)
        {
            WriteCellFormulaText(writer, cell.Formula);
        }

        string cachedValue = GetCellCachedValueForWrite(cell);
        if (cachedValue != null && cachedValue.Length > 0)
        {
            writer.WriteStartElement("", "v", GetOutputSpreadsheetNamespace(writer));
            writer.WriteString(cachedValue);
            writer.WriteEndElement();
        }

        if (cell.HasInlineString && cell.InlineString != null)
        {
            XlsxInlineStringWriter.WriteInlineStringElement(writer, cell.InlineString);
        }
        else if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
        {
            WriteInlineString(writer, cell.InlineStringText);
        }

        foreach (string unknownXml in cell.UnknownXml)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }

        writer.WriteEndElement();
    }

    internal static string GetCellCachedValueForWrite(XlsxWorksheetCell cell)
    {
        if (cell.Value != null && cell.Value.Length > 0)
        {
            return cell.Value;
        }

        if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.CachedValue != null && cell.FormulaInfo.CachedValue.Length > 0)
        {
            return cell.FormulaInfo.CachedValue;
        }

        return "";
    }

    internal static void WriteCellFormulaText(XmlWriter writer, string formulaText)
    {
        writer.WriteStartElement("", "f", GetOutputSpreadsheetNamespace(writer));
        writer.WriteString(formulaText);
        writer.WriteEndElement();
    }

    internal static void WriteCellFormula(XmlWriter writer, XlsxCellFormula formula, string fallbackText)
    {
        writer.WriteStartElement("", "f", GetOutputSpreadsheetNamespace(writer));
        if (formula.Type != null && formula.Type.Length > 0 && formula.Type != "normal")
        {
            writer.WriteAttributeString("t", formula.Type);
        }

        if (formula.Reference != null && formula.Reference.Length > 0)
        {
            writer.WriteAttributeString("ref", formula.Reference);
        }

        if (formula.HasSharedIndex)
        {
            writer.WriteAttributeString("si", XlsxXmlValueWriter.WriteUnsignedInteger(formula.SharedIndex));
        }

        if (formula.HasAlwaysCalculateArray)
        {
            writer.WriteAttributeString("aca", XlsxXmlValueWriter.WriteBoolean(formula.AlwaysCalculateArray));
        }

        if (formula.HasCalculateCell)
        {
            writer.WriteAttributeString("ca", XlsxXmlValueWriter.WriteBoolean(formula.CalculateCell));
        }

        if (formula.HasDataTable2D)
        {
            writer.WriteAttributeString("dt2D", XlsxXmlValueWriter.WriteBoolean(formula.DataTable2D));
        }

        if (formula.HasDataTableRow)
        {
            writer.WriteAttributeString("dtr", XlsxXmlValueWriter.WriteBoolean(formula.DataTableRow));
        }

        if (formula.HasInput1Deleted)
        {
            writer.WriteAttributeString("del1", XlsxXmlValueWriter.WriteBoolean(formula.Input1Deleted));
        }

        if (formula.HasInput2Deleted)
        {
            writer.WriteAttributeString("del2", XlsxXmlValueWriter.WriteBoolean(formula.Input2Deleted));
        }

        if (formula.Input1Reference != null && formula.Input1Reference.Length > 0)
        {
            writer.WriteAttributeString("r1", formula.Input1Reference);
        }

        if (formula.Input2Reference != null && formula.Input2Reference.Length > 0)
        {
            writer.WriteAttributeString("r2", formula.Input2Reference);
        }

        if (formula.HasAssignsToName)
        {
            writer.WriteAttributeString("bx", XlsxXmlValueWriter.WriteBoolean(formula.AssignsToName));
        }

        if (formula.HasDynamicArray)
        {
            writer.WriteAttributeString("dynamicArray", XlsxXmlValueWriter.WriteBoolean(formula.DynamicArray));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, formula.UnknownAttributes);
        string text = formula.Text;
        if ((text == null || text.Length == 0) && fallbackText != null && fallbackText.Length > 0)
        {
            text = fallbackText;
        }

        if (text != null && text.Length > 0)
        {
            writer.WriteString(text);
        }

        writer.WriteEndElement();
    }

    internal static void WriteCellTypeAttribute(XmlWriter writer, XlsxWorksheetCell cell)
    {
        if (cell.Type != null && cell.Type.Length > 0)
        {
            writer.WriteAttributeString("t", cell.Type);
            return;
        }

        if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.CachedType != null && cell.FormulaInfo.CachedType.Length > 0)
        {
            writer.WriteAttributeString("t", cell.FormulaInfo.CachedType);
            return;
        }

        if (cell.HasInlineString)
        {
            writer.WriteAttributeString("t", "inlineStr");
            return;
        }

        if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
        {
            writer.WriteAttributeString("t", "inlineStr");
        }
    }

    internal static void WriteInlineString(XmlWriter writer, string text)
    {
        XlsxInlineStringWriter.WriteInlineStringElement(writer, XlsxInlineString.CreateText(text));
    }

    internal static XlsxWorksheetPartWriteResult ValidateBasicWorksheetPart(XlsxWorksheetPart part)
    {
        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.partNull", "Worksheet part must not be null."));
            return result;
        }

        ValidateRows(result, part);
        ValidateSheetViews(result, part);
        ValidateMergedCells(result, part);
        ValidateHyperlinks(result, part);
        ValidatePagePrintSettings(result, part);
        ValidateSheetProtection(result, part);
        ValidateAutoFilter(result, part);
        ValidateConditionalFormattings(result, part);
        ValidateDataValidations(result, part);
        ValidateTableReferences(result, part);
        ValidatePivotTableReferences(result, part);
        ValidateEmbeddedObjectReferences(result, part);
        ValidateDrawing(result, part);
        ValidateSparklineGroups(result, part);
        return result;
    }

    internal static void ValidateEmbeddedObjectReferences(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.EmbeddedObjectReferences == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.embeddedObjectReferencesNull", "Worksheet embedded object reference list must not be null."));
            return;
        }
        foreach (XlsxWorksheetEmbeddedObjectReference reference in part.EmbeddedObjectReferences)
        {
            if (reference == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.embeddedObjectReferenceNull", "Worksheet embedded object reference list must not contain null entries."));
            }
            else if (reference.RelationshipId == null || reference.RelationshipId.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.embeddedObjectRelationshipMissing", "Worksheet embedded object references require a relationship identifier."));
            }
        }
    }

    internal static void ValidateSparklineGroups(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasSparklineGroups)
        {
            return;
        }

        if (part.SparklineGroups == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineGroupsNull", "Worksheet sparkline group list must not be null when HasSparklineGroups is true."));
            return;
        }

        foreach (XlsxSparklineGroup group in part.SparklineGroups)
        {
            ValidateSparklineGroup(result, group);
        }
    }

    internal static void ValidateSparklineGroup(XlsxWorksheetPartWriteResult result, XlsxSparklineGroup group)
    {
        if (group == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineGroupNull", "Worksheet sparkline groups must not contain null entries."));
            return;
        }

        if (!XlsxSparklineGroupsWriter.IsSupportedSparklineType(group.Type))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineTypeUnsupported", "Sparkline type must be line, column, or stacked."));
        }

        if (group.Sparklines == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklinesNull", "Sparkline group sparkline list must not be null."));
            return;
        }

        foreach (XlsxSparkline sparkline in group.Sparklines)
        {
            ValidateSparkline(result, sparkline);
        }
    }

    internal static void ValidateSparkline(XlsxWorksheetPartWriteResult result, XlsxSparkline sparkline)
    {
        if (sparkline == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineNull", "Sparkline list must not contain null entries."));
            return;
        }

        if (sparkline.Formula == null || sparkline.Formula.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineFormulaMissing", "Sparkline formula must not be empty."));
        }

        if (sparkline.CellReference == null || sparkline.CellReference.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sparklineCellMissing", "Sparkline target cell reference must not be empty."));
        }
        else
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(sparkline.CellReference);
            foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void ValidateAutoFilter(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasAutoFilter)
        {
            return;
        }

        if (part.AutoFilter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.autoFilterNull", "Worksheet auto filter must not be null when HasAutoFilter is true."));
            return;
        }

        XlsxAutoFilterWriteResult filterResult = XlsxAutoFilterWriter.ValidateAutoFilter(part.AutoFilter);
        foreach (XlsxDiagnostic diagnostic in filterResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateDataValidations(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasDataValidations)
        {
            return;
        }

        if (part.DataValidations == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.dataValidationsNull", "Worksheet data validations must not be null when HasDataValidations is true."));
            return;
        }

        XlsxDataValidationWriteResult validationResult = XlsxDataValidationWriter.ValidateDataValidations(part.DataValidations);
        foreach (XlsxDiagnostic diagnostic in validationResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateConditionalFormattings(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.ConditionalFormattings == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.conditionalFormattingsNull", "Worksheet conditional formatting list must not be null."));
            return;
        }

        foreach (XlsxConditionalFormatting formatting in part.ConditionalFormattings)
        {
            XlsxConditionalFormattingWriteResult formattingResult = XlsxConditionalFormattingWriter.ValidateConditionalFormatting(formatting);
            foreach (XlsxDiagnostic diagnostic in formattingResult.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void ValidateDrawing(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasDrawing)
        {
            return;
        }

        if (part.Drawing == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.drawingNull", "Worksheet drawing reference must not be null when HasDrawing is true."));
            return;
        }

        ValidateDrawingReference(result, part.Drawing, "worksheet");
    }

    internal static void ValidateDrawingReference(XlsxWorksheetPartWriteResult result, XlsxSheetDrawingReference drawing, string elementName)
    {
        if (drawing.RelationshipId == null || drawing.RelationshipId.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.drawingRelationshipIdMissing", "Worksheet drawing reference must have an r:id attribute."));
            return;
        }

        XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(drawing.RelationshipId);
        foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, "r:id");
            result.AddDiagnostic(diagnostic);
        }

        if (drawing.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.drawingUnknownAttributesNull", "Worksheet drawing reference unknown attribute list must not be null."));
        }
    }

    internal static void ValidateSheetProtection(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasSheetProtection)
        {
            return;
        }

        if (part.SheetProtection == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetProtectionNull", "Worksheet sheet protection must not be null when HasSheetProtection is true."));
            return;
        }

        if (part.SheetProtection.HasSpinCount && part.SheetProtection.SpinCount == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetProtectionSpinCountRange", "Worksheet sheet protection spin count must be greater than zero when present."));
        }

        ValidateSheetProtectionHashDefinition(result, part.SheetProtection);

        if (part.SheetProtection.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetProtectionUnknownAttributesNull", "Worksheet sheet protection unknown attributes list must not be null."));
        }
    }

    internal static void ValidateSheetProtectionHashDefinition(XlsxWorksheetPartWriteResult result, XlsxWorksheetProtection protection)
    {
        bool hasAlgorithm = protection.AlgorithmName != null && protection.AlgorithmName.Length > 0;
        bool hasHash = protection.HashValue != null && protection.HashValue.Length > 0;
        bool hasSalt = protection.SaltValue != null && protection.SaltValue.Length > 0;
        bool hasDefinition = hasAlgorithm || hasHash || hasSalt || protection.HasSpinCount;
        if (hasDefinition && (!hasAlgorithm || !hasHash || !hasSalt || !protection.HasSpinCount))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetProtectionHashIncomplete", "Worksheet protection modern hash settings require an algorithm, hash value, salt value, and spin count."));
        }
    }

    internal static void ValidatePagePrintSettings(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        ValidatePrintOptions(result, part);
        ValidatePageMargins(result, part);
        ValidatePageSetup(result, part);
        ValidateHeaderFooter(result, part);
        ValidatePageBreaks(result, part, part.RowBreaks, "rowBreaks");
        ValidatePageBreaks(result, part, part.ColumnBreaks, "colBreaks");
    }

    internal static void ValidatePrintOptions(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.HasPrintOptions && part.PrintOptions == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.printOptionsNull", "Worksheet print options must not be null when HasPrintOptions is true."));
        }
    }

    internal static void ValidatePageMargins(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasPageMargins)
        {
            return;
        }

        if (part.PageMargins == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageMarginsNull", "Worksheet page margins must not be null when HasPageMargins is true."));
            return;
        }

        if (part.PageMargins.Left < 0 || part.PageMargins.Right < 0 || part.PageMargins.Top < 0 || part.PageMargins.Bottom < 0 || part.PageMargins.Header < 0 || part.PageMargins.Footer < 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageMarginRange", "Worksheet page margins must not be negative."));
        }
    }

    internal static void ValidatePageSetup(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasPageSetup)
        {
            return;
        }

        if (part.PageSetup == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageSetupNull", "Worksheet page setup must not be null when HasPageSetup is true."));
            return;
        }

        if (part.PageSetup.HasScale && (part.PageSetup.Scale < 10 || part.PageSetup.Scale > 400))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageSetupScaleRange", "Worksheet page setup scale must be between 10 and 400."));
        }

        if (part.PageSetup.Orientation != null && part.PageSetup.Orientation.Length > 0 && part.PageSetup.Orientation != "portrait" && part.PageSetup.Orientation != "landscape")
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageSetupOrientationUnsupported", "Worksheet page setup orientation must be portrait or landscape."));
        }

        if (part.PageSetup.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageSetupUnknownAttributesNull", "Worksheet page setup unknown attributes list must not be null."));
        }
    }

    internal static void ValidateHeaderFooter(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (!part.HasHeaderFooter)
        {
            return;
        }

        if (part.HeaderFooter == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.headerFooterNull", "Worksheet header/footer must not be null when HasHeaderFooter is true."));
            return;
        }

        if (part.HeaderFooter.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.headerFooterUnknownAttributesNull", "Worksheet header/footer unknown attributes list must not be null."));
        }

        if (part.HeaderFooter.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.headerFooterUnknownXmlNull", "Worksheet header/footer unknown XML list must not be null."));
        }
    }

    internal static void ValidatePageBreaks(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, List<XlsxWorksheetPageBreak> breaks, string elementName)
    {
        if (breaks == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageBreaksNull", "Worksheet page break lists must not be null.");
            diagnostic.SetLocation(part.PartPath, elementName, "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int index = 0;
        foreach (XlsxWorksheetPageBreak pageBreak in breaks)
        {
            if (pageBreak == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageBreakNull", "Worksheet page break lists must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, elementName, index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidatePageBreak(result, part, pageBreak, elementName, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidatePageBreak(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetPageBreak pageBreak, string elementName, int index)
    {
        if (!pageBreak.HasId)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageBreakIdMissing", "Worksheet page break must have an id attribute.");
            diagnostic.SetLocation(part.PartPath, elementName, index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (pageBreak.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pageBreakUnknownAttributesNull", "Worksheet page break unknown attributes list must not be null."));
        }
    }

    internal static void ValidateSheetViews(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.SheetViews == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewsNull", "Worksheet sheet views list must not be null.");
            diagnostic.SetLocation(part.PartPath, "sheetViews", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int index = 0;
        foreach (XlsxWorksheetView view in part.SheetViews)
        {
            if (view == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewNull", "Worksheet sheet views must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "sheetView", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateSheetView(result, part, view, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateSheetView(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetView view, int index)
    {
        if (view.View != null && view.View.Length > 0 && !IsSupportedSheetViewMode(view.View))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewModeUnsupported", "Worksheet sheet view mode must be normal, pageBreakPreview, or pageLayout.");
            diagnostic.SetLocation(part.PartPath, "sheetView", "view");
            result.AddDiagnostic(diagnostic);
        }

        if (view.HasZoomScale && (view.ZoomScale < 10 || view.ZoomScale > 400))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewZoomRange", "Worksheet sheet view zoom scale must be between 10 and 400.");
            diagnostic.SetLocation(part.PartPath, "sheetView", "zoomScale");
            result.AddDiagnostic(diagnostic);
        }

        if (view.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewUnknownAttributesNull", "Worksheet sheet view unknown attributes list must not be null."));
        }

        if (view.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.sheetViewUnknownXmlNull", "Worksheet sheet view unknown XML list must not be null."));
        }

        if (view.HasPane)
        {
            ValidatePane(result, part, view.Pane, index);
        }

        if (view.Selections == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.selectionsNull", "Worksheet sheet view selections list must not be null."));
            return;
        }

        int selectionIndex = 0;
        foreach (XlsxWorksheetSelection selection in view.Selections)
        {
            ValidateSelection(result, part, selection, selectionIndex);
            selectionIndex = selectionIndex + 1;
        }
    }

    internal static void ValidatePane(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetPane pane, int index)
    {
        if (pane == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneNull", "Worksheet sheet view pane must not be null when HasPane is true.");
            diagnostic.SetLocation(part.PartPath, "pane", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if ((pane.HasXSplit && pane.XSplit < 0) || (pane.HasYSplit && pane.YSplit < 0))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneSplitRange", "Worksheet pane split positions must not be negative.");
            diagnostic.SetLocation(part.PartPath, "pane", "");
            result.AddDiagnostic(diagnostic);
        }

        ValidateOptionalCellReference(result, part, pane.TopLeftCell, "pane", "topLeftCell", "worksheet.paneTopLeftCellInvalid");
        if (pane.ActivePane != null && pane.ActivePane.Length > 0 && !IsSupportedPaneName(pane.ActivePane))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneActivePaneInvalid", "Worksheet pane activePane must be topLeft, topRight, bottomLeft, or bottomRight.");
            diagnostic.SetLocation(part.PartPath, "pane", "activePane");
            result.AddDiagnostic(diagnostic);
        }

        if (pane.State != null && pane.State.Length > 0 && !IsSupportedPaneState(pane.State))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneStateInvalid", "Worksheet pane state must be split, frozen, or frozenSplit.");
            diagnostic.SetLocation(part.PartPath, "pane", "state");
            result.AddDiagnostic(diagnostic);
        }

        if (pane.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneUnknownAttributesNull", "Worksheet pane unknown attributes list must not be null."));
        }

        if (pane.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.paneUnknownXmlNull", "Worksheet pane unknown XML list must not be null."));
        }
    }

    internal static void ValidateSelection(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetSelection selection, int index)
    {
        if (selection == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.selectionNull", "Worksheet selections must not contain null entries.");
            diagnostic.SetLocation(part.PartPath, "selection", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (selection.Pane != null && selection.Pane.Length > 0 && !IsSupportedPaneName(selection.Pane))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.selectionPaneInvalid", "Worksheet selection pane must be topLeft, topRight, bottomLeft, or bottomRight.");
            diagnostic.SetLocation(part.PartPath, "selection", "pane");
            result.AddDiagnostic(diagnostic);
        }

        ValidateOptionalCellReference(result, part, selection.ActiveCell, "selection", "activeCell", "worksheet.selectionActiveCellInvalid");
        ValidateOptionalSqref(result, part, selection.Sqref, "selection", "sqref", "worksheet.selectionSqrefInvalid");
        if (selection.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.selectionUnknownAttributesNull", "Worksheet selection unknown attributes list must not be null."));
        }

        if (selection.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.selectionUnknownXmlNull", "Worksheet selection unknown XML list must not be null."));
        }
    }

    internal static bool IsSupportedSheetViewMode(string value)
    {
        return value == "normal" || value == "pageBreakPreview" || value == "pageLayout";
    }

    internal static bool IsSupportedPaneName(string value)
    {
        return value == "topLeft" || value == "topRight" || value == "bottomLeft" || value == "bottomRight";
    }

    internal static bool IsSupportedPaneState(string value)
    {
        return value == "split" || value == "frozen" || value == "frozenSplit";
    }

    internal static void ValidateOptionalCellReference(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, string reference, string elementName, string attributeName, string code)
    {
        if (reference == null || reference.Length == 0)
        {
            return;
        }

        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, "Worksheet sheet view cell reference is invalid.");
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            result.AddDiagnostic(diagnostic);
            AddCellReferenceDiagnostics(result, referenceResult);
        }
    }

    internal static void ValidateOptionalSqref(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, string sqref, string elementName, string attributeName, string code)
    {
        if (sqref == null || sqref.Length == 0)
        {
            return;
        }

        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            XlsxA1RangeReferenceResult rangeResult = XlsxA1RangeReferenceConverter.Parse(trimmed);
            if (!rangeResult.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, "Worksheet sheet view selected range reference is invalid.");
                diagnostic.SetLocation(part.PartPath, elementName, attributeName);
                result.AddDiagnostic(diagnostic);
                AddRangeReferenceDiagnostics(result, rangeResult);
            }
        }
    }

    internal static void AddCellReferenceDiagnostics(XlsxWorksheetPartWriteResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddRangeReferenceDiagnostics(XlsxWorksheetPartWriteResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateRows(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.Rows == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.rowsNull", "Worksheet rows list must not be null.");
            diagnostic.SetLocation(part.PartPath, "sheetData", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int rowIndex = 0;
        foreach (XlsxWorksheetRow row in part.Rows)
        {
            if (row == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.rowNull", "Worksheet rows must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "row", rowIndex.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateCells(result, part, row, rowIndex);
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void ValidateCells(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetRow row, int rowIndex)
    {
        if (row.Cells == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellsNull", "Worksheet row cell lists must not be null.");
            diagnostic.SetLocation(part.PartPath, "row", rowIndex.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        int cellIndex = 0;
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellNull", "Worksheet cells must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "c", rowIndex.ToString(CultureInfo.InvariantCulture) + ":" + cellIndex.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateCell(result, part, cell, rowIndex, cellIndex);
            }

            cellIndex = cellIndex + 1;
        }
    }

    internal static void ValidateCell(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetCell cell, int rowIndex, int cellIndex)
    {
        if (cell.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellUnknownAttributesNull", "Worksheet cell unknown attributes list must not be null."));
        }

        if (cell.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellUnknownXmlNull", "Worksheet cell unknown XML list must not be null."));
        }

        if (cell.HasFormula)
        {
            ValidateCellFormula(result, part, cell.FormulaInfo, rowIndex, cellIndex);
        }
    }

    internal static void ValidateCellFormula(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxCellFormula formula, int rowIndex, int cellIndex)
    {
        if (formula == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaNull", "Worksheet cell formula must not be null when HasFormula is true.");
            diagnostic.SetLocation(part.PartPath, "f", rowIndex.ToString(CultureInfo.InvariantCulture) + ":" + cellIndex.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (!IsSupportedCellFormulaType(formula.Type))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaTypeUnsupported", "Worksheet cell formula type is not supported.");
            diagnostic.SetLocation(part.PartPath, "f", "t");
            result.AddDiagnostic(diagnostic);
        }

        if (formula.Type == "shared" && !formula.HasSharedIndex)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaSharedIndexMissing", "Shared worksheet formulas must have a shared index.");
            diagnostic.SetLocation(part.PartPath, "f", "si");
            result.AddDiagnostic(diagnostic);
        }

        if (formula.Type == "array" || formula.Type == "dynamicArray")
        {
            ValidateCellFormulaRange(result, part, formula);
        }

        if (formula.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaUnknownAttributesNull", "Worksheet cell formula unknown attributes list must not be null."));
        }
    }

    internal static void ValidateCellFormulaRange(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxCellFormula formula)
    {
        if (formula.Reference == null || formula.Reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaRangeMissing", "Array and dynamic array formulas must have a ref range.");
            diagnostic.SetLocation(part.PartPath, "f", "ref");
            result.AddDiagnostic(diagnostic);
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(formula.Reference);
        if (!range.IsValid)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.cellFormulaRangeInvalid", "Array and dynamic array formula ref must be a valid A1 range.");
            diagnostic.SetLocation(part.PartPath, "f", "ref");
            result.AddDiagnostic(diagnostic);
            foreach (XlsxDiagnostic rangeDiagnostic in range.Validation.Diagnostics)
            {
                result.AddDiagnostic(rangeDiagnostic);
            }
        }
    }

    internal static bool IsSupportedCellFormulaType(string type)
    {
        if (type == null || type.Length == 0 || type == "normal")
        {
            return true;
        }

        if (type == "shared" || type == "array" || type == "dynamicArray" || type == "dataTable")
        {
            return true;
        }

        return false;
    }

    internal static void ValidateMergedCells(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        XlsxWorksheetMergeCellsResult mergeResult = part.ValidateMergedCells();
        foreach (XlsxDiagnostic diagnostic in mergeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateHyperlinks(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.Hyperlinks == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            diagnostic.SetLocation(part.PartPath, "hyperlinks", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int index = 0;
        foreach (XlsxWorksheetHyperlink hyperlink in part.Hyperlinks)
        {
            if (hyperlink == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkNull", "Worksheet hyperlinks must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "hyperlink", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateHyperlink(result, part, hyperlink, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateHyperlink(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetHyperlink hyperlink, int index)
    {
        if (hyperlink.Reference == null || hyperlink.Reference.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkReferenceMissing", "Worksheet hyperlink must have a ref attribute.");
            diagnostic.SetLocation(part.PartPath, "hyperlink", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxA1RangeReferenceResult referenceResult = XlsxA1RangeReferenceConverter.Parse(hyperlink.Reference);
            if (!referenceResult.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkReferenceInvalid", "Worksheet hyperlink ref attribute must be a valid A1 cell or range reference.");
                diagnostic.SetLocation(part.PartPath, "hyperlink", "ref");
                result.AddDiagnostic(diagnostic);
                AddHyperlinkReferenceDiagnostics(result, referenceResult);
            }
        }

        bool hasRelationship = hyperlink.RelationshipId != null && hyperlink.RelationshipId.Length > 0;
        bool hasLocation = hyperlink.Location != null && hyperlink.Location.Length > 0;
        if (!hasRelationship && !hasLocation)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkTargetMissing", "Worksheet hyperlink must have an internal location or relationship id.");
            diagnostic.SetLocation(part.PartPath, "hyperlink", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (hasRelationship)
        {
            XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(hyperlink.RelationshipId);
            if (!relationshipId.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkRelationshipIdInvalid", "Worksheet hyperlink relationship id is invalid.");
                diagnostic.SetLocation(part.PartPath, "hyperlink", "r:id");
                diagnostic.SetRelationshipId(hyperlink.RelationshipId);
                result.AddDiagnostic(diagnostic);
                AddHyperlinkRelationshipIdDiagnostics(result, relationshipId);
            }
        }

        if (hyperlink.TargetMode != null && hyperlink.TargetMode.Length > 0 && hyperlink.TargetMode != "Internal" && hyperlink.TargetMode != "External")
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.hyperlinkTargetModeUnsupported", "Worksheet hyperlink target mode must be Internal or External.");
            diagnostic.SetLocation(part.PartPath, "hyperlink", "TargetMode");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateTableReferences(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.TableReferences == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tableReferencesNull", "Worksheet table reference list must not be null.");
            diagnostic.SetLocation(part.PartPath, "tableParts", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int index = 0;
        foreach (XlsxWorksheetTableReference tableReference in part.TableReferences)
        {
            if (tableReference == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tableReferenceNull", "Worksheet table reference list must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "tablePart", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateTableReference(result, part, tableReference, index);
            }

            index = index + 1;
        }

        if (part.Tables == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tablesNull", "Worksheet table list must not be null.");
            diagnostic.SetLocation(part.PartPath, "tableParts", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        foreach (XlsxTablePart table in part.Tables)
        {
            XlsxTablePartWriteResult tableResult = XlsxTablePartWriter.ValidateTablePart(table);
            foreach (XlsxDiagnostic diagnostic in tableResult.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void ValidateTableReference(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetTableReference tableReference, int index)
    {
        if (tableReference.RelationshipId == null || tableReference.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tableReferenceIdMissing", "Worksheet tablePart must have an r:id attribute.");
            diagnostic.SetLocation(part.PartPath, "tablePart", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(tableReference.RelationshipId);
            if (!relationshipId.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tableReferenceIdInvalid", "Worksheet tablePart r:id is invalid.");
                diagnostic.SetLocation(part.PartPath, "tablePart", "r:id");
                diagnostic.SetRelationshipId(tableReference.RelationshipId);
                result.AddDiagnostic(diagnostic);
                foreach (XlsxDiagnostic idDiagnostic in relationshipId.Validation.Diagnostics)
                {
                    result.AddDiagnostic(idDiagnostic);
                }
            }
        }

        if (tableReference.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.tableReferenceUnknownAttributesNull", "Worksheet tablePart unknown attributes list must not be null."));
        }
    }

    internal static void ValidatePivotTableReferences(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part)
    {
        if (part.PivotTableReferences == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pivotTableReferencesNull", "Worksheet pivot table reference list must not be null.");
            diagnostic.SetLocation(part.PartPath, "pivotTableDefinition", "");
            result.AddDiagnostic(diagnostic);
            return;
        }

        int index = 0;
        foreach (XlsxWorksheetPivotTableReference pivotTableReference in part.PivotTableReferences)
        {
            if (pivotTableReference == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pivotTableReferenceNull", "Worksheet pivot table reference list must not contain null entries.");
                diagnostic.SetLocation(part.PartPath, "pivotTableDefinition", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidatePivotTableReference(result, part, pivotTableReference, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidatePivotTableReference(XlsxWorksheetPartWriteResult result, XlsxWorksheetPart part, XlsxWorksheetPivotTableReference pivotTableReference, int index)
    {
        if (pivotTableReference.RelationshipId == null || pivotTableReference.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pivotTableRelationshipIdMissing", "Worksheet pivotTableDefinition must have an r:id attribute.");
            diagnostic.SetLocation(part.PartPath, "pivotTableDefinition", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(pivotTableReference.RelationshipId);
            if (!relationshipId.IsValid)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pivotTableRelationshipIdInvalid", "Worksheet pivotTableDefinition r:id is invalid.");
                diagnostic.SetLocation(part.PartPath, "pivotTableDefinition", "r:id");
                diagnostic.SetRelationshipId(pivotTableReference.RelationshipId);
                result.AddDiagnostic(diagnostic);
                foreach (XlsxDiagnostic idDiagnostic in relationshipId.Validation.Diagnostics)
                {
                    result.AddDiagnostic(idDiagnostic);
                }
            }
        }

        if (pivotTableReference.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.pivotTableUnknownAttributesNull", "Worksheet pivotTableDefinition unknown attribute list must not be null."));
        }
    }

    internal static void AddHyperlinkReferenceDiagnostics(XlsxWorksheetPartWriteResult result, XlsxA1RangeReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddHyperlinkRelationshipIdDiagnostics(XlsxWorksheetPartWriteResult result, XlsxXmlValueParseResult relationshipId)
    {
        foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
