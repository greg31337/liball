using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes styles part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxStylesPartWriter : IXlsxStylesPartWriter
{
    /// <summary>
    /// Initializes a new styles part writer with its default state.
    /// </summary>
    public XlsxStylesPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStylesPartWriteResult WriteString(XlsxStylesPart part)
    {
        XlsxStylesPartWriteResult result = WriteBytes(part);
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
    public XlsxStylesPartWriteResult WriteBytes(XlsxStylesPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxStylesPartWriteResult result = WriteStream(stream, part);
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
    public XlsxStylesPartWriteResult WriteStream(Stream stream, XlsxStylesPart part)
    {
        XlsxStylesPartWriteResult result = ValidateStylesPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        string namespaceUri = part.NamespaceUri;
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            namespaceUri = XlsxNamespaces.Spreadsheet;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "styleSheet", namespaceUri);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteNumberFormats(writer, part, namespaceUri);
            WriteFontCollection(writer, part, namespaceUri);
            WriteFillCollection(writer, part, namespaceUri);
            WriteBorderCollection(writer, part, namespaceUri);
            WriteCellFormats(writer, "cellStyleXfs", part.CellStyleFormats, part.HasCellStyleFormatsCount, part.CellStyleFormatsCount, namespaceUri);
            WriteCellFormats(writer, "cellXfs", part.CellFormats, part.HasCellFormatsCount, part.CellFormatsCount, namespaceUri);
            WriteNamedStyles(writer, part, namespaceUri);
            WriteDifferentialStyles(writer, part, namespaceUri);
            WriteTableStyles(writer, part, namespaceUri);
            WriteColors(writer, part, namespaceUri);
            WriteExtensionList(writer, part.ExtensionList, part.HasExtensionList, namespaceUri);
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxStylesPartWriteResult ValidateStylesPart(XlsxStylesPart part)
    {
        XlsxStylesPartWriteResult result = new XlsxStylesPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "styles.partNull", "Styles part must not be null."));
            return result;
        }

        ValidateList(result, part.NumberFormats, "styles.numberFormatsNull");
        ValidateList(result, part.Fonts, "styles.fontsNull");
        ValidateList(result, part.Fills, "styles.fillsNull");
        ValidateList(result, part.Borders, "styles.bordersNull");
        ValidateList(result, part.CellStyleFormats, "styles.cellStyleFormatsNull");
        ValidateList(result, part.CellFormats, "styles.cellFormatsNull");
        ValidateList(result, part.DifferentialFormats, "styles.differentialFormatsNull");
        ValidateList(result, part.TypedFonts, "styles.typedFontsNull");
        ValidateList(result, part.TypedFills, "styles.typedFillsNull");
        ValidateList(result, part.TypedBorders, "styles.typedBordersNull");
        ValidateList(result, part.TypedDifferentialFormats, "styles.typedDifferentialFormatsNull");
        ValidateList(result, part.NamedStyles, "styles.namedStylesNull");
        ValidateList(result, part.IndexedColors, "styles.indexedColorsNull");
        ValidateList(result, part.MostRecentlyUsedColors, "styles.mruColorsNull");
        ValidateList(result, part.UnknownRootAttributes, "styles.unknownRootAttributesNull");
        ValidateList(result, part.UnknownXml, "styles.unknownXmlNull");
        ValidateNumberFormats(result, part.NumberFormats);
        ValidateRawItems(result, part.Fonts, "styles.fontNull");
        ValidateRawItems(result, part.Fills, "styles.fillNull");
        ValidateRawItems(result, part.Borders, "styles.borderNull");
        ValidateRawItems(result, part.DifferentialFormats, "styles.differentialFormatNull");
        ValidateCellFormats(result, part.CellStyleFormats, "styles.cellStyleFormatNull");
        ValidateCellFormats(result, part.CellFormats, "styles.cellFormatNull");
        return result;
    }

    internal static void ValidateList(XlsxStylesPartWriteResult result, object list, string code)
    {
        if (list == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, "Styles collection must not be null."));
        }
    }

    internal static void ValidateNumberFormats(XlsxStylesPartWriteResult result, List<XlsxNumberFormatDefinition> formats)
    {
        if (formats == null)
        {
            return;
        }

        foreach (XlsxNumberFormatDefinition format in formats)
        {
            if (format == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "styles.numberFormatNull", "Number format must not be null."));
            }
        }
    }

    internal static void ValidateRawItems(XlsxStylesPartWriteResult result, List<XlsxRawStyleElement> items, string code)
    {
        if (items == null)
        {
            return;
        }

        foreach (XlsxRawStyleElement item in items)
        {
            if (item == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, "Raw style item must not be null."));
            }
        }
    }

    internal static void ValidateCellFormats(XlsxStylesPartWriteResult result, List<XlsxCellFormatDefinition> formats, string code)
    {
        if (formats == null)
        {
            return;
        }

        foreach (XlsxCellFormatDefinition format in formats)
        {
            if (format == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, "Cell format must not be null."));
            }
        }
    }

    internal static void WriteNumberFormats(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (!part.HasNumberFormatsCount && part.NumberFormats.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "numFmts", namespaceUri);
        WriteCountAttribute(writer, part.HasNumberFormatsCount, part.NumberFormatsCount, part.NumberFormats.Count);
        foreach (XlsxNumberFormatDefinition format in part.NumberFormats)
        {
            WriteNumberFormat(writer, format, namespaceUri);
        }

        writer.WriteEndElement();
    }

    internal static void WriteNumberFormat(XmlWriter writer, XlsxNumberFormatDefinition format, string namespaceUri)
    {
        writer.WriteStartElement("", "numFmt", namespaceUri);
        if (format.HasNumberFormatId)
        {
            writer.WriteAttributeString("numFmtId", XlsxXmlValueWriter.WriteUnsignedInteger(format.NumberFormatId));
        }

        if (format.FormatCode != null && format.FormatCode.Length > 0)
        {
            writer.WriteAttributeString("formatCode", format.FormatCode);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, format.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteRawStyleCollection(XmlWriter writer, string collectionName, string itemName, List<XlsxRawStyleElement> items, bool hasCount, uint count, string namespaceUri)
    {
        if (!hasCount && items.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", collectionName, namespaceUri);
        WriteCountAttribute(writer, hasCount, count, items.Count);
        foreach (XlsxRawStyleElement item in items)
        {
            WriteRawStyleItem(writer, itemName, item, namespaceUri);
        }

        writer.WriteEndElement();
    }

    internal static void WriteRawStyleItem(XmlWriter writer, string itemName, XlsxRawStyleElement item, string namespaceUri)
    {
        if (item.RawXml != null && item.RawXml.Length > 0)
        {
            writer.WriteRaw(item.RawXml);
            return;
        }

        writer.WriteStartElement("", itemName, namespaceUri);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, item.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteCellFormats(XmlWriter writer, string collectionName, List<XlsxCellFormatDefinition> formats, bool hasCount, uint count, string namespaceUri)
    {
        if (!hasCount && formats.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", collectionName, namespaceUri);
        WriteCountAttribute(writer, hasCount, count, formats.Count);
        foreach (XlsxCellFormatDefinition format in formats)
        {
            WriteCellFormat(writer, format, namespaceUri);
        }

        writer.WriteEndElement();
    }

    internal static void WriteCellFormat(XmlWriter writer, XlsxCellFormatDefinition format, string namespaceUri)
    {
        if (format.RawXml != null && format.RawXml.Length > 0)
        {
            writer.WriteRaw(format.RawXml);
            return;
        }

        writer.WriteStartElement("", "xf", namespaceUri);
        if (format.HasNumberFormatId)
        {
            writer.WriteAttributeString("numFmtId", XlsxXmlValueWriter.WriteUnsignedInteger(format.NumberFormatId));
        }

        if (format.HasFontId)
        {
            writer.WriteAttributeString("fontId", XlsxXmlValueWriter.WriteUnsignedInteger(format.FontId));
        }

        if (format.HasFillId)
        {
            writer.WriteAttributeString("fillId", XlsxXmlValueWriter.WriteUnsignedInteger(format.FillId));
        }

        if (format.HasBorderId)
        {
            writer.WriteAttributeString("borderId", XlsxXmlValueWriter.WriteUnsignedInteger(format.BorderId));
        }

        if (format.HasFormatId)
        {
            writer.WriteAttributeString("xfId", XlsxXmlValueWriter.WriteUnsignedInteger(format.FormatId));
        }

        if (format.HasApplyNumberFormat)
        {
            writer.WriteAttributeString("applyNumberFormat", XlsxXmlValueWriter.WriteBoolean(format.ApplyNumberFormat));
        }

        if (format.HasApplyFont)
        {
            writer.WriteAttributeString("applyFont", XlsxXmlValueWriter.WriteBoolean(format.ApplyFont));
        }

        if (format.HasApplyFill)
        {
            writer.WriteAttributeString("applyFill", XlsxXmlValueWriter.WriteBoolean(format.ApplyFill));
        }

        if (format.HasApplyBorder)
        {
            writer.WriteAttributeString("applyBorder", XlsxXmlValueWriter.WriteBoolean(format.ApplyBorder));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, format.UnknownAttributes);
        if (format.HasAlignment)
        {
            WriteAlignment(writer, format.Alignment, namespaceUri);
        }
        if (format.HasProtection)
        {
            WriteProtection(writer, format.Protection, namespaceUri);
        }
        writer.WriteEndElement();
    }

    internal static void WriteTableStyles(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (!part.HasTableStyles || part.TableStyles == null)
        {
            return;
        }

        if (part.TableStyles.RawXml != null && part.TableStyles.RawXml.Length > 0 && part.TableStyles.Styles.Count == 0 && part.TableStyles.SlicerStyles.Count == 0)
        {
            writer.WriteRaw(part.TableStyles.RawXml);
            return;
        }

        writer.WriteStartElement("", "tableStyles", namespaceUri);
        if (part.TableStyles.HasCount)
        {
            writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger(part.TableStyles.Count));
        }

        if (part.TableStyles.DefaultTableStyle != null && part.TableStyles.DefaultTableStyle.Length > 0)
        {
            writer.WriteAttributeString("defaultTableStyle", part.TableStyles.DefaultTableStyle);
        }

        if (part.TableStyles.DefaultPivotStyle != null && part.TableStyles.DefaultPivotStyle.Length > 0)
        {
            writer.WriteAttributeString("defaultPivotStyle", part.TableStyles.DefaultPivotStyle);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.TableStyles.UnknownAttributes);
        foreach (XlsxTableStyleDefinition style in part.TableStyles.Styles)
        {
            WriteTableStyle(writer, style, false, namespaceUri);
        }
        foreach (XlsxTableStyleDefinition style in part.TableStyles.SlicerStyles)
        {
            WriteTableStyle(writer, style, true, namespaceUri);
        }
        writer.WriteEndElement();
    }

    internal static void WriteFontCollection(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.TypedFonts.Count == 0) { WriteRawStyleCollection(writer, "fonts", "font", part.Fonts, part.HasFontsCount, part.FontsCount, namespaceUri); return; }
        writer.WriteStartElement("", "fonts", namespaceUri); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)part.TypedFonts.Count));
        foreach (XlsxStyleFont font in part.TypedFonts) { WriteFont(writer, font, namespaceUri); }
        writer.WriteEndElement();
    }

    internal static void WriteFillCollection(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.TypedFills.Count == 0) { WriteRawStyleCollection(writer, "fills", "fill", part.Fills, part.HasFillsCount, part.FillsCount, namespaceUri); return; }
        writer.WriteStartElement("", "fills", namespaceUri); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)part.TypedFills.Count));
        foreach (XlsxStyleFill fill in part.TypedFills) { WriteFill(writer, fill, namespaceUri); }
        writer.WriteEndElement();
    }

    internal static void WriteBorderCollection(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.TypedBorders.Count == 0) { WriteRawStyleCollection(writer, "borders", "border", part.Borders, part.HasBordersCount, part.BordersCount, namespaceUri); return; }
        writer.WriteStartElement("", "borders", namespaceUri); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)part.TypedBorders.Count));
        foreach (XlsxStyleBorder border in part.TypedBorders) { WriteBorder(writer, border, namespaceUri); }
        writer.WriteEndElement();
    }

    internal static void WriteDifferentialStyles(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.TypedDifferentialFormats.Count == 0) { WriteRawStyleCollection(writer, "dxfs", "dxf", part.DifferentialFormats, part.HasDifferentialFormatsCount, part.DifferentialFormatsCount, namespaceUri); return; }
        writer.WriteStartElement("", "dxfs", namespaceUri); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)part.TypedDifferentialFormats.Count));
        foreach (XlsxDifferentialStyle style in part.TypedDifferentialFormats) { WriteDifferentialStyle(writer, style, namespaceUri); }
        writer.WriteEndElement();
    }

    internal static void WriteFont(XmlWriter writer, XlsxStyleFont font, string namespaceUri)
    {
        if (font.RawXml != null && font.RawXml.Length > 0) { writer.WriteRaw(font.RawXml); return; }
        writer.WriteStartElement("", "font", namespaceUri);
        WriteBooleanElement(writer, "b", font.Bold, namespaceUri); WriteBooleanElement(writer, "i", font.Italic, namespaceUri); WriteBooleanElement(writer, "strike", font.Strike, namespaceUri);
        WriteBooleanElement(writer, "outline", font.Outline, namespaceUri); WriteBooleanElement(writer, "shadow", font.Shadow, namespaceUri); WriteBooleanElement(writer, "condense", font.Condense, namespaceUri); WriteBooleanElement(writer, "extend", font.Extend, namespaceUri);
        if (font.HasSize) { WriteValueElement(writer, "sz", XlsxXmlValueWriter.WriteDouble(font.Size), namespaceUri); }
        if (font.HasColor) { WriteColor(writer, "color", font.Color, namespaceUri); }
        if (font.Name != null && font.Name.Length > 0) { WriteValueElement(writer, "name", font.Name, namespaceUri); }
        if (font.HasFamily) { WriteValueElement(writer, "family", XlsxXmlValueWriter.WriteUnsignedInteger(font.Family), namespaceUri); }
        if (font.HasCharacterSet) { WriteValueElement(writer, "charset", XlsxXmlValueWriter.WriteUnsignedInteger(font.CharacterSet), namespaceUri); }
        if (font.Scheme != null && font.Scheme.Length > 0) { WriteValueElement(writer, "scheme", font.Scheme, namespaceUri); }
        if (font.Underline != null && font.Underline.Length > 0) { WriteValueElement(writer, "u", font.Underline, namespaceUri); }
        if (font.VerticalAlignment != null && font.VerticalAlignment.Length > 0) { WriteValueElement(writer, "vertAlign", font.VerticalAlignment, namespaceUri); }
        WriteUnknownXml(writer, font.UnknownXml); writer.WriteEndElement();
    }

    internal static void WriteFill(XmlWriter writer, XlsxStyleFill fill, string namespaceUri)
    {
        if (fill.RawXml != null && fill.RawXml.Length > 0) { writer.WriteRaw(fill.RawXml); return; }
        writer.WriteStartElement("", "fill", namespaceUri);
        if (fill.IsGradient)
        {
            writer.WriteStartElement("", "gradientFill", namespaceUri);
            if (fill.GradientType != null && fill.GradientType.Length > 0) { writer.WriteAttributeString("type", fill.GradientType); }
            if (fill.HasDegree) { writer.WriteAttributeString("degree", XlsxXmlValueWriter.WriteDouble(fill.Degree)); }
            foreach (XlsxStyleGradientStop stop in fill.GradientStops)
            {
                writer.WriteStartElement("", "stop", namespaceUri); writer.WriteAttributeString("position", XlsxXmlValueWriter.WriteDouble(stop.Position)); WriteColor(writer, "color", stop.Color, namespaceUri); writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        else
        {
            writer.WriteStartElement("", "patternFill", namespaceUri); if (fill.PatternType != null && fill.PatternType.Length > 0) { writer.WriteAttributeString("patternType", fill.PatternType); }
            if (fill.HasForegroundColor) { WriteColor(writer, "fgColor", fill.ForegroundColor, namespaceUri); }
            if (fill.HasBackgroundColor) { WriteColor(writer, "bgColor", fill.BackgroundColor, namespaceUri); }
            writer.WriteEndElement();
        }
        WriteUnknownXml(writer, fill.UnknownXml); writer.WriteEndElement();
    }

    internal static void WriteBorder(XmlWriter writer, XlsxStyleBorder border, string namespaceUri)
    {
        if (border.RawXml != null && border.RawXml.Length > 0) { writer.WriteRaw(border.RawXml); return; }
        writer.WriteStartElement("", "border", namespaceUri);
        if (border.DiagonalUp) { writer.WriteAttributeString("diagonalUp", "1"); } if (border.DiagonalDown) { writer.WriteAttributeString("diagonalDown", "1"); } if (border.Outline) { writer.WriteAttributeString("outline", "1"); }
        WriteBorderSide(writer, "left", border.Left, namespaceUri); WriteBorderSide(writer, "right", border.Right, namespaceUri); WriteBorderSide(writer, "top", border.Top, namespaceUri);
        WriteBorderSide(writer, "bottom", border.Bottom, namespaceUri); WriteBorderSide(writer, "diagonal", border.Diagonal, namespaceUri); WriteBorderSide(writer, "vertical", border.Vertical, namespaceUri); WriteBorderSide(writer, "horizontal", border.Horizontal, namespaceUri);
        WriteUnknownXml(writer, border.UnknownXml); writer.WriteEndElement();
    }

    internal static void WriteBorderSide(XmlWriter writer, string name, XlsxStyleBorderSide side, string namespaceUri)
    {
        if (side == null || ((side.Style == null || side.Style.Length == 0) && !side.HasColor)) { return; }
        writer.WriteStartElement("", name, namespaceUri); if (side.Style != null && side.Style.Length > 0) { writer.WriteAttributeString("style", side.Style); }
        if (side.HasColor) { WriteColor(writer, "color", side.Color, namespaceUri); } writer.WriteEndElement();
    }

    internal static void WriteDifferentialStyle(XmlWriter writer, XlsxDifferentialStyle style, string namespaceUri)
    {
        if (style.RawXml != null && style.RawXml.Length > 0) { writer.WriteRaw(style.RawXml); return; }
        writer.WriteStartElement("", "dxf", namespaceUri);
        if (style.HasFont) { WriteFont(writer, style.Font, namespaceUri); } if (style.HasFill) { WriteFill(writer, style.Fill, namespaceUri); } if (style.HasBorder) { WriteBorder(writer, style.Border, namespaceUri); }
        if (style.HasNumberFormat) { WriteNumberFormat(writer, style.NumberFormat, namespaceUri); } if (style.HasAlignment) { WriteAlignment(writer, style.Alignment, namespaceUri); } if (style.HasProtection) { WriteProtection(writer, style.Protection, namespaceUri); }
        writer.WriteEndElement();
    }

    internal static void WriteAlignment(XmlWriter writer, XlsxStyleAlignment alignment, string namespaceUri)
    {
        writer.WriteStartElement("", "alignment", namespaceUri);
        if (alignment.Horizontal != null && alignment.Horizontal.Length > 0) { writer.WriteAttributeString("horizontal", alignment.Horizontal); }
        if (alignment.Vertical != null && alignment.Vertical.Length > 0) { writer.WriteAttributeString("vertical", alignment.Vertical); }
        if (alignment.HasTextRotation) { writer.WriteAttributeString("textRotation", XlsxXmlValueWriter.WriteInteger(alignment.TextRotation)); }
        if (alignment.HasWrapText) { writer.WriteAttributeString("wrapText", XlsxXmlValueWriter.WriteBoolean(alignment.WrapText)); }
        if (alignment.HasShrinkToFit) { writer.WriteAttributeString("shrinkToFit", XlsxXmlValueWriter.WriteBoolean(alignment.ShrinkToFit)); }
        if (alignment.HasIndent) { writer.WriteAttributeString("indent", XlsxXmlValueWriter.WriteUnsignedInteger(alignment.Indent)); }
        if (alignment.HasReadingOrder) { writer.WriteAttributeString("readingOrder", XlsxXmlValueWriter.WriteUnsignedInteger(alignment.ReadingOrder)); }
        writer.WriteEndElement();
    }

    internal static void WriteProtection(XmlWriter writer, XlsxStyleProtection protection, string namespaceUri)
    {
        writer.WriteStartElement("", "protection", namespaceUri);
        if (protection.HasLocked) { writer.WriteAttributeString("locked", XlsxXmlValueWriter.WriteBoolean(protection.Locked)); }
        if (protection.HasHidden) { writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(protection.Hidden)); }
        writer.WriteEndElement();
    }

    internal static void WriteColor(XmlWriter writer, string name, XlsxSpreadsheetColor color, string namespaceUri)
    {
        writer.WriteStartElement("", name, namespaceUri);
        if (color.HasRgb) { writer.WriteAttributeString("rgb", color.Rgb); } if (color.HasAuto) { writer.WriteAttributeString("auto", XlsxXmlValueWriter.WriteBoolean(color.IsAuto)); }
        if (color.HasIndexed) { writer.WriteAttributeString("indexed", XlsxXmlValueWriter.WriteUnsignedInteger(color.Indexed)); } if (color.HasTheme) { writer.WriteAttributeString("theme", XlsxXmlValueWriter.WriteUnsignedInteger(color.Theme)); }
        if (color.HasTint) { writer.WriteAttributeString("tint", XlsxXmlValueWriter.WriteDouble(color.Tint)); } writer.WriteEndElement();
    }

    internal static void WriteBooleanElement(XmlWriter writer, string name, bool value, string namespaceUri)
    {
        if (!value) { return; } writer.WriteStartElement("", name, namespaceUri); writer.WriteEndElement();
    }

    internal static void WriteValueElement(XmlWriter writer, string name, string value, string namespaceUri)
    {
        writer.WriteStartElement("", name, namespaceUri); writer.WriteAttributeString("val", value); writer.WriteEndElement();
    }

    internal static void WriteNamedStyles(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.NamedStyles.Count == 0) { return; }
        writer.WriteStartElement("", "cellStyles", namespaceUri); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)part.NamedStyles.Count));
        foreach (XlsxNamedStyle style in part.NamedStyles)
        {
            writer.WriteStartElement("", "cellStyle", namespaceUri); writer.WriteAttributeString("name", style.Name); writer.WriteAttributeString("xfId", XlsxXmlValueWriter.WriteUnsignedInteger(style.FormatId));
            if (style.HasBuiltInId) { writer.WriteAttributeString("builtinId", XlsxXmlValueWriter.WriteUnsignedInteger(style.BuiltInId)); } if (style.HasHidden) { writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(style.Hidden)); } writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteTableStyle(XmlWriter writer, XlsxTableStyleDefinition style, bool slicer, string namespaceUri)
    {
        if (style.RawXml != null && style.RawXml.Length > 0) { writer.WriteRaw(style.RawXml); return; }
        string rootName = "tableStyle"; string elementName = "tableStyleElement"; if (slicer) { rootName = "slicerStyle"; elementName = "slicerStyleElement"; }
        writer.WriteStartElement("", rootName, namespaceUri); writer.WriteAttributeString("name", style.Name); writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)style.Elements.Count));
        if (!slicer) { writer.WriteAttributeString("table", XlsxXmlValueWriter.WriteBoolean(style.AppliesToTables)); writer.WriteAttributeString("pivot", XlsxXmlValueWriter.WriteBoolean(style.AppliesToPivots)); }
        foreach (XlsxTableStyleElement element in style.Elements)
        {
            writer.WriteStartElement("", elementName, namespaceUri); writer.WriteAttributeString("type", element.Type); writer.WriteAttributeString("dxfId", XlsxXmlValueWriter.WriteUnsignedInteger(element.DifferentialFormatId)); if (element.HasSize) { writer.WriteAttributeString("size", XlsxXmlValueWriter.WriteUnsignedInteger(element.Size)); } writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteColors(XmlWriter writer, XlsxStylesPart part, string namespaceUri)
    {
        if (part.IndexedColors.Count == 0 && part.MostRecentlyUsedColors.Count == 0) { WriteRawElement(writer, part.HasColors, part.Colors); return; }
        writer.WriteStartElement("", "colors", namespaceUri);
        if (part.IndexedColors.Count > 0) { writer.WriteStartElement("", "indexedColors", namespaceUri); foreach (XlsxIndexedStyleColor color in part.IndexedColors) { WriteColor(writer, "rgbColor", color.Color, namespaceUri); } writer.WriteEndElement(); }
        if (part.MostRecentlyUsedColors.Count > 0) { writer.WriteStartElement("", "mruColors", namespaceUri); foreach (XlsxIndexedStyleColor color in part.MostRecentlyUsedColors) { WriteColor(writer, "color", color.Color, namespaceUri); } writer.WriteEndElement(); }
        writer.WriteEndElement();
    }

    internal static void WriteRawElement(XmlWriter writer, bool hasElement, XlsxRawStyleElement item)
    {
        if (!hasElement || item == null || item.RawXml == null || item.RawXml.Length == 0)
        {
            return;
        }

        writer.WriteRaw(item.RawXml);
    }

    internal static void WriteExtensionList(XmlWriter writer, XlsxExtensionList list, bool hasList, string namespaceUri)
    {
        if (!hasList || list == null)
        {
            return;
        }

        if (list.RawXml != null && list.RawXml.Length > 0)
        {
            writer.WriteRaw(list.RawXml);
            return;
        }

        string prefix = list.Prefix;
        if (prefix == null)
        {
            prefix = "";
        }

        string extensionNamespaceUri = list.NamespaceUri;
        if (extensionNamespaceUri == null || extensionNamespaceUri.Length == 0)
        {
            extensionNamespaceUri = namespaceUri;
        }

        writer.WriteStartElement(prefix, "extLst", extensionNamespaceUri);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, list.UnknownRootAttributes);
        foreach (XlsxExtension extension in list.Extensions)
        {
            XlsxExtensionListWriter.WriteExtension(writer, extension, extensionNamespaceUri, prefix);
        }

        foreach (string unknownXml in list.UnknownXml)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }

        writer.WriteEndElement();
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

    internal static void WriteCountAttribute(XmlWriter writer, bool hasCount, uint count, int itemCount)
    {
        if (hasCount)
        {
            writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger(count));
            return;
        }

        if (itemCount > 0)
        {
            writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger((uint)itemCount));
        }
    }
}
