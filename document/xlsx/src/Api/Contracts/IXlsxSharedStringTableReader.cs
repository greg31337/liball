using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxSharedStringTableReader"/>.
/// </summary>
public interface IXlsxSharedStringTableReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadString(string xml);
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadStringWithOptions(string xml, XlsxReaderOptions options);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadBytes(byte[] bytes);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadBytesWithOptions(byte[] bytes, XlsxReaderOptions options);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadStream(Stream stream);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The shared string table part produced by the operation.</returns>
    XlsxSharedStringTablePart ReadStreamWithOptions(Stream stream, XlsxReaderOptions options);
}
