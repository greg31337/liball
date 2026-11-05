using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads theme part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxThemePartReader : IXlsxThemePartReader
{
    /// <summary>
    /// Initializes a new theme part reader with its default state.
    /// </summary>
    public XlsxThemePartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The theme part produced by the operation.</returns>
    public XlsxThemePart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxThemePart part = ReadBytes(bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The theme part produced by the operation.</returns>
    public XlsxThemePart ReadBytes(byte[] bytes)
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
    /// <returns>The theme part produced by the operation.</returns>
    public XlsxThemePart ReadStream(Stream stream)
    {
        XlsxThemePart part = new XlsxThemePart();
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

                if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI) && reader.LocalName == "themeElements")
                {
                    ReadThemeElements(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.missingRoot", "Theme XML must contain a theme root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxThemePart CreateMissingRootResult()
    {
        XlsxThemePart part = new XlsxThemePart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.missingRoot", "Theme XML must contain a theme root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxThemePart part, XmlReader reader)
    {
        if (reader.LocalName != "theme" || !IsDrawingNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "theme.invalidRoot", "Theme XML root must be a DrawingML theme element.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        part.Name = reader.GetAttribute("name");
        XlsxExtensionListReader.ReadUnknownAttributes(reader, part.UnknownRootAttributes, "name");
    }

    internal static void ReadThemeElements(XlsxThemePart part, XmlReader reader)
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "clrScheme")
                {
                    part.ColorScheme = ReadColorSchemeString(part, reader.ReadOuterXml());
                    part.HasColorScheme = true;
                    continue;
                }

                if (reader.LocalName == "fontScheme")
                {
                    part.FontScheme = ReadFontSchemeString(reader.ReadOuterXml());
                    part.HasFontScheme = true;
                    continue;
                }

                if (reader.LocalName == "fmtScheme")
                {
                    part.FormatScheme = ReadFormatSchemeString(reader.ReadOuterXml());
                    part.HasFormatScheme = true;
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxThemeColorScheme ReadColorSchemeString(XlsxThemePart part, string xml)
    {
        XlsxThemeColorScheme scheme = new XlsxThemeColorScheme();
        scheme.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                ReadColorScheme(part, scheme, reader);
            }
        }

        return scheme;
    }

    internal static void ReadColorScheme(XlsxThemePart part, XlsxThemeColorScheme scheme, XmlReader reader)
    {
        bool sawRoot = false;
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
                scheme.Name = reader.GetAttribute("name");
                XlsxExtensionListReader.ReadUnknownAttributes(reader, scheme.UnknownAttributes, "name");
                reader.Read();
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                scheme.Colors.Add(ReadThemeColor(part, reader));
                continue;
            }

            scheme.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static XlsxThemeColor ReadThemeColor(XlsxThemePart part, XmlReader reader)
    {
        XlsxThemeColor color = new XlsxThemeColor();
        color.Slot = reader.LocalName;
        color.RawXml = reader.ReadOuterXml();
        byte[] bytes = Encoding.UTF8.GetBytes(color.RawXml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader colorReader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                ReadThemeColorRaw(part, color, colorReader);
            }
        }

        return color;
    }

    internal static void ReadThemeColorRaw(XlsxThemePart part, XlsxThemeColor color, XmlReader reader)
    {
        bool sawRoot = false;
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
                XlsxExtensionListReader.ReadUnknownAttributes(reader, color.UnknownAttributes, "");
                reader.Read();
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                color.Kind = reader.LocalName;
                color.Value = reader.GetAttribute("val");
                color.LastColor = reader.GetAttribute("lastClr");
                ReadColorTransforms(part, color, reader);
                continue;
            }

            color.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadColorTransforms(XlsxThemePart part, XlsxThemeColor color, XmlReader reader)
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                SetColorTransform(part, color, reader.LocalName, reader.GetAttribute("val"));
                reader.ReadOuterXml();
                continue;
            }

            reader.Read();
        }
    }

    internal static void SetColorTransform(XlsxThemePart part, XlsxThemeColor color, string name, string? value)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(value);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, name, "val");
            return;
        }

        if (name == "tint")
        {
            color.Tint = result.UnsignedIntValue;
            color.HasTint = true;
        }
        else if (name == "shade")
        {
            color.Shade = result.UnsignedIntValue;
            color.HasShade = true;
        }
        else if (name == "lumMod")
        {
            color.LuminanceModulation = result.UnsignedIntValue;
            color.HasLuminanceModulation = true;
        }
        else if (name == "lumOff")
        {
            color.LuminanceOffset = result.UnsignedIntValue;
            color.HasLuminanceOffset = true;
        }
    }

    internal static XlsxThemeFontScheme ReadFontSchemeString(string xml)
    {
        XlsxThemeFontScheme scheme = new XlsxThemeFontScheme();
        scheme.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                ReadFontScheme(scheme, reader);
            }
        }

        return scheme;
    }

    internal static void ReadFontScheme(XlsxThemeFontScheme scheme, XmlReader reader)
    {
        bool sawRoot = false;
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
                scheme.Name = reader.GetAttribute("name");
                XlsxExtensionListReader.ReadUnknownAttributes(reader, scheme.UnknownAttributes, "name");
                reader.Read();
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI) && reader.LocalName == "majorFont")
            {
                scheme.MajorFont = ReadFontSet(reader.ReadOuterXml());
                scheme.HasMajorFont = true;
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI) && reader.LocalName == "minorFont")
            {
                scheme.MinorFont = ReadFontSet(reader.ReadOuterXml());
                scheme.HasMinorFont = true;
                continue;
            }

            scheme.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static XlsxThemeFontSet ReadFontSet(string xml)
    {
        XlsxThemeFontSet set = new XlsxThemeFontSet();
        set.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                ReadFontSetElements(set, reader);
            }
        }

        return set;
    }

    internal static void ReadFontSetElements(XlsxThemeFontSet set, XmlReader reader)
    {
        bool sawRoot = false;
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
                reader.Read();
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "latin")
                {
                    set.LatinTypeface = reader.GetAttribute("typeface");
                    reader.ReadOuterXml();
                    continue;
                }

                if (reader.LocalName == "ea")
                {
                    set.EastAsianTypeface = reader.GetAttribute("typeface");
                    reader.ReadOuterXml();
                    continue;
                }

                if (reader.LocalName == "cs")
                {
                    set.ComplexScriptTypeface = reader.GetAttribute("typeface");
                    reader.ReadOuterXml();
                    continue;
                }
            }

            set.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static XlsxThemeFormatScheme ReadFormatSchemeString(string xml)
    {
        XlsxThemeFormatScheme scheme = new XlsxThemeFormatScheme();
        scheme.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                ReadFormatScheme(scheme, reader);
            }
        }

        return scheme;
    }

    internal static void ReadFormatScheme(XlsxThemeFormatScheme scheme, XmlReader reader)
    {
        bool sawRoot = false;
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
                scheme.Name = reader.GetAttribute("name");
                XlsxExtensionListReader.ReadUnknownAttributes(reader, scheme.UnknownAttributes, "name");
                reader.Read();
                continue;
            }

            if (reader.Depth == 1 && IsDrawingNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "fillStyleLst")
                {
                    scheme.FillStyleCount = CountDirectChildElements(reader.ReadOuterXml());
                    continue;
                }

                if (reader.LocalName == "lnStyleLst")
                {
                    scheme.LineStyleCount = CountDirectChildElements(reader.ReadOuterXml());
                    continue;
                }

                if (reader.LocalName == "effectStyleLst")
                {
                    scheme.EffectStyleCount = CountDirectChildElements(reader.ReadOuterXml());
                    continue;
                }

                if (reader.LocalName == "bgFillStyleLst")
                {
                    scheme.BackgroundFillStyleCount = CountDirectChildElements(reader.ReadOuterXml());
                    continue;
                }
            }

            scheme.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static int CountDirectChildElements(string xml)
    {
        int count = 0;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        count = count + 1;
                    }

                    reader.Read();
                }
            }
        }

        return count;
    }

    internal static void AddValueDiagnostics(XlsxThemePart part, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsDrawingNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Drawing;
    }
}
