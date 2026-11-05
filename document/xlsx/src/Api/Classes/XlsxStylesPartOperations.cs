using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>Provides typed stylesheet interning, mutation, and resolution operations.</summary>
public class XlsxStylesPartOperations : IXlsxStylesPartOperations
{
    /// <summary>Initializes stylesheet operations.</summary>
    public XlsxStylesPartOperations() { }

    /// <summary>Interns a font and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="font">The typed font.</param><returns>The font index, or -1 on invalid input.</returns>
    public int InternFont(XlsxStylesPart data, XlsxStyleFont font)
    {
        if (data == null || font == null) { return -1; }
        string key = SerializeFont(font); int index = 0;
        while (index < data.TypedFonts.Count) { if (SerializeFont(data.TypedFonts[index]) == key) { return index; } index = index + 1; }
        data.TypedFonts.Add(CopyStyleFont(font)); XlsxRawStyleElement raw = new XlsxRawStyleElement(); raw.RawXml = key; data.Fonts.Add(raw); UpdateCounts(data); return data.TypedFonts.Count - 1;
    }

    /// <summary>Interns a fill and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="fill">The typed fill.</param><returns>The fill index, or -1 on invalid input.</returns>
    public int InternFill(XlsxStylesPart data, XlsxStyleFill fill)
    {
        if (data == null || fill == null) { return -1; }
        string key = SerializeFill(fill); int index = 0;
        while (index < data.TypedFills.Count) { if (SerializeFill(data.TypedFills[index]) == key) { return index; } index = index + 1; }
        data.TypedFills.Add(CopyStyleFill(fill)); XlsxRawStyleElement raw = new XlsxRawStyleElement(); raw.RawXml = key; data.Fills.Add(raw); UpdateCounts(data); return data.TypedFills.Count - 1;
    }

    /// <summary>Interns a border and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="border">The typed border.</param><returns>The border index, or -1 on invalid input.</returns>
    public int InternBorder(XlsxStylesPart data, XlsxStyleBorder border)
    {
        if (data == null || border == null) { return -1; }
        string key = SerializeBorder(border); int index = 0;
        while (index < data.TypedBorders.Count) { if (SerializeBorder(data.TypedBorders[index]) == key) { return index; } index = index + 1; }
        data.TypedBorders.Add(CopyStyleBorder(border)); XlsxRawStyleElement raw = new XlsxRawStyleElement(); raw.RawXml = key; data.Borders.Add(raw); UpdateCounts(data); return data.TypedBorders.Count - 1;
    }

    /// <summary>Interns a differential style and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="style">The differential style.</param><returns>The differential style index, or -1 on invalid input.</returns>
    public int InternDifferentialStyle(XlsxStylesPart data, XlsxDifferentialStyle style)
    {
        if (data == null || style == null) { return -1; }
        string key = SerializeDifferentialStyle(style); int index = 0;
        while (index < data.TypedDifferentialFormats.Count) { if (SerializeDifferentialStyle(data.TypedDifferentialFormats[index]) == key) { return index; } index = index + 1; }
        data.TypedDifferentialFormats.Add(CopyDifferentialStyle(style)); XlsxRawStyleElement raw = new XlsxRawStyleElement(); raw.RawXml = key; data.DifferentialFormats.Add(raw); UpdateCounts(data); return data.TypedDifferentialFormats.Count - 1;
    }

    /// <summary>Interns a cell format and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="format">The cell format.</param><returns>The cell format index, or -1 on invalid input.</returns>
    public int InternCellFormat(XlsxStylesPart data, XlsxCellFormatDefinition format)
    {
        if (data == null || format == null) { return -1; }
        return (int)new XlsxStyleInternPool().GetOrAddCellFormat(data, CloneCellFormat(format));
    }

