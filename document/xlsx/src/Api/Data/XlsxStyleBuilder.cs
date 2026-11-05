using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Builds a workbook cell style by recording only explicitly selected formatting settings before interning it in a workbook.
/// </summary>
public class XlsxStyleBuilder
{
    /// <summary>
    /// Stores the number format value used by this model.
    /// </summary>
    public string NumberFormat;
    /// <summary>
    /// Indicates whether number format is present and should be written.
    /// </summary>
    public bool HasNumberFormat;
    /// <summary>
    /// Stores the font name value used by this model.
    /// </summary>
    public string FontName;
    /// <summary>
    /// Indicates whether font name is present and should be written.
    /// </summary>
    public bool HasFontName;
    /// <summary>
    /// Stores the font size value used by this model.
    /// </summary>
    public double FontSize;
    /// <summary>
    /// Indicates whether font size is present and should be written.
    /// </summary>
    public bool HasFontSize;
    /// <summary>
    /// Stores the bold value used by this model.
    /// </summary>
    public bool Bold;
    /// <summary>
    /// Indicates whether bold is present and should be written.
    /// </summary>
    public bool HasBold;
    /// <summary>
    /// Stores the italic value used by this model.
    /// </summary>
    public bool Italic;
    /// <summary>
    /// Indicates whether italic is present and should be written.
    /// </summary>
    public bool HasItalic;
    /// <summary>
    /// Stores the font color value used by this model.
    /// </summary>
    public string FontColor;
    /// <summary>
    /// Indicates whether font color is present and should be written.
    /// </summary>
    public bool HasFontColor;
    /// <summary>
    /// Stores the fill color value used by this model.
    /// </summary>
    public string FillColor;
    /// <summary>
    /// Indicates whether fill color is present and should be written.
    /// </summary>
    public bool HasFillColor;
    /// <summary>
    /// Stores the horizontal alignment value used by this model.
    /// </summary>
    public string HorizontalAlignment;
    /// <summary>
    /// Indicates whether horizontal alignment is present and should be written.
    /// </summary>
    public bool HasHorizontalAlignment;
    /// <summary>
    /// Stores the vertical alignment value used by this model.
    /// </summary>
    public string VerticalAlignment;
    /// <summary>
    /// Indicates whether vertical alignment is present and should be written.
    /// </summary>
    public bool HasVerticalAlignment;
    /// <summary>
    /// Stores the wrap text value used by this model.
    /// </summary>
    public bool WrapText;
    /// <summary>
    /// Indicates whether wrap text is present and should be written.
    /// </summary>
    public bool HasWrapText;
    /// <summary>
    /// Contains borders.
    /// </summary>
    public List<XlsxStyleBorderSetting> Borders;
    /// <summary>Stores a complete typed font.</summary>
    public XlsxStyleFont TypedFont;
    /// <summary>Indicates whether a complete typed font is present.</summary>
    public bool HasTypedFont;
    /// <summary>Stores a complete typed fill.</summary>
    public XlsxStyleFill TypedFill;
    /// <summary>Indicates whether a complete typed fill is present.</summary>
    public bool HasTypedFill;
    /// <summary>Stores a complete typed border.</summary>
    public XlsxStyleBorder TypedBorder;
    /// <summary>Indicates whether a complete typed border is present.</summary>
    public bool HasTypedBorder;
    /// <summary>Stores complete typed alignment.</summary>
    public XlsxStyleAlignment TypedAlignment;
    /// <summary>Indicates whether complete typed alignment is present.</summary>
    public bool HasTypedAlignment;
    /// <summary>Stores complete typed protection.</summary>
    public XlsxStyleProtection TypedProtection;
    /// <summary>Indicates whether complete typed protection is present.</summary>
    public bool HasTypedProtection;
    /// <summary>
    /// Initializes a new style builder with its default state.
    /// </summary>
    public XlsxStyleBuilder()
    {
        NumberFormat = "";
        HasNumberFormat = false;
        FontName = "";
        HasFontName = false;
        FontSize = 0;
        HasFontSize = false;
        Bold = false;
        HasBold = false;
        Italic = false;
        HasItalic = false;
        FontColor = "";
        HasFontColor = false;
        FillColor = "";
        HasFillColor = false;
        HorizontalAlignment = "";
        HasHorizontalAlignment = false;
        VerticalAlignment = "";
        HasVerticalAlignment = false;
        WrapText = false;
        HasWrapText = false;
        Borders = new List<XlsxStyleBorderSetting>();
        TypedFont = new XlsxStyleFont(); HasTypedFont = false; TypedFill = new XlsxStyleFill(); HasTypedFill = false; TypedBorder = new XlsxStyleBorder(); HasTypedBorder = false;
        TypedAlignment = new XlsxStyleAlignment(); HasTypedAlignment = false; TypedProtection = new XlsxStyleProtection(); HasTypedProtection = false;
    }

