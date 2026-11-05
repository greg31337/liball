using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes rich text run data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxRichTextRunWriter : IXlsxRichTextRunWriter
{
    /// <summary>
    /// Initializes a new rich text run writer with its default state.
    /// </summary>
    public XlsxRichTextRunWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "run">The run value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRichTextRunResult WriteString(XlsxRichTextRun run)
    {
        XlsxRichTextRunResult result = WriteBytes(run);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "run">The run value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRichTextRunResult WriteBytes(XlsxRichTextRun run)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxRichTextRunResult result = WriteStream(stream, run);
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
    /// <param name = "run">The run value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRichTextRunResult WriteStream(Stream stream, XlsxRichTextRun run)
    {
        XlsxRichTextRunResult result = ValidateRichTextRun(run);
        if (!result.IsValid)
        {
            return result;
        }

        if (run.RawXml != null && run.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(run.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            result.Run = run;
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            WriteRunElement(writer, run);
            writer.WriteEndDocument();
        }

        result.Run = run;
        return result;
    }

    internal static XlsxRichTextRunResult ValidateRichTextRun(XlsxRichTextRun run)
    {
        XlsxRichTextRunResult result = new XlsxRichTextRunResult();
        if (run == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "richTextRunWriter.runNull", "Rich text run must not be null."));
        }

        return result;
    }

    internal static void WriteRunElement(XmlWriter writer, XlsxRichTextRun run)
    {
        if (run.RawXml != null && run.RawXml.Length > 0)
        {
            writer.WriteRaw(run.RawXml);
            return;
        }

        writer.WriteStartElement("", "r", XlsxNamespaces.Spreadsheet);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, run.UnknownAttributes);
        if (run.PropertiesRawXml != null && run.PropertiesRawXml.Length > 0)
        {
            writer.WriteRaw(run.PropertiesRawXml);
        }

        XlsxSharedStringTableWriter.WriteTextElement(writer, run.Text);
        XlsxSharedStringTableWriter.WriteUnknownXml(writer, run.UnknownXml);
        writer.WriteEndElement();
    }

    internal static XlsxRichTextRun FromSharedStringRun(XlsxSharedStringRichTextRun run)
    {
        XlsxRichTextRun value = new XlsxRichTextRun();
        if (run == null)
        {
            return value;
        }

        value.Text = XlsxCellValueConverter.NormalizeText(run.Text);
        value.RawXml = XlsxCellValueConverter.NormalizeText(run.RawXml);
        value.PropertiesRawXml = XlsxCellValueConverter.NormalizeText(run.PropertiesRawXml);
        foreach (XlsxXmlAttribute attribute in run.UnknownAttributes)
        {
            value.UnknownAttributes.Add(attribute);
        }

        foreach (string unknownXml in run.UnknownXml)
        {
            value.UnknownXml.Add(unknownXml);
        }

        return value;
    }
}