    /// <summary>Replaces a font at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="font">The replacement font.</param><returns>Validation diagnostics.</returns>
    public XlsxValidationResult UpdateFont(XlsxStylesPart data, int index, XlsxStyleFont font)
    {
        if (data == null || font == null) { return ValidateIndex(data, index, 0, font, "styles.fontIndexRange"); }
        XlsxValidationResult result = ValidateIndex(data, index, data.TypedFonts.Count, font, "styles.fontIndexRange"); if (!result.IsValid) { return result; }
        string rawXml = SerializeFont(font); data.TypedFonts[index] = CopyStyleFont(font); data.Fonts[index].RawXml = rawXml; return result;
    }

    /// <summary>Replaces a fill at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="fill">The replacement fill.</param><returns>Validation diagnostics.</returns>
    public XlsxValidationResult UpdateFill(XlsxStylesPart data, int index, XlsxStyleFill fill)
    {
        if (data == null || fill == null) { return ValidateIndex(data, index, 0, fill, "styles.fillIndexRange"); }
        XlsxValidationResult result = ValidateIndex(data, index, data.TypedFills.Count, fill, "styles.fillIndexRange"); if (!result.IsValid) { return result; }
        string rawXml = SerializeFill(fill); data.TypedFills[index] = CopyStyleFill(fill); data.Fills[index].RawXml = rawXml; return result;
    }

    /// <summary>Replaces a border at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="border">The replacement border.</param><returns>Validation diagnostics.</returns>
    public XlsxValidationResult UpdateBorder(XlsxStylesPart data, int index, XlsxStyleBorder border)
    {
        if (data == null || border == null) { return ValidateIndex(data, index, 0, border, "styles.borderIndexRange"); }
        XlsxValidationResult result = ValidateIndex(data, index, data.TypedBorders.Count, border, "styles.borderIndexRange"); if (!result.IsValid) { return result; }
        string rawXml = SerializeBorder(border); data.TypedBorders[index] = CopyStyleBorder(border); data.Borders[index].RawXml = rawXml; return result;
    }

    /// <summary>Replaces a cell format at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="format">The replacement format.</param><returns>Validation diagnostics.</returns>
    public XlsxValidationResult UpdateCellFormat(XlsxStylesPart data, int index, XlsxCellFormatDefinition format)
    {
        if (data == null || format == null) { return ValidateIndex(data, index, 0, format, "styles.cellFormatIndexRange"); }
        XlsxValidationResult result = ValidateIndex(data, index, data.CellFormats.Count, format, "styles.cellFormatIndexRange"); if (!result.IsValid) { return result; }
        data.CellFormats[index] = CloneCellFormat(format); data.CellFormats[index].RawXml = ""; return result;
    }

    /// <summary>Adds a named cell style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The named style.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddNamedStyle(XlsxStylesPart data, XlsxNamedStyle style)
    {
        if (data == null || style == null || style.Name == null || style.Name.Length == 0) { return -1; } data.NamedStyles.Add(CloneNamedStyle(style)); return data.NamedStyles.Count - 1;
    }

    /// <summary>Adds a custom table style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The table style.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddTableStyle(XlsxStylesPart data, XlsxTableStyleDefinition style)
    {
        if (!ValidateTableStyle(data, style)) { return -1; } XlsxTableStyleDefinition stored = CloneTableStyle(style); stored.RawXml = ""; stored.AppliesToTables = true; data.TableStyles.Styles.Add(stored); data.HasTableStyles = true; return data.TableStyles.Styles.Count - 1;
    }

    /// <summary>Adds a custom pivot style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The pivot style.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddPivotStyle(XlsxStylesPart data, XlsxTableStyleDefinition style)
    {
        if (!ValidateTableStyle(data, style)) { return -1; } XlsxTableStyleDefinition stored = CloneTableStyle(style); stored.RawXml = ""; stored.AppliesToPivots = true; data.TableStyles.Styles.Add(stored); data.HasTableStyles = true; return data.TableStyles.Styles.Count - 1;
    }

