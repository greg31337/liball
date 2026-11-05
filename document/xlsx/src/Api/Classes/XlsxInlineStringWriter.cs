using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes inline string data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxInlineStringWriter : IXlsxInlineStringWriter
{
    /// <summary>
    /// Initializes a new inline string writer with its default state.
    /// </summary>
    public XlsxInlineStringWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult WriteString(XlsxInlineString value)
    {
        XlsxInlineStringResult result = WriteBytes(value);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult WriteBytes(XlsxInlineString value)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxInlineStringResult result = WriteStream(stream, value);
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
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInlineStringResult WriteStream(Stream stream, XlsxInlineString value)
    {
        XlsxInlineStringResult result = ValidateInlineString(value);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteInlineStringElement(writer, value);
            writer.WriteEndDocument();
        }

        result.Value = value;
        return result;
    }

    internal static XlsxInlineStringResult ValidateInlineString(XlsxInlineString value)
    {
        XlsxInlineStringResult result = new XlsxInlineStringResult();
        if (value == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inlineStringWriter.valueNull", "Inline string value must not be null."));
            return result;
        }

        if (value.IsRichText && value.RichTextRuns == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inlineStringWriter.richTextRunsNull", "Inline string rich text run list must not be null."));
        }

        return result;
    }

    internal static void WriteInlineStringElement(XmlWriter writer, XlsxInlineString value)
    {
        writer.WriteStartElement("", "is", XlsxWorksheetPartWriter.GetOutputSpreadsheetNamespace(writer));
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, value.UnknownAttributes);
        if (value.IsRichText && value.RichTextRuns != null && value.RichTextRuns.Count > 0)
        {
            foreach (XlsxSharedStringRichTextRun run in value.RichTextRuns)
            {
                XlsxSharedStringTableWriter.WriteRichTextRun(writer, run);
            }
        }
        else
        {
            XlsxSharedStringTableWriter.WriteTextElement(writer, value.Text);
        }

        XlsxSharedStringTableWriter.WritePhoneticTexts(writer, value.PhoneticTexts);
        if (value.HasPhoneticProperties && value.PhoneticPropertiesRawXml != null && value.PhoneticPropertiesRawXml.Length > 0)
        {
            writer.WriteRaw(value.PhoneticPropertiesRawXml);
        }
        else if (value.HasPhoneticProperties)
        {
            XlsxSharedStringTableWriter.WritePhoneticProperties(writer, value.PhoneticProperties);
        }

        XlsxSharedStringTableWriter.WriteUnknownXml(writer, value.UnknownXml);
        writer.WriteEndElement();
    }
}
