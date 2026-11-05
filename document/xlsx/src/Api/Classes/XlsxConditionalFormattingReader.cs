using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads conditional formatting data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxConditionalFormattingReader : IXlsxConditionalFormattingReader
{
    /// <summary>
    /// Initializes a new conditional formatting reader with its default state.
    /// </summary>
    public XlsxConditionalFormattingReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The conditional formatting produced by the operation.</returns>
    public XlsxConditionalFormatting ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxConditionalFormatting formatting = ReadBytes(bytes);
        formatting.RawXml = xml;
        return formatting;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The conditional formatting produced by the operation.</returns>
    public XlsxConditionalFormatting ReadBytes(byte[] bytes)
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
    /// <returns>The conditional formatting produced by the operation.</returns>
    public XlsxConditionalFormatting ReadStream(Stream stream)
    {
        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
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
                    if (reader.LocalName == "conditionalFormatting" && IsSpreadsheetNamespace(reader.NamespaceURI))
                    {
                        ReadConditionalFormattingRoot(formatting, reader);
                        continue;
                    }

                    formatting.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormatting.invalidRoot", "Conditional formatting XML root must be a conditionalFormatting element."));
                    reader.ReadOuterXml();
                    continue;
                }

                formatting.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            formatting.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormatting.missingRoot", "Conditional formatting XML must contain a conditionalFormatting root element."));
        }

        return formatting;
    }

    internal static XlsxConditionalFormatting CreateMissingRootResult()
    {
        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
        formatting.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormatting.missingRoot", "Conditional formatting XML must contain a conditionalFormatting root element."));
        return formatting;
    }

    internal static void ReadConditionalFormattingRoot(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        formatting.NamespaceUri = reader.NamespaceURI;
        formatting.Sqref = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("sqref"));
        SetOptionalBool(formatting, reader.GetAttribute("pivot"), "conditionalFormatting", "pivot", formatting, 1);
        ValidateSqref(formatting, formatting.Sqref, "conditionalFormatting", "sqref");
        ReadUnknownAttributes(reader, formatting.UnknownAttributes, "conditionalFormatting");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "cfRule")
                {
                    formatting.Rules.Add(ReadRule(formatting, reader));
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    formatting.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                formatting.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxConditionalFormatRule ReadRule(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatRule rule = new XlsxConditionalFormatRule();
        rule.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
        rule.Operator = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("operator"));
        rule.Text = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("text"));
        rule.TimePeriod = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("timePeriod"));
        rule.Id = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("id"));
        SetOptionalUInt(formatting, reader.GetAttribute("priority"), "cfRule", "priority", rule, 1);
        SetOptionalUInt(formatting, reader.GetAttribute("dxfId"), "cfRule", "dxfId", rule, 2);
        SetOptionalUInt(formatting, reader.GetAttribute("rank"), "cfRule", "rank", rule, 3);
        SetOptionalUInt(formatting, reader.GetAttribute("stdDev"), "cfRule", "stdDev", rule, 4);
        SetOptionalBool(formatting, reader.GetAttribute("stopIfTrue"), "cfRule", "stopIfTrue", rule, 1);
        SetOptionalBool(formatting, reader.GetAttribute("aboveAverage"), "cfRule", "aboveAverage", rule, 2);
        SetOptionalBool(formatting, reader.GetAttribute("percent"), "cfRule", "percent", rule, 3);
        SetOptionalBool(formatting, reader.GetAttribute("bottom"), "cfRule", "bottom", rule, 4);
        SetOptionalBool(formatting, reader.GetAttribute("equalAverage"), "cfRule", "equalAverage", rule, 5);
        ReadUnknownAttributes(reader, rule.UnknownAttributes, "cfRule");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return rule;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return rule;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "formula")
                {
                    string rawXml = reader.ReadOuterXml();
                    rule.FormulaRawXml.Add(rawXml);
                    rule.Formulas.Add(ReadElementText(rawXml));
                    continue;
                }

                if (reader.LocalName == "colorScale")
                {
                    rule.ColorScale = ReadColorScale(formatting, reader);
                    rule.HasColorScale = true;
                    continue;
                }

                if (reader.LocalName == "dataBar")
                {
                    rule.DataBar = ReadDataBar(formatting, reader);
                    rule.HasDataBar = true;
                    continue;
                }

                if (reader.LocalName == "iconSet")
                {
                    rule.IconSet = ReadIconSet(formatting, reader);
                    rule.HasIconSet = true;
                    continue;
                }

                if (reader.LocalName == "dxf")
                {
                    rule.DifferentialStyleRawXml = reader.ReadOuterXml();
                    continue;
                }

                if (reader.LocalName == "extLst")
                {
                    rule.ExtensionListRawXml = reader.ReadOuterXml();
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                rule.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return rule;
    }

    internal static XlsxConditionalFormatColorScale ReadColorScale(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatColorScale colorScale = new XlsxConditionalFormatColorScale();
        ReadUnknownAttributes(reader, colorScale.UnknownAttributes, "colorScale");
        ReadThresholdColorChildren(formatting, reader, colorScale.Values, colorScale.Colors, colorScale.UnknownXml);
        return colorScale;
    }

    internal static XlsxConditionalFormatDataBar ReadDataBar(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatDataBar dataBar = new XlsxConditionalFormatDataBar();
        SetOptionalUInt(formatting, reader.GetAttribute("minLength"), "dataBar", "minLength", dataBar, 1);
        SetOptionalUInt(formatting, reader.GetAttribute("maxLength"), "dataBar", "maxLength", dataBar, 2);
        SetOptionalBool(formatting, reader.GetAttribute("showValue"), "dataBar", "showValue", dataBar, 1);
        ReadUnknownAttributes(reader, dataBar.UnknownAttributes, "dataBar");
        ReadThresholdColorChildren(formatting, reader, dataBar.Values, dataBar.Colors, dataBar.UnknownXml);
        return dataBar;
    }

    internal static XlsxConditionalFormatIconSet ReadIconSet(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatIconSet iconSet = new XlsxConditionalFormatIconSet();
        iconSet.IconSet = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("iconSet"));
        SetOptionalBool(formatting, reader.GetAttribute("showValue"), "iconSet", "showValue", iconSet, 1);
        SetOptionalBool(formatting, reader.GetAttribute("percent"), "iconSet", "percent", iconSet, 2);
        SetOptionalBool(formatting, reader.GetAttribute("reverse"), "iconSet", "reverse", iconSet, 3);
        SetOptionalBool(formatting, reader.GetAttribute("custom"), "iconSet", "custom", iconSet, 4);
        ReadUnknownAttributes(reader, iconSet.UnknownAttributes, "iconSet");
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return iconSet;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return iconSet;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI) && reader.LocalName == "cfvo")
            {
                iconSet.Values.Add(ReadValueObject(formatting, reader));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                iconSet.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return iconSet;
    }

    internal static void ReadThresholdColorChildren(XlsxConditionalFormatting formatting, XmlReader reader, List<XlsxConditionalFormatValueObject> values, List<XlsxConditionalFormatColor> colors, List<string> unknownXml)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsSpreadsheetNamespace(reader.NamespaceURI))
            {
                if (reader.LocalName == "cfvo")
                {
                    values.Add(ReadValueObject(formatting, reader));
                    continue;
                }

                if (reader.LocalName == "color")
                {
                    colors.Add(ReadColor(formatting, reader));
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                unknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxConditionalFormatValueObject ReadValueObject(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatValueObject value = new XlsxConditionalFormatValueObject();
        value.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
        value.Value = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
        SetOptionalBool(formatting, reader.GetAttribute("gte"), "cfvo", "gte", value, 1);
        ReadUnknownAttributes(reader, value.UnknownAttributes, "cfvo");
        reader.ReadOuterXml();
        return value;
    }

    internal static XlsxConditionalFormatColor ReadColor(XlsxConditionalFormatting formatting, XmlReader reader)
    {
        XlsxConditionalFormatColor color = new XlsxConditionalFormatColor();
        color.RawXml = reader.ReadOuterXml();
        using (StringReader stringReader = new StringReader(color.RawXml))
        {
            using (XmlReader colorReader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!colorReader.EOF)
                {
                    if (colorReader.NodeType == XmlNodeType.Element)
                    {
                        color.Rgb = XlsxCellValueConverter.NormalizeText(colorReader.GetAttribute("rgb"));
                        SetOptionalUInt(formatting, colorReader.GetAttribute("indexed"), "color", "indexed", color, 1);
                        SetOptionalUInt(formatting, colorReader.GetAttribute("theme"), "color", "theme", color, 2);
                        SetOptionalDouble(formatting, colorReader.GetAttribute("tint"), "color", "tint", color, 1);
                        SetOptionalBool(formatting, colorReader.GetAttribute("auto"), "color", "auto", color, 1);
                        ReadUnknownAttributes(colorReader, color.UnknownAttributes, "color");
                        return color;
                    }

                    colorReader.Read();
                }
            }
        }

        return color;
    }

    internal static string ReadElementText(string rawXml)
    {
        if (rawXml == null || rawXml.Length == 0)
        {
            return "";
        }

        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.ReadElementContentAsString();
                    }

                    reader.Read();
                }
            }
        }

        return "";
    }

    internal static void SetOptionalUInt(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatRule rule, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            rule.Priority = result.UnsignedIntValue;
            rule.HasPriority = true;
        }
        else if (target == 2)
        {
            rule.DxfId = result.UnsignedIntValue;
            rule.HasDxfId = true;
        }
        else if (target == 3)
        {
            rule.Rank = result.UnsignedIntValue;
            rule.HasRank = true;
        }
        else if (target == 4)
        {
            rule.StdDev = result.UnsignedIntValue;
            rule.HasStdDev = true;
        }
    }

    internal static void SetOptionalUInt(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatDataBar dataBar, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            dataBar.MinLength = result.UnsignedIntValue;
            dataBar.HasMinLength = true;
        }
        else if (target == 2)
        {
            dataBar.MaxLength = result.UnsignedIntValue;
            dataBar.HasMaxLength = true;
        }
    }

    internal static void SetOptionalUInt(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatColor color, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            color.Indexed = result.UnsignedIntValue;
            color.HasIndexed = true;
        }
        else if (target == 2)
        {
            color.Theme = result.UnsignedIntValue;
            color.HasTheme = true;
        }
    }

    internal static void SetOptionalDouble(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatColor color, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        color.Tint = result.DoubleValue;
        color.HasTint = true;
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatting root, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        root.Pivot = result.BooleanValue;
        root.HasPivot = true;
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatRule rule, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            rule.StopIfTrue = result.BooleanValue;
            rule.HasStopIfTrue = true;
        }
        else if (target == 2)
        {
            rule.AboveAverage = result.BooleanValue;
            rule.HasAboveAverage = true;
        }
        else if (target == 3)
        {
            rule.Percent = result.BooleanValue;
            rule.HasPercent = true;
        }
        else if (target == 4)
        {
            rule.Bottom = result.BooleanValue;
            rule.HasBottom = true;
        }
        else if (target == 5)
        {
            rule.EqualAverage = result.BooleanValue;
            rule.HasEqualAverage = true;
        }
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatDataBar dataBar, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        dataBar.ShowValue = result.BooleanValue;
        dataBar.HasShowValue = true;
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatIconSet iconSet, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        if (target == 1)
        {
            iconSet.ShowValue = result.BooleanValue;
            iconSet.HasShowValue = true;
        }
        else if (target == 2)
        {
            iconSet.Percent = result.BooleanValue;
            iconSet.HasPercent = true;
        }
        else if (target == 3)
        {
            iconSet.Reverse = result.BooleanValue;
            iconSet.HasReverse = true;
        }
        else if (target == 4)
        {
            iconSet.Custom = result.BooleanValue;
            iconSet.HasCustom = true;
        }
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatValueObject value, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        value.GreaterThanOrEqual = result.BooleanValue;
        value.HasGreaterThanOrEqual = true;
    }

    internal static void SetOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName, XlsxConditionalFormatColor color, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(formatting, text, elementName, attributeName);
        if (text == null || text.Length == 0 || !result.IsValid)
        {
            return;
        }

        color.Auto = result.BooleanValue;
        color.HasAuto = true;
    }

    internal static XlsxXmlValueParseResult ParseOptionalUInt(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddValueDiagnostics(formatting, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalDouble(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseDouble(text);
        AddValueDiagnostics(formatting, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalBool(XlsxConditionalFormatting formatting, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseBoolean(text);
        AddValueDiagnostics(formatting, result, elementName, attributeName);
        return result;
    }

    internal static void AddValueDiagnostics(XlsxConditionalFormatting formatting, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", elementName, attributeName);
            formatting.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateSqref(XlsxConditionalFormatting formatting, string sqref, string elementName, string attributeName)
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

            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(trimmed);
            foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics)
            {
                diagnostic.SetLocation("", elementName, attributeName);
                formatting.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string elementName)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool known = reader.NamespaceURI == "" && IsKnownAttribute(elementName, reader.LocalName);
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownAttribute(string elementName, string localName)
    {
        if (elementName == "conditionalFormatting")
        {
            return localName == "sqref" || localName == "pivot";
        }

        if (elementName == "cfRule")
        {
            return localName == "type" || localName == "priority" || localName == "dxfId" || localName == "stopIfTrue" || localName == "aboveAverage" || localName == "percent" || localName == "bottom" || localName == "equalAverage" || localName == "operator" || localName == "text" || localName == "timePeriod" || localName == "rank" || localName == "stdDev" || localName == "id";
        }

        if (elementName == "colorScale")
        {
            return false;
        }

        if (elementName == "dataBar")
        {
            return localName == "minLength" || localName == "maxLength" || localName == "showValue";
        }

        if (elementName == "iconSet")
        {
            return localName == "iconSet" || localName == "showValue" || localName == "percent" || localName == "reverse" || localName == "custom";
        }

        if (elementName == "cfvo")
        {
            return localName == "type" || localName == "val" || localName == "gte";
        }

        if (elementName == "color")
        {
            return localName == "rgb" || localName == "indexed" || localName == "theme" || localName == "tint" || localName == "auto";
        }

        return false;
    }

    internal static bool IsSpreadsheetNamespace(string namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.Spreadsheet || namespaceUri == XlsxNamespaces.StrictSpreadsheet;
    }
}