    /// <summary>Adds a custom slicer style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The slicer style.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddSlicerStyle(XlsxStylesPart data, XlsxTableStyleDefinition style)
    {
        if (!ValidateTableStyle(data, style)) { return -1; } XlsxTableStyleDefinition stored = CloneTableStyle(style); stored.RawXml = ""; stored.AppliesToSlicers = true; data.TableStyles.SlicerStyles.Add(stored); data.HasTableStyles = true; return data.TableStyles.SlicerStyles.Count - 1;
    }

    /// <summary>Adds an indexed color.</summary>
    /// <param name="data">The styles part.</param><param name="color">The color.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddIndexedColor(XlsxStylesPart data, XlsxSpreadsheetColor color)
    {
        if (data == null || color == null) { return -1; } XlsxIndexedStyleColor item = new XlsxIndexedStyleColor(); item.Color = CloneSpreadsheetColor(color); data.IndexedColors.Add(item); data.HasColors = true; return data.IndexedColors.Count - 1;
    }

    /// <summary>Adds a theme-based color to the recent color collection.</summary>
    /// <param name="data">The styles part.</param><param name="theme">The theme color index.</param><param name="tint">The tint adjustment.</param><returns>The added index, or -1 on invalid input.</returns>
    public int AddThemeColor(XlsxStylesPart data, uint theme, double tint)
    {
        if (data == null || tint < -1 || tint > 1) { return -1; } XlsxIndexedStyleColor item = new XlsxIndexedStyleColor(); item.Color.Theme = theme; item.Color.HasTheme = true; item.Color.Tint = tint; item.Color.HasTint = true;
        data.MostRecentlyUsedColors.Add(item); data.HasColors = true; return data.MostRecentlyUsedColors.Count - 1;
    }

    /// <summary>Resolves a cell format and its referenced components.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based cell format index.</param><returns>The resolved style and diagnostics.</returns>
    public XlsxResolvedCellStyle ResolveCellFormat(XlsxStylesPart data, int index)
    {
        XlsxResolvedCellStyle result = new XlsxResolvedCellStyle();
        if (data == null || index < 0 || index >= data.CellFormats.Count) { result.AddError("styles.cellFormatIndexRange", "Cell format index is outside the styles collection."); return result; }
        XlsxCellFormatDefinition format = data.CellFormats[index]; XlsxCellFormatDefinition resolvedFormat = CloneCellFormat(format); result.CellFormat = resolvedFormat; result.Alignment = resolvedFormat.Alignment; result.HasAlignment = resolvedFormat.HasAlignment; result.Protection = resolvedFormat.Protection; result.HasProtection = resolvedFormat.HasProtection;
        if (format.HasFontId) { int fontIndex = (int)format.FontId; if (fontIndex < data.TypedFonts.Count) { result.Font = CopyStyleFont(data.TypedFonts[fontIndex]); result.HasFont = true; } else if (fontIndex < data.Fonts.Count) { result.Font = CloneStyleFont(data.Fonts[fontIndex].RawXml); result.HasFont = true; } else { result.AddError("styles.fontIndexRange", "Cell format font index is outside the font collection."); } }
        if (format.HasFillId) { int fillIndex = (int)format.FillId; if (fillIndex < data.TypedFills.Count) { result.Fill = CopyStyleFill(data.TypedFills[fillIndex]); result.HasFill = true; } else if (fillIndex < data.Fills.Count) { result.Fill = CloneStyleFill(data.Fills[fillIndex].RawXml); result.HasFill = true; } else { result.AddError("styles.fillIndexRange", "Cell format fill index is outside the fill collection."); } }
        if (format.HasBorderId) { int borderIndex = (int)format.BorderId; if (borderIndex < data.TypedBorders.Count) { result.Border = CopyStyleBorder(data.TypedBorders[borderIndex]); result.HasBorder = true; } else if (borderIndex < data.Borders.Count) { result.Border = CloneStyleBorder(data.Borders[borderIndex].RawXml); result.HasBorder = true; } else { result.AddError("styles.borderIndexRange", "Cell format border index is outside the border collection."); } }
        if (format.HasNumberFormatId) { result.NumberFormatCode = ResolveNumberFormat(data, format.NumberFormatId); }
        result.IsValid = result.Validation.IsValid; return result;
    }

