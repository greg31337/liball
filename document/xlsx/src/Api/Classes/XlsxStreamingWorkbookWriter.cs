using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes streaming workbook data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxStreamingWorkbookWriter : IXlsxStreamingWorkbookWriter
{
    /// <summary>
    /// Initializes a new streaming workbook writer with its default state.
    /// </summary>
    public XlsxStreamingWorkbookWriter()
    {
    }

    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult BeginStream(Stream stream)
    {
        return BeginStreamWithOptions(stream, new XlsxWriterOptions());
    }

    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookWriteResult BeginStreamWithOptions(Stream stream, XlsxWriterOptions options)
    {
        XlsxStreamingWorkbookWriteResult result = new XlsxStreamingWorkbookWriteResult();
        if (stream == null)
        {
            result.AddError("streamingWorkbook.write.streamNull", "Workbook output stream must not be null.");
            return result;
        }

        if (!stream.CanWrite)
        {
            result.AddError("streamingWorkbook.write.streamNotWritable", "Workbook output stream must be writable.");
            return result;
        }

        XlsxWriterOptions writerOptions = options;
        if (writerOptions == null)
        {
            writerOptions = new XlsxWriterOptions();
        }

        XlsxValidationResult optionValidation = writerOptions.Validate();
        foreach (XlsxDiagnostic diagnostic in optionValidation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return result;
        }

        ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        XlsxStreamingWorkbookWriteSession session = new XlsxStreamingWorkbookWriteSession(archive, writerOptions, result);
        result.Session = session;
        return result;
    }
}
