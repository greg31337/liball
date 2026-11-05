using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes shared string table data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxSharedStringTableWriter : IXlsxSharedStringTableWriter
{
    /// <summary>
    /// Initializes a new shared string table writer with its default state.
    /// </summary>
    public XlsxSharedStringTableWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxSharedStringTableWriteResult WriteString(XlsxSharedStringTablePart part)
    {
        XlsxSharedStringTableWriteResult result = WriteBytes(part);
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
    public XlsxSharedStringTableWriteResult WriteBytes(XlsxSharedStringTablePart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxSharedStringTableWriteResult result = WriteStream(stream, part);
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
    public XlsxSharedStringTableWriteResult WriteStream(Stream stream, XlsxSharedStringTablePart part)
    {
        XlsxSharedStringTableWriteResult result = ValidateSharedStringTablePart(part);
        if (!result.IsValid)
        {
            return result;
        }

        List<XlsxSharedStringItem> items = BuildDeduplicatedItems(part);
        result.Count = CalculateReferenceCount(items);
        result.UniqueCount = (uint)items.Count;
        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "sst", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(part.NamespaceUri));
            writer.WriteAttributeString("count", XlsxXmlValueWriter.WriteUnsignedInteger(result.Count));
            writer.WriteAttributeString("uniqueCount", XlsxXmlValueWriter.WriteUnsignedInteger(result.UniqueCount));
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxSharedStringItem item in items)
            {
                WriteSharedStringItem(writer, item);
            }

            WriteUnknownXml(writer, part.UnknownXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxSharedStringTableWriteResult ValidateSharedStringTablePart(XlsxSharedStringTablePart part)
    {
        XlsxSharedStringTableWriteResult result = new XlsxSharedStringTableWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTableWriter.partNull", "Shared string table part must not be null."));
            return result;
        }

        if (part.Items == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTableWriter.itemsNull", "Shared string table items must not be null."));
            return result;
        }

        foreach (XlsxSharedStringItem item in part.Items)
        {
            if (item == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTableWriter.itemNull", "Shared string table items must not contain null entries."));
            }
            else if (item.IsRichText && item.RichTextRuns == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "sharedStringTableWriter.richTextRunsNull", "Shared string rich text run lists must not be null."));
            }
        }

        return result;
    }

    internal static List<XlsxSharedStringItem> BuildDeduplicatedItems(XlsxSharedStringTablePart part)
    {
        List<XlsxSharedStringItem> items = new List<XlsxSharedStringItem>();
        foreach (XlsxSharedStringItem item in part.Items)
        {
            XlsxSharedStringItem? existing = FindEquivalentItem(items, item);
            if (existing != null)
            {
                existing.ReferenceCount = existing.ReferenceCount + GetEffectiveReferenceCount(item);
            }
            else
            {
                XlsxSharedStringItem clone = CloneItem(item);
                clone.ReferenceCount = GetEffectiveReferenceCount(item);
                items.Add(clone);
            }
        }

        return items;
    }

    internal static XlsxSharedStringItem? FindEquivalentItem(List<XlsxSharedStringItem> items, XlsxSharedStringItem item)
    {
        foreach (XlsxSharedStringItem existing in items)
        {
            if (ItemsEquivalent(existing, item))
            {
                return existing;
            }
        }

        return null;
    }

    internal static bool ItemsEquivalent(XlsxSharedStringItem left, XlsxSharedStringItem right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.Text != right.Text || left.IsRichText != right.IsRichText)
        {
            return false;
        }

        if (!left.IsRichText)
        {
            return true;
        }

        if (left.RichTextRuns == null || right.RichTextRuns == null || left.RichTextRuns.Count != right.RichTextRuns.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.RichTextRuns.Count)
        {
            if (!RunsEquivalent(left.RichTextRuns[index], right.RichTextRuns[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool RunsEquivalent(XlsxSharedStringRichTextRun left, XlsxSharedStringRichTextRun right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.Text == right.Text && left.PropertiesRawXml == right.PropertiesRawXml && left.RawXml == right.RawXml;
    }

    internal static XlsxSharedStringItem CloneItem(XlsxSharedStringItem item)
    {
        XlsxSharedStringItem clone = new XlsxSharedStringItem();
        clone.Text = XlsxCellValueConverter.NormalizeText(item.Text);
        clone.IsRichText = item.IsRichText;
        clone.ReferenceCount = item.ReferenceCount;
        foreach (XlsxSharedStringRichTextRun run in item.RichTextRuns)
        {
            clone.RichTextRuns.Add(CloneRun(run));
        }

        foreach (XlsxPhoneticText phoneticText in item.PhoneticTexts)
        {
            clone.PhoneticTexts.Add(ClonePhoneticText(phoneticText));
        }

        clone.PhoneticPropertiesRawXml = XlsxCellValueConverter.NormalizeText(item.PhoneticPropertiesRawXml);
        clone.PhoneticProperties = XlsxCellValue.ClonePhoneticProperties(item.PhoneticProperties);
        clone.HasPhoneticProperties = item.HasPhoneticProperties;
        foreach (XlsxXmlAttribute attribute in item.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(attribute);
        }

        foreach (string unknownXml in item.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxPhoneticText ClonePhoneticText(XlsxPhoneticText phoneticText)
    {
        XlsxPhoneticText clone = new XlsxPhoneticText();
        if (phoneticText == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(phoneticText.Text);
        clone.StartBaseIndex = phoneticText.StartBaseIndex;
        clone.HasStartBaseIndex = phoneticText.HasStartBaseIndex;
        clone.EndBaseIndex = phoneticText.EndBaseIndex;
        clone.HasEndBaseIndex = phoneticText.HasEndBaseIndex;
        clone.RawXml = XlsxCellValueConverter.NormalizeText(phoneticText.RawXml);
        foreach (XlsxXmlAttribute attribute in phoneticText.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(attribute);
        }

        foreach (string unknownXml in phoneticText.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxSharedStringRichTextRun CloneRun(XlsxSharedStringRichTextRun run)
    {
        XlsxSharedStringRichTextRun clone = new XlsxSharedStringRichTextRun();
        if (run == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(run.Text);
        clone.RawXml = XlsxCellValueConverter.NormalizeText(run.RawXml);
        clone.PropertiesRawXml = XlsxCellValueConverter.NormalizeText(run.PropertiesRawXml);
        foreach (XlsxXmlAttribute attribute in run.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(attribute);
        }

        foreach (string unknownXml in run.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static int GetEffectiveReferenceCount(XlsxSharedStringItem item)
    {
        if (item.ReferenceCount > 0)
        {
            return item.ReferenceCount;
        }

        return 1;
    }

    internal static uint CalculateReferenceCount(List<XlsxSharedStringItem> items)
    {
        uint count = 0;
        foreach (XlsxSharedStringItem item in items)
        {
            count = count + (uint)GetEffectiveReferenceCount(item);
        }

        return count;
    }

    internal static void WriteSharedStringItem(XmlWriter writer, XlsxSharedStringItem item)
    {
        writer.WriteStartElement("", "si", XlsxWorksheetPartWriter.GetOutputSpreadsheetNamespace(writer));
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, item.UnknownAttributes);
        if (item.IsRichText && item.RichTextRuns != null && item.RichTextRuns.Count > 0)
        {
            foreach (XlsxSharedStringRichTextRun run in item.RichTextRuns)
            {
                WriteRichTextRun(writer, run);
            }
        }
        else
        {
            WriteTextElement(writer, item.Text);
        }

        WritePhoneticTexts(writer, item.PhoneticTexts);
        if (item.HasPhoneticProperties && item.PhoneticPropertiesRawXml != null && item.PhoneticPropertiesRawXml.Length > 0)
        {
            writer.WriteRaw(item.PhoneticPropertiesRawXml);
        }
        else if (item.HasPhoneticProperties)
        {
            WritePhoneticProperties(writer, item.PhoneticProperties);
        }

        WriteUnknownXml(writer, item.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteRichTextRun(XmlWriter writer, XlsxSharedStringRichTextRun run)
    {
        XlsxRichTextRun value = XlsxRichTextRunWriter.FromSharedStringRun(run);
        XlsxRichTextRunWriter.WriteRunElement(writer, value);
    }

    internal static void WritePhoneticTexts(XmlWriter writer, List<XlsxPhoneticText> phoneticTexts)
    {
        if (phoneticTexts == null)
        {
            return;
        }

        foreach (XlsxPhoneticText phoneticText in phoneticTexts)
        {
            WritePhoneticText(writer, phoneticText);
        }
    }

    internal static void WritePhoneticProperties(XmlWriter writer, XlsxPhoneticProperties properties)
    {
        if (properties == null)
        {
            return;
        }
        writer.WriteStartElement("", "phoneticPr", XlsxWorksheetPartWriter.GetOutputSpreadsheetNamespace(writer));
        if (properties.HasFontId)
        {
            writer.WriteAttributeString("fontId", XlsxXmlValueWriter.WriteUnsignedInteger(properties.FontId));
        }
        if (properties.Type != null && properties.Type.Length > 0)
        {
            writer.WriteAttributeString("type", properties.Type);
        }
        if (properties.Alignment != null && properties.Alignment.Length > 0)
        {
            writer.WriteAttributeString("alignment", properties.Alignment);
        }
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, properties.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WritePhoneticText(XmlWriter writer, XlsxPhoneticText phoneticText)
    {
        if (phoneticText == null)
        {
            return;
        }

        if (phoneticText.RawXml != null && phoneticText.RawXml.Length > 0)
        {
            writer.WriteRaw(phoneticText.RawXml);
            return;
        }

        writer.WriteStartElement("", "rPh", XlsxWorksheetPartWriter.GetOutputSpreadsheetNamespace(writer));
        if (phoneticText.HasStartBaseIndex)
        {
            writer.WriteAttributeString("sb", XlsxXmlValueWriter.WriteUnsignedInteger(phoneticText.StartBaseIndex));
        }

        if (phoneticText.HasEndBaseIndex)
        {
            writer.WriteAttributeString("eb", XlsxXmlValueWriter.WriteUnsignedInteger(phoneticText.EndBaseIndex));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, phoneticText.UnknownAttributes);
        WriteTextElement(writer, phoneticText.Text);
        WriteUnknownXml(writer, phoneticText.UnknownXml);
        writer.WriteEndElement();
    }

    internal static void WriteTextElement(XmlWriter writer, string text)
    {
        string value = XlsxCellValueConverter.NormalizeText(text);
        writer.WriteStartElement("", "t", XlsxWorksheetPartWriter.GetOutputSpreadsheetNamespace(writer));
        if (NeedsPreserveSpace(value))
        {
            writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        }

        writer.WriteString(value);
        writer.WriteEndElement();
    }

    internal static bool NeedsPreserveSpace(string text)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        return IsSpaceCharacter(text[0]) || IsSpaceCharacter(text[text.Length - 1]);
    }

    internal static bool IsSpaceCharacter(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXmlItems)
    {
        foreach (string unknownXml in unknownXmlItems)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }
    }
}