    internal static XlsxStyleFont CopyStyleFont(XlsxStyleFont source)
    {
        if (source.RawXml != null && source.RawXml.Length > 0) { return XlsxStylesPartReader.ReadTypedFont(source.RawXml); }
        XlsxStyleFont value = new XlsxStyleFont();
        value.Name = XlsxCellValueConverter.NormalizeText(source.Name); value.Size = source.Size; value.HasSize = source.HasSize; value.Bold = source.Bold; value.Italic = source.Italic;
        value.Strike = source.Strike; value.Outline = source.Outline; value.Shadow = source.Shadow; value.Condense = source.Condense; value.Extend = source.Extend;
        value.Underline = XlsxCellValueConverter.NormalizeText(source.Underline); value.VerticalAlignment = XlsxCellValueConverter.NormalizeText(source.VerticalAlignment);
        value.Family = source.Family; value.HasFamily = source.HasFamily; value.CharacterSet = source.CharacterSet; value.HasCharacterSet = source.HasCharacterSet;
        value.Scheme = XlsxCellValueConverter.NormalizeText(source.Scheme); value.Color = CloneSpreadsheetColor(source.Color); value.HasColor = source.HasColor;
        foreach (string unknown in source.UnknownXml) { value.UnknownXml.Add(unknown); }
        return value;
    }

    internal static XlsxStyleFill CopyStyleFill(XlsxStyleFill source)
    {
        if (source.RawXml != null && source.RawXml.Length > 0) { return XlsxStylesPartReader.ReadTypedFill(source.RawXml); }
        XlsxStyleFill value = new XlsxStyleFill();
        value.PatternType = XlsxCellValueConverter.NormalizeText(source.PatternType); value.ForegroundColor = CloneSpreadsheetColor(source.ForegroundColor); value.HasForegroundColor = source.HasForegroundColor;
        value.BackgroundColor = CloneSpreadsheetColor(source.BackgroundColor); value.HasBackgroundColor = source.HasBackgroundColor; value.IsGradient = source.IsGradient;
        value.GradientType = XlsxCellValueConverter.NormalizeText(source.GradientType); value.Degree = source.Degree; value.HasDegree = source.HasDegree;
        foreach (XlsxStyleGradientStop sourceStop in source.GradientStops)
        {
            XlsxStyleGradientStop stop = new XlsxStyleGradientStop(); stop.Position = sourceStop.Position; stop.Color = CloneSpreadsheetColor(sourceStop.Color); value.GradientStops.Add(stop);
        }
        foreach (string unknown in source.UnknownXml) { value.UnknownXml.Add(unknown); }
        return value;
    }

    internal static XlsxStyleBorder CopyStyleBorder(XlsxStyleBorder source)
    {
        if (source.RawXml != null && source.RawXml.Length > 0) { return XlsxStylesPartReader.ReadTypedBorder(source.RawXml); }
        XlsxStyleBorder value = new XlsxStyleBorder();
        value.Left = CopyBorderSide(source.Left); value.Right = CopyBorderSide(source.Right); value.Top = CopyBorderSide(source.Top); value.Bottom = CopyBorderSide(source.Bottom);
        value.Diagonal = CopyBorderSide(source.Diagonal); value.Vertical = CopyBorderSide(source.Vertical); value.Horizontal = CopyBorderSide(source.Horizontal);
        value.DiagonalUp = source.DiagonalUp; value.DiagonalDown = source.DiagonalDown; value.Outline = source.Outline;
        foreach (string unknown in source.UnknownXml) { value.UnknownXml.Add(unknown); }
        return value;
    }

