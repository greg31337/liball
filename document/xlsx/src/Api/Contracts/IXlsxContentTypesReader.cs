using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxContentTypesReader"/>.
/// </summary>
public interface IXlsxContentTypesReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The content types part produced by the operation.</returns>
    XlsxContentTypesPart ReadString(string xml);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    XlsxContentTypesPart ReadBytes(byte[] bytes);
    /// <summary>
    /// Parses UTF-8 OOXML from read-only memory.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    XlsxContentTypesPart ReadMemory(ReadOnlyMemory<byte> bytes);
    /// <summary>
    /// Parses UTF-8 OOXML from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    XlsxContentTypesPart ReadSpan(ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The content types part produced by the operation.</returns>
    XlsxContentTypesPart ReadStream(Stream stream);
}
