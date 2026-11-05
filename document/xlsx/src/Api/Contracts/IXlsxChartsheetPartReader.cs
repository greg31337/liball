using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxChartsheetPartReader"/>.
/// </summary>
public interface IXlsxChartsheetPartReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    XlsxChartsheetPart ReadString(string xml);
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    XlsxChartsheetPart ReadStringWithRelationships(string xml, XlsxRelationshipsPart relationships);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    XlsxChartsheetPart ReadBytes(byte[] bytes);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    XlsxChartsheetPart ReadBytesWithRelationships(byte[] bytes, XlsxRelationshipsPart relationships);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The chartsheet part produced by the operation.</returns>
    XlsxChartsheetPart ReadStream(Stream stream);
}