    internal static XlsxStyleBorderSide CopyBorderSide(XlsxStyleBorderSide source)
    {
        XlsxStyleBorderSide value = new XlsxStyleBorderSide();
        if (source == null) { return value; }
        value.Style = XlsxCellValueConverter.NormalizeText(source.Style); value.Color = CloneSpreadsheetColor(source.Color); value.HasColor = source.HasColor;
        return value;
    }

    internal static XlsxDifferentialStyle CopyDifferentialStyle(XlsxDifferentialStyle source)
    {
        if (source.RawXml != null && source.RawXml.Length > 0) { return XlsxStylesPartReader.ReadTypedDifferentialStyle(source.RawXml); }
        XlsxDifferentialStyle value = new XlsxDifferentialStyle();
        value.NumberFormat = CopyNumberFormat(source.NumberFormat); value.HasNumberFormat = source.HasNumberFormat; value.Font = CopyStyleFont(source.Font); value.HasFont = source.HasFont;
        value.Fill = CopyStyleFill(source.Fill); value.HasFill = source.HasFill; value.Border = CopyStyleBorder(source.Border); value.HasBorder = source.HasBorder;
        value.Alignment = CloneStyleAlignment(source.Alignment); value.HasAlignment = source.HasAlignment; value.Protection = CloneStyleProtection(source.Protection); value.HasProtection = source.HasProtection;
        return value;
    }

