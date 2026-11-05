using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxWorksheetPartReader"/>.
/// </summary>
public interface IXlsxWorksheetPartReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    XlsxWorksheetPart ReadString(string xml);
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    XlsxWorksheetPart ReadStringWithRelationships(string xml, XlsxRelationshipsPart relationships);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    XlsxWorksheetPart ReadBytes(byte[] bytes);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    XlsxWorksheetPart ReadBytesWithRelationships(byte[] bytes, XlsxRelationshipsPart relationships);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    XlsxWorksheetPart ReadStream(Stream stream);
}
