using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxPackageRelationshipsReader"/>.
/// </summary>
public interface IXlsxPackageRelationshipsReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadString(string xml);
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadStringWithOptions(string xml, XlsxReaderOptions options);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadBytes(byte[] bytes, XlsxReaderOptions options);
    /// <summary>
    /// Parses package relationships from read-only memory.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadMemory(ReadOnlyMemory<byte> bytes, XlsxReaderOptions options);
    /// <summary>
    /// Parses package relationships from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadSpan(ReadOnlySpan<byte> bytes, XlsxReaderOptions options);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    XlsxRelationshipsPart ReadStream(Stream stream, XlsxReaderOptions options);
}