    internal static XlsxNumberFormatDefinition CopyNumberFormat(XlsxNumberFormatDefinition source)
    {
        XlsxNumberFormatDefinition value = new XlsxNumberFormatDefinition();
        value.NumberFormatId = source.NumberFormatId; value.HasNumberFormatId = source.HasNumberFormatId; value.FormatCode = XlsxCellValueConverter.NormalizeText(source.FormatCode);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes) { value.UnknownAttributes.Add(CloneXmlAttribute(attribute)); }
        return value;
    }

    internal static XlsxStyleFont CloneStyleFont(string rawXml)
    {
        XlsxStyleFont value = XlsxStylesPartReader.ReadTypedFont(rawXml);
        value.RawXml = "";
        return value;
    }

    internal static XlsxStyleFill CloneStyleFill(string rawXml)
    {
        XlsxStyleFill value = XlsxStylesPartReader.ReadTypedFill(rawXml);
        value.RawXml = "";
        return value;
    }

    internal static XlsxStyleBorder CloneStyleBorder(string rawXml)
    {
        XlsxStyleBorder value = XlsxStylesPartReader.ReadTypedBorder(rawXml);
        value.RawXml = "";
        return value;
    }

    internal static XlsxDifferentialStyle CloneDifferentialStyle(string rawXml)
    {
        XlsxDifferentialStyle value = XlsxStylesPartReader.ReadTypedDifferentialStyle(rawXml);
        value.RawXml = "";
        return value;
    }

    internal static XlsxCellFormatDefinition CloneCellFormat(XlsxCellFormatDefinition source)
    {
        XlsxCellFormatDefinition value = new XlsxCellFormatDefinition();
        value.NumberFormatId = source.NumberFormatId; value.HasNumberFormatId = source.HasNumberFormatId; value.FontId = source.FontId; value.HasFontId = source.HasFontId;
        value.FillId = source.FillId; value.HasFillId = source.HasFillId; value.BorderId = source.BorderId; value.HasBorderId = source.HasBorderId;
        value.FormatId = source.FormatId; value.HasFormatId = source.HasFormatId; value.ApplyNumberFormat = source.ApplyNumberFormat; value.HasApplyNumberFormat = source.HasApplyNumberFormat;
        value.ApplyFont = source.ApplyFont; value.HasApplyFont = source.HasApplyFont; value.ApplyFill = source.ApplyFill; value.HasApplyFill = source.HasApplyFill;
        value.ApplyBorder = source.ApplyBorder; value.HasApplyBorder = source.HasApplyBorder; value.Alignment = CloneStyleAlignment(source.Alignment); value.HasAlignment = source.HasAlignment;
        value.Protection = CloneStyleProtection(source.Protection); value.HasProtection = source.HasProtection; value.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes) { value.UnknownAttributes.Add(CloneXmlAttribute(attribute)); }
        return value;
    }

    internal static XlsxStyleAlignment CloneStyleAlignment(XlsxStyleAlignment source)
    {
        XlsxStyleAlignment value = new XlsxStyleAlignment();
        if (source == null) { return value; }
        value.Horizontal = XlsxCellValueConverter.NormalizeText(source.Horizontal); value.Vertical = XlsxCellValueConverter.NormalizeText(source.Vertical);
        value.TextRotation = source.TextRotation; value.HasTextRotation = source.HasTextRotation; value.WrapText = source.WrapText; value.HasWrapText = source.HasWrapText;
        value.ShrinkToFit = source.ShrinkToFit; value.HasShrinkToFit = source.HasShrinkToFit; value.Indent = source.Indent; value.HasIndent = source.HasIndent;
        value.ReadingOrder = source.ReadingOrder; value.HasReadingOrder = source.HasReadingOrder;
        return value;
    }

    internal static XlsxStyleProtection CloneStyleProtection(XlsxStyleProtection source)
    {
        XlsxStyleProtection value = new XlsxStyleProtection();
        if (source == null) { return value; }
        value.Locked = source.Locked; value.HasLocked = source.HasLocked; value.Hidden = source.Hidden; value.HasHidden = source.HasHidden;
        return value;
    }

    internal static XlsxNamedStyle CloneNamedStyle(XlsxNamedStyle source)
    {
        XlsxNamedStyle value = new XlsxNamedStyle();
        value.Name = XlsxCellValueConverter.NormalizeText(source.Name); value.FormatId = source.FormatId; value.BuiltInId = source.BuiltInId;
        value.HasBuiltInId = source.HasBuiltInId; value.Hidden = source.Hidden; value.HasHidden = source.HasHidden;
        return value;
    }

    internal static XlsxTableStyleDefinition CloneTableStyle(XlsxTableStyleDefinition source)
    {
        XlsxTableStyleDefinition value = new XlsxTableStyleDefinition();
        value.Name = XlsxCellValueConverter.NormalizeText(source.Name); value.AppliesToTables = source.AppliesToTables; value.AppliesToPivots = source.AppliesToPivots;
        value.AppliesToSlicers = source.AppliesToSlicers; value.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        foreach (XlsxTableStyleElement sourceElement in source.Elements)
        {
            XlsxTableStyleElement element = new XlsxTableStyleElement();
            element.Type = XlsxCellValueConverter.NormalizeText(sourceElement.Type); element.DifferentialFormatId = sourceElement.DifferentialFormatId;
            element.Size = sourceElement.Size; element.HasSize = sourceElement.HasSize; value.Elements.Add(element);
        }
        return value;
    }

    internal static XlsxSpreadsheetColor CloneSpreadsheetColor(XlsxSpreadsheetColor source)
    {
        XlsxSpreadsheetColor value = new XlsxSpreadsheetColor();
        value.Rgb = XlsxCellValueConverter.NormalizeText(source.Rgb); value.HasRgb = source.HasRgb; value.IsAuto = source.IsAuto; value.HasAuto = source.HasAuto;
        value.Indexed = source.Indexed; value.HasIndexed = source.HasIndexed; value.Theme = source.Theme; value.HasTheme = source.HasTheme; value.Tint = source.Tint; value.HasTint = source.HasTint;
        return value;
    }

    internal static XlsxXmlAttribute CloneXmlAttribute(XlsxXmlAttribute source)
    {
        XlsxXmlAttribute value = new XlsxXmlAttribute();
        if (source == null) { return value; }
        value.NamespaceUri = XlsxCellValueConverter.NormalizeText(source.NamespaceUri); value.LocalName = XlsxCellValueConverter.NormalizeText(source.LocalName);
        value.Prefix = XlsxCellValueConverter.NormalizeText(source.Prefix); value.Value = XlsxCellValueConverter.NormalizeText(source.Value);
        return value;
    }

    internal static string ResolveNumberFormat(XlsxStylesPart data, uint id)
    {
        foreach (XlsxNumberFormatDefinition format in data.NumberFormats) { if (format.HasNumberFormatId && format.NumberFormatId == id) { return XlsxCellValueConverter.NormalizeText(format.FormatCode); } }
        return XlsxNumberFormatParser.GetBuiltInFormatCode(id);
    }

    internal static XlsxValidationResult ValidateIndex(XlsxStylesPart? data, int index, int count, object? value, string code)
    {
        XlsxValidationResult result = new XlsxValidationResult(); if (data == null || value == null || index < 0 || index >= count) { result.AddError(code, "Style index is outside the target collection or the replacement value is null."); } return result;
    }

    internal static bool ValidateTableStyle(XlsxStylesPart data, XlsxTableStyleDefinition style)
    {
        return data != null && style != null && style.Name != null && style.Name.Length > 0;
    }

    internal static void UpdateCounts(XlsxStylesPart data)
    {
        data.FontsCount = (uint)data.Fonts.Count; data.HasFontsCount = data.Fonts.Count > 0; data.FillsCount = (uint)data.Fills.Count; data.HasFillsCount = data.Fills.Count > 0;
        data.BordersCount = (uint)data.Borders.Count; data.HasBordersCount = data.Borders.Count > 0; data.DifferentialFormatsCount = (uint)data.DifferentialFormats.Count; data.HasDifferentialFormatsCount = data.DifferentialFormats.Count > 0;
    }

    internal static XmlWriterSettings CreateFragmentSettings()
    {
        XmlWriterSettings settings = XlsxXmlWriterHelpers.CreateStableWriterSettings(); settings.OmitXmlDeclaration = true; settings.ConformanceLevel = ConformanceLevel.Fragment; return settings;
    }

    internal static string SerializeFont(XlsxStyleFont font)
    {
        if (font.RawXml != null && font.RawXml.Length > 0) { return font.RawXml; }
        StringBuilder text = new StringBuilder(); using (XmlWriter writer = XmlWriter.Create(text, CreateFragmentSettings())) { XlsxStylesPartWriter.WriteFont(writer, font, XlsxNamespaces.Spreadsheet); } return text.ToString();
    }

    internal static string SerializeFill(XlsxStyleFill fill)
    {
        if (fill.RawXml != null && fill.RawXml.Length > 0) { return fill.RawXml; }
        StringBuilder text = new StringBuilder(); using (XmlWriter writer = XmlWriter.Create(text, CreateFragmentSettings())) { XlsxStylesPartWriter.WriteFill(writer, fill, XlsxNamespaces.Spreadsheet); } return text.ToString();
    }

    internal static string SerializeBorder(XlsxStyleBorder border)
    {
        if (border.RawXml != null && border.RawXml.Length > 0) { return border.RawXml; }
        StringBuilder text = new StringBuilder(); using (XmlWriter writer = XmlWriter.Create(text, CreateFragmentSettings())) { XlsxStylesPartWriter.WriteBorder(writer, border, XlsxNamespaces.Spreadsheet); } return text.ToString();
    }

    internal static string SerializeDifferentialStyle(XlsxDifferentialStyle style)
    {
        if (style.RawXml != null && style.RawXml.Length > 0) { return style.RawXml; }
        StringBuilder text = new StringBuilder(); using (XmlWriter writer = XmlWriter.Create(text, CreateFragmentSettings())) { XlsxStylesPartWriter.WriteDifferentialStyle(writer, style, XlsxNamespaces.Spreadsheet); } return text.ToString();
    }
}
