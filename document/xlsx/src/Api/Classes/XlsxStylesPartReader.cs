using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads styles part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxStylesPartReader : IXlsxStylesPartReader
{
    /// <summary>
    /// Initializes a new styles part reader with its default state.
    /// </summary>
    public XlsxStylesPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The styles part produced by the operation.</returns>
    public XlsxStylesPart ReadString(string xml)
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
    /// <returns>The styles part produced by the operation.</returns>
    public XlsxStylesPart ReadBytes(byte[] bytes)
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
    /// <returns>The styles part produced by the operation.</returns>
    public XlsxStylesPart ReadStream(Stream stream)
    {
        XlsxStylesPart part = new XlsxStylesPart();
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

                if (reader.Depth == 1 && IsStylesNamespace(part, reader.NamespaceURI))
                {
                    ReadStylesChild(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "styles.missingRoot", "Styles XML must contain a styleSheet root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxStylesPart CreateMissingRootResult()
    {
        XlsxStylesPart part = new XlsxStylesPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "styles.missingRoot", "Styles XML must contain a styleSheet root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxStylesPart part, XmlReader reader)
    {
        if (reader.LocalName != "styleSheet" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "styles.invalidRoot", "Styles XML root must be a styleSheet element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        XlsxSharedStringTableReader.ReadUnknownAttributes(reader, part.UnknownRootAttributes, "", "");
    }

    internal static void ReadStylesChild(XlsxStylesPart part, XmlReader reader)
    {
        if (reader.LocalName == "numFmts")
        {
            ReadNumberFormats(part, reader);
        }
        else if (reader.LocalName == "fonts")
        {
            ReadRawStyleCollection(part, reader, part.Fonts, 1);
        }
        else if (reader.LocalName == "fills")
        {
            ReadRawStyleCollection(part, reader, part.Fills, 2);
        }
        else if (reader.LocalName == "borders")
        {
            ReadRawStyleCollection(part, reader, part.Borders, 3);
        }
        else if (reader.LocalName == "cellStyleXfs")
        {
            ReadCellFormats(part, reader, part.CellStyleFormats, 4);
        }
        else if (reader.LocalName == "cellXfs")
        {
            ReadCellFormats(part, reader, part.CellFormats, 5);
        }
        else if (reader.LocalName == "cellStyles")
        {
            ReadNamedStyles(part, reader);
        }
        else if (reader.LocalName == "dxfs")
        {
            ReadRawStyleCollection(part, reader, part.DifferentialFormats, 6);
        }
        else if (reader.LocalName == "tableStyles")
        {
            ReadTableStyles(part, reader);
        }
        else if (reader.LocalName == "colors")
        {
            part.Colors.RawXml = reader.ReadOuterXml();
            ReadTypedColors(part, part.Colors.RawXml);
            part.HasColors = true;
        }
        else if (reader.LocalName == "extLst")
        {
            string rawXml = reader.ReadOuterXml();
            part.ExtensionList = new XlsxExtensionListReader().ReadString(rawXml);
            part.HasExtensionList = true;
        }
        else
        {
            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadNumberFormats(XlsxStylesPart part, XmlReader reader)
    {
        SetCollectionCount(part, reader.GetAttribute("count"), 0);
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsStylesNamespace(part, reader.NamespaceURI) && reader.LocalName == "numFmt")
            {
                ReadNumberFormat(part, reader);
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadNumberFormat(XlsxStylesPart part, XmlReader reader)
    {
        XlsxNumberFormatDefinition format = new XlsxNumberFormatDefinition();
        SetOptionalUInt(part, reader.GetAttribute("numFmtId"), "numFmt", "numFmtId", format, 1);
        format.FormatCode = reader.GetAttribute("formatCode");
        XlsxSharedStringTableReader.ReadUnknownAttributes(reader, format.UnknownAttributes, "numFmtId", "formatCode");
        part.NumberFormats.Add(format);
        reader.ReadOuterXml();
    }

    internal static void ReadRawStyleCollection(XlsxStylesPart part, XmlReader reader, List<XlsxRawStyleElement> items, int target)
    {
        SetCollectionCount(part, reader.GetAttribute("count"), target);
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsStylesNamespace(part, reader.NamespaceURI))
            {
                XlsxRawStyleElement item = new XlsxRawStyleElement();
                item.RawXml = reader.ReadOuterXml();
                items.Add(item);
                AddTypedRawStyle(part, item.RawXml, target);
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadCellFormats(XlsxStylesPart part, XmlReader reader, List<XlsxCellFormatDefinition> items, int target)
    {
        SetCollectionCount(part, reader.GetAttribute("count"), target);
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsStylesNamespace(part, reader.NamespaceURI) && reader.LocalName == "xf")
            {
                items.Add(ReadCellFormat(part, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxCellFormatDefinition ReadCellFormat(XlsxStylesPart part, XmlReader reader)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        SetOptionalUInt(part, reader.GetAttribute("numFmtId"), "xf", "numFmtId", format, 1);
        SetOptionalUInt(part, reader.GetAttribute("fontId"), "xf", "fontId", format, 2);
        SetOptionalUInt(part, reader.GetAttribute("fillId"), "xf", "fillId", format, 3);
        SetOptionalUInt(part, reader.GetAttribute("borderId"), "xf", "borderId", format, 4);
        SetOptionalUInt(part, reader.GetAttribute("xfId"), "xf", "xfId", format, 5);
        SetOptionalBool(part, reader.GetAttribute("applyNumberFormat"), "xf", "applyNumberFormat", format, 1);
        SetOptionalBool(part, reader.GetAttribute("applyFont"), "xf", "applyFont", format, 2);
        SetOptionalBool(part, reader.GetAttribute("applyFill"), "xf", "applyFill", format, 3);
        SetOptionalBool(part, reader.GetAttribute("applyBorder"), "xf", "applyBorder", format, 4);
        ReadUnknownCellFormatAttributes(reader, format.UnknownAttributes);
        format.RawXml = reader.ReadOuterXml();
        ReadCellFormatChildren(format);
        return format;
    }

    internal static void ReadTableStyles(XlsxStylesPart part, XmlReader reader)
    {
        XlsxStylesTableStyles tableStyles = new XlsxStylesTableStyles();
        SetOptionalUInt(part, reader.GetAttribute("count"), "tableStyles", "count", tableStyles, 1);
        tableStyles.DefaultTableStyle = reader.GetAttribute("defaultTableStyle");
        tableStyles.DefaultPivotStyle = reader.GetAttribute("defaultPivotStyle");
        ReadUnknownTableStylesAttributes(reader, tableStyles.UnknownAttributes);
        tableStyles.RawXml = reader.ReadOuterXml();
        ReadTypedTableStyles(tableStyles);
        part.TableStyles = tableStyles;
        part.HasTableStyles = true;
    }

    internal static void AddTypedRawStyle(XlsxStylesPart part, string rawXml, int target)
    {
        if (target == 1)
        {
            part.TypedFonts.Add(ReadTypedFont(rawXml));
        }
        else if (target == 2)
        {
            part.TypedFills.Add(ReadTypedFill(rawXml));
        }
        else if (target == 3)
        {
            part.TypedBorders.Add(ReadTypedBorder(rawXml));
        }
        else if (target == 6)
        {
            part.TypedDifferentialFormats.Add(ReadTypedDifferentialStyle(rawXml));
        }
    }

    internal static XlsxStyleFont ReadTypedFont(string rawXml)
    {
        XlsxStyleFont font = new XlsxStyleFont();
        font.RawXml = rawXml;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        string name = reader.LocalName;
                        if (name == "name") { font.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")); }
                        else if (name == "sz") { SetFontNumber(font, reader.GetAttribute("val"), 1); }
                        else if (name == "family") { SetFontNumber(font, reader.GetAttribute("val"), 2); }
                        else if (name == "charset") { SetFontNumber(font, reader.GetAttribute("val"), 3); }
                        else if (name == "b") { font.Bold = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "i") { font.Italic = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "strike") { font.Strike = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "outline") { font.Outline = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "shadow") { font.Shadow = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "condense") { font.Condense = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "extend") { font.Extend = ReadStyleBoolean(reader.GetAttribute("val")); }
                        else if (name == "u") { font.Underline = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")); if (font.Underline.Length == 0) { font.Underline = "single"; } }
                        else if (name == "vertAlign") { font.VerticalAlignment = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")); }
                        else if (name == "scheme") { font.Scheme = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")); }
                        else if (name == "color") { font.Color = ReadTypedColor(reader); font.HasColor = true; }
                    }
                    reader.Read();
                }
            }
        }
        return font;
    }

    internal static void SetFontNumber(XlsxStyleFont font, string? text, int target)
    {
        if (target == 1)
        {
            XlsxXmlValueParseResult parsedDouble = XlsxXmlValueParser.ParseDouble(XlsxCellValueConverter.NormalizeText(text));
            if (parsedDouble.IsValid) { font.Size = parsedDouble.DoubleValue; font.HasSize = true; }
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(text));
        if (!parsed.IsValid) { return; }
        if (target == 2) { font.Family = parsed.UnsignedIntValue; font.HasFamily = true; }
        else { font.CharacterSet = parsed.UnsignedIntValue; font.HasCharacterSet = true; }
    }

    internal static XlsxStyleFill ReadTypedFill(string rawXml)
    {
        XlsxStyleFill fill = new XlsxStyleFill();
        fill.RawXml = rawXml;
        XlsxStyleGradientStop? stop = null;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.LocalName == "patternFill") { fill.PatternType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("patternType")); }
                        else if (reader.LocalName == "fgColor") { fill.ForegroundColor = ReadTypedColor(reader); fill.HasForegroundColor = true; }
                        else if (reader.LocalName == "bgColor") { fill.BackgroundColor = ReadTypedColor(reader); fill.HasBackgroundColor = true; }
                        else if (reader.LocalName == "gradientFill") { fill.IsGradient = true; fill.GradientType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type")); SetFillDegree(fill, reader.GetAttribute("degree")); }
                        else if (reader.LocalName == "stop") { stop = new XlsxStyleGradientStop(); SetStopPosition(stop, reader.GetAttribute("position")); fill.GradientStops.Add(stop); }
                        else if (reader.LocalName == "color" && stop != null) { stop.Color = ReadTypedColor(reader); }
                    }
                    reader.Read();
                }
            }
        }
        return fill;
    }

    internal static void SetFillDegree(XlsxStyleFill fill, string? text)
    {
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDouble(XlsxCellValueConverter.NormalizeText(text));
        if (parsed.IsValid) { fill.Degree = parsed.DoubleValue; fill.HasDegree = true; }
    }

    internal static void SetStopPosition(XlsxStyleGradientStop stop, string? text)
    {
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDouble(XlsxCellValueConverter.NormalizeText(text));
        if (parsed.IsValid) { stop.Position = parsed.DoubleValue; }
    }

    internal static XlsxStyleBorder ReadTypedBorder(string rawXml)
    {
        XlsxStyleBorder border = new XlsxStyleBorder();
        border.RawXml = rawXml;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "border")
                    {
                        string? diagonalUp = reader.GetAttribute("diagonalUp");
                        string? diagonalDown = reader.GetAttribute("diagonalDown");
                        string? outline = reader.GetAttribute("outline");
                        if (diagonalUp != null) { border.DiagonalUp = ReadStyleBoolean(diagonalUp); }
                        if (diagonalDown != null) { border.DiagonalDown = ReadStyleBoolean(diagonalDown); }
                        if (outline != null) { border.Outline = ReadStyleBoolean(outline); }
                        reader.Read();
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && IsBorderSide(reader.LocalName))
                    {
                        SetBorderSide(border, reader.LocalName, ReadTypedBorderSide(reader.ReadOuterXml()));
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        return border;
    }

    internal static bool IsBorderSide(string name)
    {
        return name == "left" || name == "right" || name == "top" || name == "bottom" || name == "diagonal" || name == "vertical" || name == "horizontal";
    }

    internal static XlsxStyleBorderSide ReadTypedBorderSide(string rawXml)
    {
        XlsxStyleBorderSide side = new XlsxStyleBorderSide();
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0) { side.Style = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("style")); }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "color") { side.Color = ReadTypedColor(reader); side.HasColor = true; }
                    reader.Read();
                }
            }
        }
        return side;
    }

    internal static void SetBorderSide(XlsxStyleBorder border, string name, XlsxStyleBorderSide side)
    {
        if (name == "left") { border.Left = side; }
        else if (name == "right") { border.Right = side; }
        else if (name == "top") { border.Top = side; }
        else if (name == "bottom") { border.Bottom = side; }
        else if (name == "diagonal") { border.Diagonal = side; }
        else if (name == "vertical") { border.Vertical = side; }
        else { border.Horizontal = side; }
    }

    internal static XlsxDifferentialStyle ReadTypedDifferentialStyle(string rawXml)
    {
        XlsxDifferentialStyle style = new XlsxDifferentialStyle();
        style.RawXml = rawXml;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        string name = reader.LocalName;
                        string child = reader.ReadOuterXml();
                        if (name == "font") { style.Font = ReadTypedFont(child); style.HasFont = true; }
                        else if (name == "fill") { style.Fill = ReadTypedFill(child); style.HasFill = true; }
                        else if (name == "border") { style.Border = ReadTypedBorder(child); style.HasBorder = true; }
                        else if (name == "alignment") { style.Alignment = ReadTypedAlignment(child); style.HasAlignment = true; }
                        else if (name == "protection") { style.Protection = ReadTypedProtection(child); style.HasProtection = true; }
                        else if (name == "numFmt") { style.NumberFormat = ReadTypedNumberFormat(child); style.HasNumberFormat = true; }
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        return style;
    }

    internal static XlsxNumberFormatDefinition ReadTypedNumberFormat(string rawXml)
    {
        XlsxNumberFormatDefinition format = new XlsxNumberFormatDefinition();
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        XlsxXmlValueParseResult id = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("numFmtId")));
                        if (id.IsValid) { format.NumberFormatId = id.UnsignedIntValue; format.HasNumberFormatId = true; }
                        format.FormatCode = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("formatCode"));
                        return format;
                    }
                    reader.Read();
                }
            }
        }
        return format;
    }

    internal static void ReadCellFormatChildren(XlsxCellFormatDefinition format)
    {
        using (StringReader text = new StringReader(format.RawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "alignment") { format.Alignment = ReadTypedAlignment(reader.ReadOuterXml()); format.HasAlignment = true; continue; }
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "protection") { format.Protection = ReadTypedProtection(reader.ReadOuterXml()); format.HasProtection = true; continue; }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxStyleAlignment ReadTypedAlignment(string rawXml)
    {
        XlsxStyleAlignment alignment = new XlsxStyleAlignment();
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        alignment.Horizontal = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("horizontal")); alignment.Vertical = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("vertical"));
                        SetAlignmentInteger(alignment, reader.GetAttribute("textRotation"), 1); SetAlignmentBoolean(alignment, reader.GetAttribute("wrapText"), 1);
                        SetAlignmentBoolean(alignment, reader.GetAttribute("shrinkToFit"), 2); SetAlignmentInteger(alignment, reader.GetAttribute("indent"), 2); SetAlignmentInteger(alignment, reader.GetAttribute("readingOrder"), 3);
                        return alignment;
                    }
                    reader.Read();
                }
            }
        }
        return alignment;
    }

    internal static void SetAlignmentBoolean(XlsxStyleAlignment alignment, string? text, int target)
    {
        if (text == null || text.Length == 0) { return; }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text); if (!parsed.IsValid) { return; }
        if (target == 1) { alignment.WrapText = parsed.BooleanValue; alignment.HasWrapText = true; }
        else { alignment.ShrinkToFit = parsed.BooleanValue; alignment.HasShrinkToFit = true; }
    }

    internal static void SetAlignmentInteger(XlsxStyleAlignment alignment, string? text, int target)
    {
        if (text == null || text.Length == 0) { return; }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(text); if (!parsed.IsValid) { return; }
        if (target == 1) { alignment.TextRotation = (int)parsed.UnsignedIntValue; alignment.HasTextRotation = true; }
        else if (target == 2) { alignment.Indent = parsed.UnsignedIntValue; alignment.HasIndent = true; }
        else { alignment.ReadingOrder = parsed.UnsignedIntValue; alignment.HasReadingOrder = true; }
    }

    internal static XlsxStyleProtection ReadTypedProtection(string rawXml)
    {
        XlsxStyleProtection protection = new XlsxStyleProtection();
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        SetProtectionBoolean(protection, reader.GetAttribute("locked"), 1); SetProtectionBoolean(protection, reader.GetAttribute("hidden"), 2); return protection;
                    }
                    reader.Read();
                }
            }
        }
        return protection;
    }

    internal static void SetProtectionBoolean(XlsxStyleProtection protection, string? text, int target)
    {
        if (text == null || text.Length == 0) { return; }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text); if (!parsed.IsValid) { return; }
        if (target == 1) { protection.Locked = parsed.BooleanValue; protection.HasLocked = true; }
        else { protection.Hidden = parsed.BooleanValue; protection.HasHidden = true; }
    }

    internal static XlsxSpreadsheetColor ReadTypedColor(XmlReader reader)
    {
        XlsxSpreadsheetColor color = new XlsxSpreadsheetColor();
        string? rgb = reader.GetAttribute("rgb"); if (rgb != null) { color.Rgb = rgb; color.HasRgb = true; }
        XlsxXmlValueParseResult indexed = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("indexed"))); if (indexed.IsValid) { color.Indexed = indexed.UnsignedIntValue; color.HasIndexed = true; }
        XlsxXmlValueParseResult theme = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("theme"))); if (theme.IsValid) { color.Theme = theme.UnsignedIntValue; color.HasTheme = true; }
        XlsxXmlValueParseResult tint = XlsxXmlValueParser.ParseDouble(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("tint"))); if (tint.IsValid) { color.Tint = tint.DoubleValue; color.HasTint = true; }
        string? auto = reader.GetAttribute("auto"); if (auto != null) { color.IsAuto = ReadStyleBoolean(auto); color.HasAuto = true; }
        return color;
    }

    internal static bool ReadStyleBoolean(string? text)
    {
        if (text == null || text.Length == 0) { return true; }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text); return parsed.IsValid && parsed.BooleanValue;
    }

    internal static void ReadNamedStyles(XlsxStylesPart part, XmlReader reader)
    {
        string rawXml = reader.ReadOuterXml();
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader child = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!child.EOF)
                {
                    if (child.NodeType == XmlNodeType.Element && child.LocalName == "cellStyle")
                    {
                        XlsxNamedStyle style = new XlsxNamedStyle();
                        style.Name = XlsxCellValueConverter.NormalizeText(child.GetAttribute("name"));
                        XlsxXmlValueParseResult formatId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(child.GetAttribute("xfId")));
                        if (formatId.IsValid) { style.FormatId = formatId.UnsignedIntValue; }
                        XlsxXmlValueParseResult builtInId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(child.GetAttribute("builtinId")));
                        if (builtInId.IsValid) { style.BuiltInId = builtInId.UnsignedIntValue; style.HasBuiltInId = true; }
                        string? hidden = child.GetAttribute("hidden"); if (hidden != null) { style.Hidden = ReadStyleBoolean(hidden); style.HasHidden = true; }
                        part.NamedStyles.Add(style);
                    }
                    child.Read();
                }
            }
        }
    }

    internal static void ReadTypedTableStyles(XlsxStylesTableStyles tableStyles)
    {
        using (StringReader text = new StringReader(tableStyles.RawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && (reader.LocalName == "tableStyle" || reader.LocalName == "slicerStyle"))
                    {
                        string name = reader.LocalName;
                        XlsxTableStyleDefinition style = ReadTypedTableStyle(reader.ReadOuterXml(), name == "slicerStyle");
                        if (style.AppliesToSlicers) { tableStyles.SlicerStyles.Add(style); }
                        else { tableStyles.Styles.Add(style); }
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxTableStyleDefinition ReadTypedTableStyle(string rawXml, bool slicer)
    {
        XlsxTableStyleDefinition style = new XlsxTableStyleDefinition();
        style.RawXml = rawXml;
        style.AppliesToSlicers = slicer;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
                    {
                        style.Name = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("name"));
                        string? table = reader.GetAttribute("table"); if (table != null) { style.AppliesToTables = ReadStyleBoolean(table); }
                        string? pivot = reader.GetAttribute("pivot"); if (pivot != null) { style.AppliesToPivots = ReadStyleBoolean(pivot); }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && (reader.LocalName == "tableStyleElement" || reader.LocalName == "slicerStyleElement"))
                    {
                        XlsxTableStyleElement element = new XlsxTableStyleElement();
                        element.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
                        XlsxXmlValueParseResult dxfId = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("dxfId"))); if (dxfId.IsValid) { element.DifferentialFormatId = dxfId.UnsignedIntValue; }
                        XlsxXmlValueParseResult size = XlsxXmlValueParser.ParseUnsignedInteger(XlsxCellValueConverter.NormalizeText(reader.GetAttribute("size"))); if (size.IsValid) { element.Size = size.UnsignedIntValue; element.HasSize = true; }
                        style.Elements.Add(element);
                    }
                    reader.Read();
                }
            }
        }
        return style;
    }

    internal static void ReadTypedColors(XlsxStylesPart part, string rawXml)
    {
        int target = 0;
        using (StringReader text = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "indexedColors") { target = 1; }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "mruColors") { target = 2; }
                    else if (reader.NodeType == XmlNodeType.Element && (reader.LocalName == "rgbColor" || (target == 2 && reader.LocalName == "color")))
                    {
                        XlsxIndexedStyleColor item = new XlsxIndexedStyleColor(); item.Color = ReadTypedColor(reader);
                        if (target == 1) { part.IndexedColors.Add(item); } else if (target == 2) { part.MostRecentlyUsedColors.Add(item); }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && (reader.LocalName == "indexedColors" || reader.LocalName == "mruColors")) { target = 0; }
                    reader.Read();
                }
            }
        }
    }

    internal static void ReadUnknownCellFormatAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && IsKnownCellFormatAttribute(reader.LocalName);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownTableStylesAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && IsKnownTableStylesAttribute(reader.LocalName);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownCellFormatAttribute(string localName)
    {
        return localName == "numFmtId" || localName == "fontId" || localName == "fillId" || localName == "borderId" || localName == "xfId" || localName == "applyNumberFormat" || localName == "applyFont" || localName == "applyFill" || localName == "applyBorder";
    }

    internal static bool IsKnownTableStylesAttribute(string localName)
    {
        return localName == "count" || localName == "defaultTableStyle" || localName == "defaultPivotStyle";
    }

    internal static void SetCollectionCount(XlsxStylesPart part, string? text, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, "count");
            return;
        }

        if (target == 1)
        {
            part.FontsCount = result.UnsignedIntValue;
            part.HasFontsCount = true;
        }
        else if (target == 2)
        {
            part.FillsCount = result.UnsignedIntValue;
            part.HasFillsCount = true;
        }
        else if (target == 3)
        {
            part.BordersCount = result.UnsignedIntValue;
            part.HasBordersCount = true;
        }
        else if (target == 4)
        {
            part.CellStyleFormatsCount = result.UnsignedIntValue;
            part.HasCellStyleFormatsCount = true;
        }
        else if (target == 5)
        {
            part.CellFormatsCount = result.UnsignedIntValue;
            part.HasCellFormatsCount = true;
        }
        else if (target == 6)
        {
            part.DifferentialFormatsCount = result.UnsignedIntValue;
            part.HasDifferentialFormatsCount = true;
        }
        else
        {
            part.NumberFormatsCount = result.UnsignedIntValue;
            part.HasNumberFormatsCount = true;
        }
    }

    internal static void SetOptionalUInt(XlsxStylesPart part, string? text, string elementName, string attributeName, XlsxNumberFormatDefinition format, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, attributeName);
            return;
        }

        format.NumberFormatId = result.UnsignedIntValue;
        format.HasNumberFormatId = true;
    }

    internal static void SetOptionalUInt(XlsxStylesPart part, string? text, string elementName, string attributeName, XlsxCellFormatDefinition format, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, attributeName);
            return;
        }

        SetCellFormatUInt(format, result.UnsignedIntValue, target);
    }

    internal static void SetCellFormatUInt(XlsxCellFormatDefinition format, uint value, int target)
    {
        if (target == 1)
        {
            format.NumberFormatId = value;
            format.HasNumberFormatId = true;
        }
        else if (target == 2)
        {
            format.FontId = value;
            format.HasFontId = true;
        }
        else if (target == 3)
        {
            format.FillId = value;
            format.HasFillId = true;
        }
        else if (target == 4)
        {
            format.BorderId = value;
            format.HasBorderId = true;
        }
        else if (target == 5)
        {
            format.FormatId = value;
            format.HasFormatId = true;
        }
    }

    internal static void SetOptionalUInt(XlsxStylesPart part, string? text, string elementName, string attributeName, XlsxStylesTableStyles tableStyles, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, attributeName);
            return;
        }

        tableStyles.Count = result.UnsignedIntValue;
        tableStyles.HasCount = true;
    }

    internal static void SetOptionalBool(XlsxStylesPart part, string? text, string elementName, string attributeName, XlsxCellFormatDefinition format, int target)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        if (!result.IsValid)
        {
            AddParseDiagnostics(part, result, attributeName);
            return;
        }

        if (target == 1)
        {
            format.ApplyNumberFormat = result.BooleanValue;
            format.HasApplyNumberFormat = true;
        }
        else if (target == 2)
        {
            format.ApplyFont = result.BooleanValue;
            format.HasApplyFont = true;
        }
        else if (target == 3)
        {
            format.ApplyFill = result.BooleanValue;
            format.HasApplyFill = true;
        }
        else if (target == 4)
        {
            format.ApplyBorder = result.BooleanValue;
            format.HasApplyBorder = true;
        }
    }

    internal static void AddParseDiagnostics(XlsxStylesPart part, XlsxXmlValueParseResult result, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, "", attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsStylesNamespace(XlsxStylesPart part, string namespaceUri)
    {
        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }
}