    /// <summary>
    /// Sets number format and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "format">The format value used by the operation.</param>
    internal void SetNumberFormat(string format)
    {
        NumberFormat = NormalizeText(format);
        HasNumberFormat = NumberFormat.Length > 0;
    }

    /// <summary>
    /// Sets font name and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    internal void SetFontName(string name)
    {
        FontName = NormalizeText(name);
        HasFontName = FontName.Length > 0;
    }

    /// <summary>
    /// Sets font size and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "size">The size value used by the operation.</param>
    internal void SetFontSize(double size)
    {
        FontSize = size;
        HasFontSize = true;
    }

    /// <summary>
    /// Sets bold and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    internal void SetBold(bool value)
    {
        Bold = value;
        HasBold = true;
    }

    /// <summary>
    /// Sets italic and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    internal void SetItalic(bool value)
    {
        Italic = value;
        HasItalic = true;
    }

    /// <summary>
    /// Sets font color and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rgb">The rgb value used by the operation.</param>
    internal void SetFontColor(string rgb)
    {
        FontColor = NormalizeRgb(rgb);
        HasFontColor = FontColor.Length > 0;
    }

    /// <summary>
    /// Sets fill color and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rgb">The rgb value used by the operation.</param>
    internal void SetFillColor(string? rgb)
    {
        FillColor = NormalizeRgb(rgb);
        HasFillColor = FillColor.Length > 0;
    }

    /// <summary>
    /// Sets border and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "side">The side value used by the operation.</param>
    /// <param name = "style">The style value used by the operation.</param>
    /// <param name = "rgb">The rgb value used by the operation.</param>
    internal void SetBorder(string side, string style, string rgb)
    {
        string normalizedSide = NormalizeText(side);
        XlsxStyleBorderSetting? setting = FindBorder(normalizedSide);
        if (setting == null)
        {
            setting = new XlsxStyleBorderSetting();
            setting.Side = normalizedSide;
            Borders.Add(setting);
        }

        setting.Style = NormalizeText(style);
        setting.Rgb = NormalizeRgb(rgb);
    }

    /// <summary>
    /// Sets horizontal alignment and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    internal void SetHorizontalAlignment(string value)
    {
        HorizontalAlignment = NormalizeText(value);
        HasHorizontalAlignment = HorizontalAlignment.Length > 0;
    }

    /// <summary>
    /// Sets vertical alignment and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    internal void SetVerticalAlignment(string value)
    {
        VerticalAlignment = NormalizeText(value);
        HasVerticalAlignment = VerticalAlignment.Length > 0;
    }

    /// <summary>
    /// Sets wrap text and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    internal void SetWrapText(bool value)
    {
        WrapText = value;
        HasWrapText = true;
    }

    internal void SetTypedFont(XlsxStyleFont value) { TypedFont = value; HasTypedFont = value != null; }

    internal void SetTypedFill(XlsxStyleFill value) { TypedFill = value; HasTypedFill = value != null; }

    internal void SetTypedBorder(XlsxStyleBorder value) { TypedBorder = value; HasTypedBorder = value != null; }

    internal void SetTypedAlignment(XlsxStyleAlignment value) { TypedAlignment = value; HasTypedAlignment = value != null; }

    internal void SetTypedProtection(XlsxStyleProtection value) { TypedProtection = value; HasTypedProtection = value != null; }

