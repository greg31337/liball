using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingWorkbookReader"/>.
/// </summary>
public interface IXlsxStreamingWorkbookReader
{
    /// <summary>
    /// Opens and parses the requested file and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadFile(string filePath);
    /// <summary>
    /// Opens and parses the requested file and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadFileWithOptions(string filePath, XlsxReaderOptions options);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadBytes(byte[] bytes);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadBytesWithOptions(byte[] bytes, XlsxReaderOptions options);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadStream(Stream stream);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookReadResult ReadStreamWithOptions(Stream stream, XlsxReaderOptions options);
}
