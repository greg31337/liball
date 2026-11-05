using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads streaming sheet data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxStreamingSheetReader : IXlsxStreamingSheetReader
{
    /// <summary>
    /// Initializes a new streaming sheet reader with its default state.
    /// </summary>
    public XlsxStreamingSheetReader()
    {
    }

    internal XlsxStreamingSheetOpenResult OpenString(string xml)
    {
        return OpenStringWithWorkbookContext(xml, new XlsxSharedStringTablePart(), new XlsxStylesPart(), false);
    }

    internal XlsxStreamingSheetOpenResult OpenStringWithWorkbookContext(string xml, XlsxSharedStringTablePart sharedStrings, XlsxStylesPart styles, bool use1904DateSystem)
    {
        if (xml == null || xml.Length == 0)
        {
            XlsxStreamingSheetOpenResult result = new XlsxStreamingSheetOpenResult();
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingSheet.open.bytesEmpty", "Worksheet XML bytes must not be empty."));
            return result;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return OpenBytesWithWorkbookContext(bytes, sharedStrings, styles, use1904DateSystem);
    }

    /// <summary>
    /// Opens worksheet XML bytes for forward-only row reading.
    /// </summary>
    /// <param name="bytes">The complete worksheet XML bytes.</param>
    /// <returns>The streaming cursor and validation diagnostics.</returns>
    public XlsxStreamingSheetOpenResult OpenBytes(byte[] bytes)
    {
        return OpenBytesWithWorkbookContext(bytes, new XlsxSharedStringTablePart(), new XlsxStylesPart(), false);
    }

    internal XlsxStreamingSheetOpenResult OpenBytesWithWorkbookContext(byte[] bytes, XlsxSharedStringTablePart sharedStrings, XlsxStylesPart styles, bool use1904DateSystem)
    {
        if (bytes == null || bytes.Length == 0)
        {
            XlsxStreamingSheetOpenResult result = new XlsxStreamingSheetOpenResult();
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingSheet.open.bytesEmpty", "Worksheet XML bytes must not be empty."));
            return result;
        }

        MemoryStream stream = new MemoryStream(bytes);
        return OpenStreamWithWorkbookContext(stream, sharedStrings, styles, use1904DateSystem);
    }

    internal XlsxStreamingSheetOpenResult OpenStream(Stream stream)
    {
        return OpenStreamWithWorkbookContext(stream, new XlsxSharedStringTablePart(), new XlsxStylesPart(), false);
    }

    internal XlsxStreamingSheetOpenResult OpenStreamWithWorkbookContext(Stream stream, XlsxSharedStringTablePart sharedStrings, XlsxStylesPart styles, bool use1904DateSystem)
    {
        XlsxStreamingSheetOpenResult result = new XlsxStreamingSheetOpenResult();
        if (stream == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingSheet.open.streamNull", "Worksheet XML stream must not be null."));
            return result;
        }

        if (!stream.CanRead)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingSheet.open.streamNotReadable", "Worksheet XML stream must be readable."));
            return result;
        }

        XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings());
        XlsxStreamingSheetRowCursor cursor = new XlsxStreamingSheetRowCursor(reader, sharedStrings, styles, use1904DateSystem);
        cursor.Initialize();
        result.Cursor = cursor;
        CopyDiagnostics(result, cursor.Validation);
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static void CopyDiagnostics(XlsxStreamingSheetOpenResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