    /// <summary>
    /// Adds to workbook to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int AddToWorkbook(XlsxWorkbook? workbook)
    {
        if (workbook == null)
        {
            return -1;
        }

        if (workbook.Styles == null)
        {
            workbook.Styles = new XlsxStylesPart();
        }

        workbook.HasStyles = true;
        return AddToStylesPart(workbook.Styles);
    }

    /// <summary>
    /// Adds to styles part to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "styles">The styles value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int AddToStylesPart(XlsxStylesPart? styles)
    {
        if (styles == null)
        {
            return -1;
        }

        XlsxStyleInternPool pool = new XlsxStyleInternPool();
        uint numberFormatId = 0;
        bool hasNumberFormatId = false;
        uint fontId = 0;
        bool hasFontId = false;
        uint fillId = 0;
        bool hasFillId = false;
        uint borderId = 0;
        bool hasBorderId = false;
        if (HasNumberFormat)
        {
            XlsxNumberFormatDefinition numberFormat = new XlsxNumberFormatDefinition();
            numberFormat.FormatCode = NumberFormat;
            numberFormatId = pool.GetOrAddNumberFormat(styles, numberFormat);
            hasNumberFormatId = true;
        }

        if (HasTypedFont)
        {
            int typedFontId = new XlsxStylesPartOperations().InternFont(styles, TypedFont);
            if (typedFontId >= 0) { fontId = (uint)typedFontId; hasFontId = true; }
        }
        else if (HasFontValues())
        {
            XlsxRawStyleElement font = new XlsxRawStyleElement();
            font.RawXml = BuildFontRawXml();
            fontId = pool.GetOrAddFont(styles, font);
            hasFontId = true;
        }

        if (HasTypedFill)
        {
            int typedFillId = new XlsxStylesPartOperations().InternFill(styles, TypedFill);
            if (typedFillId >= 0) { fillId = (uint)typedFillId; hasFillId = true; }
        }
        else if (HasFillColor)
        {
            XlsxRawStyleElement fill = new XlsxRawStyleElement();
            fill.RawXml = BuildFillRawXml();
            fillId = pool.GetOrAddFill(styles, fill);
            hasFillId = true;
        }

        if (HasTypedBorder)
        {
            int typedBorderId = new XlsxStylesPartOperations().InternBorder(styles, TypedBorder);
            if (typedBorderId >= 0) { borderId = (uint)typedBorderId; hasBorderId = true; }
        }
        else if (Borders.Count > 0)
        {
            XlsxRawStyleElement border = new XlsxRawStyleElement();
            border.RawXml = BuildBorderRawXml();
            borderId = pool.GetOrAddBorder(styles, border);
            hasBorderId = true;
        }

        XlsxCellFormatDefinition format = BuildCellFormat(numberFormatId, hasNumberFormatId, fontId, hasFontId, fillId, hasFillId, borderId, hasBorderId);
        return (int)pool.GetOrAddCellFormat(styles, format);
    }

    /// <summary>
    /// Performs the build cell format operation using the current model state.
    /// </summary>
    /// <param name = "numberFormatId">The number format id value used by the operation.</param>
    /// <param name = "hasNumberFormatId">The has number format id value used by the operation.</param>
    /// <param name = "fontId">The font id value used by the operation.</param>
    /// <param name = "hasFontId">The has font id value used by the operation.</param>
    /// <param name = "fillId">The fill id value used by the operation.</param>
    /// <param name = "hasFillId">The has fill id value used by the operation.</param>
    /// <param name = "borderId">The border id value used by the operation.</param>
    /// <param name = "hasBorderId">The has border id value used by the operation.</param>
    /// <returns>The cell format definition produced by the operation.</returns>
    internal XlsxCellFormatDefinition BuildCellFormat(uint numberFormatId, bool hasNumberFormatId, uint fontId, bool hasFontId, uint fillId, bool hasFillId, uint borderId, bool hasBorderId)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        format.NumberFormatId = numberFormatId;
        format.HasNumberFormatId = hasNumberFormatId;
        format.FontId = fontId;
        format.HasFontId = hasFontId;
        format.FillId = fillId;
        format.HasFillId = hasFillId;
        format.BorderId = borderId;
        format.HasBorderId = hasBorderId;
        format.ApplyNumberFormat = hasNumberFormatId;
        format.HasApplyNumberFormat = hasNumberFormatId;
        format.ApplyFont = hasFontId;
        format.HasApplyFont = hasFontId;
        format.ApplyFill = hasFillId;
        format.HasApplyFill = hasFillId;
        format.ApplyBorder = hasBorderId;
        format.HasApplyBorder = hasBorderId;
        if (HasAlignmentValues())
        {
            format.RawXml = BuildCellFormatRawXml(format);
        }
        if (HasTypedAlignment)
        {
            format.Alignment = TypedAlignment; format.HasAlignment = true; format.RawXml = "";
        }
        if (HasTypedProtection)
        {
            format.Protection = TypedProtection; format.HasProtection = true; format.RawXml = "";
        }

