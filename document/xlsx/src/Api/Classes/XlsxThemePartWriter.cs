using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes theme part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxThemePartWriter : IXlsxThemePartWriter
{
    /// <summary>
    /// Initializes a new theme part writer with its default state.
    /// </summary>
    public XlsxThemePartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxThemePartWriteResult WriteString(XlsxThemePart part)
    {
        XlsxThemePartWriteResult result = WriteBytes(part);
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
    public XlsxThemePartWriteResult WriteBytes(XlsxThemePart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxThemePartWriteResult result = WriteStream(stream, part);
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
    public XlsxThemePartWriteResult WriteStream(Stream stream, XlsxThemePart part)
    {
        XlsxThemePartWriteResult result = ValidateThemePart(part);
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
            writer.WriteStartElement("a", "theme", XlsxNamespaces.Drawing);
            if (part.Name != null && part.Name.Length > 0)
            {
                writer.WriteAttributeString("name", part.Name);
            }

            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            writer.WriteStartElement("a", "themeElements", XlsxNamespaces.Drawing);
            WriteColorScheme(writer, part);
            WriteFontScheme(writer, part);
            WriteFormatScheme(writer, part);
            writer.WriteEndElement();
            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxThemePartWriteResult ValidateThemePart(XlsxThemePart part)
    {
        XlsxThemePartWriteResult result = new XlsxThemePartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.partNull", "Theme part must not be null."));
            return result;
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.unknownRootAttributesNull", "Theme root unknown attributes must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.unknownXmlNull", "Theme unknown XML list must not be null."));
        }

        if (part.HasColorScheme && (part.ColorScheme == null || part.ColorScheme.Colors == null))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.colorSchemeInvalid", "Theme color scheme and colors must not be null."));
        }

        if (part.HasFontScheme && part.FontScheme == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.fontSchemeNull", "Theme font scheme must not be null."));
        }

        if (part.HasFormatScheme && part.FormatScheme == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.formatSchemeNull", "Theme format scheme must not be null."));
        }

        return result;
    }

    internal static void WriteColorScheme(XmlWriter writer, XlsxThemePart part)
    {
        if (!part.HasColorScheme || part.ColorScheme == null)
        {
            return;
        }

        if (part.ColorScheme.RawXml != null && part.ColorScheme.RawXml.Length > 0)
        {
            writer.WriteRaw(part.ColorScheme.RawXml);
            return;
        }

        writer.WriteStartElement("a", "clrScheme", XlsxNamespaces.Drawing);
        if (part.ColorScheme.Name != null && part.ColorScheme.Name.Length > 0)
        {
            writer.WriteAttributeString("name", part.ColorScheme.Name);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.ColorScheme.UnknownAttributes);
        foreach (XlsxThemeColor color in part.ColorScheme.Colors)
        {
            WriteThemeColor(writer, color);
        }

        WriteUnknownXml(writer, part.ColorScheme.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteThemeColor(XmlWriter writer, XlsxThemeColor color)
    {
        if (color == null)
        {
            return;
        }

        if (color.RawXml != null && color.RawXml.Length > 0)
        {
            writer.WriteRaw(color.RawXml);
            return;
        }

        string slot = color.Slot;
        if (slot == null || slot.Length == 0)
        {
            slot = "accent1";
        }

        string kind = color.Kind;
        if (kind == null || kind.Length == 0)
        {
            kind = "srgbClr";
        }

        writer.WriteStartElement("a", slot, XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", kind, XlsxNamespaces.Drawing);
        if (color.Value != null && color.Value.Length > 0)
        {
            writer.WriteAttributeString("val", color.Value);
        }

        if (color.LastColor != null && color.LastColor.Length > 0)
        {
            writer.WriteAttributeString("lastClr", color.LastColor);
        }

        WriteColorTransform(writer, "tint", color.Tint, color.HasTint);
        WriteColorTransform(writer, "shade", color.Shade, color.HasShade);
        WriteColorTransform(writer, "lumMod", color.LuminanceModulation, color.HasLuminanceModulation);
        WriteColorTransform(writer, "lumOff", color.LuminanceOffset, color.HasLuminanceOffset);
        writer.WriteEndElement();
        WriteUnknownXml(writer, color.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteColorTransform(XmlWriter writer, string name, uint value, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        writer.WriteStartElement("a", name, XlsxNamespaces.Drawing);
        writer.WriteAttributeString("val", XlsxXmlValueWriter.WriteUnsignedInteger(value));
        writer.WriteEndElement();
    }

    internal static void WriteFontScheme(XmlWriter writer, XlsxThemePart part)
    {
        if (!part.HasFontScheme || part.FontScheme == null)
        {
            return;
        }

        if (part.FontScheme.RawXml != null && part.FontScheme.RawXml.Length > 0)
        {
            writer.WriteRaw(part.FontScheme.RawXml);
            return;
        }

        writer.WriteStartElement("a", "fontScheme", XlsxNamespaces.Drawing);
        if (part.FontScheme.Name != null && part.FontScheme.Name.Length > 0)
        {
            writer.WriteAttributeString("name", part.FontScheme.Name);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.FontScheme.UnknownAttributes);
        if (part.FontScheme.HasMajorFont)
        {
            WriteFontSet(writer, "majorFont", part.FontScheme.MajorFont);
        }

        if (part.FontScheme.HasMinorFont)
        {
            WriteFontSet(writer, "minorFont", part.FontScheme.MinorFont);
        }

        WriteUnknownXml(writer, part.FontScheme.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteFontSet(XmlWriter writer, string elementName, XlsxThemeFontSet fontSet)
    {
        if (fontSet == null)
        {
            return;
        }

        if (fontSet.RawXml != null && fontSet.RawXml.Length > 0)
        {
            writer.WriteRaw(fontSet.RawXml);
            return;
        }

        writer.WriteStartElement("a", elementName, XlsxNamespaces.Drawing);
        WriteTypeface(writer, "latin", fontSet.LatinTypeface);
        WriteTypeface(writer, "ea", fontSet.EastAsianTypeface);
        WriteTypeface(writer, "cs", fontSet.ComplexScriptTypeface);
        WriteUnknownXml(writer, fontSet.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteTypeface(XmlWriter writer, string elementName, string? typeface)
    {
        writer.WriteStartElement("a", elementName, XlsxNamespaces.Drawing);
        writer.WriteAttributeString("typeface", NormalizeText(typeface));
        writer.WriteEndElement();
    }

    internal static void WriteFormatScheme(XmlWriter writer, XlsxThemePart part)
    {
        if (!part.HasFormatScheme || part.FormatScheme == null)
        {
            return;
        }

        if (part.FormatScheme.RawXml != null && part.FormatScheme.RawXml.Length > 0)
        {
            writer.WriteRaw(part.FormatScheme.RawXml);
            return;
        }

        writer.WriteStartElement("a", "fmtScheme", XlsxNamespaces.Drawing);
        if (part.FormatScheme.Name != null && part.FormatScheme.Name.Length > 0)
        {
            writer.WriteAttributeString("name", part.FormatScheme.Name);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.FormatScheme.UnknownAttributes);
        WriteEmptyList(writer, "fillStyleLst");
        WriteEmptyList(writer, "lnStyleLst");
        WriteEmptyList(writer, "effectStyleLst");
        WriteEmptyList(writer, "bgFillStyleLst");
        WriteUnknownXml(writer, part.FormatScheme.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteEmptyList(XmlWriter writer, string elementName)
    {
        writer.WriteStartElement("a", elementName, XlsxNamespaces.Drawing);
        writer.WriteEndElement();
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

    internal static XlsxThemePart CreateDefaultOfficeTheme()
    {
        XlsxThemePart part = new XlsxThemePart();
        part.Name = "Office Theme";
        part.HasColorScheme = true;
        part.ColorScheme.Name = "Office";
        AddSystemColor(part.ColorScheme, "dk1", "windowText", "000000");
        AddSystemColor(part.ColorScheme, "lt1", "window", "FFFFFF");
        AddRgbColor(part.ColorScheme, "dk2", "1F1F1F");
        AddRgbColor(part.ColorScheme, "lt2", "EEECE1");
        AddRgbColor(part.ColorScheme, "accent1", "4472C4");
        AddRgbColor(part.ColorScheme, "accent2", "ED7D31");
        AddRgbColor(part.ColorScheme, "accent3", "A5A5A5");
        AddRgbColor(part.ColorScheme, "accent4", "FFC000");
        AddRgbColor(part.ColorScheme, "accent5", "5B9BD5");
        AddRgbColor(part.ColorScheme, "accent6", "70AD47");
        AddRgbColor(part.ColorScheme, "hlink", "0563C1");
        AddRgbColor(part.ColorScheme, "folHlink", "954F72");
        part.HasFontScheme = true;
        part.FontScheme.Name = "Office";
        part.FontScheme.HasMajorFont = true;
        part.FontScheme.MajorFont.LatinTypeface = "Aptos Display";
        part.FontScheme.MajorFont.EastAsianTypeface = "";
        part.FontScheme.MajorFont.ComplexScriptTypeface = "";
        part.FontScheme.HasMinorFont = true;
        part.FontScheme.MinorFont.LatinTypeface = "Aptos";
        part.FontScheme.MinorFont.EastAsianTypeface = "";
        part.FontScheme.MinorFont.ComplexScriptTypeface = "";
        part.HasFormatScheme = true;
        part.FormatScheme.Name = "Office";
        part.FormatScheme.RawXml = CreateDefaultFormatSchemeXml();
        return part;
    }

    internal static void AddRgbColor(XlsxThemeColorScheme scheme, string slot, string rgb)
    {
        XlsxThemeColor color = new XlsxThemeColor();
        color.Slot = slot;
        color.Kind = "srgbClr";
        color.Value = rgb;
        scheme.Colors.Add(color);
    }

    internal static void AddSystemColor(XlsxThemeColorScheme scheme, string slot, string value, string lastColor)
    {
        XlsxThemeColor color = new XlsxThemeColor();
        color.Slot = slot;
        color.Kind = "sysClr";
        color.Value = value;
        color.LastColor = lastColor;
        scheme.Colors.Add(color);
    }

    internal static string CreateDefaultFormatSchemeXml()
    {
        return "<a:fmtScheme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Office\">" + "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\" /></a:solidFill><a:gradFill rotWithShape=\"1\"><a:gsLst><a:gs pos=\"0\"><a:schemeClr val=\"phClr\"><a:lumMod val=\"110000\" /><a:satMod val=\"105000\" /></a:schemeClr></a:gs><a:gs pos=\"100000\"><a:schemeClr val=\"phClr\"><a:lumMod val=\"105000\" /><a:satMod val=\"103000\" /></a:schemeClr></a:gs></a:gsLst><a:lin ang=\"5400000\" scaled=\"0\" /></a:gradFill></a:fillStyleLst>" + "<a:lnStyleLst><a:ln w=\"6350\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\" /></a:solidFill><a:prstDash val=\"solid\" /></a:ln></a:lnStyleLst>" + "<a:effectStyleLst><a:effectStyle><a:effectLst /></a:effectStyle></a:effectStyleLst>" + "<a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"phClr\" /></a:solidFill></a:bgFillStyleLst>" + "</a:fmtScheme>";
    }

    internal static string NormalizeText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }
}
