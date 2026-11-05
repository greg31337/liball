using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes conditional formatting data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxConditionalFormattingWriter : IXlsxConditionalFormattingWriter
{
    /// <summary>
    /// Initializes a new conditional formatting writer with its default state.
    /// </summary>
    public XlsxConditionalFormattingWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "formatting">The formatting value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxConditionalFormattingWriteResult WriteString(XlsxConditionalFormatting formatting)
    {
        XlsxConditionalFormattingWriteResult result = WriteBytes(formatting);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "formatting">The formatting value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxConditionalFormattingWriteResult WriteBytes(XlsxConditionalFormatting formatting)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxConditionalFormattingWriteResult result = WriteStream(stream, formatting);
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
    /// <param name = "formatting">The formatting value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxConditionalFormattingWriteResult WriteStream(Stream stream, XlsxConditionalFormatting formatting)
    {
        XlsxConditionalFormattingWriteResult result = ValidateConditionalFormatting(formatting);
        if (!result.IsValid)
        {
            return result;
        }

        if (formatting.RawXml != null && formatting.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(formatting.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteConditionalFormattingElement(writer, formatting);
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxConditionalFormattingWriteResult ValidateConditionalFormatting(XlsxConditionalFormatting formatting)
    {
        XlsxConditionalFormattingWriteResult result = new XlsxConditionalFormattingWriteResult();
        if (formatting == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.formattingNull", "Conditional formatting must not be null."));
            return result;
        }

        if (formatting.RawXml != null && formatting.RawXml.Length > 0)
        {
            return result;
        }

        if (formatting.NamespaceUri == null || formatting.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.namespaceMissing", "Conditional formatting must have a namespace URI."));
        }
        else if (!XlsxConditionalFormattingReader.IsSpreadsheetNamespace(formatting.NamespaceUri))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.namespaceInvalid", "Conditional formatting namespace URI must be a SpreadsheetML namespace."));
        }

        if (formatting.Sqref == null || formatting.Sqref.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.sqrefMissing", "Conditional formatting must have a sqref attribute."));
        }
        else
        {
            ValidateSqref(result, formatting.Sqref, "conditionalFormatting", "sqref");
        }

        if (formatting.Rules == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.rulesNull", "Conditional formatting rule list must not be null."));
        }
        else
        {
            int index = 0;
            foreach (XlsxConditionalFormatRule rule in formatting.Rules)
            {
                ValidateRule(result, rule, index);
                index = index + 1;
            }
        }

        if (formatting.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.unknownAttributesNull", "Conditional formatting unknown attribute list must not be null."));
        }

        if (formatting.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.unknownXmlNull", "Conditional formatting unknown XML list must not be null."));
        }

        return result;
    }

    internal static void ValidateRule(XlsxConditionalFormattingWriteResult result, XlsxConditionalFormatRule rule, int index)
    {
        if (rule == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.ruleNull", "Conditional formatting rule list must not contain null entries.");
            diagnostic.SetLocation("", "cfRule", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (rule.Type == null || rule.Type.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.ruleTypeMissing", "Conditional formatting rule must have a type attribute.");
            diagnostic.SetLocation("", "cfRule", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (!rule.HasPriority)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.priorityMissing", "Conditional formatting rule must have a priority attribute.");
            diagnostic.SetLocation("", "cfRule", index.ToString(CultureInfo.InvariantCulture));
            result.AddDiagnostic(diagnostic);
        }

        if (rule.Formulas == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.formulasNull", "Conditional formatting rule formula list must not be null."));
        }

        if (rule.FormulaRawXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.formulaRawXmlNull", "Conditional formatting rule raw formula list must not be null."));
        }

        if (rule.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.ruleUnknownAttributesNull", "Conditional formatting rule unknown attribute list must not be null."));
        }

        if (rule.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.ruleUnknownXmlNull", "Conditional formatting rule unknown XML list must not be null."));
        }

        if (rule.HasColorScale)
        {
            ValidateColorScale(result, rule.ColorScale);
        }

        if (rule.HasDataBar)
        {
            ValidateDataBar(result, rule.DataBar);
        }

        if (rule.HasIconSet)
        {
            ValidateIconSet(result, rule.IconSet);
        }
    }

    internal static void ValidateColorScale(XlsxConditionalFormattingWriteResult result, XlsxConditionalFormatColorScale colorScale)
    {
        if (colorScale == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.colorScaleNull", "Conditional formatting color scale must not be null when HasColorScale is true."));
            return;
        }

        ValidateValueObjects(result, colorScale.Values, "conditionalFormattingWriter.colorScaleValuesNull");
        ValidateColors(result, colorScale.Colors, "conditionalFormattingWriter.colorScaleColorsNull");
        if (colorScale.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.colorScaleUnknownAttributesNull", "Conditional formatting color scale unknown attribute list must not be null."));
        }

        if (colorScale.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.colorScaleUnknownXmlNull", "Conditional formatting color scale unknown XML list must not be null."));
        }
    }

    internal static void ValidateDataBar(XlsxConditionalFormattingWriteResult result, XlsxConditionalFormatDataBar dataBar)
    {
        if (dataBar == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.dataBarNull", "Conditional formatting data bar must not be null when HasDataBar is true."));
            return;
        }

        ValidateValueObjects(result, dataBar.Values, "conditionalFormattingWriter.dataBarValuesNull");
        ValidateColors(result, dataBar.Colors, "conditionalFormattingWriter.dataBarColorsNull");
        if (dataBar.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.dataBarUnknownAttributesNull", "Conditional formatting data bar unknown attribute list must not be null."));
        }

        if (dataBar.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.dataBarUnknownXmlNull", "Conditional formatting data bar unknown XML list must not be null."));
        }
    }

    internal static void ValidateIconSet(XlsxConditionalFormattingWriteResult result, XlsxConditionalFormatIconSet iconSet)
    {
        if (iconSet == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.iconSetNull", "Conditional formatting icon set must not be null when HasIconSet is true."));
            return;
        }

        ValidateValueObjects(result, iconSet.Values, "conditionalFormattingWriter.iconSetValuesNull");
        if (iconSet.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.iconSetUnknownAttributesNull", "Conditional formatting icon set unknown attribute list must not be null."));
        }

        if (iconSet.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.iconSetUnknownXmlNull", "Conditional formatting icon set unknown XML list must not be null."));
        }
    }

    internal static void ValidateValueObjects(XlsxConditionalFormattingWriteResult result, List<XlsxConditionalFormatValueObject> values, string nullCode)
    {
        if (values == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, nullCode, "Conditional formatting value object list must not be null."));
            return;
        }

        foreach (XlsxConditionalFormatValueObject value in values)
        {
            if (value == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.valueObjectNull", "Conditional formatting value object list must not contain null entries."));
            }
            else if (value.UnknownAttributes == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.valueObjectUnknownAttributesNull", "Conditional formatting value object unknown attribute list must not be null."));
            }
        }
    }

    internal static void ValidateColors(XlsxConditionalFormattingWriteResult result, List<XlsxConditionalFormatColor> colors, string nullCode)
    {
        if (colors == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, nullCode, "Conditional formatting color list must not be null."));
            return;
        }

        foreach (XlsxConditionalFormatColor color in colors)
        {
            if (color == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.colorNull", "Conditional formatting color list must not contain null entries."));
            }
            else if (color.UnknownAttributes == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "conditionalFormattingWriter.colorUnknownAttributesNull", "Conditional formatting color unknown attribute list must not be null."));
            }
        }
    }

    internal static void ValidateSqref(XlsxConditionalFormattingWriteResult result, string sqref, string elementName, string attributeName)
    {
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
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static void WriteConditionalFormattingElement(XmlWriter writer, XlsxConditionalFormatting formatting)
    {
        if (formatting.RawXml != null && formatting.RawXml.Length > 0)
        {
            writer.WriteRaw(formatting.RawXml);
            return;
        }

        writer.WriteStartElement("", "conditionalFormatting", formatting.NamespaceUri);
        WriteOptionalStringAttribute(writer, "sqref", formatting.Sqref);
        WriteOptionalBoolAttribute(writer, "pivot", formatting.Pivot, formatting.HasPivot);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, formatting.UnknownAttributes);
        foreach (XlsxConditionalFormatRule rule in formatting.Rules)
        {
            WriteRuleElement(writer, formatting.NamespaceUri, rule);
        }

        WriteRawXml(writer, formatting.ExtensionListRawXml);
        WriteUnknownXml(writer, formatting.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteRuleElement(XmlWriter writer, string namespaceUri, XlsxConditionalFormatRule rule)
    {
        writer.WriteStartElement("", "cfRule", namespaceUri);
        WriteOptionalStringAttribute(writer, "type", rule.Type);
        WriteOptionalUIntAttribute(writer, "priority", rule.Priority, rule.HasPriority);
        WriteOptionalUIntAttribute(writer, "dxfId", rule.DxfId, rule.HasDxfId);
        WriteOptionalBoolAttribute(writer, "stopIfTrue", rule.StopIfTrue, rule.HasStopIfTrue);
        WriteOptionalBoolAttribute(writer, "aboveAverage", rule.AboveAverage, rule.HasAboveAverage);
        WriteOptionalBoolAttribute(writer, "percent", rule.Percent, rule.HasPercent);
        WriteOptionalBoolAttribute(writer, "bottom", rule.Bottom, rule.HasBottom);
        WriteOptionalBoolAttribute(writer, "equalAverage", rule.EqualAverage, rule.HasEqualAverage);
        WriteOptionalStringAttribute(writer, "operator", rule.Operator);
        WriteOptionalStringAttribute(writer, "text", rule.Text);
        WriteOptionalStringAttribute(writer, "timePeriod", rule.TimePeriod);
        WriteOptionalUIntAttribute(writer, "rank", rule.Rank, rule.HasRank);
        WriteOptionalUIntAttribute(writer, "stdDev", rule.StdDev, rule.HasStdDev);
        WriteOptionalStringAttribute(writer, "id", rule.Id);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, rule.UnknownAttributes);
        WriteFormulaElements(writer, namespaceUri, rule);
        if (rule.HasColorScale)
        {
            WriteColorScaleElement(writer, namespaceUri, rule.ColorScale);
        }

        if (rule.HasDataBar)
        {
            WriteDataBarElement(writer, namespaceUri, rule.DataBar);
        }

        if (rule.HasIconSet)
        {
            WriteIconSetElement(writer, namespaceUri, rule.IconSet);
        }

        WriteRawXml(writer, rule.DifferentialStyleRawXml);
        WriteRawXml(writer, rule.ExtensionListRawXml);
        WriteUnknownXml(writer, rule.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteFormulaElements(XmlWriter writer, string namespaceUri, XlsxConditionalFormatRule rule)
    {
        if (rule.FormulaRawXml != null && rule.FormulaRawXml.Count > 0)
        {
            foreach (string rawXml in rule.FormulaRawXml)
            {
                WriteRawXml(writer, rawXml);
            }

            return;
        }

        foreach (string formula in rule.Formulas)
        {
            writer.WriteStartElement("", "formula", namespaceUri);
            writer.WriteString(formula);
            writer.WriteEndElement();
        }
    }

    internal static void WriteColorScaleElement(XmlWriter writer, string namespaceUri, XlsxConditionalFormatColorScale colorScale)
    {
        writer.WriteStartElement("", "colorScale", namespaceUri);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, colorScale.UnknownAttributes);
        WriteValueObjects(writer, namespaceUri, colorScale.Values);
        WriteColors(writer, namespaceUri, colorScale.Colors);
        WriteUnknownXml(writer, colorScale.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteDataBarElement(XmlWriter writer, string namespaceUri, XlsxConditionalFormatDataBar dataBar)
    {
        writer.WriteStartElement("", "dataBar", namespaceUri);
        WriteOptionalUIntAttribute(writer, "minLength", dataBar.MinLength, dataBar.HasMinLength);
        WriteOptionalUIntAttribute(writer, "maxLength", dataBar.MaxLength, dataBar.HasMaxLength);
        WriteOptionalBoolAttribute(writer, "showValue", dataBar.ShowValue, dataBar.HasShowValue);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, dataBar.UnknownAttributes);
        WriteValueObjects(writer, namespaceUri, dataBar.Values);
        WriteColors(writer, namespaceUri, dataBar.Colors);
        WriteUnknownXml(writer, dataBar.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteIconSetElement(XmlWriter writer, string namespaceUri, XlsxConditionalFormatIconSet iconSet)
    {
        writer.WriteStartElement("", "iconSet", namespaceUri);
        WriteOptionalStringAttribute(writer, "iconSet", iconSet.IconSet);
        WriteOptionalBoolAttribute(writer, "showValue", iconSet.ShowValue, iconSet.HasShowValue);
        WriteOptionalBoolAttribute(writer, "percent", iconSet.Percent, iconSet.HasPercent);
        WriteOptionalBoolAttribute(writer, "reverse", iconSet.Reverse, iconSet.HasReverse);
        WriteOptionalBoolAttribute(writer, "custom", iconSet.Custom, iconSet.HasCustom);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, iconSet.UnknownAttributes);
        WriteValueObjects(writer, namespaceUri, iconSet.Values);
        WriteUnknownXml(writer, iconSet.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteValueObjects(XmlWriter writer, string namespaceUri, List<XlsxConditionalFormatValueObject> values)
    {
        foreach (XlsxConditionalFormatValueObject value in values)
        {
            writer.WriteStartElement("", "cfvo", namespaceUri);
            WriteOptionalStringAttribute(writer, "type", value.Type);
            WriteOptionalStringAttribute(writer, "val", value.Value);
            WriteOptionalBoolAttribute(writer, "gte", value.GreaterThanOrEqual, value.HasGreaterThanOrEqual);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, value.UnknownAttributes);
            writer.WriteEndElement();
        }
    }

    internal static void WriteColors(XmlWriter writer, string namespaceUri, List<XlsxConditionalFormatColor> colors)
    {
        foreach (XlsxConditionalFormatColor color in colors)
        {
            WriteColorElement(writer, namespaceUri, color);
        }
    }

    internal static void WriteColorElement(XmlWriter writer, string namespaceUri, XlsxConditionalFormatColor color)
    {
        if (color.RawXml != null && color.RawXml.Length > 0)
        {
            writer.WriteRaw(color.RawXml);
            return;
        }

        writer.WriteStartElement("", "color", namespaceUri);
        WriteOptionalStringAttribute(writer, "rgb", color.Rgb);
        WriteOptionalUIntAttribute(writer, "indexed", color.Indexed, color.HasIndexed);
        WriteOptionalUIntAttribute(writer, "theme", color.Theme, color.HasTheme);
        WriteOptionalDoubleAttribute(writer, "tint", color.Tint, color.HasTint);
        WriteOptionalBoolAttribute(writer, "auto", color.Auto, color.HasAuto);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, color.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteOptionalStringAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length > 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    internal static void WriteOptionalUIntAttribute(XmlWriter writer, string name, uint value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteUnsignedInteger(value));
        }
    }

    internal static void WriteOptionalDoubleAttribute(XmlWriter writer, string name, double value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteDouble(value));
        }
    }

    internal static void WriteOptionalBoolAttribute(XmlWriter writer, string name, bool value, bool hasValue)
    {
        if (hasValue)
        {
            writer.WriteAttributeString(name, XlsxXmlValueWriter.WriteBoolean(value));
        }
    }

    internal static void WriteRawXml(XmlWriter writer, string rawXml)
    {
        if (rawXml != null && rawXml.Length > 0)
        {
            writer.WriteRaw(rawXml);
        }
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        foreach (string item in unknownXml)
        {
            WriteRawXml(writer, item);
        }
    }
}