        return format;
    }

    /// <summary>
    /// Performs the has font values operation using the current model state.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFontValues()
    {
        return HasFontName || HasFontSize || HasBold || HasItalic || HasFontColor;
    }

    /// <summary>
    /// Performs the has alignment values operation using the current model state.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAlignmentValues()
    {
        return HasHorizontalAlignment || HasVerticalAlignment || HasWrapText;
    }

    /// <summary>
    /// Performs the find border operation using the current model state.
    /// </summary>
    /// <param name = "side">The side value used by the operation.</param>
    /// <returns>The style border setting produced by the operation.</returns>
    internal XlsxStyleBorderSetting? FindBorder(string side)
    {
        foreach (XlsxStyleBorderSetting setting in Borders)
        {
            if (setting.Side == side)
            {
                return setting;
            }
        }

        return null;
    }

    /// <summary>
    /// Performs the build font raw xml operation using the current model state.
    /// </summary>
    /// <returns>The text produced by the operation.</returns>
    internal string BuildFontRawXml()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<font>");
        if (HasBold && Bold)
        {
            builder.Append("<b/>");
        }

        if (HasItalic && Italic)
        {
            builder.Append("<i/>");
        }

        if (HasFontSize)
        {
            builder.Append("<sz val=\"");
            AppendEscapedAttribute(builder, XlsxXmlValueWriter.WriteDouble(FontSize));
            builder.Append("\"/>");
        }

        if (HasFontColor)
        {
            builder.Append("<color rgb=\"");
            AppendEscapedAttribute(builder, FontColor);
            builder.Append("\"/>");
        }

        if (HasFontName)
        {
            builder.Append("<name val=\"");
            AppendEscapedAttribute(builder, FontName);
            builder.Append("\"/>");
        }

        builder.Append("</font>");
        return builder.ToString();
    }

    /// <summary>
    /// Performs the build fill raw xml operation using the current model state.
    /// </summary>
    /// <returns>The text produced by the operation.</returns>
    internal string BuildFillRawXml()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<fill><patternFill patternType=\"solid\"><fgColor rgb=\"");
        AppendEscapedAttribute(builder, FillColor);
        builder.Append("\"/><bgColor indexed=\"64\"/></patternFill></fill>");
        return builder.ToString();
    }

    /// <summary>
    /// Performs the build border raw xml operation using the current model state.
    /// </summary>
    /// <returns>The text produced by the operation.</returns>
    internal string BuildBorderRawXml()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<border>");
        foreach (XlsxStyleBorderSetting setting in Borders)
        {
            AppendBorderElement(builder, setting);
        }

        builder.Append("</border>");
        return builder.ToString();
    }

    /// <summary>
    /// Performs the build cell format raw xml operation using the current model state.
    /// </summary>
    /// <param name = "format">The format value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal string BuildCellFormatRawXml(XlsxCellFormatDefinition format)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<xf");
        AppendCellFormatAttributes(builder, format);
        builder.Append("><alignment");
        if (HasHorizontalAlignment)
        {
            builder.Append(" horizontal=\"");
            AppendEscapedAttribute(builder, HorizontalAlignment);
            builder.Append("\"");
        }

        if (HasVerticalAlignment)
        {
            builder.Append(" vertical=\"");
            AppendEscapedAttribute(builder, VerticalAlignment);
            builder.Append("\"");
        }

        if (HasWrapText)
        {
            builder.Append(" wrapText=\"");
            AppendEscapedAttribute(builder, XlsxXmlValueWriter.WriteBoolean(WrapText));
            builder.Append("\"");
        }

        builder.Append("/></xf>");
        return builder.ToString();
    }

    internal static void AppendCellFormatAttributes(StringBuilder builder, XlsxCellFormatDefinition format)
    {
        if (format.HasNumberFormatId)
        {
            builder.Append(" numFmtId=\"");
            builder.Append(XlsxXmlValueWriter.WriteUnsignedInteger(format.NumberFormatId));
            builder.Append("\"");
        }

        if (format.HasFontId)
        {
            builder.Append(" fontId=\"");
            builder.Append(XlsxXmlValueWriter.WriteUnsignedInteger(format.FontId));
            builder.Append("\"");
        }

        if (format.HasFillId)
        {
            builder.Append(" fillId=\"");
            builder.Append(XlsxXmlValueWriter.WriteUnsignedInteger(format.FillId));
            builder.Append("\"");
        }

        if (format.HasBorderId)
        {
            builder.Append(" borderId=\"");
            builder.Append(XlsxXmlValueWriter.WriteUnsignedInteger(format.BorderId));
            builder.Append("\"");
        }

        if (format.HasApplyNumberFormat)
        {
            builder.Append(" applyNumberFormat=\"");
            builder.Append(XlsxXmlValueWriter.WriteBoolean(format.ApplyNumberFormat));
            builder.Append("\"");
        }

        if (format.HasApplyFont)
        {
            builder.Append(" applyFont=\"");
            builder.Append(XlsxXmlValueWriter.WriteBoolean(format.ApplyFont));
            builder.Append("\"");
        }

        if (format.HasApplyFill)
        {
            builder.Append(" applyFill=\"");
            builder.Append(XlsxXmlValueWriter.WriteBoolean(format.ApplyFill));
            builder.Append("\"");
        }

        if (format.HasApplyBorder)
        {
            builder.Append(" applyBorder=\"");
            builder.Append(XlsxXmlValueWriter.WriteBoolean(format.ApplyBorder));
            builder.Append("\"");
        }
    }

    internal static void AppendBorderElement(StringBuilder builder, XlsxStyleBorderSetting setting)
    {
        if (setting.Side == null || setting.Side.Length == 0)
        {
            return;
        }

        builder.Append("<");
        AppendEscapedName(builder, setting.Side);
        if (setting.Style != null && setting.Style.Length > 0)
        {
            builder.Append(" style=\"");
            AppendEscapedAttribute(builder, setting.Style);
            builder.Append("\"");
        }

        if (setting.Rgb != null && setting.Rgb.Length > 0)
        {
            builder.Append("><color rgb=\"");
            AppendEscapedAttribute(builder, setting.Rgb);
            builder.Append("\"/></");
            AppendEscapedName(builder, setting.Side);
            builder.Append(">");
        }
        else
        {
            builder.Append("/>");
        }
    }

    internal static void AppendEscapedAttribute(StringBuilder builder, string text)
    {
        string value = NormalizeText(text);
        int index = 0;
        while (index < value.Length)
        {
            char ch = value[index];
            if (ch == '&')
            {
                builder.Append("&amp;");
            }
            else if (ch == '"')
            {
                builder.Append("&quot;");
            }
            else if (ch == '<')
            {
                builder.Append("&lt;");
            }
            else if (ch == '>')
            {
                builder.Append("&gt;");
            }
            else
            {
                builder.Append(ch);
            }

            index = index + 1;
        }
    }

    internal static void AppendEscapedName(StringBuilder builder, string text)
    {
        string value = NormalizeText(text);
        int index = 0;
        while (index < value.Length)
        {
            char ch = value[index];
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-')
            {
                builder.Append(ch);
            }

            index = index + 1;
        }
    }

    internal static string NormalizeText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal static string NormalizeRgb(string? rgb)
    {
        string value = NormalizeText(rgb).Trim();
        if (value.Length == 6)
        {
            return "FF" + value.ToUpperInvariant();
        }

        return value.ToUpperInvariant();
    }
}
