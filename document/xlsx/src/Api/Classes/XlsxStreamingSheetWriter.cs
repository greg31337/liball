using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes streaming sheet data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxStreamingSheetWriter : IXlsxStreamingSheetWriter
{
    /// <summary>
    /// Initializes a new streaming sheet writer with its default state.
    /// </summary>
    public XlsxStreamingSheetWriter()
    {
    }

    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingSheetWriteResult BeginStream(Stream stream)
    {
        return BeginStreamWithOptions(stream, new XlsxStreamingSheetWriterOptions());
    }

    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingSheetWriteResult BeginStreamWithOptions(Stream stream, XlsxStreamingSheetWriterOptions options)
    {
        XlsxStreamingSheetWriteResult result = new XlsxStreamingSheetWriteResult();
        if (stream == null)
        {
            result.AddError("streamingSheet.write.streamNull", "Worksheet output stream must not be null.");
            return result;
        }

        if (!stream.CanWrite)
        {
            result.AddError("streamingSheet.write.streamNotWritable", "Worksheet output stream must be writable.");
            return result;
        }

        XlsxStreamingSheetWriterOptions writerOptions = options;
        if (writerOptions == null)
        {
            writerOptions = new XlsxStreamingSheetWriterOptions();
        }

        XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings());
        XlsxStreamingSheetWriteSession session = new XlsxStreamingSheetWriteSession(writer, writerOptions, result);
        result.Session = session;
        session.WriteStartWorksheet();
        return result;
    }
}
